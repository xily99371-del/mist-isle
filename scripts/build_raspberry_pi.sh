#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

for tool in cmake python3 cc; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "Missing required tool: $tool"
    exit 1
  fi
done

echo "Building Mist Isle for Raspberry Pi 5 (ARM64, desktop/X11, OpenGL 2.1)"
echo "Required development packages: build-essential cmake git python3"
echo "  libx11-dev libxcursor-dev libxinerama-dev libxrandr-dev libxi-dev"
echo "  libasound2-dev libgl1-mesa-dev libwayland-dev libxkbcommon-dev wayland-protocols"
python3 tools/generate_ground_atlas.py
cmake -S . -B build-pi -DCMAKE_BUILD_TYPE=Release -DMISTISLE_PI_PROFILE=ON
cmake --build build-pi --parallel "$(nproc)"

DIST="$ROOT/dist-pi"
rm -rf "$DIST"
mkdir -p "$DIST"
cp build-pi/mist_isle "$DIST/"
cp -a assets "$DIST/"
cp README.md "$DIST/"
echo
echo "Build complete: $DIST/mist_isle"
echo "Run from the package directory so relative asset paths resolve:"
echo "  cd $DIST && ./mist_isle"
