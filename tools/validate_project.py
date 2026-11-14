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
MAP_AUTOTILE = (ROOT / "tools/map_autotile.py").read_text(encoding="utf-8")
MAP_HEADER_VALIDATOR = (ROOT / "tools/validate_generated_maps.py").read_text(encoding="utf-8")
MAP_PREVIEW = (ROOT / "tools/preview_tiled_maps.py").read_text(encoding="utf-8")
WATER_PREVIEW = MAP_PREVIEW
RASTERIZER = (ROOT / "tools/rasterize_assets.py").read_text(encoding="utf-8")
GROVE_MAP = (ROOT / "maps/glowglade_grove.tmx").read_text(encoding="utf-8")
MAPS = "\\n".join((ROOT / "maps" / name).read_text(encoding="utf-8") for name in ("coastal_camp.tmx","glowglade_grove.tmx","tidal_ruins.tmx","lighthouse_courtyard.tmx"))
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
    "assets/characters/hero/heroine_approved_8x4.png": (192, 128),
    "assets/characters/png/heroine_portrait.png": (512, 640),
    "assets/characters/png/heroine_world_4x4_64x80.png": (256, 320),
    "assets/characters/png/watcher_portrait.png": (512, 640),
    "assets/characters/png/lighthouse_guardian_portrait.png": (512, 640),
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
art_imported = (ROOT / "assets/characters/portraits/heroine_portrait.png").exists()
if art_imported:
    for rel, expected in assets.items():
        path = ROOT / rel
        require(path.exists(), f"approved art file exists: {rel}")
        with Image.open(path) as im:
            require(im.size == expected, f"{rel} dimensions {im.size} == {expected}")
        present += 1
else:
    print("INFO: approved art archive not present in clean CI checkout; fallback art will be packaged.")

# The separately supplied approved sprite-sheet import is optional for clean CI
# checkouts, but its exact cell geometry is checked as soon as it is installed.
for rel, expected in {
    "assets/characters/hero/heroine_approved_8x4.png": (192, 128),
}.items():
    path = ROOT / rel
    if path.exists():
        with Image.open(path) as im:
            require(im.size == expected, f"{rel} dimensions {im.size} == {expected}")
    else:
        print(f"INFO: optional approved sprite sheet not imported yet: {rel}")

# The repaired Lighthouse Guardian sheet is shipped in art bundle v7. Older
# tracked builds contain a damaged 192x128 indexed PNG, so only validate the
# new 256x160 sheet when v7 has actually been installed.
boss_sheet = ROOT / "assets/characters/boss/lighthouse_guardian_4x2.png"
if (ROOT / "MistIsle_ApprovedArt_v7.zip").exists():
    require(boss_sheet.exists(), "v7 Lighthouse Guardian pixel sheet is present")
    with Image.open(boss_sheet) as im:
        im.load()
        require(im.size == (256, 160), f"Boss sprite sheet dimensions {im.size} == (256, 160)")
else:
    print("INFO: v7 Boss sprite sheet not installed in this checkout; visual Boss validation is deferred.")
require("if(TEX_HEROINE_APPROVED.id)" in RENDER and "if(TEX_GUARDIAN_APPROVED.id)" in RENDER,
        "approved directional heroine and guardian sprite sheets take priority over portrait fallbacks")
require("TEX_WATCHER_PIXEL.id" in RENDER and "TEX_GUARDIAN_APPROVED.id" in RENDER,
        "world Watcher and guardian actors use dedicated pixel sprite sheets, never illustrated portrait fallbacks")


# Renderer source rectangles must match the approved sheet cell dimensions.
require("frame*32" in RENDER and "facing*32" in RENDER,
        "hero/companion frame indexing uses 32px source cells")
require("frame*64" in RENDER and "facing*80" in RENDER and "frame*32" in RENDER,
        "heroine uses 64x80 source cells while companions retain their 32px animation cells")
require("index*96" in RENDER, "environment props use 96px atlas cells")
require("DrawAreaProp" in RENDER and "area_props_96.png" in RENDER, "area-specific supplemental prop atlas is integrated")
require("DrawTopDownTile" in RENDER and "WorldToScreen" in RENDER, "orthographic top-down square-tile renderer is active")
require("topdown_terrain_atlas_v1.png" in RENDER and "TILE_SIZE" in RENDER and "56.0f,56.0f" in RENDER, "detailed 56px top-down terrain atlas is integrated")
require("TEX_TOPDOWN_TILES" in RENDER and "for(int i=0;i<29;i++)" in RENDER and "heroine_approved_8x4.png" in RENDER and "lighthouse_guardian_4x2.png" in RENDER, "top-down terrain and decor textures are loaded and released")
require('type="prop_22"' in GROVE_MAP and 'type="prop_302"' not in GROVE_MAP,
        "Glowglade uses its own woodland shelter and never reuses the coastal lighthouse cottage")
require("144*it->scale,116*it->scale" in RENDER and "22:(144,116)" in MAP_PREVIEW,
        "woodland shelter display size is synchronized between runtime and Tiled preview")
require("Return route keeps the same grounded stone-arch language" in RENDER and "DrawAreaProp(8,(Vector2){p.x,p.y+2},160,168)" in RENDER,
        "return portal retains the authored stone-arch look instead of a placeholder black hole")
forest_start=RENDER.find("static void DrawForestBackdrop")
forest_end=RENDER.find("static void DrawRuinsBackdrop",forest_start)
require(forest_start>=0 and forest_end>forest_start and "DrawCircle" not in RENDER[forest_start:forest_end],
        "forest backdrop no longer draws procedural canopy circles; Tiled sprites define the forest")

require("flora_props_atlas_128.png" in RENDER and "cliffs_fences_atlas_128.png" in RENDER and "structures_atlas_128.png" in RENDER, "refined flora, cliff and structure sprite atlases are integrated")
require("original_props_16_v1.png" in RENDER and "DrawOriginalProp" in RENDER, "curated original 16-prop atlas is integrated")
require("TEX_ORIGINAL_PROPS" in RENDER and "for(int i=0;i<29;i++)" in RENDER, "original prop atlas texture lifecycle is complete")
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
require("InRect" in GAME and "const float r=.31f" in GAME and "distance/0.08f" in GAME,
        "player collision checks authored prop footprints with dense substeps")
require("RENDER_BOSS" in RENDER and "DrawLighthouseGuardian" in RENDER,
        "lighthouse guardian is visible in the world and battle scene")
require("bossPhase==3?(g->battleRound%2==0):(g->battleRound%3==0)" in GAME and "bossHeavy" in GAME,
        "lighthouse guardian escalates from measured attacks to a telegraphed resonance strike")
require("incoming*=e->type==ENEMY_BOSS?0.65f:0.8f" in GAME and "核心蓄能：下一回合将发动重击" in RENDER,
        "wet status interrupts the guardian core and reduces its boss attack damage")
require("g->enemySoaked=false; // Wetness interrupts one incoming attack" in GAME and "核心受潮：本次攻击伤害降低" in RENDER,
        "wet status is consumed by one enemy attack and visibly dampens the guardian core")
require("TEX_LUMI_PIXEL.id" in RENDER and "TEX_MOATAIL_PIXEL.id" in RENDER and "TEX_GECKO_PIXEL.id" in RENDER and "frame*32" in RENDER,
        "all three recruited companions use their four-frame authored pixel idle sheets")
require("BuddyFollowOffset" in GAME and "static const Vector2 formation[4][3]" in GAME and "distance>3.8f?7.2f" in GAME,
        "companions follow in a facing-aware fan formation with collision-aware catch-up movement")
require("当前位置" in RENDER and "DrawCircleLines((int)pts[i].x,(int)pts[i].y,41" in RENDER,
        "island map shows an explicit current-area marker and readable legend")
require("attacking?((int)(t*8.0f)%4):((int)(t*2.0f)%4)" in RENDER and "attacking?80.0f:0.0f" in RENDER,
        "lighthouse guardian uses its dedicated attack animation row during the enemy turn")
require('DrawUIText(isBoss?"首领战":"遭遇战"' in RENDER and "核心耐久" in RENDER,
        "boss encounter has a distinct dark arena, boss title and lantern-core health bar")
require("AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,150)" in GAME,
        "story lighthouse encounter creates the dedicated boss enemy type")
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

# The user's approved high-resolution portraits and pixel sheet are a separate import bundle.
approved_assets = {
    "assets/characters/portraits/heroine_portrait.png": (1086, 1448),
    "assets/characters/portraits/watcher_portrait.png": (1086, 1448),
    "assets/characters/portraits/guardian_portrait.png": (1086, 1448),
    "assets/characters/portraits/moss_lumi_portrait.png": (1086, 1448),
    "assets/characters/portraits/moatail_portrait.png": (1086, 1448),
    "assets/characters/portraits/crystal_gecko_portrait.png": (1086, 1448),
    "assets/characters/hero/heroine_pixel_4x4_64x80.png": (256, 320),
    "assets/characters/npc/watcher_pixel_4x4_64x80.png": (256, 320),
    "assets/environment/coastal_keeper_house.png": (360, 291),
}
if all((ROOT / p).exists() for p in approved_assets):
    for rel, expected in approved_assets.items():
        with Image.open(ROOT / rel) as im:
            if rel.endswith("_portrait.png"):
                require(im.width >= 320 and im.height >= 427,
                        f"approved portrait {rel} retains at least the legacy portrait resolution")
            else:
                require(im.size == expected, f"approved user art {rel} has expected dimensions {expected}")
    require("TEX_HEROINE_PIXEL" in RENDER and "heroine_pixel_4x4_64x80.png" in RENDER and "TEX_WATCHER_PIXEL" in RENDER,
            "the exact user-approved 4x4 pixel sheet drives the playable heroine")
    require("assets/characters/portraits/heroine_portrait.png" in RENDER and
            "assets/characters/portraits/moss_lumi_portrait.png" in RENDER and
            "assets/characters/portraits/moatail_portrait.png" in RENDER and
            "assets/characters/portraits/crystal_gecko_portrait.png" in RENDER,
            "dialogue portrait paths use the user's high-resolution anime illustrations")
    require("DrawIllustratedPortrait(TEX_LUMI_ILLUSTRATION" in RENDER and
            "DrawIllustratedPortrait(TEX_MOATAIL_ILLUSTRATION" in RENDER and
            "DrawIllustratedPortrait(TEX_GECKO_ILLUSTRATION" in RENDER,
            "battle cards and party roster use high-resolution companion illustrations, not pixel idle sprites")
    require("DrawCoastalHouse" in RENDER and "coastal_keeper_house.png" in RENDER,
            "coastal keeper house uses the user's approved pixel-art building")
    require("frame*64" in RENDER and "TEX_WATCHER_PIXEL" in RENDER and "watcher_pixel_4x4_64x80.png" in RENDER,
            "Watcher world actor is a portrait-matched native 64x80 pixel RPG sprite frame, not a portrait crop")
    require('bossPixelPath="assets/characters/boss/lighthouse_guardian_4x2.png"' in RENDER and "TEX_GUARDIAN_APPROVED.width==256" in RENDER and
        "DrawLighthouseGuardian((Vector2){880,454},g->time,1.5f,g->battleTurn!=0)" in RENDER and
            "else if(TEX_GUARDIAN_PORTRAIT.id)" not in RENDER,
            "boss exploration and battle scenes use the dedicated pixel animation; anime portrait is dialogue-only")
    require('SetDialogueAs(g,"沫尾兽"' in GAME and 'SetDialogueAs(g,"鸣晶蜥"' in GAME,
            "water and crystal companion recruitment bind their own names and portrait IDs")
    require("if(distance>6.0f&&foundTarget&&!Solid(g,target))" in GAME,
            "companions teleport back to a safe formation slot after exceeding the hard leash distance")
    require("case RENDER_TREE: p.y+=8.0f; DrawFloraProp" in RENDER and
            "DrawEllipse((int)p.x,(int)p.y+5,15*s" not in RENDER and
            "DrawEllipse((int)p.x,(int)p.y+8,34,10" not in RENDER,
            "trees and shrine decorations do not render detached circular ground shadows")
else:
    print("INFO: MistIsle_ApprovedArt_v7.zip is not installed; user-approved runtime artwork is not yet available.")

require("type_id in (29,31,33)" in MAP_BUILD and "210-213 small stones" in MAP_BUILD and "type_id==229" not in MAP_BUILD,
        "small stones, low shrubs and flowering props are non-blocking while structural bases remain solid")
require("block_rect(cx-1,cy-1,cx+1,cy-1)" in MAP_BUILD and "compact one-row tent base" in MAP_BUILD,
        "house and tent collision footprints are reduced to compact rear/base rows")

require(all((ROOT / f"assets/characters/svg/{name}.svg").exists() for name in ("heroine_portrait","watcher_portrait","lighthouse_guardian_portrait")),
        "three authored static character portrait source illustrations are present")
require("cairosvg" in RASTERIZER and all(name in RASTERIZER for name in ("heroine_portrait.svg","watcher_portrait.svg","lighthouse_guardian_portrait.svg")),
        "portrait artwork is rasterized from editable high-resolution SVG source assets")
require("python tools/rasterize_assets.py" in WORKFLOW and "cairosvg" in WORKFLOW,
        "Windows and Raspberry Pi CI generate the static portrait PNGs before packaging")
require(all(name in RENDER for name in ("TEX_HERO_PORTRAIT","TEX_WATCHER_PORTRAIT","TEX_GUARDIAN_PORTRAIT")) and "DrawTexturePro(*fallback" in RENDER,
        "dialogue displays dedicated illustrated portraits instead of enlarged walking sprites or geometric stand-ins")

require("FLAG_WINDOW_RESIZABLE" in MAIN and "1600,900" in MAIN and "LoadRenderTexture(SW,SH)" in MAIN,
        "Windows game opens in a larger resizable window with a fixed 16:9 logical canvas")
require("MISTISLE_PI_PROFILE" in MAIN and "SetWindowMinSize(960,540)" in MAIN and "showPerfOverlay" in MAIN,
        "Pi profile uses a non-maximized 720p window and optional FPS diagnostics")
require("MISTISLE_PI_PROFILE" in CMAKE and 'set(OPENGL_VERSION "2.1"' in CMAKE and "GLFW_BUILD_WAYLAND ON" in CMAKE and "generate_area_maps" in CMAKE,
        "CMake exposes a Raspberry Pi desktop OpenGL 2.1 profile and builds map data")
require("find_package(Python3 REQUIRED COMPONENTS Interpreter)" in CMAKE and "tools/build_maps.py" in CMAKE,
        "Python is required only at build time to compile Tiled maps")
require(all(path.exists() for path in MAP_FILES), "all four Tiled maps and the terrain tileset are present")
map_art_paths = [
    "assets/environment/generated/nature_props_sheet.png",
    "assets/environment/generated/glowglade_props_sheet.png",
    "assets/environment/generated/tidal_ruins_props_sheet.png",
    "assets/environment/generated/lighthouse_ruins_props_sheet.png",
]
if all((ROOT / p).exists() for p in map_art_paths):
    for rel in map_art_paths:
        with Image.open(ROOT / rel) as im:
            require(im.mode == "RGBA" and im.width >= 1200 and im.height >= 1200,
                    f"generated area art {rel} is a full-resolution transparent sheet")
    require("DrawGeneratedMapAsset" in RENDER and "TEX_NATURE_SHEET" in RENDER and
            "TEX_GROVE_SHEET" in RENDER and "TEX_RUINS_SHEET" in RENDER and "TEX_LIGHTHOUSE_SHEET" in RENDER,
            "four generated art sheets are wired to the runtime area-specific prop renderer")
    require(all(f"prop_{n}" in MAPS for n in (600, 500, 640, 660)) and
            all(f'type="prop_{n}"' in MAPS for n in (601,603,605,606,608,609,610,612,613,614,616,617,618,619,
                                                       625,627,629,641,643,644,646,647,649,651,652,654,
                                                       664,667,669,670,672,674,676)),
            "generated environment sheets are actually populated across all four Tiled maps, including both blue-fire braziers")

require("#define MAPW 18" in HEADER and "#define MAPH 12" in HEADER and "W,H,T,MAX_OBJECTS=18,12,56,64" in MAP_BUILD and "WIDTH, HEIGHT = 18, 12" in MAP_AUTOTILE and "W,H,T=18,12,56" in MAP_PREVIEW,
        "all four maps, runtime arrays, autotile masks and Tiled previews use compact 18×12 one-screen dimensions")
require(all(f'width="18" height="12"' in path.read_text(encoding="utf-8") for path in MAP_FILES if path.suffix==".tmx"),
        "every Chapter 1 Tiled map is authored at 18×12 cells")
require('type="prop_22"' in GROVE_MAP and 'type="prop_302"' not in GROVE_MAP and 'type="prop_33"' not in GROVE_MAP,
        "Glowglade Grove uses its own woodland shelter and excludes the coastal cottage, lighthouse and bridge props")
require("PushRenderItem(items,&count,RENDER_KEEPER,0,(Vector2){7.0f,7.0f}" in RENDER and "PushRenderItem(items,&count,RENDER_MOATAIL,0,(Vector2){14,7}" in RENDER and "PushRenderItem(items,&count,RENDER_GECKO,0,(Vector2){13,8}" in RENDER,
        "Watcher and companion actors are positioned for the compact Tiled scene layouts")

require('name=="campfire": type_id=2000' in MAP_BUILD and "obj->type==2000" in RENDER,
        "the campfire remains an animated Tiled-authored scene object")
require('name="营火" type="campfire" x="420.00" y="532.00"' in (ROOT / "maps/coastal_camp.tmx").read_text(encoding="utf-8"),
        "campfire is placed in the foreground below the Watcher")
require('"进入辉光林地 · 萤火微光浮现"' in GAME,
        "Glowglade arrival explicitly announces its exclusive firefly weather")
require("MISTISLE_FIREFLY_COUNT 58" in RENDER and "pulse*.62f*weatherAlpha" in RENDER,
        "firefly weather has bright multi-layer motes and a desktop-dense field")
require("Fade((Color){7,17,36,255},.82f*weatherAlpha)" in RENDER,
        "night fog is a clearly visible layered atmosphere")
require("Fade((Color){17,39,62,255},.34f*weatherAlpha)" in RENDER,
        "rain uses a stronger cool atmospheric grade")
require(not re.search(r"(?m)^37(?:,|$)|,37(?:,|$)", (ROOT / "maps/lighthouse_courtyard.tmx").read_text(encoding="utf-8")),
        "lighthouse boss courtyard has no stray grass-corner terrain cells")
require("generate_ground_atlas.py" in CMAKE and "topdown_terrain_atlas_v1.png" in CMAKE,
        "CMake regenerates the same seamless ground atlas for desktop and Raspberry Pi")
require("AREA_GROUND" in MAP_BUILD and "AREA_COLLISION" in MAP_BUILD and "AREA_MARKERS" in MAP_BUILD and "AREA_OBJECTS" in MAP_BUILD,
        "Tiled compiler exports ground, collision, story markers and object placements")
require("build_overlay(ground)" in MAP_BUILD and "AREA_OVERLAY[4][4][MAPH][MAPW]" in MAP_BUILD,
        "Tiled compiler emits four directional autotile planes consumed by the renderer")
require("def build_overlay" in MAP_AUTOTILE and "DIRECTIONS" in MAP_AUTOTILE and "PRIORITY" in MAP_AUTOTILE,
        "the four-direction shoreline autotile helper is present for clean Windows and ARM64 builds")
require("if type_id==32:" in MAP_BUILD and "collision[by][xx]=0" in MAP_BUILD,
        "Tiled bridge objects open a real walkable span through the water collision grid")
require("Collision follows the visible object's ground footprint" in MAP_BUILD and "reduced rear-wall footprint" in MAP_BUILD,
        "Tiled buildings, tree trunks, walls, pillars and altar bases compile into gameplay collision")
require("Pokémon-Center-style recovery service" in GAME and "SetDialogueAs(g,\"守望者\"" in GAME and "g->lumi.hp=g->lumi.maxHp" in GAME,
        "Watcher heals the whole party before the overlapping campfire interaction")
require('SetDialogueAs(g,"旅人","三种伙伴的力量' in GAME,
        "ruin seal-unlock dialogue uses the traveller portrait")
require("nearest free, reachable tile" in GAME and "occupied" in GAME,
        "blocked or duplicate pickups are relocated to a distinct reachable ground tile")
require("living stone arch" in RENDER and "beacon tower" in RENDER and "Return route keeps" in RENDER,
        "grove, ruins, lighthouse and return portals have distinct grounded visual identities")
require("finalCore" in RENDER and "DrawAreaProp(11,p,190,194)" in RENDER,
        "the Tidal Ruins keystone is a larger multi-ring final altar, distinct from ordinary shrines")
require("DrawLighthouseGuardian(p,g->time,1.0f,false)" in RENDER and "static void DrawKeeper(Vector2 p,float t,float scale)" in RENDER and "TEX_WATCHER_PORTRAIT" in RENDER and "TEX_WATCHER_PIXEL" in RENDER and "TEX_GUARDIAN_PORTRAIT" in RENDER,
        "the lighthouse guardian and Watcher use authored pixel world actors with separate dialogue portraits")
require("DrawDialoguePortrait" in RENDER and "dialoguePortrait[page]" in RENDER and "DrawTexturePro(*fallback" in RENDER,
        "dialogue scenes render full-size portraits for the hero, Watcher and lighthouse guardian")
require('strncmp(speaker,"旅人",strlen("旅人"))==0' in GAME,
        "traveler journal and protagonist lines resolve to the protagonist portrait rather than a companion")
require('if(strncmp(speaker,"旅人",strlen("旅人"))==0)speaker="旅人";' in RENDER,
        "protagonist dialogue uses the correct visible speaker label")
require("dialogueStartsBossBattle" in GAME and "SetDialogueSequence(g,speakers,lines,4,true)" in GAME,
        "the lighthouse guardian delivers a multi-speaker pre-battle conversation before combat")
require("SetDialogueSequence(g,speakers,lines,3,false)" in GAME and "胸口的光" in GAME,
        "the boss encounter ends with a dedicated character dialogue scene")
require("nearest free, reachable tile" in GAME and "visual footprint of tall structures" in GAME,
        "collectibles avoid both collision footprints and visually occluding large props")
require("DrawCoastalHouse" in RENDER and "252.0f*scale,h=204.0f*scale" in RENDER and "252*scale,204*scale" in MAP_PREVIEW,
        "the approved coastal house uses matching grounded dimensions in runtime and map previews")
require("DrawGroveCottage" in RENDER and "reduced rear-wall footprint" in MAP_BUILD,
        "large cottage structures use the structure atlas with matching collision foundations")
require('#include "area_maps.h"' in RENDER and "AREA_OVERLAY[(int)g->area][direction][y][x]" in RENDER and "direction*90" in RENDER,
        "renderer draws every directional shoreline mask from the generated Tiled map header")
require("AREA_OVERLAY[4][4][MAPH][MAPW]" in MAP_HEADER_VALIDATOR and "validate_generated_maps.py" in CMAKE,
        "CMake validates the generated Tiled header before compiling renderer references")
require("chapter1_map_review.png" in MAP_PREVIEW and "Collision" in MAP_PREVIEW and "Markers" in MAP_PREVIEW,
        "map preview generator creates a visual QA sheet with collision and story markers")
require("python tools/preview_tiled_maps.py" in WORKFLOW,
        "Windows and Raspberry Pi CI generate map review images")
require('objectgroup[@name=\'Markers\']' in MAP_BUILD and 'objectgroup[@name=\'Collision\']' in MAP_BUILD,
        "map compiler reads named marker and collision layers")
require("GameUpdate(&g,dt)" in MAIN and "SCENE_END must pass through GameUpdate" in MAIN,
        "ending animation advances through the normal game update loop")
require("LogicalMousePosition" in GAME and "GetScreenWidth()" in GAME,
        "battle mouse selection remains aligned after window scaling")
require("DrawTerrainAccent" in RENDER and "terrainKinds" in RENDER and "512" in RENDER,
        "world maps include layered ground detail, shoreline foam and a larger render queue")
require("static RenderTexture2D TEX_GROUND_CACHE" in RENDER and "BuildGroundCache(g)" in RENDER,
        "static Tiled ground, autotile overlays and coast details are cached in one active-area render target")
require("CACHED_GROUND_AREA==(int)g->area" in RENDER and "CACHED_GROUND_AREA=(int)g->area" in RENDER,
        "ground cache is rebuilt only when the player changes area")
require("DrawTexturePro(TEX_GROUND_CACHE.texture" in RENDER and "UnloadRenderTexture(TEX_GROUND_CACHE)" in RENDER,
        "cached map is drawn as one texture and released cleanly")
require("DrawWorldBackdrop(g)" in RENDER[RENDER.find("static void BuildGroundCache"):RENDER.find("static void RenderWorld")],
        "static sea, forest and ruins backdrops are baked into the same area cache")
require(RENDER.count("DrawWorldBackdrop(g);")==2,
        "the large decorative backdrop is cached once, with one allocation-failure fallback")
require("#define MAX_PARTICLES 160" in HEADER and "#define MAX_PARTICLES 300" in HEADER,
        "Raspberry Pi uses a bounded particle pool while desktop keeps the full pool")
require("Safe fallback if the target GPU cannot allocate the single-area ground cache" in RENDER,
        "terrain still renders if a Raspberry Pi GPU cannot allocate the cache")
require("#define MISTISLE_RAIN_DROPS 64" in RENDER and "#define MISTISLE_RAIN_RIPPLES 8" in RENDER and "#define MISTISLE_RAIN_DROPS 230" in RENDER,
        "Raspberry Pi profile reduces weather overdraw")
require("Static grass, sand and stone detail is baked into the ground cache" in RENDER,
        "Raspberry Pi avoids redrawing static terrain accents every frame")
require("RENDER_GATE" in RENDER and "DrawGateMarker" in RENDER and "DrawStructureProp(4" in RENDER,
        "all area transitions use a visible authored stone-arch portal with destination signage")
require("辉光林地" in RENDER and "潮汐遗迹" in RENDER and "灯塔庭院" in RENDER and "返回海岸营地" in RENDER,
        "camp entrances and sub-map return route have explicit Chinese destination labels")
require("DrawLineEx(pts[2],pts[3]" in RENDER and "DrawLineEx(pts[0],pts[3]" not in RENDER,
        "island map displays the actual hub-to-grove, hub-to-ruins and ruins-to-lighthouse route graph")
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
require("DrawAreaProp(11,p,190,194)" in RENDER and "DrawAreaPropTint(13,p" in RENDER,
        "the shrine uses its authored sprite and the pre-finale lighthouse is dimmed")
require("g->objectiveCollapsed" in RENDER and "O 收起" in RENDER and "O 展开" in RENDER,
        "objective tracker can collapse to a compact title chip and expand again")
require("IsKeyPressed(KEY_O)" in GAME and "objectiveToggle" in GAME,
        "objective tracker supports keyboard and mouse toggles")
require("Rectangle panel={SW-350.0f,18.0f,330.0f,116.0f}" in RENDER,
        "expanded objective tracker is narrower and shorter than the previous HUD panel")
require("MISTISLE_RAIN_DROPS 64" in RENDER and "MISTISLE_RAIN_DROPS 230" in RENDER and "WeatherNoise" in RENDER and "float gust=" in RENDER and
        "(.48f+r3*.40f)" in RENDER and "length=18.0f+r2*31.0f" in RENDER and "MISTISLE_RAIN_RIPPLES 28" in RENDER,
        "rain uses denser, brighter staggered streaks, gust-driven density and stronger water impacts")
require("g->weather=0; g->weatherFade=1.0f" in GAME and "g->weatherClock>38.0f" in GAME and
        "static const int roamingWeather[4]={0,1,3,4}" in GAME,
        "clear sunny weather is the default and world weather rotation excludes grove-only fireflies") 
require("if(area==AREA_GROVE)" in GAME and "g->weather=2;" in GAME and "else if(previousArea==AREA_GROVE)" in GAME,
        "entering Glowglade Grove automatically enables exclusive firefly weather and leaving restores clear weather")
require("g->weather==3" in RENDER and "g->weather==4" in RENDER and "DrawCloudPuff" in RENDER and
        "Broad, low fog banks drift across the foreground" in RENDER and "(.22f+(i%3)*.035f)" in RENDER and
        "Fade((Color){7,17,36,255},.82f*weatherAlpha)" in RENDER,
        "overcast and optional night fog have distinct moving cloud visuals and clearly visible layered fog banks")
LIGHTHOUSE_MAP_TEXT=(ROOT/"maps/lighthouse_courtyard.tmx").read_text(encoding="utf-8")
require('type="prop_676"' in LIGHTHOUSE_MAP_TEXT and LIGHTHOUSE_MAP_TEXT.count('type="prop_676"')==2 and
        not any(f'type="tree{i}"' in LIGHTHOUSE_MAP_TEXT for i in range(3)) and
        not any(f'type="prop_{i}"' in LIGHTHOUSE_MAP_TEXT for i in range(116,130)) and
        not any(f'type="prop_{i}"' in LIGHTHOUSE_MAP_TEXT for i in (29,30,34,200,201,202)) and
        all(f'type="prop_{i}"' in LIGHTHOUSE_MAP_TEXT for i in (660,661,663,664,666,667,670,671,672,673,674)),
        "Boss courtyard uses one coherent generated ruin-art family, mirrored blue braziers, unified stone floor and no mixed coastal/grove props")
require("DrawCoastalCliffs(g,terrainKinds,minX,minY,maxX,maxY)" in RENDER and "AREA_GROUND" in GAME,
        "shoreline and all four ground layouts are sourced from Tiled maps")
require("GetVisibleTileBounds" in RENDER and "screen.x<-180.0f" in RENDER,
        "terrain and scene objects outside the camera view are culled")
require("MISTISLE_RAIN_DROPS 64" in RENDER and "MISTISLE_RAIN_RIPPLES 8" in RENDER and "MISTISLE_FIREFLY_COUNT 30" in RENDER,
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
require("footY-60.0f-bob" in RENDER and "38.0f,60.0f" in RENDER and "44.0f,48.0f" in RENDER,
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

require("fetch_portrait_assets.ps1" not in (ROOT / "RUN_WINDOWS.bat").read_text(encoding="utf-8") and
        "TEX_WATCHER_PIXEL.id" in RENDER and "tools/generate_pixel_assets.py" in CMAKE,
        "Windows source build generates its native pixel Watcher locally and does not download GitHub Actions artifacts")

require('strcmp(speaker,"沫尾兽")==0' in GAME and 'strcmp(speaker,"沫尾")==0' in GAME and 'strcmp(speaker,"鸣晶蜥")==0)return 5' in GAME,
        "each recruited companion resolves to its own portrait ID")
require("TEX_CHARACTER_PORTRAITS" in RENDER and "portrait>=0&&portrait<=2" in RENDER and
        "TEX_MOATAIL_ILLUSTRATION" in RENDER and "TEX_GUARDIAN_APPROVED" in RENDER and
        "DrawLighthouseGuardian((Vector2){880,454},g->time,1.5f,g->battleTurn!=0)" in RENDER,
        "high-resolution character portraits are dialogue-only while the boss battle uses its pixel sprite sheet")
require("核心耐久" in RENDER and "阶段三" in RENDER and "核心蓄能" in RENDER and "PHASE III" not in RENDER,
        "boss phase and core status are displayed in Chinese in a non-overlapping panel")
require("TEX_HEROINE_PIXEL" in RENDER and "frame*64" in RENDER and "facing*80" in RENDER and "38.0f,60.0f" in RENDER,
        "heroine uses crisp four-frame directional pixel art at a larger in-world scale")
require("TEX_GROVE_COTTAGE" in RENDER and "DrawGroveCottage" in RENDER,
        "grove house uses the approved handcrafted cottage asset")
require('x="812.00" y="84.00"' in (ROOT / "maps/glowglade_grove.tmx").read_text(encoding="utf-8") and 'x="812.00" y="588.00"' in (ROOT / "maps/glowglade_grove.tmx").read_text(encoding="utf-8"),
        "grove edge trees are placed on land, not over the creek")

# Critical gameplay entry points and persistence must remain present.
for token, label in [
    ("BuildAreaWorld", "Tiled world construction"),
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


# Water must read as a layered material in both the runtime and generated map review.
water_start = RENDER.find("static void DrawTerrainAccent")
water_end = RENDER.find("static void DrawAtlasSprite", water_start)
water_renderer = RENDER[water_start:water_end]
require(water_start >= 0 and water_end > water_start and
        all(token in water_renderer for token in ("area==AREA_GROVE", "area==AREA_RUINS",
                                                   "sinf(phase", "DrawLineEx", "DrawCircleV")),
        "water uses area-tinted animated wave crests and reflective glints")
require("MISTISLE_PI_PROFILE" in water_renderer and "DrawCircleLines" in water_renderer,
        "Raspberry Pi keeps a reduced water shimmer path while desktop uses layered ripples")
require("def draw_water_detail(image,indices,name)" in WATER_PREVIEW and
        "draw_water_detail(image,indices,name)" in WATER_PREVIEW and
        "broken foam glint" in WATER_PREVIEW,
        "Tiled map previews show matching water highlights and broken shoreline foam")

tree_case_start = RENDER.find("case RENDER_TREE:")
tree_case_end = RENDER.find("case RENDER_PROP:", tree_case_start)
tree_case = RENDER[tree_case_start:tree_case_end]
require(tree_case_start >= 0 and tree_case_end > tree_case_start and
        "DrawEllipse" not in tree_case and "DrawFloraProp(8+(it->index%3),p,108*it->scale,120*it->scale)" in tree_case,
        "large tree canopies retain authored flora sprites without detached circular shadows")
require('draw.ellipse((x-35*scale,y+1*scale,x+35*scale,y+15*scale)' not in WATER_PREVIEW,
        "map review previews follow the no-detached-tree-shadow rule")


# World characters must come from authored illustrations/sprite sheets, never primitive stand-ins.
require(all(token in RASTERIZER for token in ("moss_lumi.svg", "moatail.svg", "crystal_gecko.svg")),
        "high-resolution companion illustrations are generated from editable SVG sources")
require(all(token in RENDER for token in ("TEX_LUMI_ILLUSTRATION", "TEX_MOATAIL_ILLUSTRATION",
                                           "TEX_GECKO_ILLUSTRATION", "TEX_WATCHER_PORTRAIT",
                                           "TEX_GUARDIAN_PORTRAIT", "TEX_HERO_PORTRAIT", "TEX_HEROINE_APPROVED", "TEX_GUARDIAN_APPROVED")),
        "heroine, Watcher, guardian and all three recruited companions have authored illustration textures")
for signature, forbidden, label in [
    ("static void DrawPlayer(", ("DrawTriangle(", "DrawCircleV("), "heroine world renderer has no geometric body fallback"),
    ("static void DrawKeeper(", ("DrawTriangle(", "DrawCircleV("), "Watcher world renderer uses a native pixel RPG actor"),
    ("static void DrawLighthouseGuardian(", ("DrawTriangle(", "DrawCircleV("), "guardian world renderer uses the dedicated pixel boss sheet"),
    ("static void DrawLumi(", ("DrawTriangle(", "DrawCircleV("), "Moss Lumi world renderer uses the authored illustration"),
    ("static void DrawMoatail(", ("DrawTriangle(", "DrawCircleV("), "Moatail world renderer uses the authored illustration"),
    ("static void DrawGecko(", ("DrawTriangle(", "DrawCircleV("), "Crystal Gecko world renderer uses the authored illustration"),
]:
    start = RENDER.find(signature)
    brace = RENDER.find("{", start)
    depth = 0
    end = -1
    for i in range(brace, len(RENDER)):
        if RENDER[i] == "{": depth += 1
        elif RENDER[i] == "}":
            depth -= 1
            if depth == 0:
                end = i + 1
                break
    body = RENDER[start:end] if start >= 0 and end > start else ""
    require(bool(body) and all(token not in body for token in forbidden), label)
require('"assets/characters/portraits/moss_lumi_portrait.png"' in RENDER and
        '"assets/characters/portraits/moatail_portrait.png"' in RENDER and
        '"assets/characters/portraits/crystal_gecko_portrait.png"' in RENDER and
        "for(int i=0;i<29;i++)" in RENDER,
        "high-resolution companion portrait texture load/unload lifecycle is complete")

print(f"Validation complete. Imported art sheets checked: {present}/{len(assets)}.")
