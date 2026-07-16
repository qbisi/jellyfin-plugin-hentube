using Jellyfin.Plugin.MetaTube.Helpers;
#if __EMBY__
using System.ComponentModel;
using Emby.Web.GenericEdit;
using MediaBrowser.Model.Attributes;

#else
using MediaBrowser.Model.Plugins;
#endif

namespace Jellyfin.Plugin.MetaTube.Configuration;

#if __EMBY__
public class PluginConfiguration : EditableOptionsBase
{
    public override string EditorTitle => Plugin.ProviderName;
#else
public class PluginConfiguration : BasePluginConfiguration
{
#endif

#if __EMBY__
    [DisplayName("Server")]
    [Description("Optional MetaTube Server URL. Leave blank to use filename metadata only.")]
#endif
    public string Server { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("Token")]
    [Description("Access token for the MetaTube Server, or blank if no token is set by the backend.")]
#endif
    public string Token { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("Studio presets")]
    [Description("Bracket tags matching one of these values are stored as studios. One value per line.")]
    [EditMultiline(5)]
#endif
    public string RawStudioPresets { get; set; } = string.Empty;

#if __EMBY__
    [DisplayName("Ignored filename tags")]
    [Description("Bracket tags matching one of these values are not added to the movie. One value per line.")]
    [EditMultiline(5)]
#endif
    public string RawIgnoredTags { get; set; } = string.Empty;

    public IReadOnlyList<string> GetStudioPresets() => ParseLines(RawStudioPresets);

    public IReadOnlyList<string> GetIgnoredTags() => ParseLines(RawIgnoredTags);

#if __EMBY__
    [DisplayName("Enable auto update")]
    [Description("Automatically update the plugin through scheduled tasks.")]
    public bool EnableAutoUpdate { get; set; } = true;
#endif

#if __EMBY__
    [DisplayName("Enable collections")]
    [Description("Automatically create collections by series.")]
#endif
    public bool EnableCollections { get; set; } = false;

#if __EMBY__
    [DisplayName("Enable ratings")]
    [Description("Display community ratings from the original website.")]
#endif
    public bool EnableRatings { get; set; } = true;

#if __EMBY__
    [DisplayName("Enable trailers")]
    [Description("Generate online video trailers in strm format.")]
#endif
    public bool EnableTrailers { get; set; } = false;

#if __EMBY__
    [DisplayName("Primary image ratio")]
    [Description("Aspect ratio for primary images, set a negative value to use the default.")]
#endif
    public double PrimaryImageRatio { get; set; } = -1;

#if __EMBY__
    [DisplayName("Default image quality")]
    [Description("Default compression quality for JPEG images, set between 0 and 100. (default: 90)")]
    [MinValue(0)]
    [MaxValue(100)]
    [Required]
#endif
    public int DefaultImageQuality { get; set; } = 90;

#if __EMBY__
    [DisplayName("Enable title substitution")]
#endif
    public bool EnableTitleSubstitution { get; set; } = false;

#if __EMBY__
    [DisplayName("Title substitution table")]
    [Description(
        "One record per line, separated by equal signs. Leave the target substring blank to delete the source substring.")]
    [EditMultiline(5)]
#endif
    public string TitleRawSubstitutionTable
    {
        get => _titleSubstitutionTable?.ToString();
        set => _titleSubstitutionTable = SubstitutionTable.Parse(value);
    }

    public SubstitutionTable GetTitleSubstitutionTable()
    {
        return _titleSubstitutionTable;
    }

    private SubstitutionTable _titleSubstitutionTable;

#if __EMBY__
    [DisplayName("Enable genre substitution")]
#endif
    public bool EnableGenreSubstitution { get; set; } = false;

#if __EMBY__
    [DisplayName("Title substitution table")]
    [Description(
        "One record per line, separated by equal signs. Leave the target genre blank to delete the source genre.")]
    [EditMultiline(5)]
#endif
    public string GenreRawSubstitutionTable
    {
        get => _genreSubstitutionTable?.ToString();
        set => _genreSubstitutionTable = SubstitutionTable.Parse(value);
    }

    public SubstitutionTable GetGenreSubstitutionTable()
    {
        return _genreSubstitutionTable;
    }

    private SubstitutionTable _genreSubstitutionTable;

    private static IReadOnlyList<string> ParseLines(string value)
    {
        return (value ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
