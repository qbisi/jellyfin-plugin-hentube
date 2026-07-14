#!/usr/bin/env python3
"""Apply the remaining build-time HenTube branding substitutions.

Business logic, documentation, and manifest generation live directly in the
repository.  This script only patches upstream-facing resources that still use
MetaTube identifiers in the checked-in source tree.
"""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PLUGIN_GUID = "a32c0577-9dd2-431b-94a6-73720e040d86"


def replace_once(relative_path: str, old: str, new: str) -> None:
    path = ROOT / relative_path
    text = path.read_text(encoding="utf-8")
    if new in text:
        return
    if old not in text:
        raise RuntimeError(
            f"Expected either original or customized text in {relative_path}: "
            f"{old[:80]!r}"
        )
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


def main() -> None:
    plugin = "Jellyfin.Plugin.MetaTube/Plugin.cs"
    replace_once(
        plugin,
        'public const string ProviderName = "MetaTube";',
        'public const string ProviderName = "HenTube";',
    )
    replace_once(
        plugin,
        'public const string ProviderId = "MetaTube";',
        'public const string ProviderId = "HenTube";',
    )
    replace_once(
        plugin,
        'public override string Description => "MetaTube Plugin for Jellyfin/Emby";',
        'public override string Description => "HenTube metadata plugin for Jellyfin/Emby";',
    )
    replace_once(
        plugin,
        'Guid.Parse("01cc53ec-c415-4108-bbd4-a684a9801a32")',
        f'Guid.Parse("{PLUGIN_GUID}")',
    )

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

    config_page = "Jellyfin.Plugin.MetaTube/Configuration/configPage.html"
    replace_once(config_page, "<title>MetaTube</title>", "<title>HenTube</title>")
    replace_once(config_page, "<h1>MetaTube</h1>", "<h1>HenTube</h1>")
    replace_once(
        config_page,
        "MetaTube Plugin for Jellyfin/Emby.",
        "HenTube metadata plugin for Jellyfin/Emby.",
    )
    replace_once(
        config_page,
        'pluginUniqueId: "01cc53ec-c415-4108-bbd4-a684a9801a32"',
        f'pluginUniqueId: "{PLUGIN_GUID}"',
    )

    updater = "Jellyfin.Plugin.MetaTube/ScheduledTasks/UpdatePluginTask.cs"
    replace_once(
        updater,
        "https://api.github.com/repos/metatube-community/jellyfin-plugin-metatube/releases/latest",
        "https://api.github.com/repos/qbisi/jellyfin-plugin-hentube/releases/latest",
    )


if __name__ == "__main__":
    main()
