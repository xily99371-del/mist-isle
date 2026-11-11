from pathlib import Path
from zipfile import ZipFile
ROOT=Path(__file__).resolve().parents[1]
ARCHIVE=ROOT/"assets"/"MistIsle_BinaryAssets_v2.zip"
with ZipFile(ARCHIVE) as z:
    bad=z.testzip()
    if bad: raise SystemExit(f"Corrupt asset member: {bad}")
    z.extractall(ROOT)
required=[
 "assets/characters/png/hero_walk_4dir.png",
 "assets/characters/png/lumi_idle_pixel.png",
 "assets/characters/png/moatail_idle_pixel.png",
 "assets/characters/png/gecko_idle_pixel.png",
 "assets/tiles/png/island_iso_tiles.png",
 "assets/tiles/png/environment_props.png",
]
missing=[p for p in required if not (ROOT/p).exists()]
if missing: raise SystemExit("Missing assets: "+", ".join(missing))
print("Mist Isle pixel assets installed and verified.")
