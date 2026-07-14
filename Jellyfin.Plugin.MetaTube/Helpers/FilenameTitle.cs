using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.MetaTube.Helpers;

internal static partial class FilenameTitle
{
    public static bool TryGetStructuredTitle(string path, out string title)
    {
        var basename = GetBasename(path);
        if (string.IsNullOrWhiteSpace(basename) ||
            (!BracketTagRegex().IsMatch(basename) && !HasStandaloneDate(basename)))
        {
            title = string.Empty;
            return false;
        }

        title = CleanTitle(basename);
        return !string.IsNullOrWhiteSpace(title);
    }

    public static string GetSearchQuery(string path, string fallback)
    {
        var basename = GetBasename(path);
        return !string.IsNullOrWhiteSpace(basename) ? basename : fallback?.Trim() ?? string.Empty;
    }

    public static string GetOriginalTitle(string path, string fallback)
    {
        var basename = GetBasename(path);
        if (string.IsNullOrWhiteSpace(basename))
            basename = fallback ?? string.Empty;

        var title = CleanTitle(basename);
        return !string.IsNullOrWhiteSpace(title) ? title : fallback?.Trim() ?? string.Empty;
    }

    public static string SelectMetadataTitle(string matchedTitle, string originalTitle)
    {
        if (!string.IsNullOrWhiteSpace(matchedTitle) && JapaneseTextRegex().IsMatch(matchedTitle))
            return matchedTitle.Trim();

        return originalTitle?.Trim() ?? string.Empty;
    }

    private static string CleanTitle(string value)
    {
        var title = BracketTagRegex().Replace(value, " ");
        title = StandaloneDateRegex().Replace(title,
            match => IsShortDate(match.Value) ? " " : match.Value);
        return WhitespaceRegex().Replace(title, " ").Trim();
    }

    private static bool HasStandaloneDate(string value)
    {
        foreach (Match match in StandaloneDateRegex().Matches(value))
        {
            if (IsShortDate(match.Value)) return true;
        }

        return false;
    }

    private static bool IsShortDate(string value)
    {
        if (value.Length != 6 || !int.TryParse(value.AsSpan(2, 2), out var month) ||
            !int.TryParse(value.AsSpan(4, 2), out var day) || month is < 1 or > 12)
            return false;

        return day >= 1 && day <= DateTime.DaysInMonth(2000, month);
    }

    private static string GetBasename(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetFileNameWithoutExtension(trimmed);
    }

    [GeneratedRegex(@"\[[^\[\]]*\]")]
    private static partial Regex BracketTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?<!\S)\d{6}(?!\S)")]
    private static partial Regex StandaloneDateRegex();

    [GeneratedRegex(@"[\u3005-\u3007\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uff66-\uff9f]")]
    private static partial Regex JapaneseTextRegex();
}
