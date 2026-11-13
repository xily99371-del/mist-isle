# Classic top-down RPG map pass

This pass moves the four Chapter 1 maps toward the readable exploration grammar of classic handheld top-down RPGs.

## What changed

- Re-authored the four Tiled Ground layers around intentional route networks instead of a visually noisy, repeating terrain pattern.
- Coastal Camp now has a layered beach, a readable village/camp path network and an east-side route to each chapter entrance.
- Glowglade Grove now has a winding trail, two stream crossings aligned to existing bridge props, a small secondary crossing and distinct clearings for exploration.
- Tidal Ruins now reads as a stone causeway with two water cuts crossed by the existing bridges, plus worn sandy margins.
- Lighthouse Courtyard now has a central stone plaza, garden pockets and a direct route from the entrance to the shrine and guardian.
- Kept the existing 22 × 18 map dimensions, 56px orthogonal grid, named layers, object IDs, story marker positions, save data and gameplay progression unchanged.

## Validation boundary

The map compiler treats water as solid except for the three cells under each prop_32 bridge. Tree objects and authored collision rectangles also block movement. All six required markers on each map must remain reachable from spawn.

The new ground layouts were checked against those rules before being committed. The repository build must still run tools/build_maps.py, tools/validate_generated_maps.py, and tools/validate_project.py in CI.

## Next visual QA

1. Review all four generated map previews at native resolution and as a chapter contact sheet.
2. Adjust prop placement where trees, structures or foliage compete with the route.
3. Check bridge alignment and player walkability in the actual game.
4. Check water animation and shoreline foam at 60 FPS on Windows, then measure the Pi 5 profile with F3.
5. Only after the top-down maps are visually accepted, tune the title, HUD and battle presentation to match the same pixel-art language.

## Important

This is a map-composition pass, not a final art sign-off. The current maps reuse the existing original Mist Isle terrain and prop atlases. Pokémon Emerald is a design reference for legibility and exploration flow only; no Pokémon artwork or map data is used.
