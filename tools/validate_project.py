#!/usr/bin/env python3
"""Static and asset-layout checks for the Mist Isle Windows playtest candidate."""
from pathlib import Path
import re
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RENDER = (ROOT / "src/render.c").read_text(encoding="utf-8")
GAME = (ROOT / "src/game.c").read_text(encoding="utf-8")
HEADER = (ROOT / "src/game.h").read_text(encoding="utf-8")

def require(condition, message):
    if not condition:
        raise SystemExit("VALIDATION FAILED: " + message)
    print("PASS:", message)

# The imported approved art is optional in CI, but if present its real frame geometry is enforced.
assets = {
    "assets/characters/hero/hero_walk_4dir_64.png": (128, 128),
    "assets/characters/companions/moss_lumi_idle_64.png": (128, 32),
    "assets/characters/companions/moatail_idle_64.png": (128, 32),
    "assets/characters/companions/crystal_gecko_idle_64.png": (128, 32),
    "assets/characters/companions/leafling_idle_64.png": (128, 32),
    "assets/characters/companions/grove_mote_idle_64.png": (128, 32),
    "assets/environment/environment_props_96.png": (1152, 96),
    "assets/tiles/png/island_iso_tiles.png": (384, 32),
}
present = 0
for rel, expected in assets.items():
    path = ROOT / rel
    if path.exists():
        with Image.open(path) as im:
            require(im.size == expected, f"{rel} dimensions {im.size} == {expected}")
        present += 1
require(present == 0 or present == len(assets),
        "approved art import is complete (all expected sheets present, or none in clean CI)")

# Renderer source rectangles must match the approved sheet cell dimensions.
require("frame*32" in RENDER and "facing*32" in RENDER,
        "hero/companion frame indexing uses 32px source cells")
require("frame*64" not in RENDER and "facing*64" not in RENDER,
        "no stale 64px source-frame indexing remains")
require("index*96" in RENDER, "environment props use 96px atlas cells")
require("tile*64" in RENDER, "isometric ground atlas uses 64x32 diamond cells")
require("RenderItem items[128]" in RENDER, "render queue has capacity for trees, props and pickups")
require("world.kind[y][x]==1" in RENDER and "RENDER_TREE" in RENDER,
        "visible forest trees are placed from the collision grid")
require("RenderInit(void)" in RENDER and "RenderClose(void)" in RENDER,
        "renderer owns texture load and release lifecycle")

# Critical gameplay entry points and persistence must remain present.
for token, label in [
    ("BuildWorld", "world construction"),
    ("GameSave", "save system"),
    ("GameLoad", "load system"),
    ("SCENE_BATTLE", "battle state"),
    ("SCENE_END", "chapter ending state"),
    ("groveMoteDefeated", "persistent grove encounter"),
    ("fireflyDiscovered", "field journal discovery"),
    ("leaflingFed", "feeding interaction"),
]:
    require(token in GAME or token in HEADER, label + " is present")

print(f"Validation complete. Imported art sheets checked: {present}/{len(assets)}.")
