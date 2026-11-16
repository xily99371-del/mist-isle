#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include "viewport.h"
#include <math.h>
#include <stdio.h>
#include <string.h>

static void Notice(TitleMenu *t,const char *text) {
    snprintf(t->notice,sizeof(t->notice),"%s",text);t->noticeTime=4.0f;
}
static void OpenPage(TitleMenu *t,TitlePage page,int selected) {
    t->page=page;t->selected=selected;t->hover=-1;
}
static bool Inside(Vector2 p,Rectangle r) {
    return p.x>=r.x&&p.x<r.x+r.width&&p.y>=r.y&&p.y<r.y+r.height;
}
static int Hit(TitlePage page,Vector2 p) {
    int count=page==TITLE_MAIN||page==TITLE_SETTINGS?4:2;
    for(int i=0;i<count;i++) {
        Rectangle r=page==TITLE_MAIN?TitleButtonRect(i):
                    page==TITLE_SETTINGS?TitleSettingsRect(i):TitleConfirmRect(i);
        if(Inside(p,r))return i;
    }
    return -1;
}
static bool BackupJourney(TitleMenu *t) {
    if(!FileExists("mist_isle_save.dat"))return true;
    int size=0;unsigned char *bytes=LoadFileData("mist_isle_save.dat",&size);
    if(!bytes||size<=0) {
        if(bytes)UnloadFileData(bytes);
        Notice(t,"旧存档无法备份，未开始新旅程。");return false;
    }
    char name[96];int index;
    for(index=1;index<=9999;index++) {
        snprintf(name,sizeof(name),"mist_isle_save.backup-%04d.dat",index);
        if(!FileExists(name))break;
    }
    bool ok=index<=9999&&SaveFileData(name,bytes,size);
    UnloadFileData(bytes);
    if(!ok)Notice(t,"备份失败，请检查文件夹写入权限。");
    return ok;
}
static void StartJourney(Game *g) {
    if(!BackupJourney(&g->title))return;
    int seed=g->lastSeed+1;
    GameReset(g,seed);GameStart(g);
    g->dialogueInputBlocked=true;
}
static void ContinueJourney(Game *g) {
    if(!FileExists("mist_isle_save.dat")) {
        g->title.hasSave=false;Notice(&g->title,"暂无存档，请先开始冒险。");return;
    }
    if(!GameLoad(g)) {
        Notice(&g->title,g->toast[0]?g->toast:"存档无法读取，请检查存档文件。");
    } else AudioPlay(AUDIO_INTERACT);
}
bool TitleMenuUpdate(Game *g,float dt) {
    TitleMenu *t=&g->title;
    if(!t->initialized) {
        memset(t,0,sizeof(*t));t->initialized=true;t->hover=-1;
        t->lastMouse=GetMousePosition();t->hasSave=FileExists("mist_isle_save.dat");
    }
    g->time+=fmaxf(0,dt);t->noticeTime=fmaxf(0,t->noticeTime-dt);
    Vector2 mouse=GetMousePosition(),point;
    bool mapped=TitleMapMouse(mouse,CurrentGameViewport().destination,&point);
    int hover=mapped?Hit(t->page,point):-1;
    bool clicked=IsMouseButtonPressed(MOUSE_BUTTON_LEFT);
    bool moved=fabsf(mouse.x-t->lastMouse.x)+fabsf(mouse.y-t->lastMouse.y)>.5f;
    t->lastMouse=mouse;t->hover=hover;
    int previous=t->selected,count=t->page==TITLE_MAIN||t->page==TITLE_SETTINGS?4:2;
    if((moved||clicked)&&hover>=0)t->selected=hover;
    bool prev=GameInputPressed(KEY_UP)||GameInputPressed(KEY_W);
    bool next=GameInputPressed(KEY_DOWN)||GameInputPressed(KEY_S);
    if(count==2) {
        prev=prev||GameInputPressed(KEY_LEFT)||GameInputPressed(KEY_A);
        next=next||GameInputPressed(KEY_RIGHT)||GameInputPressed(KEY_D);
    }
    if(prev)t->selected=(t->selected+count-1)%count;
    if(next)t->selected=(t->selected+1)%count;
    if(previous!=t->selected)AudioPlay(AUDIO_DIALOGUE);
    SetMouseCursor(hover>=0?MOUSE_CURSOR_POINTING_HAND:MOUSE_CURSOR_DEFAULT);
    if(GameInputPressed(KEY_ESCAPE)) {
        if(t->page==TITLE_MAIN)OpenPage(t,TITLE_EXIT_CONFIRM,0);
        else OpenPage(t,TITLE_MAIN,0);
        return false;
    }
    /* F9 remains available, but cannot bypass an open confirmation/modal. */
    if(t->page==TITLE_MAIN&&GameInputPressed(KEY_F9)) {
        ContinueJourney(g);return false;
    }
    bool confirm=GameInputPressed(KEY_ENTER)||GameInputPressed(KEY_SPACE)||(clicked&&hover>=0);
    if(!confirm)return false;
    AudioPlay(AUDIO_INTERACT);
    if(t->page==TITLE_NEW_CONFIRM) {
        if(t->selected==1)StartJourney(g);
        else OpenPage(t,TITLE_MAIN,0);
    } else if(t->page==TITLE_EXIT_CONFIRM) {
        if(t->selected==1)return true;
        OpenPage(t,TITLE_MAIN,3);
    } else if(t->page==TITLE_SETTINGS) {
        switch(t->selected) {
            case 0: AudioToggleMute();break;
            case 1: AudioToggleSubtitles();break;
            case 2: ToggleBorderlessWindowed();break;
            default: OpenPage(t,TITLE_MAIN,2);break;
        }
    } else {
        switch(t->selected) {
            case 0:
                t->hasSave=FileExists("mist_isle_save.dat");
                if(t->hasSave)OpenPage(t,TITLE_NEW_CONFIRM,0);
                else StartJourney(g);
                break;
            case 1: ContinueJourney(g);break;
            case 2: OpenPage(t,TITLE_SETTINGS,0);break;
            case 3: OpenPage(t,TITLE_EXIT_CONFIRM,0);break;
        }
    }
    if(g->scene!=SCENE_TITLE)SetMouseCursor(MOUSE_CURSOR_DEFAULT);
    return false;
}
