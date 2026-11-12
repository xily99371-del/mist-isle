# Hub + sub-map architecture (first pass)

## World structure
- Coastal Camp is the hub and starting map.
- Glowglade Forest is a dedicated exploration/foraging map.
- Tidal Ruins is a dedicated puzzle and crystal companion map.
- Lighthouse Courtyard is the final encounter map.

Each map is rebuilt from a deterministic seed plus its area ID. The active map has its own terrain palette, obstacle layout, pickup list and landmark composition. The hub exposes three entrances; the sub-maps return through a shared west-side exit.

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
- Each area receives a distinct ground palette and separate environment prop composition.
- Camp uses house, tent, crates, fence and campfire props.
- Grove uses trees, mushrooms, fallen log, shrubs and stream tiles.
- Ruins uses stone floor, water channels and rune-stone pillars.
- Lighthouse courtyard uses lighthouse, fence, shrubs and stone/sand paving.
- Outside the playable diamond, a themed backdrop fills the screen margins instead of a flat pale void.

## Asset status
The map composition reuses the approved art pack's current environment atlas and isometric ground tiles. This is a first content pass, not yet a complete region-specific art expansion. Dedicated coastal dock/shore autotiles, a richer ruin arch/column set, courtyard stairs/fountain, and more varied forest flora are still needed to reach final art quality.
