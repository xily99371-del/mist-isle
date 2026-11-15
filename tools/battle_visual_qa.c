/* Render actual production battle frames with deterministic input. Audio is
 * stubbed only here; live gameplay keeps the normal audio.c streaming loop. */
#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
static bool guard;
bool GameInputPressed(int key){return key==KEY_SPACE&&guard;}
bool GameInputDown(int key){(void)key;return false;}
void AudioPlay(AudioCue c){(void)c;}
void AudioMovement(float d,bool b,int s,float dt){(void)d;(void)b;(void)s;(void)dt;}
void AudioResetMovement(void){}
bool AudioSubtitlesEnabled(void){return true;}
bool AudioBossTrackActive(void){return true;}
float AudioGetMusicTime(void){return 8;}
float AudioGetBossEnergy(void){return .6f;}
bool AudioGetSubtitle(AudioSubtitle *s){
    /* Explicit QA caption, not a substituted song lyric. */
    *s=(AudioSubtitle){"Battle preview - impact timing","战斗演出 · BGM 持续播放",0,12,.55f,.6f};return true;
}
int main(int argc,char **argv){
    const char *dir=argc>1?argv[1]:"build/battle-frames";
    SetConfigFlags(FLAG_WINDOW_HIDDEN);
    InitWindow(SW,SH,"Mist Isle battle render QA");
    if(!IsWindowReady())return 2;
    RenderInit();RenderTexture2D canvas=LoadRenderTexture(SW,SH);
    Game g;GameStartBattlePreview(&g);
    g.lumi.hp=61;g.splash.hp=45;g.spark.hp=67;BattleInit(&g);
    int action=0,frame=0;float rest=0;
    for(int tick=0;tick<630;tick++) {
        guard=false;
        if(g.battle.phase==BATTLE_CHOOSE){
            rest+=1.0f/60;
            if(rest>.55f&&action<3){BattleStartAction(&g,action==0?1:action==1?2:0);action++;rest=0;}
        }
        if(g.battle.phase==BATTLE_ENEMY&&!g.battle.guardAttempted&&g.battle.clock>g.battle.impactAt-.14f)guard=true;
        GameUpdate(&g,1.0f/60);
        BeginDrawing();ClearBackground(BLACK);RenderBeginCanvas(canvas);ClearBackground(BLACK);
        GameRender(&g);EndTextureMode();EndDrawing();
        if(tick%3==0){
            Image image=LoadImageFromTexture(canvas.texture);ImageFlipVertical(&image);
            char path[1024];snprintf(path,sizeof(path),"%s/frame-%04d.png",dir,frame++);
            if(!ExportImage(image,path)){UnloadImage(image);return 3;}
            UnloadImage(image);
        }
    }
    UnloadRenderTexture(canvas);RenderClose();CloseWindow();
    printf("Captured %d production-renderer frames.\n",frame);return 0;
}
