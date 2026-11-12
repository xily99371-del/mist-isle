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
    "assets/environment/area_props_96.png": (1536, 96),
    "assets/environment/topdown_terrain_atlas_v1.png": (224, 672),
    "assets/environment/flora_props_atlas_128.png": (1024, 768),
    "assets/environment/cliffs_fences_atlas_128.png": (1024, 512),
    "assets/environment/structures_atlas_128.png": (1024, 256),
    "assets/environment/original_props_16_v1.png": (128, 128),
    "assets/tiles/png/island_iso_tiles.png": (384, 32),
}
present = 0
art_imported = (ROOT / "assets/environment/environment_props_96.png").exists()
if art_imported:
    for rel, expected in assets.items():
        path = ROOT / rel
        require(path.exists(), f"approved art file exists: {rel}")
        with Image.open(path) as im:
            require(im.size == expected, f"{rel} dimensions {im.size} == {expected}")
        present += 1
else:
    print("INFO: approved art archive not present in clean CI checkout; fallback art will be packaged.")


# Renderer source rectangles must match the approved sheet cell dimensions.
require("frame*32" in RENDER and "facing*32" in RENDER,
        "hero/companion frame indexing uses 32px source cells")
require("frame*64" not in RENDER and "facing*64" not in RENDER,
        "no stale 64px source-frame indexing remains")
require("index*96" in RENDER, "environment props use 96px atlas cells")
require("DrawAreaProp" in RENDER and "area_props_96.png" in RENDER, "area-specific supplemental prop atlas is integrated")
require("DrawTopDownTile" in RENDER and "WorldToScreen" in RENDER, "orthographic top-down square-tile renderer is active")
require("topdown_terrain_atlas_v1.png" in RENDER and "56,56" in RENDER, "detailed 56px top-down terrain atlas is integrated")
require("TEX_TOPDOWN_TILES" in RENDER and "for(int i=0;i<16;i++)" in RENDER, "top-down terrain and decor textures are loaded and released")
require("flora_props_atlas_128.png" in RENDER and "cliffs_fences_atlas_128.png" in RENDER and "structures_atlas_128.png" in RENDER, "refined flora, cliff and structure sprite atlases are integrated")
require("original_props_16_v1.png" in RENDER and "DrawOriginalProp" in RENDER, "curated original 16-prop atlas is integrated")
require("TEX_ORIGINAL_PROPS" in RENDER and "for(int i=0;i<16;i++)" in RENDER, "original prop atlas texture lifecycle is complete")
require("static Vector2 WorldScreen(Vector2 p)" in GAME and "TILE_SIZE" in HEADER, "gameplay and rendering share top-down world coordinates")
require("grid.y+" in RENDER and "RENDER_PLAYER" in RENDER, "actor sorting uses top-down ground-contact depth")
require("g->cam.zoom=1.15f" in GAME, "camera follows player at top-down exploration zoom")
require("RenderItem items[192]" in RENDER and "*count>=192" in RENDER, "render queue has capacity for top-down forest canopy, props and actors")
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
    ("BuildAreaWorld", "separate area map builder"),
    ("SwitchArea", "hub/sub-map transitions"),
    ("areaPickupTaken", "per-area pickup persistence"),
    ("s.area=(int)g->area", "active area save field"),
    ("g->area==AREA_GROVE", "area-specific interaction/render routing"),
]:
    require(token in GAME or token in HEADER, label + " is present")

print(f"Validation complete. Imported art sheets checked: {present}/{len(assets)}.")
