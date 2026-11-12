# Asset sources and licenses

This repository separates original project artwork from third-party assets.

## Original project artwork
The following files under `assets/environment/` and `assets/characters/` were created specifically for Mist Isle for this project:
- `topdown_terrain_atlas_v1.png`
- `flora_props_atlas_128.png`
- `cliffs_fences_atlas_128.png`
- `structures_atlas_128.png`
- `area_props_96.png`
- `environment_props_96.png`
- `ground_grass_variants.png`, `ground_sand_variants.png`, `ground_stone_variants.png`, `water_and_shore.png`, `cliffs_and_fences.png`, `flora_and_props.png`, `camp_ruins_lighthouse.png`
- The hero and companion sprite sheets under `assets/characters/`

These are original project assets, not extracted from Stardew Valley.

## Third-party assets included in the repository
Only assets with a clearly documented license that permits redistribution in the game's source repository may be vendored here.

| Pack | Intended use | License / conditions | Source |
|---|---|---|---|
| Kenney Tiny Town | supplementary 3/4-view buildings, fences and town props; use selectively with orthographic ground | CC0; attribution not required; original ZIP and license preserved in third_party/kenney/tiny-town/ | https://kenney.nl/assets/tiny-town |
| Kenney Pixel Pack | candidate source for supplementary props, particles and UI; not yet bundled | CC0; attribution not required | https://kenney.nl/assets/pixel-pack |
| Kenney RPG Base | candidate source for orthographic RPG ground/props; not yet bundled | CC0; attribution not required | https://kenney.nl/assets/rpg-base |
| Kenney Pixel UI Pack | candidate source for pixel UI panels/icons; not yet bundled | CC0; attribution not required | https://kenney.nl/assets/pixel-ui-pack |
| OpenGameArt — Forest Tiles | supplementary forest foliage/ground variants | CC0; retain the source page and author metadata | https://opengameart.org/content/forest-tiles |
| OpenGameArt — Pixel Art Forest Tilesets | supplementary forest terrain | CC0 | https://opengameart.org/content/pixel-art-forest-tilesets |

Kenney Tiny Town is already vendored at `third_party/kenney/tiny-town/kenney_tiny_town.zip` with its license and source note. A CI-generated contact sheet is produced for review. The pack uses a 3/4 view, so its building sprites should be used selectively as foreground structures; do not mix its terrain tiles into the orthographic ground layer. Before importing any additional third-party pack, retain its original license/readme and record the exact files used. Do not copy sprites, tiles, music, character designs or extracted game files from Stardew Valley itself; its assets are copyrighted. The visual direction may be inspired by its readable top-down composition, warm palette, tile transitions, dense prop placement and cozy environmental storytelling.

## Distribution rule
Do not add assets to the repository until their license explicitly permits redistribution. A pack that permits use inside a finished game but prohibits redistribution of the source files must not be committed as a raw asset pack.


## Current integration status (2026-11-12)
- Kenney Tiny Town is already committed in `third_party/kenney/tiny-town/`; its 4 curated building/fence sprites are generated into `assets/environment/kenney_buildings_atlas.png` at build time and used by the renderer.
- The original v6 art pack (hero/companions, terrain, flora, cliffs, structures and 16 original props) is still held in ChatGPT Library and is not yet present in the GitHub tree. Use `IMPORT_ART_V6_TO_GITHUB.bat` for the one-time import.
- Kenney RPG Base and Pixel UI Pack are license-cleared candidates only; they are not yet downloaded or integrated. Add them only after visual review and a documented import.


## Supplemental CC0 packs prepared for import (2026-11-12)

| Pack | Source | License | Planned use |
|---|---|---|---|
| Kenney RPG Base | https://opengameart.org/content/rpg-pack-base-set | CC0 | Orthogonal ground, path, building and prop candidates |
| Kenney Pixel UI Pack | https://opengameart.org/content/pixel-ui-pack-750-assets | CC0 | Inventory, buttons, panels, cursors and bars |

Original downloads are stored under `third_party/kenney/rpg-base/` and `third_party/kenney/pixel-ui/` after running `IMPORT_ART_V6_TO_GITHUB.bat`. The import script downloads the archives from the linked OpenGameArt file URLs, validates their ZIP integrity, generates review contact sheets, and commits the original archives plus the v6 original art to GitHub. These two packs are candidates for selective in-game use; they are not automatically drawn into the runtime until their sprites have been visually selected and mapped.

## User-provided art transfer
The v6 original art archive is provided directly in the ChatGPT conversation. Download that attachment to the Windows PC and select it in the import script's file picker. No ChatGPT Library access is required.
