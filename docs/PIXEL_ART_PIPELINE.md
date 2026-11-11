# Pixel art pipeline

## Movement sprites
- `hero_walk_4dir.svg`: 128×128, 4 columns × 4 rows, each cell 32×32. Rows: down, left, right, up.
- `lumi_idle_pixel.svg`, `moatail_idle_pixel.svg`, `gecko_idle_pixel.svg`: 64×32, two 32×32 idle frames per companion.
- Nearest-neighbor filtering is required. Do not bilinear-smooth pixel sprites.

## Isometric ground tiles
- `assets/tiles/island_iso_tiles.svg`: six 64×32 diamond tiles: three grass variants, sandy path, water, ruin stone.
- Render tile sprites with nearest-neighbor sampling and depth-sort entities by their ground Y coordinate.

## Rasterization
```powershell
python -m pip install cairosvg
python tools/rasterize_assets.py
```
The rasterizer outputs PNGs to `assets/characters/png/` and `assets/tiles/png/`.

The visual direction is an original cozy pixel adventure inspired by broad top-down farming/adventure RPG conventions; no Stardew Valley character, sprite, tile, or audio assets are copied.
