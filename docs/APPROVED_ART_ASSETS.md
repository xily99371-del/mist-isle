# Approved generated art import

The user selected the generated art direction consisting of:
- Pokémon-like original creature character appeal (cute, expressive, collectible designs; not Pokémon assets).
- Stardew-like cozy pixel environment mood (warm natural palette, handcrafted props and tiles; not Stardew Valley assets).

## Runtime assets
The approved runtime sprite archive is saved in ChatGPT Library at `/Game Projects/Mist Isle/MistIsle_RuntimeSprites_v1.zip`. Download and extract it into the repository root once; it creates the runtime folders listed below. The GitHub CI remains able to compile without the optional art folders, but a CI package built without them will use fallback sprites.

After extraction:
- `assets/characters/hero/hero_walk_4dir_64.png`: 4x4 sprite sheet, 32x32 cells; rows front/left/right/back, columns animation frames.
- `assets/characters/companions/moss_lumi_idle_64.png`
- `assets/characters/companions/moatail_idle_64.png`
- `assets/characters/companions/crystal_gecko_idle_64.png`

Each companion sheet is a four-frame horizontal strip with 32x32 cells. Leafling and Grove Mote sprite strips are included for upcoming wildlife rendering.

The renderer loads the approved hero and three companion sprite sheets directly. Texture filtering is point sampling to preserve crisp pixel edges.

## Art source archive
The Library archive `MistIsle_GeneratedArt_Import_v1.zip` preserves:
- Original generated character sprite sheets.
- Original generated environment tileset and HUD reference board.
- Cropped environment art bands for grass, sand, stone, water, cliffs, fences, flora, props, camp, ruins and lighthouse.
- Processed 64px-cell source sprites and import notes.

The environment board is a generated illustration atlas, not yet a fully separated autotile set. It is retained as source art; do not claim every visual crop is already integrated into the live map. Further environment integration requires manual tile extraction and collision/occlusion mapping.

## Attribution and IP
These are original generated assets created for this project using ChatGPT image generation. They are not extracted from Pokémon or Stardew Valley. Those games are style references only.
