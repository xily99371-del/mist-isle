#include "game.h"
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
static void SetSolidRect(World *w,int x0,int y0,int x1,int y1) {
    for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
        if(x>0&&y>0&&x<MAPW-1&&y<MAPH-1)w->solid[y][x]=1;
    }
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

static void SwitchArea(Game *g,Area area);
static void BuildAreaWorld(Game *g,Area area) {
    BuildWorld(&g->world,g->lastSeed+(int)area*7919);
    g->area=area;
    World *w=&g->world;
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        int edge=(x==0||y==0||x==MAPW-1||y==MAPH-1);
        w->kind[y][x]=edge?3:0;
        w->solid[y][x]=0;
    }
    // A broad, guaranteed walkable route and a central open area.
    for(int x=1;x<21;x++){w->kind[9][x]=0;w->kind[10][x]=0;}
    for(int y=3;y<15;y++){w->kind[y][3]=0;w->kind[y][4]=0;}
    for(int y=5;y<14;y++)for(int x=7;x<17;x++)if((x+y)%4!=0)w->kind[y][x]=0;
    // Keep every gate and story interaction reachable on every deterministic seed.
    for(int x=14;x<20;x++){w->kind[4][x]=0;w->kind[9][x]=0;w->kind[14][x]=0;}
    for(int y=4;y<15;y++)w->kind[y][2]=0;
    if(area==AREA_GROVE) {
        for(int y=4;y<=10;y++)for(int x=8;x<=14;x++)w->kind[y][x]=0;
    } else if(area==AREA_RUINS) {
        for(int y=5;y<=9;y++)for(int x=13;x<=19;x++)w->kind[y][x]=0;
    } else if(area==AREA_LIGHTHOUSE) {
        for(int y=10;y<=14;y++)for(int x=14;x<=19;x++)w->kind[y][x]=0;
    }
    w->pickupCount=0;
    if(area==AREA_CAMP) {
        w->camp=(Vector2){4,13};w->groveGate=(Vector2){18,4};w->ruinGate=(Vector2){18,9};w->shrine=(Vector2){18,14};w->bossPos=(Vector2){18,14};
        for(int y=1;y<17;y++)for(int x=1;x<5;x++)if(y<7||y>14)w->kind[y][x]=2;
        for(int i=0;i<5;i++)AddPickup(w,0,7+i*2,7+(i%2)*4);
        AddPickup(w,1,15,6);AddPickup(w,2,15,12);
    } else if(area==AREA_GROVE) {
        w->camp=(Vector2){2,9};w->groveGate=(Vector2){2,9};w->ruinGate=(Vector2){19,9};w->shrine=(Vector2){17,5};w->bossPos=(Vector2){17,13};
        for(int y=3;y<15;y++)for(int x=9;x<12;x++)w->kind[y][x]=2;
        for(int x=8;x<14;x++)w->kind[8][x]=0;
        AddPickup(w,0,6,5);AddPickup(w,3,8,12);AddPickup(w,0,14,5);AddPickup(w,1,16,11);AddPickup(w,2,17,13);
    } else if(area==AREA_RUINS) {
        w->camp=(Vector2){2,9};w->groveGate=(Vector2){2,9};w->ruinGate=(Vector2){2,9};w->shrine=(Vector2){17,7};w->bossPos=(Vector2){18,13};
        for(int y=2;y<16;y++)for(int x=2;x<20;x++)if((x+y)%3==0)w->kind[y][x]=2;
        for(int x=2;x<20;x++)w->kind[9][x]=0;
        AddPickup(w,1,7,6);AddPickup(w,1,11,12);AddPickup(w,2,15,5);AddPickup(w,2,18,11);
    } else {
        w->camp=(Vector2){2,9};w->groveGate=(Vector2){2,9};w->ruinGate=(Vector2){2,9};w->shrine=(Vector2){17,7};w->bossPos=(Vector2){18,13};
        for(int y=3;y<15;y++)for(int x=3;x<19;x++)if(x<6||x>15||y<5||y>13)w->kind[y][x]=2;
        for(int x=2;x<20;x++)w->kind[9][x]=0;
        AddPickup(w,0,6,5);AddPickup(w,1,12,12);AddPickup(w,2,17,5);
    }
    // Hand-authored tree clusters frame each area while keeping the main routes open.
    if(area==AREA_CAMP) {
        const int trees[][2]={{2,2},{7,2},{10,2},{19,2},{20,6},{20,15},{12,16},{7,16},{8,2},{12,2},{20,4},{19,16},{2,6}};
        for(int i=0;i<(int)(sizeof(trees)/sizeof(trees[0]));i++) {
            int x=trees[i][0],y=trees[i][1];w->kind[y][x]=1;w->solid[y][x]=1;
        }
    } else if(area==AREA_GROVE) {
        const int trees[][2]={{3,2},{6,2},{8,3},{14,2},{18,2},{20,4},{4,6},{6,7},{7,13},{13,14},{17,14},{20,12},{2,4},{2,13},{6,15},{18,15},{20,7},{3,15}};
        for(int i=0;i<(int)(sizeof(trees)/sizeof(trees[0]));i++) {
            int x=trees[i][0],y=trees[i][1];w->kind[y][x]=1;w->solid[y][x]=1;
        }
    } else if(area==AREA_LIGHTHOUSE) {
        for(int y=6;y<=9;y++)for(int x=15;x<=19;x++)w->kind[y][x]=3;
        for(int y=3;y<=6;y++)for(int x=3;x<=6;x++)w->kind[y][x]=2;
        for(int y=12;y<=14;y++)for(int x=3;x<=7;x++)w->kind[y][x]=2;
        const int trees[][2]={{5,5},{7,5},{5,13},{7,14},{12,14},{19,11}};
        for(int i=0;i<(int)(sizeof(trees)/sizeof(trees[0]));i++) {
            int x=trees[i][0],y=trees[i][1];w->kind[y][x]=1;w->solid[y][x]=1;
        }
    }
    // Authored collision footprints keep the player outside large buildings and props.
    if(area==AREA_CAMP) {
        SetSolidRect(w,7,5,9,6);       // campsite tent
    } else if(area==AREA_GROVE) {
        SetSolidRect(w,4,3,5,4);       // flowering tree
        SetSolidRect(w,15,3,16,4);     // grove tree
        SetSolidRect(w,9,11,10,12);    // fallen log; keep the nearby fruit node clear
        SetSolidRect(w,14,12,15,12);   // mushroom cluster
    } else if(area==AREA_RUINS) {
        SetSolidRect(w,14,4,14,5);     // left arch footing
        SetSolidRect(w,16,4,16,5);     // right arch footing
        SetSolidRect(w,5,4,6,5);       // broken pillar
        SetSolidRect(w,9,11,10,12);    // broken wall
        SetSolidRect(w,15,11,16,12);   // rune fountain
    } else {
        SetSolidRect(w,16,3,18,4);     // lighthouse base; keep the crystal at (17,5) accessible
        SetSolidRect(w,9,6,10,7);      // garden fountain
        SetSolidRect(w,4,12,5,13);     // courtyard gate
        SetSolidRect(w,14,12,15,13);   // garden shrub
    }
    // Resource nodes are always placed on reachable ground, never inside a random blocker.
    for(int i=0;i<w->pickupCount;i++) {
        int px=(int)w->pickups[i].tile.x, py=(int)w->pickups[i].tile.y;
        if(px>0&&py>0&&px<MAPW-1&&py<MAPH-1)w->kind[py][px]=0;
    }
}
static void SwitchArea(Game *g,Area area) {
    BuildAreaWorld(g,area);
    g->player.pos=(area==AREA_CAMP)?(Vector2){16,9}:(Vector2){3,9};
    for(int i=0;i<g->world.pickupCount && i<MAX_PICKUPS;i++)g->world.pickups[i].taken=g->areaPickupTaken[(int)area][i]!=0;
    g->enemyCount=0;
    g->lumi.pos=Vector2Add(g->player.pos,(Vector2){-1,.5f});
    g->splash.pos=Vector2Add(g->player.pos,(Vector2){-1,1});
    g->spark.pos=Vector2Add(g->player.pos,(Vector2){0,1});
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

    // Authored object colliders match the visible footprints, not just their anchor tile.
    if(g->area==AREA_CAMP) {
        if(InRect(p,3.05f,1.55f,5.95f,4.75f))return true;   // blue cottage
        if(InRect(p,12.55f,2.75f,15.45f,6.15f))return true; // red cottage
        if(InRect(p,7.15f,5.15f,9.05f,7.15f))return true;   // main tent
        if(InRect(p,12.55f,3.25f,13.65f,4.75f))return true; // supply tent
        if(InRect(p,2.55f,9.55f,3.45f,10.45f))return true;  // camp keeper
        if(!g->lumi.owned && InRect(p,3.55f,11.55f,4.45f,12.45f))return true; // Moss Lumi
        float nx=(p.x-10.5f)/11.0f, ny=(p.y-8.5f)/9.0f;
        float coastNoise=.045f*sinf((float)x*.73f+(float)y*.29f)+.026f*cosf((float)y*.91f-(float)x*.24f);
        float shore=nx*nx+ny*ny+coastNoise;
        bool onPath=((y==9&&x>=3&&x<=19)||(x==4&&y>=9&&y<=13)||(x==18&&y>=3&&y<=15));
        bool dockWater=(x>=1&&x<=3&&y>=11&&y<=15);
        bool sea=shore>.90f;
        if(!onPath&&(dockWater||sea))return true;
    } else if(g->area==AREA_GROVE) {
        if(InRect(p,3.95f,2.95f,6.05f,4.85f))return true;   // flowering tree
        if(InRect(p,14.95f,3.05f,17.05f,4.95f))return true;  // grove tree
        if(InRect(p,8.35f,11.15f,10.75f,12.95f))return true;// fallen log
        if(InRect(p,13.75f,11.45f,15.85f,13.15f))return true;// mushroom cluster
        if(!g->splash.owned && InRect(p,8.55f,4.55f,9.45f,5.45f))return true; // Moatail
        bool stream=(x>=9&&x<=11&&y>=3&&y<=14);
        bool bridge=(y==9&&x>=2&&x<=19);
        if(stream&&!bridge)return true;
    } else if(g->area==AREA_RUINS) {
        if(InRect(p,13.65f,3.95f,14.75f,6.45f))return true; // left arch footing
        if(InRect(p,15.25f,3.95f,16.35f,6.45f))return true; // right arch footing
        if(InRect(p,4.75f,4.05f,6.85f,6.25f))return true;   // broken pillar
        if(InRect(p,8.45f,11.05f,10.75f,13.05f))return true;// broken wall
        if(InRect(p,14.35f,11.05f,16.35f,13.15f))return true;// rune fountain
        if(InRect(p,17.05f,11.55f,19.05f,13.55f))return true;// east broken pillar
        if(InRect(p,16.68f,6.68f,17.32f,7.32f))return true;  // shrine base only
        if(!g->spark.owned && InRect(p,14.55f,6.55f,15.45f,7.45f))return true; // Crystal Gecko
        bool stream=(x>=9&&x<=11&&y>=3&&y<=14);
        bool bridge=(y==9&&x>=2&&x<=19);
        if(stream&&!bridge)return true;
    } else {
        if(InRect(p,15.45f,2.65f,18.65f,5.15f))return true; // lighthouse tower
        if(InRect(p,8.65f,5.75f,10.85f,7.85f))return true;  // garden fountain
        if(InRect(p,3.75f,11.65f,5.65f,13.85f))return true;// courtyard gate
        if(InRect(p,13.65f,11.65f,15.85f,13.95f))return true;// garden shrub
        if(InRect(p,16.68f,6.68f,17.32f,7.32f))return true;  // shrine base only
        if(!g->bossDefeated && InRect(p,17.55f,12.55f,18.45f,13.45f))return true; // lighthouse guardian
        float nx=(p.x-10.8f)/10.6f, ny=(p.y-8.5f)/8.7f;
        float coastNoise=.038f*sinf((float)x*.68f+(float)y*.34f)+.024f*cosf((float)y*.86f-(float)x*.27f);
        if(nx*nx+ny*ny+coastNoise>.94f)return true;
    }
    return false;
}
static bool Solid(Game *g,Vector2 p) {
    // Eight perimeter samples approximate the full body footprint and prevent diagonal corner clipping.
    const float r=.37f;
    const float d=.262f;
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
    s.magic=0x4D495354; s.version=4; s.seed=g->lastSeed; s.area=(int)g->area;
    s.px=g->player.pos.x; s.py=g->player.pos.y; s.hp=g->player.hp;
    s.berries=g->player.berries; s.shells=g->player.shells; s.crystals=g->player.crystals;
    s.lumi=g->lumi.owned; s.splash=g->splash.owned; s.spark=g->spark.owned; s.selected=g->selectedBuddy;
    s.shrineSolved=g->shrineSolved; s.bossDefeated=g->bossDefeated;
    s.fireflyDiscovered=g->fireflyDiscovered; s.leaflingFed=g->leaflingFed; s.groveMoteDefeated=g->groveMoteDefeated;
    s.lumiLevel=g->lumi.level;s.lumiXp=g->lumi.xp;s.splashLevel=g->splash.level;s.splashXp=g->splash.xp;s.sparkLevel=g->spark.level;s.sparkXp=g->spark.xp;
    for(int a=0;a<4;a++)for(int i=0;i<MAX_PICKUPS;i++)s.pickupTaken[a][i]=g->areaPickupTaken[a][i];
    if(!SaveFileData("mist_isle_save.dat",&s,(int)sizeof(s)))Toast(g,"保存失败：无法写入存档文件。");
}
bool GameLoad(Game *g) {
    int size=0; unsigned char *raw=LoadFileData("mist_isle_save.dat",&size);
    if(!raw || size!=(int)sizeof(SaveData)){if(raw)UnloadFileData(raw);Toast(g,"没有找到有效存档。");return false;}
    SaveData s; memcpy(&s,raw,sizeof(s)); UnloadFileData(raw);
    if(s.magic!=0x4D495354 || s.version!=4){Toast(g,"存档版本不兼容。");return false;}
    GameReset(g,s.seed);
    SwitchArea(g,(Area)Clamp(s.area,AREA_CAMP,AREA_LIGHTHOUSE));
    g->scene=SCENE_PLAY; g->player.pos=(Vector2){s.px,s.py};SnapCameraToPlayer(g);g->player.hp=fmaxf(1.0f,fminf(100.0f,s.hp));
    g->player.berries=s.berries;g->player.shells=s.shells;g->player.crystals=s.crystals;
    g->lumi.owned=s.lumi!=0;g->splash.owned=s.splash!=0;g->spark.owned=s.spark!=0;g->selectedBuddy=s.selected;
    g->shrineSolved=s.shrineSolved!=0;g->bossDefeated=s.bossDefeated!=0;
    g->fireflyDiscovered=s.fireflyDiscovered!=0;g->leaflingFed=s.leaflingFed!=0;g->groveMoteDefeated=s.groveMoteDefeated!=0;
    g->lumi.level=s.lumiLevel;g->lumi.xp=s.lumiXp;g->splash.level=s.splashLevel;g->splash.xp=s.splashXp;g->spark.level=s.sparkLevel;g->spark.xp=s.sparkXp;
    for(int a=0;a<4;a++)for(int i=0;i<MAX_PICKUPS;i++)g->areaPickupTaken[a][i]=s.pickupTaken[a][i];
    for(int i=0;i<g->world.pickupCount && i<MAX_PICKUPS;i++)g->world.pickups[i].taken=g->areaPickupTaken[(int)g->area][i]!=0;
    if(g->lumi.owned)g->lumi.pos=Vector2Add(g->player.pos,(Vector2){-.55f,.42f});
    if(g->splash.owned)g->splash.pos=Vector2Add(g->player.pos,(Vector2){-.95f,.85f});
    if(g->spark.owned)g->spark.pos=Vector2Add(g->player.pos,(Vector2){-.15f,.95f});
    Toast(g,"已读取上次的旅程。"); return true;
}
void GameReset(Game *g,int seed) {
    memset(g,0,sizeof(*g)); g->scene=SCENE_TITLE; g->area=AREA_CAMP;
    g->lastSeed=seed; BuildWorld(&g->world,seed);
    g->player.pos=g->world.camp; g->player.hp=g->player.maxHp=100; g->player.stamina=100;
    g->lumi=(Buddy){.pos={2.3f,12.4f},.owned=false,.level=1};
    g->splash=(Buddy){.pos={8,7},.owned=false,.level=1};
    g->spark=(Buddy){.pos={15,8},.owned=false,.level=1};
    g->cam.offset=(Vector2){SW/2.0f,SH/2.0f}; g->cam.zoom=1.10f;
    g->fireflyPhase=0; g->dayClock=0.28f; g->weatherClock=0; g->weather=0; g->journalOpen=false; g->inventoryOpen=false; g->mapOpen=false;
    g->fireflyDiscovered=false; g->leaflingFed=false; g->groveMoteDefeated=false; g->enemySoaked=false; g->battleRound=0; g->defeatedGuards=0;
}
void GameStart(Game *g) {
    g->scene=SCENE_PLAY; SwitchArea(g,AREA_CAMP); g->player.pos=g->world.camp; SnapCameraToPlayer(g);
    SetDialogueAs(g,"旅人手记","你在海岸边醒来，远处的灯塔已经熄灭。",
        "一只小小的苔灯灵躲在营火旁。它似乎受了伤。",
        "靠近它，按 E 互动。WASD 移动，SHIFT 冲刺。");
}
static void AddBuddyFriend(Game *g,int id) {
    if(id==0){g->lumi.owned=true;g->lumi.pos=Vector2Add(g->player.pos,(Vector2){-0.7f,0.4f});}
    if(id==1){g->splash.owned=true;g->splash.pos=Vector2Add(g->player.pos,(Vector2){-0.7f,0.4f});}
    if(id==2){g->spark.owned=true;g->spark.pos=Vector2Add(g->player.pos,(Vector2){-0.7f,0.4f});}
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
static int OwnedBuddyIdAt(Game *g,int slot) {
    int count=0;
    for(int id=0;id<3;id++) {
        Buddy *b=BuddyById(g,id);
        if(b&&b->owned) {
            if(count==slot)return id;
            count++;
        }
    }
    return -1;
}
static int OwnedBuddyCount(Game *g) {
    return (g->lumi.owned?1:0)+(g->splash.owned?1:0)+(g->spark.owned?1:0);
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
    static const Vector2 exitPoint={2,9};
    *hasTarget=true;
    *title="继续探索";
    *detail="打开旅人手记 J 查看章节记录";
    *target=g->player.pos;
    if(!g->lumi.owned) {
        if(g->area==AREA_CAMP) {
            *title="救助苔灯灵";
            *detail="到营火旁找苔灯灵，按 E 救助";
            *target=(Vector2){4,12};
        } else {
            *title="返回海岸营地";
            *detail="沿西侧石路返回海岸营地，按 E";
            *target=exitPoint;
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
                *target=(Vector2){9,5};
            } else {
                *title="收集岛屿果实";
                *detail="在林地采集一颗果实";
                if(!FindNearestPickup(g,0,3,target))*target=(Vector2){6,5};
            }
        } else {
            *title="返回辉光林地";
            *detail="从西侧出口回营地，再进辉光林地";
            *target=exitPoint;
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
                *target=(Vector2){15,7};
            } else {
                *title="收集星晶";
                *detail="在遗迹寻找并采集星晶";
                if(!FindNearestPickup(g,2,2,target))*target=(Vector2){15,5};
            }
        } else {
            *title="返回潮汐遗迹";
            *detail="从西侧出口回营地，再进潮汐遗迹";
            *target=exitPoint;
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
                if(!FindNearestPickup(g,1,1,target))*target=(Vector2){7,6};
            } else {
                *title="激活遗迹机关";
                *detail="带着潮汐贝壳，靠近中央机关按 E";
                *target=g->world.shrine;
            }
        } else {
            *title="返回潮汐遗迹";
            *detail="沿西侧出口返回营地，再进入潮汐遗迹";
            *target=exitPoint;
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
            *target=exitPoint;
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
        *target=exitPoint;
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
    AddEnemy(g,ENEMY_GUARD,18,13,55);
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
    g->weatherClock+=dt;
    if(g->weatherClock>95.0f){g->weatherClock=0;g->weather=GetRandomValue(0,3);Toast(g,g->weather==0?"海风轻轻吹过。":g->weather==1?"林间飘起了细雨。":g->weather==2?"萤火虫开始活跃。":"云层散开，阳光洒进林地。");}
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
            g->weather=(g->weather+1)%4;
            Toast(g,g->weather==0?"天气：海风晴朗":g->weather==1?"天气：持续降雨":g->weather==2?"天气：萤火微光":"天气：云开见日");
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
            if(g->dialoguePage>=g->dialogueCount)g->scene=SCENE_PLAY;
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
                    if(e->type==ENEMY_BOSS){g->bossDefeated=true;g->player.crystals+=3;g->player.shells+=2;GameSave(g);}
                }
            } else {
                float incoming=e->type==ENEMY_BOSS?16.0f:10.0f;
                if(g->enemySoaked)incoming*=0.8f;
                g->player.hp-=incoming;Burst(g,WorldScreen(g->player.pos),C_PINK,10);
                g->battleTurn=0;g->battleRound++;
                if(g->player.hp<=0){g->player.hp=55;g->scene=SCENE_PLAY;Toast(g,"伙伴把你带回营地，恢复了体力。");}
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
        Vector2 offset = bi==0 ? (Vector2){-0.55f,0.42f} : bi==1 ? (Vector2){-0.95f,0.85f} : (Vector2){-0.15f,0.95f};
        Vector2 target=Vector2Add(p->pos,offset);
        Vector2 next=Vector2Add(b->pos,Vector2Scale(Vector2Subtract(target,b->pos),fminf(1,dt*(4.2f+bi*.25f))));
        if(!Solid(g,next))b->pos=next;
        else {
            Vector2 fallback=Vector2Add(p->pos,Vector2Scale(offset,0.55f));
            if(!Solid(g,fallback))b->pos=fallback;
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
        } else if(Dist(p->pos,(Vector2){2,9})<1.6f) {
            SwitchArea(g,AREA_CAMP);GameSave(g);Toast(g,"返回海岸营地");return;
        }
    }
    if(IsKeyPressed(KEY_E)) {
        // Rescue Moss Lumi at the camp.
        if(g->area==AREA_CAMP&&!g->lumi.owned&&Dist(p->pos,(Vector2){4,12})<1.5f) {
            AddBuddyFriend(g,0);
            GameSave(g);
            SetDialogue(g,"苔灯灵抬起头，灯囊重新亮了起来。",
                "它决定跟着你。靠近暗处时，它会照亮隐藏的纹路。",
                "按 TAB 可以切换已获得的伙伴。");
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
        Vector2 fireflyPos={10.6f+sinf(g->fireflyPhase*0.8f)*0.22f,6.2f+cosf(g->fireflyPhase*0.65f)*0.18f};
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
        if(g->area==AREA_GROVE&&!g->splash.owned&&Dist(p->pos,(Vector2){9,5})<1.6f) {
            if(p->berries>=1){p->berries--;AddBuddyFriend(g,1);GameSave(g);SetDialogue(g,"沫尾兽被果实吸引，开心地绕着你转了一圈。","它可以喷水清理蒸汽，也能让遗迹机关显形。","按 TAB 切换伙伴。");}
            else Toast(g,"沫尾兽想吃一颗岛屿果实。");
            return;
        }
        // Crystal companion encounter.
        if(g->area==AREA_RUINS&&!g->spark.owned&&Dist(p->pos,(Vector2){15,7})<1.6f) {
            if(p->crystals>=1){p->crystals--;AddBuddyFriend(g,2);GameSave(g);SetDialogue(g,"鸣晶蜥吸收了星晶，背上的宝石依次亮起。","它能为古代装置供能，也能在战斗中放电。","按 TAB 切换伙伴。");}
            else Toast(g,"鸣晶蜥需要一枚星晶才愿意同行。");
            return;
        }
        // Rest beside the campfire after rescuing the first companion.
        if(g->area==AREA_CAMP&&g->lumi.owned && Dist(p->pos,(Vector2){4.1f,12.5f})<1.25f) {
            p->hp=fminf(p->maxHp,p->hp+22.0f);
            Toast(g,"在营火旁休息了一会儿，恢复了生命。");
            Burst(g,WorldScreen((Vector2){4.1f,12.5f}),C_GOLD,10);
            return;
        }
        // Ruin puzzle.
        if(g->area==AREA_RUINS&&Dist(p->pos,g->world.shrine)<1.7f&&!g->shrineSolved) {
            if(g->lumi.owned&&g->splash.owned&&g->spark.owned&&p->shells>=1) {
                g->shrineSolved=true;p->shells--;GameSave(g);
                Burst(g,WorldScreen(g->world.shrine),C_GOLD,40);
                SetDialogue(g,"三种伙伴的力量在遗迹中央交汇。","水纹显现了线路，星光照亮符文，晶弧重新接通能源。","灯塔庭院的封印解除了。");
            } else Toast(g,"遗迹需要三位伙伴协力，并带来一枚潮汐贝壳。");
            return;
        }
        // Final encounter trigger.
        if(g->area==AREA_LIGHTHOUSE&&g->shrineSolved&&Dist(p->pos,g->world.bossPos)<2.0f&&!g->bossDefeated) {
            AddEnemy(g,ENEMY_BOSS,g->world.bossPos.x,g->world.bossPos.y,150);EnterBattle(g,g->enemyCount-1);return;
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
        if(g->area==AREA_CAMP&&Dist(p->pos,(Vector2){3,10})<1.6f) {
            SetDialogueAs(g,"守望者","灯塔熄灭后，岛上的小生灵都变得不安。","找到三位伙伴，唤醒潮汐遗迹，灯塔就能重新点亮。","果实、贝壳和星晶散落在岛上，按 E 采集。");
            return;
        }
    }
    // The final scene is entered only through the explicit E interaction above.
    if(g->shrineSolved&&g->enemyCount>0&&!g->enemies[g->enemyCount-1].alive)g->bossDefeated=true;

    UpdatePlayerCamera(g,dt);
    if(g->toastTimer>0)g->toastTimer-=dt;
}
