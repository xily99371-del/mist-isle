#include "ending.h"
#include "ui_text.h"
#include <math.h>
#include <stdio.h>

/* Three authored wide illustrations, one continuous 28-second performance.
 * Rendering is deterministic: revisiting a time renders the same composition.
 * This module deliberately never starts, seeks, or stops the soundtrack. */
static Texture2D film[3];
static const Color INK = {10, 19, 31, 255};
static const Color FILM_GOLD = {255, 225, 162, 255};
static const Color PAPER = {250, 243, 224, 255};

static float Ease(float from, float to, float value) {
    float u = Clamp((value - from) / (to - from), 0.0f, 1.0f);
    return u * u * (3.0f - 2.0f * u);
}

static float Window(float t, float in, float out) {
    return Ease(in, in + .8f, t) * (1.0f - Ease(out - .8f, out, t));
}

static void TextLayer(Font font, const char *text, Vector2 position, float size, Color color) {
    UITextDraw(font,text,position,size,1.0f,color);
}

static void TextAt(Font font, const char *text, Vector2 position, float size, Color color) {
    if (color.a == 0) return;
    position.x = roundf(position.x);
    position.y = roundf(position.y);
    const Vector2 edge[] = {{-1, 0}, {1, 0}, {0, -1}, {0, 1},
        {-1, -1}, {1, -1}, {-1, 1}, {1, 1}};
    float stroke = size >= 36.0f ? 1.5f : 1.0f;
    Color outline = {3, 9, 16, (unsigned char)((float)color.a * .94f)};
    Color shadow = {3, 9, 16, (unsigned char)((float)color.a * .72f)};
    TextLayer(font, text, (Vector2){position.x, position.y + 3.0f}, size, shadow);
    /* An opaque hold between fades and a fine dark edge keep the same caption
     * readable over both moonlit stone and the bright sunrise artwork. */
    for (int i = 0; i < 8; ++i) {
        TextLayer(font, text, (Vector2){position.x + edge[i].x * stroke,
            position.y + edge[i].y * stroke}, size, outline);
    }
    TextLayer(font, text, position, size, color);
}

static float TextWidth(Font font, const char *text, float size) {
    return UITextMeasure(font,text,size,1.0f).x;
}

static void TextCenter(Font font, const char *text, float y, float size, Color color) {
    float width = TextWidth(font, text, size);
    TextAt(font, text, (Vector2){((float)SW - width) * .5f, y}, size, color);
}

static void EndingControls(Font font, bool preview, float opacity) {
    const char *keys[] = {preview ? "Enter / Esc" : "Enter", preview ? "P" : "R", "P"};
    const char *labels[] = {preview ? "退出预览" : "返回标题",
        preview ? "重播演出" : "开始新旅程", "重播演出"};
    int count = preview ? 2 : 3;
    float widths[3], keyWidths[3], total = 0;
    for (int i = 0; i < count; ++i) {
        keyWidths[i] = TextWidth(font, keys[i], 20) + 22;
        widths[i] = keyWidths[i] + 10 + TextWidth(font, labels[i], 21);
        total += widths[i] + (i > 0 ? 30 : 0);
    }
    float x = ((float)SW - total) * .5f;
    for (int i = 0; i < count; ++i) {
        Rectangle key = {roundf(x), 658, keyWidths[i], 36};
        DrawRectangleRounded(key, .22f, 6, Fade(INK, opacity * .82f));
        DrawRectangleRoundedLines(key, .22f, 6, Fade(FILM_GOLD, opacity * .62f));
        TextAt(font, keys[i], (Vector2){x + 11, 665}, 20, Fade(FILM_GOLD, opacity));
        TextAt(font, labels[i], (Vector2){x + keyWidths[i] + 10, 664}, 21,
            Fade(PAPER, opacity));
        x += widths[i] + 30;
    }
}

/* All coordinates below refer to the authored image. The same transform is
 * applied to the actors, lamp, and path so zooming never detaches the light. */
static Rectangle FrameRect(Texture2D texture, float zoom, Vector2 pan) {
    float width = texture.width > 0 ? (float)texture.width : 16.0f;
    float height = texture.height > 0 ? (float)texture.height : 9.0f;
    float cover = fmaxf((float)SW / width, (float)SH / height) * zoom;
    return (Rectangle){((float)SW - width * cover) * .5f + pan.x,
        ((float)SH - height * cover) * .5f + pan.y, width * cover, height * cover};
}

static Vector2 InFrame(Rectangle r, float x, float y) {
    return (Vector2){r.x + r.width * x, r.y + r.height * y};
}

static void Background(Texture2D texture, Rectangle frame, float alpha, Color tint) {
    if (texture.id) {
        DrawTexturePro(texture, (Rectangle){0, 0, (float)texture.width, (float)texture.height},
            frame, (Vector2){0, 0}, 0.0f, Fade(tint, alpha));
    } else {
        DrawRectangleGradientV(0, 0, SW, SH, Fade((Color){27, 48, 74, 255}, alpha),
            Fade((Color){60, 87, 97, 255}, alpha));
    }
}

static void Atmosphere(float t, float opacity, bool dawn) {
    Color mist = dawn ? (Color){244, 210, 170, 255} : (Color){170, 199, 218, 255};
    for (int i = 0; i < 4; ++i) {
        int y = 330 + i * 51 + (int)(sinf(t * .13f + i * 1.9f) * 17.0f);
        int x = -180 + (int)(sinf(t * .075f + i * 1.3f) * 130.0f);
        int h = 27 + i * 10;
        DrawRectangleGradientV(x, y, SW + 360, h, Fade(mist, 0), Fade(mist, opacity * .10f));
        DrawRectangleGradientV(x, y + h, SW + 360, h, Fade(mist, opacity * .10f), Fade(mist, 0));
    }
}

static void Motes(float t, float opacity, bool dawn) {
    int count = 34;
#if defined(MISTISLE_PI_PROFILE)
    count = 18;
#endif
    for (int i = 0; i < count; ++i) {
        float x = (float)((i * 197 + 41) % SW) + sinf(t * .22f + i) * 22.0f;
        float y = fmodf((float)(i * 83 + 71) + 900.0f - t * (3.0f + (float)(i % 4)), 800.0f) - 40.0f;
        float shimmer = .35f + .45f * (.5f + .5f * sinf(t * 1.3f + i * 1.47f));
        Color c = dawn ? FILM_GOLD : (i % 3 ? (Color){164, 213, 224, 255} : FILM_GOLD);
        DrawCircleGradient((int)x, (int)y, (i % 3 == 0 ? 4.0f : 2.7f), Fade(c, opacity * shimmer), Fade(c, 0));
    }
}

static void Lantern(Vector2 lamp, float t, float power, float heading, float reach) {
    if (power <= .001f) return;
    BeginBlendMode(BLEND_ADDITIVE);
    for (int layer = 0; layer < 9; ++layer) {
        float spread = .008f + (float)layer * .012f;
        Vector2 a = {lamp.x + cosf(heading - spread) * reach, lamp.y + sinf(heading - spread) * reach};
        Vector2 b = {lamp.x + cosf(heading + spread) * reach, lamp.y + sinf(heading + spread) * reach};
        DrawTriangle(lamp, b, a, Fade(FILM_GOLD, power * .016f));
    }
    float breath = 1.0f + .05f * sinf(t * 1.9f);
    DrawCircleGradient((int)lamp.x, (int)lamp.y, 85.0f * breath, Fade(FILM_GOLD, power * .35f), Fade(FILM_GOLD, 0));
    DrawCircleGradient((int)lamp.x, (int)lamp.y, 33.0f, Fade(PAPER, power * .78f), Fade(FILM_GOLD, 0));
    DrawCircleGradient((int)lamp.x, (int)lamp.y, 9.0f, Fade(WHITE, power), Fade(FILM_GOLD, 0));
    EndBlendMode();
}

static void Actor(Texture2D texture, int columns, int rows, int row, float t,
                  Vector2 feet, float height, bool moving, float opacity) {
    if (!texture.id) return;
    float cellW = (float)texture.width / (float)columns;
    float cellH = (float)texture.height / (float)rows;
    int frame = moving ? (int)(t * (rows > 1 ? 7.0f : 4.4f)) % columns : 0;
    float width = height * cellW / cellH;
    float bob = moving ? fabsf(sinf(t * 11.0f)) * .75f : 0.0f;
    if (rows == 1) bob = sinf(t * 3.4f) * 1.4f;
    DrawEllipse((int)feet.x, (int)(feet.y - 1), width * .24f, height * .05f, Fade(INK, opacity * .30f));
    DrawTexturePro(texture, (Rectangle){frame * cellW, row * cellH, cellW, cellH},
        (Rectangle){roundf(feet.x - width * .5f), roundf(feet.y - height - bob), width, height},
        (Vector2){0, 0}, 0, Fade(WHITE, opacity));
}

static Vector2 WalkPoint(Rectangle frame, float progress, int behind) {
    float p = Clamp(progress - (float)behind * .09f, -.27f, 1.0f);
    /* Follow the hand-painted path, not an unrelated straight screen line. */
    float x = Lerp(.24f, .69f, p);
    float y = Lerp(.88f, .62f, p) - sinf(p * PI) * .02f;
    return InFrame(frame, x, y);
}

static void Approach(const Game *g, const EndingCast *cast, float t, float alpha) {
    float move = Ease(.3f, 7.4f, t);
    float zoom = 1.025f + .065f * Ease(0, 14, t);
    Rectangle frame = FrameRect(film[0], zoom, (Vector2){-12.0f * move, 9.0f * move});
    Background(film[0], frame, alpha, WHITE);
    Atmosphere(t, alpha * (1.0f - Ease(8, 13, t)), false);
    float actorAlpha = alpha * (1.0f - Ease(10.5f, 13.2f, t));
    float height = Lerp(84, 63, move);
    /* Reverse party order preserves the painter's depth order on the path. */
    if (g->spark.owned) Actor(cast->companions[2], 4, 1, 0, t + 2,
        WalkPoint(frame, move, 3), height * .66f, true, actorAlpha);
    if (g->splash.owned) Actor(cast->companions[1], 4, 1, 0, t + 1,
        WalkPoint(frame, move, 2), height * .66f, true, actorAlpha);
    if (g->lumi.owned) Actor(cast->companions[0], 4, 1, 0, t,
        WalkPoint(frame, move, 1), height * .62f, true, actorAlpha);
    Actor(cast->heroine, 4, 4, t < 6.4f ? 2 : 3, t,
        WalkPoint(frame, move, 0), height, t < 7.4f, actorAlpha);
    Vector2 lamp = InFrame(frame, .701f, .205f);
    float power = Ease(7.6f, 10.2f, t);
    if (t > 6.4f && t < 11.4f) {
        for (int i = 0; i < 24; ++i) {
            float travel = Clamp((t - 6.4f - i * .09f) / 2.7f, 0.0f, 1.0f);
            Vector2 from = InFrame(frame, .65f + .025f * sinf((float)i), .63f);
            Vector2 p = Vector2Lerp(from, lamp, travel);
            p.x += sinf(travel * PI) * sinf(i * 2.7f + t) * 37.0f;
            float glow = sinf(travel * PI) * alpha;
            DrawCircleGradient((int)p.x, (int)p.y, 6.5f, Fade(i % 3 ? FILM_GOLD : WHITE, glow * .8f), Fade(FILM_GOLD, 0));
        }
    }
    Lantern(lamp, t, power * alpha, -.12f + .19f * sinf((t - 9.0f) * .42f), 1350.0f);
    /* A gentle exposure lift lets the ignition read without a full-screen flash. */
    float ignition = Window(t, 9.0f, 11.7f);
    DrawRectangle(0, 0, SW, SH, Fade((Color){255, 228, 179, 255}, ignition * .06f * alpha));
    Motes(t, alpha * .65f, false);
}

static void Sea(float t, Rectangle frame, float alpha, float left) {
    BeginBlendMode(BLEND_ADDITIVE);
    for (int i = 0; i < 46; ++i) {
        float x = left + (float)((i * 37) % 61) / 61.0f * (.99f - left);
        float y = .53f + (float)((i * 13) % 31) / 100.0f;
        Vector2 p = InFrame(frame, x + sinf(t * .28f + i) * .005f, y);
        float shine = powf(.5f + .5f * sinf(t * 1.2f + i * 1.7f), 3.0f);
        float width = (3.0f + y * 14.0f) * shine;
        DrawLineEx(p, (Vector2){p.x + width, p.y}, 1.0f, Fade(FILM_GOLD, alpha * shine * .24f));
    }
    EndBlendMode();
}

static void Dawn(float t, float alpha) {
    float local = Clamp((t - 13.0f) / 9.0f, 0.0f, 1.0f);
    Rectangle frame = FrameRect(film[1], Lerp(1.065f, 1.018f, local), (Vector2){Lerp(15, -9, local), 0});
    Background(film[1], frame, alpha, WHITE);
    Vector2 lamp = InFrame(frame, .254f, .304f);
    Lantern(lamp, t, alpha * .75f, .015f + .13f * sinf(t * .32f), 1250.0f);
    Sea(t, frame, alpha, .39f);
    Atmosphere(t, (1.0f - local) * alpha * .7f, true);
    Motes(t, alpha * .26f, true);
}

static void Homecoming(float t, float alpha) {
    float local = Ease(20, ENDING_DURATION, t);
    Rectangle frame = FrameRect(film[2], Lerp(1.02f, 1.055f, local), (Vector2){-9 * local, 5 * local});
    Background(film[2], frame, alpha, WHITE);
    Sea(t, frame, alpha * .6f, .73f);
    Lantern(InFrame(frame, .874f, .322f), t, alpha * .36f, -.09f, 400);
    Motes(t, alpha * .8f, true);
}

static void Caption(Font font, const char *label, const char *text, float opacity) {
    if (opacity <= .001f) return;
    /* The soft scrim is strongest behind the text, then dissolves back into
     * the illustration instead of leaving a hard subtitle box. */
    DrawRectangleGradientV(0, 498, SW, 128, Fade(INK, 0), Fade(INK, opacity * .78f));
    DrawRectangleGradientV(0, 626, SW, 48, Fade(INK, opacity * .78f), Fade(INK, 0));
    TextCenter(font, label, 555, 21, Fade(FILM_GOLD, opacity));
    float halfLabel = TextWidth(font, label, 21) * .5f;
    DrawLineEx((Vector2){SW * .5f - halfLabel - 52, 566},
        (Vector2){SW * .5f - halfLabel - 18, 566}, 1, Fade(FILM_GOLD, opacity * .60f));
    DrawLineEx((Vector2){SW * .5f + halfLabel + 18, 566},
        (Vector2){SW * .5f + halfLabel + 52, 566}, 1, Fade(FILM_GOLD, opacity * .60f));
    TextCenter(font, text, 590, 31, Fade(PAPER, opacity));
}

void EndingInit(void) {
    static const char *paths[3] = {
        "assets/cinematics/ending_v9/01_lighthouse_path.png",
        "assets/cinematics/ending_v9/02_dawn_sea.png",
        "assets/cinematics/ending_v9/03_homecoming.png"
    };
    for (int i = 0; i < 3; ++i) {
        if (FileExists(paths[i])) film[i] = LoadTexture(paths[i]);
        if (film[i].id) {
            SetTextureFilter(film[i], TEXTURE_FILTER_BILINEAR);
            TraceLog(LOG_INFO, "Mist Isle: ending shot %d loaded (%dx%d): %s", i + 1,
                film[i].width, film[i].height, paths[i]);
        } else TraceLog(LOG_ERROR, "Mist Isle: missing cinematic shot: %s", paths[i]);
    }
}

void EndingClose(void) {
    for (int i = 0; i < 3; ++i) {
        if (film[i].id) UnloadTexture(film[i]);
        film[i] = (Texture2D){0};
    }
}

void EndingDraw(const Game *g, Font font, const EndingCast *cast) {
    float t = fmaxf(0, g->endingTime);
    ClearBackground(INK);
    float sea = Ease(12.6f, 14.0f, t);
    float home = Ease(20.0f, 21.5f, t);
    if (sea < 1.0f) Approach(g, cast, t, 1.0f);
    if (sea > 0.0f && home < 1.0f) Dawn(t, sea);
    if (home > 0.0f) Homecoming(t, home);
    /* Letterboxing is reserved for the performance; the last frame opens up. */
    float open = Ease(26.0f, ENDING_DURATION, t);
    int bar = (int)Lerp(48, 0, open);
    DrawRectangle(0, 0, SW, bar, INK);
    DrawRectangle(0, SH - bar, SW, bar, INK);
    Caption(font, "归途", "这一次，我们一起走向灯火。", Window(t, 1.0f, 6.8f));
    Caption(font, "点亮", "微小的光，也能唤醒沉睡的灯塔。", Window(t, 7.1f, 13.2f));
    Caption(font, "破晓", "海雾渐散，远方终于有了方向。", Window(t, 14.0f, 20.2f));
    Caption(font, "同行", "而回家的路，从此不再孤单。", Window(t, 21.5f, 27.0f));
    if (t < ENDING_DURATION) {
        if (t > 1.0f) {
            const char *skip = "Esc  跳过演出";
            TextAt(font, skip, (Vector2){SW - 56 - TextWidth(font, skip, 18), 682},
                18, Fade(PAPER, .96f * (1.0f - open)));
        }
        float progress = Clamp(t / ENDING_DURATION, 0, 1);
        DrawRectangle(56, 707, SW - 112, 1, Fade(PAPER, .24f * (1.0f - open)));
        DrawRectangle(56, 707, (int)((SW - 112) * progress), 1, Fade(FILM_GOLD, .86f * (1.0f - open)));
    } else {
        float a = Ease(ENDING_DURATION, ENDING_DURATION + 1.0f, t);
        DrawRectangleGradientV(0, 0, SW, 285, Fade(INK, .68f * a), Fade(INK, 0));
        TextCenter(font, "雾 屿 回 响", 65, 50, Fade(PAPER, a));
        TextCenter(font, "第一章 · 失落的灯塔", 134, 24, Fade(FILM_GOLD, a));
        TextCenter(font, "旅程完成", 176, 20, Fade(PAPER, a));
        DrawLineEx((Vector2){SW * .5f - 48, 211}, (Vector2){SW * .5f + 48, 211},
            1, Fade(FILM_GOLD, .65f * a));
        DrawRectangleGradientV(0, 550, SW, 170, Fade(INK, 0), Fade(INK, .93f * a));
        TextCenter(font, "灯火会记得，每一个同行的名字。", 609, 25, Fade(PAPER, a));
        EndingControls(font, g->previewOnly, a);
    }
    float fade = 1.0f - Ease(0.0f, .85f, t);
    if (fade > 0) DrawRectangle(0, 0, SW, SH, Fade(BLACK, fade));
}
