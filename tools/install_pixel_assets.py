from pathlib import Path
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT / "assets" / "MistIsle_PixelArt_v1.zip"
if not ARCHIVE.exists():
    raise SystemExit(f"Missing asset bundle: {ARCHIVE}")
with ZipFile(ARCHIVE) as z:
    z.extractall(ROOT)
required = [
    ROOT / "assets/characters/png/hero_walk_4dir.png",
    ROOT / "assets/characters/png/lumi_idle_pixel.png",
    ROOT / "assets/characters/png/moatail_idle_pixel.png",
    ROOT / "assets/characters/png/gecko_idle_pixel.png",
    ROOT / "assets/tiles/png/island_iso_tiles.png",
]
missing = [str(p.relative_to(ROOT)) for p in required if not p.exists()]
if missing:
    raise SystemExit("Missing extracted assets: " + ", ".join(missing))
print("Mist Isle pixel assets ready:")
for p in required:
    print(" -", p.relative_to(ROOT))
