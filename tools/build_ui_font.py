#!/usr/bin/env python3
"""Build the shipped, static SemiBold UI font from the upstream Noto Sans SC VF.

Maintainer-only asset step: requires fonttools; normal builds use the checked-in
TTF and need no font tooling. Include every source/UI and lyric character so new
Chinese captions cannot silently render as missing-glyph squares.
"""
from pathlib import Path
import argparse
import re

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True,
                        help="Original Noto Sans SC variable TTF, licensed under OFL")
    args = parser.parse_args()
    points = set(range(32, 127))
    sources = sorted((ROOT / "src").glob("*.[ch]"))
    sources.append(ROOT / "assets/audio/enemy.tsv")
    for path in sources:
        points.update(ord(char) for char in path.read_text(encoding="utf-8")
                      if ord(char) >= 32 and char != "\ufeff")
    font = TTFont(args.source)
    missing = points - set(font.getBestCmap())
    if missing:
        raise SystemExit("Source font is missing codepoints: " +
                         ", ".join(f"U+{cp:04X}" for cp in sorted(missing)))
    # Instantiate real outlines. Merely changing OS/2.usWeightClass does not
    # change how stb_truetype rasterizes a variable font's default weight.
    font = instantiateVariableFont(font, {"wght": 600}, inplace=True)
    names = {1: "Noto Sans SC SemiBold", 2: "Regular",
             4: "Noto Sans SC SemiBold", 6: "NotoSansSC-SemiBold",
             16: "Noto Sans SC", 17: "SemiBold"}
    for name_id, value in names.items():
        font["name"].setName(value, name_id, 3, 1, 0x409)
    options = subset.Options()
    options.name_IDs = [0, 1, 2, 3, 4, 5, 6, 13, 14, 16, 17]
    options.name_legacy = True
    options.name_languages = [0x409]
    subsetter = subset.Subsetter(options=options)
    subsetter.populate(unicodes=points)
    subsetter.subset(font)
    output = ROOT / "assets/fonts/NotoSansSC-SemiBold.ttf"
    font.save(output)
    renderer = ROOT / "src/render.c"
    code = renderer.read_text(encoding="utf-8")
    declaration = "static const int UI_CODEPOINTS[]={" + ",".join(map(str, sorted(points))) + "};"
    code, count = re.subn(r"static const int UI_CODEPOINTS\[\]=\{[^}]*\};",
                         declaration, code, count=1)
    if count != 1:
        raise SystemExit("Cannot find the UI codepoint declaration")
    renderer.write_text(code, encoding="utf-8")
    print(f"Built static SemiBold UI font: {len(points)} codepoints, {output.stat().st_size} bytes")


if __name__ == "__main__":
    main()
