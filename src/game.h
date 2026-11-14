#ifndef MIST_ISLE_GAME_H
#define MIST_ISLE_GAME_H
#include "raylib.h"
#include "raymath.h"
#include <stdbool.h>
#define SW 1280
#define SH 720
#define MAPW 18
#define MAPH 12
#define TILE_SIZE 56.0f
#define WORLD_PIXEL_W (MAPW*TILE_SIZE)
#define WORLD_PIXEL_H (MAPH*TILE_SIZE)
#define MAX_ENEMIES 8
#if defined(MISTISLE_PI_PROFILE)
#define MAX_PARTICLES 160
#else
#define MAX_PARTICLES 300
#endif
#define MAX_PICKUPS 32
#define MAX_MAP_OBJECTS 64
#define PI2 6.28318530718f
typedef enum { SCENE_TITLE, SCENE_PLAY, SCENE_DIALOGUE, SCENE_BATTLE, SCENE_END } Scene;
typedef enum { AREA_CAMP, AREA_GROVE, AREA_RUINS, AREA_LIGHTHOUSE } Area;
typedef enum { ENEMY_MOTE, ENEMY_GUARD, ENEMY_BOSS } EnemyType;
typedef enum { SKILL_GLOW, SKILL_SPLASH, SKILL_SPARK } SkillType;

typedef struct { Vector2 p, v; float life, maxLife, size; Color c; } Particle;
typedef struct {
    int magic, version, seed, area;
    float px, py, hp;
    int berries, shells, crystals;
    int lumi, splash, spark, selected;
    int shrineSolved, bossDefeated, fireflyDiscovered, leaflingFed, groveMoteDefeated;
    int lumiLevel,lumiXp,splashLevel,splashXp,sparkLevel,sparkXp;
    unsigned char pickupTaken[4][MAX_PICKUPS];
} SaveDataV4;
typedef struct {
    int magic, version, seed, area;
    float px, py, hp;
    int berries, shells, crystals;
    int lumi, splash, spark, selected;
    int shrineSolved, bossDefeated, fireflyDiscovered, leaflingFed, groveMoteDefeated;
    int lumiLevel,lumiXp,splashLevel,splashXp,sparkLevel,sparkXp;
    unsigned char pickupTaken[4][MAX_PICKUPS];
    float lumiHp, splashHp, sparkHp;
} SaveData;
typedef struct { Vector2 tile; int kind; bool taken; float phase; } Pickup;
typedef struct {
    Vector2 pos, vel;
    float hp, maxHp, stamina;
    int berries, shells, crystals;
    float dashCd, invuln;
    int facing;
} Player;
typedef struct {
    Vector2 pos;
    float bob, mood, blink;
    float hp, maxHp;
    int level, xp;
    bool owned;
} Buddy;
typedef struct {
    Vector2 pos;
    EnemyType type;
    float hp, maxHp, phase, attackCd;
    bool alive;
} Enemy;
typedef struct { int type; Vector2 pos; float scale; } MapObject;
typedef struct {
    unsigned char terrain[MAPH][MAPW];
    unsigned char solid[MAPH][MAPW];
    int seed;
    Pickup pickups[MAX_PICKUPS];
    int pickupCount;
    MapObject objects[MAX_MAP_OBJECTS];
    int objectCount;
    Vector2 camp, returnPoint, groveGate, ruinGate, shrine, bossPos;
} World;

typedef struct {
    Scene scene;
    Area area;
    Player player;
    Buddy lumi, splash, spark;
    World world;
    unsigned char areaPickupTaken[4][MAX_PICKUPS];
    Enemy enemies[MAX_ENEMIES];
    int enemyCount;
    Particle particles[MAX_PARTICLES];
    int chapterFlags;
    int dialoguePage;
    char dialogue[4][180];
    char dialogueSpeaker[32];
    char dialogueLineSpeaker[4][32];
    int dialoguePortrait[4];
    int dialogueCount;
    bool dialogueStartsBossBattle;
    int battleEnemy;
    int battleTurn;
    int battleMessage;
    float battleTimer;
    int battleChoice;
    float time;
    float endingTime;
    float cameraZoom;
    Camera2D cam;
    int collectedRelics;
    bool shrineSolved;
    bool bossDefeated;
    int lastSeed;
    float transition;
    int selectedBuddy;
    char toast[128];
    float toastTimer;
    bool journalOpen;
    bool inventoryOpen;
    bool mapOpen;
    bool objectiveCollapsed;
    bool enemySoaked;
    int battleRound;
    int defeatedGuards;
    bool fireflyDiscovered;
    bool leaflingFed;
    bool groveMoteDefeated;
    float fireflyPhase;
    float dayClock;
    float weatherClock;
    int weather;
} Game;
extern Color C_BG,C_INK,C_PAPER,C_GREEN,C_DARKGREEN,C_GOLD,C_BLUE,C_LILAC,C_PINK,C_RED;
void GameReset(Game *g,int seed);
void GameUpdate(Game *g,float dt);
void GameRender(Game *g);
void RenderInit(void);
void RenderClose(void);
void GameSave(Game *g);
bool GameLoad(Game *g);
void GameStart(Game *g);
Buddy *CurrentBuddy(Game *g);
void GetCurrentObjective(Game *g,const char **title,const char **detail,Vector2 *target,bool *hasTarget);
float Dist(Vector2 a,Vector2 b);
#endif