# Approved art integration v2

## Runtime integration
- Main character sprite sheet now uses the approved 256x256 sheet with 64x64 frames (4 columns x 4 directions).
- Moss Lumi, Moatail and Crystal Gecko now use 256x64 companion strips (four 64x64 frames).
- Leafling and Grove Mote are loaded as four-frame strips and rendered with the same nearest-neighbor filtering.
- Environment prop atlas is a 12-slot strip of 96x96 sprites: leafy tree, pink tree, shrub, mushroom group, fallen log, rune stone, lantern post, crate, barrel, lighthouse, lighthouse building and fence.
- Six 64x32 isometric ground diamonds are built from the approved grass, sand, stone and water source sheets.
- Existing collision/navigation grid remains independent from visual tile art.

## Installation
Extract `MistIsle_ArtIntegration_v2.zip` into the repository root, merging `assets/`. The local launcher and CI continue to work without the optional imported art, using fallback visuals where assets are absent.

## Visual scope
This pass replaces player/companion sprite rendering, tree variants, selected campsite/forest props and isometric ground tile textures. It does not yet replace every building, cliff, shoreline segment or UI component; the larger source atlases remain available for subsequent hand-authored scene composition.
