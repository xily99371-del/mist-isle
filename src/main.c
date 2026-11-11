#include "raylib.h"
#include "raymath.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <time.h>

#define SW 1280
#define SH 720
#define MAPW 22
#define MAPH 18
#define MAX_ENEMIES 8
#define MAX_PARTICLES 260
#define MAX_PICKUPS 32
#define PI2 6.28318530718f

typedef enum { SCENE_TITLE, SCENE_PLAY, SCENE_DIALOGUE, SCENE_BATTLE, SCENE_END } Scene;
typedef enum { AREA_CAMP, AREA_GROVE, AREA_RUINS, AREA_LIGHTHOUSE } Area;
typedef enum { ENEMY_MOTE, ENEMY_GUARD, ENEMY_BOSS } EnemyType;
typedef enum { SKILL_GLOW, SKILL_SPLASH, SKILL_SPARK } SkillType;

typedef struct { Vector2 p, v; float life, maxLife, size; Color c; } Particle;
typedef struct {
    int magic, version, seed;
    float px, py, hp;
    int berries, shells, crystals;
    int lumi, splash, spark, selected;
    int shrineSolved, bossDefeated, fireflyDiscovered, leaflingFed;
    unsigned char pickupTaken[MAX_PICKUPS];
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
    int level, xp;
    bool owned;
} Buddy;
typedef struct {
    Vector2 pos;
    EnemyType type;
    float hp, maxHp, phase, attackCd;
    bool alive;
} Enemy;
typedef struct {
    int kind[MAPH][MAPW];
    int seed;
    Pickup pickups[MAX_PICKUPS];
    int pickupCount;
    Vector2 camp, groveGate, ruinGate, shrine, bossPos;
} World;

typedef struct {
    Scene scene;
    Area area;
    Player player;
    Buddy lumi, splash, spark;
    World world;
    Enemy enemies[MAX_ENEMIES];
    int enemyCount;
    Particle particles[MAX_PARTICLES];
    int chapterFlags;
    int dialoguePage;
    char dialogue[4][180];
    char dialogueSpeaker[32];
    int dialogueCount;
    int battleEnemy;
    int battleTurn;
    int battleMessage;
    float battleTimer;
    int battleChoice;
    float time;
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
    bool fireflyDiscovered;
    bool leaflingFed;
    float fireflyPhase;
} Game;

static Color C_BG={207,232,222,255}, C_INK={67,83,73,255};
static Color C_PAPER={255,250,232,246}, C_GREEN={112,166,116,255};
static Color C_DARKGREEN={70,119,86,255}, C_GOLD={255,219,133,255};
static Color C_BLUE={119,190,208,255}, C_LILAC={190,166,220,255};
static Color C_PINK={239,169,177,255}, C_RED={217,105,106,255};
static Texture2D TEX_HERO={0}, TEX_LUMI={0}, TEX_MOATAIL={0}, TEX_GECKO={0}, TEX_KEEPER={0};
static Texture2D TEX_HERO_WALK={0}, TEX_LUMI_PIXEL={0}, TEX_MOATAIL_PIXEL={0}, TEX_GECKO_PIXEL={0}, TEX_ISO_TILES={0}, TEX_PROPS={0};
static void DrawPortrait(Texture2D tex, Vector2 p, float w, float h, float bob) {
    if (tex.id == 0) return;
    Rectangle src={0,0,(float)tex.width,(float)tex.height};
    Rectangle dst={p.x-w*0.5f,p.y-h-bob,w,h};
    DrawTexturePro(tex,src,dst,(Vector2){0,0},0,WHITE);
}

static Vector2 Iso(float x,float y,float z) {
    return (Vector2){(x-y)*31.0f,(x+y)*15.5f-z};
}
static Vector2 WorldScreen(Vector2 p) { return Iso(p.x,p.y,0); }
static void Toast(Game *g,const char *s) {
    snprintf(g->toast,sizeof(g->toast),"%s",s); g->toastTimer=2.4f;
}
static float Dist(Vector2 a,Vector2 b) { return Vector2Distance(a,b); }
static void AddParticle(Game *g,Vector2 p,Color c,float size) {
    for(int i=0;i<MAX_PARTICLES;i++) if(g->particles[i].life<=0) {
        float a=(float)GetRandomValue(0,628)/100.0f;
        float sp=(float)GetRandomValue(25,125)/35.0f;
        g->particles[i]=(Particle){p,{cosf(a)*sp,sinf(a)*sp-0.6f},0.55f,0.55f,size,c};
        break;
    }
}
static void Burst(Game *g,Vector2 p,Color c,int n) {
    for(int i=0;i<n;i++) AddParticle(g,p,c,(float)GetRandomValue(2,6));
}
static void SetDialogueAs(Game *g,const char *speaker,const char *a,const char *b,const char *c) {
    snprintf(g->dialogueSpeaker,sizeof(g->dialogueSpeaker),"%s",speaker?speaker:"旁白");
    g->dialogueCount=0;
    if(a){snprintf(g->dialogue[g->dialogueCount++],180,"%s",a);}
    if(b){snprintf(g->dialogue[g->dialogueCount++],180,"%s",b);}
    if(c){snprintf(g->dialogue[g->dialogueCount++],180,"%s",c);}
    g->dialoguePage=0; g->scene=SCENE_DIALOGUE;
}
static void SetDialogue(Game *g,const char *a,const char *b,const char *c) {
    SetDialogueAs(g,"苔灯灵",a,b,c);
}
static void AddPickup(World *w,int kind,float x,float y) {
    if(w->pickupCount>=MAX_PICKUPS)return;
    int index = w->pickupCount;
    w->pickups[index] = (Pickup){{x,y},kind,false,(float)index*0.7f};
    w->pickupCount++;
}
static void BuildWorld(World *w,int seed) {
    memset(w,0,sizeof(*w)); w->seed=seed;
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        int edge=(x==0||y==0||x==MAPW-1||y==MAPH-1);
        w->kind[y][x]=edge?3:0;
        if(!edge) {
            unsigned v=(unsigned)(x*92837111u+y*689287499u+(unsigned)seed*283923481u);
            v^=v>>13; v*=1274126177u; v^=v>>16;
            if(v%100<8)w->kind[y][x]=1;
            else if(v%100<13)w->kind[y][x]=2;
        }
    }
    // Keep a guaranteed walkable spine between all story areas.
    for(int x=2;x<20;x++) { w->kind[9][x]=0; w->kind[10][x]=0; }
    for(int y=3;y<15;y++) { w->kind[y][4]=0; w->kind[y][5]=0; }
    for(int y=5;y<14;y++) { w->kind[y][14]=0; w->kind[y][15]=0; }
    for(int y=4;y<=8;y++)for(int x=8;x<=13;x++)w->kind[y][x]=0;
    w->camp=(Vector2){3,12}; w->groveGate=(Vector2){8,8};
    w->ruinGate=(Vector2){14,9}; w->shrine=(Vector2){17,5};
    w->bossPos=(Vector2){18,13};
    AddPickup(w,0,6,12); AddPickup(w,0,8,6); AddPickup(w,0,11,11);
    AddPickup(w,1,10,5); AddPickup(w,1,15,7); AddPickup(w,1,18,8);
    AddPickup(w,2,12,13); AddPickup(w,2,16,12);
    AddPickup(w,3,7,4); AddPickup(w,3,19,5);
    for(int i=0;i<8;i++) AddPickup(w,0,5+(i*3)%13,4+(i*5)%10);
    // Forest-only forage, distributed away from the guaranteed path.
    AddPickup(w,3,9,3); AddPickup(w,3,11,7); AddPickup(w,0,12,5);
    AddPickup(w,0,7,6); AddPickup(w,1,13,6);
}
static bool Solid(Game *g,Vector2 p) {
    int x=(int)floorf(p.x+0.5f),y=(int)floorf(p.y+0.5f);
    if(x<1||y<1||x>=MAPW-1||y>=MAPH-1)return true;
    if(g->world.kind[y][x]==1)return true;
    return false;
}
static void AddEnemy(Game *g,EnemyType t,float x,float y,float hp) {
    if(g->enemyCount>=MAX_ENEMIES)return;
    g->enemies[g->enemyCount++]=(Enemy){{x,y},t,hp,hp,(float)GetRandomValue(0,628)/100.0f,1.2f,true};
}
static void ResetGame(Game *g,int seed);
static void SaveGame(Game *g) {
    SaveData s={0};
    s.magic=0x4D495354; s.version=1; s.seed=g->lastSeed;
    s.px=g->player.pos.x; s.py=g->player.pos.y; s.hp=g->player.hp;
    s.berries=g->player.berries; s.shells=g->player.shells; s.crystals=g->player.crystals;
    s.lumi=g->lumi.owned; s.splash=g->splash.owned; s.spark=g->spark.owned; s.selected=g->selectedBuddy;
    s.shrineSolved=g->shrineSolved; s.bossDefeated=g->bossDefeated;
    s.fireflyDiscovered=g->fireflyDiscovered; s.leaflingFed=g->leaflingFed;
    for(int i=0;i<g->world.pickupCount && i<MAX_PICKUPS;i++)s.pickupTaken[i]=(unsigned char)g->world.pickups[i].taken;
    if(!SaveFileData("mist_isle_save.dat",&s,(int)sizeof(s)))Toast(g,"保存失败：无法写入存档文件。");
}
static bool LoadGame(Game *g) {
    int size=0; unsigned char *raw=LoadFileData("mist_isle_save.dat",&size);
    if(!raw || size!=(int)sizeof(SaveData)){if(raw)UnloadFileData(raw);Toast(g,"没有找到有效存档。");return false;}
    SaveData s; memcpy(&s,raw,sizeof(s)); UnloadFileData(raw);
    if(s.magic!=0x4D495354 || s.version!=1){Toast(g,"存档版本不兼容。");return false;}
    ResetGame(g,s.seed);
    g->scene=SCENE_PLAY; g->player.pos=(Vector2){s.px,s.py};g->player.hp=fmaxf(1.0f,fminf(100.0f,s.hp));
    g->player.berries=s.berries;g->player.shells=s.shells;g->player.crystals=s.crystals;
    g->lumi.owned=s.lumi!=0;g->splash.owned=s.splash!=0;g->spark.owned=s.spark!=0;g->selectedBuddy=s.selected;
    g->shrineSolved=s.shrineSolved!=0;g->bossDefeated=s.bossDefeated!=0;
    g->fireflyDiscovered=s.fireflyDiscovered!=0;g->leaflingFed=s.leaflingFed!=0;
    for(int i=0;i<g->world.pickupCount && i<MAX_PICKUPS;i++)g->world.pickups[i].taken=s.pickupTaken[i]!=0;
    if(g->lumi.owned)g->lumi.pos=Vector2Add(g->player.pos,(Vector2){-.55f,.42f});
    if(g->splash.owned)g->splash.pos=Vector2Add(g->player.pos,(Vector2){-.95f,.85f});
    if(g->spark.owned)g->spark.pos=Vector2Add(g->player.pos,(Vector2){-.15f,.95f});
    Toast(g,"已读取上次的旅程。"); return true;
}
static void ResetGame(Game *g,int seed) {
    memset(g,0,sizeof(*g)); g->scene=SCENE_TITLE; g->area=AREA_CAMP;
    g->lastSeed=seed; BuildWorld(&g->world,seed);
    g->player.pos=g->world.camp; g->player.hp=g->player.maxHp=100; g->player.stamina=100;
    g->lumi=(Buddy){.pos={2.3f,12.4f},.owned=false,.level=1};
    g->splash=(Buddy){.pos={8,7},.owned=false,.level=1};
    g->spark=(Buddy){.pos={15,8},.owned=false,.level=1};
    g->cam.offset=(Vector2){SW/2.0f,SH/2.0f+20}; g->cam.zoom=1;
    g->fireflyPhase=0; g->journalOpen=false; g->fireflyDiscovered=false; g->leaflingFed=false;
}
static void InitPlay(Game *g) {
    g->scene=SCENE_PLAY; g->area=AREA_CAMP; g->player.pos=g->world.camp;
    SetDialogueAs(g,"旅人手记","你在海岸边醒来，远处的灯塔已经熄灭。",
        "一只小小的苔灯灵躲在营火旁。它似乎受了伤。",
        "靠近它，按 E 互动。WASD 移动，SHIFT 冲刺。");
}
static void AddBuddyFriend(Game *g,int id) {
    if(id==0){g->lumi.owned=true;g->lumi.pos=Vector2Add(g->player.pos,(Vector2){-0.7f,0.4f});}
    if(id==1){g->splash.owned=true;g->splash.pos=Vector2Add(g->player.pos,(Vector2){-0.7f,0.4f});}
    if(id==2){g->spark.owned=true;g->spark.pos=Vector2Add(g->player.pos,(Vector2){-0.7f,0.4f});}
}
static Buddy *CurrentBuddy(Game *g) {
    if(g->selectedBuddy==1 && g->splash.owned)return &g->splash;
    if(g->selectedBuddy==2 && g->spark.owned)return &g->spark;
    return &g->lumi;
}
static void EnterBattle(Game *g,int idx) {
    g->battleEnemy=idx;g->battleTurn=0;g->battleChoice=0;g->battleTimer=0;
    g->scene=SCENE_BATTLE;
    g->battleMessage=0;
}
static void StartStoryBattle(Game *g) {
    AddEnemy(g,ENEMY_GUARD,18,13,55);
    EnterBattle(g,g->enemyCount-1);
}
static void UpdateGame(Game *g,float dt) {
    g->time+=dt; if(g->toastTimer>0)g->toastTimer-=dt;
    g->fireflyPhase+=dt;
    if(g->journalOpen) { if(IsKeyPressed(KEY_J)||IsKeyPressed(KEY_ESCAPE))g->journalOpen=false; return; }
    for(int i=0;i<MAX_PARTICLES;i++) if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];p->life-=dt;p->p=Vector2Add(p->p,Vector2Scale(p->v,dt));p->v.y-=0.3f*dt;
    }
    if(g->scene==SCENE_TITLE) {
        if(IsKeyPressed(KEY_F9)) { if(LoadGame(g)) return; }
        if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) InitPlay(g);
        return;
    }
    if(g->scene==SCENE_DIALOGUE) {
        if(IsKeyPressed(KEY_E)||IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
            g->dialoguePage++;
            if(g->dialoguePage>=g->dialogueCount)g->scene=SCENE_PLAY;
        }
        return;
    }
    if(g->scene==SCENE_END) {
        if(IsKeyPressed(KEY_R)) ResetGame(g,g->lastSeed+1);
        return;
    }
    if(g->scene==SCENE_BATTLE) {
        Enemy *e=&g->enemies[g->battleEnemy];
        if(!e->alive){g->scene=SCENE_PLAY;return;}
        if(IsKeyPressed(KEY_ONE)||IsKeyPressed(KEY_Q))g->battleChoice=0;
        if(IsKeyPressed(KEY_TWO)||IsKeyPressed(KEY_W))g->battleChoice=1;
        if(IsKeyPressed(KEY_THREE)||IsKeyPressed(KEY_E))g->battleChoice=2;
        if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
            if(g->battleTurn==0) {
                if(g->battleChoice==0) {e->hp-=22;Burst(g,WorldScreen(e->pos),C_BLUE,18);Toast(g,"苔灯灵：星光治愈！");}
                else if(g->battleChoice==1 && g->splash.owned){e->hp-=17;Burst(g,WorldScreen(e->pos),C_BLUE,24);Toast(g,"沫尾兽：泡泡水流！");}
                else if(g->battleChoice==2 && g->spark.owned){e->hp-=27;Burst(g,WorldScreen(e->pos),C_LILAC,24);Toast(g,"鸣晶蜥：晶弧放电！");}
                else {e->hp-=12;Toast(g,"伙伴发动了基础攻击！");}
                g->battleTurn=1;
                if(e->hp<=0){e->alive=false;g->battleTurn=0;g->scene=SCENE_PLAY;Toast(g,"守卫失去战斗能力！");}
            } else {
                g->player.hp-=12;Burst(g,WorldScreen(g->player.pos),C_PINK,10);
                g->battleTurn=0;
                if(g->player.hp<=0){g->player.hp=55;g->scene=SCENE_PLAY;Toast(g,"伙伴把你带回安全处，恢复了体力。");}
            }
        }
        return;
    }

    Player *p=&g->player;
    if(IsKeyPressed(KEY_J)){g->journalOpen=true;return;}
    if(IsKeyPressed(KEY_F5)){SaveGame(g);Toast(g,"旅程已保存！");return;}
    if(IsKeyPressed(KEY_F9)){LoadGame(g);return;}
    p->dashCd=fmaxf(0,p->dashCd-dt);
    p->invuln=fmaxf(0,p->invuln-dt);
    Vector2 in={0,0};
    if(IsKeyDown(KEY_W)||IsKeyDown(KEY_UP))in.y-=1;
    if(IsKeyDown(KEY_S)||IsKeyDown(KEY_DOWN))in.y+=1;
    if(IsKeyDown(KEY_A)||IsKeyDown(KEY_LEFT))in.x-=1;
    if(IsKeyDown(KEY_D)||IsKeyDown(KEY_RIGHT))in.x+=1;
    float l=Vector2Length(in);if(l>0.01f)in=Vector2Scale(in,1/l);
    float speed=3.3f;
    if(IsKeyPressed(KEY_LEFT_SHIFT)&&p->dashCd<=0&&l>0.01f){speed=8.5f;p->dashCd=0.9f;p->invuln=0.18f;Burst(g,WorldScreen(p->pos),WHITE,8);}
    p->vel=Vector2Scale(in,speed);
    if (fabsf(in.x) > fabsf(in.y) && fabsf(in.x) > 0.01f) p->facing = in.x < 0 ? 1 : 2;
    else if (fabsf(in.y) > 0.01f) p->facing = in.y < 0 ? 3 : 0;
    Vector2 n=p->pos;n.x+=p->vel.x*dt;if(!Solid(g,n))p->pos.x=n.x;
    n=p->pos;n.y+=p->vel.y*dt;if(!Solid(g,n))p->pos.y=n.y;

    Buddy *followers[3]={&g->lumi,&g->splash,&g->spark};
    for(int bi=0;bi<3;bi++) {
        Buddy *b=followers[bi];
        if(!b->owned) continue;
        Vector2 offset = bi==0 ? (Vector2){-0.55f,0.42f} : bi==1 ? (Vector2){-0.95f,0.85f} : (Vector2){-0.15f,0.95f};
        Vector2 target=Vector2Add(p->pos,offset);
        b->pos=Vector2Add(b->pos,Vector2Scale(Vector2Subtract(target,b->pos),fminf(1,dt*(4.2f+bi*.25f))));
        b->bob=sinf(g->time*(4.0f+bi*.5f)+bi)*0.12f;
        b->blink-=dt;if(b->blink<=0)b->blink=2.0f+(float)GetRandomValue(0,15)/10.0f;
    }
    for(int i=0;i<g->world.pickupCount;i++)g->world.pickups[i].phase+=dt*2.2f;

    if(IsKeyPressed(KEY_TAB)) {
        int start = g->selectedBuddy;
        for (int step=1; step<=3; ++step) {
            int candidate = (start + step) % 3;
            bool available = candidate==0 ? g->lumi.owned : candidate==1 ? g->splash.owned : g->spark.owned;
            if (available) { g->selectedBuddy=candidate; break; }
        }
        const char *names[3]={"苔灯灵","沫尾兽","鸣晶蜥"};
        Toast(g,TextFormat("当前伙伴：%s",names[g->selectedBuddy]));
    }
    if(IsKeyPressed(KEY_E)) {
        // Rescue Moss Lumi at the camp.
        if(!g->lumi.owned&&Dist(p->pos,(Vector2){4,12})<1.5f) {
            AddBuddyFriend(g,0);
            SaveGame(g);
            SetDialogue(g,"苔灯灵抬起头，灯囊重新亮了起来。",
                "它决定跟着你。靠近暗处时，它会照亮隐藏的纹路。",
                "按 TAB 可以切换已获得的伙伴。");
            return;
        }
        // Pickups.
        for(int i=0;i<g->world.pickupCount;i++) {
            Pickup *q=&g->world.pickups[i];
            if(!q->taken&&Dist(p->pos,q->tile)<1.0f) {
                q->taken=true;
                if(q->kind==0)p->berries++;
                if(q->kind==1)p->shells++;
                if(q->kind==2)p->crystals++;
                if(q->kind==3)p->berries++;
                Burst(g,WorldScreen(q->tile),C_GOLD,12);
                Toast(g,q->kind==1?"获得潮汐贝壳！":q->kind==2?"获得星晶！":"获得岛屿果实！");
                SaveGame(g);
                return;
            }
        }
        // A shy forest firefly can be registered in the field journal.
        Vector2 fireflyPos={10.6f+sinf(g->fireflyPhase*0.8f)*0.22f,6.2f+cosf(g->fireflyPhase*0.65f)*0.18f};
        if(!g->fireflyDiscovered && Dist(p->pos,fireflyPos)<1.35f) {
            g->fireflyDiscovered=true;
            Burst(g,WorldScreen(fireflyPos),C_GOLD,22);
            SetDialogueAs(g,"萤芽","一只小小的萤芽停在叶片上，胸口的光一闪一闪。","它没有跟你走，只是把这片林地的秘密记在了旅人手记里。","按 J 打开手记，可以查看发现与当前目标。");
            return;
        }
        // A hungry leafling accepts a berry and reveals a hidden grove cache.
        if(!g->leaflingFed && Dist(p->pos,(Vector2){12.4f,8.0f})<1.4f) {
            if(p->berries>0) {
                p->berries--; g->leaflingFed=true;
                AddPickup(&g->world,2,13.2f,8.6f);
                Burst(g,WorldScreen((Vector2){12.4f,8.0f}),C_GREEN,18);
                SetDialogueAs(g,"叶团","叶团闻到了果实的香气，开心地蹦了起来。","它拨开藤蔓，露出一枚被藏起来的星晶！","探索时多留意会主动回应你的生物。");
            } else Toast(g,"叶团饿了：给它一颗岛屿果实吧。");
            return;
        }
        // Water companion encounter.
        if(!g->splash.owned&&Dist(p->pos,(Vector2){9,5})<1.6f) {
            if(p->berries>=1){p->berries--;AddBuddyFriend(g,1);SaveGame(g);SetDialogue(g,"沫尾兽被果实吸引，开心地绕着你转了一圈。","它可以喷水清理蒸汽，也能让遗迹机关显形。","按 TAB 切换伙伴。");}
            else Toast(g,"沫尾兽想吃一颗岛屿果实。");
            return;
        }
        // Crystal companion encounter.
        if(!g->spark.owned&&Dist(p->pos,(Vector2){15,7})<1.6f) {
            if(p->crystals>=1){p->crystals--;AddBuddyFriend(g,2);SaveGame(g);SetDialogue(g,"鸣晶蜥吸收了星晶，背上的宝石依次亮起。","它能为古代装置供能，也能在战斗中放电。","按 TAB 切换伙伴。");}
            else Toast(g,"鸣晶蜥需要一枚星晶才愿意同行。");
            return;
        }
        // Rest beside the campfire after rescuing the first companion.
        if(g->lumi.owned && Dist(p->pos,(Vector2){4.1f,12.5f})<1.25f) {
            p->hp=fminf(p->maxHp,p->hp+22.0f);
            Toast(g,"在营火旁休息了一会儿，恢复了生命。");
            Burst(g,WorldScreen((Vector2){4.1f,12.5f}),C_GOLD,10);
            return;
        }
        // Ruin puzzle.
        if(Dist(p->pos,g->world.shrine)<1.7f&&!g->shrineSolved) {
            if(g->lumi.owned&&g->splash.owned&&g->spark.owned&&p->shells>=1) {
                g->shrineSolved=true;p->shells--;SaveGame(g);
                Burst(g,WorldScreen(g->world.shrine),C_GOLD,40);
                SetDialogue(g,"三种伙伴的力量在遗迹中央交汇。","水纹显现了线路，星光照亮符文，晶弧重新接通能源。","灯塔庭院的封印解除了。");
            } else Toast(g,"遗迹需要三位伙伴协力，并带来一枚潮汐贝壳。");
            return;
        }
        // Final encounter trigger.
        if(g->shrineSolved&&Dist(p->pos,g->world.bossPos)<2.0f&&!g->bossDefeated) {
            AddEnemy(g,ENEMY_BOSS,18,13,90);EnterBattle(g,g->enemyCount-1);return;
        }
        // NPC at camp.
        if(Dist(p->pos,(Vector2){3,10})<1.6f) {
            SetDialogueAs(g,"守望者","灯塔熄灭后，岛上的小生灵都变得不安。","找到三位伙伴，唤醒潮汐遗迹，灯塔就能重新点亮。","果实、贝壳和星晶散落在岛上，按 E 采集。");
            return;
        }
    }
    // Story areas: region messages / final ending.
    if(g->bossDefeated&&Dist(p->pos,g->world.shrine)<2.0f) {
        g->scene=SCENE_END;return;
    }
    if(g->shrineSolved&&g->enemyCount>0&&!g->enemies[g->enemyCount-1].alive)g->bossDefeated=true;

    g->cam.target=Iso(p->pos.x,p->pos.y,0);
    if(g->toastTimer>0)g->toastTimer-=dt;
}
static void DrawIsoTile(int x,int y,int tile,Color fallback) {
    Vector2 p=Iso((float)x,(float)y,0);
    if (TEX_ISO_TILES.id) {
        Rectangle src={(float)(tile*64),0,64,32};
        Rectangle dst={p.x-32,p.y-16,64,32};
        DrawTexturePro(TEX_ISO_TILES,src,dst,(Vector2){0,0},0,WHITE);
        return;
    }
    Vector2 a={p.x,p.y-15.5f},b={p.x+31,p.y},d={p.x-31,p.y},e={p.x,p.y+15.5f};
    DrawTriangle(a,b,d,fallback);DrawTriangle(b,e,d,fallback);
}
static void DrawTree(Vector2 p,float s,float t) {
    if(TEX_PROPS.id) {
        int variant=((int)(p.x+p.y))%3;
        Rectangle src={(float)(variant*64),0,64,64};
        Rectangle dst={p.x-32*s,p.y-62*s,64*s,64*s};
        DrawTexturePro(TEX_PROPS,src,dst,(Vector2){0,0},0,WHITE);
        return;
    }
    DrawEllipse((int)p.x,(int)p.y+5,15*s,5*s,(Color){67,101,76,75});
    DrawRectangle((int)(p.x-3*s),(int)(p.y-24*s),(int)(6*s),(int)(28*s),(Color){153,113,77,255});
    DrawCircleV((Vector2){p.x,p.y-36*s},16*s,(Color){99,163,109,255});
    DrawCircleV((Vector2){p.x-9*s,p.y-42*s},11*s,(Color){126,186,126,255});
    DrawCircleV((Vector2){p.x+9*s,p.y-42*s},11*s,(Color){112,177,119,255});
    DrawCircleV((Vector2){p.x+4*s,p.y-49*s},4*s,Fade(C_GOLD,.3f+.1f*sinf(t+p.x)));
}
static void DrawProp(int index,Vector2 p,float w,float h) {
    if(!TEX_PROPS.id)return;
    Rectangle src={(float)(index*64),0,64,64};
    Rectangle dst={p.x-w*.5f,p.y-h,w,h};
    DrawTexturePro(TEX_PROPS,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawKeeper(Vector2 p,float t) {
    DrawEllipse((int)p.x,(int)p.y+7,13,4,(Color){67,88,76,80});
    if(TEX_KEEPER.id) {
        Rectangle src={0,0,32,32};
        Rectangle dst={p.x-18,p.y-39+sinf(t*2.0f)*0.5f,36,36};
        DrawTexturePro(TEX_KEEPER,src,dst,(Vector2){0,0},0,WHITE);
        return;
    }
    DrawCircleV((Vector2){p.x,p.y-29},10,(Color){229,222,198,255});
    DrawCircleV((Vector2){p.x-3,p.y-30},1.5f,C_INK);
    DrawCircleV((Vector2){p.x+3,p.y-30},1.5f,C_INK);
    DrawRectangle((int)p.x-9,(int)p.y-19,18,18,(Color){111,151,116,255});
}
static void DrawFirefly(Vector2 p,float t) {
    float bob=sinf(t*3.8f)*4.0f;
    DrawCircleV((Vector2){p.x,p.y-22-bob},13,Fade(C_GOLD,.10f+.06f*sinf(t*2)));
    DrawCircleV((Vector2){p.x,p.y-22-bob},5,(Color){255,232,150,255});
    DrawCircleV((Vector2){p.x-2,p.y-24-bob},1.5f,WHITE);
    DrawCircleV((Vector2){p.x-9,p.y-18-bob},2,Fade(WHITE,.75f));
    DrawCircleV((Vector2){p.x+9,p.y-29-bob},2,Fade(C_GOLD,.75f));
}
static void DrawLeafling(Vector2 p,float t) {
    float bob=sinf(t*4.0f)*2.5f;
    DrawEllipse((int)p.x,(int)p.y+5,11,4,(Color){70,105,76,75});
    DrawCircleV((Vector2){p.x,p.y-10-bob},10,(Color){157,192,118,255});
    DrawTriangle((Vector2){p.x-5,p.y-16-bob},(Vector2){p.x+1,p.y-29-bob},(Vector2){p.x+7,p.y-15-bob},(Color){102,160,94,255});
    DrawCircleV((Vector2){p.x-3,p.y-11-bob},1.6f,C_INK);
    DrawCircleV((Vector2){p.x+3,p.y-11-bob},1.6f,C_INK);
    DrawCircleV((Vector2){p.x,p.y-6-bob},1.4f,(Color){189,111,102,255});
}
static void DrawCampfire(Vector2 p,float t) {
    DrawProp(8,p,48,48);
    float pulse=0.5f+0.5f*sinf(t*7.0f);
    DrawCircleV((Vector2){p.x,p.y-30},18,Fade((Color){255,177,91,255},0.10f+0.06f*pulse));
    DrawCircleV((Vector2){p.x,p.y-28},7,Fade((Color){255,220,132,255},0.15f+0.10f*pulse));
    if(((int)(t*8.0f))%3==0) {
        DrawCircleV((Vector2){p.x-8,p.y-47-(t*9.0f-floorf(t*9.0f))*7},2,Fade(C_GOLD,0.7f));
        DrawCircleV((Vector2){p.x+7,p.y-41-(t*7.0f-floorf(t*7.0f))*6},1.5f,Fade(WHITE,0.65f));
    }
}
static void DrawLumi(Vector2 p,float t,float bob) {
    DrawEllipse((int)p.x,(int)p.y+7,16,6,(Color){72,109,77,75});
    if (TEX_LUMI_PIXEL.id) {
        int frame=(int)(t*2.5f)%2;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-18,p.y-38-sinf(t*4.4f)*2.0f,36,36};
        DrawTexturePro(TEX_LUMI_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
        return;
    }
    if (TEX_LUMI.id) { DrawPortrait(TEX_LUMI,p,58,58,sinf(t*4.4f)*3.0f); return; }
    p.y-=bob*16;
    DrawCircleV((Vector2){p.x,p.y-11},14,(Color){159,202,145,255});
    DrawCircleV((Vector2){p.x-7,p.y-21},8,(Color){128,181,121,255});
    DrawCircleV((Vector2){p.x+7,p.y-21},8,(Color){128,181,121,255});
    DrawTriangle((Vector2){p.x-7,p.y-26},(Vector2){p.x+7,p.y-26},(Vector2){p.x,p.y-42},(Color){116,171,110,255});
    DrawCircleV((Vector2){p.x,p.y-13},9,(Color){218,232,180,255});
    DrawCircleV((Vector2){p.x-4,p.y-15},2.1f,(Color){53,69,56,255});
    DrawCircleV((Vector2){p.x+4,p.y-15},2.1f,(Color){53,69,56,255});
    DrawCircleV((Vector2){p.x,p.y-34},7,Fade(C_GOLD,.45f+.3f*sinf(t*2)));
    DrawCircleV((Vector2){p.x,p.y-34},3.3f,(Color){255,244,185,255});
}
static void DrawPlayer(Vector2 p,float t,bool moving,int facing) {
    float bob=moving?fabsf(sinf(t*13))*1.5f:sinf(t*2)*0.4f;
    DrawEllipse((int)p.x,(int)p.y+8,13,4,(Color){67,88,101,85});
    if (TEX_HERO_WALK.id) {
        int frame = moving ? ((int)(t*8.0f)%4) : 0;
        Rectangle src={(float)(frame*32),(float)(facing*32),32,32};
        Rectangle dst={p.x-18,p.y-39-bob,36,40};
        DrawTexturePro(TEX_HERO_WALK,src,dst,(Vector2){0,0},0,WHITE);
        return;
    }
    if (TEX_HERO.id) { DrawPortrait(TEX_HERO,p,66,72,bob); return; }
    DrawTriangle((Vector2){p.x-12,p.y-20-bob},(Vector2){p.x+12,p.y-20-bob},(Vector2){p.x,p.y+2-bob},(Color){105,161,190,255});
    DrawCircleV((Vector2){p.x,p.y-32-bob},11,(Color){250,218,185,255});
    DrawCircleV((Vector2){p.x-4,p.y-33-bob},2,(Color){59,65,75,255});
    DrawCircleV((Vector2){p.x+4,p.y-33-bob},2,(Color){59,65,75,255});
    DrawCircleV((Vector2){p.x-8,p.y-41-bob},3,(Color){228,166,111,255});
    DrawCircleV((Vector2){p.x+8,p.y-41-bob},3,(Color){228,166,111,255});
}
static void DrawShrine(Vector2 p,float t,bool solved) {
    Color c=solved?C_GOLD:(Color){158,185,166,255};
    DrawEllipse((int)p.x,(int)p.y+7,29,9,(Color){66,95,85,95});
    DrawRectangle((int)p.x-14,(int)p.y-25,28,31,(Color){156,174,149,255});
    DrawRectangle((int)p.x-8,(int)p.y-45,16,22,(Color){198,209,171,255});
    DrawCircleV((Vector2){p.x,p.y-56-sinf(t*2)*3},8,c);
    if(solved)DrawCircleV((Vector2){p.x,p.y-55},29,Fade(c,.18f));
}
static void DrawPickup(Pickup *p) {
    Vector2 s=Iso(p->tile.x,p->tile.y,0);float bob=sinf(p->phase)*3;
    Color c=p->kind==1?C_BLUE:p->kind==2?C_LILAC:p->kind==3?C_PINK:C_GOLD;
    DrawEllipse((int)s.x,(int)s.y+4,8,3,(Color){70,104,79,85});
    DrawCircleV((Vector2){s.x,s.y-10-bob},7,c);
    DrawCircleV((Vector2){s.x-2,s.y-13-bob},2,Fade(WHITE,.8f));
    DrawCircleV((Vector2){s.x,s.y-10-bob},12,Fade(c,.12f));
}
typedef enum {
    RENDER_TREE, RENDER_PROP, RENDER_FIRE, RENDER_PICKUP, RENDER_SHRINE,
    RENDER_KEEPER, RENDER_LUMI, RENDER_MOATAIL, RENDER_GECKO, RENDER_PLAYER, RENDER_FIREFLY, RENDER_LEAFLING
} RenderKind;
typedef struct { float depth; RenderKind kind; int index; Vector2 grid; float scale; } RenderItem;
static void PushRenderItem(RenderItem *items,int *count,RenderKind kind,int index,Vector2 grid,float scale) {
    if(*count>=64)return;
    items[*count]=(RenderItem){grid.x+grid.y,kind,index,grid,scale};
    (*count)++;
}
static void DrawMoatail(Vector2 p,float t) {
    DrawEllipse((int)p.x,(int)p.y+6,15,5,(Color){75,112,130,75});
    if(TEX_MOATAIL_PIXEL.id) {
        int frame=(int)(t*2.5f)%2;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-18,p.y-38-sinf(t*4)*2.0f,36,36};
        DrawTexturePro(TEX_MOATAIL_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_MOATAIL.id) DrawPortrait(TEX_MOATAIL,p,58,58,sinf(t*4)*2.5f);
    else {
        DrawCircleV((Vector2){p.x,p.y-11},12,(Color){121,202,215,255});
        DrawCircleV((Vector2){p.x-5,p.y-20},5,(Color){151,220,224,255});
        DrawCircleV((Vector2){p.x+5,p.y-20},5,(Color){151,220,224,255});
        DrawCircleV((Vector2){p.x-4,p.y-12},2,(Color){52,83,94,255});
        DrawCircleV((Vector2){p.x+4,p.y-12},2,(Color){52,83,94,255});
    }
}
static void DrawGecko(Vector2 p,float t) {
    DrawEllipse((int)p.x,(int)p.y+6,15,5,(Color){83,91,130,75});
    if(TEX_GECKO_PIXEL.id) {
        int frame=(int)(t*2.5f)%2;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-18,p.y-38-sinf(t*5)*2.0f,36,36};
        DrawTexturePro(TEX_GECKO_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_GECKO.id) DrawPortrait(TEX_GECKO,p,58,58,sinf(t*5)*2.0f);
    else {
        DrawCircleV((Vector2){p.x,p.y-10},11,(Color){155,154,214,255});
        DrawTriangle((Vector2){p.x-7,p.y-17},(Vector2){p.x,p.y-32},(Vector2){p.x+7,p.y-17},(Color){179,218,229,255});
        DrawCircleV((Vector2){p.x-4,p.y-11},2,(Color){50,60,88,255});
        DrawCircleV((Vector2){p.x+4,p.y-11},2,(Color){50,60,88,255});
    }
}
static void RenderWorld(Game *g) {
    ClearBackground(C_BG);
    BeginMode2D(g->cam);

    // Ground first: every tile is drawn before any elevated object.
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        Color c=((x+y)&1)?(Color){151,198,144,255}:(Color){160,205,150,255};
        if(g->world.kind[y][x]==1)c=(Color){116,157,117,255};
        if(g->world.kind[y][x]==2)c=(Color){192,181,143,255};
        if(x>=8&&x<=13&&y>=3&&y<=8)c=(Color){130,181,123,255};
        if(x>=14&&x<=19&&y>=4&&y<=10)c=(Color){184,181,151,255};
        int tile=(g->world.kind[y][x]==1)?2:(g->world.kind[y][x]==2)?3:((x+y)%3);
        if(x>=8&&x<=13&&y>=3&&y<=8)tile=(x+y)%3;
        if(x>=14&&x<=19&&y>=4&&y<=10)tile=5;
        DrawIsoTile(x,y,tile,c);
    }
    // A small tidal pool and stepping-stone edge.
    for(int y=3;y<6;y++)for(int x=2;x<6;x++)DrawIsoTile(x,y,4,(Color){119,191,199,255});

    // Sort elevated objects by isometric depth (x+y): characters can pass behind trees.
    RenderItem items[64]; int count=0;
    for(int i=0;i<15;i++) {
        int x=2+(i*7)%17,y=2+(i*11)%13;
        if(g->world.kind[y][x]!=1)PushRenderItem(items,&count,RENDER_TREE,i,(Vector2){(float)x,(float)y},.72f+(i%3)*.1f);
    }
    PushRenderItem(items,&count,RENDER_PROP,4,(Vector2){3.2f,11.4f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,5,(Vector2){4.6f,10.8f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,6,(Vector2){5.5f,11.7f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,7,(Vector2){19.0f,2.2f},1.0f);
    PushRenderItem(items,&count,RENDER_FIRE,8,(Vector2){4.1f,12.5f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,9,(Vector2){5.9f,10.3f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,10,(Vector2){6.6f,12.5f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,11,(Vector2){2.4f,14.0f},1.0f);
    // Dense grove dressing gives the forest its own visual identity.
    PushRenderItem(items,&count,RENDER_PROP,12,(Vector2){9.0f,4.0f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,12,(Vector2){12.8f,6.8f},0.85f);
    PushRenderItem(items,&count,RENDER_PROP,13,(Vector2){10.0f,8.0f},1.0f);
    PushRenderItem(items,&count,RENDER_PROP,14,(Vector2){13.0f,5.0f},0.9f);
    PushRenderItem(items,&count,RENDER_PROP,15,(Vector2){8.4f,7.4f},1.0f);
    for(int i=0;i<7;i++) {
        int gx=8+(i*3)%6, gy=3+(i*5)%6;
        PushRenderItem(items,&count,RENDER_TREE,20+i,(Vector2){(float)gx,(float)gy},0.68f+(i%3)*0.08f);
    }
    for(int i=0;i<g->world.pickupCount;i++) if(!g->world.pickups[i].taken)
        PushRenderItem(items,&count,RENDER_PICKUP,i,g->world.pickups[i].tile,1.0f);
    PushRenderItem(items,&count,RENDER_SHRINE,0,g->world.shrine,1.0f);
    PushRenderItem(items,&count,RENDER_KEEPER,0,(Vector2){3.0f,10.0f},1.0f);
    Vector2 lumiPos=g->lumi.owned?g->lumi.pos:(Vector2){4.0f,12.0f};
    PushRenderItem(items,&count,RENDER_LUMI,0,lumiPos,1.0f);
    if(g->splash.owned)PushRenderItem(items,&count,RENDER_MOATAIL,0,g->splash.pos,1.0f);
    if(g->spark.owned)PushRenderItem(items,&count,RENDER_GECKO,0,g->spark.pos,1.0f);
    Vector2 fireflyGrid={10.6f+sinf(g->fireflyPhase*0.8f)*0.22f,6.2f+cosf(g->fireflyPhase*0.65f)*0.18f};
    PushRenderItem(items,&count,RENDER_FIREFLY,0,fireflyGrid,1.0f);
    if(!g->leaflingFed)PushRenderItem(items,&count,RENDER_LEAFLING,0,(Vector2){12.4f,8.0f},1.0f);
    PushRenderItem(items,&count,RENDER_PLAYER,0,g->player.pos,1.0f);

    for(int i=1;i<count;i++) {
        RenderItem key=items[i]; int j=i-1;
        while(j>=0 && items[j].depth>key.depth) { items[j+1]=items[j]; j--; }
        items[j+1]=key;
    }
    for(int i=0;i<count;i++) {
        RenderItem *it=&items[i]; Vector2 p=Iso(it->grid.x,it->grid.y,0);
        switch(it->kind) {
            case RENDER_TREE: DrawTree(p,it->scale,g->time); break;
            case RENDER_PROP:
                if(it->index==4)DrawProp(4,p,72,68);
                else if(it->index==5)DrawProp(5,p,38,38);
                else if(it->index==6)DrawProp(6,p,34,38);
                else if(it->index==7)DrawProp(7,p,62,112);
                else if(it->index==9)DrawProp(9,p,32,36);
                else if(it->index==10)DrawProp(10,p,46,38);
                else if(it->index==11)DrawProp(11,p,36,30);
                else if(it->index==12)DrawProp(12,p,42*it->scale,48*it->scale);
                else if(it->index==13)DrawProp(13,p,54*it->scale,34*it->scale);
                else if(it->index==14)DrawProp(14,p,42*it->scale,54*it->scale);
                else if(it->index==15)DrawProp(15,p,50*it->scale,42*it->scale);
                break;
            case RENDER_FIRE: DrawCampfire(p,g->time); break;
            case RENDER_PICKUP: DrawPickup(&g->world.pickups[it->index]); break;
            case RENDER_SHRINE: DrawShrine(p,g->time,g->shrineSolved); break;
            case RENDER_KEEPER: DrawKeeper(p,g->time); break;
            case RENDER_LUMI: DrawLumi(p,g->time,g->lumi.bob); break;
            case RENDER_MOATAIL: DrawMoatail(p,g->time); break;
            case RENDER_GECKO: DrawGecko(p,g->time); break;
            case RENDER_FIREFLY: DrawFirefly(p,g->time); break;
            case RENDER_LEAFLING: DrawLeafling(p,g->time); break;
            case RENDER_PLAYER: DrawPlayer(p,g->time,Vector2Length(g->player.vel)>.1f,g->player.facing); break;
        }
    }
    for(int i=0;i<MAX_PARTICLES;i++)if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];
        DrawCircleV(p->p,p->size*(p->life/p->maxLife),Fade(p->c,p->life/p->maxLife));
    }
    EndMode2D();
}
static void Panel(Rectangle r) { DrawRectangleRounded(r,.13f,18,C_PAPER);DrawRectangleRoundedLines(r,.13f,18,(Color){221,215,188,255});}
static void RenderHUD(Game *g) {
    Panel((Rectangle){20,18,405,112});
    DrawText("MIST ISLE  /  CHAPTER 01",40,32,15,(Color){119,145,116,255});
    DrawText("失落的灯塔",40,54,28,C_INK);
    DrawText(TextFormat("生命 %d    果实 %d    贝壳 %d    星晶 %d",(int)g->player.hp,g->player.berries,g->player.shells,g->player.crystals),40,96,17,(Color){104,119,93,255});
    Panel((Rectangle){1020,18,238,92});
    DrawText("伙伴图鉴",1040,32,17,(Color){119,145,116,255});
    DrawText(TextFormat("苔灯灵 %s",g->lumi.owned?"已同行":"未发现"),1040,57,16,C_INK);
    DrawText(TextFormat("沫尾兽 %s",g->splash.owned?"已同行":"未发现"),1040,79,16,C_INK);
    DrawText(TextFormat("鸣晶蜥 %s",g->spark.owned?"已同行":"未发现"),1040,101,16,C_INK);
    Panel((Rectangle){20,652,720,48});
    DrawText("WASD 移动   SHIFT 冲刺   E 互动   TAB 伙伴   J 手记   F5 保存 / F9 读取",38,667,14,(Color){91,115,96,255});
    if(!g->lumi.owned && Dist(g->player.pos,(Vector2){4,12})<1.65f) {
        Panel((Rectangle){530,585,220,40});
        DrawText("E  救助苔灯灵",550,596,17,(Color){91,115,96,255});
    } else if(Dist(g->player.pos,(Vector2){3,10})<2.0f) {
        Panel((Rectangle){530,585,220,40});
        DrawText("E  与守望者交谈",550,596,17,(Color){91,115,96,255});
    } else if(g->lumi.owned && Dist(g->player.pos,(Vector2){4.1f,12.5f})<1.5f) {
        Panel((Rectangle){530,585,220,40});
        DrawText("E  在营火旁休息",550,596,17,(Color){91,115,96,255});
    }
    if(g->toastTimer>0) {
        Panel((Rectangle){380,600,520,42});
        DrawText(g->toast,400,612,18,(Color){78,112,91,255});
    }
}
static void RenderJournal(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){250,105,780,510});
    DrawText("旅 人 手 记",300,135,34,(Color){74,105,83,255});
    DrawText("CHAPTER 01  /  THE LOST LIGHTHOUSE",303,179,15,(Color){132,145,112,255});
    DrawLine(300,207,980,207,(Color){220,211,183,255});
    DrawText("当前目标",303,230,21,(Color){104,135,94,255});
    DrawText(g->shrineSolved?"遗迹已被唤醒，前往庭院寻找守卫。":"寻找三位伙伴，收集潮汐贝壳并唤醒古代遗迹。",303,265,19,C_INK);
    DrawText("岛屿发现",303,320,21,(Color){104,135,94,255});
    DrawText(g->fireflyDiscovered?"萤芽  —  已记录：会在林间微光处出现。":"萤芽  —  未发现：留意林地里漂浮的微光。",303,355,18,C_INK);
    DrawText(g->leaflingFed?"叶团  —  已帮助：它为你找到了隐藏星晶。":"叶团  —  未帮助：它似乎很喜欢岛屿果实。",303,388,18,C_INK);
    DrawText("伙伴同行",303,445,21,(Color){104,135,94,255});
    DrawText(TextFormat("苔灯灵  %s       沫尾兽  %s       鸣晶蜥  %s",g->lumi.owned?"同行中":"未发现",g->splash.owned?"同行中":"未发现",g->spark.owned?"同行中":"未发现"),303,480,18,C_INK);
    DrawText("资源",303,525,21,(Color){104,135,94,255});
    DrawText(TextFormat("果实 %d     潮汐贝壳 %d     星晶 %d",g->player.berries,g->player.shells,g->player.crystals),303,560,18,C_INK);
    DrawText("F5 保存   F9 读取   按 J 或 ESC 关闭",760,570,16,(Color){132,137,111,255});
}
static void RenderDialogue(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.26f));
    Panel((Rectangle){125,510,1030,165});
    DrawCircle(190,510,43,(Color){157,201,143,255});
    DrawCircle(190,510,29,(Color){220,233,183,255});
    DrawCircle(180,506,3,C_INK);DrawCircle(200,506,3,C_INK);
    DrawText(g->dialogueSpeaker,245,535,18,(Color){125,149,112,255});
    DrawText(g->dialogue[g->dialoguePage],245,568,23,C_INK);
    DrawText("按 E / Enter / 鼠标左键继续",820,635,16,(Color){135,135,107,255});
}
static void RenderBattle(Game *g) {
    DrawRectangle(0,0,SW,SH,(Color){42,57,75,255});
    // stylized arena
    DrawCircle(640,300,205,(Color){76,99,108,255});
    DrawCircle(640,300,180,(Color){106,133,132,255});
    DrawCircle(640,300,145,(Color){130,158,143,255});
    DrawEllipse(405,440,70,22,(Color){42,54,64,120});
    DrawEllipse(875,270,92,26,(Color){42,54,64,120});
    DrawCircle(870,235,55,(Color){166,143,163,255});
    DrawTriangle((Vector2){830,206},(Vector2){855,150},(Vector2){879,206},(Color){204,181,205,255});
    DrawTriangle((Vector2){864,205},(Vector2){901,164},(Vector2){916,222},(Color){183,160,192,255});
    DrawCircle(852,232,6,C_INK);DrawCircle(888,232,6,C_INK);
    DrawCircle(852,230,2,WHITE);DrawCircle(888,230,2,WHITE);
    DrawText("遭遇战",55,40,30,WHITE);
    Enemy *e=&g->enemies[g->battleEnemy];
    DrawText(TextFormat("守卫 HP  %d / %d",(int)e->hp,(int)e->maxHp),760,80,22,WHITE);
    DrawRectangle(760,112,340,17,(Color){70,75,85,255});
    DrawRectangle(760,112,(int)(340*fmaxf(0,e->hp/e->maxHp)),17,C_PINK);
    DrawCircle(380,365,42,(Color){153,201,143,255});
    DrawCircle(380,365,30,(Color){219,232,180,255});
    DrawCircle(370,361,3,C_INK);DrawCircle(390,361,3,C_INK);
    DrawText("选择伙伴技能",70,525,23,WHITE);
    const char *opts[3]={"1  苔灯灵：星光治愈","2  沫尾兽：泡泡水流","3  鸣晶蜥：晶弧放电"};
    for(int i=0;i<3;i++) {
        Rectangle r={70,565+i*42,440,34};
        DrawRectangleRounded(r,.2f,8,i==g->battleChoice?(Color){255,226,158,255}:(Color){238,236,218,235});
        DrawText(opts[i],88,573+i*42,18,(Color){64,82,76,255});
    }
    DrawText(g->battleTurn==0?"选择技能并按 Enter":"敌方行动：按 Enter 继续",760,570,21,WHITE);
}
static void RenderTitle(Game *g) {
    ClearBackground((Color){187,222,211,255});
    for(int i=0;i<10;i++)DrawTree((Vector2){90+i*130,410+(i%3)*20},1.0f+(i%2)*.2f,g->time);
    DrawRectangle(0,0,SW,SH,Fade((Color){239,242,221,255},.25f));
    DrawText("雾 屿 回 响",390,155,64,(Color){69,104,83,255});
    DrawText("第一章  ·  失落的灯塔",445,235,28,(Color){107,141,115,255});
    DrawText("一段关于伙伴、勇气与重新点亮世界的旅程",350,315,23,(Color){89,119,103,255});
    DrawRectangleRounded((Rectangle){500,425,280,62},.2f,16,(Color){255,249,224,245});
    DrawText("按 Enter 开始旅程",535,444,23,(Color){70,115,87,255});
    DrawText(FileExists("mist_isle_save.dat")?"F9 继续上次旅程":"F9 读取存档",565,500,18,(Color){94,127,101,255});
    DrawText("CARTOON PIXEL ADVENTURE",470,650,17,(Color){110,143,119,255});
}
static void RenderEnd(Game *g) {
    ClearBackground((Color){35,59,72,255});
    DrawCircle(640,285,210,Fade(C_GOLD,.12f));
    DrawCircle(640,285,150,Fade(C_GOLD,.15f));
    DrawRectangle(610,140,60,310,(Color){210,205,168,255});
    DrawTriangle((Vector2){590,160},(Vector2){640,65},(Vector2){690,160},(Color){234,220,166,255});
    DrawCircle(640,160,23,C_GOLD);
    DrawText("灯塔重新亮起了",435,475,38,(Color){255,246,218,255});
    DrawText("伙伴们的光芒，照亮了整座岛屿。",405,535,23,(Color){218,231,211,255});
    DrawText("第一章 完成   ·   按 R 再次探索",430,620,20,(Color){255,222,154,255});
}
int main(void) {
    SetConfigFlags(FLAG_VSYNC_HINT|FLAG_MSAA_4X_HINT);
    InitWindow(SW,SH,"雾屿回响 - 第一章：失落的灯塔");
    if (FileExists("assets/characters/png/hero_explorer.png")) TEX_HERO=LoadTexture("assets/characters/png/hero_explorer.png");
    if (FileExists("assets/characters/png/hero_walk_4dir.png")) { TEX_HERO_WALK=LoadTexture("assets/characters/png/hero_walk_4dir.png"); SetTextureFilter(TEX_HERO_WALK,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/characters/png/lumi_idle_pixel.png")) { TEX_LUMI_PIXEL=LoadTexture("assets/characters/png/lumi_idle_pixel.png"); SetTextureFilter(TEX_LUMI_PIXEL,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/characters/png/moatail_idle_pixel.png")) { TEX_MOATAIL_PIXEL=LoadTexture("assets/characters/png/moatail_idle_pixel.png"); SetTextureFilter(TEX_MOATAIL_PIXEL,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/characters/png/gecko_idle_pixel.png")) { TEX_GECKO_PIXEL=LoadTexture("assets/characters/png/gecko_idle_pixel.png"); SetTextureFilter(TEX_GECKO_PIXEL,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/characters/png/camp_keeper.png")) { TEX_KEEPER=LoadTexture("assets/characters/png/camp_keeper.png"); SetTextureFilter(TEX_KEEPER,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/tiles/png/island_iso_tiles.png")) { TEX_ISO_TILES=LoadTexture("assets/tiles/png/island_iso_tiles.png"); SetTextureFilter(TEX_ISO_TILES,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/tiles/png/environment_props.png")) { TEX_PROPS=LoadTexture("assets/tiles/png/environment_props.png"); SetTextureFilter(TEX_PROPS,TEXTURE_FILTER_POINT); }
    if (FileExists("assets/characters/png/moss_lumi.png")) TEX_LUMI=LoadTexture("assets/characters/png/moss_lumi.png");
    if (FileExists("assets/characters/png/moatail.png")) TEX_MOATAIL=LoadTexture("assets/characters/png/moatail.png");
    if (FileExists("assets/characters/png/crystal_gecko.png")) TEX_GECKO=LoadTexture("assets/characters/png/crystal_gecko.png");
    SetTargetFPS(60);
    SetExitKey(KEY_NULL);
    Game g; ResetGame(&g,(int)time(NULL));
    while(!WindowShouldClose()) {
        float dt=GetFrameTime();if(dt>.05f)dt=.05f;
        if(g.scene==SCENE_TITLE) {
            g.time+=dt;
            if(IsKeyPressed(KEY_F9)) { if(LoadGame(&g)) { /* restored directly into play */ } }
            else if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT))InitPlay(&g);
        } else if(g.scene==SCENE_END) {
            if(IsKeyPressed(KEY_R))ResetGame(&g,g.lastSeed+1);
        } else {
            UpdateGame(&g,dt);
        }
        BeginDrawing();
        if(g.scene==SCENE_TITLE)RenderTitle(&g);
        else if(g.scene==SCENE_BATTLE)RenderBattle(&g);
        else if(g.scene==SCENE_END)RenderEnd(&g);
        else {RenderWorld(&g);RenderHUD(&g);if(g.scene==SCENE_DIALOGUE)RenderDialogue(&g);if(g.journalOpen)RenderJournal(&g);}
        EndDrawing();
    }
    if(TEX_HERO_WALK.id)UnloadTexture(TEX_HERO_WALK);
    if(TEX_LUMI_PIXEL.id)UnloadTexture(TEX_LUMI_PIXEL);
    if(TEX_MOATAIL_PIXEL.id)UnloadTexture(TEX_MOATAIL_PIXEL);
    if(TEX_GECKO_PIXEL.id)UnloadTexture(TEX_GECKO_PIXEL);
    if(TEX_KEEPER.id)UnloadTexture(TEX_KEEPER);
    if(TEX_ISO_TILES.id)UnloadTexture(TEX_ISO_TILES);
    if(TEX_PROPS.id)UnloadTexture(TEX_PROPS);
    if(TEX_HERO.id)UnloadTexture(TEX_HERO);
    if(TEX_LUMI.id)UnloadTexture(TEX_LUMI);
    if(TEX_MOATAIL.id)UnloadTexture(TEX_MOATAIL);
    if(TEX_GECKO.id)UnloadTexture(TEX_GECKO);
    CloseWindow();return 0;
}