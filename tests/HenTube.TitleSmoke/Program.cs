using Jellyfin.Plugin.MetaTube.Helpers;
using Jellyfin.Plugin.MetaTube.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;

const string path = "/media/[051225][Milky][1080p] 姉とボイン Vol.1 [Uncensored].mkv";
const string basename = "[051225][Milky][1080p] 姉とボイン Vol.1 [Uncensored]";
const string title = "姉とボイン Vol.1";

AssertEqual(basename, FilenameTitle.GetSearchQuery(path, "[Milky"), "search query");
AssertEqual(title, FilenameTitle.GetOriginalTitle(path, "[Milky"), "original title");
AssertEqual("Fallback", FilenameTitle.GetOriginalTitle(string.Empty, "Fallback"), "path fallback");
AssertEqual(true, FilenameTitle.TryGetTaggedTitle(path, out var taggedTitle), "tag detection");
AssertEqual(title, taggedTitle, "tagged title");

var movie = new Movie { Path = path, Name = "[Milky" };
var provider = new MovieTitleProvider(NullLogger<MovieTitleProvider>.Instance);
var update = await provider.FetchAsync(movie, null!, CancellationToken.None);

AssertEqual(title, movie.Name, "movie name");
AssertEqual(title, movie.OriginalTitle, "movie original title");
AssertEqual(ItemUpdateType.MetadataEdit, update, "update type");

var ordinaryMovie = new Movie { Path = "/media/Ordinary Movie.mkv", Name = "Existing metadata" };
update = await provider.FetchAsync(ordinaryMovie, null!, CancellationToken.None);
AssertEqual("Existing metadata", ordinaryMovie.Name, "ordinary movie name");
AssertEqual(ItemUpdateType.None, update, "ordinary movie update type");

static void AssertEqual<T>(T expected, T actual, string field)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Unexpected {field}: expected '{expected}', got '{actual}'");
}
