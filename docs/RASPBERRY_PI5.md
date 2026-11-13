# Raspberry Pi 5 build and performance profile

Mist Isle is developed on Windows and built natively on Raspberry Pi OS 64-bit. The Pi build uses the same C source, Tiled maps and runtime assets as Windows.

## Graphics compatibility

The default Windows build uses raylib 5.5 with desktop OpenGL 3.3. The Pi profile selects raylib's desktop OpenGL 2.1 backend for the Raspberry Pi OS desktop Wayland/X11 path. Raspberry Pi 5's VideoCore VII supports OpenGL ES 3.1 and Vulkan 1.2, but this project intentionally starts with the conservative raylib desktop backend to keep one shared rendering code path.

The Pi profile disables 4x MSAA, opens a 1280x720 resizable window without forcing maximization, keeps the 1280x720 logical render canvas, and reduces rain/ripple draw counts. Static area backdrops, ground tiles, Tiled autotile transitions, shoreline foam and cliff edges are baked into one active-area render texture (1232x1008 RGBA, about 4.7 MiB). The cache is rebuilt only when changing areas; animated water highlights, actors, particles and weather remain dynamic. Only one area cache is retained at a time, and a per-tile fallback remains available if render-target allocation fails. This avoids issuing hundreds of static terrain draw calls every frame without requiring extra CPU map parsing or a runtime Tiled dependency. F3 toggles a small FPS/frame-time overlay.

## Prepare Raspberry Pi OS

Use a 64-bit Raspberry Pi OS desktop installation. The build supports the default Wayland session and X11 through GLFW's dual backend. Install the native build dependencies:

    sudo apt update
    sudo apt install -y build-essential cmake git python3 \
      libx11-dev libxcursor-dev libxinerama-dev libxrandr-dev libxi-dev \
      libasound2-dev libgl1-mesa-dev libwayland-dev libxkbcommon-dev wayland-protocols

The first CMake configure downloads raylib 5.5 through FetchContent, so internet access is required for the initial build.

## Build

From the repository root:

    chmod +x scripts/build_raspberry_pi.sh
    ./scripts/build_raspberry_pi.sh

The script creates build-pi/mist_isle and a runnable dist-pi folder containing the executable and assets. The runtime does not need Python or Tiled.

## Performance checks

- Press F3 to show FPS and frame time.
- Test at the 1280x720 window size first.
- Use a stable 60 FPS target; if frame time rises, check thermal throttling and GPU driver configuration before increasing visual effects.
- The low-cost Pi profile reduces rain streaks to 56 and screen ripples to 6, caps the particle pool at 160, renders one water highlight on selected water tiles and skips static grass/sand/stone accent redraws because those details are baked into the cache.
- Verify that frame time stays below 16.7 ms for a 60 FPS target; 33.3 ms is the 30 FPS fallback target. Record FPS after 10 minutes of play to catch thermal throttling.
- Check the F3 overlay in the camp, grove, ruins, lighthouse courtyard, rain and battle scenes. The most demanding scene, not the title screen, determines acceptance.

This is a native ARM64 compile profile, not a claim that the final frame rate has already been measured on the physical Pi 5. Final performance acceptance must be measured on the target board.

## Current performance scope

The ARM64 CI job verifies that the Raspberry Pi profile configures and compiles on an ARM64 Linux runner. It does not emulate the VideoCore VII GPU and therefore does not certify physical-board FPS. The one-area terrain cache is a bounded GPU-memory optimization, not a measured performance claim. Final acceptance still requires a real Raspberry Pi 5 run with the F3 frame-time overlay and a 10-minute thermal soak.
