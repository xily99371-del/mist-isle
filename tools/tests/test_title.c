#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include "viewport.h"
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include <math.h>
#include <stdlib.h>

static int keys[512], clicks, sw=1280,sh=720,rw=1280,rh=720;
static Vector2 mouse={0,0};static bool muted,subtitles=true,borderless;
static bool haveSave,loadOk,writeOk=true,backupExists;
static int starts,loads,writes,checks;
static unsigned char original[12]="SAVE-EXACT";
static char written[96];
static unsigned char copy[12];
#define CHECK(x) do { checks++; if(!(x)){fprintf(stderr,"FAIL line %d: %s\n",__LINE__,#x);exit(1);} }while(0)
bool GameInputPressed(int k){return k>=0&&k<512&&keys[k];}
bool GameInputDown(int k){return GameInputPressed(k);}
int GetScreenWidth(void){return sw;} int GetScreenHeight(void){return sh;}
int GetRenderWidth(void){return rw;} int GetRenderHeight(void){return rh;}
Vector2 GetMousePosition(void){return mouse;}
bool IsMouseButtonPressed(int b){return b==MOUSE_BUTTON_LEFT&&clicks;}
void SetMouseCursor(int c){(void)c;}
void ToggleBorderlessWindowed(void){borderless=!borderless;}
bool FileExists(const char *p) {
    if(strcmp(p,"mist_isle_save.dat")==0)return haveSave;
    return backupExists&&strcmp(p,"mist_isle_save.backup-0001.dat")==0;
}
unsigned char *LoadFileData(const char *p,int *size) {
    (void)p;*size=sizeof original;unsigned char *out=malloc(sizeof original);
    memcpy(out,original,sizeof original);return out;
}
void UnloadFileData(unsigned char *p){free(p);}
bool SaveFileData(const char *p,void *data,int size) {
    writes++;snprintf(written,sizeof written,"%s",p);CHECK(size==(int)sizeof original);
    memcpy(copy,data,sizeof copy);return writeOk;
}
void AudioPlay(AudioCue c){(void)c;}
void AudioToggleMute(void){muted=!muted;}
void AudioToggleSubtitles(void){subtitles=!subtitles;}
void GameReset(Game *g,int seed){memset(g,0,sizeof(*g));g->lastSeed=seed;g->scene=SCENE_TITLE;}
void GameStart(Game *g){starts++;g->scene=SCENE_DIALOGUE;}
bool GameLoad(Game *g){loads++;if(loadOk)g->scene=SCENE_PLAY;else strcpy(g->toast,"invalid save");return loadOk;}
static void Init(Game *g) {
    sw=rw=1280;sh=rh=720;mouse=(Vector2){0,0};clicks=0;
    memset(keys,0,sizeof keys);GameReset(g,10);TitleMenuUpdate(g,.016f);
}
static bool Key(Game *g,int key) {
    memset(keys,0,sizeof keys);keys[key]=1;
    bool result=TitleMenuUpdate(g,.016f);memset(keys,0,sizeof keys);return result;
}
static void Click(Game *g,float x,float y) {
    mouse=(Vector2){x,y};clicks=1;TitleMenuUpdate(g,.016f);clicks=0;
}
int main(void) {
    Game g;Init(&g);CHECK(g.title.selected==0);CHECK(!g.title.hasSave);CHECK(writes==0);
    Click(&g,100,100);CHECK(g.scene==SCENE_TITLE);CHECK(starts==0);
    Key(&g,KEY_UP);CHECK(g.title.selected==3);Key(&g,KEY_DOWN);CHECK(g.title.selected==0);
    Key(&g,KEY_DOWN);Key(&g,KEY_ENTER);CHECK(loads==0);CHECK(g.title.noticeTime>0);
    CHECK(writes==0);Key(&g,KEY_F9);CHECK(loads==0);
    Key(&g,KEY_DOWN);Key(&g,KEY_ENTER);CHECK(g.title.page==TITLE_SETTINGS);
    Key(&g,KEY_ENTER);CHECK(muted);Key(&g,KEY_DOWN);Key(&g,KEY_ENTER);CHECK(!subtitles);
    Key(&g,KEY_DOWN);Key(&g,KEY_ENTER);CHECK(borderless);
    Key(&g,KEY_F9);CHECK(loads==0);CHECK(g.title.page==TITLE_SETTINGS);
    Key(&g,KEY_ESCAPE);CHECK(g.title.page==TITLE_MAIN);
    Key(&g,KEY_ESCAPE);CHECK(g.title.page==TITLE_EXIT_CONFIRM&&g.title.selected==0);
    CHECK(!Key(&g,KEY_ENTER));CHECK(g.title.page==TITLE_MAIN);
    Key(&g,KEY_ESCAPE);Key(&g,KEY_RIGHT);CHECK(Key(&g,KEY_ENTER));
    Init(&g);Key(&g,KEY_ENTER);CHECK(starts==1&&g.scene==SCENE_DIALOGUE);CHECK(g.dialogueInputBlocked);
    CHECK(writes==0);
    haveSave=true;Init(&g);CHECK(g.title.hasSave);Key(&g,KEY_ENTER);
    CHECK(g.title.page==TITLE_NEW_CONFIRM&&g.title.selected==0);CHECK(starts==1&&writes==0);
    Key(&g,KEY_ENTER);CHECK(g.title.page==TITLE_MAIN);CHECK(starts==1);
    backupExists=true;Key(&g,KEY_ENTER);Key(&g,KEY_RIGHT);Key(&g,KEY_ENTER);
    CHECK(starts==2&&writes==1);CHECK(strcmp(written,"mist_isle_save.backup-0002.dat")==0);
    CHECK(memcmp(copy,original,sizeof copy)==0);
    writeOk=false;Init(&g);Key(&g,KEY_ENTER);Key(&g,KEY_RIGHT);Key(&g,KEY_ENTER);
    CHECK(starts==2&&g.scene==SCENE_TITLE);CHECK(g.title.noticeTime>0);
    writeOk=true;loadOk=false;Init(&g);Key(&g,KEY_F9);
    CHECK(loads==1&&g.scene==SCENE_TITLE);CHECK(strcmp(g.title.notice,"invalid save")==0);
    loadOk=true;Key(&g,KEY_F9);CHECK(loads==2&&g.scene==SCENE_PLAY);
    haveSave=false;
    int sizes[][4]={{1280,720,1280,720},{1600,900,1600,900},{1920,1080,1920,1080},
        {1000,1000,1000,1000},{2560,1080,2560,1080},{1280,720,2560,1440},{960,540,960,540}};
    for(int size=0;size<7;size++)for(int item=0;item<4;item++) {
        Init(&g);sw=sizes[size][0];sh=sizes[size][1];rw=sizes[size][2];rh=sizes[size][3];
        GameViewport v=CurrentGameViewport();Rectangle r=TitleButtonRect(item);
        Vector2 p={v.destination.x+(r.x+r.width*.5f)*v.destination.width/1280,
                   v.destination.y+(r.y+r.height*.5f)*v.destination.height/720},mapped;
        CHECK(TitleMapMouse(p,v.destination,&mapped));CHECK(fabsf(mapped.x-r.x-r.width*.5f)<.01f);
        Click(&g,p.x,p.y);
        CHECK(item==0?g.scene==SCENE_DIALOGUE:item==1?g.title.noticeTime>0:
              item==2?g.title.page==TITLE_SETTINGS:g.title.page==TITLE_EXIT_CONFIRM);
    }
    Init(&g);sw=1000;sh=1000;rw=1000;rh=1000;
    Click(&g,500,40);CHECK(g.scene==SCENE_TITLE&&g.title.page==TITLE_MAIN);
    Click(&g,500,960);CHECK(g.scene==SCENE_TITLE&&g.title.page==TITLE_MAIN);
    CHECK(!TitleMapMouse((Vector2){0,0},(Rectangle){0,0,0,0},&mouse));
    /* Idle cursor cannot overwrite keyboard navigation. */
    Init(&g);mouse=(Vector2){680,380};TitleMenuUpdate(&g,.016f);
    CHECK(g.title.selected==1);Key(&g,KEY_DOWN);CHECK(g.title.selected==2);
    TitleMenuUpdate(&g,.016f);CHECK(g.title.selected==2);
    printf("Title menu: %d assertions passed.\n",checks);return 0;
}
