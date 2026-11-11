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
