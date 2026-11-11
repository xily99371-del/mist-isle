# Art integration v2.1 and scene alignment

## Corrected sprite geometry
The approved PNG files have these actual dimensions:
- Hero: 128x128, arranged as 4 columns x 4 rows of 32x32 frames.
- Companions and wildlife: 128x32, arranged as four 32x32 frames.
- Environment props: 1152x96, twelve 96x96 cells.
- Isometric ground atlas: 384x32, six 64x32 diamond cells.

The renderer now indexes all character/wildlife frames as 32x32 source cells. The `_64` filename is retained for the asset pack naming convention but does not describe the source cell dimensions.

## Scene integration
- Tree variants use the imported environment atlas.
- Trees are rendered from the same world grid cells marked solid by collision, preventing visible trees and collision obstacles from drifting apart.
- Camp sign, crate and lighthouse map to their matching atlas cells.
- Ground tiles map to grass, flower grass, sand, stone and water. The ruins use stone; the forest pocket uses grass; the tidal pool uses water.
- Render queue expanded to 128 items to accommodate collision-aligned trees, props, pickups and actors.

## Generator behavior
The procedural ground atlas is generated only if no imported `assets/tiles/png/island_iso_tiles.png` exists. This allows the approved imported art to survive `RUN_WINDOWS.bat` asset generation.

## Validation
`tools/validate_project.py` checks imported asset dimensions when the optional art pack is present and validates renderer frame indexing, atlas cell sizes, render queue capacity and core gameplay symbols. Windows CI runs this check before CMake.
