#!/usr/bin/env python3
"""Rasterize Mist Isle SVG source art to transparent PNGs.

Install: python -m pip install cairosvg
Run:    python tools/rasterize_assets.py
"""
from pathlib import Path
import cairosvg

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "assets" / "characters" / "svg"
OUT = ROOT / "assets" / "characters" / "png"
OUT.mkdir(parents=True, exist_ok=True)

for svg in sorted(SRC.glob("*.svg")):
    target = OUT / f"{svg.stem}.png"
    cairosvg.svg2png(url=str(svg), write_to=str(target),
                     output_width=256, output_height=256)
    print(f"created {target.relative_to(ROOT)}")
