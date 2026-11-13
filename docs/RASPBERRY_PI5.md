# Raspberry Pi 5 build and performance profile

Mist Isle is developed on Windows and built natively on Raspberry Pi OS 64-bit. The Pi build uses the same C source, Tiled maps and runtime assets as Windows.

## Graphics compatibility

The default Windows build uses raylib 5.5 with desktop OpenGL 3.3. The Pi profile selects raylib's desktop OpenGL 2.1 backend for the Raspberry Pi OS desktop Wayland/X11 path. Raspberry Pi 5's VideoCore VII supports OpenGL ES 3.1 and Vulkan 1.2, but this project intentionally starts with the conservative raylib desktop backend to keep one shared rendering code path.

The Pi profile disables 4x MSAA, opens a 1280x720 resizable window without forcing maximization, keeps the 1280x720 logical render canvas, and reduces rain/ripple draw counts. The renderer also culls terrain, props and weather ripples outside the camera view. F3 toggles a small FPS/frame-time overlay.

## Prepare Raspberry Pi OS

Use a 64-bit Raspberry Pi OS desktop installation. Install the native build dependencies:

    sudo apt update
    sudo apt install -y build-essential cmake git python3 \
      libx11-dev libxcursor-dev libxinerama-dev libxrandr-dev libxi-dev \
      libasound2-dev libgl1-mesa-dev

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
- The low-cost Pi profile reduces rain streaks and surface ripples while preserving gameplay and map data.

This is a native ARM64 compile profile, not a claim that the final frame rate has already been measured on the physical Pi 5. Final performance acceptance must be measured on the target board.
