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
/media/movies/[studio] title [260522].mkv
```

HenTube sends this query to the configured MetaTube-compatible server:

```text
[studio] title [260522]
```

When Jellyfin does not provide a usable path, HenTube falls back to `info.Name`.

## Compatibility

- Jellyfin 10.11.x
- Emby 4.9.x
- MetaTube-compatible server API

## Upstream

Based on [metatube-community/jellyfin-plugin-metatube](https://github.com/metatube-community/jellyfin-plugin-metatube).
