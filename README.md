# Mist Isle — 雾屿回响

A cute anime-inspired 2.5D companion adventure built in C with raylib. Windows-first development, Raspberry Pi 5 deployment target.

## Chapter 1: The Lost Lighthouse
Explore a small island, befriend three companions, solve an elemental ruin puzzle, and restore the lighthouse.

## Current status
The repository is being initialized. The first commit will contain the playable chapter prototype, asset pipeline, source/license manifest, and Windows build instructions. Visual assets are being added separately from code-generated placeholders.

## Build
Requires CMake, Git, and a C compiler. CMake fetches raylib 5.5.

```powershell
cmake -S . -B build
cmake --build build --config Release
.\build\Release\mist_isle.exe
```

Controls: WASD move, Shift dash, E interact, Tab switch companion, 1/2/3 select battle skill, Enter confirm.

## Art direction
Cute anime / storybook fantasy, rounded silhouettes, expressive companions, warm island palette, painterly outlines and soft magical lighting. See `docs/ART_DIRECTION.md`.


## Art assets
Original vector character portraits are included under `assets/characters/svg/`:
- `hero_explorer.svg`
- `moss_lumi.svg`
- `moatail.svg`
- `crystal_gecko.svg`

The C renderer loads the rasterized PNG versions when present and falls back to the code-drawn placeholder if they are absent. Generate the PNGs before running:

```powershell
python -m pip install cairosvg
python tools/rasterize_assets.py
```

Then run the executable with the repository root as the working directory so relative asset paths resolve.

See `docs/ART_DIRECTION.md` and `docs/ASSET_MANIFEST.md` for the visual style and asset/license record.
