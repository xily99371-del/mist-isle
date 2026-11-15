#include "platform_input.h"

#if defined(_WIN32)

/* Keep Win32 types and functions in this translation unit: windows.h declares
 * Rectangle, CloseWindow and DrawText, which conflict with raylib's names. */
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#include <imm.h>
#include <string.h>

static HWND gameWindow;
static HIMC savedImeContext;
static bool haveSavedImeContext;
static bool frameFocused;
static bool keyDown[256];
static bool keyPressed[256];

/* raylib 5.5 uses GLFW's KeyboardKey values. Printable A-Z and 0-9 are ASCII;
 * non-printable keys are translated explicitly instead of being passed to the
 * Win32 API. This file deliberately does not include raylib.h. */
static int VirtualKeyForGameKey(int key)
{
    if ((key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')) return key;
    if (key >= 290 && key <= 301) return VK_F1 + key - 290; /* F1-F12 */
    switch (key) {
        case 32:  return VK_SPACE;
        case 256: return VK_ESCAPE;
        case 257: return VK_RETURN;
        case 258: return VK_TAB;
        case 259: return VK_BACK;
        case 260: return VK_INSERT;
        case 261: return VK_DELETE;
        case 262: return VK_RIGHT;
        case 263: return VK_LEFT;
        case 264: return VK_DOWN;
        case 265: return VK_UP;
        case 266: return VK_PRIOR;
        case 267: return VK_NEXT;
        case 268: return VK_HOME;
        case 269: return VK_END;
        case 335: return VK_RETURN; /* keypad Enter */
        case 340: return VK_LSHIFT;
        case 341: return VK_LCONTROL;
        case 342: return VK_LMENU;
        case 344: return VK_RSHIFT;
        case 345: return VK_RCONTROL;
        case 346: return VK_RMENU;
        default: return 0;
    }
}

static bool WindowIsForeground(void)
{
    return gameWindow != NULL && IsWindow(gameWindow) &&
           GetForegroundWindow() == gameWindow;
}

static void DetachGameIme(void)
{
    /* NULL removes only this HWND's input context. Save the context returned
     * by the first detach, even if it is NULL, for exact restoration on exit.
     * Repeating this while focused also handles an IME/layout reactivation. */
    HIMC previous = ImmAssociateContext(gameWindow, NULL);
    if (!haveSavedImeContext) {
        savedImeContext = previous;
        haveSavedImeContext = true;
    }
}

void PlatformInputInit(void *windowHandle)
{
    PlatformInputClose();
    gameWindow = (HWND)windowHandle;
    if (gameWindow != NULL && IsWindow(gameWindow)) DetachGameIme();
    /* First update snapshots held keys without inventing press events. */
    PlatformInputUpdate();
}

void PlatformInputUpdate(void)
{
    bool focused = WindowIsForeground();
    if (!focused) {
        memset(keyDown, 0, sizeof(keyDown));
        memset(keyPressed, 0, sizeof(keyPressed));
        frameFocused = false;
        return;
    }

    DetachGameIme();
    for (int vk = 0; vk < 256; ++vk) {
        /* The low "pressed since last call" bit is shared and unreliable.
         * Use the high bit and our own frame-to-frame rising edge instead. */
        bool down = (GetAsyncKeyState(vk) & 0x8000) != 0;
        keyPressed[vk] = frameFocused && down && !keyDown[vk];
        keyDown[vk] = down;
    }
    frameFocused = true;
}

bool GameInputDown(int key)
{
    int vk = VirtualKeyForGameKey(key);
    return vk != 0 && frameFocused && WindowIsForeground() && keyDown[vk];
}

bool GameInputPressed(int key)
{
    int vk = VirtualKeyForGameKey(key);
    return vk != 0 && frameFocused && WindowIsForeground() && keyPressed[vk];
}

void PlatformInputClose(void)
{
    if (gameWindow != NULL && IsWindow(gameWindow) && haveSavedImeContext) {
        ImmAssociateContext(gameWindow, savedImeContext);
    }
    gameWindow = NULL;
    savedImeContext = NULL;
    haveSavedImeContext = false;
    frameFocused = false;
    memset(keyDown, 0, sizeof(keyDown));
    memset(keyPressed, 0, sizeof(keyPressed));
}

#else

#include "raylib.h"

void PlatformInputInit(void *windowHandle) { (void)windowHandle; }
void PlatformInputUpdate(void) { }
void PlatformInputClose(void) { }
bool GameInputDown(int key) { return IsKeyDown(key); }
bool GameInputPressed(int key) { return IsKeyPressed(key); }

#endif
