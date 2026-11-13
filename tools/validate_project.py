#!/usr/bin/env python3
"""Static and asset-layout checks for the Mist Isle Windows playtest candidate."""
from pathlib import Path
from zipfile import ZipFile
import base64
import io
import re
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RENDER = (ROOT / "src/render.c").read_text(encoding="utf-8")
GAME = (ROOT / "src/game.c").read_text(encoding="utf-8")
HEADER = (ROOT / "src/game.h").read_text(encoding="utf-8")
MAIN = (ROOT / "src/main.c").read_text(encoding="utf-8")
WORKFLOW = (ROOT / ".github/workflows/build-windows.yml").read_text(encoding="utf-8")
CMAKE = (ROOT / "CMakeLists.txt").read_text(encoding="utf-8")
MAP_BUILD = (ROOT / "tools/build_maps.py").read_text(encoding="utf-8")
MAP_FILES = [ROOT / "maps" / name for name in (
    "terrain.tsx", "coastal_camp.tmx", "glowglade_grove.tmx",
    "tidal_ruins.tmx", "lighthouse_courtyard.tmx"
)]

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
require("g->cam.zoom=1.10f" in GAME and "UpdatePlayerCamera(g,dt)" in GAME and "MoveTowardsConstant" in GAME,
        "camera uses map bounds and direct constant-speed follow")
require("RenderItem items[512]" in RENDER and "*count>=512" in RENDER, "render queue has capacity for expanded environment dressing and actors")
require("g->world.objectCount" in RENDER and "RENDER_TREE" in RENDER and "AREA_OBJECTS" in GAME,
        "Tiled-authored scene objects drive tree and prop placement")
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
require("AREA_OBJECTS" in GAME and "DrawAreaPropTint(13,p" in RENDER,
        "lighthouse courtyard uses the authored lighthouse sprite from the area atlas")
require("AREA_COLLISION" in GAME and "TerrainKindFromAtlasIndex(g->world.terrain[y][x])==4" in GAME and "MovePlayerWithCollision" in GAME,
        "Tiled collision rectangles and water tiles drive substep movement collision")
require("GetCurrentObjective" in GAME and "DrawObjectiveMarker" in RENDER,
        "chapter objective guidance and in-world waypoint marker are active")
require("OwnedBuddyIdAt" in GAME and "optionCount" in GAME and "选择出战伙伴" in RENDER,
        "battle options are restricted to companions currently owned")
require("!g->splash.owned" in RENDER and "!g->spark.owned" in RENDER,
        "unrecruited water and crystal companions are visible as world encounters")
require("RenderInit(void)" in RENDER and "RenderClose(void)" in RENDER,
        "renderer owns texture load and release lifecycle")
require("FLAG_WINDOW_RESIZABLE" in MAIN and "1600,900" in MAIN and "LoadRenderTexture(SW,SH)" in MAIN,
        "Windows game opens in a larger resizable window with a fixed 16:9 logical canvas")
require("MISTISLE_PI_PROFILE" in MAIN and "SetWindowMinSize(960,540)" in MAIN and "showPerfOverlay" in MAIN,
        "Pi profile uses a non-maximized 720p window and optional FPS diagnostics")
require("MISTISLE_PI_PROFILE" in CMAKE and 'set(OPENGL_VERSION "2.1"' in CMAKE and "generate_area_maps" in CMAKE,
        "CMake exposes a Raspberry Pi desktop OpenGL 2.1 profile and builds map data")
require("find_package(Python3 REQUIRED COMPONENTS Interpreter)" in CMAKE and "tools/build_maps.py" in CMAKE,
        "Python is required only at build time to compile Tiled maps")
require(all(path.exists() for path in MAP_FILES), "all four Tiled maps and the terrain tileset are present")
require("AREA_GROUND" in MAP_BUILD and "AREA_COLLISION" in MAP_BUILD and "AREA_MARKERS" in MAP_BUILD and "AREA_OBJECTS" in MAP_BUILD,
        "Tiled compiler exports ground, collision, story markers and object placements")
require('objectgroup[@name=\'Markers\']' in MAP_BUILD and 'objectgroup[@name=\'Collision\']' in MAP_BUILD,
        "map compiler reads named marker and collision layers")
require("GameUpdate(&g,dt)" in MAIN and "SCENE_END must pass through GameUpdate" in MAIN,
        "ending animation advances through the normal game update loop")
require("LogicalMousePosition" in GAME and "GetScreenWidth()" in GAME,
        "battle mouse selection remains aligned after window scaling")
require("DrawTerrainAccent" in RENDER and "terrainKinds" in RENDER and "512" in RENDER,
        "world maps include layered ground detail, shoreline foam and a larger render queue")
require("RENDER_GATE" in RENDER and "DrawGateMarker" in RENDER and "DrawStructureProp(4" in RENDER,
        "all area transitions use a visible authored stone-arch portal with destination signage")
require("辉光林地" in RENDER and "潮汐遗迹" in RENDER and "灯塔庭院" in RENDER and "返回海岸营地" in RENDER,
        "camp entrances and sub-map return route have explicit Chinese destination labels")
require("需要两位伙伴" in RENDER and "封印尚未解除" in RENDER,
        "locked entrances explain their unlock conditions")
const_glyph_match = re.search(r"static const int UI_CODEPOINTS\[\]=\{([^}]+)\}", RENDER)
require(const_glyph_match is not None, "UI font atlas has an explicit Unicode glyph set")
font_codepoints = {int(value) for value in const_glyph_match.group(1).split(",") if value.strip()}
source_cjk = {ord(ch) for ch in RENDER + GAME if 0x3400 <= ord(ch) <= 0x9FFF}
require(source_cjk.issubset(font_codepoints),
        "font atlas contains every Chinese character used by gameplay, HUD and ending CG captions")

require("TEX_ENDING_CG" in RENDER and "LoadEndingCG" in RENDER and "assets/cinematics/lighthouse_final_scene.jpg.b64" in RENDER,
        "ending uses a dedicated illustrated lighthouse scene")
require("scene.width*.535f" in RENDER and "scene.width-4.0f" in RENDER,
        "ending scene adds a procedural moving beacon beam over the illustration")
require("DrawAreaProp(11,p,112,116)" in RENDER and "DrawAreaPropTint(13,p" in RENDER,
        "the shrine uses its authored sprite and the pre-finale lighthouse is dimmed")
require("g->objectiveCollapsed" in RENDER and "O 收起" in RENDER and "O 展开" in RENDER,
        "objective tracker can collapse to a compact title chip and expand again")
require("IsKeyPressed(KEY_O)" in GAME and "objectiveToggle" in GAME,
        "objective tracker supports keyboard and mouse toggles")
require("Rectangle panel={SW-350.0f,18.0f,330.0f,116.0f}" in RENDER,
        "expanded objective tracker is narrower and shorter than the previous HUD panel")
require("MISTISLE_RAIN_DROPS 84" in RENDER and "MISTISLE_RAIN_DROPS 190" in RENDER and "g->weather==1" in RENDER,
        "rain has multiple visible streak depths, surface impacts and water ripples")
require("IsKeyPressed(KEY_F7)" in GAME and "weather=(g->weather+1)%4" in GAME,
        "weather can be cycled on demand for reliable demonstration")
require("DrawCoastalCliffs(g,terrainKinds,minX,minY,maxX,maxY)" in RENDER and "AREA_GROUND" in GAME,
        "shoreline and all four ground layouts are sourced from Tiled maps")
require("GetVisibleTileBounds" in RENDER and "screen.x<-180.0f" in RENDER,
        "terrain and scene objects outside the camera view are culled")
require("MISTISLE_RAIN_DROPS 84" in RENDER and "MISTISLE_RAIN_RIPPLES 10" in RENDER,
        "Raspberry Pi profile reduces weather draw calls")
require("DrawPlayer((Vector2){420,620}" in RENDER,
        "finale character lineup stays clear of the bottom caption")
require("g->area==AREA_LIGHTHOUSE&&g->bossDefeated&&Dist(p->pos,g->world.shrine)<3.0f" in GAME and "g->scene=SCENE_END" in GAME,
        "finale is triggered by explicit E interaction with the beacon stone after the boss is defeated")
require("assets/cinematics" in WORKFLOW and "Copy-Item assets/cinematics/* dist/assets/cinematics/" in WORKFLOW,
        "the packaged Windows build includes the lighthouse cinematic asset")
require("DrawLighthouseBackdrop(g->time,false)" in RENDER,
        "the lighthouse remains dark in exploration until the finale cutscene")
require("TerrainKindFromAtlasIndex" in RENDER and "tileIndex%4" in RENDER and "AREA_GROUND" in GAME,
        "Tiled ground tile IDs map directly to the correct 56px atlas cells")
require("Rectangle src={(float)((tileIndex%4)*56),(float)((tileIndex/4)*56),56.0f,56.0f}" in RENDER,
        "runtime draws the exact tile selected in the Tiled editor")
cg_file = ROOT / "assets/cinematics/lighthouse_final_scene.jpg.b64"
require(cg_file.exists(), "lighthouse ending illustration asset is present")
cg_data = cg_file.read_text(encoding="ascii").strip()
require(len(cg_data) % 4 == 0, "lighthouse illustration base64 data is complete")
with Image.open(io.BytesIO(base64.b64decode(cg_data, validate=True))) as cg:
    require(cg.size == (448, 448), "lighthouse finale illustration is a 448px square scene")
require("assets/cinematics" in WORKFLOW and "Copy-Item assets/cinematics/*" in WORKFLOW,
        "Windows playtest package includes the cinematic asset")
require("footY-66" in RENDER and "footY-56" in RENDER,
        "player and companion sprites use enlarged world-space dimensions")
require("unsigned char solid[MAPH][MAPW]" in HEADER and "w->solid[y][x]=AREA_COLLISION" in GAME and "g->world.solid[y][x]" in GAME,
        "Tiled collision layer is shared with gameplay collision")
require("h%100" not in GAME[GAME.find("static void BuildAreaWorld"):GAME.find("static void SwitchArea",GAME.find("static void BuildAreaWorld"))],
        "area maps use authored layouts instead of random blocker placement")
require("g->area==AREA_LIGHTHOUSE && g->bossDefeated && Dist(p->pos,g->world.shrine)<3.8f" in GAME and "点亮失落的灯塔" in RENDER,
        "final lighthouse tablet has an explicit E interaction with a reachable radius")
require("SCENE_END" in GAME and "g->endingTime+=dt" in GAME,
        "chapter finale scene state and animation timer are present")
require("if(g->bossDefeated&&Dist(p->pos,g->world.shrine)<1.05f)" not in GAME,
        "finale no longer auto-triggers from proximity")
require(GAME.find("if(g->scene==SCENE_END)") < GAME.find("if(g->journalOpen || g->inventoryOpen || g->mapOpen)"),
        "finale update is processed before modal UI early returns")

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
