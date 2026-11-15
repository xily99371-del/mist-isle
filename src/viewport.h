#ifndef MISTISLE_VIEWPORT_H
#define MISTISLE_VIEWPORT_H

#include "raylib.h"

/* The displayed 16:9 region uses whole physical pixels. Drawing and mouse
 * mapping share this rectangle, including letterboxing and HiDPI scaling. */
typedef struct {
    Rectangle destination;
    int pixelWidth, pixelHeight;
} GameViewport;

static inline GameViewport CalculateGameViewport(int screenW, int screenH,
                                                 int renderW, int renderH) {
    if (screenW < 1) screenW = 1;
    if (screenH < 1) screenH = 1;
    if (renderW < 16) renderW = 16;
    if (renderH < 9) renderH = 9;
    int units = renderW / 16;
    if (renderH / 9 < units) units = renderH / 9;
    int width = units * 16, height = units * 9;
    int left = (renderW - width) / 2, top = (renderH - height) / 2;
    float toScreenX = (float)screenW / (float)renderW;
    float toScreenY = (float)screenH / (float)renderH;
    return (GameViewport){
        {(float)left * toScreenX, (float)top * toScreenY,
         (float)width * toScreenX, (float)height * toScreenY},
        width, height
    };
}

static inline GameViewport CurrentGameViewport(void) {
    return CalculateGameViewport(GetScreenWidth(), GetScreenHeight(),
                                 GetRenderWidth(), GetRenderHeight());
}

#endif
