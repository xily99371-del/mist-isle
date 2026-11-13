# Chapter 1 Map Review

Chapter 1 is authored as four compact, single-screen Tiled scenes. The game and the map preview tool read the same `.tmx` ground, object, and marker data.

## Scene composition

| Map | Layout intent | Landmark composition |
|---|---|---|
| Coastal Camp | Central stone forecourt with branching sand paths | Keeper's house, supply tent, fenced campsite, dock and boat, rocky shoreline, three region routes |
| Glowglade Grove | Winding forest clearing opening onto a creek | Woodland cottage, field shelter, flower groves, mushroom clearing, creek bridge, cliff and waterfall edge |
| Tidal Ruins | Stone processional platform framed by tidal pools | Broken arches, paired columns, side altars, central shrine approach, scattered rubble and moss |
| Lighthouse Courtyard | Formal stone approach with an open central arena | Lighthouse tower, paired ornamental gates, broken side walls, columns, shoreline cliffs and guardian approach |

## Authoring and runtime rules

- Each map is 18 × 12 cells at 56 px per cell.
- Tiled layers own ground, collision, scene objects and story markers.
- Large buildings, tree trunks, water, cliff bases, boulders and fence segments compile into movement collision.
- Bridge objects explicitly open a walkable span through water.
- Story markers are checked for both collision and four-direction reachability from the map spawn.
- Collectibles are placed on distinct reachable ground cells and avoid large sprite silhouettes.
- Static terrain and shoreline layers are cached per active area; the Raspberry Pi profile uses bounded weather particles and reduced overdraw.

## Verification

The Windows and Raspberry Pi 5 CI workflow generates a four-map review sheet, validates the Tiled data, and builds both targets. The four map branches were checked independently before being integrated into `feature/scene-interaction-polish`.
