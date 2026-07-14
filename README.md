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

## Compatibility

- Jellyfin 10.11.x
- Emby 4.9.x
- MetaTube-compatible server API

## Upstream

Based on [metatube-community/jellyfin-plugin-metatube](https://github.com/metatube-community/jellyfin-plugin-metatube).
