#include "game.h"

static Vector2 BuddyFollowOffset(int id,int facing);
#include "area_maps.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <time.h>
Color C_BG={207,232,222,255}, C_INK={67,83,73,255};
Color C_PAPER={255,250,232,246}, C_GREEN={112,166,116,255};
Color C_DARKGREEN={70,119,86,255}, C_GOLD={255,219,133,255};
Color C_BLUE={119,190,208,255}, C_LILAC={190,166,220,255};
Color C_PINK={239,169,177,255}, C_RED={217,105,106,255};
static Texture2D TEX_HERO={0}, TEX_LUMI={0}, TEX_MOATAIL={0}, TEX_GECKO={0}, TEX_KEEPER={0};
static Texture2D TEX_HERO_WALK={0}, TEX_LUMI_PIXEL={0}, TEX_MOATAIL_PIXEL={0}, TEX_GECKO_PIXEL={0}, TEX_ISO_TILES={0}, TEX_PROPS={0};
static Vector2 Iso(float x,float y,float z) {
    return (Vector2){(x-y)*31.0f,(x+y)*15.5f-z};
}
static Vector2 WorldScreen(Vector2 p) { return (Vector2){(p.x+0.5f)*TILE_SIZE,(p.y+0.5f)*TILE_SIZE}; }
static Vector2 ClampCameraTarget(Game *g,Vector2 target) {
    float zoom=g->cam.zoom>0.01f?g->cam.zoom:1.0f;
    float viewW=(float)SW/zoom, viewH=(float)SH/zoom;
    if(viewW>=WORLD_PIXEL_W)target.x=WORLD_PIXEL_W*0.5f;
    else target.x=Clamp(target.x,viewW*0.5f,WORLD_PIXEL_W-viewW*0.5f);
    if(viewH>=WORLD_PIXEL_H)target.y=WORLD_PIXEL_H*0.5f;
    else target.y=Clamp(target.y,viewH*0.5f,WORLD_PIXEL_H-viewH*0.5f);
    return target;
}
static void SnapCameraToPlayer(Game *g) {
    g->cam.target=ClampCameraTarget(g,WorldScreen(g->player.pos));
}
static Vector2 MoveTowardsConstant(Vector2 current,Vector2 target,float maxDistance) {
    Vector2 delta=Vector2Subtract(target,current);
    float distance=Vector2Length(delta);
    if(distance<=maxDistance || distance<0.0001f)return target;
    return Vector2Add(current,Vector2Scale(delta,maxDistance/distance));
}
static void UpdatePlayerCamera(Game *g,float dt) {
    Vector2 desired=ClampCameraTarget(g,WorldScreen(g->player.pos));
    // Constant-speed lock-on: no dead zone and no exponential ease-in/out.
    g->cam.target=MoveTowardsConstant(g->cam.target,desired,680.0f*dt);
    g->cam.target=ClampCameraTarget(g,g->cam.target);
}
static Vector2 LogicalMousePosition(void) {
    Vector2 mouse=GetMousePosition();
    float sx=(float)GetScreenWidth()/(float)SW;
    float sy=(float)GetScreenHeight()/(float)SH;
    float scale=sx<sy?sx:sy;
    if(scale<0.001f)scale=1.0f;
    float ox=((float)GetScreenWidth()-(float)SW*scale)*.5f;
    float oy=((float)GetScreenHeight()-(float)SH*scale)*.5f;
    return (Vector2){(mouse.x-ox)/scale,(mouse.y-oy)/scale};
}
static void Toast(Game *g,const char *s) {
    snprintf(g->toast,sizeof(g->toast),"%s",s); g->toastTimer=2.4f;
}
float Dist(Vector2 a,Vector2 b) { return Vector2Distance(a,b); }
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
static int DialoguePortraitForSpeaker(const char *speaker) {
    if(!speaker)return 0;
    if(strcmp(speaker,"守望者")==0)return 1;
    if(strcmp(speaker,"灯塔守卫")==0)return 2;
    if(strcmp(speaker,"苔灯灵")==0||strcmp(speaker,"苔藓灵")==0)return 3;
    if(strcmp(speaker,"沫尾兽")==0||strcmp(speaker,"沫尾")==0)return 4;
    if(strcmp(speaker,"鸣晶蜥")==0)return 5;
    if(strncmp(speaker,"旅人",strlen("旅人"))==0||strcmp(speaker,"主角")==0)return 0;
    return 0;
}
static void SetDialogueSequence(Game *g,const char *const *speakers,const char *const *lines,int count,bool startsBossBattle) {
    g->dialogueCount=count<4?count:4;
    g->dialoguePage=0;
    g->dialogueStartsBossBattle=startsBossBattle;
    for(int i=0;i<g->dialogueCount;i++) {
        const char *speaker=speakers&&speakers[i]?speakers[i]:"旁白";
        snprintf(g->dialogueLineSpeaker[i],sizeof(g->dialogueLineSpeaker[i]),"%s",speaker);
        snprintf(g->dialogue[i],sizeof(g->dialogue[i]),"%s",lines&&lines[i]?lines[i]:"");
        g->dialoguePortrait[i]=DialoguePortraitForSpeaker(speaker);
    }
    if(g->dialogueCount>0)snprintf(g->dialogueSpeaker,sizeof(g->dialogueSpeaker),"%s",g->dialogueLineSpeaker[0]);
    g->scene=SCENE_DIALOGUE;
}
static void SetDialogueAs(Game *g,const char *speaker,const char *a,const char *b,const char *c) {
    const char *speakers[3]={speaker?speaker:"旁白",speaker?speaker:"旁白",speaker?speaker:"旁白"};
    const char *lines[3]={a,b,c};
    int count=(a?1:0)+(b?1:0)+(c?1:0);
    const char *compact[3];int n=0;
    for(int i=0;i<3;i++)if(lines[i])compact[n++]=lines[i];
    SetDialogueSequence(g,speakers,compact,count,false);
}
static void SetDialogue(Game *g,const char *a,const char *b,const char *c) {
    SetDialogueAs(g,"苔灯灵",a,b,c);
}
static int TerrainKindFromAtlasIndex(unsigned char index);
static void AddPickup(World *w,int kind,float x,float y) {
    if(w->pickupCount>=MAX_PICKUPS)return;
    // Place every pickup on the nearest free, reachable tile; relocate it when a prop,
    // tree trunk or water cell occupies its authored position.
    int ox=(int)roundf(x),oy=(int)roundf(y),bestX=-1,bestY=-1;
    float best=1.0e9f;
    for(int radius=0;radius<=4;radius++) {
        for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++) {
            if(abs(dx)+abs(dy)!=radius)continue;
            int tx=ox+dx,ty=oy+dy;
            if(tx<1||ty<1||tx>=MAPW-1||ty>=MAPH-1)continue;
            if(w->solid[ty][tx]||TerrainKindFromAtlasIndex(w->terrain[ty][tx])==4)continue;
            bool occupied=false;
            for(int i=0;i<w->pickupCount;i++)
                if((int)roundf(w->pickups[i].tile.x)==tx&&(int)roundf(w->pickups[i].tile.y)==ty) {
                    occupied=true;break;
                }
            // Keep collectible sprites outside the visual footprint of tall structures,
            // broad canopies and altar silhouettes, not merely outside their trunk collision.
            for(int i=0;!occupied&&i<w->objectCount;i++) {
                MapObject *o=&w->objects[i];
                float dx=fabsf((float)tx-o->pos.x),dy=fabsf((float)ty-o->pos.y);
                float rx=.0f,ry=.0f;
                if(o->type==302||o->type==500||o->type==501){rx=3.1f;ry=2.8f;}
                else if(o->type==303||o->type==22){rx=2.1f;ry=1.8f;}
                else if(o->type>=1000&&o->type<1003){rx=1.45f;ry=1.55f;}
                else if(o->type==24||o->type==33||o->type==35){rx=1.8f;ry=1.8f;}
                else if(o->type==25||o->type==27||o->type==28||o->type==31){rx=1.5f;ry=1.5f;}
                else if(o->type==26||o->type==29||o->type==30||o->type==34){rx=1.1f;ry=1.1f;}
                else if(o->type==20||o->type==21||o->type==32){rx=1.5f;ry=1.1f;}
                else if(o->type>=200&&o->type<300){rx=1.4f;ry=1.4f;}
                else if(o->type==2000){rx=.9f;ry=.9f;}
                if(rx>0.0f&&dx<rx&&dy<ry)occupied=true;
            }
            if(occupied)continue;
            float score=(float)(dx*dx+dy*dy)+(float)ty*0.0001f;
            if(score<best){best=score;bestX=tx;bestY=ty;}
        }
        if(bestX>=0)break;
    }
    if(bestX<0)return;
    int index=w->pickupCount;
    w->pickups[index]=(Pickup){{(float)bestX,(float)bestY},kind,false,(float)index*0.7f};
    w->pickupCount++;
}
static int TerrainKindFromAtlasIndex(unsigned char index) {
    int row=(int)index/4;
    if(row>=9)return 4; // water
    if(row>=6)return 3; // stone
    if(row>=3)return 2; // sand
    return 0;           // grass
}
static void SwitchArea(Game *g,Area area);
static void BuildAreaWorld(Game *g,Area area) {
    World *w=&g->world;
    memset(w,0,sizeof(*w));
    w->seed=g->lastSeed+(int)area*7919;
    g->area=area;
    w->pickupCount=0;
    w->objectCount=0;
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        w->terrain[y][x]=AREA_GROUND[(int)area][y][x];
        w->solid[y][x]=AREA_COLLISION[(int)area][y][x];
    }
    w->camp=AREA_MARKERS[(int)area][0];
    w->returnPoint=AREA_MARKERS[(int)area][1];
    w->groveGate=AREA_MARKERS[(int)area][2];
    w->ruinGate=AREA_MARKERS[(int)area][3];
    w->shrine=AREA_MARKERS[(int)area][4];
    w->bossPos=AREA_MARKERS[(int)area][5];
    w->objectCount=AREA_OBJECT_COUNTS[(int)area];
    for(int i=0;i<w->objectCount;i++)w->objects[i]=AREA_OBJECTS[(int)area][i];

    // Resource placement remains progression-driven; terrain, static objects,
    // collision and all major story markers are authored in Tiled.
    if(area==AREA_CAMP) {
        AddPickup(w,0,8,8);AddPickup(w,0,12,8);AddPickup(w,0,14,6);
        AddPickup(w,0,5,9);AddPickup(w,1,5,10);AddPickup(w,2,12,9);
    } else if(area==AREA_GROVE) {
        AddPickup(w,0,6,7);AddPickup(w,0,12,6);AddPickup(w,0,12,7);
        AddPickup(w,1,4,10);AddPickup(w,2,10,10);
    } else if(area==AREA_RUINS) {
        AddPickup(w,1,7,7);AddPickup(w,1,14,6);
        AddPickup(w,2,7,8);AddPickup(w,2,12,10);
    } else {
        AddPickup(w,0,7,7);AddPickup(w,1,11,7);AddPickup(w,2,10,10);
    }
    for(int i=0;i<w->pickupCount;i++) {
        int px=(int)w->pickups[i].tile.x, py=(int)w->pickups[i].tile.y;
        if(px>0&&py>0&&px<MAPW-1&&py<MAPH-1)w->solid[py][px]=0;
    }
}
static void SwitchArea(Game *g,Area area) {
    Area previousArea=g->area;
    BuildAreaWorld(g,area);
    /* Firefly weather belongs exclusively to Glowglade Grove. */
    if(area==AREA_GROVE) {
        g->weather=2;
        g->weatherClock=0.0f;
        g->weatherFade=0.0f;
    } else if(previousArea==AREA_GROVE) {
        g->weather=0;
        g->weatherClock=0.0f;
        g->weatherFade=0.0f;
    }
    g->player.pos=(area==AREA_CAMP)?g->world.returnPoint:g->world.camp;
    for(int i=0;i<g->world.pickupCount && i<MAX_PICKUPS;i++)g->world.pickups[i].taken=g->areaPickupTaken[(int)area][i]!=0;
    g->enemyCount=0;
    g->lumi.pos=Vector2Add(g->player.pos,BuddyFollowOffset(0,g->player.facing));
    g->splash.pos=Vector2Add(g->player.pos,BuddyFollowOffset(1,g->player.facing));
    g->spark.pos=Vector2Add(g->player.pos,BuddyFollowOffset(2,g->player.facing));
    g->transition=.35f;
    SnapCameraToPlayer(g);
}

static bool InRect(Vector2 p,float left,float top,float right,float bottom) {
    return p.x>=left && p.x<=right && p.y>=top && p.y<=bottom;
}
static bool SolidPoint(Game *g,Vector2 p) {
    int x=(int)floorf(p.x+0.5f),y=(int)floorf(p.y+0.5f);
    if(x<1||y<1||x>=MAPW-1||y>=MAPH-1)return true;
    if(g->world.solid[y][x])return true;
    if(TerrainKindFromAtlasIndex(g->world.terrain[y][x])==4)return true;

    // Dynamic characters and large interactables have compact ground footprints.
    if(g->area==AREA_CAMP) {
        if(InRect(p,6.62f,6.62f,7.38f,7.38f))return true; // Watcher
        if(!g->lumi.owned&&InRect(p,5.62f,7.62f,6.38f,8.38f))return true;
    } else if(g->area==AREA_GROVE) {
        if(!g->splash.owned&&InRect(p,13.62f,6.62f,14.38f,7.38f))return true;
    } else if(g->area==AREA_RUINS) {
        if(!g->spark.owned&&InRect(p,12.62f,7.62f,13.38f,8.38f))return true;
        if(InRect(p,g->world.shrine.x-.58f,g->world.shrine.y-.48f,
                    g->world.shrine.x+.58f,g->world.shrine.y+.48f))return true;
    } else {
        if(InRect(p,g->world.shrine.x-.58f,g->world.shrine.y-.48f,
                    g->world.shrine.x+.58f,g->world.shrine.y+.48f))return true;
        if(!g->bossDefeated&&InRect(p,g->world.bossPos.x-.62f,g->world.bossPos.y-.48f,
                                    g->world.bossPos.x+.62f,g->world.bossPos.y+.48f))return true;
    }
    return false;
}
static bool Solid(Game *g,Vector2 p) {
    // Eight perimeter samples approximate the full body footprint and prevent diagonal corner clipping.
    const float r=.31f;
    const float d=.219f;
    return SolidPoint(g,p) ||
        SolidPoint(g,(Vector2){p.x-r,p.y}) ||
        SolidPoint(g,(Vector2){p.x+r,p.y}) ||
        SolidPoint(g,(Vector2){p.x,p.y-r}) ||
        SolidPoint(g,(Vector2){p.x,p.y+r}) ||
        SolidPoint(g,(Vector2){p.x-d,p.y-d}) ||
        SolidPoint(g,(Vector2){p.x+d,p.y-d}) ||
        SolidPoint(g,(Vector2){p.x-d,p.y+d}) ||
        SolidPoint(g,(Vector2){p.x+d,p.y+d});
}
static void MovePlayerWithCollision(Game *g,Vector2 delta) {
    float distance=Vector2Length(delta);
    int steps=(int)ceilf(distance/0.08f);
    if(steps<1)steps=1;
    Vector2 step=Vector2Scale(delta,1.0f/(float)steps);
    for(int i=0;i<steps;i++) {
        Vector2 next=g->player.pos;
        next.x+=step.x;
        if(!Solid(g,next))g->player.pos.x=next.x;
        next=g->player.pos;
        next.y+=step.y;
        if(!Solid(g,next))g->player.pos.y=next.y;
    }
}
static void AddEnemy(Game *g,EnemyType t,float x,float y,float hp) {
    if(g->enemyCount>=MAX_ENEMIES)return;
    g->enemies[g->enemyCount++]=(Enemy){{x,y},t,hp,hp,(float)GetRandomValue(0,628)/100.0f,1.2f,true};
}
void GameReset(Game *g,int seed);
void GameSave(Game *g) {
    SaveData s={0};
    s.magic=0x4D495354; s.version=5; s.seed=g->lastSeed; s.area=(int)g->area;
    s.px=g->player.pos.x; s.py=g->player.pos.y; s.hp=g->player.hp;
    s.berries=g->player.berries; s.shells=g->player.shells; s.crystals=g->player.crystals;
    s.lumi=g->lumi.owned; s.splash=g->splash.owned; s.spark=g->spark.owned; s.selected=g->selectedBuddy;
    s.shrineSolved=g->shrineSolved; s.bossDefeated=g->bossDefeated;
    s.fireflyDiscovered=g->fireflyDiscovered; s.leaflingFed=g->leaflingFed; s.groveMoteDefeated=g->groveMoteDefeated;
    s.lumiLevel=g->lumi.level;s.lumiXp=g->lumi.xp;s.splashLevel=g->splash.level;s.splashXp=g->splash.xp;s.sparkLevel=g->spark.level;s.sparkXp=g->spark.xp;
    s.lumiHp=g->lumi.hp;s.splashHp=g->splash.hp;s.sparkHp=g->spark.hp;
    for(int a=0;a<4;a++)for(int i=0;i<MAX_PICKUPS;i++)s.pickupTaken[a][i]=g->areaPickupTaken[a][i];
    if(!SaveFileData("mist_isle_save.dat",&s,(int)sizeof(s)))Toast(g,"保存失败：无法写入存档文件。");
}
bool GameLoad(Game *g) {
    int size=0; unsigned char *raw=LoadFileData("mist_isle_save.dat",&size);
    if(!raw){Toast(g,"没有找到有效存档。");return false;}
    SaveData s={0};
    if(size==(int)sizeof(SaveData)) memcpy(&s,raw,sizeof(s));
    else if(size==(int)sizeof(SaveDataV4)) {
        SaveDataV4 old;memcpy(&old,raw,sizeof(old));
        s.magic=old.magic;s.version=old.version;s.seed=old.seed;s.area=old.area;
        s.px=old.px;s.py=old.py;s.hp=old.hp;s.berries=old.berries;s.shells=old.shells;s.crystals=old.crystals;
        s.lumi=old.lumi;s.splash=old.splash;s.spark=old.spark;s.selected=old.selected;
        s.shrineSolved=old.shrineSolved;s.bossDefeated=old.bossDefeated;s.fireflyDiscovered=old.fireflyDiscovered;
        s.leaflingFed=old.leaflingFed;s.groveMoteDefeated=old.groveMoteDefeated;
        s.lumiLevel=old.lumiLevel;s.lumiXp=old.lumiXp;s.splashLevel=old.splashLevel;s.splashXp=old.splashXp;s.sparkLevel=old.sparkLevel;s.sparkXp=old.sparkXp;
        memcpy(s.pickupTaken,old.pickupTaken,sizeof(s.pickupTaken));
        s.lumiHp=s.splashHp=s.sparkHp=100.0f;
    } else {UnloadFileData(raw);Toast(g,"存档文件长度不正确。");return false;}
    UnloadFileData(raw);
    if(s.magic!=0x4D495354 || (s.version!=4&&s.version!=5)){Toast(g,"存档版本不兼容。");return false;}
    GameReset(g,s.seed);
    SwitchArea(g,(Area)Clamp(s.area,AREA_CAMP,AREA_LIGHTHOUSE));
    g->scene=SCENE_PLAY; g->player.pos=(Vector2){s.px,s.py};SnapCameraToPlayer(g);g->player.hp=fmaxf(1.0f,fminf(100.0f,s.hp));
    g->player.berries=s.berries;g->player.shells=s.shells;g->player.crystals=s.crystals;
    g->lumi.owned=s.lumi!=0;g->splash.owned=s.splash!=0;g->spark.owned=s.spark!=0;g->selectedBuddy=s.selected;
    g->shrineSolved=s.shrineSolved!=0;g->bossDefeated=s.bossDefeated!=0;
    g->fireflyDiscovered=s.fireflyDiscovered!=0;g->leaflingFed=s.leaflingFed!=0;g->groveMoteDefeated=s.groveMoteDefeated!=0;
    g->lumi.level=s.lumiLevel;g->lumi.xp=s.lumiXp;g->splash.level=s.splashLevel;g->splash.xp=s.splashXp;g->spark.level=s.sparkLevel;g->spark.xp=s.sparkXp;
    g->lumi.hp=fmaxf(0,fminf(g->lumi.maxHp,s.lumiHp));g->splash.hp=fmaxf(0,fminf(g->splash.maxHp,s.splashHp));g->spark.hp=fmaxf(0,fminf(g->spark.maxHp,s.sparkHp));
    for(int a=0;a<4;a++)for(int i=0;i<MAX_PICKUPS;i++)g->areaPickupTaken[a][i]=s.pickupTaken[a][i];
    for(int i=0;i<g->world.pickupCount && i<MAX_PICKUPS;i++)g->world.pickups[i].taken=g->areaPickupTaken[(int)g->area][i]!=0;
    if(g->lumi.owned)g->lumi.pos=Vector2Add(g->player.pos,BuddyFollowOffset(0,g->player.facing));
    if(g->splash.owned)g->splash.pos=Vector2Add(g->player.pos,BuddyFollowOffset(1,g->player.facing));
    if(g->spark.owned)g->spark.pos=Vector2Add(g->player.pos,BuddyFollowOffset(2,g->player.facing));
    Toast(g,"已读取上次的旅程。"); return true;
}
void GameReset(Game *g,int seed) {
    memset(g,0,sizeof(*g)); g->scene=SCENE_TITLE; g->area=AREA_CAMP;
    g->lastSeed=seed; memset(&g->world,0,sizeof(g->world));
    g->player.pos=(Vector2){4,13}; g->player.hp=g->player.maxHp=100; g->player.stamina=100;
    g->lumi=(Buddy){.pos={2.3f,12.4f},.hp=100,.maxHp=100,.owned=false,.level=1};
    g->splash=(Buddy){.pos={8,7},.hp=100,.maxHp=100,.owned=false,.level=1};
    g->spark=(Buddy){.pos={15,8},.hp=100,.maxHp=100,.owned=false,.level=1};
    g->cam.offset=(Vector2){SW/2.0f,SH/2.0f}; g->cam.zoom=1.10f;
    g->fireflyPhase=0; g->dayClock=0.02f; g->weatherClock=0; g->weather=0; g->weatherFade=1.0f; g->journalOpen=false; g->inventoryOpen=false; g->mapOpen=false;
    g->fireflyDiscovered=false; g->leaflingFed=false; g->groveMoteDefeated=false; g->enemySoaked=false; g->battleRound=0; g->defeatedGuards=0;
}
void GameStart(Game *g) {
    g->scene=SCENE_PLAY; SwitchArea(g,AREA_CAMP); g->player.pos=g->world.camp; SnapCameraToPlayer(g);
    SetDialogueAs(g,"旅人手记","你在海岸边醒来，远处的灯塔已经熄灭。",
        "一只小小的苔灯灵躲在营火旁。它似乎受了伤。",
        "靠近它，按 E 互动。WASD 移动，SHIFT 冲刺。");
}
static void AddBuddyFriend(Game *g,int id) {
    if(id==0){g->lumi.owned=true;g->lumi.hp=g->lumi.maxHp;g->lumi.pos=Vector2Add(g->player.pos,BuddyFollowOffset(0,g->player.facing));}
    if(id==1){g->splash.owned=true;g->splash.hp=g->splash.maxHp;g->splash.pos=Vector2Add(g->player.pos,BuddyFollowOffset(1,g->player.facing));}
    if(id==2){g->spark.owned=true;g->spark.hp=g->spark.maxHp;g->spark.pos=Vector2Add(g->player.pos,BuddyFollowOffset(2,g->player.facing));}
}
Buddy *CurrentBuddy(Game *g) {
    if(g->selectedBuddy==0 && g->lumi.owned)return &g->lumi;
    if(g->selectedBuddy==1 && g->splash.owned)return &g->splash;
    if(g->selectedBuddy==2 && g->spark.owned)return &g->spark;
    if(g->lumi.owned)return &g->lumi;
    if(g->splash.owned)return &g->splash;
    if(g->spark.owned)return &g->spark;
    return &g->lumi;
}
static Buddy *BuddyById(Game *g,int id) {
    if(id==0)return &g->lumi;
    if(id==1)return &g->splash;
    if(id==2)return &g->spark;
    return NULL;
}
static Vector2 BuddyFollowOffset(int id,int facing) {
    // Keep the party in a loose fan behind the traveller instead of stacking
    // every companion on the same diagonal tile.
    static const Vector2 formation[4][3]={
        {{-.82f,-.92f},{0.0f,-1.30f},{.82f,-.92f}},   // facing down
        {{1.05f,-.82f},{1.36f,0.0f},{1.05f,.82f}},    // facing left
        {{-1.05f,-.82f},{-1.36f,0.0f},{-1.05f,.82f}}, // facing right
        {{-.82f,.92f},{0.0f,1.30f},{.82f,.92f}}       // facing up
    };
    if(id<0||id>2)id=0;
    if(facing<0||facing>3)facing=0;
    return formation[facing][id];
}
static int OwnedBuddyIdAt(Game *g,int slot) {
    int count=0;
    for(int id=0;id<3;id++) {
        Buddy *b=BuddyById(g,id);
        if(b&&b->owned&&b->hp>0.0f) {
            if(count==slot)return id;
            count++;
        }
    }
    return -1;
}
static int OwnedBuddyCount(Game *g) {
    return (g->lumi.owned&&g->lumi.hp>0?1:0)+(g->splash.owned&&g->splash.hp>0?1:0)+(g->spark.owned&&g->spark.hp>0?1:0);
}
static bool FindNearestPickup(Game *g,int kindA,int kindB,Vector2 *out) {
    float best=1.0e9f;
    bool found=false;
    for(int i=0;i<g->world.pickupCount;i++) {
        Pickup *p=&g->world.pickups[i];
        if(p->taken || (p->kind!=kindA && p->kind!=kindB))continue;
        float d=Dist(g->player.pos,p->tile);
        if(d<best){best=d;*out=p->tile;found=true;}
    }
    return found;
}
void GetCurrentObjective(Game *g,const char **title,const char **detail,Vector2 *target,bool *hasTarget) {
    *hasTarget=true;
    *title="继续探索";
    *detail="打开旅人手记 J 查看章节记录";
    *target=g->player.pos;
    if(!g->lumi.owned) {
        if(g->area==AREA_CAMP) {
            *title="救助苔灯灵";
            *detail="到营火旁找苔灯灵，按 E 救助";
            *target=(Vector2){6,8};
        } else {
            *title="返回海岸营地";
            *detail="沿西侧石路返回海岸营地，按 E";
            *target=g->world.returnPoint;
        }
        return;
    }
    if(!g->splash.owned) {
        if(g->area==AREA_CAMP) {
            *title="寻找沫尾兽";
            *detail="从营地东侧入口进入辉光林地";
            *target=g->world.groveGate;
        } else if(g->area==AREA_GROVE) {
            if(g->player.berries>0) {
                *title="招募沫尾兽";
                *detail="带一颗果实，到浅水边喂沫尾兽";
                *target=(Vector2){14,7};
            } else {
                *title="收集岛屿果实";
                *detail="在林地采集一颗果实";
                if(!FindNearestPickup(g,0,3,target))*target=(Vector2){6,7};
            }
        } else {
            *title="返回辉光林地";
            *detail="从西侧出口回营地，再进辉光林地";
            *target=g->world.returnPoint;
        }
        return;
    }
    if(!g->spark.owned) {
        if(g->area==AREA_CAMP) {
            *title="寻找鸣晶蜥";
            *detail="从营地东侧入口进入潮汐遗迹";
            *target=g->world.ruinGate;
        } else if(g->area==AREA_RUINS) {
            if(g->player.crystals>0) {
                *title="招募鸣晶蜥";
                *detail="带一枚星晶，到石台招募鸣晶蜥";
                *target=(Vector2){13,8};
            } else {
                *title="收集星晶";
                *detail="在遗迹寻找并采集星晶";
                if(!FindNearestPickup(g,2,2,target))*target=(Vector2){10,9};
            }
        } else {
            *title="返回潮汐遗迹";
            *detail="从西侧出口回营地，再进潮汐遗迹";
            *target=g->world.returnPoint;
        }
        return;
    }
    if(!g->shrineSolved) {
        if(g->area==AREA_CAMP) {
            *title="唤醒潮汐遗迹";
            *detail="进入潮汐遗迹，收集一枚潮汐贝壳";
            *target=g->world.ruinGate;
        } else if(g->area==AREA_RUINS) {
            if(g->player.shells<=0) {
                *title="寻找潮汐贝壳";
                *detail="沿水道寻找潮汐贝壳，按 E 采集";
                if(!FindNearestPickup(g,1,1,target))*target=(Vector2){6,7};
            } else {
                *title="激活遗迹机关";
                *detail="带着潮汐贝壳，靠近中央机关按 E";
                *target=g->world.shrine;
            }
        } else {
            *title="返回潮汐遗迹";
            *detail="沿西侧出口返回营地，再进入潮汐遗迹";
            *target=g->world.returnPoint;
        }
        return;
    }
    if(!g->bossDefeated) {
        if(g->area==AREA_CAMP) {
            *title="前往灯塔庭院";
            *detail="封印已解除，从营地东南入口进入庭院";
            *target=g->world.shrine;
        } else if(g->area==AREA_LIGHTHOUSE) {
            *title="击败灯塔守卫";
            *detail="找到庭院中央的守卫，靠近后按 E 挑战";
            *target=g->world.bossPos;
        } else {
            *title="返回灯塔庭院";
            *detail="从西侧出口回营地，再进灯塔庭院";
            *target=g->world.returnPoint;
        }
        return;
    }
    if(g->area==AREA_LIGHTHOUSE) {
        *title="点亮失落的灯塔";
        *detail="靠近中央石碑，按 E 点亮灯塔";
        *target=g->world.shrine;
    } else if(g->area==AREA_CAMP) {
        *title="返回灯塔庭院";
        *detail="从营地东南入口回到灯塔庭院";
        *target=g->world.shrine;
    } else {
        *title="返回灯塔庭院";
        *detail="从西侧出口回营地，再进灯塔庭院";
        *target=g->world.returnPoint;
    }
    (void)hasTarget;
}
static void EnterBattle(Game *g,int idx) {
    g->battleEnemy=idx;g->battleTurn=0;g->battleChoice=0;g->battleTimer=0;g->battleRound=1;g->enemySoaked=false;
    for(int slot=0;slot<OwnedBuddyCount(g);slot++)if(OwnedBuddyIdAt(g,slot)==g->selectedBuddy){g->battleChoice=slot;break;}
    g->scene=SCENE_BATTLE;
    g->battleMessage=0;
}
static void StartStoryBattle(Game *g) {
    AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,150);
    EnterBattle(g,g->enemyCount-1);
}
void GameUpdate(Game *g,float dt) {
    g->time+=dt;
    if(g->scene==SCENE_END) {
        g->endingTime+=dt;
        if(IsKeyPressed(KEY_R)) {
            int seed=g->lastSeed+1;
            GameReset(g,seed);
            GameStart(g);
        } else if(g->endingTime>=5.0f&&(IsKeyPressed(KEY_ENTER)||IsKeyPressed(KEY_ESCAPE))) {
            g->scene=SCENE_TITLE;
            g->endingTime=0.0f;
        }
        return;
    }
    if(g->toastTimer>0)g->toastTimer-=dt;
    g->fireflyPhase+=dt;
    g->dayClock=fmodf(g->dayClock+dt/420.0f,1.0f);
    g->weatherFade=fminf(1.0f,g->weatherFade+dt*0.32f);
    if(g->area==AREA_GROVE) {
        /* The grove always carries its signature firefly ambience. */
        if(g->weather!=2) {
            g->weather=2;
            g->weatherClock=0.0f;
            g->weatherFade=0.0f;
        }
    } else {
        g->weatherClock+=dt;
        if(g->weather==2)g->weather=0; /* Never leak grove-only fireflies into other maps. */
        if(g->weatherClock>38.0f) {
            static const int roamingWeather[4]={0,1,3,4};
            int pick=GetRandomValue(0,3);
            int nextWeather=roamingWeather[pick];
            if(nextWeather==g->weather)nextWeather=roamingWeather[(pick+1+GetRandomValue(0,2))%4];
            g->weather=nextWeather;
            g->weatherClock=0.0f;
            g->weatherFade=0.0f;
            const char *weatherNotice[5]={"天气转晴，暖光穿过云层。","林间阵雨聚拢，风势渐强。","萤火微光在林间浮现。","云层压低，海风转冷。","夜雾漫过岛屿，灯火逐渐显现。"};
            Toast(g,weatherNotice[g->weather]);
        }
    }
    if(g->scene==SCENE_PLAY) {
        if(IsKeyPressed(KEY_O))g->objectiveCollapsed=!g->objectiveCollapsed;
        if(IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
            Vector2 mouse=LogicalMousePosition();
            Rectangle objectiveToggle=g->objectiveCollapsed
                ?(Rectangle){SW-88.0f,24.0f,68.0f,28.0f}
                :(Rectangle){SW-92.0f,26.0f,72.0f,28.0f};
            if(CheckCollisionPointRec(mouse,objectiveToggle))g->objectiveCollapsed=!g->objectiveCollapsed;
        }
        if(IsKeyPressed(KEY_F7)) {
            if(g->area==AREA_GROVE) {
                g->weather=2;
                Toast(g,"辉光林地：萤火微光在林间浮现。");
            } else {
                static const int roamingWeather[4]={0,1,3,4};
                int current=-1;
                for(int i=0;i<4;i++)if(roamingWeather[i]==g->weather)current=i;
                int next=roamingWeather[(current+1)%4];
                g->weather=next;
                g->weatherClock=0.0f;
                g->weatherFade=0.0f;
                const char *weatherNotice[5]={"天气：晴朗","天气：阵雨","天气：萤火","天气：阴云","天气：夜雾"};
                Toast(g,weatherNotice[g->weather]);
            }
        }
    }
    if(g->journalOpen || g->inventoryOpen || g->mapOpen) {
        if(IsKeyPressed(KEY_F5)){GameSave(g);Toast(g,"旅程已保存！");}
        if(IsKeyPressed(KEY_F9))GameLoad(g);
        if(IsKeyPressed(KEY_J)||IsKeyPressed(KEY_I)||IsKeyPressed(KEY_M)||IsKeyPressed(KEY_ESCAPE)) { g->journalOpen=false;g->inventoryOpen=false;g->mapOpen=false; }
        return;
    }
    for(int i=0;i<MAX_PARTICLES;i++) if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];p->life-=dt;p->p=Vector2Add(p->p,Vector2Scale(p->v,dt));p->v.y-=0.3f*dt;
    }
    if(g->scene==SCENE_TITLE) {
        if(IsKeyPressed(KEY_F9)) { if(GameLoad(g)) return; }
        if(IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) GameStart(g);
        return;
    }
    if(g->scene==SCENE_DIALOGUE) {
        if(IsKeyPressed(KEY_E)||IsKeyPressed(KEY_ENTER)||IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
            g->dialoguePage++;
            if(g->dialoguePage>=g->dialogueCount) {
                if(g->dialogueStartsBossBattle) {
                    g->dialogueStartsBossBattle=false;
                    AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,150);
                    EnterBattle(g,g->enemyCount-1);
                } else g->scene=SCENE_PLAY;
            } else {
                snprintf(g->dialogueSpeaker,sizeof(g->dialogueSpeaker),"%s",g->dialogueLineSpeaker[g->dialoguePage]);
            }
        }
        return;
    }
    if(g->scene==SCENE_BATTLE) {
        Enemy *e=&g->enemies[g->battleEnemy];
        if(!e->alive){g->scene=SCENE_PLAY;return;}
        int buddyCount=OwnedBuddyCount(g);
        int optionCount=buddyCount>0?buddyCount:1;
        if(IsKeyPressed(KEY_ONE)||IsKeyPressed(KEY_Q))g->battleChoice=0;
        if((IsKeyPressed(KEY_TWO)||IsKeyPressed(KEY_W))&&optionCount>1)g->battleChoice=1;
        if((IsKeyPressed(KEY_THREE)||IsKeyPressed(KEY_E))&&optionCount>2)g->battleChoice=2;
        if(IsKeyPressed(KEY_LEFT)||IsKeyPressed(KEY_A))g->battleChoice=(g->battleChoice+optionCount-1)%optionCount;
        if(IsKeyPressed(KEY_RIGHT)||IsKeyPressed(KEY_D))g->battleChoice=(g->battleChoice+1)%optionCount;
        if(g->battleChoice>=optionCount)g->battleChoice=optionCount-1;
        int selectedId=buddyCount>0?OwnedBuddyIdAt(g,g->battleChoice):-1;
        if(selectedId>=0)g->selectedBuddy=selectedId;
        bool mousePressed=IsMouseButtonPressed(MOUSE_BUTTON_LEFT);
        bool mouseSelected=false;
        if(mousePressed) {
            Vector2 mouse=LogicalMousePosition();
            for(int slot=0;slot<optionCount;slot++) {
                Rectangle card={(float)(50+slot*245),555,230,112};
                if(CheckCollisionPointRec(mouse,card)) {
                    g->battleChoice=slot;
                    selectedId=buddyCount>0?OwnedBuddyIdAt(g,slot):-1;
                    if(selectedId>=0)g->selectedBuddy=selectedId;
                    mouseSelected=true;
                    break;
                }
            }
        }
        bool confirm=IsKeyPressed(KEY_ENTER)||(mousePressed&&!mouseSelected);
        if(confirm) {
            if(g->battleTurn==0) {
                Buddy *active=selectedId>=0?BuddyById(g,selectedId):NULL;
                float damage=10.0f;
                if(selectedId==0 && active) {
                    g->player.hp=fminf(g->player.maxHp,g->player.hp+18.0f+(active->level-1)*3.0f);
                    damage=8.0f+(active->level-1)*2.0f;
                    Burst(g,WorldScreen(g->player.pos),C_GOLD,16);
                    Toast(g,"苔灯灵：星光治愈，恢复生命并造成微光伤害。");
                } else if(selectedId==1 && active) {
                    damage=20.0f+(active->level-1)*3.0f;
                    g->enemySoaked=true;
                    Burst(g,WorldScreen(e->pos),C_BLUE,22);
                    Toast(g,"沫尾兽：泡泡水流，敌人进入潮湿状态！");
                } else if(selectedId==2 && active) {
                    bool boosted=g->enemySoaked;
                    damage=(boosted?38.0f:26.0f)+(active->level-1)*4.0f;
                    Burst(g,WorldScreen(e->pos),C_LILAC,26);
                    Toast(g,boosted?"鸣晶蜥：潮湿导电！造成额外伤害！":"鸣晶蜥：晶弧放电！");
                    g->enemySoaked=false;
                } else {
                    Toast(g,"旅人发动普通攻击！");
                }
                e->hp-=damage;
                g->battleTurn=1;
                if(e->hp<=0) {
                    e->alive=false;g->battleTurn=0;g->scene=SCENE_PLAY;g->defeatedGuards++;
                    if(active) {
                        active->xp+=20;
                        if(active->xp>=50){active->xp-=50;active->level++;Toast(g,"伙伴成长了！技能效果也得到提升。");}
                        else Toast(g,"敌人被击退！出战伙伴获得20点经验。");
                    } else Toast(g,"敌人被击退！");
                    GameSave(g);
                    if(e->type==ENEMY_MOTE){g->groveMoteDefeated=true;g->player.berries+=2;GameSave(g);Toast(g,"击退林地微光兽，获得2颗果实！");}
                    if(e->type==ENEMY_BOSS) {
                        g->bossDefeated=true;g->player.crystals+=3;g->player.shells+=2;GameSave(g);
                        const char *speakers[3]={"灯塔守卫","旅人","灯塔守卫"};
                        const char *lines[3]={
                            "胸口的光……终于不再灼痛了。原来我一直在等待有人唤醒灯塔。",
                            "你不必再独自守着这里了。让这束光照亮整座岛吧。",
                            "去吧，旅人。灯火会记住你们。"
                        };
                        SetDialogueSequence(g,speakers,lines,3,false);
                    }
                }
            } else {
                int bossPhase=e->hp<=50.0f?3:(e->hp<=100.0f?2:1);
                bool bossHeavy=e->type==ENEMY_BOSS &&
                    (bossPhase==3?(g->battleRound%2==0):(g->battleRound%3==0));
                float incoming=e->type==ENEMY_BOSS?(bossPhase==3?20.0f:(bossPhase==2?17.0f:14.0f)):10.0f;
                if(bossHeavy)incoming+=(bossPhase==3?12.0f:9.0f);
                // Wetting the guardian's core interrupts its resonance and softens the next blow.
                bool damped=g->enemySoaked;
                if(damped)incoming*=e->type==ENEMY_BOSS?0.65f:0.8f;
                g->enemySoaked=false; // Wetness interrupts one incoming attack, then dissipates.
                Buddy *defender=BuddyById(g,g->selectedBuddy);
                bool buddyTookHit=defender&&defender->owned&&defender->hp>0.0f;
                if(buddyTookHit)defender->hp=fmaxf(0.0f,defender->hp-incoming);
                else g->player.hp-=incoming;
                Burst(g,WorldScreen(buddyTookHit?defender->pos:g->player.pos),bossHeavy?C_GOLD:C_PINK,bossHeavy?22:10);
                g->battleTurn=0;g->battleRound++;
                if(!buddyTookHit&&g->player.hp<=0){g->player.hp=55;g->scene=SCENE_PLAY;Toast(g,"伙伴把你带回营地，恢复了体力。");}
                else if(buddyTookHit&&defender->hp<=0)Toast(g,TextFormat("%s体力耗尽！回到守望者处可以恢复。",g->selectedBuddy==0?"苔灯灵":g->selectedBuddy==1?"沫尾兽":"鸣晶蜥"));
                else if(damped&&e->type==ENEMY_BOSS)Toast(g,TextFormat("CORE DISRUPTED! DAMAGE REDUCED TO %.0f.",incoming));
                else if(bossHeavy)Toast(g,TextFormat("RESONANCE STRIKE! %.0f damage.",incoming));
                else if(e->type==ENEMY_BOSS)Toast(g,TextFormat("LIGHTHOUSE GUARDIAN ATTACK! %.0f damage.",incoming));
                else Toast(g,TextFormat("敌人发动攻击！受到 %.0f 点伤害。",incoming));
            }
        }
        return;
    }

    Player *p=&g->player;
    // Area identity is controlled by explicit entrance/exit gates.
    if(IsKeyPressed(KEY_J)){g->journalOpen=true;return;}
    if(IsKeyPressed(KEY_I)){g->inventoryOpen=true;return;}
    if(IsKeyPressed(KEY_M)){g->mapOpen=true;return;}
    if(IsKeyPressed(KEY_F5)){GameSave(g);Toast(g,"旅程已保存！");return;}
    if(IsKeyPressed(KEY_F9)){GameLoad(g);return;}
    p->dashCd=fmaxf(0,p->dashCd-dt);
    p->invuln=fmaxf(0,p->invuln-dt);
    Vector2 in={0,0};
    if(IsKeyDown(KEY_W)||IsKeyDown(KEY_UP))in.y-=1;
    if(IsKeyDown(KEY_S)||IsKeyDown(KEY_DOWN))in.y+=1;
    if(IsKeyDown(KEY_A)||IsKeyDown(KEY_LEFT))in.x-=1;
    if(IsKeyDown(KEY_D)||IsKeyDown(KEY_RIGHT))in.x+=1;
    float l=Vector2Length(in);if(l>0.01f)in=Vector2Scale(in,1/l);
    float speed=3.65f;
    if(IsKeyPressed(KEY_LEFT_SHIFT)&&p->dashCd<=0&&l>0.01f){speed=8.5f;p->dashCd=0.9f;p->invuln=0.18f;Burst(g,WorldScreen(p->pos),WHITE,8);}
    Vector2 desired=Vector2Scale(in,speed);
    float response=(l>0.01f)?13.0f:18.0f;
    p->vel=Vector2Lerp(p->vel,desired,fminf(1.0f,dt*response));
    if (fabsf(in.x) > fabsf(in.y) && fabsf(in.x) > 0.01f) p->facing = in.x < 0 ? 1 : 2;
    else if (fabsf(in.y) > 0.01f) p->facing = in.y < 0 ? 3 : 0;
    MovePlayerWithCollision(g,Vector2Scale(p->vel,dt));

    Buddy *followers[3]={&g->lumi,&g->splash,&g->spark};
    for(int bi=0;bi<3;bi++) {
        Buddy *b=followers[bi];
        if(!b->owned) continue;
        Vector2 offset=BuddyFollowOffset(bi,p->facing);
        Vector2 target=Vector2Add(p->pos,offset);
        // Pick an unoccupied walkable formation slot. If scenery blocks the
        // preferred slot, search nearby offsets and then a wider recovery ring.
        bool foundTarget=!Solid(g,target);
        const Vector2 alternatives[6]={
            {offset.x,offset.y-.48f},{offset.x,offset.y+.48f},
            {offset.x-.48f,offset.y},{offset.x+.48f,offset.y},
            {offset.x*.55f,offset.y*.55f},{offset.x*1.35f,offset.y*1.35f}
        };
        float best=foundTarget?Dist(target,b->pos):1.0e9f;
        for(int ai=0;ai<6;ai++) {
            Vector2 candidate=Vector2Add(p->pos,alternatives[ai]);
            if(Solid(g,candidate))continue;
            bool occupied=false;
            for(int oi=0;oi<3;oi++)if(oi!=bi&&followers[oi]->owned&&Dist(candidate,followers[oi]->pos)<.62f)occupied=true;
            if(occupied)continue;
            float score=Dist(candidate,b->pos);
            if(!foundTarget||score<best){best=score;target=candidate;foundTarget=true;}
        }
        if(!foundTarget) {
            for(int radius=1;radius<=3&&!foundTarget;radius++) {
                for(int ai=0;ai<8;ai++) {
                    float a=(float)ai*PI2/8.0f;
                    Vector2 candidate=Vector2Add(p->pos,(Vector2){cosf(a)*(1.25f+.42f*radius),sinf(a)*(1.25f+.42f*radius)});
                    if(Solid(g,candidate))continue;
                    bool occupied=false;
                    for(int oi=0;oi<3;oi++)if(oi!=bi&&followers[oi]->owned&&Dist(candidate,followers[oi]->pos)<.62f)occupied=true;
                    if(!occupied){target=candidate;foundTarget=true;break;}
                }
            }
        }
        Vector2 delta=Vector2Subtract(target,b->pos);
        float distance=Vector2Length(delta);
        // Hard leash: a follower that falls far behind is repositioned instantly
        // into its safe formation slot instead of visibly running across the map.
        if(distance>6.0f&&foundTarget&&!Solid(g,target)) {
            b->pos=target;
            distance=0.0f;
            delta=(Vector2){0,0};
        }
        float speed=distance>3.8f?7.2f:4.4f+bi*.25f;
        if(distance>.015f) {
            Vector2 step=Vector2Scale(delta,fminf(1.0f,speed*dt/distance));
            int substeps=(int)ceilf(Vector2Length(step)/.10f);
            if(substeps<1)substeps=1;
            Vector2 part=Vector2Scale(step,1.0f/(float)substeps);
            for(int si=0;si<substeps;si++) {
                Vector2 next=b->pos;
                next.x+=part.x;if(!Solid(g,next))b->pos.x=next.x;
                next=b->pos;
                next.y+=part.y;if(!Solid(g,next))b->pos.y=next.y;
            }
        }
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
        // One authoritative final-story interaction. It is handled before exits, pickups and NPCs.
        if(g->area==AREA_LIGHTHOUSE && g->bossDefeated && Dist(p->pos,g->world.shrine)<3.8f) {
            g->journalOpen=false;
            g->inventoryOpen=false;
            g->mapOpen=false;
            g->endingTime=0.0f;
            g->player.vel=(Vector2){0,0};
            g->toastTimer=0.0f;
            g->scene=SCENE_END;
            GameSave(g);
            return;
        }
        if(g->area==AREA_CAMP) {
            if(Dist(p->pos,g->world.groveGate)<1.4f){SwitchArea(g,AREA_GROVE);GameSave(g);Toast(g,"进入辉光林地");return;}
            if(Dist(p->pos,g->world.ruinGate)<1.4f){
                if(!(g->lumi.owned&&g->splash.owned)){Toast(g,"先让苔灯灵和沫尾兽加入队伍，便可进入遗迹寻找鸣晶蜥。");return;}
                SwitchArea(g,AREA_RUINS);GameSave(g);Toast(g,"进入潮汐遗迹");return;
            }
            if(Dist(p->pos,g->world.shrine)<1.4f){
                if(!g->shrineSolved){Toast(g,"灯塔庭院仍被古老封印阻挡。");return;}
                SwitchArea(g,AREA_LIGHTHOUSE);GameSave(g);Toast(g,"进入灯塔庭院");return;
            }
        } else if(Dist(p->pos,g->world.returnPoint)<1.6f) {
            SwitchArea(g,AREA_CAMP);GameSave(g);Toast(g,"返回海岸营地");return;
        }
    }
    if(IsKeyPressed(KEY_E)) {
        // Rescue Moss Lumi at the camp.
        if(g->area==AREA_CAMP&&!g->lumi.owned&&Dist(p->pos,(Vector2){6,8})<1.5f) {
            AddBuddyFriend(g,0);
            GameSave(g);
            SetDialogue(g,"苔灯灵抬起头，灯囊重新亮了起来。",
                "它决定跟着你。靠近暗处时，它会照亮隐藏的纹路。",
                "按 TAB 可以切换已获得的伙伴。");
            return;
        }
        // The Watcher runs the island's Pokémon-Center-style recovery service.
        // This check precedes the campfire and pickup checks because their ranges overlap.
        if(g->area==AREA_CAMP&&Dist(p->pos,(Vector2){7.0f,7.0f})<2.0f) {
            p->hp=p->maxHp;
            if(g->lumi.owned)g->lumi.hp=g->lumi.maxHp;
            if(g->splash.owned)g->splash.hp=g->splash.maxHp;
            if(g->spark.owned)g->spark.hp=g->spark.maxHp;
            GameSave(g);
            SetDialogueAs(g,"守望者","欢迎回来。让我检查一下大家的状态。","伙伴们已经恢复到最佳状态，随时可以继续探索。","旅途累了就回来休息，我会一直守着营地。");
            Burst(g,WorldScreen((Vector2){7.0f,7.0f}),C_GREEN,16);
            return;
        }
        // Pickups.
        for(int i=0;i<g->world.pickupCount;i++) {
            Pickup *q=&g->world.pickups[i];
            if(!q->taken&&Dist(p->pos,q->tile)<1.0f) {
                q->taken=true; g->areaPickupTaken[(int)g->area][i]=1;
                if(q->kind==0)p->berries++;
                if(q->kind==1)p->shells++;
                if(q->kind==2)p->crystals++;
                if(q->kind==3)p->berries++;
                Burst(g,WorldScreen(q->tile),C_GOLD,12);
                Toast(g,q->kind==1?"获得潮汐贝壳！":q->kind==2?"获得星晶！":"获得岛屿果实！");
                GameSave(g);
                return;
            }
        }
        // A shy forest firefly can be registered in the field journal.
        Vector2 fireflyPos={10.6f+sinf(g->fireflyPhase*0.8f)*0.22f,5.4f+cosf(g->fireflyPhase*0.65f)*0.18f};
        if(g->area==AREA_GROVE&&!g->fireflyDiscovered && Dist(p->pos,fireflyPos)<1.35f) {
            g->fireflyDiscovered=true;
            Burst(g,WorldScreen(fireflyPos),C_GOLD,22);
            SetDialogueAs(g,"萤芽","一只小小的萤芽停在叶片上，胸口的光一闪一闪。","它没有跟你走，只是把这片林地的秘密记在了旅人手记里。","按 J 打开手记，可以查看发现与当前目标。");
            return;
        }
        // A hungry leafling accepts a berry and reveals a hidden grove cache.
        if(g->area==AREA_GROVE&&!g->leaflingFed && Dist(p->pos,(Vector2){12.4f,8.0f})<1.4f) {
            if(p->berries>0) {
                p->berries--; g->leaflingFed=true;
                AddPickup(&g->world,2,13.2f,8.6f);
                Burst(g,WorldScreen((Vector2){12.4f,8.0f}),C_GREEN,18);
                SetDialogueAs(g,"叶团","叶团闻到了果实的香气，开心地蹦了起来。","它拨开藤蔓，露出一枚被藏起来的星晶！","探索时多留意会主动回应你的生物。");
            } else Toast(g,"叶团饿了：给它一颗岛屿果实吧。");
            return;
        }
        // A small wild mote guards the old grove path. Win once to clear the route.
        if(g->area==AREA_GROVE&&!g->groveMoteDefeated && Dist(p->pos,(Vector2){13.2f,9.2f})<1.45f) {
            AddEnemy(g,ENEMY_MOTE,13.2f,9.2f,34.0f);
            EnterBattle(g,g->enemyCount-1);
            return;
        }
        // Water companion encounter.
        if(g->area==AREA_GROVE&&!g->splash.owned&&Dist(p->pos,(Vector2){14,7})<1.6f) {
            if(p->berries>=1){p->berries--;AddBuddyFriend(g,1);GameSave(g);SetDialogueAs(g,"沫尾兽","沫尾兽被果实吸引，开心地绕着你转了一圈。","它可以喷水清理蒸汽，也能让遗迹机关显形。","按 TAB 切换伙伴。");}
            else Toast(g,"沫尾兽想吃一颗岛屿果实。");
            return;
        }
        // Crystal companion encounter.
        if(g->area==AREA_RUINS&&!g->spark.owned&&Dist(p->pos,(Vector2){13,8})<1.6f) {
            if(p->crystals>=1){p->crystals--;AddBuddyFriend(g,2);GameSave(g);SetDialogueAs(g,"鸣晶蜥","鸣晶蜥吸收了星晶，背上的宝石依次亮起。","它能为古代装置供能，也能在战斗中放电。","按 TAB 切换伙伴。");}
            else Toast(g,"鸣晶蜥需要一枚星晶才愿意同行。");
            return;
        }
        // Ruin puzzle.
        if(g->area==AREA_RUINS&&Dist(p->pos,g->world.shrine)<1.7f&&!g->shrineSolved) {
            if(g->lumi.owned&&g->splash.owned&&g->spark.owned&&p->shells>=1) {
                g->shrineSolved=true;p->shells--;GameSave(g);
                Burst(g,WorldScreen(g->world.shrine),C_GOLD,40);
                SetDialogueAs(g,"旅人","三种伙伴的力量在遗迹中央交汇。","水纹显现了线路，星光照亮符文，晶弧重新接通能源。","灯塔庭院的封印解除了。");
            } else Toast(g,"遗迹需要三位伙伴协力，并带来一枚潮汐贝壳。");
            return;
        }
        // The lighthouse guardian speaks before the battle begins.
        if(g->area==AREA_LIGHTHOUSE&&g->shrineSolved&&Dist(p->pos,g->world.bossPos)<2.0f&&!g->bossDefeated) {
            const char *speakers[4]={"灯塔守卫","旅人","灯塔守卫","旅人"};
            const char *lines[4]={
                "停下。灯塔的光已经沉睡太久，我不会让任何人再次触碰它。",
                "这座岛屿正在失去光。我们不是来夺走它的。",
                "我曾守护灯火，直到潮汐吞没了我的名字……",
                "那就让我们一起找回它。"
            };
            SetDialogueSequence(g,speakers,lines,4,true);
            return;
        }
        // Chapter finale: the player must explicitly activate the central beacon stone.
        // The stone's collision footprint prevents standing on its centre, so use a generous
        // interaction radius and keep this check ahead of generic NPC/area interactions.
        if(g->area==AREA_LIGHTHOUSE&&g->bossDefeated&&Dist(p->pos,g->world.shrine)<3.0f) {
            g->endingTime=0.0f;
            g->scene=SCENE_END;
            GameSave(g);
            Burst(g,WorldScreen(g->world.shrine),C_GOLD,64);
            return;
        }
        // NPC at camp.
        if(g->area==AREA_CAMP&&Dist(p->pos,(Vector2){7,7})<1.6f) {
            SetDialogueAs(g,"守望者","灯塔熄灭后，岛上的小生灵都变得不安。","找到三位伙伴，唤醒潮汐遗迹，灯塔就能重新点亮。","果实、贝壳和星晶散落在岛上，按 E 采集。");
            return;
        }
    }
    // The final scene is entered only through the explicit E interaction above.
    if(g->shrineSolved&&g->enemyCount>0&&!g->enemies[g->enemyCount-1].alive)g->bossDefeated=true;

    UpdatePlayerCamera(g,dt);
    if(g->toastTimer>0)g->toastTimer-=dt;
}
