using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.MetaTube.Helpers;

internal sealed class SedSubstitution
{
    private readonly Regex _regex;
    private readonly string _replacement;
    private readonly bool _global;

    private SedSubstitution(Regex regex, string replacement, bool global)
    {
        _regex = regex;
        _replacement = replacement;
        _global = global;
    }

    public string Apply(string value)
    {
        return _regex.Replace(value, match => ExpandReplacement(match, _replacement), _global ? -1 : 1);
    }

    public static IReadOnlyList<SedSubstitution> ParseLines(string value)
    {
        return SedExpression.Lines(value)
            .Select(TryParse)
            .Where(rule => rule is not null)
            .Cast<SedSubstitution>()
            .ToArray();
    }

    internal static SedSubstitution TryParse(string expression)
    {
        expression = expression.Trim();
        if (expression.Length < 4 || expression[0] != 's')
            return null;

        var delimiter = expression[1];
        if (char.IsLetterOrDigit(delimiter) || char.IsWhiteSpace(delimiter) || delimiter == '\\')
            return null;

        var index = 2;
        if (!SedExpression.TryReadSection(expression, delimiter, ref index, out var pattern) ||
            !SedExpression.TryReadSection(expression, delimiter, ref index, out var replacement))
            return null;

        var flags = expression[index..].Trim();
        if (flags.Any(flag => flag is not ('g' or 'i' or 'I')))
            return null;

        try
        {
            var options = RegexOptions.CultureInvariant;
            if (flags.Contains('i') || flags.Contains('I'))
                options |= RegexOptions.IgnoreCase;
            return new SedSubstitution(new Regex(SedExpression.NormalizePattern(pattern), options), replacement,
                flags.Contains('g'));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string ExpandReplacement(Match match, string replacement)
    {
        var result = new StringBuilder();
        for (var index = 0; index < replacement.Length; index++)
        {
            var current = replacement[index];
            if (current == '&')
            {
                result.Append(match.Value);
                continue;
            }

            if (current != '\\' || index + 1 >= replacement.Length)
            {
                result.Append(current);
                continue;
            }

            var escaped = replacement[++index];
            if (escaped is >= '1' and <= '9')
            {
                var group = match.Groups[escaped - '0'];
                if (group.Success)
                    result.Append(group.Value);
            }
            else
            {
                result.Append(escaped);
            }
        }

        return result.ToString();
    }
}

internal sealed class SedDeleteExpression
{
    private readonly Regex _regex;

    private SedDeleteExpression(Regex regex)
    {
        _regex = regex;
    }

    public bool Matches(string value) => _regex.IsMatch(value);

    public static IReadOnlyList<SedDeleteExpression> ParseLines(string value)
    {
        return SedExpression.Lines(value)
            .Select(TryParse)
            .Where(rule => rule is not null)
            .Cast<SedDeleteExpression>()
            .ToArray();
    }

    internal static SedDeleteExpression TryParse(string expression)
    {
        expression = expression.Trim();
        if (expression.Length < 3 || expression[0] != '/')
            return null;

        var index = 1;
        if (!SedExpression.TryReadSection(expression, '/', ref index, out var pattern))
            return null;

        var command = expression[index..].Trim();
        if (!command.EndsWith('d'))
            return null;

        var flags = command[..^1];
        if (flags.Any(flag => flag is not ('i' or 'I')))
            return null;

        try
        {
            var options = RegexOptions.CultureInvariant;
            if (flags.Contains('i') || flags.Contains('I'))
                options |= RegexOptions.IgnoreCase;
            return new SedDeleteExpression(new Regex(SedExpression.NormalizePattern(pattern), options));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

internal static class SedExpression
{
    public static IEnumerable<string> Lines(string value)
    {
        return (value ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'));
    }

    public static bool TryReadSection(string expression, char delimiter, ref int index, out string value)
    {
        var result = new StringBuilder();
        while (index < expression.Length)
        {
            var current = expression[index++];
            if (current == delimiter)
            {
                value = result.ToString();
                return true;
            }

            if (current == '\\' && index < expression.Length)
            {
                var escaped = expression[index++];
                if (escaped != delimiter)
                    result.Append('\\');
                result.Append(escaped);
                continue;
            }

            result.Append(current);
        }

        value = string.Empty;
        return false;
    }

    public static string NormalizePattern(string pattern)
    {
        return pattern
            .Replace(@"\(", "(", StringComparison.Ordinal)
            .Replace(@"\)", ")", StringComparison.Ordinal)
            .Replace(@"\+", "+", StringComparison.Ordinal)
            .Replace(@"\?", "?", StringComparison.Ordinal)
            .Replace(@"\|", "|", StringComparison.Ordinal)
            .Replace(@"\{", "{", StringComparison.Ordinal)
            .Replace(@"\}", "}", StringComparison.Ordinal);
    }
}
