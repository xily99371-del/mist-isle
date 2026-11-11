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
#define MAX_PARTICLES 220
#define PI2 6.28318530718f

typedef enum { SCENE_TITLE, SCENE_PLAY, SCENE_DIALOGUE, SCENE_BATTLE, SCENE_END } Scene;
typedef enum { AREA_CAMP, AREA_GROVE, AREA_RUINS, AREA_LIGHTHOUSE } Area;
typedef enum { ENEMY_MOTE, ENEMY_GUARD, ENEMY_BOSS } EnemyType;
typedef enum { SKILL_GLOW, SKILL_SPLASH, SKILL_SPARK } SkillType;

typedef struct { Vector2 p, v; float life, maxLife, size; Color c; } Particle;
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
    Pickup pickups[18];
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
} Game;

static Color C_BG={207,232,222,255}, C_INK={67,83,73,255};
static Color C_PAPER={255,250,232,246}, C_GREEN={112,166,116,255};
static Color C_DARKGREEN={70,119,86,255}, C_GOLD={255,219,133,255};
static Color C_BLUE={119,190,208,255}, C_LILAC={190,166,220,255};
static Color C_PINK={239,169,177,255}, C_RED={217,105,106,255};
static Texture2D TEX_HERO={0}, TEX_LUMI={0}, TEX_MOATAIL={0}, TEX_GECKO={0};
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
static void SetDialogue(Game *g,const char *a,const char *b,const char *c) {
    g->dialogueCount=0;
    if(a){snprintf(g->dialogue[g->dialogueCount++],180,"%s",a);}
    if(b){snprintf(g->dialogue[g->dialogueCount++],180,"%s",b);}
    if(c){snprintf(g->dialogue[g->dialogueCount++],180,"%s",c);}
    g->dialoguePage=0; g->scene=SCENE_DIALOGUE;
}
static void AddPickup(World *w,int kind,float x,float y) {
    if(w->pickupCount>=18)return;
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
    w->camp=(Vector2){3,12}; w->groveGate=(Vector2){8,8};
    w->ruinGate=(Vector2){14,9}; w->shrine=(Vector2){17,5};
    w->bossPos=(Vector2){18,13};
    AddPickup(w,0,6,12); AddPickup(w,0,8,6); AddPickup(w,0,11,11);
    AddPickup(w,1,10,5); AddPickup(w,1,15,7); AddPickup(w,1,18,8);
    AddPickup(w,2,12,13); AddPickup(w,2,16,12);
    AddPickup(w,3,7,4); AddPickup(w,3,19,5);
    for(int i=0;i<8;i++) AddPickup(w,0,5+(i*3)%13,4+(i*5)%10);
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
static void ResetGame(Game *g,int seed) {
    memset(g,0,sizeof(*g)); g->scene=SCENE_TITLE; g->area=AREA_CAMP;
    g->lastSeed=seed; BuildWorld(&g->world,seed);
    g->player.pos=g->world.camp; g->player.hp=g->player.maxHp=100; g->player.stamina=100;
    g->lumi=(Buddy){.pos={2.3f,12.4f},.owned=false,.level=1};
    g->splash=(Buddy){.pos={8,7},.owned=false,.level=1};
    g->spark=(Buddy){.pos={15,8},.owned=false,.level=1};
    g->cam.offset=(Vector2){SW/2.0f,SH/2.0f+20}; g->cam.zoom=1;
}
static void InitPlay(Game *g) {
    g->scene=SCENE_PLAY; g->area=AREA_CAMP; g->player.pos=g->world.camp;
    SetDialogue(g,"你在海岸边醒来，远处的灯塔已经熄灭。",
        "一只小小的苔灯灵躲在营火旁。它似乎受了伤。",
        "靠近它，按 E 互动。WASD 移动，鼠标点击观察。");
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
    for(int i=0;i<MAX_PARTICLES;i++) if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];p->life-=dt;p->p=Vector2Add(p->p,Vector2Scale(p->v,dt));p->v.y-=0.3f*dt;
    }
    if(g->scene==SCENE_TITLE) {
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
                return;
            }
        }
        // Water companion encounter.
        if(!g->splash.owned&&Dist(p->pos,(Vector2){9,5})<1.6f) {
            if(p->berries>=1){p->berries--;AddBuddyFriend(g,1);SetDialogue(g,"沫尾兽被果实吸引，开心地绕着你转了一圈。","它可以喷水清理蒸汽，也能让遗迹机关显形。","按 TAB 切换伙伴。");}
            else Toast(g,"沫尾兽想吃一颗岛屿果实。");
            return;
        }
        // Crystal companion encounter.
        if(!g->spark.owned&&Dist(p->pos,(Vector2){15,7})<1.6f) {
            if(p->crystals>=1){p->crystals--;AddBuddyFriend(g,2);SetDialogue(g,"鸣晶蜥吸收了星晶，背上的宝石依次亮起。","它能为古代装置供能，也能在战斗中放电。","按 TAB 切换伙伴。");}
            else Toast(g,"鸣晶蜥需要一枚星晶才愿意同行。");
            return;
        }
        // Ruin puzzle.
        if(Dist(p->pos,g->world.shrine)<1.7f&&!g->shrineSolved) {
            if(g->lumi.owned&&g->splash.owned&&g->spark.owned&&p->shells>=1) {
                g->shrineSolved=true;p->shells--;
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
            SetDialogue(g,"营地守望者：灯塔熄灭后，岛上的小生灵都变得不安。","找到三位伙伴，唤醒潮汐遗迹，灯塔就能重新点亮。","果实、贝壳和星晶散落在岛上，按 E 采集。");
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
static void RenderWorld(Game *g) {
    ClearBackground(C_BG);
    BeginMode2D(g->cam);
    // Soft distant island backdrop.
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        Color c=((x+y)&1)?(Color){151,198,144,255}:(Color){160,205,150,255};
        if(g->world.kind[y][x]==1)c=(Color){116,157,117,255};
        if(g->world.kind[y][x]==2)c=(Color){192,181,143,255};
        if(x>=8&&x<=11&&y>=3&&y<=7)c=(Color){133,190,175,255};
        if(x>=14&&x<=19&&y>=4&&y<=10)c=(Color){184,181,151,255};
        int tile=(g->world.kind[y][x]==1)?2:(g->world.kind[y][x]==2)?3:((x+y)%3);
        if(x>=8&&x<=11&&y>=3&&y<=7)tile=4;
        if(x>=14&&x<=19&&y>=4&&y<=10)tile=5;
        DrawIsoTile(x,y,tile,c);
        Vector2 p=Iso(x,y,0);
        if(g->world.kind[y][x]==1)DrawCircleV((Vector2){p.x,p.y-5},7,(Color){102,143,107,255});
    }
    // Water pool and stepping stones.
    for(int y=3;y<6;y++)for(int x=2;x<6;x++)DrawIsoTile(x,y,4,(Color){119,191,199,255});
    for(int i=0;i<15;i++) {
        int x=2+(i*7)%17,y=2+(i*11)%13;
        if(g->world.kind[y][x]!=1)DrawTree(Iso(x,y,0),.72f+(i%3)*.1f,g->time);
    }
    // Storybook props: campsite, supply crate, signpost and distant lighthouse.
    DrawProp(4,Iso(3.2f,11.4f,0),72,68);
    DrawProp(5,Iso(4.6f,10.8f,0),38,38);
    DrawProp(6,Iso(5.2f,11.7f,0),34,38);
    DrawProp(7,Iso(19.0f,2.2f,0),62,112);
    for(int i=0;i<g->world.pickupCount;i++)if(!g->world.pickups[i].taken)DrawPickup(&g->world.pickups[i]);
    DrawShrine(Iso(g->world.shrine.x,g->world.shrine.y,0),g->time,g->shrineSolved);
    if(g->lumi.owned)DrawLumi(Iso(g->lumi.pos.x,g->lumi.pos.y,0),g->time,g->lumi.bob);
    if(g->splash.owned) {
        Vector2 p=Iso(g->splash.pos.x,g->splash.pos.y,0);
        DrawEllipse((int)p.x,(int)p.y+6,15,5,(Color){75,112,130,75});
        if(TEX_MOATAIL_PIXEL.id) {
            int frame=(int)(g->time*2.5f)%2;
            Rectangle src={(float)(frame*32),0,32,32};
            Rectangle dst={p.x-18,p.y-38-sinf(g->time*4)*2.0f,36,36};
            DrawTexturePro(TEX_MOATAIL_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
        } else if(TEX_MOATAIL.id) DrawPortrait(TEX_MOATAIL,p,58,58,sinf(g->time*4)*2.5f);
        else {
            DrawCircleV((Vector2){p.x,p.y-11},12,(Color){121,202,215,255});
            DrawCircleV((Vector2){p.x-5,p.y-20},5,(Color){151,220,224,255});
            DrawCircleV((Vector2){p.x+5,p.y-20},5,(Color){151,220,224,255});
            DrawCircleV((Vector2){p.x-4,p.y-12},2,(Color){52,83,94,255});
            DrawCircleV((Vector2){p.x+4,p.y-12},2,(Color){52,83,94,255});
        }
    }
    if(g->spark.owned) {
        Vector2 p=Iso(g->spark.pos.x,g->spark.pos.y,0);
        DrawEllipse((int)p.x,(int)p.y+6,15,5,(Color){83,91,130,75});
        if(TEX_GECKO_PIXEL.id) {
            int frame=(int)(g->time*2.5f)%2;
            Rectangle src={(float)(frame*32),0,32,32};
            Rectangle dst={p.x-18,p.y-38-sinf(g->time*5)*2.0f,36,36};
            DrawTexturePro(TEX_GECKO_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
        } else if(TEX_GECKO.id) DrawPortrait(TEX_GECKO,p,58,58,sinf(g->time*5)*2.0f);
        else {
            DrawCircleV((Vector2){p.x,p.y-10},11,(Color){155,154,214,255});
            DrawTriangle((Vector2){p.x-7,p.y-17},(Vector2){p.x,p.y-32},(Vector2){p.x+7,p.y-17},(Color){179,218,229,255});
            DrawCircleV((Vector2){p.x-4,p.y-11},2,(Color){50,60,88,255});
            DrawCircleV((Vector2){p.x+4,p.y-11},2,(Color){50,60,88,255});
        }
    }
    DrawPlayer(Iso(g->player.pos.x,g->player.pos.y,0),g->time,Vector2Length(g->player.vel)>.1f,g->player.facing);
    for(int i=0;i<MAX_PARTICLES;i++)if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];
        DrawCircleV(p->p,p->size* (p->life/p->maxLife),Fade(p->c,p->life/p->maxLife));
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
    DrawText("WASD 移动   SHIFT 冲刺   E 互动/采集   TAB 切换伙伴",38,667,17,(Color){91,115,96,255});
    if(g->toastTimer>0) {
        Panel((Rectangle){380,600,520,42});
        DrawText(g->toast,400,612,18,(Color){78,112,91,255});
    }
}
static void RenderDialogue(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.26f));
    Panel((Rectangle){125,510,1030,165});
    DrawCircle(190,510,43,(Color){157,201,143,255});
    DrawCircle(190,510,29,(Color){220,233,183,255});
    DrawCircle(180,506,3,C_INK);DrawCircle(200,506,3,C_INK);
    DrawText("苔灯灵",245,535,18,(Color){125,149,112,255});
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
    DrawText("CARTOON 2.5D ADVENTURE",490,650,17,(Color){110,143,119,255});
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
            if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT))InitPlay(&g);
        } else if(g.scene==SCENE_END) {
            if(IsKeyPressed(KEY_R))ResetGame(&g,g.lastSeed+1);
        } else {
            UpdateGame(&g,dt);
        }
        BeginDrawing();
        if(g.scene==SCENE_TITLE)RenderTitle(&g);
        else if(g.scene==SCENE_BATTLE)RenderBattle(&g);
        else if(g.scene==SCENE_END)RenderEnd(&g);
        else {RenderWorld(&g);RenderHUD(&g);if(g.scene==SCENE_DIALOGUE)RenderDialogue(&g);}
        EndDrawing();
    }
    if(TEX_HERO_WALK.id)UnloadTexture(TEX_HERO_WALK);
    if(TEX_LUMI_PIXEL.id)UnloadTexture(TEX_LUMI_PIXEL);
    if(TEX_MOATAIL_PIXEL.id)UnloadTexture(TEX_MOATAIL_PIXEL);
    if(TEX_GECKO_PIXEL.id)UnloadTexture(TEX_GECKO_PIXEL);
    if(TEX_ISO_TILES.id)UnloadTexture(TEX_ISO_TILES);
    if(TEX_PROPS.id)UnloadTexture(TEX_PROPS);
    if(TEX_HERO.id)UnloadTexture(TEX_HERO);
    if(TEX_LUMI.id)UnloadTexture(TEX_LUMI);
    if(TEX_MOATAIL.id)UnloadTexture(TEX_MOATAIL);
    if(TEX_GECKO.id)UnloadTexture(TEX_GECKO);
    CloseWindow();return 0;
}