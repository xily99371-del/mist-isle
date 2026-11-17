# Sprite Import Rules

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Multiple for atlas sheets; Single for standalone sprites.
- Pixels Per Unit: 16.
- Filter Mode: Point (no filter).
- Compression: None.
- Mip Maps: Off.
- Mesh Type: Tight for irregular props; Full Rect for terrain tiles.
- Pivot: set per sprite using `AssetManifest.json`; large props should use a bottom-center ground pivot.
- Sorting Layers: Ground, Terrain, Buildings, Props, Characters, FrontDecor, VFX, UI.

## Current atlas notes
`02_Tilesets/*/TS_*_8x8_16px.png` are 128×128 PNGs containing 8×8 cells of 16×16 pixels.
`03_Objects/Common/Objects_Environment_Atlas.png` is an extracted transparent object atlas. Sprite rectangles in the manifest are approximate and must be visually checked in Sprite Editor before production use.
