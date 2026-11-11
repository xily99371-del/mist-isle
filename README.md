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
