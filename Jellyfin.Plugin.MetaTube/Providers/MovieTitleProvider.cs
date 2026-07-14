#if !__EMBY__
using Jellyfin.Plugin.MetaTube.Helpers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetaTube.Providers;

/// <summary>
/// Corrects Jellyfin's one-tag filename parsing before remote metadata runs.
/// This is deliberately independent of HenTube server matching.
/// </summary>
public sealed class MovieTitleProvider : ICustomMetadataProvider<Movie>, IPreRefreshProvider, IHasOrder
{
    private readonly ILogger<MovieTitleProvider> _logger;

    public MovieTitleProvider(ILogger<MovieTitleProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "HenTube Filename Title";

    public int Order => -1000;

    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Leave ordinary movie names to Jellyfin and other metadata providers.
        if (!FilenameTitle.TryGetTaggedTitle(item.Path, out var title))
            return Task.FromResult(ItemUpdateType.None);

        if (!string.Equals(item.Name, title, StringComparison.Ordinal) ||
            !string.Equals(item.OriginalTitle, title, StringComparison.Ordinal))
        {
            _logger.LogInformation("Correct movie title from filename: {OldTitle} => {Title}", item.Name, title);
            item.Name = title;
            item.OriginalTitle = title;
            return Task.FromResult(ItemUpdateType.MetadataEdit);
        }

        return Task.FromResult(ItemUpdateType.None);
    }
}
#endif
