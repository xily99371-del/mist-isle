#ifndef MIST_ISLE_PLATFORM_INPUT_H
#define MIST_ISLE_PLATFORM_INPUT_H

#include <stdbool.h>

/* Call after InitWindow, once at the start of each frame, and before CloseWindow.
 * The native handle is raylib's GetWindowHandle(). This module never changes
 * the user's keyboard layout or the IME settings of any other application. */
void PlatformInputInit(void *windowHandle);
void PlatformInputUpdate(void);
void PlatformInputClose(void);

/* Accept raylib KeyboardKey values. On Windows these query the frame's physical
 * key snapshot, so an IME cannot swallow movement or action keys. Other targets
 * keep raylib's input implementation. Unsupported keys return false on Windows. */
bool GameInputDown(int key);
bool GameInputPressed(int key);

#endif
