#include "game.h"
#include <math.h>
#include <stdio.h>
static Texture2D TEX_HERO={0}, TEX_LUMI={0}, TEX_MOATAIL={0}, TEX_GECKO={0}, TEX_KEEPER={0};
static Texture2D TEX_HERO_PIXEL={0}, TEX_LUMI_PIXEL={0}, TEX_MOATAIL_PIXEL={0}, TEX_GECKO_PIXEL={0}, TEX_TILES={0}, TEX_PROPS={0};
static Texture2D TEX_ISO_TILES={0}, TEX_HERO_WALK={0}, TEX_ENV_PROPS={0}, TEX_LEAFLING={0}, TEX_GROVE_MOTE={0};
static void DrawPortrait(Texture2D tex, Vector2 p, float w, float h, float bob) {
    if (tex.id == 0) return;
    Rectangle src={0,0,(float)tex.width,(float)tex.height};
    Rectangle dst={p.x-w*0.5f,p.y-h-bob,w,h};
    DrawTexturePro(tex,src,dst,(Vector2){0,0},0,WHITE);
}
static Vector2 Iso(float x,float y,float z) { return (Vector2){(x-y)*31.0f,(x+y)*15.5f-z}; }
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
static void DrawEnvProp(int index,Vector2 p,float w,float h) {
    if(!TEX_ENV_PROPS.id)return;
    Rectangle src={(float)(index*96),0,96,96};
    Rectangle dst={p.x-w*.5f,p.y-h,w,h};
    DrawTexturePro(TEX_ENV_PROPS,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawTree(Vector2 p,float s,float t) {
    if(TEX_ENV_PROPS.id) {
        int variant=((int)(p.x+p.y))%3;
        DrawEnvProp(variant,p,82*s,92*s);
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
static void DrawMote(Vector2 p,float t) {
    float bob=sinf(t*4.6f)*3.0f;
    DrawEllipse((int)p.x,(int)p.y+5,12,4,(Color){71,92,100,75});
    if(TEX_GROVE_MOTE.id){int frame=(int)(t*2.5f)%4;Rectangle src={(float)(frame*32),0,32,32};Rectangle dst={p.x-21,p.y-45-bob,42,42};DrawTexturePro(TEX_GROVE_MOTE,src,dst,(Vector2){0,0},0,WHITE);return;}
    DrawCircleV((Vector2){p.x,p.y-11-bob},11,(Color){168,183,211,255});
    DrawTriangle((Vector2){p.x-7,p.y-17-bob},(Vector2){p.x,p.y-29-bob},(Vector2){p.x+7,p.y-17-bob},(Color){202,195,226,255});
    DrawCircleV((Vector2){p.x-4,p.y-12-bob},1.7f,C_INK);
    DrawCircleV((Vector2){p.x+4,p.y-12-bob},1.7f,C_INK);
}
static void DrawLeafling(Vector2 p,float t) {
    float bob=sinf(t*4.0f)*2.5f;
    DrawEllipse((int)p.x,(int)p.y+5,11,4,(Color){70,105,76,75});
    if(TEX_LEAFLING.id){int frame=(int)(t*2.5f)%4;Rectangle src={(float)(frame*32),0,32,32};Rectangle dst={p.x-21,p.y-45-bob,42,42};DrawTexturePro(TEX_LEAFLING,src,dst,(Vector2){0,0},0,WHITE);return;}
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
        int frame=(int)(t*2.5f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-22,p.y-46-sinf(t*4.4f)*2.0f,44,44};
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
        Rectangle dst={p.x-22,p.y-48-bob,44,48};
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
    RENDER_KEEPER, RENDER_LUMI, RENDER_MOATAIL, RENDER_GECKO, RENDER_PLAYER, RENDER_FIREFLY, RENDER_LEAFLING, RENDER_MOTE
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
        int frame=(int)(t*2.5f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-22,p.y-46-sinf(t*4)*2.0f,44,44};
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
        int frame=(int)(t*2.5f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-22,p.y-46-sinf(t*5)*2.0f,44,44};
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
        int tile=((x+y)&1)?0:1;                 // grass variants
        if(g->world.kind[y][x]==1)tile=3;        // solid rocks/tree roots
        if(g->world.kind[y][x]==2)tile=2;        // sandy footpaths
        if(g->world.kind[y][x]==3)tile=3;        // island boundary stone
        if(x>=8&&x<=13&&y>=3&&y<=8)tile=(x+y)%2; // flower grove grass
        if(x>=14&&x<=19&&y>=4&&y<=10)tile=3;    // old stone ruins
        DrawIsoTile(x,y,tile,c);
    }
    // A small tidal pool and stepping-stone edge.
    for(int y=3;y<6;y++)for(int x=2;x<6;x++)DrawIsoTile(x,y,4,(Color){119,191,199,255});

    // Sort elevated objects by isometric depth (x+y): characters can pass behind trees.
    RenderItem items[64]; int count=0;
    for(int y=1;y<MAPH-1;y++)for(int x=1;x<MAPW-1;x++) {
        if(g->world.kind[y][x]==1)
            PushRenderItem(items,&count,RENDER_TREE,(x*3+y)%3,(Vector2){(float)x,(float)y},.82f+((x+y)%3)*.08f);
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
    if(!g->groveMoteDefeated)PushRenderItem(items,&count,RENDER_MOTE,0,(Vector2){13.2f,9.2f},1.0f);
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
                else if(it->index==5)DrawEnvProp(7,p,52,52);
                else if(it->index==6)DrawEnvProp(5,p,44,46);
                else if(it->index==7)DrawEnvProp(9,p,84,112);
                else if(it->index==9)DrawProp(9,p,32,36);
                else if(it->index==10)DrawProp(10,p,46,38);
                else if(it->index==11)DrawProp(11,p,36,30);
                else if(it->index==12)DrawEnvProp(3,p,48*it->scale,52*it->scale);
                else if(it->index==13)DrawEnvProp(4,p,62*it->scale,40*it->scale);
                else if(it->index==14)DrawEnvProp(5,p,54*it->scale,62*it->scale);
                else if(it->index==15)DrawEnvProp(2,p,62*it->scale,48*it->scale);
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
            case RENDER_MOTE: DrawMote(p,g->time); break;
            case RENDER_PLAYER: DrawPlayer(p,g->time,Vector2Length(g->player.vel)>.1f,g->player.facing); break;
        }
    }
    for(int i=0;i<MAX_PARTICLES;i++)if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];
        DrawCircleV(p->p,p->size*(p->life/p->maxLife),Fade(p->c,p->life/p->maxLife));
    }
    EndMode2D();
    float night=0.5f+0.5f*cosf(g->dayClock*PI2);
    if(night>0.62f)DrawRectangle(0,0,SW,SH,Fade((Color){43,59,99,255},(night-0.62f)*0.22f));
    if(g->weather==1) { for(int i=0;i<72;i++){int x=(i*173+(int)(g->time*115))%SW;int y=(i*97+(int)(g->time*205))%SH;DrawLine(x,y,x-5,y+11,Fade((Color){183,211,224,255},.55f));} }
    else if(g->weather==2) { for(int i=0;i<24;i++){int x=(i*97+(int)(sinf(g->time+i)*18)+SW)%SW;int y=(i*53+(int)(cosf(g->time*.7f+i)*12)+SH)%SH;DrawCircle(x,y,2,Fade(C_GOLD,.6f));} }
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
    Buddy *active=CurrentBuddy(g);
    DrawText(TextFormat("当前伙伴 Lv.%d  XP %d/50",active->level,active->xp),1040,124,13,(Color){104,119,93,255});
    Panel((Rectangle){20,652,720,48});
    DrawText("WASD移动 SHIFT冲刺 E互动 TAB伙伴 J手记 I背包 M地图 F5保存",38,667,13,(Color){91,115,96,255});
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
    DrawText(g->groveMoteDefeated?"微光兽 — 已击退：林间小径恢复安全。":"微光兽 — 尚未击退：它守在林地旧路旁。",303,416,18,C_INK);
    DrawText("伙伴同行",303,445,21,(Color){104,135,94,255});
    DrawText(TextFormat("苔灯灵  %s       沫尾兽  %s       鸣晶蜥  %s",g->lumi.owned?"同行中":"未发现",g->splash.owned?"同行中":"未发现",g->spark.owned?"同行中":"未发现"),303,480,18,C_INK);
    DrawText("资源",303,525,21,(Color){104,135,94,255});
    DrawText(TextFormat("果实 %d     潮汐贝壳 %d     星晶 %d",g->player.berries,g->player.shells,g->player.crystals),303,560,18,C_INK);
    DrawText("F5 保存   F9 读取   按 J 或 ESC 关闭",760,570,16,(Color){132,137,111,255});
}
static void RenderInventory(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){300,120,680,470});
    DrawText("探 险 背 包",350,150,32,(Color){74,105,83,255});
    DrawLine(350,200,930,200,(Color){220,211,183,255});
    DrawText(TextFormat("岛屿果实       x %d",g->player.berries),360,238,22,C_INK);
    DrawText(TextFormat("潮汐贝壳       x %d",g->player.shells),360,285,22,C_INK);
    DrawText(TextFormat("星晶           x %d",g->player.crystals),360,332,22,C_INK);
    DrawText(TextFormat("生命           %d / %d",(int)g->player.hp,(int)g->player.maxHp),360,379,22,C_INK);
    DrawText("伙伴成长",360,432,22,(Color){104,135,94,255});
    DrawText(TextFormat("苔灯灵 Lv.%d   沫尾兽 Lv.%d   鸣晶蜥 Lv.%d",g->lumi.level,g->splash.level,g->spark.level),360,470,19,C_INK);
    DrawText("按 I / ESC 关闭",760,550,16,(Color){132,137,111,255});
}
static void RenderMap(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){250,90,780,540});
    DrawText("岛 屿 地 图",300,120,32,(Color){74,105,83,255});
    const char *names[4]={"海岸营地","辉光林地","潮汐遗迹","灯塔庭院"};
    Vector2 pts[4]={{390,320},{560,250},{745,330},{900,220}};
    for(int i=0;i<4;i++) {
        bool unlocked=(i==0)||(i==1&&g->lumi.owned)||(i==2&&g->splash.owned)||(i==3&&g->shrineSolved);
        DrawCircleV(pts[i],28,unlocked?(Color){144,190,132,255}:(Color){181,184,164,255});
        DrawCircleV(pts[i],13,(Color){255,240,196,255});
        DrawText(names[i],(int)pts[i].x-45,(int)pts[i].y+42,17,C_INK);
        if(i<3)DrawLineEx(pts[i],pts[i+1],5,(Color){185,177,139,255});
    }
    DrawCircleV((Vector2){390+(g->player.pos.x-3)*27,320+(g->player.pos.y-12)*15},7,C_RED);
    DrawText("红点：当前位置   灰色区域：尚未解锁",320,545,18,(Color){105,121,97,255});
    DrawText("按 M / ESC 关闭",800,580,16,(Color){132,137,111,255});
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
    DrawText("伙伴协同战斗",55,40,30,WHITE);
    DrawText(TextFormat("回合 %d   %s",g->battleRound,g->enemySoaked?"敌人：潮湿（电击伤害提升）":"观察敌人状态"),55,82,18,(Color){224,232,215,255});
    Enemy *e=&g->enemies[g->battleEnemy];
    DrawText(TextFormat("守卫 HP  %d / %d",(int)e->hp,(int)e->maxHp),760,80,22,WHITE);
    DrawRectangle(760,112,340,17,(Color){70,75,85,255});
    DrawRectangle(760,112,(int)(340*fmaxf(0,e->hp/e->maxHp)),17,C_PINK);
    DrawCircle(380,365,42,(Color){153,201,143,255});
    DrawCircle(380,365,30,(Color){219,232,180,255});
    DrawCircle(370,361,3,C_INK);DrawCircle(390,361,3,C_INK);
    DrawText("选择伙伴技能",70,525,23,WHITE);
    const char *opts[3]={"1  苔灯灵：治疗 + 微光攻击","2  沫尾兽：潮湿水流","3  鸣晶蜥：晶弧放电（潮湿克制）"};
    for(int i=0;i<3;i++) {
        Rectangle r={70,565+i*42,440,34};
        DrawRectangleRounded(r,.2f,8,i==g->battleChoice?(Color){255,226,158,255}:(Color){238,236,218,235});
        DrawText(opts[i],88,573+i*42,18,(Color){64,82,76,255});
    }
    DrawText(g->battleTurn==0?"选择技能并按 Enter":"敌方行动：按 Enter 继续",760,570,21,WHITE);
    DrawText("技能联动：先喷水，再使用晶弧可造成额外伤害。",760,610,17,(Color){226,236,222,255});
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
    for(int i=0;i<12;i++) {
        float a=g->time*0.35f+i*PI2/12.0f;
        Vector2 q={640+cosf(a)*(190+12*sinf(g->time*2+i)),150+sinf(a)*(110+10*cosf(g->time+i))};
        DrawCircleV(q,3.0f,Fade(i%2?C_GOLD:C_BLUE,.65f));
    }
    DrawCircle(640,160,62,Fade(C_GOLD,.10f+.06f*sinf(g->time*2)));
    DrawText("灯塔重新亮起了",435,475,38,(Color){255,246,218,255});
    DrawText("伙伴们的光芒，照亮了整座岛屿。",405,535,23,(Color){218,231,211,255});
    DrawText("第一章 完成   ·   按 R 再次探索",430,620,20,(Color){255,222,154,255});
}
void RenderInit(void) {
    const char *paths[]={
        "assets/characters/hero/hero_walk_4dir_64.png",
        "assets/characters/companions/moss_lumi_idle_64.png",
        "assets/characters/companions/moatail_idle_64.png",
        "assets/characters/companions/crystal_gecko_idle_64.png",
        "assets/characters/companions/leafling_idle_64.png",
        "assets/characters/companions/grove_mote_idle_64.png",
        "assets/environment/environment_props_96.png",
        "assets/characters/png/camp_keeper.png",
        "assets/tiles/png/island_iso_tiles.png",
        "assets/tiles/png/environment_props.png"
    };
    Texture2D *targets[]={&TEX_HERO_WALK,&TEX_LUMI_PIXEL,&TEX_MOATAIL_PIXEL,&TEX_GECKO_PIXEL,&TEX_LEAFLING,&TEX_GROVE_MOTE,&TEX_ENV_PROPS,&TEX_KEEPER,&TEX_ISO_TILES,&TEX_PROPS};
    for(int i=0;i<10;i++) {
        if(FileExists(paths[i])) {
            *targets[i]=LoadTexture(paths[i]);
            if(targets[i]->id)SetTextureFilter(*targets[i],TEXTURE_FILTER_POINT);
        }
    }
}
void RenderClose(void) {
    Texture2D *textures[]={&TEX_HERO_WALK,&TEX_LUMI_PIXEL,&TEX_MOATAIL_PIXEL,&TEX_GECKO_PIXEL,&TEX_LEAFLING,&TEX_GROVE_MOTE,&TEX_ENV_PROPS,&TEX_KEEPER,&TEX_ISO_TILES,&TEX_PROPS};
    for(int i=0;i<10;i++)if(textures[i]->id)UnloadTexture(*textures[i]);
}
void GameRender(Game *g) {
    if(g->scene==SCENE_TITLE)RenderTitle(g);
    else if(g->scene==SCENE_BATTLE)RenderBattle(g);
    else if(g->scene==SCENE_END)RenderEnd(g);
    else {RenderWorld(g);RenderHUD(g);if(g->scene==SCENE_DIALOGUE)RenderDialogue(g);if(g->journalOpen)RenderJournal(g);if(g->inventoryOpen)RenderInventory(g);if(g->mapOpen)RenderMap(g);}
}