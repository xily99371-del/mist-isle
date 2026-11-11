# Grove exploration milestone

## Gameplay additions
- Field journal opened with J and closed with J or Esc.
- Firefly encounter: approach the drifting grove light and press E to register the discovery.
- Leafling encounter: offer one island fruit; it reveals a hidden star crystal cache.
- Additional forage nodes are distributed in the grove and resource capacity is raised to 32.
- Grove floor pocket is kept walkable around the encounter points to prevent procedural obstacle placement from blocking story interactions.

## Visual additions
- Four new original pixel props are generated: mushroom cluster, fallen log, rune stone and flower patch.
- Grove dressing is depth-sorted together with trees, props, pickups and characters.
- Firefly glow and leafling idle bobbing provide local ambient animation.

## Save system
- F5 writes a versioned binary save to `mist_isle_save.dat`.
- F9 loads the save from the title screen or while exploring.
- Autosave occurs after collecting resources, befriending companions and solving the shrine.
- Saved state includes player position/health, inventory, companions, selected companion, puzzle/boss flags, discoveries and collected pickup flags.

## Validation
The Windows GitHub Actions workflow regenerates pixel assets, configures CMake, compiles the C application, packages the executable and includes the runtime asset directories. A successful CI run validates compilation and packaging; interactive gameplay still requires a local playthrough.
