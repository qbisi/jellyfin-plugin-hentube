using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.MetaTube.Helpers;

internal static partial class FilenameTitle
{
    public static bool TryGetTaggedTitle(string path, out string title)
    {
        var basename = GetBasename(path);
        if (string.IsNullOrWhiteSpace(basename) || !BracketTagRegex().IsMatch(basename))
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

    private static string CleanTitle(string value)
    {
        var title = BracketTagRegex().Replace(value, " ");
        return WhitespaceRegex().Replace(title, " ").Trim();
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
}
