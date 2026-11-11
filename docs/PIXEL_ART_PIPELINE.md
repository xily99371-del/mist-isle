# Pixel movement and companion animation

The island exploration camera now uses the cozy top-down pixel-RPG movement style: 32×32 characters, nearest-neighbor texture sampling, four facing directions and four walk frames per direction.

## Sprite sheets
- `hero_walk_4dir.svg`: 128×128, 4 columns × 4 rows, each cell 32×32. Rows: down, left, right, up.
- `lumi_idle_pixel.svg`, `moatail_idle_pixel.svg`, `gecko_idle_pixel.svg`: 64×32, two 32×32 idle frames per companion.
- Existing painted portrait SVGs remain available for dialogue/profile presentation.

## Build assets
```powershell
python -m pip install cairosvg
python tools/rasterize_assets.py
```
The generated PNGs are loaded from `assets/characters/png/`. Keep the renderer's nearest-neighbor filtering for pixel sprites.

The visual direction is an original cozy pixel adventure inspired by the broad conventions of top-down farming/adventure RPGs; no Stardew Valley character, sprite, tile or audio assets are copied.
