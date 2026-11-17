# Tileset Slicing and Tilemap Setup

## Grid
- Cell size: 1 × 1 Unity unit.
- Tile pixels: 16 × 16.
- Pixels Per Unit: 16.
- Atlas size: 128 × 128.
- Slice mode: Grid by Cell Size, 16 × 16 px, zero offset, zero padding.

## Atlas row guide
| Row (top-down) | Category |
|---:|---|
| 0 | Grass variants and flower patches |
| 1 | Dirt, packed earth and stony soil |
| 2 | Beach sand, shells and shore transition |
| 3 | Stone paving, cobbles and mossy path |
| 4 | Wooden planks, dock boards and platforms |
| 5 | Shallow and deep water |
| 6 | Rock, cliff and mossy stone |
| 7 | Dark forest floor, leaves and mushrooms |

## Tilemap layers
1. Ground
2. Water
3. CliffsBack
4. PropsBack
5. Collision
6. Interactions
7. Triggers
8. Spawn
9. PropsFront
10. VFXMarkers

Do not use the palette variants as final autotile sets without adding edge, corner, inside-corner and transition variants. The current sheets are a starter block library.
