#include "game.h"
#include <math.h>
#include <stdio.h>
static Font UI_FONT={0};
static const int UI_CODEPOINTS[]={32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,183,8212,8220,8221,8230,12288,12289,12290,19968,19977,19978,19981,19982,19990,20013,20026,20027,20046,20063,20102,20110,20113,20114,20132,20142,20154,20173,20195,20197,20204,20214,20237,20241,20249,20250,20260,20276,20284,20301,20307,20320,20351,20379,20381,20415,20445,20572,20799,20808,20809,20811,20837,20840,20851,20860,20861,20877,20889,20914,20915,20986,20987,20999,21040,21046,21050,21069,21147,21152,21160,21161,21191,21253,21306,21319,21327,21355,21360,21457,21462,21463,21464,21475,21476,21482,21487,21494,21507,21508,21512,21516,21518,21543,21560,21561,21629,21644,21709,21796,21916,21943,22218,22238,22242,22270,22280,22312,22320,22495,22522,22612,22771,22788,22797,22806,22810,22830,22833,22836,22987,23384,23427,23432,23433,23436,23450,23453,23454,23475,23481,23494,23519,23547,23548,23553,23567,23578,23601,23614,23618,23679,23707,23736,24038,24050,24102,24110,24182,24212,24231,24237,24320,24341,24359,24403,24405,24418,24448,24452,24456,24471,24494,24515,24577,24674,24687,24819,24840,24847,24895,25104,25110,25112,25163,25165,25171,25214,25216,25226,25260,25320,25321,25353,25377,25442,25506,25509,25552,25910,25915,25918,25928,25932,25937,25955,25972,25991,26007,26032,26041,26049,26053,26080,26087,26102,26143,26159,26174,26230,26263,26377,26395,26410,26412,26426,26469,26519,26522,26524,26530,26597,26631,26723,27425,27426,27573,27668,27700,27719,27728,27773,27809,27819,27835,27861,27873,27922,27963,27969,28014,28023,28165,28287,28304,28418,28526,28779,28781,28783,28784,28789,28857,29031,29060,29255,29256,29289,29366,29616,29702,29983,29992,30005,30028,30041,30103,30333,30340,30446,30475,30528,30707,30784,31181,31192,31227,31243,31456,31526,31532,32034,32418,32441,32445,32447,32454,32463,32469,32473,32487,32493,32622,32769,32773,32852,32972,33016,33021,33394,33426,33469,33492,33719,33828,33829,33853,33976,34067,34255,34276,34411,34597,34892,34987,35013,35201,35266,35299,35753,35760,35835,35848,36125,36133,36164,36208,36215,36291,36319,36335,36454,36530,36716,36731,36745,36793,36807,36817,36820,36825,36827,36828,36830,36857,36864,36873,36890,36896,36951,37117,37266,37319,37324,37325,37327,37492,38145,38190,38271,38378,38381,38388,38395,38431,38451,38459,38498,38500,38505,38544,38598,38632,38654,38656,38706,38752,39063,39069,39118,39128,39295,39321,39564,40483,40736,65281,65288,65289,65292,65306,65307};
static bool UIFontHasRequiredGlyphs(Font f) {
    static const int required[]={22833,33853,30340,28783,22612,28023,23736,33829,33829,26519,22320,22270,23545,35805};
    for(int i=0;i<(int)(sizeof(required)/sizeof(required[0]));i++) {
        if(GetGlyphIndex(f,required[i])<=0) {
            TraceLog(LOG_WARNING,"Mist Isle: font missing required glyph U+%04X",required[i]);
            return false;
        }
    }
    return true;
}
static void LoadUIFont(void) {
    // Prefer standalone TTF files: raylib's stb_truetype path is more reliable with TTF than TTC collections.
    const char *paths[]={"assets/fonts/NotoSansSC-VF.ttf","C:/Windows/Fonts/simhei.ttf","C:/Windows/Fonts/msyh.ttc","C:/Windows/Fonts/simsun.ttc"};
    for(int i=0;i<4 && UI_FONT.texture.id==0;i++) {
        if(!FileExists(paths[i]))continue;
        Font candidate=LoadFontEx(paths[i],32,(int*)UI_CODEPOINTS,(int)(sizeof(UI_CODEPOINTS)/sizeof(UI_CODEPOINTS[0])));
        if(candidate.texture.id && UIFontHasRequiredGlyphs(candidate)) {
            UI_FONT=candidate;
            SetTextureFilter(UI_FONT.texture,TEXTURE_FILTER_BILINEAR);
            TraceLog(LOG_INFO,"Mist Isle: validated Chinese UI font loaded from %s (%d glyphs)",paths[i],UI_FONT.glyphCount);
        } else if(candidate.texture.id) {
            UnloadFont(candidate);
        }
    }
    if(!UI_FONT.texture.id)TraceLog(LOG_ERROR,"Mist Isle: no usable Chinese UI font; all candidate fonts failed glyph validation.");
}
static void UnloadUIFont(void) { if(UI_FONT.texture.id)UnloadFont(UI_FONT); }
static void DrawUIText(const char *text,int x,int y,int size,Color tint) {
    if(UI_FONT.texture.id)DrawTextEx(UI_FONT,text,(Vector2){(float)x,(float)y},(float)size,1.0f,tint);
    else DrawText(text,x,y,size,tint);
}

static Texture2D TEX_HERO={0}, TEX_LUMI={0}, TEX_MOATAIL={0}, TEX_GECKO={0}, TEX_KEEPER={0};
static Texture2D TEX_HERO_PIXEL={0}, TEX_LUMI_PIXEL={0}, TEX_MOATAIL_PIXEL={0}, TEX_GECKO_PIXEL={0}, TEX_TILES={0}, TEX_PROPS={0};
static Texture2D TEX_ISO_TILES={0}, TEX_HERO_WALK={0}, TEX_ENV_PROPS={0}, TEX_LEAFLING={0}, TEX_GROVE_MOTE={0}, TEX_AREA_PROPS={0};
static void DrawPortrait(Texture2D tex, Vector2 p, float w, float h, float bob) {
    if (tex.id == 0) return;
    Rectangle src={0,0,(float)tex.width,(float)tex.height};
    Rectangle dst={p.x-w*0.5f,p.y-h-bob,w,h};
    DrawTexturePro(tex,src,dst,(Vector2){0,0},0,WHITE);
}
static Vector2 WorldToScreen(float x,float y) { return (Vector2){(x+0.5f)*TILE_SIZE,(y+0.5f)*TILE_SIZE}; }
static unsigned int TileHash(int x,int y,int salt) {
    unsigned int n=(unsigned int)(x*374761393u+y*668265263u+salt*2246822519u);
    n=(n^(n>>13))*1274126177u; return n^(n>>16);
}
static void DrawTopDownTile(int x,int y,Area area,int kind,float time) {
    Vector2 p=WorldToScreen((float)x,(float)y);
    Rectangle tile={p.x-TILE_SIZE*.5f,p.y-TILE_SIZE*.5f,TILE_SIZE+1,TILE_SIZE+1};
    Color base;
    if(kind==4) base=(Color){67,158,190,255};
    else if(kind==3) base=area==AREA_RUINS?(Color){126,132,119,255}:(Color){105,126,112,255};
    else if(kind==2) base=(Color){207,180,126,255};
    else if(area==AREA_GROVE) base=(Color){91,151,91,255};
    else if(area==AREA_RUINS) base=(Color){151,151,130,255};
    else if(area==AREA_LIGHTHOUSE) base=(Color){158,157,133,255};
    else base=(Color){111,166,91,255};
    if(kind==1)base=(Color){67,113,72,255};
    DrawRectangleRec(tile,base);
    unsigned int hash=TileHash(x,y,(int)area);
    if(kind==4) {
        float wave=6.0f*sinf(time*1.7f+y*.65f+x*.35f);
        DrawLineEx((Vector2){tile.x+7,tile.y+14+wave*.15f},(Vector2){tile.x+TILE_SIZE-9,tile.y+14+wave*.15f},2.0f,Fade((Color){190,229,222,255},.32f));
        if((hash%4)==0)DrawLineEx((Vector2){tile.x+12,tile.y+34-wave*.12f},(Vector2){tile.x+30,tile.y+34-wave*.12f},1.0f,Fade(WHITE,.25f));
    } else if(kind==2) {
        for(int i=0;i<4;i++){float px=tile.x+7+(float)((hash>>(i*5))%43);float py=tile.y+8+(float)((hash>>(i*4+3))%40);DrawPixel((int)px,(int)py,(Color){230,207,157,255});}
    } else if(kind==3) {
        DrawRectangleLinesEx((Rectangle){tile.x+2,tile.y+2,TILE_SIZE-4,TILE_SIZE-4},1.0f,Fade((Color){70,84,78,255},.2f));
        DrawLine((int)tile.x+7,(int)(tile.y+TILE_SIZE*.5f),(int)(tile.x+TILE_SIZE-7),(int)(tile.y+TILE_SIZE*.5f),Fade(WHITE,.12f));
    } else {
        for(int i=0;i<5;i++){
            float px=tile.x+5+(float)((hash>>(i*5))%47);
            float py=tile.y+5+(float)((hash>>(i*3+7))%47);
            Color fleck=(i%2)?(Color){158,198,119,255}:(Color){83,142,80,255};
            DrawPixel((int)px,(int)py,Fade(fleck,.55f));
        }
        if((hash%7)==0){DrawCircle((int)(tile.x+17),(int)(tile.y+18),2,(Color){247,230,173,255});DrawPixel((int)(tile.x+20),(int)(tile.y+15),WHITE);}
    }
}
static void DrawAreaProp(int index,Vector2 p,float w,float h) {
    if(!TEX_AREA_PROPS.id)return;
    Rectangle src={(float)(index*96),0,96,96};
    Rectangle dst={p.x-w*.5f,p.y-h,w,h};
    DrawTexturePro(TEX_AREA_PROPS,src,dst,(Vector2){0,0},0,WHITE);
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
    const float footY=p.y+8.0f;
    DrawEllipse((int)p.x,(int)footY,13,4,(Color){48,66,57,105});
    if(TEX_KEEPER.id) {
        Rectangle src={0,0,32,32};
        Rectangle dst={p.x-18,footY-36+sinf(t*2.0f)*0.35f,36,36};
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
    const float footY=p.y+7.0f;
    DrawEllipse((int)p.x,(int)footY,12,4,(Color){48,66,57,95});
    if(TEX_GROVE_MOTE.id){int frame=(int)(t*2.5f)%4;Rectangle src={(float)(frame*32),0,32,32};Rectangle dst={p.x-20,footY-40-bob,40,40};DrawTexturePro(TEX_GROVE_MOTE,src,dst,(Vector2){0,0},0,WHITE);return;}
    DrawCircleV((Vector2){p.x,p.y-11-bob},11,(Color){168,183,211,255});
    DrawTriangle((Vector2){p.x-7,p.y-17-bob},(Vector2){p.x,p.y-29-bob},(Vector2){p.x+7,p.y-17-bob},(Color){202,195,226,255});
    DrawCircleV((Vector2){p.x-4,p.y-12-bob},1.7f,C_INK);
    DrawCircleV((Vector2){p.x+4,p.y-12-bob},1.7f,C_INK);
}
static void DrawLeafling(Vector2 p,float t) {
    float bob=sinf(t*4.0f)*2.5f;
    const float footY=p.y+7.0f;
    DrawEllipse((int)p.x,(int)footY,11,4,(Color){48,66,57,95});
    if(TEX_LEAFLING.id){int frame=(int)(t*2.5f)%4;Rectangle src={(float)(frame*32),0,32,32};Rectangle dst={p.x-20,footY-40-bob,40,40};DrawTexturePro(TEX_LEAFLING,src,dst,(Vector2){0,0},0,WHITE);return;}
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
    const float footY=p.y+8.0f;
    DrawEllipse((int)p.x,(int)footY,16,6,(Color){48,66,57,95});
    if (TEX_LUMI_PIXEL.id) {
        int frame=(int)(t*2.5f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-21,footY-42-sinf(t*4.4f)*1.3f,42,42};
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
    float bob=moving?fabsf(sinf(t*13))*1.2f:0.0f;
    const float footY=p.y+9.0f;
    DrawEllipse((int)p.x,(int)footY,13,4,(Color){48,66,57,105});
    if (TEX_HERO_WALK.id) {
        int frame = moving ? ((int)(t*8.0f)%4) : 0;
        Rectangle src={(float)(frame*32),(float)(facing*32),32,32};
        Rectangle dst={p.x-21,footY-42-bob,42,42};
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
    DrawEllipse((int)p.x,(int)p.y+8,34,10,(Color){66,95,85,95});
    if(TEX_ENV_PROPS.id) DrawEnvProp(4,p,82,82);
    else {
        DrawRectangle((int)p.x-14,(int)p.y-25,28,31,(Color){156,174,149,255});
        DrawRectangle((int)p.x-8,(int)p.y-45,16,22,(Color){198,209,171,255});
    }
    DrawCircleV((Vector2){p.x,p.y-56-sinf(t*2)*3},solved?11:7,c);
    DrawCircleV((Vector2){p.x,p.y-56-sinf(t*2)*3},3,WHITE);
    if(solved) {
        DrawCircleV((Vector2){p.x,p.y-55},38,Fade(c,.15f));
        for(int i=0;i<4;i++){float a=t*.7f+i*PI2/4;DrawCircleV((Vector2){p.x+cosf(a)*35,p.y-42+sinf(a)*22},2.5f,Fade(C_GOLD,.8f));}
    }
}
static void DrawPickup(Pickup *p) {
    Vector2 s=WorldToScreen(p->tile.x,p->tile.y);float bob=sinf(p->phase)*3;
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
    if(*count>=192)return;
    items[*count]=(RenderItem){grid.y+((kind==RENDER_PLAYER||kind==RENDER_TREE||kind==RENDER_PROP)?0.16f:0.0f),kind,index,grid,scale};
    (*count)++;
}
static void DrawMoatail(Vector2 p,float t) {
    const float footY=p.y+8.0f;
    DrawEllipse((int)p.x,(int)footY,15,5,(Color){48,66,57,95});
    if(TEX_MOATAIL_PIXEL.id) {
        int frame=(int)(t*2.5f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-21,footY-42-sinf(t*4)*1.3f,42,42};
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
    const float footY=p.y+8.0f;
    DrawEllipse((int)p.x,(int)footY,15,5,(Color){48,66,57,95});
    if(TEX_GECKO_PIXEL.id) {
        int frame=(int)(t*2.5f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-21,footY-42-sinf(t*5)*1.3f,42,42};
        DrawTexturePro(TEX_GECKO_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_GECKO.id) DrawPortrait(TEX_GECKO,p,58,58,sinf(t*5)*2.0f);
    else {
        DrawCircleV((Vector2){p.x,p.y-10},11,(Color){155,154,214,255});
        DrawTriangle((Vector2){p.x-7,p.y-17},(Vector2){p.x,p.y-32},(Vector2){p.x+7,p.y-17},(Color){179,218,229,255});
        DrawCircleV((Vector2){p.x-4,p.y-11},2,(Color){50,60,88,255});
        DrawCircleV((Vector2){p.x+4,p.y-11},2,(Color){50,60,88,255});
    }
}
static void DrawCloudPuff(float x,float y,float s,Color color) {
    DrawCircle((int)(x-18*s),(int)y,(int)(18*s),color);
    DrawCircle((int)x,(int)(y-7*s),(int)(23*s),color);
    DrawCircle((int)(x+22*s),(int)y,(int)(17*s),color);
    DrawRectangle((int)(x-18*s),(int)(y-2*s),(int)(40*s),(int)(15*s),color);
}
static void DrawSeaBackdrop(float t) {
    Color top=(Color){177,220,214,255},bottom=(Color){94,174,184,255};
    DrawRectangleGradientV(0,0,WORLD_PIXEL_W,WORLD_PIXEL_H,top,bottom);
    // Distant headlands create a horizon instead of a blank cyan field.
    DrawCircle(80,185,145,(Color){104,157,145,255});
    DrawCircle(190,205,112,(Color){123,174,153,255});
    DrawCircle(1135,185,150,(Color){101,153,146,255});
    DrawCircle(1040,205,105,(Color){128,177,155,255});
    DrawRectangle(0,205,WORLD_PIXEL_W,65,(Color){126,184,168,255});
    DrawCloudPuff(275,104,1.0f,(Color){236,242,216,190});
    DrawCloudPuff(880,130,.78f,(Color){239,244,221,170});
    for(int row=0;row<16;row++) {
        float y=295+row*52.0f;
        for(int x=-30;x<WORLD_PIXEL_W+50;x+=118) {
            float phase=t*.22f+row*.8f+x*.012f;
            float yy=y+sinf(phase)*5.0f;
            DrawLineEx((Vector2){(float)x,yy},(Vector2){x+43.0f,yy-2.0f},2.0f,Fade((Color){224,242,218,255},.25f));
            DrawLineEx((Vector2){x+49.0f,yy+5.0f},(Vector2){x+88.0f,yy+3.0f},1.5f,Fade((Color){65,139,153,255},.22f));
        }
    }
    // A few distant sail silhouettes add scale to the island.
    DrawTriangle((Vector2){1000,285},(Vector2){1000,220},(Vector2){1040,285},(Color){242,231,194,180});
    DrawLineEx((Vector2){1000,215},(Vector2){1000,294},2,(Color){76,116,112,190});
    DrawTriangle((Vector2){215,320},(Vector2){215,270},(Vector2){246,320},(Color){242,231,194,150});
    DrawLineEx((Vector2){215,266},(Vector2){215,325},2,(Color){76,116,112,170});
}
static void DrawForestBackdrop(float t) {
    DrawRectangleGradientV(0,0,WORLD_PIXEL_W,WORLD_PIXEL_H,(Color){196,220,174,255},(Color){104,151,111,255});
    // Layered canopy silhouettes frame the playfield on all four sides.
    for(int layer=0;layer<3;layer++) {
        int y=95+layer*64;
        Color leaf=layer==0?(Color){103,150,105,210}:layer==1?(Color){79,132,91,205}:(Color){65,116,82,190};
        for(int x=-35;x<WORLD_PIXEL_W+60;x+=105) {
            float sway=sinf(t*.35f+x*.02f+layer)*5;
            DrawCircle((int)(x+sway),y,(int)(58-layer*7),leaf);
            DrawCircle((int)(x+38+sway),y+22,(int)(43-layer*5),Fade(leaf,.9f));
        }
    }
    DrawCloudPuff(250,165,.65f,(Color){235,240,204,90});
    DrawCloudPuff(990,125,.55f,(Color){235,240,204,75});
    for(int i=0;i<36;i++) {
        float x=(float)((i*197)%(int)WORLD_PIXEL_W), y=(float)(220+(i*83)%(int)(WORLD_PIXEL_H-220));
        DrawCircle((int)x,(int)y,2.0f+(i%3),Fade((Color){238,237,156,255},.25f));
    }
}
static void DrawRuinsBackdrop(float t) {
    DrawRectangleGradientV(0,0,WORLD_PIXEL_W,WORLD_PIXEL_H,(Color){179,191,180,255},(Color){91,111,112,255});
    DrawCircle(1030,125,78,Fade((Color){246,224,168,255},.45f));
    // Broken walls and distant pillars behind the playable island.
    for(int i=0;i<9;i++) {
        int x=i*165-30, base=300+(i%3)*22, w=78+(i%2)*24;
        DrawRectangle(x,base-120-(i%3)*25,w,145,(Color){91,119,112,170});
        DrawRectangle(x-12,base-128-(i%3)*25,w+24,12,(Color){116,139,124,180});
        DrawRectangle(x+12,base-100,w-24,22,(Color){179,189,164,120});
    }
    for(int i=0;i<30;i++) {
        int x=(i*137)%(int)WORLD_PIXEL_W,y=340+(i*71)%(int)(WORLD_PIXEL_H-340);
        DrawCircle(x,y,2+(i%3),Fade((Color){215,215,173,255},.2f));
    }
}
static void DrawLighthouseBackdrop(float t) {
    DrawRectangleGradientV(0,0,WORLD_PIXEL_W,WORLD_PIXEL_H,(Color){164,199,203,255},(Color){91,139,157,255});
    DrawCircle(1000,125,92,Fade((Color){255,224,157,255},.45f));
    DrawCircle(1000,125,58,Fade((Color){255,238,193,255},.38f));
    // Distant sea bands and a warm lighthouse beam across the sky.
    for(int i=0;i<13;i++) {
        int y=300+i*55;
        DrawLineEx((Vector2){0,(float)y+sinf(t*.2f+i)*4},(Vector2){(float)WORLD_PIXEL_W,(float)y+sinf(t*.2f+i)*4},2,Fade((Color){220,237,214,255},.2f));
    }
    DrawTriangle((Vector2){1000,125},(Vector2){WORLD_PIXEL_W,40},(Vector2){WORLD_PIXEL_W,230},Fade((Color){255,232,176,255},.12f));
    DrawCloudPuff(220,130,.7f,(Color){237,241,216,130});
    DrawCloudPuff(760,190,.55f,(Color){237,241,216,95});
}
static void DrawWorldBackdrop(Game *g) {
    Color surround=g->area==AREA_CAMP?(Color){83,163,178,255}:
        g->area==AREA_GROVE?(Color){66,112,78,255}:
        g->area==AREA_RUINS?(Color){84,101,101,255}:(Color){76,126,145,255};
    DrawRectangle(-SW,-SH,WORLD_PIXEL_W+2*SW,WORLD_PIXEL_H+2*SH,surround);
    switch(g->area) {
        case AREA_CAMP: DrawSeaBackdrop(g->time); break;
        case AREA_GROVE: DrawForestBackdrop(g->time); break;
        case AREA_RUINS: DrawRuinsBackdrop(g->time); break;
        default: DrawLighthouseBackdrop(g->time); break;
    }
}

static void RenderWorld(Game *g) {
    BeginMode2D(g->cam);
    DrawWorldBackdrop(g);

    // Orthographic top-down map: terrain follows screen cardinal directions.
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        int kind=0;
        if(g->world.kind[y][x]==1)kind=1;
        if(g->world.kind[y][x]==2)kind=2;
        if(g->world.kind[y][x]==3)kind=3;
        if(g->area==AREA_CAMP) {
            float nx=((float)x-10.5f)/11.0f, ny=((float)y-8.5f)/9.0f;
            float shore=nx*nx+ny*ny;
            if(shore>.88f)kind=4;
            else if(shore>.68f)kind=2;
            // Broad sandy paths connect the dock, campfire and three area gates.
            if((y>=8&&y<=10&&x>=3&&x<=19) ||
               (x>=17&&x<=19&&y>=3&&y<=15))kind=2;
            if(x>=1&&x<=3&&y>=11&&y<=15)kind=4;
            if(g->world.kind[y][x]==1)kind=1;
        } else if(g->area==AREA_GROVE) {
            if(x>=9&&x<=11&&y>=3&&y<=14)kind=4;
            if((x*3+y)%13==0&&kind==0)kind=2;
            if((y>=8&&y<=10&&x>=2&&x<=19))kind=2;
            if(x==0||y==0||x==MAPW-1||y==MAPH-1)kind=1;
        } else if(g->area==AREA_RUINS) {
            if(kind==0)kind=3;
            if((x+y)%6==0&&kind==3)kind=2;
            if(x>=9&&x<=11&&y>=3&&y<=14)kind=4;
            if(y>=8&&y<=10&&x>=2&&x<=19)kind=3;
            if(x==0||y==0||x==MAPW-1||y==MAPH-1)kind=3;
        } else {
            if(x<5||x>17||y<4||y>14)kind=2;
            if((x+y)%9==0&&kind==0)kind=3;
            if(y>=8&&y<=10&&x>=2&&x<=19)kind=3;
            if(x==0||y==0||x==MAPW-1||y==MAPH-1)kind=4;
        }
        DrawTopDownTile(x,y,g->area,kind,g->time);
    }

    // Sort objects by their ground-contact Y coordinate: walk behind trees from the top.
    RenderItem items[192]; int count=0;
    for(int y=1;y<MAPH-1;y++)for(int x=1;x<MAPW-1;x++) {
        if(g->world.kind[y][x]==1 || (g->area==AREA_GROVE&&(x==0||y==0||x==MAPW-1||y==MAPH-1)))
            PushRenderItem(items,&count,RENDER_TREE,(x*3+y)%3,(Vector2){(float)x,(float)y},.82f+((x+y)%3)*.08f);
    }
    // Each sub-map has its own authored prop composition.
    if(g->area==AREA_CAMP) {
        PushRenderItem(items,&count,RENDER_PROP,4,(Vector2){3.2f,11.4f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,20,(Vector2){3.0f,14.0f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,21,(Vector2){2.0f,15.0f},.9f);
        PushRenderItem(items,&count,RENDER_PROP,22,(Vector2){5.5f,10.5f},.9f);
        PushRenderItem(items,&count,RENDER_PROP,23,(Vector2){2.0f,12.0f},.7f);
        PushRenderItem(items,&count,RENDER_PROP,7,(Vector2){19.0f,2.2f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,16,(Vector2){2.1f,10.5f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,17,(Vector2){6.6f,10.9f},.82f);
        PushRenderItem(items,&count,RENDER_PROP,18,(Vector2){6.3f,12.2f},.72f);
        PushRenderItem(items,&count,RENDER_FIRE,8,(Vector2){4.1f,12.5f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,9,(Vector2){5.9f,10.3f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,10,(Vector2){6.6f,12.5f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,11,(Vector2){2.4f,14.0f},1.0f);
        // Gate markers on the hub plaza.
        PushRenderItem(items,&count,RENDER_PROP,28,g->world.groveGate,1.0f);
        PushRenderItem(items,&count,RENDER_PROP,28,g->world.ruinGate,1.0f);
        PushRenderItem(items,&count,RENDER_PROP,34,g->world.shrine,1.0f);
    } else if(g->area==AREA_GROVE) {
        PushRenderItem(items,&count,RENDER_PROP,28,(Vector2){2,9},.9f);
        for(int i=0;i<4;i++)PushRenderItem(items,&count,RENDER_PROP,24,(Vector2){6.0f+i*3.2f,3.0f+(i%2)*8.0f},.95f);
        PushRenderItem(items,&count,RENDER_PROP,25,(Vector2){7.5f,11.0f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,26,(Vector2){10.0f,13.0f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,27,(Vector2){15.0f,5.0f},.9f);
        PushRenderItem(items,&count,RENDER_PROP,12,(Vector2){6.0f,3.0f},.9f);
    } else if(g->area==AREA_RUINS) {
        PushRenderItem(items,&count,RENDER_PROP,34,(Vector2){2,9},.9f);
        PushRenderItem(items,&count,RENDER_PROP,28,(Vector2){16.0f,5.0f},1.25f);
        for(int i=0;i<4;i++)PushRenderItem(items,&count,RENDER_PROP,29,(Vector2){6.0f+i*3.6f,4.0f+(i%2)*8.0f},.95f);
        PushRenderItem(items,&count,RENDER_PROP,30,(Vector2){6.0f,13.0f},1.1f);
        PushRenderItem(items,&count,RENDER_PROP,31,(Vector2){17.0f,7.0f},1.0f);
        PushRenderItem(items,&count,RENDER_PROP,14,(Vector2){5.0f,5.0f},.8f);
    } else {
        PushRenderItem(items,&count,RENDER_PROP,14,(Vector2){2,9},.8f);
        PushRenderItem(items,&count,RENDER_PROP,33,(Vector2){17.0f,4.0f},1.2f);
        PushRenderItem(items,&count,RENDER_PROP,32,(Vector2){16.0f,12.0f},1.05f);
        PushRenderItem(items,&count,RENDER_PROP,34,(Vector2){10.0f,6.0f},.9f);
        PushRenderItem(items,&count,RENDER_PROP,35,(Vector2){10.0f,11.0f},1.0f);
        for(int i=0;i<4;i++)PushRenderItem(items,&count,RENDER_PROP,15,(Vector2){4.0f+i*3.0f,13.0f},.9f);
    }
    for(int i=0;i<g->world.pickupCount;i++) if(!g->world.pickups[i].taken)
        PushRenderItem(items,&count,RENDER_PICKUP,i,g->world.pickups[i].tile,1.0f);
    if(g->area==AREA_RUINS||g->area==AREA_LIGHTHOUSE)PushRenderItem(items,&count,RENDER_SHRINE,0,g->world.shrine,1.0f);
    if(g->area==AREA_CAMP) {
        PushRenderItem(items,&count,RENDER_KEEPER,0,(Vector2){3.0f,10.0f},1.0f);
        if(!g->lumi.owned)PushRenderItem(items,&count,RENDER_LUMI,0,(Vector2){4.0f,12.0f},1.0f);
    }
    if(g->lumi.owned)PushRenderItem(items,&count,RENDER_LUMI,0,g->lumi.pos,1.0f);
    if(g->splash.owned)PushRenderItem(items,&count,RENDER_MOATAIL,0,g->splash.pos,1.0f);
    if(g->spark.owned)PushRenderItem(items,&count,RENDER_GECKO,0,g->spark.pos,1.0f);
    if(g->area==AREA_GROVE) {
        Vector2 fireflyGrid={10.6f+sinf(g->fireflyPhase*0.8f)*0.22f,6.2f+cosf(g->fireflyPhase*0.65f)*0.18f};
        if(!g->fireflyDiscovered)PushRenderItem(items,&count,RENDER_FIREFLY,0,fireflyGrid,1.0f);
        if(!g->leaflingFed)PushRenderItem(items,&count,RENDER_LEAFLING,0,(Vector2){12.4f,8.0f},1.0f);
        if(!g->groveMoteDefeated)PushRenderItem(items,&count,RENDER_MOTE,0,(Vector2){13.2f,9.2f},1.0f);
    }
    PushRenderItem(items,&count,RENDER_PLAYER,0,g->player.pos,1.0f);

    for(int i=1;i<count;i++) {
        RenderItem key=items[i]; int j=i-1;
        while(j>=0 && items[j].depth>key.depth) { items[j+1]=items[j]; j--; }
        items[j+1]=key;
    }
    for(int i=0;i<count;i++) {
        RenderItem *it=&items[i]; Vector2 p=WorldToScreen(it->grid.x,it->grid.y);
        switch(it->kind) {
            case RENDER_TREE: p.y+=9.0f; if(g->area==AREA_RUINS)DrawEnvProp(5,p,48*it->scale,62*it->scale); else DrawTree(p,it->scale,g->time); break;
            case RENDER_PROP:
                p.y+=7.0f;
                if(it->index==4)DrawProp(4,p,72,68);
                else if(it->index==5)DrawEnvProp(7,p,52,52);
                else if(it->index==6)DrawEnvProp(5,p,44,46);
                else if(it->index==7)DrawEnvProp(9,p,84,112);
                else if(it->index==9)DrawProp(9,p,32,36);
                else if(it->index==10)DrawProp(10,p,46,38);
                else if(it->index==11)DrawEnvProp(11,p,52,48);
                else if(it->index==12)DrawEnvProp(3,p,48*it->scale,52*it->scale);
                else if(it->index==13)DrawEnvProp(4,p,62*it->scale,40*it->scale);
                else if(it->index==14)DrawEnvProp(5,p,54*it->scale,62*it->scale);
                else if(it->index==15)DrawEnvProp(2,p,62*it->scale,48*it->scale);
                else if(it->index==16)DrawEnvProp(10,p,76*it->scale,82*it->scale);
                else if(it->index==17)DrawEnvProp(11,p,68*it->scale,38*it->scale);
                else if(it->index==18)DrawEnvProp(8,p,38*it->scale,46*it->scale);
                else if(it->index>=20&&it->index<=35)DrawAreaProp(it->index-20,p,76*it->scale,82*it->scale);
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
    DrawUIText("MIST ISLE  /  CHAPTER 01",40,32,15,(Color){119,145,116,255});
    const char *areaName=g->area==AREA_CAMP?"海岸营地":g->area==AREA_GROVE?"辉光林地":g->area==AREA_RUINS?"潮汐遗迹":"灯塔庭院";
    DrawUIText(areaName,560,32,18,(Color){83,119,88,255});
    DrawCircle(397,42,8,Fade(C_GOLD,.75f));
    DrawUIText("失落的灯塔",40,54,28,C_INK);
    DrawUIText(TextFormat("生命 %d    果实 %d    贝壳 %d    星晶 %d",(int)g->player.hp,g->player.berries,g->player.shells,g->player.crystals),40,96,17,(Color){104,119,93,255});
    Panel((Rectangle){1020,18,238,92});
    DrawUIText("伙伴图鉴",1040,32,17,(Color){119,145,116,255});
    DrawUIText(TextFormat("苔灯灵 %s",g->lumi.owned?"已同行":"未发现"),1040,57,16,C_INK);
    DrawUIText(TextFormat("沫尾兽 %s",g->splash.owned?"已同行":"未发现"),1040,79,16,C_INK);
    DrawUIText(TextFormat("鸣晶蜥 %s",g->spark.owned?"已同行":"未发现"),1040,101,16,C_INK);
    Buddy *active=CurrentBuddy(g);
    DrawUIText(TextFormat("当前伙伴 Lv.%d  XP %d/50",active->level,active->xp),1040,124,13,(Color){104,119,93,255});
    Panel((Rectangle){20,652,720,48});
    DrawUIText("WASD移动 SHIFT冲刺 E互动 TAB伙伴 J手记 I背包 M地图 F5保存",38,667,13,(Color){91,115,96,255});
    if(g->area==AREA_CAMP && Dist(g->player.pos,g->world.groveGate)<1.6f) {
        Panel((Rectangle){530,585,220,40});DrawUIText("E  前往辉光林地",550,596,17,(Color){91,115,96,255});
    } else if(g->area==AREA_CAMP && Dist(g->player.pos,g->world.ruinGate)<1.6f) {
        Panel((Rectangle){530,585,220,40});DrawUIText("E  前往潮汐遗迹",550,596,17,(Color){91,115,96,255});
    } else if(g->area==AREA_CAMP && Dist(g->player.pos,g->world.shrine)<1.6f) {
        Panel((Rectangle){530,585,220,40});DrawUIText("E  前往灯塔庭院",550,596,17,(Color){91,115,96,255});
    } else if(g->area!=AREA_CAMP && Dist(g->player.pos,(Vector2){2,9})<1.8f) {
        Panel((Rectangle){530,585,220,40});DrawUIText("E  返回海岸营地",550,596,17,(Color){91,115,96,255});
    } else if(g->area==AREA_CAMP && !g->lumi.owned && Dist(g->player.pos,(Vector2){4,12})<1.65f) {
        Panel((Rectangle){530,585,220,40});
        DrawUIText("E  救助苔灯灵",550,596,17,(Color){91,115,96,255});
    } else if(g->area==AREA_CAMP && Dist(g->player.pos,(Vector2){3,10})<2.0f) {
        Panel((Rectangle){530,585,220,40});
        DrawUIText("E  与守望者交谈",550,596,17,(Color){91,115,96,255});
    } else if(g->area==AREA_CAMP && g->lumi.owned && Dist(g->player.pos,(Vector2){4.1f,12.5f})<1.5f) {
        Panel((Rectangle){530,585,220,40});
        DrawUIText("E  在营火旁休息",550,596,17,(Color){91,115,96,255});
    }
    if(g->toastTimer>0) {
        Panel((Rectangle){380,600,520,42});
        DrawUIText(g->toast,400,612,18,(Color){78,112,91,255});
    }
}
static void RenderJournal(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){250,105,780,510});
    DrawUIText("旅 人 手 记",300,135,34,(Color){74,105,83,255});
    DrawUIText("CHAPTER 01  /  THE LOST LIGHTHOUSE",303,179,15,(Color){132,145,112,255});
    DrawLine(300,207,980,207,(Color){220,211,183,255});
    DrawUIText("当前目标",303,230,21,(Color){104,135,94,255});
    DrawUIText(g->shrineSolved?"遗迹已被唤醒，前往庭院寻找守卫。":"寻找三位伙伴，收集潮汐贝壳并唤醒古代遗迹。",303,265,19,C_INK);
    DrawUIText("岛屿发现",303,320,21,(Color){104,135,94,255});
    DrawUIText(g->fireflyDiscovered?"萤芽  —  已记录：会在林间微光处出现。":"萤芽  —  未发现：留意林地里漂浮的微光。",303,355,18,C_INK);
    DrawUIText(g->leaflingFed?"叶团  —  已帮助：它为你找到了隐藏星晶。":"叶团  —  未帮助：它似乎很喜欢岛屿果实。",303,388,18,C_INK);
    DrawUIText(g->groveMoteDefeated?"微光兽 — 已击退：林间小径恢复安全。":"微光兽 — 尚未击退：它守在林地旧路旁。",303,416,18,C_INK);
    DrawUIText("伙伴同行",303,445,21,(Color){104,135,94,255});
    DrawUIText(TextFormat("苔灯灵  %s       沫尾兽  %s       鸣晶蜥  %s",g->lumi.owned?"同行中":"未发现",g->splash.owned?"同行中":"未发现",g->spark.owned?"同行中":"未发现"),303,480,18,C_INK);
    DrawUIText("资源",303,525,21,(Color){104,135,94,255});
    DrawUIText(TextFormat("果实 %d     潮汐贝壳 %d     星晶 %d",g->player.berries,g->player.shells,g->player.crystals),303,560,18,C_INK);
    DrawUIText("F5 保存   F9 读取   按 J 或 ESC 关闭",760,570,16,(Color){132,137,111,255});
}
static void RenderInventory(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){300,120,680,470});
    DrawUIText("探 险 背 包",350,150,32,(Color){74,105,83,255});
    DrawLine(350,200,930,200,(Color){220,211,183,255});
    DrawUIText(TextFormat("岛屿果实       x %d",g->player.berries),360,238,22,C_INK);
    DrawUIText(TextFormat("潮汐贝壳       x %d",g->player.shells),360,285,22,C_INK);
    DrawUIText(TextFormat("星晶           x %d",g->player.crystals),360,332,22,C_INK);
    DrawUIText(TextFormat("生命           %d / %d",(int)g->player.hp,(int)g->player.maxHp),360,379,22,C_INK);
    DrawUIText("伙伴成长",360,432,22,(Color){104,135,94,255});
    DrawUIText(TextFormat("苔灯灵 Lv.%d   沫尾兽 Lv.%d   鸣晶蜥 Lv.%d",g->lumi.level,g->splash.level,g->spark.level),360,470,19,C_INK);
    DrawUIText("按 I / ESC 关闭",760,550,16,(Color){132,137,111,255});
}
static void RenderMap(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){250,90,780,540});
    DrawUIText("岛 屿 地 图",300,120,32,(Color){74,105,83,255});
    const char *names[4]={"海岸营地","辉光林地","潮汐遗迹","灯塔庭院"};
    Vector2 pts[4]={{390,320},{560,235},{745,320},{900,235}};
    DrawLineEx(pts[0],pts[1],5,(Color){185,177,139,255});
    DrawLineEx(pts[0],pts[2],5,(Color){185,177,139,255});
    DrawLineEx(pts[0],pts[3],5,(Color){185,177,139,255});
    for(int i=0;i<4;i++) {
        bool unlocked=(i==0)||(i==1)||(i==2&&g->lumi.owned&&g->splash.owned)||(i==3&&g->shrineSolved);
        DrawCircleV(pts[i],g->area==i?36:28,g->area==i?C_GOLD:unlocked?(Color){144,190,132,255}:(Color){181,184,164,255});
        DrawCircleV(pts[i],13,(Color){255,240,196,255});
        DrawUIText(names[i],(int)pts[i].x-45,(int)pts[i].y+42,17,C_INK);
    }
    DrawUIText("主地图为枢纽；各区域通过入口往返。",320,520,17,(Color){105,121,97,255});
    DrawUIText("红点：当前位置   灰色区域：尚未解锁",320,545,18,(Color){105,121,97,255});
    DrawUIText("按 M / ESC 关闭",800,580,16,(Color){132,137,111,255});
}
static void RenderDialogue(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.26f));
    Panel((Rectangle){145,552,990,125});
    DrawCircle(195,552,37,(Color){157,201,143,255});
    DrawCircle(195,552,25,(Color){220,233,183,255});
    DrawCircle(187,548,2.8f,C_INK);DrawCircle(203,548,2.8f,C_INK);
    DrawUIText(g->dialogueSpeaker,250,566,17,(Color){125,149,112,255});
    DrawUIText(g->dialogue[g->dialoguePage],250,595,20,C_INK);
    DrawUIText("按 E / Enter / 鼠标左键继续",835,645,14,(Color){135,135,107,255});
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
    DrawUIText("伙伴协同战斗",55,40,30,WHITE);
    DrawUIText(TextFormat("回合 %d   %s",g->battleRound,g->enemySoaked?"敌人：潮湿（电击伤害提升）":"观察敌人状态"),55,82,18,(Color){224,232,215,255});
    Enemy *e=&g->enemies[g->battleEnemy];
    DrawUIText(TextFormat("守卫 HP  %d / %d",(int)e->hp,(int)e->maxHp),760,80,22,WHITE);
    DrawRectangle(760,112,340,17,(Color){70,75,85,255});
    DrawRectangle(760,112,(int)(340*fmaxf(0,e->hp/e->maxHp)),17,C_PINK);
    // Show the selected companion as a real sprite in the battle party panel.
    if(g->selectedBuddy==0 && g->lumi.owned)DrawLumi((Vector2){380,390},g->time,0);
    else if(g->selectedBuddy==1 && g->splash.owned)DrawMoatail((Vector2){380,390},g->time);
    else if(g->selectedBuddy==2 && g->spark.owned)DrawGecko((Vector2){380,390},g->time);
    else DrawPlayer((Vector2){380,390},g->time,false,0);
    if(e->type==ENEMY_MOTE)DrawMote((Vector2){870,300},g->time);
    else {
        Color enemyColor=e->type==ENEMY_BOSS?(Color){103,94,145,255}:(Color){166,143,163,255};
        DrawCircle(870,235,55,enemyColor);
        DrawTriangle((Vector2){830,206},(Vector2){855,150},(Vector2){879,206},(Color){204,181,205,255});
        DrawTriangle((Vector2){864,205},(Vector2){901,164},(Vector2){916,222},(Color){183,160,192,255});
        DrawCircle(852,232,6,C_INK);DrawCircle(888,232,6,C_INK);
        DrawCircle(852,230,2,WHITE);DrawCircle(888,230,2,WHITE);
        if(e->type==ENEMY_BOSS)DrawCircle(870,235,78,Fade(C_LILAC,.15f));
    }
    DrawUIText("选择伙伴技能",70,525,23,WHITE);
    const char *opts[3]={"1  苔灯灵：治疗 + 微光攻击","2  沫尾兽：潮湿水流","3  鸣晶蜥：晶弧放电（潮湿克制）"};
    for(int i=0;i<3;i++) {
        Rectangle r={70,565+i*42,440,34};
        DrawRectangleRounded(r,.2f,8,i==g->battleChoice?(Color){255,226,158,255}:(Color){238,236,218,235});
        DrawUIText(opts[i],88,573+i*42,18,(Color){64,82,76,255});
    }
    DrawUIText(g->battleTurn==0?"选择技能并按 Enter":"敌方行动：按 Enter 继续",760,570,21,WHITE);
    DrawUIText("技能联动：先喷水，再使用晶弧可造成额外伤害。",760,610,17,(Color){226,236,222,255});
}
static void RenderTitle(Game *g) {
    ClearBackground((Color){187,222,211,255});
    for(int i=0;i<10;i++)DrawTree((Vector2){90+i*130,410+(i%3)*20},1.0f+(i%2)*.2f,g->time);
    DrawRectangle(0,0,SW,SH,Fade((Color){239,242,221,255},.25f));
    DrawUIText("雾 屿 回 响",390,155,64,(Color){69,104,83,255});
    DrawUIText("第一章  ·  失落的灯塔",445,235,28,(Color){107,141,115,255});
    DrawUIText("一段关于伙伴、勇气与重新点亮世界的旅程",350,315,23,(Color){89,119,103,255});
    DrawRectangleRounded((Rectangle){500,425,280,62},.2f,16,(Color){255,249,224,245});
    DrawUIText("按 Enter 开始旅程",535,444,23,(Color){70,115,87,255});
    DrawUIText(FileExists("mist_isle_save.dat")?"F9 继续上次旅程":"F9 读取存档",565,500,18,(Color){94,127,101,255});
    DrawUIText("CARTOON PIXEL ADVENTURE",470,650,17,(Color){110,143,119,255});
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
    DrawUIText("灯塔重新亮起了",435,475,38,(Color){255,246,218,255});
    DrawUIText("伙伴们的光芒，照亮了整座岛屿。",405,535,23,(Color){218,231,211,255});
    DrawUIText("第一章 完成   ·   按 R 再次探索",430,620,20,(Color){255,222,154,255});
}
void RenderInit(void) {
    LoadUIFont();
    const char *paths[]={
        "assets/characters/hero/hero_walk_4dir_64.png",
        "assets/characters/companions/moss_lumi_idle_64.png",
        "assets/characters/companions/moatail_idle_64.png",
        "assets/characters/companions/crystal_gecko_idle_64.png",
        "assets/characters/companions/leafling_idle_64.png",
        "assets/characters/companions/grove_mote_idle_64.png",
        "assets/environment/environment_props_96.png",
        "assets/environment/area_props_96.png",
        "assets/characters/png/camp_keeper.png",
        "assets/tiles/png/island_iso_tiles.png",
        "assets/tiles/png/environment_props.png"
    };
    Texture2D *targets[]={&TEX_HERO_WALK,&TEX_LUMI_PIXEL,&TEX_MOATAIL_PIXEL,&TEX_GECKO_PIXEL,&TEX_LEAFLING,&TEX_GROVE_MOTE,&TEX_ENV_PROPS,&TEX_AREA_PROPS,&TEX_KEEPER,&TEX_ISO_TILES,&TEX_PROPS};
    for(int i=0;i<11;i++) {
        if(FileExists(paths[i])) {
            *targets[i]=LoadTexture(paths[i]);
            if(targets[i]->id)SetTextureFilter(*targets[i],TEXTURE_FILTER_POINT);
        }
    }
}
void RenderClose(void) {
    UnloadUIFont();
    Texture2D *textures[]={&TEX_HERO_WALK,&TEX_LUMI_PIXEL,&TEX_MOATAIL_PIXEL,&TEX_GECKO_PIXEL,&TEX_LEAFLING,&TEX_GROVE_MOTE,&TEX_ENV_PROPS,&TEX_AREA_PROPS,&TEX_KEEPER,&TEX_ISO_TILES,&TEX_PROPS};
    for(int i=0;i<11;i++)if(textures[i]->id)UnloadTexture(*textures[i]);
}
void GameRender(Game *g) {
    if(g->scene==SCENE_TITLE)RenderTitle(g);
    else if(g->scene==SCENE_BATTLE)RenderBattle(g);
    else if(g->scene==SCENE_END)RenderEnd(g);
    else {RenderWorld(g);RenderHUD(g);if(g->scene==SCENE_DIALOGUE)RenderDialogue(g);if(g->journalOpen)RenderJournal(g);if(g->inventoryOpen)RenderInventory(g);if(g->mapOpen)RenderMap(g);}
}