#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include "viewport.h"
#include <math.h>
#include <stdio.h>
#include <string.h>
#include <time.h>

static const char WINDOW_TITLE[] = "雾屿回响 v" MISTISLE_VERSION " - 第一章：失落的灯塔";

static void LocateGameAssets(void) {
    if(FileExists("assets/characters/portraits/heroine_portrait.png"))return;
    const char *directory=GetApplicationDirectory();
    const char *suffixes[]={"","..","../.."};
    for(int i=0;i<3;i++) {
        char candidate[2048],probe[2200];
        snprintf(candidate,sizeof(candidate),"%s%s",directory,suffixes[i]);
        snprintf(probe,sizeof(probe),"%s/assets/characters/portraits/heroine_portrait.png",candidate);
        if(FileExists(probe)){ChangeDirectory(candidate);return;}
    }
}

int main(int argc,char **argv) {
    bool endingPreview=false;
    for(int i=1;i<argc;i++)if(strcmp(argv[i],"--ending-preview")==0)endingPreview=true;
    LocateGameAssets();
#if defined(MISTISLE_PI_PROFILE)
    // Raspberry Pi 5: 720p logical canvas, no MSAA, no forced maximization.
    SetConfigFlags(FLAG_WINDOW_RESIZABLE|FLAG_VSYNC_HINT);
    InitWindow(1280,720,WINDOW_TITLE);
    SetWindowMinSize(960,540);
#else
    SetConfigFlags(FLAG_WINDOW_RESIZABLE|FLAG_VSYNC_HINT|FLAG_MSAA_4X_HINT);
    InitWindow(1600,900,WINDOW_TITLE);
    SetWindowMinSize(1280,720);
    SetWindowState(FLAG_WINDOW_MAXIMIZED);
#endif
    PlatformInputInit(GetWindowHandle());
    RenderTexture2D canvas={0};
    RenderInit();
    AudioInit();
    SetTargetFPS(60);
    bool showPerfOverlay=false;
    float audioNoticeTime=0.0f;
    const char *audioNotice="";
    SetExitKey(KEY_NULL);
    Game g;
    GameReset(&g,(int)time(NULL));
    if(endingPreview) {
        g.previewOnly=true;g.scene=SCENE_END;g.bossDefeated=true;
        g.lumi.owned=g.splash.owned=g.spark.owned=true;
        g.endingTime=0.0f;
    }
    while(!WindowShouldClose()) {
        PlatformInputUpdate();
        bool focused=IsWindowFocused();
        if(GameInputPressed(KEY_F3))showPerfOverlay=!showPerfOverlay;
        float dt=GetFrameTime();
        if(dt>.05f)dt=.05f;
        if(GameInputPressed(KEY_F6)) {
            AudioToggleMute();audioNoticeTime=2.0f;
            audioNotice=AudioIsMuted()?"BGM + SFX: OFF (F6)":"BGM + SFX: ON (F6)";
        }
        if(GameInputPressed(KEY_F8)) {
            AudioToggleSubtitles();audioNoticeTime=2.0f;
            audioNotice=AudioSubtitlesEnabled()?"Enemy subtitles: ON (F8)":"Enemy subtitles: OFF (F8)";
        }
        if(audioNoticeTime>0.0f)audioNoticeTime-=dt;
        if(!focused) {
            g.player.vel=(Vector2){0,0};
        } else if(g.scene==SCENE_TITLE) {
            g.time+=dt;
            if(GameInputPressed(KEY_F9)) {
                GameLoad(&g);
            } else if(GameInputPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
                int seed=g.lastSeed+1;
                GameReset(&g,seed);
                GameStart(&g);
            }
        } else {
            // SCENE_END must pass through GameUpdate so the lighthouse sequence advances.
            GameUpdate(&g,dt);
        }
        if(endingPreview&&g.scene!=SCENE_END)break;
        /* Keep the same music stream alive across exploration, dialogue and battle. */
        AudioUpdate(&g,dt,focused);
        GameViewport viewport=CurrentGameViewport();
#if defined(MISTISLE_PI_PROFILE)
        int canvasWidth=SW,canvasHeight=SH;
#else
        int canvasWidth=viewport.pixelWidth,canvasHeight=viewport.pixelHeight;
#endif
        if(!canvas.id||canvas.texture.width!=canvasWidth||canvas.texture.height!=canvasHeight) {
            if(canvas.id)UnloadRenderTexture(canvas);
            canvas=LoadRenderTexture(canvasWidth,canvasHeight);
            if(!canvas.id) {
                TraceLog(LOG_ERROR,"Mist Isle: cannot allocate the display canvas");
                break;
            }
            SetTextureFilter(canvas.texture,TEXTURE_FILTER_POINT);
        }
        /* Cache generation owns its own FBO and must finish before the frame
         * canvas is bound. raylib does not support nesting TextureMode calls. */
        RenderPrepareFrame(&g);
        BeginDrawing();
        ClearBackground(g.scene==SCENE_POST_BATTLE_BLACKOUT?BLACK:(Color){7,13,24,255});
        RenderBeginCanvas(canvas);
        ClearBackground((Color){7,13,24,255});
        GameRender(&g);
        EndTextureMode();
        Rectangle source={0,0,(float)canvas.texture.width,-(float)canvas.texture.height};
        DrawTexturePro(canvas.texture,source,viewport.destination,(Vector2){0,0},0,WHITE);
        if(audioNoticeTime>0.0f&&g.scene!=SCENE_POST_BATTLE_BLACKOUT) {
            DrawRectangle(12,GetScreenHeight()-45,304,32,(Color){7,18,29,230});
            DrawText(audioNotice,22,GetScreenHeight()-36,16,(Color){216,235,225,255});
        }
        if(showPerfOverlay&&g.scene!=SCENE_POST_BATTLE_BLACKOUT) {
            DrawRectangle(10,10,190,30,(Color){7,18,29,210});
            DrawText(TextFormat("FPS %d  %.1f ms",GetFPS(),GetFrameTime()*1000.0f),18,17,14,(Color){225,238,224,255});
        }
        EndDrawing();
    }
    AudioClose();
    RenderClose();
    if(canvas.id)UnloadRenderTexture(canvas);
    PlatformInputClose();
    CloseWindow();
    return 0;
}
