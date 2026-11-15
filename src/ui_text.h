#ifndef MIST_ISLE_UI_TEXT_H
#define MIST_ISLE_UI_TEXT_H

#include "raylib.h"

/* Own copies of the validated font path and complete UI/lyric character set.
 * Configure and close while the graphics context is still available. */
void UITextConfigure(const char *path, const int *codepoints, int count);

/* Physical pixels per logical unit, including camera zoom when applicable.
 * This does not change the projection, camera, or caller's drawing state. */
void UITextSetRasterScale(float sx, float sy);

/* Translation in physical pixels, used to snap text after camera movement. */
void UITextSetRasterOffset(float ox, float oy);

/* Draw and measure use the same pixel-sized font and rounded pixel spacing.
 * Positions are snapped using the current raster scale and offset, without
 * changing the camera. The fallback font remains owned by the caller. */
void UITextDraw(Font fallback, const char *text, Vector2 logicalPos,
                float logicalSize, float spacing, Color tint);
Vector2 UITextMeasure(Font fallback, const char *text,
                     float logicalSize, float spacing);
void UITextClose(void);

#endif
