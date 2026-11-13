# Chapter 1 map authoring

The four runtime maps are authored in Tiled and compiled into a small C header during the CMake build. The .tmx files in maps/ are the source of truth; the generated C header is never edited by hand.

## Files
- maps/coastal_camp.tmx
- maps/glowglade_grove.tmx
- maps/tidal_ruins.tmx
- maps/lighthouse_courtyard.tmx
- maps/terrain.tsx

All maps are orthogonal, 22 × 18 cells, with 56 × 56 pixel tiles. The terrain tileset references assets/environment/topdown_terrain_atlas_v1.png.

## Layers
- Ground: CSV tile layer. Use complete grass, sand, stone and water tiles only. The compiler rejects transparent edge-mask tiles used as full ground.
- Collision: rectangle objects. These are rasterized into the runtime collision grid. Water tiles are solid. The current Chapter 1 composition deliberately avoids bridge-dependent routes.
- Objects: point objects. Use tree0, tree1, tree2, prop_20–prop_35, prop_100–prop_199, prop_200–prop_299, prop_300–prop_399, prop_400–prop_415, prop_500–prop_503, or campfire. Add a float property named scale when required.
- Markers: point objects. Each map requires spawn, return, grove_gate, ruin_gate, shrine and boss.

Object and marker points are placed at the centre of their tile. Moving a marker in Tiled updates the corresponding gameplay position without editing C. The map compiler checks that all markers are unblocked and reachable from spawn.

## Composition rules

- Build the main route and all story clearings before placing decoration.
- Keep the main route visually continuous; avoid scattered one-tile patches of sand.
- Use grass as the dominant ground in the camp and grove, stone as the dominant ground in ruins and courtyard.
- Sand is a route or a deliberate coastal/erosion band, not a random accent tile.
- Water is reserved for shoreline, isolated pools and decorative ponds. Do not place water across a required route.
- Do not use prop_32 bridge objects in the current map pass. The previous bridge atlas item was visually weak and did not match its collision footprint. A bridge may return only with a redesigned asset, explicit walkable deck geometry and automated collision regression tests.
- Props should frame the path, define clearings and establish area identity. Keep pickups, NPCs and story markers visually unobstructed.
- Grove identity comes from a winding forest trail, dense perimeter tree groups and luminous mushroom/flower clusters.
- Ruins identity comes from a broad stone processional, a few weathered sand edges, columns, arches, broken walls and a rune landmark.
- Coastal Camp identity comes from its beach, keeper's cottage compound, dock/camp structures and a legible three-way hub route.
- Glowglade Grove uses the distinct woodland shelter prop_22 (area-prop atlas tent), not the coastal cottage compound prop_302. Frame it with tree canopies, lanterns, luminous flowers and mushroom clusters; keep its entrance and nearby pickups clear.
- Lighthouse Courtyard identity comes from its central stone plaza, garden beds and lighthouse landmark.

## Terrain transitions

The terrain atlas includes transparent masks for grass, sand, stone and water. tools/map_autotile.py computes four independent edge planes (north, east, south, west) for each map cell. The renderer composites these masks into the static area cache. Keep material boundaries intentional and avoid alternating materials tile-by-tile.

## Tiled usage

You do not need to open Tiled for routine map changes: the .tmx files are XML and can be edited directly. Tiled is retained for visual review and optional manual placement.

To inspect a map, open Tiled and choose File → Open File, then select one of the .tmx files. The terrain.tsx tileset loads through its relative path. Keep the map size, tile size, layer names, marker names and recognized object type names unchanged unless the compiler is updated too.

## Preview and build

From the repository root:

```bash
python tools/generate_ground_atlas.py
python tools/preview_tiled_maps.py --output build/map-previews
python tools/validate_project.py
python tools/build_maps.py --output build/area_maps.h
python tools/validate_generated_maps.py --header build/area_maps.h
```

CMake invokes tools/build_maps.py and tools/validate_generated_maps.py, then compiles the generated area_maps.h. Python and Tiled are authoring/build tools only; the Windows and Raspberry Pi runtimes do not need either.
