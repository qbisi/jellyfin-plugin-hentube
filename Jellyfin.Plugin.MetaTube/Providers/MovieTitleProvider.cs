#if !__EMBY__
using Jellyfin.Plugin.MetaTube.Helpers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaTube.Providers;

/// <summary>
/// Applies the local filename metadata before remote metadata runs.
/// </summary>
public sealed class MovieTitleProvider : ICustomMetadataProvider<Movie>, IPreRefreshProvider, IHasOrder
{
    private readonly ILogger<MovieTitleProvider> _logger;

    public MovieTitleProvider(ILogger<MovieTitleProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "HenTube Filename Metadata";

    public int Order => -1000;

    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configuration = Plugin.Instance?.Configuration ?? new Configuration.PluginConfiguration();
        if (!FilenameMetadataParser.TryParse(item.Path, configuration.GetStudioPresets(),
                configuration.GetIgnoredTags(), out var metadata))
            return Task.FromResult(ItemUpdateType.None);

        var changed = !string.Equals(item.Name, metadata.Title, StringComparison.Ordinal) ||
                      !string.Equals(item.OriginalTitle, metadata.Title, StringComparison.Ordinal) ||
                      item.PremiereDate != metadata.ReleaseDate ||
                      item.ProductionYear != metadata.ReleaseDate?.Year ||
                      !(item.Studios ?? Array.Empty<string>()).SequenceEqual(metadata.Studios) ||
                      !(item.Tags ?? Array.Empty<string>()).SequenceEqual(metadata.Tags);

        if (!changed)
            return Task.FromResult(ItemUpdateType.None);

        _logger.LogInformation("Apply filename metadata: {OldTitle} => {Title}", item.Name, metadata.Title);
        item.Name = metadata.Title;
        item.OriginalTitle = metadata.Title;
        item.PremiereDate = metadata.ReleaseDate;
        item.ProductionYear = metadata.ReleaseDate?.Year;
        item.Studios = metadata.Studios;
        item.Tags = metadata.Tags;

        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}
#endif
