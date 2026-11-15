/* Drive the production GameUpdate with deterministic input and storage. The
 * renderer/audio tests separately verify black pixels and actual stream silence. */
#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include <math.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static bool pressed[512],down[512],mousePressed,mouseDown;
static SaveData storedSave;
static bool hasSave;
static int checks,soundCalls[AUDIO_CUE_COUNT],movementResets,saveWrites;
#define CHECK(condition,message) do { checks++; if(!(condition)){fprintf(stderr,"FAIL line %d: %s\n",__LINE__,message);exit(1);} } while(0)
#define NEAR(a,b) (fabsf((a)-(b))<0.0001f)

bool GameInputPressed(int key) { return key>=0&&key<512&&pressed[key]; }
bool GameInputDown(int key) { return key>=0&&key<512&&down[key]; }
bool IsMouseButtonPressed(int key) { return key==MOUSE_BUTTON_LEFT&&mousePressed; }
bool IsMouseButtonDown(int key) { return key==MOUSE_BUTTON_LEFT&&mouseDown; }
Vector2 GetMousePosition(void) { return (Vector2){SW*.5f,SH*.5f}; }
int GetScreenWidth(void) { return SW; }
int GetScreenHeight(void) { return SH; }
int GetRenderWidth(void) { return SW; }
int GetRenderHeight(void) { return SH; }
int GetRandomValue(int min,int max) { (void)max;return min; }
bool CheckCollisionPointRec(Vector2 point,Rectangle rec) {
    return point.x>=rec.x&&point.x<=rec.x+rec.width&&point.y>=rec.y&&point.y<=rec.y+rec.height;
}
const char *TextFormat(const char *format,...) {
    static char buffer[1024];va_list args;va_start(args,format);
    vsnprintf(buffer,sizeof(buffer),format,args);va_end(args);return buffer;
}
bool SaveFileData(const char *path,void *data,int size) {
    (void)path;CHECK(size==(int)sizeof(storedSave),"save format stays compatible");
    memcpy(&storedSave,data,sizeof(storedSave));hasSave=true;saveWrites++;return true;
}
unsigned char *LoadFileData(const char *path,int *size) {
    (void)path;if(!hasSave){*size=0;return NULL;}
    unsigned char *data=malloc(sizeof(storedSave));CHECK(data!=NULL,"save allocation");
    memcpy(data,&storedSave,sizeof(storedSave));*size=(int)sizeof(storedSave);return data;
}
void UnloadFileData(unsigned char *data) { free(data); }
void AudioPlay(AudioCue cue) { CHECK(cue>=0&&cue<AUDIO_CUE_COUNT,"valid sound cue");soundCalls[cue]++; }
void AudioResetMovement(void) { movementResets++; }
void AudioMovement(float distance,bool blocked,int surface,float dt) {
    (void)distance;(void)blocked;(void)surface;(void)dt;
}
static void Release(void) {
    memset(pressed,0,sizeof(pressed));memset(down,0,sizeof(down));
    mousePressed=false;mouseDown=false;
}
static void Confirm(Game *g) {
    Release();pressed[KEY_ENTER]=down[KEY_ENTER]=true;GameUpdate(g,.016f);Release();
}
static void Press(Game *g,int key,float dt) {
    Release();pressed[key]=down[key]=true;GameUpdate(g,dt);Release();
}
static void CheckEndingControls(const Game *completed) {
    /* Use a real victory state, and drive GameUpdate just like the normal
     * frame loop. No assumptions about the source's branch layout. */
    Game film=*completed;
    const int ignoredKeys[]={KEY_ENTER,KEY_R,KEY_P};
    for(int i=0;i<3;i++) {
        film.endingTime=3.0f;
        Press(&film,ignoredKeys[i],.25f);
        CHECK(film.scene==SCENE_END&&NEAR(film.endingTime,3.25f),
              "confirm, restart and replay cannot interrupt an unfinished film");
        CHECK(film.bossDefeated&&film.lastSeed==completed->lastSeed,
              "early ending input preserves the completed journey");
    }
    film.endingTime=0.0f;
    Press(&film,KEY_ESCAPE,.5f);
    CHECK(film.scene==SCENE_END&&NEAR(film.endingTime,.5f),
          "Escape is ignored during the first second of the film");
    GameUpdate(&film,-.25f);
    CHECK(NEAR(film.endingTime,.5f),"a negative frame delta never rewinds the film");
    Press(&film,KEY_ESCAPE,.5f);
    CHECK(film.scene==SCENE_END&&NEAR(film.endingTime,28.0f),
          "Escape at one second skips to the completed frame without returning to title");
    GameUpdate(&film,.5f);
    CHECK(film.scene==SCENE_END&&film.endingTime>28.0f,
          "the completed frame remains visible until another deliberate action");
    Press(&film,KEY_P,.016f);
    CHECK(film.scene==SCENE_END&&NEAR(film.endingTime,0),
          "completed-film replay resets its clock while retaining the ending scene");
    CHECK(film.bossDefeated&&film.player.crystals==completed->player.crystals&&
          film.player.shells==completed->player.shells&&film.lumi.xp==completed->lumi.xp,
          "film replay retains progress and never grants duplicate victory rewards");

    GameUpdate(&film,27.75f);
    CHECK(film.scene==SCENE_END&&film.endingTime<28.0f,
          "film remains active immediately before its scheduled finish");
    GameUpdate(&film,.25f);
    CHECK(film.scene==SCENE_END&&NEAR(film.endingTime,28.0f),
          "natural playback reaches the same completed frame as skipping");
    Game finished=film;
    Press(&film,KEY_ENTER,.016f);
    CHECK(film.scene==SCENE_TITLE&&NEAR(film.endingTime,0),
          "Enter returns a completed film to the title");
    film=finished;
    Press(&film,KEY_ESCAPE,.016f);
    CHECK(film.scene==SCENE_TITLE,"a second Escape returns the completed film to title");
    film=finished;
    Press(&film,KEY_R,.016f);
    CHECK(film.scene==SCENE_DIALOGUE&&film.area==AREA_CAMP&&film.dialogueCount>0,
          "restart begins the new journey's opening dialogue at the camp");
    CHECK(film.lastSeed==completed->lastSeed+1&&!film.bossDefeated&&
          !film.shrineSolved&&!film.lumi.owned&&film.player.crystals==0,
          "restart clears chapter progress and selects the next journey seed");

    /* The preview is deliberately isolated even if an ordinary save already
     * exists, and must never start a journey with its synthetic victory data. */
    Game preview=finished;
    preview.previewOnly=true;
    SaveData beforePreview=storedSave;
    int writesBeforePreview=saveWrites;
    preview.player.crystals+=100;
    GameSave(&preview);
    CHECK(saveWrites==writesBeforePreview&&memcmp(&storedSave,&beforePreview,sizeof(storedSave))==0,
          "preview saves never write or replace the existing journey save");
    Press(&preview,KEY_R,.016f);
    CHECK(preview.scene==SCENE_TITLE&&preview.lastSeed==finished.lastSeed&&preview.bossDefeated,
          "restart from preview exits to title without beginning a new journey");
    CHECK(saveWrites==writesBeforePreview,
          "leaving the preview never writes its synthetic progress to disk");
}
static Game BossFight(void) {
    Game g;GameReset(&g,42);g.area=AREA_LIGHTHOUSE;g.scene=SCENE_BATTLE;
    g.player.pos=(Vector2){8,7};g.player.vel=(Vector2){2,3};
    g.lumi.owned=true;
    g.enemyCount=1;g.battleEnemy=0;g.battleChoice=0;
    g.enemies[0]=(Enemy){.pos={9,7},.type=ENEMY_BOSS,.hp=1,.maxHp=150,.alive=true};
    BattleInit(&g);
    return g;
}

int main(void) {
    Game g=BossFight();g.shrineSolved=true;
    Confirm(&g);
    CHECK(g.scene==SCENE_BATTLE&&NEAR(g.enemies[0].hp,1),"confirm does not deal damage before impact");
    for(int frame=0;frame<300&&g.scene==SCENE_BATTLE;frame++)GameUpdate(&g,1.0f/60.0f);
    CHECK(g.scene==SCENE_POST_BATTLE_BLACKOUT,"Boss defeat enters blackout before any dialogue");
    CHECK(g.bossDefeated&&!g.enemies[0].alive,"Boss stays defeated");
    CHECK(g.player.crystals==3&&g.player.shells==2&&g.lumi.xp==20,"victory rewards are retained once");
    CHECK(hasSave&&storedSave.bossDefeated&&storedSave.crystals==3,"victory is saved before transition");
    CHECK(g.dialogueCount==0&&NEAR(g.postBattleBlackoutTime,0),"blackout starts at zero with no text");
    CHECK(!g.journalOpen&&!g.inventoryOpen&&!g.mapOpen&&NEAR(g.toastTimer,0),"blackout closes UI and notices");
    CHECK(NEAR(g.player.vel.x,0)&&NEAR(g.player.vel.y,0)&&movementResets>0,"movement stops at victory");
    CHECK(soundCalls[AUDIO_VICTORY]==0&&soundCalls[AUDIO_DIALOGUE]==0,"no victory sting or early dialogue sound");

    /* Confirm, Escape, restart, save/load and menu presses cannot skip the beat. */
    for(int i=0;i<512;i++)pressed[i]=down[i]=true;
    mousePressed=mouseDown=true;
    GameUpdate(&g,.5f);GameUpdate(&g,.499f);
    CHECK(g.scene==SCENE_POST_BATTLE_BLACKOUT,"input cannot skip before one second");
    CHECK(g.dialogueCount==0&&soundCalls[AUDIO_DIALOGUE]==0,"silent beat contains no dialogue");
    GameUpdate(&g,.001f);
    CHECK(g.scene==SCENE_DIALOGUE&&g.dialogueStartsEnding,"one second opens ending dialogue");
    CHECK(g.dialogueCount==4&&g.dialoguePage==0,"all four post-battle lines are available");
    CHECK(g.dialoguePortrait[0]==2&&g.dialoguePortrait[1]==0&&g.dialoguePortrait[2]==3,"speakers use matching portraits");
    CHECK(g.dialogueInputBlocked,"new dialogue requires confirmation release");
    GameUpdate(&g,.2f);
    CHECK(g.dialoguePage==0,"held kill key/click cannot dismiss first line");
    Release();GameUpdate(&g,.016f);
    CHECK(!g.dialogueInputBlocked&&g.dialoguePage==0,"release arms next deliberate input");
    for(int page=1;page<4;page++) {
        Confirm(&g);
        CHECK(g.scene==SCENE_DIALOGUE&&g.dialoguePage==page,"post-battle lines progress normally");
    }
    Confirm(&g);
    CHECK(g.scene==SCENE_END&&NEAR(g.endingTime,0),"final line starts animation automatically");
    CHECK(!g.dialogueStartsEnding,"ending action cannot run twice");
    GameUpdate(&g,.25f);
    CHECK(g.scene==SCENE_END&&NEAR(g.endingTime,.25f),"ending animation keeps advancing");
    CHECK(g.player.crystals==3&&g.player.shells==2&&g.lumi.xp==20,"transition never duplicates rewards");

    CheckEndingControls(&g);

    /* A save made at victory still skips the boss and can replay the ending. */
    Game loaded;GameReset(&loaded,99);CHECK(GameLoad(&loaded),"victory save can load");
    CHECK(loaded.scene==SCENE_PLAY&&loaded.bossDefeated&&loaded.enemyCount==0,"loaded victory cannot restart Boss");
    loaded.player.pos=loaded.world.shrine;
    Release();pressed[KEY_E]=down[KEY_E]=true;GameUpdate(&loaded,.016f);Release();
    CHECK(loaded.scene==SCENE_END,"altar can recover/replay a completed chapter");

    Game ordinary=BossFight();ordinary.area=AREA_GROVE;ordinary.enemies[0].type=ENEMY_MOTE;
    Confirm(&ordinary);
    for(int frame=0;frame<300&&ordinary.scene==SCENE_BATTLE;frame++)GameUpdate(&ordinary,1.0f/60.0f);
    CHECK(ordinary.scene==SCENE_PLAY&&!ordinary.bossDefeated&&ordinary.groveMoteDefeated,
          "ordinary fights retain their existing exploration flow");
    GameReset(&g,43);
    CHECK(!g.dialogueStartsEnding&&!g.dialogueInputBlocked&&NEAR(g.postBattleBlackoutTime,0),"new game clears transition state");
    printf("Post-battle gameplay: %d checks passed.\n",checks);return 0;
}
