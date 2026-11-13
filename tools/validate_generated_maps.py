#!/usr/bin/env python3
"""Fail the build if the generated Tiled header is incomplete or stale."""
from __future__ import annotations
import argparse
from pathlib import Path

REQUIRED = (
    "AREA_GROUND[4][MAPH][MAPW]",
    "AREA_COLLISION[4][MAPH][MAPW]",
    "AREA_OVERLAY[4][4][MAPH][MAPW]",
    "AREA_MARKERS[4][6]",
    "AREA_OBJECTS[4][MAX_MAP_OBJECTS]",
)

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--header", required=True, type=Path)
    args = parser.parse_args()
    if not args.header.is_file():
        raise SystemExit(f"Generated map header missing: {args.header}")
    content = args.header.read_text(encoding="utf-8")
    missing = [name for name in REQUIRED if name not in content]
    if missing:
        raise SystemExit("Generated map header is incomplete: " + ", ".join(missing))
    print(f"Generated map header OK: {args.header} ({len(REQUIRED)} required arrays)")

if __name__ == "__main__":
    main()
