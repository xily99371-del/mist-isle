# Chapter 1 map authoring

The four runtime maps are authored in Tiled and compiled into a small C header during the CMake build. The `.tmx` files in `maps/` are the source of truth; the generated C header is never edited by hand.

## Files
- `maps/coastal_camp.tmx`
- `maps/glowglade_grove.tmx`
- `maps/tidal_ruins.tmx`
- `maps/lighthouse_courtyard.tmx`
- `maps/terrain.tsx`

All maps are orthogonal, 22 × 18 cells, with 56 × 56 pixel tiles. The terrain tileset references `assets/environment/topdown_terrain_atlas_v1.png`.

## Layers
- `Ground`: CSV tile layer. Use complete grass, sand, stone and water tiles only. The compiler rejects transparent edge-mask tiles used as full ground.
- `Collision`: rectangle objects. These are rasterized into the runtime collision grid. Water tiles are automatically solid except where a `prop_32` bridge spans the channel.
- `Objects`: point objects. Use `tree0`, `tree1`, `tree2`, `prop_20`–`prop_35`, `prop_100`–`prop_199`, `prop_200`–`prop_299`, `prop_300`–`prop_399`, `prop_400`–`prop_415`, `prop_500`–`prop_503`, or `campfire`. Add a float property named `scale` when required.
- `Markers`: point objects. Each map requires `spawn`, `return`, `grove_gate`, `ruin_gate`, `shrine` and `boss`.

Object and marker points are placed at the centre of their tile. Moving a marker in Tiled updates the corresponding gameplay position without editing C. The map compiler checks that all markers are unblocked and reachable from `spawn`.

## Terrain transitions

The terrain atlas includes transparent masks for grass, sand, stone and water. `tools/map_autotile.py` computes four independent edge planes (north, east, south, west) for each map cell. The renderer composites these masks into the static area cache, so curved creek banks and beach transitions are not limited to one straight edge per tile.

## Tiled usage

You do not need to open Tiled for routine map changes: the `.tmx` files are XML and can be edited directly. Tiled is retained for visual review and optional manual placement.

To inspect a map, open Tiled and choose `File → Open File`, then select one of the `.tmx` files. The `terrain.tsx` tileset loads through its relative path. Keep the map size, tile size, layer names, marker names and recognized object type names unchanged unless the compiler is updated too.

## Preview and build

From the repository root:

```bash
python tools/generate_ground_atlas.py
python tools/preview_tiled_maps.py --output build/map-previews
python tools/validate_project.py
```

CMake invokes `tools/build_maps.py` and `tools/validate_generated_maps.py`, then compiles the generated `area_maps.h`. Python and Tiled are authoring/build tools only; the Windows and Raspberry Pi runtimes do not need either.