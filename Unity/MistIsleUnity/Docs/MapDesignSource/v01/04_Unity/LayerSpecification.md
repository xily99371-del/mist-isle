# Unity Layer Specification

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

## Collision rules
- Buildings, cliff walls, deep water, and sealed gates are blocked.
- Shallow ford tiles are walkable only when explicitly marked in map data.
- Root bridges, docks, floating platforms, stairs, and ramps are walkable.

## Trigger rules
- All area exits are box triggers.
- Guardian arenas use a trigger + lock-in gate + cutscene event.
- Treasure chests use single-activation interaction objects.
