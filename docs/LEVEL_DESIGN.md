# Chapter 1 — authored level layout

The four Tiled scenes are designed as a connected chapter route, not decorative backdrops. The playable footprint remains 18 × 12 cells at 56 px per cell so the camera, save format, movement and Raspberry Pi profile remain unchanged.

## Spatial progression

| Area | Primary route | Side spaces | Landmark logic |
|---|---|---|---|
| Coastal Camp | Southern spawn, through the campfire clearing, then branch east | West beach and dock; keeper/supply compound | The keeper house anchors the upper camp, the tent belongs to the fenced supply area, and the dock is attached to the shoreline |
| Glowglade Grove | A narrow trail enters from the west and bends toward the creek | Woodland shelter, mushroom clearing and creek bank | The bridge marks the eastern creek crossing; the water companion waits at the near bank |
| Tidal Ruins | A broad stone processional runs north from the western entrance | Paired tidal pools, flanking altars and rubble pockets | The shrine occupies the central axis; crystals and the companion encounter sit on the eastern approach |
| Lighthouse Courtyard | The southern entry leads directly up the central forecourt | Symmetrical garden pockets and broken perimeter walls | The tower anchors the north; the guardian occupies the open southern arena, with the beacon stone between them |

## Placement rules

- Main routes are established in the ground layer before props are placed.
- Buildings, trees, walls and shore features frame the route; they do not form arbitrary obstacles across it.
- Story interaction coordinates and progression markers remain stable so existing dialogue, pickups, saves and transitions are preserved.
- Keep a clear approach cell around the keeper, companion encounters, shrine and boss.
- Decoration is clustered by function: supplies around the tent, coastal objects around the dock, flora around grove clearings, masonry around ruin altars, and garden props around the lighthouse court.
- Water and cliffs describe island geography; large flat blue or sand rectangles are not used as visual filler.

## Technical constraints

- Orthogonal top-down projection; 18 × 12 cells, 56 px per cell.
- Tiled remains the single source of truth for terrain, static props, collision and story markers.
- Runtime map compilation and BFS reachability validation remain mandatory.
- Raspberry Pi 5 uses the same compiled map data; no runtime Tiled or Python dependency.
