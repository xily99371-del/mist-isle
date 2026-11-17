# Collision Specification

## General
- Use CompositeCollider2D for cliffs/walls when practical.
- Use PolygonCollider2D or BoxCollider2D for large props/buildings.
- Use separate trigger colliders for exits, interactables, boss intros, and cutscenes.

## Color convention for future debug overlays
- Red: blocked collision
- Blue: deep water blocked
- Green: walkable
- Yellow: interaction trigger
- Purple: progression trigger / cutscene trigger
