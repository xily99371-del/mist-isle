# Coastal camp — implementation milestone

The first playable area is being developed as a compact, readable pixel-art campsite.

## Scene composition
- Canvas: 22 × 18 logical tiles, isometric projection.
- Main landmarks: explorer tent, keeper NPC, campfire, supply crate, signpost, mailbox, flower patch, shore rock and distant lighthouse.
- The lighthouse remains visible from the starting area to establish the chapter goal.
- The campfire uses a warm glow and small animated sparks; it is a visual focal point and a safe gathering place.

## Interaction
- The keeper is visible near the tent and uses the same interaction coordinate as the story dialogue.
- The player approaches and presses E to talk.
- The first companion rescue remains available at the camp.
- HUD shows a contextual E prompt while the player is close to the keeper.

## Asset generation
`tools/generate_pixel_assets.py` creates:
- Hero 4-direction walk sheet (4 frames per direction).
- Idle sheets for Moss Lumi, Moatail and Crystal Gecko.
- Isometric ground atlas.
- 12-slot environment props atlas.
- Camp keeper sprite.

All art in the generated sprite sheets is original project art. It is a first-pass pixel-art set, not a third-party game asset rip.
