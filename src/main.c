#include "game.h"
#include <math.h>
#include <time.h>
int main(void) {
    SetConfigFlags(FLAG_WINDOW_RESIZABLE|FLAG_VSYNC_HINT|FLAG_MSAA_4X_HINT);
    InitWindow(1600,900,"雾屿回响 - 第一章：失落的灯塔");
    SetWindowMinSize(1280,720);
    SetWindowState(FLAG_WINDOW_MAXIMIZED);
    RenderTexture2D canvas=LoadRenderTexture(SW,SH);
    SetTextureFilter(canvas.texture,TEXTURE_FILTER_POINT);
    RenderInit();
    SetTargetFPS(60);
    SetExitKey(KEY_NULL);
    Game g;
    GameReset(&g,(int)time(NULL));
    while(!WindowShouldClose()) {
        float dt=GetFrameTime();
        if(dt>.05f)dt=.05f;
        if(g.scene==SCENE_TITLE) {
            g.time+=dt;
            if(IsKeyPressed(KEY_F9)) {
                GameLoad(&g);
            } else if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
                int seed=g.lastSeed+1;
                GameReset(&g,seed);
                GameStart(&g);
            }
        } else {
            // SCENE_END must pass through GameUpdate so the lighthouse sequence advances.
            GameUpdate(&g,dt);
        }
        BeginDrawing();
        ClearBackground((Color){7,13,24,255});
        BeginTextureMode(canvas);
        ClearBackground((Color){7,13,24,255});
        GameRender(&g);
        EndTextureMode();
        float sx=(float)GetScreenWidth()/(float)SW;
        float sy=(float)GetScreenHeight()/(float)SH;
        float scale=sx<sy?sx:sy;
        float drawW=(float)SW*scale,drawH=(float)SH*scale;
        Rectangle source={0,0,(float)SW,-(float)SH};
        Rectangle destination={
            ((float)GetScreenWidth()-drawW)*.5f,
            ((float)GetScreenHeight()-drawH)*.5f,
            drawW,drawH
        };
        DrawTexturePro(canvas.texture,source,destination,(Vector2){0,0},0,WHITE);
        EndDrawing();
    }
    RenderClose();
    UnloadRenderTexture(canvas);
    CloseWindow();
    return 0;
}
