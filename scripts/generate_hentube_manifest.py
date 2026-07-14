#!/usr/bin/env python3
"""Generate the Jellyfin plugin repository manifest for HenTube."""

from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPOSITORY = "qbisi/jellyfin-plugin-hentube"
PLUGIN_GUID = "a32c0577-9dd2-431b-94a6-73720e040d86"
PROJECT = Path(__file__).resolve().parent.parent / "Jellyfin.Plugin.MetaTube" / "Jellyfin.Plugin.MetaTube.csproj"


def md5sum(path: Path) -> str:
    digest = hashlib.md5()
    with path.open("rb") as file:
        for chunk in iter(lambda: file.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def target_abi() -> str:
    root = ET.parse(PROJECT).getroot()
    for package in root.iter("PackageReference"):
        if package.attrib.get("Include") in {"Jellyfin.Controller", "Jellyfin.Model"}:
            version = package.attrib.get("Version", "").split("+", 1)[0].split("-", 1)[0]
            if version:
                return f"{version}.0"
    raise RuntimeError(f"Jellyfin package version not found in {PROJECT}")


def empty_manifest() -> list[dict[str, Any]]:
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


def load_existing_manifest() -> list[dict[str, Any]]:
    result = subprocess.run(
        ["git", "show", "refs/remotes/origin/dist:manifest.json"],
        check=False,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        return empty_manifest()

    try:
        manifest = json.loads(result.stdout)
    except json.JSONDecodeError:
        return empty_manifest()

    if (
        not isinstance(manifest, list)
        or not manifest
        or not isinstance(manifest[0], dict)
        or manifest[0].get("guid") != PLUGIN_GUID
        or not isinstance(manifest[0].get("versions"), list)
    ):
        return empty_manifest()

    return manifest


def version_from_filename(path: Path) -> str:
    marker = "@v"
    if marker not in path.name or not path.name.endswith(".zip"):
        raise ValueError(f"Unexpected plugin package name: {path.name}")
    return path.name.split(marker, 1)[1].removesuffix(".zip")


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit(f"usage: {Path(sys.argv[0]).name} JELLYFIN_PLUGIN.zip")

    package = Path(sys.argv[1])
    if not package.is_file():
        raise FileNotFoundError(package)

    version = version_from_filename(package)
    release = {
        "checksum": md5sum(package),
        "changelog": "Use the media basename for metadata searches.",
        "targetAbi": target_abi(),
        "sourceUrl": (
            f"https://github.com/{REPOSITORY}/releases/download/"
            f"v{version}/Jellyfin.HenTube@v{version}.zip"
        ),
        "timestamp": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "version": version,
    }

    manifest = load_existing_manifest()
    manifest[0]["versions"] = [
        item for item in manifest[0]["versions"] if item.get("version") != version
    ]
    manifest[0]["versions"].insert(0, release)

    output = Path(os.environ.get("MANIFEST_OUTPUT", "manifest.json"))
    output.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Generated {output} for HenTube {version} ({release['targetAbi']})")


if __name__ == "__main__":
    main()
