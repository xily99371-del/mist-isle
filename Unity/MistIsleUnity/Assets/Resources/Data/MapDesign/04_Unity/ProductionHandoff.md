# Production Handoff for Codex

## Goal
Build the Unity Tilemap scenes from the supplied blueprints and data; do not invent map layouts or substitute unrelated art.

## Source of truth
- World connections: `00_World/WorldConnections.json`
- Progression gates: `00_World/ProgressionGraph.json`
- Per-map layout and interactions: `01_Maps/<MapID>/*`
- Asset locations and import metadata: `05_Unity/AssetManifest.json`
- Tile slicing: `05_Unity/TilesetSlicing.md`
- Object slicing: `05_Unity/ObjectAtlasSlicing.md`

## Build order
1. Import and slice the common tileset.
2. Create reusable Grid and Tilemap layers.
3. Build M00 as a greybox using exact map dimensions and entrance marker.
4. Replace greybox terrain with supplied tiles and object atlas.
5. Validate collision and exit trigger.
6. Build M01 and verify its three paired exits.
7. Build M02 and M03 as two branches from M01.
8. Build M04 and gate the entrance on all three seals.

## Acceptance checks
- Every exit ID has exactly one matching target entry ID.
- Player spawn points are walkable and face the intended direction.
- No decorative sprite automatically becomes a solid collider.
- All large structures have authored collider geometry.
- Map size matches Layout.json exactly.
- Every interaction has a stable ID and is discoverable in the scene hierarchy.
