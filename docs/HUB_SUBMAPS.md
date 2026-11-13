# Hub + sub-map architecture (first pass)

## World structure
- Coastal Camp is the hub and starting map.
- Glowglade Forest is a dedicated exploration/foraging map.
- Tidal Ruins is a dedicated puzzle and crystal companion map.
- Lighthouse Courtyard is the final encounter map.

Each map is hand-authored in Tiled. Ground tiles, collision rectangles, scene-object placements and story markers are compiled into static C arrays at build time. The hub exposes three entrances; the sub-maps return through their marked west-side exits.

## Progression
1. Start at Coastal Camp and rescue Moss Lumi.
2. Enter Glowglade Forest from the northern/eastern hub gate; discover Firefly, help Leafling, defeat the Grove Mote and recruit Moatail.
3. Return to the hub. The Tidal Ruins gate opens once Moss Lumi and Moatail are owned.
4. Find a Tide Shell and Star Crystal in the ruins, recruit Crystal Gecko, and activate the shrine with all three companions.
5. Return to the hub and enter Lighthouse Courtyard after the seal is lifted.
6. Defeat the lighthouse guardian and return to the shrine in the courtyard for the ending sequence.

## Persistence
- Save format v4 stores the active area and collected pickup flags separately for all four maps.
- Re-entering a map restores its collected resource state.
- Gate transitions save the current area; loading rebuilds the corresponding map and restores its resources.

## Visual composition
- Each area receives a hand-authored ground layer, collision layer, object layer and marker layer. Its ground palette and object composition are edited in Tiled rather than generated from a random seed.
- Camp uses house, tent, crates, fence and campfire props.
- Grove uses trees, mushrooms, fallen log, shrubs and stream tiles.
- Ruins uses stone floor, water channels and rune-stone pillars.
- Lighthouse courtyard uses lighthouse, fence, shrubs and stone/sand paving.
- Outside the playable diamond, a themed backdrop fills the screen margins instead of a flat pale void.

## Asset status
The map composition reuses the approved art pack's current environment atlas and isometric ground tiles. This is a first content pass, not yet a complete region-specific art expansion. Dedicated coastal dock/shore autotiles, a richer ruin arch/column set, courtyard stairs/fountain, and more varied forest flora are still needed to reach final art quality.


## Supplemental area prop atlas v1
A new original 16-prop atlas is provided in `MistIsle_AreaProps_v1.zip`, extracted to `assets/environment/area_props_96.png`. The 16 cells are 96x96 and grouped in order:
- Coast: dock, boat, tent, buoy.
- Grove: flowering tree, glow mushrooms, mossy log, lumen flower.
- Ruins: arch, pillar, broken wall, rune fountain.
- Lighthouse: stone stairs, lighthouse tower, iron gate, garden fountain.

The renderer loads this as an optional atlas and places the matching props in each sub-map. The local game build requires the asset pack to be extracted into the repository root; a clean GitHub CI build without the optional pack falls back to the existing props.


## Orthographic top-down control update
The world renderer now uses an orthographic 2D grid with 56px square cells. WASD and arrow keys map directly to screen directions: W/up moves toward the top of the screen, S/down toward the bottom, A/left and D/right horizontally. The camera follows the player and clamps to each sub-map's world bounds. Ground and object projection use the same tile-center transform; trees and props are sorted by ground-contact Y for correct foreground overlap. The coastal hub uses an irregular oval shoreline with sand transition, while forest, ruins and lighthouse maps use distinct border treatments.


## Tiled map pipeline

The editable map sources live in maps/*.tmx and use maps/terrain.tsx. tools/build_maps.py validates the 22x18 layers and compiles them to area_maps.h in the CMake build directory. Runtime terrain, static collision, object placement and gate/spawn positions all read from that generated data. See MAP_AUTHORING.md.
