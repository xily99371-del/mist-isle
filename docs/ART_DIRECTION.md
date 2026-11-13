# Art direction — Mist Isle

## Style lock

The world uses a **classic handheld-era top-down pixel RPG map language**, taking structural inspiration from the readable exploration maps of Pokémon Emerald while retaining Mist Isle's own cozy storybook-fantasy identity. This is a reference for spatial design and visual hierarchy, not a request to copy Pokémon sprites, tiles, characters, maps or UI.

- Orthogonal top-down view; no isometric ground projection in playable maps.
- Clear 16/32-pixel-art logic at the sprite level, displayed on the existing 56px world grid.
- Readable routes, distinct landmarks, deliberate clearings, loops and optional side paths.
- Terrain boundaries should describe geography: beaches, creek banks, forest edges and worn stone.
- Props are grouped into coherent clusters around places and landmarks; avoid evenly scattered decoration.
- Every sub-map entrance and return exit must be legible from the scene composition and the in-game gate sign.
- Player and companions remain visually distinct from the ground; depth sorting uses their ground-contact Y.

## Chapter 1 map language

- **Coastal Camp:** a welcoming hub with a curved beach, a small village path network, camp clearings and visible routes to the three chapter regions.
- **Glowglade Grove:** dense forest framing a winding footpath, small stream crossings, bridge landmarks, open story clearings and a route that branches toward the shrine and guardian.
- **Tidal Ruins:** an ancient stone causeway, shallow water cuts, two readable bridge crossings, weathered sandy margins and a clear approach to the shrine.
- **Lighthouse Courtyard:** a compact stone plaza with garden pockets, a strong central approach, coastal edge and an unobstructed route to the lighthouse encounter.

## Palette

- Leaf: #8FBE86
- Sea: #79C8D1
- Warm cream: #FFF1D2
- Lantern gold: #FFD875
- Mist lilac: #BEB0E8
- Ink outline: #536B62

Terrain atlases should retain enough local color variation to avoid flat color fields, but their contrast must remain subordinate to characters, paths and landmarks. Water needs visible depth variation, shore transitions and restrained animated highlights; avoid a single uninterrupted saturated-blue fill.

## Composition rules

1. Establish the main route first, then place optional paths and story clearings.
2. Keep a walkable corridor between every required story marker; verify it against object and water collision.
3. Use trees, rocks, fences and buildings to frame routes rather than randomly obstructing them.
4. Reserve visual breathing room around pickups, NPCs, bridges, gates and interaction points.
5. Vary prop clusters by area. Reuse of the same prop is acceptable only when scale, spacing and grouping feel intentional.
6. Keep the shoreline and map perimeter irregular and terrain-led; do not use a repetitive rectangular border.
7. The map preview is a visual QA artifact, not a runtime asset.

## Original character assets

The first original vector character set is in assets/characters/svg/:
- hero_explorer.svg — young island explorer
- moss_lumi.svg — shy lantern-moss companion
- moatail.svg — playful water companion
- crystal_gecko.svg — alert crystal gecko

These are original project vector illustrations created for this repository. They are initial portrait/character concept assets, not final rigged 3D models or animation sheets.

## External asset candidates

- Quaternius Ultimate Stylized Nature: https://quaternius.com/packs/ultimatestylizednature.html
- Quaternius Ultimate Monsters: https://quaternius.com/packs/ultimatemonsters.html
- Kenney UI Pack Adventure: https://kenney.nl/assets/ui-pack-adventure
- Kenney Particle Pack: https://kenney.nl/assets/particle-pack

Before importing third-party packs, inspect the exact current license shipped with the downloaded pack. Do not redistribute raw packs unless the license permits it. Keep license notices and source attribution in assets/licenses/.
## Region-specific architecture and Tiled composition

- Coastal Camp owns the lighthouse-keeper cottage compound, dock, boat and canvas supply tent. Do not reuse this compound asset in the grove.
- Glowglade Grove uses the separate woodland shelter/canvas hideaway asset (Tiled object `prop_22`), framed by tree canopies, lanterns, luminous flowers and mushroom clusters. The coastal lighthouse silhouette must never appear in this map.
- Tidal Ruins uses arches, columns, broken walls, rune basins and the unique multi-ring keystone. Do not place coastal or woodland buildings in the ruins.
- Lighthouse Courtyard owns the lighthouse tower and formal stone forecourt.

## Art production rule

Do not create character portraits, buildings, trees, portals or decorative props from runtime primitives or procedural drawing code. Use authored atlas sprites, Tiled object placement, and finished static illustration assets. Runtime code is reserved for rendering, animation, lighting, collision and interaction. Procedural geometric silhouettes are temporary fallbacks only and must not be treated as final art.
