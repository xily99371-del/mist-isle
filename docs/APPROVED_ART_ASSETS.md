# Approved generated art import

The user selected the generated art direction consisting of:
- Pokémon-like original creature character appeal (cute, expressive, collectible designs; not Pokémon assets).
- Stardew-like cozy pixel environment mood (warm natural palette, handcrafted props and tiles; not Stardew Valley assets).

## Runtime assets
The approved runtime sprite archive is saved in ChatGPT Library at `/Game Projects/Mist Isle/MistIsle_RuntimeSprites_v1.zip`. Download and extract it into the repository root once; it creates the runtime folders listed below. The GitHub CI remains able to compile without the optional art folders, but a CI package built without them will use fallback sprites.

After extraction:
- `assets/characters/hero/hero_walk_4dir_64.png`: 4x4 sprite sheet, 32x32 cells; rows front/left/right/back, columns animation frames.
- `assets/characters/companions/moss_lumi_idle_64.png`
- `assets/characters/companions/moatail_idle_64.png`
- `assets/characters/companions/crystal_gecko_idle_64.png`

Each companion sheet is a four-frame horizontal strip with 32x32 cells. Leafling and Grove Mote sprite strips are included for upcoming wildlife rendering.

The renderer loads the approved hero and three companion sprite sheets directly. Texture filtering is point sampling to preserve crisp pixel edges.

## Art source archive
The Library archive `MistIsle_GeneratedArt_Import_v1.zip` preserves:
- Original generated character sprite sheets.
- Original generated environment tileset and HUD reference board.
- Cropped environment art bands for grass, sand, stone, water, cliffs, fences, flora, props, camp, ruins and lighthouse.
- Processed 64px-cell source sprites and import notes.

The environment board is a generated illustration atlas, not yet a fully separated autotile set. It is retained as source art; do not claim every visual crop is already integrated into the live map. Further environment integration requires manual tile extraction and collision/occlusion mapping.

## Attribution and IP
These are original generated assets created for this project using ChatGPT image generation. They are not extracted from Pokémon or Stardew Valley. Those games are style references only.


## Approved heroine and lighthouse guardian revision

The user approved the 8-column × 4-direction heroine concept and the four-view / four-action lighthouse guardian concept generated on 2026-11-14. Runtime import targets:
- `assets/characters/hero/heroine_approved_8x4.png` — 8 frames × 4 directions, 24×32 source cells, front/left/right/back rows; boots share a fixed baseline.
- `assets/characters/boss/lighthouse_guardian_4x2.png` — 4-view idle row and 4-action row, 48×64 source cells.

The approved heroine has brown hair, a blue ribbon, teal scarf, cream adventurer coat, leather belt and brown boots. The guardian is a large ancient lighthouse sentinel in dark weathered armor, with a warm lantern core, heavy chains and a torn crimson mantle. It must never be presented as a police officer or ordinary uniformed NPC.


## Boss encounter presentation

The lighthouse guardian's idle view is kept front-facing in exploration. Its second sprite-sheet row is reserved for the four-frame attack sequence and is played only during the enemy turn. The battle scene switches to a darker slate-and-ember palette, uses a dedicated `BOSS` heading and displays the guardian's lantern core as its health bar. This keeps the guardian visually and mechanically distinct from the ordinary ruin sentry and from the camp watcher.


## User-confirmed runtime art set — 2026-11-14

The latest user-provided references supersede earlier generated stand-ins for these roles:
- `heroine_portrait.jpg`: high-resolution anime illustration of the female traveller; dialogue portrait only.
- `watcher_portrait.jpg`: high-resolution male Watcher illustration; dialogue portrait only.
- `guardian_portrait.jpg`: high-resolution lighthouse guardian illustration; dialogue portrait only.
- `moss_lumi_portrait.jpg`, `moatail_portrait.jpg`, `crystal_gecko_portrait.jpg`: high-resolution companion illustrations; dialogue/character-card portraits only. Never enlarge the small pixel idle sheets into portrait art.
- `heroine_pixel_4x4_64x80.png`: the user's approved 4×4 directional pixel sprite sheet; rows are front, left, right, back, with four frames per row. This is the only preferred exploration sprite for the heroine.
- `coastal_keeper_house.png`: the user's approved pixel-art coastal keeper house cutout.
- `watcher_pixel_idle_64x80.png`: a compact pixel-world actor derived from the approved Watcher illustration; the high-resolution original remains the dialogue portrait.

The bundle is distributed as `MistIsle_ApprovedArt_v3.zip`. Place it in the repository root; `RUN_WINDOWS.bat` and CMake extract it to the exact runtime paths. The Windows package must include `assets/characters/portraits/`, `assets/characters/hero/`, and `assets/environment/`.

The Watcher and guardian anime illustrations are not world-scale sprites. The Watcher remains a compact pixel NPC in exploration; the high-resolution illustration is used in dialogue. The guardian uses its dedicated pixel boss animation in exploration/battle and the anime illustration in dialogue/large character art panels.
