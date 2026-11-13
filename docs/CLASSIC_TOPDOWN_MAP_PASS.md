# Classic top-down RPG map pass — visual revision 2

This revision responds to in-game screenshots from the first playable pass.

## Problems identified from the screenshots

- The grove felt empty because the forest canopy was confined to the map perimeter, with too few large foreground/midground trees.
- Grove trails and clearings occupied too much yellow ground, creating disconnected blocky patches rather than a readable route.
- The ruins used broad sand bands and large water patches that competed with the stone architecture.
- Arches, columns and small environmental props were underscaled relative to the player and the map.

## Revision

- Narrowed the grove path to a single-tile winding trail. Exploration clearings remain grass rather than becoming large dirt rectangles.
- Increased tree sprite display size from 100×112 to 138×152 logical pixels, and placed additional overlapping canopy groups around the route while keeping required paths and story points open.
- Added a prominent luminous flowering tree, crystal landmark, and denser flower/mushroom clusters to give Glowglade Grove a distinct identity.
- Removed bridge-dependent map composition. Current Chapter 1 maps do not place prop_32 bridges.
- Rebuilt Tidal Ruins as a continuous stone floor with only small isolated reflecting pools and a few deliberate erosion marks. Sand is no longer used as a broad route or perimeter stripe.
- Increased the display scale of arches, pillars, broken walls, docks, boats, tents, luminous trees and rune fountains. The map preview renderer uses the same display sizes as the runtime renderer.
- Retained the existing map dimensions, tile size, story marker coordinates and save/progression model.

## Validation

All six required story markers remain reachable from the authored spawn on each map when checking tree blockers and water cells. The grove remains within the 64-object map compiler limit.

The previous CI run validated the earlier map pass. The enlarged renderer and latest decorative placements require a fresh Windows and Raspberry Pi 5 build before they should be treated as release-ready.
