using Jellyfin.Plugin.MetaTube.Configuration;
using Jellyfin.Plugin.MetaTube.Extensions;
using Jellyfin.Plugin.MetaTube.Helpers;
using Jellyfin.Plugin.MetaTube.Metadata;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MovieInfo = MediaBrowser.Controller.Providers.MovieInfo;
#if __EMBY__
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

#else
using Jellyfin.Data.Enums;
using Microsoft.Extensions.Logging;
#endif

namespace Jellyfin.Plugin.MetaTube.Providers;

#if __EMBY__
public class MovieProvider : BaseProvider, IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder, IHasMetadataFeatures
#else
public class MovieProvider : BaseProvider, IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
#endif
{
    private const string Gfriends = "Gfriends";
    private const string Rating = "JP-18+";

#if __EMBY__
    public MetadataFeatures[] Features => new[]
        { MetadataFeatures.Collections, MetadataFeatures.Adult };

    public MovieProvider(ILogManager logManager) : base(logManager.CreateLogger<MovieProvider>())
#else
    public MovieProvider(ILogger<MovieProvider> logger) : base(logger)
#endif
    {
    }

    public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info,
        CancellationToken cancellationToken)
    {
        var originalTitle = FilenameTitle.GetOriginalTitle(info.Path, info.Name);
        var filenameMetadata = GetFilenameMetadata(info.Path, info.Name);
        if (string.IsNullOrWhiteSpace(Configuration.Server))
        {
            Logger.Info("Use filename metadata because no HenTube server is configured: {0}", originalTitle);
            return filenameMetadata;
        }

        var pid = info.GetPid(Plugin.ProviderId);
        if (string.IsNullOrWhiteSpace(pid.Id) || string.IsNullOrWhiteSpace(pid.Provider))
        {
            // Search movies and pick the first result.
            var firstResult = (await GetSearchResults(info, cancellationToken)).FirstOrDefault();
            if (firstResult == null)
            {
                Logger.Info("Keep filename metadata because no HenTube metadata matched: {0}", originalTitle);
                return filenameMetadata;
            }

            pid = firstResult.GetPid(Plugin.ProviderId);
        }

        Logger.Info("Get movie info: {0}", pid.ToString());

        Jellyfin.Plugin.MetaTube.Metadata.MovieInfo m;
        try
        {
            m = await ApiClient.GetMovieInfoAsync(pid.Provider, pid.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            Logger.Warn("HenTube movie info failed; keep filename metadata: {0}", e.Message);
            return filenameMetadata;
        }

        // Keep the title derived from the basename when no Japanese provider
        // title is available.
        if (string.IsNullOrWhiteSpace(originalTitle)) originalTitle = m.Title;
        var metadataTitle = FilenameTitle.SelectMetadataTitle(m.Title, originalTitle);
        m.Title = metadataTitle;

        // Substitute title.
        if (Configuration.EnableTitleSubstitution)
            m.Title = Configuration.GetTitleSubstitutionTable().Substitute(m.Title);

        // Substitute genres.
        if (Configuration.EnableGenreSubstitution)
            m.Genres = Configuration.GetGenreSubstitutionTable().Substitute(m.Genres).ToArray();

        // Title substitution must never replace the selected Japanese title or
        // the basename fallback.
        m.Title = metadataTitle;

        // Distinct and clean blank list
        m.Genres = m.Genres?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray() ?? Array.Empty<string>();
        m.Actors = m.Actors?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray() ?? Array.Empty<string>();
        m.PreviewImages = m.PreviewImages?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray() ??
                          Array.Empty<string>();

        var result = new MetadataResult<Movie>
        {
            Item = new Movie
            {
                Name = m.Title,
                OriginalTitle = originalTitle,
                Overview = m.Summary,
                OfficialRating = Rating,
                PremiereDate = m.ReleaseDate.GetValidDateTime(),
                ProductionYear = m.ReleaseDate.GetValidYear(),
                Genres = m.Genres?.Any() == true ? m.Genres : Array.Empty<string>()
            },
            HasMetadata = true
        };

        // Set provider id.
        result.Item.SetPid(Name, m.Provider, m.Id, pid.Position);

        // Set trailer url.
        var trailerUrl = !string.IsNullOrWhiteSpace(m.PreviewVideoUrl)
            ? m.PreviewVideoUrl
            : m.PreviewVideoHlsUrl;
        if (!string.IsNullOrWhiteSpace(trailerUrl))
            result.Item.SetTrailerUrl(trailerUrl);

        // Set community rating.
        if (Configuration.EnableRatings)
            result.Item.CommunityRating = m.Score > 0 ? (float)Math.Round(m.Score * 2, 1) : null;

        // Add collection.
        if (Configuration.EnableCollections && !string.IsNullOrWhiteSpace(m.Series))
        {
            result.Item.AddCollection(m.Series);
            Logger.Info("Add Collection for movie {0} [{1}]", pid.ToString(), m.Series);
        }

        // Add studio.
        if (!string.IsNullOrWhiteSpace(m.Maker))
            result.Item.AddStudio(m.Maker);

        // Add tag (series).
        if (!string.IsNullOrWhiteSpace(m.Series))
            result.Item.AddTag(m.Series);

        // Add tag (maker).
        if (!string.IsNullOrWhiteSpace(m.Maker))
            result.Item.AddTag(m.Maker);

        // Add tag (label).
        if (!string.IsNullOrWhiteSpace(m.Label))
            result.Item.AddTag(m.Label);

        // Add actors.
        foreach (var name in m.Actors ?? Enumerable.Empty<string>())
        {
            var actor = new PersonInfo
            {
                Name = name,
#if __EMBY__
                Type = PersonType.Actor,
#else
                Type = PersonKind.Actor,
#endif
            };
            await SetActorImageUrl(actor, cancellationToken);
            result.AddPerson(actor);
        }

        return result;
    }

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Configuration.Server))
            return Array.Empty<RemoteSearchResult>();

        var pid = info.GetPid(Plugin.ProviderId);

        var searchResults = new List<MovieSearchResult>();
        if (string.IsNullOrWhiteSpace(pid.Id) || string.IsNullOrWhiteSpace(pid.Provider))
        {
            // Preserve the complete basename so date/studio tags reach the
            // HenTube server even if Jellyfin already damaged info.Name.
            var query = FilenameTitle.GetSearchQuery(info.Path, info.Name);
            Logger.Info("Search for movie: {0}", query);
            try
            {
                searchResults.AddRange(await ApiClient.SearchMovieAsync(query, pid.Provider, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.Warn("HenTube movie search failed; keep filename metadata: {0}", e.Message);
                return Array.Empty<RemoteSearchResult>();
            }
        }
        else
        {
            // Exact search.
            Logger.Info("Search for movie: {0}", pid.ToString());
            searchResults.Add(await ApiClient.GetMovieInfoAsync(pid.Provider, pid.Id,
                pid.Update != true, cancellationToken));
        }

        var results = new List<RemoteSearchResult>();
        if (!searchResults.Any())
        {
            Logger.Warn("Movie not found: {0}", pid.Id);
            return results;
        }

        foreach (var m in searchResults)
        {
            var result = new RemoteSearchResult
            {
                Name = $"[{m.Provider}] {m.Number} {m.Title}",
                SearchProviderName = Name,
                PremiereDate = m.ReleaseDate.GetValidDateTime(),
                ProductionYear = m.ReleaseDate.GetValidYear(),
                ImageUrl = ApiClient.GetPrimaryImageApiUrl(m.Provider, m.Id, m.ThumbUrl, 1.0, true)
            };
            result.SetPid(Name, m.Provider, m.Id, pid.Position);
            results.Add(result);
        }

        return results;
    }

    private async Task SetActorImageUrl(PersonInfo actor, CancellationToken cancellationToken)
    {
        try
        {
            var results = await ApiClient.SearchActorAsync(actor.Name, cancellationToken);
            if (results?.Any() != true)
            {
                Logger.Warn("Actor not found: {0}", actor.Name);
                return;
            }

            // Use the first result as the primary actor selection.
            var firstResult = results.First();
            if (firstResult.Images?.Any() == true)
            {
                actor.ImageUrl = ApiClient.GetPrimaryImageApiUrl(
                    firstResult.Provider, firstResult.Id, firstResult.Images.First(), 0.5, true);
                actor.SetPid(Name, firstResult.Provider, firstResult.Id);
            }

            // Use the Gfriends to update the actor profile image, if any.
            foreach (var result in results.Where(result => result.Provider == Gfriends && result.Images?.Any() == true))
            {
                actor.ImageUrl = ApiClient.GetPrimaryImageApiUrl(
                    result.Provider, result.Id, result.Images.First(), 0.5, true);
            }
        }
        catch (Exception e)
        {
            Logger.Error("Get actor image error: {0} ({1})", actor.Name, e.Message);
        }
    }

    private static MetadataResult<Movie> GetFilenameMetadata(string path, string fallbackTitle)
    {
        if (FilenameMetadataParser.TryParse(path, Configuration.GetStudioPresets(),
                Configuration.GetIgnoredTags(), out var metadata))
        {
            return new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = metadata.Title,
                    OriginalTitle = metadata.Title,
                    PremiereDate = metadata.ReleaseDate,
                    ProductionYear = metadata.ReleaseDate?.Year,
                    Studios = metadata.Studios,
                    Tags = metadata.Tags
                },
                HasMetadata = true
            };
        }

        var title = FilenameTitle.GetOriginalTitle(path, fallbackTitle);
        if (string.IsNullOrWhiteSpace(title)) return new MetadataResult<Movie>();

        return new MetadataResult<Movie>
        {
            Item = new Movie
            {
                Name = title,
                OriginalTitle = title
            },
            HasMetadata = true
        };
    }
}
