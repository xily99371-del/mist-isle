# Mist Isle — 雾屿回响

A cute anime-inspired 2.5D companion adventure built in C with raylib. Windows-first development, Raspberry Pi 5 deployment target.

## Chapter 1: The Lost Lighthouse
Explore a small island, befriend three companions, solve an elemental ruin puzzle, and restore the lighthouse.

## Current status
The repository contains the first chapter gameplay prototype, original pixel-art character sheets, an isometric ground atlas, and a Windows-first CMake build. The current milestone is focused on making the coastal camp and first companion sequence feel like a cohesive pixel-art adventure.

## Build
Requires CMake, Git, and a C compiler. CMake fetches raylib 5.5.

```powershell
cmake -S . -B build
cmake --build build --config Release
.\build\Release\mist_isle.exe
```

Controls: WASD move, Shift dash, E interact, Tab switch companion, 1/2/3 select battle skill, Enter confirm.

## Art direction
Cozy top-down pixel-RPG exploration inspired by classic farming/adventure games, with anime-style character concepts, rounded silhouettes, expressive companions, warm island palette, and soft magical lighting. The in-game explorer uses a 4-direction 4-frame 32×32 walking sheet; companions have 2-frame idle sheets; isometric ground uses a crisp 64×32 diamond tile atlas. See `docs/ART_DIRECTION.md`.


## Art assets
Original vector character portraits are included under `assets/characters/svg/`:
- `hero_explorer.svg`
- `moss_lumi.svg`
- `moatail.svg`
- `crystal_gecko.svg`

Original pixel sprites and environment props are generated from a portable Pillow script. Install Pillow and run `python tools/generate_pixel_assets.py` before launching. Editable SVG concept sheets are also retained in the repository:

See `docs/ART_DIRECTION.md` and `docs/ASSET_MANIFEST.md` for the visual style and asset/license record.


## Pixel-art assets
The generated game assets include:
- `assets/characters/png/hero_walk_4dir.png`
- `assets/characters/png/lumi_idle_pixel.png`
- `assets/characters/png/moatail_idle_pixel.png`
- `assets/characters/png/gecko_idle_pixel.png`
- `assets/tiles/png/island_iso_tiles.png`

Sprite textures use nearest-neighbor filtering to preserve pixel edges. Missing PNGs fall back to primitive placeholder rendering.


## CI build artifact
Every successful push to `main` builds the Windows executable and publishes a GitHub Actions artifact named `MistIsle-Windows-Prototype`, including the generated PNG assets. Download it from the repository's Actions run page.


## Coastal camp milestone
The current coastal-camp scene includes a generated pixel-art campsite prop atlas and a friendly keeper sprite:
- Tent, supply crate, signpost, lighthouse, campfire, mailbox, flower patch and shore rock.
- The keeper NPC is visible at the camp and can be spoken to with **E**.
- Campfire flicker and glow are animated in the renderer.
- All pixel textures use nearest-neighbor filtering.

Run `RUN_WINDOWS.bat` to regenerate all PNG assets before building. The GitHub Actions workflow packages the executable together with its runtime asset folders.


## New in the grove milestone
- A field journal (`J`) tracks wildlife discoveries, companion collection and chapter objectives.
- A drifting firefly can be discovered and registered.
- Feed the hungry Leafling one island fruit to reveal a hidden star crystal.
- Grove environment now includes mushroom clusters, a fallen log, rune stone and flower patch.
- Save manually with `F5`; load with `F9`. The game also autosaves after key progression events.
- The save file is `mist_isle_save.dat` in the game working directory.

See `docs/GROVE_EXPLORATION.md` for the milestone details.


## Expanded RPG systems
- `J`: field journal, wildlife discoveries and chapter objectives.
- `I`: inventory, resources, health and companion levels.
- `M`: island route map and progression unlocks.
- `F5`: save; `F9`: load. Key progression events autosave.
- Combat skill synergy: Moatail applies Soaked, then Crystal Gecko deals bonus electric damage.
- Companion XP: active companion gains XP after victory and levels up at 50 XP.

See `docs/CHAPTER1_RPG_MILESTONE.md`.


## Chapter content expansion
The grove now contains a discoverable firefly, a fruit-loving Leafling that reveals a hidden crystal, and a one-time wild mote battle with a fruit reward. The field journal tracks these encounters. The inventory and route map are available with I and M. Companion combat now has healing, Soaked status, electric bonus damage and XP-based levels.

See `docs/CHAPTER1_CONTENT_EXPANSION.md` for the current milestone and remaining work.


## Chapter 1 polish
- Player four-direction walk animation and animated companion idle sprites.
- Isometric depth sorting for characters and environmental props.
- Dynamic day/night tint and weather ambience (rain and firefly nights).
- Companion levels scale healing and elemental skill damage.
- Combat synergy: Soaked from Moatail increases Crystal Gecko electric damage.
- Active companion level/XP shown in the HUD.
- Restored lighthouse finale includes companion dialogue and animated light motes.

See `docs/CHAPTER1_POLISH_MILESTONE.md` for the implementation boundary and remaining polish.


## Windows playtest candidate
The current milestone is feature-frozen for the first full Windows playthrough. Please use the packaged Windows artifact from GitHub Actions once the latest build succeeds. The playtest checklist is in `docs/WINDOWS_PLAYTEST_CANDIDATE.md`.

After this package is delivered, feature work pauses. The next development cycle should focus on defects and usability issues discovered in the Windows playthrough, not another round of speculative features.


## Approved art direction and runtime sprites
The approved art direction combines cute, expressive monster-companion characters with a warm, cozy pixel-RPG environment mood. These are original generated project assets, not extracted Pokémon or Stardew Valley sprites.

The runtime sprite archive is stored at `assets/approved_art/MistIsle_RuntimeSprites_v1.zip`. `RUN_WINDOWS.bat` and Windows CI extract it before building. The renderer loads the new hero four-direction walk sheet and the Moss Lumi, Moatail and Crystal Gecko idle strips.

See `docs/APPROVED_ART_ASSETS.md` for sheet layouts, source archive details and remaining environment integration work.


## Approved art integration v2
Extract `MistIsle_ArtIntegration_v2.zip` into the repository root (merge the `assets/` directory). The renderer uses 32x32 source cells for the approved player/companion sheets, 96x96 cells for the environment prop atlas, and a six-variant 64x32 isometric ground atlas. Environment source bands are retained for future tile/prop selection.


## Art integration v2.1 — Windows playtest preparation
The updated art archive is available in ChatGPT Library as `MistIsle_ArtIntegration_v2_1.zip`. Extract it into the repository root and merge `assets/`. This release includes the hand-projected grass, flower-grass, sand, stone and water isometric tiles. The asset generator now preserves an imported ground atlas instead of overwriting it with procedural placeholders.

The renderer source frame size is 32x32 (the `_64` filename refers to the intended display scale, not the source-cell dimensions). The environment prop atlas uses 96x96 cells.


## Hub + sub-map world
The chapter now uses a hub-and-three-sub-map structure: Coastal Camp, Glowglade Forest, Tidal Ruins and Lighthouse Courtyard. Enter each region from the hub gates with E; use the marked west exit in a sub-map to return. The Tidal Ruins gate unlocks after recruiting Moss Lumi and Moatail; the Lighthouse Courtyard opens after solving the shrine.

Save format v4 stores the active region and resource pickups separately for all four maps. See `docs/HUB_SUBMAPS.md` for the map flow, progression and remaining art gaps.


## Supplemental area art
A new 16-prop original generated atlas is available in ChatGPT Library as `MistIsle_AreaProps_v1.zip`. Extract it into the repository root and merge `assets/`. It supplies area-specific coastal props (dock, boat, tent, buoy), grove props (flowering tree, glow mushrooms, mossy log, luminous flowers), ruin props (arch, pillar, broken wall, rune fountain), and lighthouse props (stairs, lighthouse tower, gate, garden fountain). The renderer loads the optional atlas from `assets/environment/area_props_96.png`.


## One-time art import into GitHub
The v3 art pack is intended to be committed into the repository once, so future clones and ZIP downloads contain all runtime assets. For an existing local copy, use a real Git clone (a downloaded GitHub ZIP has no `.git` metadata):

```powershell
git clone https://github.com/xily99371-del/mist-isle.git
cd mist-isle
```

Extract `MistIsle_ArtPack_v3_HubSubmaps.zip` into the cloned repository root, merge `assets/`, then run `UPLOAD_ART_TO_GITHUB.bat`. It stages only `assets/`, commits them, and pushes to the configured `origin`. After that one-time push, future source downloads will include the art pack and no separate art ZIP is needed.


## One-time v3 art import and full Windows playtest build

The complete generated art pack is maintained as a local ZIP in ChatGPT Library. To commit the binary PNG files into this repository once:

1. Clone the repository (do not use the GitHub Download ZIP, which has no `.git` folder):
   ```powershell
   cd D:\Games
   git clone https://github.com/xily99371-del/mist-isle.git
   cd mist-isle
   ```
2. Place `MistIsle_ArtPack_v3_HubSubmaps.zip` beside `UPLOAD_ART_TO_GITHUB.bat`.
3. Run `UPLOAD_ART_TO_GITHUB.bat`. It extracts the pack, validates the expected images, commits only `assets/`, and pushes to `main`.
4. The push triggers a Windows build. Download the artifact named `MistIsle-Windows-Playtest-v1` from that run.

After this one-time import, all future source ZIPs and Windows build artifacts will contain the art assets. No separate art archive merge will be required.


## Controls and movement coordinate convention
The game uses an orthographic top-down grid. WASD and arrow keys are screen-cardinal: W/up decreases world Y, S/down increases world Y, A/left decreases world X, D/right increases world X. World tiles are 56x56 pixels. Camera tracking is clamped to the active sub-map bounds.


## Refined top-down terrain art pack v4
The v4 art pack includes the approved v3 environment/character art plus `assets/environment/topdown_terrain_atlas_v1.png`, a native 56px orthographic terrain atlas built from the grass, sand, stone and water variant sheets. It contains 9 grass variants, 9 sand variants, 9 stone variants and 12 water/shore variants. Extract `MistIsle_ArtPack_v4_TopDown_Refined.zip` at the repository root and run `UPLOAD_ART_TO_GITHUB.bat` to commit the assets once.


## Refined top-down art pack v5
The v5 pack adds four runtime atlases: 56px grass/sand/stone/water terrain tiles; a 44-sprite flora and props atlas; a 30-sprite cliffs, shoreline, bridges and fences atlas; and a 10-sprite structures atlas for cottage, tent, shrine, fountain and campfire. The renderer now loads these atlases and uses the detailed square tiles and sprites for the top-down maps. Import once with `MistIsle_ArtPack_v5_TopDown_Refined.zip` and `UPLOAD_ART_TO_GITHUB.bat`.
