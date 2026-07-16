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

Before remote metadata matching, HenTube derives default local metadata directly
from the full media path. It removes every `[...]` field from the basename and
uses the result as both the title and original title. A valid `[yymmdd]` field
sets the premiere date and production year. Other bracket fields become tags,
except values listed in the studio presets or ignored-tags settings. Studio and
ignored-tag matching is case-insensitive. A field such as `[a‖b]` remains the
single tag `a‖b`; separators inside a field have no special meaning.

The MetaTube-compatible server is optional. When it is not configured, returns
no match, or cannot be reached, the filename-derived title, date, studios, and
tags remain usable without a backend.

Matched titles are Japanese-first. English and romanized provider titles are
never applied: when Japanese metadata is unavailable, HenTube keeps the cleaned
filename title.

HenTube only exposes primary poster and thumbnail images. Backdrop images are
disabled because the current metadata source does not provide suitable widescreen
artwork; poster covers are never repurposed as Jellyfin backdrops.

## Compatibility

- Jellyfin 10.11.x
- Emby 4.9.x
- Optional MetaTube-compatible server API

## Upstream

Based on [metatube-community/jellyfin-plugin-metatube](https://github.com/metatube-community/jellyfin-plugin-metatube).
