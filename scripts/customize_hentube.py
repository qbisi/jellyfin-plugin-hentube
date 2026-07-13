#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PLUGIN_GUID = "a32c0577-9dd2-431b-94a6-73720e040d86"


def replace_once(relative_path: str, old: str, new: str) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8")
    if new in text:
        return
    if old in text:
        path.write_text(text.replace(old, new, 1), encoding="utf-8")
        return
    raise RuntimeError(f"Expected text not found in {relative_path}: {old[:80]!r}")


def write_if_changed(relative_path: str, content: str) -> None:
    path = ROOT / relative_path
    if path.read_text(encoding="utf-8") != content:
        path.write_text(content, encoding="utf-8")


def main() -> None:
    plugin = "Jellyfin.Plugin.MetaTube/Plugin.cs"
    replace_once(plugin, 'public const string ProviderName = "MetaTube";',
                 'public const string ProviderName = "HenTube";')
    replace_once(plugin, 'public const string ProviderId = "MetaTube";',
                 'public const string ProviderId = "HenTube";')
    replace_once(plugin, 'public override string Description => "MetaTube Plugin for Jellyfin/Emby";',
                 'public override string Description => "HenTube metadata plugin for Jellyfin/Emby";')
    replace_once(plugin, 'Guid.Parse("01cc53ec-c415-4108-bbd4-a684a9801a32")',
                 f'Guid.Parse("{PLUGIN_GUID}")')

    project = "Jellyfin.Plugin.MetaTube/Jellyfin.Plugin.MetaTube.csproj"
    replacements = {
        "<AssemblyName>MetaTube</AssemblyName>": "<AssemblyName>HenTube</AssemblyName>",
        "<Authors>MetaTube</Authors>": "<Authors>qbisi</Authors>",
        "<Description>MetaTube Plugin for Jellyfin/Emby</Description>":
            "<Description>HenTube metadata plugin for Jellyfin/Emby</Description>",
        "<Copyright>Copyright © 2023 MetaTube</Copyright>":
            "<Copyright>Copyright © 2023 MetaTube contributors; HenTube modifications © 2026 qbisi</Copyright>",
        "https://github.com/metatube-community/jellyfin-plugin-metatube.git":
            "https://github.com/qbisi/jellyfin-plugin-hentube.git",
        "https://github.com/metatube-community/jellyfin-plugin-metatube</PackageProjectUrl>":
            "https://github.com/qbisi/jellyfin-plugin-hentube</PackageProjectUrl>",
        "https://github.com/metatube-community/jellyfin-plugin-metatube/blob/main/LICENSE":
            "https://github.com/qbisi/jellyfin-plugin-hentube/blob/main/LICENSE",
        "<PackageId>MetaTube</PackageId>": "<PackageId>HenTube</PackageId>",
        "<Company>MetaTube</Company>": "<Company>qbisi</Company>",
        "<Product>MetaTube</Product>": "<Product>HenTube</Product>",
        "$(BaseOutputPath)Jellyfin.MetaTube*.zip": "$(BaseOutputPath)Jellyfin.HenTube*.zip",
        "$(BaseOutputPath)Emby.MetaTube*.zip": "$(BaseOutputPath)Emby.HenTube*.zip",
        "$(BaseOutputPath)Jellyfin.MetaTube@v$(Version).zip":
            "$(BaseOutputPath)Jellyfin.HenTube@v$(Version).zip",
        "$(BaseOutputPath)Emby.MetaTube@v$(Version).zip":
            "$(BaseOutputPath)Emby.HenTube@v$(Version).zip",
    }
    for old, new in replacements.items():
        replace_once(project, old, new)

    provider = "Jellyfin.Plugin.MetaTube/Providers/MovieProvider.cs"
    replace_once(
        provider,
        """            // Search movie by name.
            Logger.Info("Search for movie: {0}", info.Name);
            searchResults.AddRange(await ApiClient.SearchMovieAsync(info.Name, pid.Provider, cancellationToken));""",
        """            // Use the media basename so Jellyfin title cleanup does not discard filename metadata.
            var basename = GetMediaBasename(info);
            Logger.Info("Search for movie basename: {0}", basename);
            searchResults.AddRange(await ApiClient.SearchMovieAsync(basename, pid.Provider, cancellationToken));""")
    replace_once(
        provider,
        "    private async Task SetActorImageUrl(PersonInfo actor, CancellationToken cancellationToken)",
        """    private static string GetMediaBasename(MovieInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Path))
            return info.Name;

        var path = info.Path.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar);
        var basename = System.IO.Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(basename) ? info.Name : basename;
    }

    private async Task SetActorImageUrl(PersonInfo actor, CancellationToken cancellationToken)""")

    config_page = "Jellyfin.Plugin.MetaTube/Configuration/configPage.html"
    replace_once(config_page, "<title>MetaTube</title>", "<title>HenTube</title>")
    replace_once(config_page, "<h1>MetaTube</h1>", "<h1>HenTube</h1>")
    replace_once(config_page, "MetaTube Plugin for Jellyfin/Emby.",
                 "HenTube metadata plugin for Jellyfin/Emby.")
    replace_once(config_page, 'pluginUniqueId: "01cc53ec-c415-4108-bbd4-a684a9801a32"',
                 f'pluginUniqueId: "{PLUGIN_GUID}"')

    updater = "Jellyfin.Plugin.MetaTube/ScheduledTasks/UpdatePluginTask.cs"
    replace_once(
        updater,
        "https://api.github.com/repos/metatube-community/jellyfin-plugin-metatube/releases/latest",
        "https://api.github.com/repos/qbisi/jellyfin-plugin-hentube/releases/latest")

    manifest_script = """#!/usr/bin/env python3
import hashlib
import json
import os
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from urllib.error import HTTPError, URLError
from urllib.request import urlopen

from packaging.version import Version


REPOSITORY = "qbisi/jellyfin-plugin-hentube"
PLUGIN_GUID = "a32c0577-9dd2-431b-94a6-73720e040d86"


def md5sum(filename: str) -> str:
    with open(filename, "rb") as file:
        return hashlib.md5(file.read()).hexdigest()


def get_jellyfin_version(csproj: str) -> str:
    root = ET.parse(csproj).getroot()
    for package in root.iter("PackageReference"):
        if package.attrib.get("Include") in ("Jellyfin.Controller", "Jellyfin.Model"):
            return Version(package.attrib["Version"]).base_version
    raise RuntimeError("Jellyfin version not found")


def empty_manifest() -> list[dict]:
    return [
        {
            "guid": PLUGIN_GUID,
            "name": "HenTube",
            "description": "HenTube metadata plugin for Jellyfin/Emby.",
            "overview": "Uses the media file basename when searching a MetaTube-compatible server.",
            "owner": "qbisi",
            "category": "Metadata",
            "imageUrl": (
                "https://raw.githubusercontent.com/"
                f"{REPOSITORY}/main/Jellyfin.Plugin.MetaTube/thumb.png"
            ),
            "versions": [],
        }
    ]


def load_manifest() -> list[dict]:
    url = f"https://raw.githubusercontent.com/{REPOSITORY}/dist/manifest.json"
    try:
        with urlopen(url) as response:
            manifest = json.load(response)
    except (HTTPError, URLError, json.JSONDecodeError):
        return empty_manifest()

    if (
        not isinstance(manifest, list)
        or not manifest
        or manifest[0].get("guid") != PLUGIN_GUID
    ):
        return empty_manifest()
    return manifest


def generate(filename: str, version: str, csproj: str) -> dict:
    return {
        "checksum": md5sum(filename),
        "changelog": "Use the media basename for metadata searches.",
        "targetAbi": f"{get_jellyfin_version(csproj)}.0",
        "sourceUrl": (
            f"https://github.com/{REPOSITORY}/releases/download/"
            f"v{version}/Jellyfin.HenTube@v{version}.zip"
        ),
        "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "version": version,
    }


def main() -> None:
    filename = sys.argv[1]
    version = (
        os.path.basename(filename)
        .split("@", maxsplit=1)[1]
        .removeprefix("v")
        .removesuffix(".zip")
    )
    csproj = os.path.join(
        os.path.dirname(__file__),
        "../Jellyfin.Plugin.MetaTube/Jellyfin.Plugin.MetaTube.csproj",
    )

    manifest = load_manifest()
    manifest[0]["versions"] = [
        item for item in manifest[0]["versions"] if item.get("version") != version
    ]
    manifest[0]["versions"].insert(0, generate(filename, version, csproj))

    with open("manifest.json", "w", encoding="utf-8") as file:
        json.dump(manifest, file, indent=2, ensure_ascii=False)
        file.write("\n")


if __name__ == "__main__":
    main()
"""
    write_if_changed("scripts/manifest.py", manifest_script)

    readme = """# HenTube

HenTube is a fork of the MetaTube Jellyfin/Emby plugin. It has a distinct plugin
identity and sends the media path basename (without the extension) to the
MetaTube-compatible server for movie searches, instead of Jellyfin's cleaned
`info.Name`.

## Jellyfin plugin repository

Add this repository URL in **Dashboard → Plugins → Repositories**:

```text
https://raw.githubusercontent.com/qbisi/jellyfin-plugin-hentube/dist/manifest.json
```

The release workflow publishes the Jellyfin package, release asset, checksum,
and repository manifest automatically after changes reach `main`.

## Compatibility

- Jellyfin 10.11.x
- Emby 4.9.x
- MetaTube-compatible server API

## Upstream

Based on [metatube-community/jellyfin-plugin-metatube](https://github.com/metatube-community/jellyfin-plugin-metatube).
"""
    write_if_changed("README.md", readme)


if __name__ == "__main__":
    main()
