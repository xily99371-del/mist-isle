/* Actual production renderer + selected cover. No concept/mockup painting. */
#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include <stdio.h>
#include <string.h>
bool GameInputPressed(int key){(void)key;return false;}
bool GameInputDown(int key){(void)key;return false;}
void AudioPlay(AudioCue c){(void)c;}
void AudioMovement(float d,bool b,int s,float dt){(void)d;(void)b;(void)s;(void)dt;}
void AudioResetMovement(void){}
bool AudioIsMuted(void){return false;}
bool AudioSubtitlesEnabled(void){return true;}
bool AudioBossTrackActive(void){return false;}
float AudioGetMusicTime(void){return 0;}
float AudioGetBossEnergy(void){return 0;}
bool AudioGetSubtitle(AudioSubtitle *s){(void)s;return false;}
int main(int argc,char **argv) {
    const char *dir=argc>1?argv[1]:"build/title-frames";
    SetConfigFlags(FLAG_WINDOW_HIDDEN);InitWindow(1280,720,"Title cover QA");
    if(!IsWindowReady())return 2;
    RenderInit();RenderTexture2D canvas=LoadRenderTexture(1600,900);
    Game g;GameReset(&g,42);g.time=3;g.title.initialized=true;g.title.hover=-1;
    const char *names[]={"title-start","title-continue","title-settings","title-new-confirm","title-exit","title-no-save"};
    for(int i=0;i<6;i++) {
        g.title.page=i==2?TITLE_SETTINGS:i==3?TITLE_NEW_CONFIRM:i==4?TITLE_EXIT_CONFIRM:TITLE_MAIN;
        g.title.hasSave=i!=5;g.title.selected=i==1||i==5?1:0;
        BeginDrawing();RenderBeginCanvas(canvas);ClearBackground(BLACK);GameRender(&g);
        EndTextureMode();EndDrawing();
        Image image=LoadImageFromTexture(canvas.texture);ImageFlipVertical(&image);
        char path[1024];snprintf(path,sizeof path,"%s/%s.png",dir,names[i]);
        bool ok=ExportImage(image,path);UnloadImage(image);if(!ok)return 3;
    }
    UnloadRenderTexture(canvas);RenderClose();CloseWindow();return 0;
}
