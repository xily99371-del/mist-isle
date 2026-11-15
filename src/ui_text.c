#include "ui_text.h"
#include "rlgl.h"
#include <limits.h>
#include <math.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#if defined(MISTISLE_PI_PROFILE)
#define UI_TEXT_CACHE_SLOTS 12
#else
#define UI_TEXT_CACHE_SLOTS 24
#endif

typedef struct {
    Font font;
    int pixelSize;  /* Also records failed loads, so they are not retried per draw. */
    uint64_t lastUse;
} UITextCacheEntry;

typedef struct {
    Font font;
    float size;
    float spacing;
} UITextStyle;

static UITextCacheEntry FONT_CACHE[UI_TEXT_CACHE_SLOTS];
static char *FONT_PATH;
static int *FONT_CODEPOINTS;
static int FONT_CODEPOINT_COUNT;
static uint64_t FONT_USE_CLOCK;
static float RASTER_SCALE_X = 1.0f;
static float RASTER_SCALE_Y = 1.0f;
static float RASTER_OFFSET_X;
static float RASTER_OFFSET_Y;

void UITextClose(void) {
    bool hasFonts = false;
    for (int i = 0; i < UI_TEXT_CACHE_SLOTS; i++)
        if (FONT_CACHE[i].font.texture.id) hasFonts = true;

    /* Text may still reference these textures in raylib's active batch. */
    if (hasFonts) rlDrawRenderBatchActive();
    for (int i = 0; i < UI_TEXT_CACHE_SLOTS; i++)
        if (FONT_CACHE[i].font.texture.id) UnloadFont(FONT_CACHE[i].font);
    memset(FONT_CACHE, 0, sizeof(FONT_CACHE));
    free(FONT_PATH);
    free(FONT_CODEPOINTS);
    FONT_PATH = NULL;
    FONT_CODEPOINTS = NULL;
    FONT_CODEPOINT_COUNT = 0;
    FONT_USE_CLOCK = 0;
    RASTER_SCALE_X = RASTER_SCALE_Y = 1.0f;
    RASTER_OFFSET_X = RASTER_OFFSET_Y = 0.0f;
}

void UITextConfigure(const char *path, const int *codepoints, int count) {
    UITextClose();
    if (!path || !path[0] || !codepoints || count <= 0) return;
    if ((size_t)count > SIZE_MAX/sizeof(*FONT_CODEPOINTS)) return;

    size_t pathBytes = strlen(path) + 1;
    size_t glyphBytes = (size_t)count*sizeof(*FONT_CODEPOINTS);
    FONT_PATH = malloc(pathBytes);
    FONT_CODEPOINTS = malloc(glyphBytes);
    if (!FONT_PATH || !FONT_CODEPOINTS) {
        UITextClose();
        TraceLog(LOG_WARNING, "Mist Isle: could not configure native text cache; using fallback font.");
        return;
    }
    memcpy(FONT_PATH, path, pathBytes);
    memcpy(FONT_CODEPOINTS, codepoints, glyphBytes);
    FONT_CODEPOINT_COUNT = count;
}

void UITextSetRasterScale(float sx, float sy) {
    RASTER_SCALE_X = isfinite(sx) && sx > 0.0f ? sx : 1.0f;
    RASTER_SCALE_Y = isfinite(sy) && sy > 0.0f ? sy : 1.0f;
}

void UITextSetRasterOffset(float ox, float oy) {
    RASTER_OFFSET_X = isfinite(ox) ? ox : 0.0f;
    RASTER_OFFSET_Y = isfinite(oy) ? oy : 0.0f;
}

static Font FontAtPixelSize(int pixelSize) {
    if (!FONT_PATH) return (Font){0};

    int slot = 0;
    for (int i = 0; i < UI_TEXT_CACHE_SLOTS; i++) {
        if (FONT_CACHE[i].pixelSize == pixelSize) {
            FONT_CACHE[i].lastUse = ++FONT_USE_CLOCK;
            return FONT_CACHE[i].font;
        }
        if (FONT_CACHE[i].lastUse < FONT_CACHE[slot].lastUse) slot = i;
    }

    UITextCacheEntry *entry = &FONT_CACHE[slot];
    if (entry->font.texture.id) {
        rlDrawRenderBatchActive();
        UnloadFont(entry->font);
    }
    *entry = (UITextCacheEntry){.pixelSize = pixelSize, .lastUse = ++FONT_USE_CLOCK};

    Font loaded = LoadFontEx(FONT_PATH, pixelSize, FONT_CODEPOINTS, FONT_CODEPOINT_COUNT);
    unsigned int defaultTexture = GetFontDefault().texture.id;
    if (loaded.texture.id && loaded.texture.id != defaultTexture &&
        IsFontValid(loaded) && loaded.baseSize == pixelSize &&
        loaded.glyphCount == FONT_CODEPOINT_COUNT) {
        /* At 1:1 aligned pixels this samples the original glyph coverage.
         * Bilinear filtering also accommodates fractional camera motion. */
        SetTextureFilter(loaded.texture, TEXTURE_FILTER_BILINEAR);
        entry->font = loaded;
    } else {
        if (loaded.texture.id != defaultTexture) UnloadFont(loaded);
        TraceLog(LOG_WARNING, "Mist Isle: could not load UI font at %d pixels; using fallback font.", pixelSize);
    }
    return entry->font;
}

static UITextStyle ResolveStyle(Font fallback, float logicalSize, float spacing) {
    UITextStyle style = {fallback, logicalSize, spacing};
    if (!style.font.texture.id || !IsFontValid(style.font)) style.font = GetFontDefault();

    double pixels = round((double)logicalSize*RASTER_SCALE_Y);
    if (pixels < 1.0) pixels = 1.0;
    if (isfinite(pixels) && pixels <= INT_MAX) {
        Font cached = FontAtPixelSize((int)pixels);
        if (cached.texture.id) style.font = cached;
        style.size = (float)pixels/RASTER_SCALE_Y;
    }
    /* Otherwise even a snapped origin would acquire fractional glyph advances
     * from letter spacing. Measuring follows the same rounding as drawing. */
    style.spacing = roundf(spacing*RASTER_SCALE_X)/RASTER_SCALE_X;
    return style;
}

void UITextDraw(Font fallback, const char *text, Vector2 logicalPos,
                float logicalSize, float spacing, Color tint) {
    if (!text || !text[0] || !isfinite(logicalSize) || logicalSize <= 0.0f) return;
    UITextStyle style = ResolveStyle(fallback, logicalSize, spacing);
    logicalPos.x = (roundf(logicalPos.x*RASTER_SCALE_X + RASTER_OFFSET_X) -
                    RASTER_OFFSET_X)/RASTER_SCALE_X;
    logicalPos.y = (roundf(logicalPos.y*RASTER_SCALE_Y + RASTER_OFFSET_Y) -
                    RASTER_OFFSET_Y)/RASTER_SCALE_Y;
    DrawTextEx(style.font, text, logicalPos, style.size, style.spacing, tint);
}

Vector2 UITextMeasure(Font fallback, const char *text,
                     float logicalSize, float spacing) {
    if (!text || !text[0] || !isfinite(logicalSize) || logicalSize <= 0.0f)
        return (Vector2){0};
    UITextStyle style = ResolveStyle(fallback, logicalSize, spacing);
    return MeasureTextEx(style.font, text, style.size, style.spacing);
}
