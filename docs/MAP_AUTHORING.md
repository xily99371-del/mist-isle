# Chapter 1 map authoring

The four runtime maps are authored in Tiled and compiled into a small C header during the CMake build.

## Files
- maps/coastal_camp.tmx
- maps/glowglade_grove.tmx
- maps/tidal_ruins.tmx
- maps/lighthouse_courtyard.tmx
- maps/terrain.tsx

All maps are orthogonal, 22 x 18 cells, with 56 x 56 pixel tiles. The terrain tileset references assets/environment/topdown_terrain_atlas_v1.png.

## Layers
- Ground: CSV tile layer. Use complete grass, sand, stone and water tiles only. The compiler rejects transparent edge-mask tiles used as full ground.
- Collision: rectangle objects. These are rasterized into the runtime collision grid. Water tiles are also automatically solid.
- Objects: point objects. Use tree0, tree1, tree2, or prop_116 etc. Add a float property named scale when required.
- Markers: point objects. Each map requires spawn, return, grove_gate, ruin_gate, shrine and boss.

Object and marker points are placed at the centre of their tile. Moving a marker in Tiled updates the corresponding gameplay position without editing C.

## Build
CMake invokes tools/build_maps.py and generates area_maps.h under the build directory. The generated header is compiled into the game. Python and Tiled are authoring/build tools only; the Raspberry Pi runtime does not need either.

Open any TMX file in Tiled to edit it. Keep map dimensions, tile size, layer names, marker names and object type names unchanged unless the compiler is updated too.
