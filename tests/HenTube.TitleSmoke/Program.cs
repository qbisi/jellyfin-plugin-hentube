using Jellyfin.Plugin.MetaTube.Helpers;
using Jellyfin.Plugin.MetaTube.Providers;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;

const string path = "/media/[051225][Milky][1080p] 姉とボイン Vol.1 [Uncensored].mkv";
const string basename = "[051225][Milky][1080p] 姉とボイン Vol.1 [Uncensored]";
const string title = "姉とボイン Vol.1";

AssertEqual(basename, FilenameTitle.GetSearchQuery(path, "[Milky"), "search query");
AssertEqual(title, FilenameTitle.GetOriginalTitle(path, "[Milky"), "original title");
AssertEqual("Fallback", FilenameTitle.GetOriginalTitle(string.Empty, "Fallback"), "path fallback");
AssertEqual(true, FilenameTitle.TryGetStructuredTitle(path, out var taggedTitle), "tag detection");
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

var imageProvider = new MovieImageProvider(NullLogger<MovieImageProvider>.Instance);
var supportedImages = imageProvider.GetSupportedImages(movie).ToArray();
AssertEqual(true, supportedImages.Contains(ImageType.Primary), "primary image support");
AssertEqual(true, supportedImages.Contains(ImageType.Thumb), "thumb image support");
AssertEqual(false, supportedImages.Contains(ImageType.Backdrop), "backdrop image support");

foreach (var taggedPath in new[]
         {
             "/media/[Milky‖051225] 姉とボイン Vol.1.mkv",
             "/media/[051225‖Milky] 姉とボイン Vol.1.mkv",
             "/media/[Milky][051225] 姉とボイン Vol.1.mkv",
             "/media/[051225][Milky] 姉とボイン Vol.1.mkv",
             "/media/[Milky] 姉とボイン Vol.1 [051225].mkv"
         })
{
    AssertEqual(title, FilenameTitle.GetOriginalTitle(taggedPath, "broken"), taggedPath);
    AssertEqual(true, FilenameTitle.TryGetStructuredTitle(taggedPath, out var parsedTitle), taggedPath + " detection");
    AssertEqual(title, parsedTitle, taggedPath + " title");
}

const string bareDatePath = "/media/姉とボイン Vol.1 051225.mkv";
AssertEqual(title, FilenameTitle.GetOriginalTitle(bareDatePath, "broken"), "bare date title");
AssertEqual(true, FilenameTitle.TryGetStructuredTitle(bareDatePath, out var bareDateTitle), "bare date detection");
AssertEqual(title, bareDateTitle, "bare date parsed title");

AssertEqual("フラチ", FilenameTitle.SelectMetadataTitle("フラチ", "OVA フラチ #2"),
    "Japanese metadata title");
AssertEqual("OVA フラチ #2", FilenameTitle.SelectMetadataTitle("Frach", "OVA フラチ #2"),
    "English title fallback");
AssertEqual("OVA フラチ #2", FilenameTitle.SelectMetadataTitle("Furachi", "OVA フラチ #2"),
    "romaji title fallback");

static void AssertEqual<T>(T expected, T actual, string field)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Unexpected {field}: expected '{expected}', got '{actual}'");
}
