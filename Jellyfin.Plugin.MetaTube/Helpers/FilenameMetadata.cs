using System.Globalization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.MetaTube.Helpers;

internal sealed record FilenameMetadata(
    string Title,
    DateTime? ReleaseDate,
    string[] Studios,
    string[] Tags);

internal static partial class FilenameMetadataParser
{
    public static bool TryParse(
        string path,
        IEnumerable<SedSubstitution> tagMappings,
        IEnumerable<string> studioPresets,
        IEnumerable<SedDeleteExpression> ignoredTags,
        out FilenameMetadata metadata)
    {
        var basename = GetBasename(path);
        var matches = BracketTagRegex().Matches(basename);
        if (string.IsNullOrWhiteSpace(basename) || matches.Count == 0)
        {
            metadata = null!;
            return false;
        }

        var studios = new HashSet<string>(studioPresets ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        var mappings = tagMappings?.ToArray() ?? Array.Empty<SedSubstitution>();
        var ignored = ignoredTags?.ToArray() ?? Array.Empty<SedDeleteExpression>();
        var matchedStudios = new List<string>();
        var tags = new List<string>();
        DateTime? releaseDate = null;

        foreach (Match match in matches)
        {
            var value = match.Groups[1].Value.Trim();
            foreach (var mapping in mappings)
                value = mapping.Apply(value);
            value = value.Trim();

            if (value.Length == 0)
                continue;

            if (TryParseShortDate(value, DateTime.UtcNow, out var date))
            {
                releaseDate ??= date;
                continue;
            }

            if (studios.Contains(value))
            {
                AddDistinct(matchedStudios, value);
                continue;
            }

            if (!ignored.Any(rule => rule.Matches(value)))
                AddDistinct(tags, value);
        }

        var title = WhitespaceRegex().Replace(BracketTagRegex().Replace(basename, " "), " ").Trim();
        if (title.Length == 0)
        {
            metadata = null!;
            return false;
        }

        metadata = new FilenameMetadata(title, releaseDate, matchedStudios.ToArray(), tags.ToArray());
        return true;
    }

    internal static bool TryParseShortDate(string value, DateTime now, out DateTime date)
    {
        date = default;
        if (!ShortDateRegex().IsMatch(value))
            return false;

        var shortYear = int.Parse(value.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var month = int.Parse(value.AsSpan(2, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(value.AsSpan(4, 2), CultureInfo.InvariantCulture);
        var year = shortYear <= (now.Year + 1) % 100 ? 2000 + shortYear : 1900 + shortYear;

        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return false;

        date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        return true;
    }

    internal static string GetBasename(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetFileNameWithoutExtension(trimmed);
    }

    private static void AddDistinct(List<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            values.Add(value);
    }

    [GeneratedRegex(@"\[([^\[\]]*)\]")]
    private static partial Regex BracketTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^\d{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ShortDateRegex();
}
