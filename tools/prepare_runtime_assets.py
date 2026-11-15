#!/usr/bin/env python3
"""Restore missing approved assets without overwriting newer tracked artwork."""
from pathlib import Path
from zipfile import ZipFile
import hashlib
import json

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
REQUIRED_IMAGES = {
    "assets/characters/hero/heroine_pixel_4x4_64x80.png": (256, 320),
    "assets/characters/npc/watcher_pixel_4x4_64x80.png": (256, 320),
    "assets/characters/boss/lighthouse_guardian_4x2.png": (256, 160),
    **{f"assets/characters/companions/{name}_idle_64.png": (128, 32)
       for name in ("moss_lumi", "moatail", "crystal_gecko")},
    **{f"assets/characters/companions/{name}_idle_64.png": (128, 32)
       for name in ("leafling", "grove_mote")},
    **{f"assets/characters/portraits/{name}_portrait.png": (1086, 1448)
       for name in ("heroine", "watcher", "guardian", "moss_lumi", "moatail", "crystal_gecko")},
    **{f"assets/environment/generated/{name}_props_sheet.png": (1254, 1254)
       for name in ("nature", "glowglade", "tidal_ruins", "lighthouse_ruins")},
    "assets/environment/coastal_keeper_house.png": (360, 291),
}


def ensure_assets(root=ROOT):
    restored = 0
    for bundle_name in ("MistIsle_ApprovedArt_v7.zip", "MistIsle_MapArt_v8.zip"):
        bundle = root / bundle_name
        if not bundle.exists():
            continue
        with ZipFile(bundle) as archive:
            for member in archive.infolist():
                relative = Path(member.filename)
                if member.is_dir() or relative.is_absolute() or ".." in relative.parts:
                    continue
                # Only recover approved runtime art, not obsolete bundle notes.
                if not relative.parts or relative.parts[0] != "assets":
                    continue
                target = root / relative
                if target.exists():
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(archive.read(member))
                restored += 1
    failures = []
    for relative, expected in REQUIRED_IMAGES.items():
        target = root / relative
        try:
            with Image.open(target) as image:
                actual = image.size
                image.verify()
            if actual != expected:
                failures.append(f"{relative}: expected {expected}, found {actual}")
        except (OSError, ValueError) as error:
            failures.append(f"{relative}: {error}")
    manifest = json.loads((root / "assets/characters/runtime_art.json").read_text(encoding="utf-8"))
    for definition in [manifest["heroine"], manifest["watcher"], *manifest["companions"].values()]:
        target = root / definition["runtime"]
        if target.is_file() and hashlib.sha256(target.read_bytes()).hexdigest() != definition["sha256"]:
            failures.append(f"{definition['runtime']}: differs from the approved v1.0.0 runtime artwork")
    if failures:
        raise SystemExit("Runtime artwork is incomplete. Pull the latest repository files.\n" +
                         "\n".join(failures))
    print(f"Runtime artwork verified ({len(REQUIRED_IMAGES)} images; {restored} missing bundle files restored). "
          "Official v1.0.0 character artwork verified by content hash.")


if __name__ == "__main__":
    ensure_assets()
