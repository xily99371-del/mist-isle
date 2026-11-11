#include "game.h"
#include <time.h>
int main(void) {
    SetConfigFlags(FLAG_VSYNC_HINT|FLAG_MSAA_4X_HINT);
    InitWindow(SW,SH,"雾屿回响 - 第一章：失落的灯塔");
    SetTargetFPS(60); SetExitKey(KEY_NULL);
    Game g; GameReset(&g,(int)time(NULL));
    while(!WindowShouldClose()) {
        float dt=GetFrameTime(); if(dt>.05f)dt=.05f;
        if(g.scene==SCENE_TITLE) {
            g.time+=dt;
            if(IsKeyPressed(KEY_F9)) { GameLoad(&g); }
            else if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT))GameStart(&g);
        } else if(g.scene==SCENE_END) {
            if(IsKeyPressed(KEY_R))GameReset(&g,g.lastSeed+1);
        } else GameUpdate(&g,dt);
        BeginDrawing(); GameRender(&g); EndDrawing();
    }
    CloseWindow(); return 0;
}