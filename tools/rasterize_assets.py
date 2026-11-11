#!/usr/bin/env python3
"""Rasterize editable Mist Isle SVG character sheets to pixel-crisp PNGs.
Install: python -m pip install cairosvg
Run from repository root: python tools/rasterize_assets.py
"""
from pathlib import Path
import cairosvg

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "assets" / "characters" / "svg"
OUT = ROOT / "assets" / "characters" / "png"
OUT.mkdir(parents=True, exist_ok=True)
TILE_SRC = ROOT / "assets" / "tiles"
TILE_OUT = TILE_SRC / "png"
TILE_OUT.mkdir(parents=True, exist_ok=True)

SIZES = {
    "hero_walk_4dir.svg": (128, 128),
    "lumi_idle_pixel.svg": (64, 32),
    "moatail_idle_pixel.svg": (64, 32),
    "gecko_idle_pixel.svg": (64, 32),
}
for svg in sorted(SRC.glob("*.svg")):
    width, height = SIZES.get(svg.name, (256, 256))
    target = OUT / f"{svg.stem}.png"
    cairosvg.svg2png(url=str(svg), write_to=str(target),
                     output_width=width, output_height=height)
    print(f"created {target.relative_to(ROOT)}")

for svg in sorted(TILE_SRC.glob("*.svg")):
    target = TILE_OUT / f"{svg.stem}.png"
    cairosvg.svg2png(url=str(svg), write_to=str(target),
                     output_width=384, output_height=32)
    print(f"created {target.relative_to(ROOT)}")
