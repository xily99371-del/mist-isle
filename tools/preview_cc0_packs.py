#!/usr/bin/env python3
"""Create review sheets from the vendored Kenney CC0 RPG Base and Pixel UI archives."""
from pathlib import Path
from zipfile import ZipFile
from PIL import Image, ImageDraw
import io, math

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "build" / "art-review"
OUT.mkdir(parents=True, exist_ok=True)

PACKS = [
    ("kenney-rpg-base", ROOT / "third_party/kenney/rpg-base/KenneyRPGpack.zip"),
    ("kenney-pixel-ui", ROOT / "third_party/kenney/pixel-ui/PixelUIpack.zip"),
]

def make_sheet(label, archive):
    items = []
    with ZipFile(archive) as z:
        for name in z.namelist():
            if not name.lower().endswith(".png") or any(k in name.lower() for k in ("preview", "sample")):
                continue
            try:
                im = Image.open(io.BytesIO(z.read(name))).convert("RGBA")
            except Exception:
                continue
            if im.width > 1024 or im.height > 1024:
                continue
            if im.getbbox() is None:
                continue
            items.append((name, im.copy()))
    items.sort(key=lambda item: item[0].lower())
    if not items:
        raise SystemExit(f"No usable PNGs found in {archive}")
    cols, cell_w, cell_h = 8, 144, 132
    per_page = 96
    pages = math.ceil(len(items) / per_page)
    for page in range(pages):
        subset = items[page*per_page:(page+1)*per_page]
        rows = math.ceil(len(subset) / cols)
        sheet = Image.new("RGB", (cols*cell_w, rows*cell_h), (239, 235, 218))
        d = ImageDraw.Draw(sheet)
        for i, (name, im) in enumerate(subset):
            x, y = (i % cols)*cell_w, (i // cols)*cell_h
            im.thumbnail((120, 96), Image.Resampling.NEAREST)
            tile = Image.new("RGBA", (120, 96), (239, 235, 218, 255))
            tile.alpha_composite(im, ((120-im.width)//2, (96-im.height)//2))
            sheet.paste(tile.convert("RGB"), (x+12, y+2))
            d.text((x+4, y+101), name.split("/")[-1][:22], fill=(55, 66, 55))
        out = OUT / f"{label}-{page+1:02d}.jpg"
        sheet.save(out, quality=90)
        print(f"Preview: {len(subset)} sprites -> {out}")
    print(f"{label}: {len(items)} usable PNG assets across {pages} review sheet(s)")

for label, archive in PACKS:
    if not archive.exists():
        raise SystemExit(f"Missing CC0 pack: {archive}")
    with ZipFile(archive) as z:
        bad = z.testzip()
        if bad:
            raise SystemExit(f"Corrupt entry in {archive}: {bad}")
    make_sheet(label, archive)
