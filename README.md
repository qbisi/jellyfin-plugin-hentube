# HenTube

HenTube is a fork of the MetaTube Jellyfin/Emby plugin. It has a distinct plugin identity and sends the media path basename (without the extension) to the MetaTube-compatible server for movie searches, instead of Jellyfin's cleaned `info.Name`.

## Jellyfin plugin repository

Add this repository URL in **Dashboard → Plugins → Repositories**:

```text
https://raw.githubusercontent.com/qbisi/jellyfin-plugin-hentube/dist/manifest.json
```

The release workflow publishes the Jellyfin package, GitHub Release asset, checksum, and repository manifest automatically after changes reach `main`.

## Search behavior

For a media path such as:

```text
    /media/movies/[260522][studio] title.mkv
```

HenTube sends this query to the configured MetaTube-compatible server:

```text
    [260522][studio] title
```

When Jellyfin does not provide a usable path, HenTube falls back to `info.Name`.

Before remote metadata matching, HenTube also derives the local movie title from
the media path and removes every `[...]` tag. This corrects Jellyfin's partial
parsing of names such as `[date][studio] title` even when the HenTube server is
offline or returns no match. A successful match may replace the display name
with the provider title, while `OriginalTitle` keeps the tag-free filename title.
The complete basename is sent unchanged, so the server can classify date and
studio fields independently of their order. Combined `[studio‖date]` and
`[date‖studio]`, separate or trailing tags, and filenames without tags are all
supported.

Matched titles are Japanese-first. English and romanized provider titles are
never applied: when Japanese metadata is unavailable, HenTube keeps the cleaned
filename title. Title translation is skipped for HenTube even when it is enabled
in the plugin configuration; summary translation continues to work normally.

HenTube only exposes primary poster and thumbnail images. Backdrop images are
disabled because the current metadata source does not provide suitable widescreen
artwork; poster covers are never repurposed as Jellyfin backdrops.

## Compatibility

- Jellyfin 10.11.x
- Emby 4.9.x
- MetaTube-compatible server API

## Upstream

Based on [metatube-community/jellyfin-plugin-metatube](https://github.com/metatube-community/jellyfin-plugin-metatube).
