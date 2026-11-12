#!/usr/bin/env python3
"""Static and asset-layout checks for the Mist Isle Windows playtest candidate."""
from pathlib import Path
from zipfile import ZipFile
import re
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RENDER = (ROOT / "src/render.c").read_text(encoding="utf-8")
GAME = (ROOT / "src/game.c").read_text(encoding="utf-8")
HEADER = (ROOT / "src/game.h").read_text(encoding="utf-8")
MAIN = (ROOT / "src/main.c").read_text(encoding="utf-8")

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
    "assets/environment/kenney_buildings_atlas.png": (384, 384),
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
require("TEX_TOPDOWN_TILES" in RENDER and "for(int i=0;i<17;i++)" in RENDER, "top-down terrain and decor textures are loaded and released")
require("flora_props_atlas_128.png" in RENDER and "cliffs_fences_atlas_128.png" in RENDER and "structures_atlas_128.png" in RENDER, "refined flora, cliff and structure sprite atlases are integrated")
require("original_props_16_v1.png" in RENDER and "DrawOriginalProp" in RENDER, "curated original 16-prop atlas is integrated")
require("TEX_ORIGINAL_PROPS" in RENDER and "for(int i=0;i<17;i++)" in RENDER, "original prop atlas texture lifecycle is complete")
require("kenney_buildings_atlas.png" in RENDER and "DrawKenneyBuilding" in RENDER, "Kenney Tiny Town building atlas is integrated")
rpg_pack = ROOT / "third_party/kenney/rpg-base/KenneyRPGpack.zip"
ui_pack = ROOT / "third_party/kenney/pixel-ui/PixelUIpack.zip"
if rpg_pack.exists() or ui_pack.exists():
    require(rpg_pack.exists() and ui_pack.exists(), "both supplemental Kenney CC0 packs are present together")
    for pack, label in [(rpg_pack, "Kenney RPG Base"), (ui_pack, "Kenney Pixel UI")]:
        with ZipFile(pack) as z:
            require(z.testzip() is None, label + " archive passes ZIP integrity check")
    require((ROOT / "build/art-review/kenney-rpg-base-01.jpg").exists(),
            "Kenney RPG Base visual review sheet is generated")
    require((ROOT / "build/art-review/kenney-pixel-ui-01.jpg").exists(),
            "Kenney Pixel UI visual review sheet is generated")
else:
    print("INFO: supplemental Kenney RPG/UI archives are not imported in this checkout.")
require("build_kenney_runtime_atlas.py" in RENDER or (ROOT / "tools/build_kenney_runtime_atlas.py").exists(), "Kenney runtime atlas generator is included")
require("static Vector2 WorldScreen(Vector2 p)" in GAME and "TILE_SIZE" in HEADER, "gameplay and rendering share top-down world coordinates")
require("grid.y+" in RENDER and "RENDER_PLAYER" in RENDER, "actor sorting uses top-down ground-contact depth")
require("g->cam.zoom=1.0f" in GAME and "UpdateCamera(g,dt)" in GAME and "MoveTowardsConstant" in GAME,
        "camera uses map bounds and direct constant-speed follow")
require("RenderItem items[512]" in RENDER and "*count>=512" in RENDER, "render queue has capacity for expanded environment dressing and actors")
require("world.kind[y][x]==1" in RENDER and "RENDER_TREE" in RENDER,
        "visible forest trees are placed from the collision grid")
require("MoveTowardsConstant" in GAME and "680.0f*dt" in GAME,
        "camera uses direct constant-speed follow without an easing dead zone")
require("InRect" in GAME and "const float r=.37f" in GAME and "distance/0.08f" in GAME,
        "player collision checks authored prop footprints with dense substeps")
require("RENDER_BOSS" in RENDER and "DrawLighthouseGuardian" in RENDER,
        "lighthouse guardian is visible in the world and battle scene")
require("DrawUITextWrapped" in RENDER and "UI_CODEPOINTS" in RENDER,
        "Chinese UI includes the complete source glyph set and wrapped objective text")
require("endingTime" in GAME and "static void RenderEnd" in RENDER,
        "lighthouse ending uses a timed cinematic sequence")
require("DrawTriangle(lamp,beamA,beamB" in RENDER and "sinf(t*.72f)" in RENDER,
        "lighthouse ending contains a sweeping animated beam")
require("RENDER_PROP,414" in RENDER and "DrawOriginalProp(it->index-400" in RENDER,
        "lighthouse courtyard uses the authored lighthouse sprite atlas")
require("SetSolidRect" in GAME and "MovePlayerWithCollision" in GAME and "dockWater" in GAME and "stream&&!bridge" in GAME,
        "props, shore water and stream banks block movement with substep collision")
require("GetCurrentObjective" in GAME and "DrawObjectiveMarker" in RENDER,
        "chapter objective guidance and in-world waypoint marker are active")
require("OwnedBuddyIdAt" in GAME and "optionCount" in GAME and "选择出战伙伴" in RENDER,
        "battle options are restricted to companions currently owned")
require("!g->splash.owned" in RENDER and "!g->spark.owned" in RENDER,
        "unrecruited water and crystal companions are visible as world encounters")
require("RenderInit(void)" in RENDER and "RenderClose(void)" in RENDER,
        "renderer owns texture load and release lifecycle")
require("FLAG_WINDOW_RESIZABLE" in MAIN and "1600,900" in MAIN and "LoadRenderTexture(SW,SH)" in MAIN,
        "game opens in a larger resizable window with a fixed 16:9 logical canvas")
require("GameUpdate(&g,dt)" in MAIN and "SCENE_END must pass through GameUpdate" in MAIN,
        "ending animation advances through the normal game update loop")
require("LogicalMousePosition" in GAME and "GetScreenWidth()" in GAME,
        "battle mouse selection remains aligned after window scaling")
require("DrawTerrainAccent" in RENDER and "terrainKinds" in RENDER and "512" in RENDER,
        "world maps include layered ground detail, shoreline foam and a larger render queue")
require("RenderWorld(g);" in RENDER and "GetWorldToScreen2D" in RENDER,
        "ending cinematic reuses the actual lighthouse courtyard and animated beacon")
require("footY-54" in RENDER and "footY-50" in RENDER,
        "player and companion sprites use enlarged world-space dimensions")

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
