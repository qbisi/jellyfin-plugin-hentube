using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.MetaTube.Helpers;

internal static partial class FilenameTitle
{
    public static bool TryGetStructuredTitle(string path, out string title)
    {
        if (!FilenameMetadataParser.TryParse(path, Array.Empty<SedSubstitution>(), Array.Empty<string>(),
                Array.Empty<SedDeleteExpression>(), out var metadata))
        {
            title = string.Empty;
            return false;
        }

        title = metadata.Title;
        return true;
    }

    public static string GetSearchQuery(string path, string fallback)
    {
        var basename = FilenameMetadataParser.GetBasename(path);
        return !string.IsNullOrWhiteSpace(basename) ? basename : fallback?.Trim() ?? string.Empty;
    }

    public static string GetOriginalTitle(string path, string fallback)
    {
        var basename = FilenameMetadataParser.GetBasename(path);
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
        return WhitespaceRegex().Replace(title, " ").Trim();
    }

    [GeneratedRegex(@"\[[^\[\]]*\]")]
    private static partial Regex BracketTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[\u3005-\u3007\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uff66-\uff9f]")]
    private static partial Regex JapaneseTextRegex();
}
