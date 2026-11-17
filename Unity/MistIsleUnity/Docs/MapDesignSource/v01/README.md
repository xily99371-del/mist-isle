# Mist Isle — Chapter 1 Level Design & Asset Pack v0.1

This is the first combined delivery of the five-map level design package and the initial Tilesets / Objects asset drop for Unity 6.3 LTS.

## What is included

### World and level design
- `00_World/WorldOverview.png`
- `00_World/WorldConnections.json`
- `00_World/ProgressionGraph.json`
- Five detailed map blueprints under `01_Maps/`
- Per-map `Layout.json`, `Interactions.json`, and `Design.md`

### Tilesets
- Common 8×8 terrain atlas, 16×16 px per tile.
- Coastal, Forest, Tidal Ruins and Lighthouse palette variants.
- Row/category guide and Unity slicing instructions.

### Objects
- Extracted transparent environment-object atlas.
- Original concept atlas crop for visual comparison.
- Thematic crops for Coastal, Forest, Tidal Ruins and Lighthouse.
- Approximate SpriteRect and pivot entries in `05_Unity/AssetManifest.json`.

### Unity handoff
- Tilemap layer specification.
- Collision specification.
- Sprite import rules.
- Tileset slicing guide.
- Object atlas slicing guide.
- Codex production handoff and asset limitations.

## Canonical world graph

`M00 Coastal Camp → M01 Tidewind Path → (M02 Glowglade and M03 Tidal Ruins) → M04 Lighthouse Courtyard`

M01 is the junction hub. M02 grants the Forest Seal. M03 grants the Tide Seal and Crystal Seal. M04 requires all three seals before the final gate opens.

## Tile specification
- Base tile: 16×16 px.
- Pixels Per Unit: 16.
- Unity Grid Cell: 1×1.
- Filter Mode: Point.
- Compression: None.
- Atlas layout: 8 columns × 8 rows, 128×128 px.

## Important status note

This is a **prototype asset pack (v0.1)**, not a claim that every final production asset is finished. The regional tilesheets are palette variants derived from the common atlas; they do not yet contain fully unique region-specific geometry or complete autotile edge/corner variants. The object atlas is extracted from an approved environment concept sheet; listed SpriteRects are approximate and must be verified in Unity Sprite Editor. Animated water/VFX, full interiors, individual object exports and final collider polygons remain future work.

Use this pack as the art and level-design source of truth for the first Unity greybox and first visual pass. Do not ask Codex to invent new map layouts; have it implement the provided maps and report any missing asset requirements.
