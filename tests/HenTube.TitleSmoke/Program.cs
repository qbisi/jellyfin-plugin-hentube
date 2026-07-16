using Jellyfin.Plugin.MetaTube.Helpers;
using Jellyfin.Plugin.MetaTube.Providers;
using Jellyfin.Plugin.MetaTube.Configuration;
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
AssertEqual(new DateTime(2005, 12, 25, 0, 0, 0, DateTimeKind.Utc), movie.PremiereDate,
    "movie premiere date");
AssertEqual(2005, movie.ProductionYear, "movie production year");
AssertSequence(Array.Empty<string>(), movie.Studios, "movie studios without presets");
AssertSequence(new[] { "Milky", "1080p", "Uncensored" }, movie.Tags, "movie tags without presets");
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
AssertEqual("姉とボイン Vol.1 051225", FilenameTitle.GetOriginalTitle(bareDatePath, "broken"),
    "bare date remains in title");
AssertEqual(false, FilenameTitle.TryGetStructuredTitle(bareDatePath, out _), "bare date is not a bracket field");

var configuration = new PluginConfiguration
{
    RawTagMappings = "s/^date://\ns/^studio:\\(.*\\)$/\\1/I\ns/^UHD$/2160p/I",
    RawStudioPresets = "milky\nQueen Bee\nMILKY",
    RawIgnoredTags = "/^(1080p|2160p|Uncensored)$/Id"
};
AssertSequence(new[] { "milky", "Queen Bee" }, configuration.GetStudioPresets(), "studio preset parsing");
AssertEqual(3, configuration.GetTagMappings().Count, "tag mapping parsing");
AssertEqual(1, configuration.GetIgnoredTags().Count, "ignored tag parsing");

AssertEqual("Xaa", SedSubstitution.TryParse("s/a/X/").Apply("aaa"), "sed first substitution");
AssertEqual("XXX", SedSubstitution.TryParse("s/a/X/g").Apply("aaa"), "sed global substitution");
AssertEqual("<foo-foo>", SedSubstitution.TryParse(@"s/^\(foo\)$/<&-\1>/").Apply("foo"),
    "sed match and capture replacement");
AssertEqual(true, SedDeleteExpression.TryParse(@"/^uncensored$/Id").Matches("Uncensored"),
    "sed case-insensitive delete");

const string localPath = "/media/[date:051225][studio:Milky][UHD][a‖b][Uncensored] 姉とボイン Vol.1.mkv";
AssertEqual(true, FilenameMetadataParser.TryParse(localPath, configuration.GetTagMappings(),
    configuration.GetStudioPresets(),
    configuration.GetIgnoredTags(), out var localMetadata), "local metadata detection");
AssertEqual(title, localMetadata.Title, "local metadata title");
AssertEqual(new DateTime(2005, 12, 25, 0, 0, 0, DateTimeKind.Utc), localMetadata.ReleaseDate,
    "local metadata release date");
AssertSequence(new[] { "Milky" }, localMetadata.Studios, "local metadata studios");
AssertSequence(new[] { "a‖b" }, localMetadata.Tags, "local metadata tags");

const string invalidDatePath = "/media/[991332] Invalid Date.mkv";
AssertEqual(true, FilenameMetadataParser.TryParse(invalidDatePath, Array.Empty<SedSubstitution>(),
    Array.Empty<string>(), Array.Empty<SedDeleteExpression>(), out var invalidDateMetadata),
    "invalid date metadata detection");
AssertEqual(null, invalidDateMetadata.ReleaseDate, "invalid date is not a release date");
AssertSequence(new[] { "991332" }, invalidDateMetadata.Tags, "invalid date becomes tag");

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

static void AssertSequence(IEnumerable<string> expected, IEnumerable<string> actual, string field)
{
    if (!expected.SequenceEqual(actual))
        throw new InvalidOperationException(
            $"Unexpected {field}: expected '[{string.Join(", ", expected)}]', got '[{string.Join(", ", actual)}]'");
}
