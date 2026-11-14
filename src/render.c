#include "game.h"
#include "area_maps.h"
#include "atlas_bounds.h"
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
static Font UI_FONT={0};
static const int UI_CODEPOINTS[]={32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,183,8211,8212,8230,8592,8594,12289,12290,19968,19977,19978,19981,19982,19990,19996,20004,20013,20026,20027,20046,20063,20102,20107,20110,20113,20114,20132,20142,20154,20173,20174,20195,20197,20204,20214,20219,20237,20241,20249,20250,20260,20276,20284,20301,20307,20316,20320,20351,20379,20381,20391,20415,20445,20572,20799,20808,20809,20811,20837,20840,20849,20851,20860,20861,20877,20889,20914,20915,20986,20987,20999,21040,21046,21050,21069,21147,21152,21153,21160,21161,21191,21215,21253,21306,21319,21327,21335,21345,21355,21360,21457,21462,21463,21464,21475,21476,21482,21487,21488,21494,21507,21508,21512,21516,21518,21543,21560,21561,21629,21644,21709,21796,21890,21916,21943,22218,22238,22242,22270,22280,22312,22320,22495,22522,22612,22771,22788,22797,22806,22810,22825,22830,22833,22836,22987,23384,23427,23432,23433,23436,23450,23453,23454,23475,23478,23481,23494,23547,23548,23553,23558,23567,23578,23601,23614,23618,23637,23679,23707,23736,24038,24050,24102,24110,24182,24212,24231,24237,24320,24341,24359,24403,24405,24418,24448,24452,24453,24456,24471,24494,24515,24577,24674,24687,24819,24840,24847,24895,25104,25110,25112,25163,25165,25171,25214,25216,25226,25260,25307,25320,25321,25345,25353,25361,25377,25442,25506,25509,25552,25805,25910,25915,25918,25925,25928,25932,25937,25955,25972,25991,26007,26032,26041,26045,26049,26053,26080,26085,26087,26102,26143,26159,26174,26222,26228,26230,26263,26356,26368,26377,26391,26395,26410,26412,26426,26469,26519,26522,26524,26530,26597,26631,26639,26723,27425,27426,27491,27573,27668,27700,27719,27728,27773,27785,27809,27819,27835,27839,27861,27873,27922,27963,27969,27973,28014,28023,28145,28165,28287,28304,28418,28526,28608,28779,28781,28783,28784,28789,28857,29031,29060,29255,29256,29289,29366,29616,29702,29983,29992,30005,30028,30041,30333,30340,30446,30475,30528,30561,30707,30784,30830,30865,31181,31192,31227,31243,31456,31526,31532,31561,32034,32418,32441,32445,32447,32454,32456,32463,32469,32473,32487,32493,32622,32769,32773,32852,32972,33016,33021,33394,33410,33469,33487,33492,33719,33828,33829,33853,33976,34067,34255,34276,34411,34597,34892,34987,35013,35199,35201,35265,35299,35748,35753,35760,35835,35848,36125,36133,36164,36208,36215,36234,36291,36319,36335,36454,36530,36716,36731,36745,36793,36807,36817,36820,36824,36825,36827,36828,36857,36864,36873,36890,36896,36935,36947,36951,36973,37117,37266,37319,37324,37325,37327,38145,38190,38271,38378,38381,38388,38395,38431,38451,38459,38477,38498,38500,38505,38544,38598,38632,38654,38656,38706,38752,38754,39063,39064,39069,39118,39128,39295,39321,39564,40483,40736,65281,65292,65306,65307,35282,28796,30171,21407,25105,30452,24517,29420,33258,26463,21435,20303,19979,22826,20037,20309,35302,30896,22842,26366,25252,21534,21517,23383,37027,39318,39046,26680,32784,38454,22686,24378,20302,33988,31283,26029,20108,34259,20339,22823,23613,24230,26816,32047,32791,36814,36884,38543};;;
static bool UIFontHasRequiredGlyphs(Font f) {
    for(int i=0;i<(int)(sizeof(UI_CODEPOINTS)/sizeof(UI_CODEPOINTS[0]));i++) {
        int cp=UI_CODEPOINTS[i];
        bool cjk=(cp>=0x3400&&cp<=0x9FFF)||(cp>=0xF900&&cp<=0xFAFF)||
                 (cp>=0x3000&&cp<=0x303F)||(cp>=0xFF00&&cp<=0xFFEF);
        if(cjk && GetGlyphIndex(f,cp)<=0) {
            TraceLog(LOG_WARNING,"Mist Isle: font missing required Chinese glyph U+%04X",cp);
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
static int Utf8CharLength(unsigned char c) {
    if(c<0x80)return 1;
    if((c&0xE0)==0xC0)return 2;
    if((c&0xF0)==0xE0)return 3;
    if((c&0xF8)==0xF0)return 4;
    return 1;
}
static void DrawUITextWrapped(const char *text,int x,int y,int maxWidth,int size,int maxLines,Color tint) {
    if(!text||!text[0]||maxLines<=0)return;
    if(!UI_FONT.texture.id){DrawText(text,x,y,size,tint);return;}
    char line[512];int used=0,lines=0;
    const unsigned char *p=(const unsigned char*)text;
    line[0]='\0';
    while(*p && lines<maxLines) {
        if(*p=='\n') {
            if(used>0){DrawUIText(line,x,y+lines*(size+4),size,tint);lines++;used=0;line[0]='\0';}
            p++;continue;
        }
        int n=Utf8CharLength(*p);
        if(used+n>=(int)sizeof(line))break;
        memcpy(line+used,p,(size_t)n);used+=n;line[used]='\0';
        if(MeasureTextEx(UI_FONT,line,(float)size,1.0f).x>(float)maxWidth && used>n) {
            used-=n;line[used]='\0';
            DrawUIText(line,x,y+lines*(size+4),size,tint);lines++;
            if(lines>=maxLines)break;
            memcpy(line,p,(size_t)n);used=n;line[used]='\0';
        }
        p+=n;
    }
    if(used>0 && lines<maxLines)DrawUIText(line,x,y+lines*(size+4),size,tint);
}

static Texture2D TEX_HERO={0}, TEX_LUMI={0}, TEX_MOATAIL={0}, TEX_GECKO={0}, TEX_KEEPER={0};
static Texture2D TEX_HERO_PORTRAIT={0}, TEX_WATCHER_PORTRAIT={0}, TEX_GUARDIAN_PORTRAIT={0};
static Texture2D TEX_HEROINE_APPROVED={0}, TEX_GUARDIAN_APPROVED={0};
static Texture2D TEX_HEROINE_PIXEL={0}, TEX_WATCHER_PIXEL={0}, TEX_COASTAL_HOUSE={0};
static Texture2D TEX_CHARACTER_PORTRAITS={0}, TEX_HEROINE_WORLD={0}, TEX_WATCHER_WORLD={0}, TEX_GROVE_COTTAGE={0};
static Texture2D TEX_LUMI_ILLUSTRATION={0}, TEX_MOATAIL_ILLUSTRATION={0}, TEX_GECKO_ILLUSTRATION={0};
static Texture2D TEX_HERO_PIXEL={0}, TEX_LUMI_PIXEL={0}, TEX_MOATAIL_PIXEL={0}, TEX_GECKO_PIXEL={0}, TEX_TILES={0}, TEX_PROPS={0};
static Texture2D TEX_ISO_TILES={0}, TEX_HERO_WALK={0}, TEX_ENV_PROPS={0}, TEX_LEAFLING={0}, TEX_GROVE_MOTE={0}, TEX_AREA_PROPS={0}, TEX_TOPDOWN_TILES={0}, TEX_FLORA_ATLAS={0}, TEX_CLIFF_ATLAS={0}, TEX_STRUCTURE_ATLAS={0}, TEX_ORIGINAL_PROPS={0}, TEX_KENNEY_BUILDINGS={0}, TEX_ENDING_CG={0};
static RenderTexture2D TEX_GROUND_CACHE={0};
static int CACHED_GROUND_AREA=-1;
static void DrawPortrait(Texture2D tex, Vector2 p, float w, float h, float bob) {
    if (tex.id == 0) return;
    Rectangle src={0,0,(float)tex.width,(float)tex.height};
    Rectangle dst={p.x-w*0.5f,p.y-h-bob,w,h};
    DrawTexturePro(tex,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawIllustratedPortrait(Texture2D tex,Rectangle dst) {
    if(!tex.id)return;
    Rectangle src={0,0,(float)tex.width,(float)tex.height};
    DrawTexturePro(tex,src,dst,(Vector2){0,0},0,WHITE);
}
static Vector2 WorldToScreen(float x,float y) { return (Vector2){(x+0.5f)*TILE_SIZE,(y+0.5f)*TILE_SIZE}; }
static unsigned int TileHash(int x,int y,int salt) {
    unsigned int n=(unsigned int)(x*374761393u+y*668265263u+salt*2246822519u);
    n=(n^(n>>13))*1274126177u; return n^(n>>16);
}
static int TerrainKindFromAtlasIndex(unsigned char index) {
    int row=(int)index/4;
    if(row>=9)return 4;
    if(row>=6)return 3;
    if(row>=3)return 2;
    return 0;
}
static void DrawTopDownTile(int x,int y,int tileIndex,int kind,float time) {
    Vector2 p=WorldToScreen((float)x,(float)y);
    Rectangle tile={p.x-TILE_SIZE*.5f,p.y-TILE_SIZE*.5f,TILE_SIZE,TILE_SIZE};
    unsigned int hash=TileHash(x,y,kind+7);
    Color tileBase=kind==4?(Color){47,137,171,255}:kind==3?(Color){126,139,129,255}:kind==2?(Color){198,166,111,255}:(Color){91,145,84,255};
    DrawRectangleRec(tile,tileBase);
    if(TEX_TOPDOWN_TILES.id) {
        Rectangle src={(float)((tileIndex%4)*56),(float)((tileIndex/4)*56),56.0f,56.0f};
        Rectangle dst={tile.x-1.0f,tile.y-1.0f,TILE_SIZE+2.0f,TILE_SIZE+2.0f};
        DrawTexturePro(TEX_TOPDOWN_TILES,src,dst,(Vector2){0,0},0,WHITE);
        return;
    }
    if(kind==4) {
        float wave=6.0f*sinf(time*1.7f+y*.65f+x*.35f);
        DrawLineEx((Vector2){tile.x+7,tile.y+14+wave*.15f},(Vector2){tile.x+TILE_SIZE-9,tile.y+14+wave*.15f},2.0f,Fade((Color){190,229,222,255},.32f));
    } else if(kind==2||kind==3) {
        for(int i=0;i<3;i++){float px=tile.x+7+(float)((hash>>(i*5))%43);float py=tile.y+8+(float)((hash>>(i*4+3))%40);DrawPixel((int)px,(int)py,kind==2?(Color){230,207,157,255}:(Color){93,105,95,255});}
    } else {
        for(int i=0;i<4;i++){float px=tile.x+5+(float)((hash>>(i*5))%47);float py=tile.y+5+(float)((hash>>(i*3+7))%47);DrawPixel((int)px,(int)py,Fade((Color){158,198,119,255},.55f));}
    }
}
static void DrawTerrainAccent(int x,int y,Area area,int kind,float time) {
    Vector2 p=WorldToScreen((float)x,(float)y);
    unsigned int h=TileHash(x+41,y-19,(int)area+17);
    if(kind==4) {
        Color deep=(Color){86,164,181,255};
        Color crest=(Color){211,239,230,255};
        if(area==AREA_GROVE) {
            deep=(Color){104,184,174,255};
            crest=(Color){222,244,209,255};
        } else if(area==AREA_RUINS) {
            deep=(Color){79,143,171,255};
            crest=(Color){194,225,224,255};
        } else if(area==AREA_LIGHTHOUSE) {
            deep=(Color){102,165,194,255};
            crest=(Color){235,239,221,255};
        }
        float phase=time*1.22f+(float)(h%113u)*.055f;
        float ox=(float)((h>>4)%25u)-12.0f;
        float drift=sinf(phase)*2.2f;
#if defined(MISTISLE_PI_PROFILE)
        if((h%3u)==0u) {
            float yy=p.y-9.0f+drift;
            DrawLineEx((Vector2){p.x+ox,yy},
                       (Vector2){p.x+ox+13.0f,yy-1.0f},1.25f,Fade(crest,.34f));
            DrawCircleV((Vector2){p.x+ox+17.0f,yy-1.0f},.9f,Fade(WHITE,.48f));
        }
#else
        // Two offset wave crests give each water tile a moving surface instead
        // of a flat blue fill. Their phase is stable per tile, so motion reads
        // as a continuous current rather than flickering random noise.
        for(int band=0;band<2;band++) {
            float bx=(float)((h>>(band*5+3))%23u)-11.0f;
            float by=p.y-17.0f+(float)band*19.0f+sinf(phase+band*1.1f)*2.8f;
            float length=12.0f+(float)((h>>(band*7+9))%15u);
            DrawLineEx((Vector2){p.x+bx,by},
                       (Vector2){p.x+bx+length*.48f,by-1.8f},1.55f,Fade(crest,.40f));
            DrawLineEx((Vector2){p.x+bx+length*.48f,by-1.8f},
                       (Vector2){p.x+bx+length,by-.3f},1.15f,Fade(deep,.34f));
        }
        if((h%3u)==0u) {
            float gx=p.x-19.0f+(float)((h>>8)%38u);
            float gy=p.y-21.0f+(float)((h>>13)%39u)+sinf(phase*1.35f)*2.0f;
            DrawCircleV((Vector2){gx,gy},1.35f,Fade(crest,.54f));
            DrawCircleV((Vector2){gx+2.0f,gy+1.0f},.7f,Fade(WHITE,.58f));
        }
        if((h%7u)==0u) {
            float rx=p.x-13.0f+(float)((h>>10)%26u);
            float ry=p.y-13.0f+(float)((h>>15)%26u);
            float pulse=.5f+.5f*sinf(phase*.72f);
            DrawCircleLines((int)rx,(int)ry,2.0f+2.0f*pulse,Fade(crest,.22f));
        }
#endif
        return;
    }
#if defined(MISTISLE_PI_PROFILE)
    // Static grass, sand and stone detail is baked into the ground cache.
    return;
#endif
    if(kind==2) {
        if((h%2u)==0u) {
            for(int i=0;i<3;i++) {
                int ox=5+(int)((h>>(i*4+3))%42u),oy=5+(int)((h>>(i*5+7))%42u);
                DrawCircle((int)p.x-28+ox,(int)p.y-28+oy,(i==0)?1.7f:1.1f,(Color){177,151,112,165});
                if(i==0)DrawCircle((int)p.x-28+ox+3,(int)p.y-28+oy-2,1.0f,(Color){238,219,177,190});
            }
        }
        if((h%11u)==0u) {
            float ox=(float)((h>>8)%28u)-14.0f,oy=(float)((h>>12)%24u)-12.0f;
            DrawCircleV((Vector2){p.x+ox,p.y+oy},2.1f,(Color){228,210,167,220});
            DrawCircleV((Vector2){p.x+ox-1,p.y+oy-1},.8f,WHITE);
        }
        return;
    }
    if(kind==3) {
        if((h%3u)==0u) {
            float ox=(float)((h>>5)%30u)-15.0f,oy=(float)((h>>10)%26u)-13.0f;
            DrawLineEx((Vector2){p.x+ox-5,p.y+oy-2},(Vector2){p.x+ox+4,p.y+oy+2},1.1f,Fade((Color){72,88,82,255},.38f));
            if((h%2u)==0u)DrawLineEx((Vector2){p.x+ox+1,p.y+oy+1},(Vector2){p.x+ox+4,p.y+oy+6},.9f,Fade((Color){72,88,82,255},.32f));
        }
        if((h%4u)==0u) {
            float ox=(float)((h>>7)%34u)-17.0f,oy=(float)((h>>12)%28u)-14.0f;
            Color moss=(Color){113,151,102,255};
            DrawCircleV((Vector2){p.x+ox,p.y+oy},2.0f,Fade(moss,.55f));
            DrawCircleV((Vector2){p.x+ox+3,p.y+oy+1},1.2f,Fade(moss,.45f));
        }
        return;
    }
    if((h%2u)==0u) {
        Color blade=area==AREA_RUINS?(Color){139,153,119,190}:(Color){87,145,83,205};
        for(int i=0;i<3;i++) {
            float ox=(float)((h>>(i*4+3))%38u)-19.0f;
            float oy=(float)((h>>(i*5+8))%32u)-16.0f;
            DrawLineEx((Vector2){p.x+ox,p.y+oy+4},(Vector2){p.x+ox-2.5f,p.y+oy-3},1.35f,blade);
            DrawLineEx((Vector2){p.x+ox+3,p.y+oy+4},(Vector2){p.x+ox+4,p.y+oy-2},1.1f,Fade(blade,.9f));
        }
    }
    if((h%7u)==0u) {
        Color petal=area==AREA_GROVE?(Color){245,220,154,255}:area==AREA_LIGHTHOUSE?(Color){223,190,225,255}:(Color){242,192,197,255};
        float ox=(float)((h>>6)%28u)-14.0f,oy=(float)((h>>11)%24u)-12.0f;
        DrawCircleV((Vector2){p.x+ox,p.y+oy},2.0f,petal);
        DrawCircleV((Vector2){p.x+ox+3,p.y+oy-2},1.2f,Fade(WHITE,.8f));
    }
    if((h%13u)==0u) {
        float ox=(float)((h>>8)%32u)-16.0f,oy=(float)((h>>13)%28u)-14.0f;
        DrawCircleV((Vector2){p.x+ox,p.y+oy},1.8f,(Color){160,146,118,210});
    }
}
static void DrawAtlasSprite(Texture2D tex,int index,int columns,int cell,Vector2 p,float w,float h,const MistAtlasBounds *bounds) {
    if(!tex.id)return;
    Rectangle src={(float)((index%columns)*cell),(float)((index/columns)*cell),(float)cell,(float)cell};
    Rectangle dst={p.x-w*.5f,p.y-h,w,h};
    // Trim transparent padding from the source cell and anchor the visible pixels
    // to the actual ground-contact point. This prevents floating props and cut-off feet.
    if(bounds && bounds->w>0 && bounds->h>0) {
        src.x+=(float)bounds->x; src.y+=(float)bounds->y;
        src.width=(float)bounds->w; src.height=(float)bounds->h;
        float dw=w*(float)bounds->w/(float)cell;
        float dh=h*(float)bounds->h/(float)cell;
        dst=(Rectangle){p.x-dw*.5f,p.y-dh,dw,dh};
    }
    DrawTexturePro(tex,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawFloraProp(int index,Vector2 p,float w,float h) { if(index>=0&&index<48)DrawAtlasSprite(TEX_FLORA_ATLAS,index,8,128,p,w,h,&FLORA_ATLAS_BOUNDS[index]); }
static void DrawCliffProp(int index,Vector2 p,float w,float h) { if(index>=0&&index<32)DrawAtlasSprite(TEX_CLIFF_ATLAS,index,8,128,p,w,h,&CLIFF_ATLAS_BOUNDS[index]); }
static void DrawStructureProp(int index,Vector2 p,float w,float h) { if(index>=0&&index<16)DrawAtlasSprite(TEX_STRUCTURE_ATLAS,index,8,128,p,w,h,&STRUCTURE_ATLAS_BOUNDS[index]); }
static void DrawOriginalProp(int index,Vector2 p,float w,float h) { if(index>=0&&index<16)DrawAtlasSprite(TEX_ORIGINAL_PROPS,index,4,32,p,w,h,&ORIGINAL_PROPS_BOUNDS[index]); }
static void DrawKenneyBuilding(int index,Vector2 p,float scale) {
    if(!TEX_KENNEY_BUILDINGS.id)return;
    Rectangle src,dst;
    if(index==0||index==1) {
        src=(Rectangle){(float)(index*192+24),0,144,192};
        dst=(Rectangle){p.x-72*scale,p.y-192*scale,144*scale,192*scale};
    } else if(index==2) {
        src=(Rectangle){0,240,192,144};
        dst=(Rectangle){p.x-96*scale,p.y-144*scale,192*scale,144*scale};
    } else {
        src=(Rectangle){204,336,168,48};
        dst=(Rectangle){p.x-84*scale,p.y-48*scale,168*scale,48*scale};
    }
    DrawTexturePro(TEX_KENNEY_BUILDINGS,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawAreaPropTint(int index,Vector2 p,float w,float h,Color tint) {
    if(!TEX_AREA_PROPS.id||index<0||index>=16)return;
    const MistAtlasBounds *b=&AREA_PROPS_BOUNDS[index];
    Rectangle src={(float)(index*96+b->x),(float)b->y,(float)b->w,(float)b->h};
    float dw=w*(float)b->w/96.0f,dh=h*(float)b->h/96.0f;
    Rectangle dst={p.x-dw*.5f,p.y-dh,dw,dh};
    DrawTexturePro(TEX_AREA_PROPS,src,dst,(Vector2){0,0},0,tint);
}
static void DrawAreaProp(int index,Vector2 p,float w,float h) {
    DrawAreaPropTint(index,p,w,h,WHITE);
}
static void DrawEnvProp(int index,Vector2 p,float w,float h) {
    if(!TEX_ENV_PROPS.id||index<0||index>=12)return;
    const MistAtlasBounds *b=&ENV_PROPS_BOUNDS[index];
    Rectangle src={(float)(index*96+b->x),(float)b->y,(float)b->w,(float)b->h};
    float dw=w*(float)b->w/96.0f,dh=h*(float)b->h/96.0f;
    Rectangle dst={p.x-dw*.5f,p.y-dh,dw,dh};
    DrawTexturePro(TEX_ENV_PROPS,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawCoastalHouse(Vector2 p,float scale) {
    if(!TEX_COASTAL_HOUSE.id)return;
    const float footY=p.y+7.0f;
    const float w=252.0f*scale,h=204.0f*scale;
    /* Buildings use authored foundations and contact edges, not detached oval shadows. */
    Rectangle src={0,0,(float)TEX_COASTAL_HOUSE.width,(float)TEX_COASTAL_HOUSE.height};
    Rectangle dst={p.x-w*.5f,footY-h,w,h};
    DrawTexturePro(TEX_COASTAL_HOUSE,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawGroveCottage(Vector2 p,float scale) {
    if(!TEX_GROVE_COTTAGE.id)return;
    Rectangle src={0,0,(float)TEX_GROVE_COTTAGE.width,(float)TEX_GROVE_COTTAGE.height};
    float w=176.0f*scale,h=144.0f*scale;
    Rectangle dst={p.x-w*.5f,p.y-h,w,h};
    DrawTexturePro(TEX_GROVE_COTTAGE,src,dst,(Vector2){0,0},0,WHITE);
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
    if(!TEX_PROPS.id||index<0||index>=12)return;
    const MistAtlasBounds *b=&PIXEL_PROPS_BOUNDS[index];
    Rectangle src={(float)(index*64+b->x),(float)b->y,(float)b->w,(float)b->h};
    float dw=w*(float)b->w/64.0f,dh=h*(float)b->h/64.0f;
    Rectangle dst={p.x-dw*.5f,p.y-dh,dw,dh};
    DrawTexturePro(TEX_PROPS,src,dst,(Vector2){0,0},0,WHITE);
}
static void DrawKeeper(Vector2 p,float t,float scale) {
    const float footY=p.y+7.0f*scale;
    DrawEllipse((int)p.x,(int)footY,(int)(10*scale),(int)(3*scale),(Color){35,49,45,75});
    // Four-frame native pixel NPC sheet, never a pixelated portrait crop.
    if(TEX_WATCHER_PIXEL.id) {
        int frame=(int)(t*2.2f)%4;
        Rectangle src={(float)(frame*64),0,64,80};
        float w=42.0f*scale,h=68.0f*scale;
        Rectangle dst={roundf(p.x-w*.5f),roundf(footY-h),w,h};
        DrawTexturePro(TEX_WATCHER_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else {
        TraceLog(LOG_ERROR,"Mist Isle: native pixel Watcher actor missing; no portrait or low-resolution fallback will be displayed.");
    }
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
    if(TEX_GROVE_MOTE.id){int frame=(int)(t*2.5f)%4;Rectangle src={(float)(frame*32),0,32,32};Rectangle dst={p.x-25,footY-50-bob,50,50};DrawTexturePro(TEX_GROVE_MOTE,src,dst,(Vector2){0,0},0,WHITE);return;}
    // Render only the authored Grove Mote sprite sheet; no procedural character silhouette.
}
static void DrawLeafling(Vector2 p,float t) {
    float bob=sinf(t*4.0f)*2.5f;
    const float footY=p.y+7.0f;
    DrawEllipse((int)p.x,(int)footY,11,4,(Color){48,66,57,95});
    if(TEX_LEAFLING.id){int frame=(int)(t*2.5f)%4;Rectangle src={(float)(frame*32),0,32,32};Rectangle dst={p.x-25,footY-50-bob,50,50};DrawTexturePro(TEX_LEAFLING,src,dst,(Vector2){0,0},0,WHITE);return;}
    // Render only the authored Leafling sprite sheet; no procedural character silhouette.
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
    (void)bob;
    const float footY=p.y+8.0f;
    float floatY=sinf(t*4.4f)*2.0f;
    DrawEllipse((int)p.x,(int)footY,12,4,(Color){48,66,57,82});
    if(TEX_LUMI_PIXEL.id) {
        int frame=(int)(t*5.2f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-22.0f,footY-48.0f-floatY,44.0f,48.0f};
        DrawTexturePro(TEX_LUMI_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_LUMI_ILLUSTRATION.id) {
        Rectangle src={0,0,(float)TEX_LUMI_ILLUSTRATION.width,(float)TEX_LUMI_ILLUSTRATION.height};
        Rectangle dst={p.x-22.0f,footY-48.0f-floatY,44.0f,48.0f};
        DrawTexturePro(TEX_LUMI_ILLUSTRATION,src,dst,(Vector2){0,0},0,WHITE);
    }
}
static void DrawPlayer(Vector2 p,float t,bool moving,int facing) {
    const float footY=p.y+9.0f;
    float bob=moving?fabsf(sinf(t*13.0f))*1.0f:0.0f;
    DrawEllipse((int)p.x,(int)footY,18,5,(Color){42,48,52,112});
    if(facing<0||facing>3)facing=0;
    if(TEX_HEROINE_PIXEL.id) {
        int frame=moving?((int)(t*8.0f)%4):0;
        Rectangle src={(float)(frame*64),(float)(facing*80),64,80};
        Rectangle dst={roundf(p.x-19.0f),roundf(footY-60.0f-bob),38.0f,60.0f};
        DrawTexturePro(TEX_HEROINE_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_HEROINE_APPROVED.id) {
        int frame=moving?((int)(t*10.0f)%8):0;
        Rectangle src={(float)(frame*24),(float)(facing*32),24,32};
        Rectangle dst={roundf(p.x-19.0f),roundf(footY-52.0f-bob),38.0f,52.0f};
        DrawTexturePro(TEX_HEROINE_APPROVED,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_HERO_WALK.id) {
        int frame=moving?((int)(t*8.0f)%4):0;
        Rectangle src={(float)(frame*32),(float)(facing*32),32,32};
        Rectangle dst={roundf(p.x-19.0f),roundf(footY-40.0f-bob),38.0f,40.0f};
        DrawTexturePro(TEX_HERO_WALK,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_HERO_PORTRAIT.id) {
        Rectangle src={0,0,(float)TEX_HERO_PORTRAIT.width,(float)TEX_HERO_PORTRAIT.height};
        Rectangle dst={p.x-24.0f,footY-64.0f-bob,48.0f,64.0f};
        DrawTexturePro(TEX_HERO_PORTRAIT,src,dst,(Vector2){0,0},0,WHITE);
    }
}
static void DrawShrine(Vector2 p,float t,bool solved,bool finalCore) {
    Color c=solved?C_GOLD:(Color){158,185,166,255};
    if(finalCore) {
        // The Tidal Ruins keystone is the chapter's central mechanism, not another small shrine.
        DrawEllipse((int)p.x,(int)p.y+13,104,25,(Color){35,57,61,145});
        DrawCircleLines((int)p.x,(int)p.y+7,91,Fade((Color){93,169,174,255},.38f));
        DrawCircleLines((int)p.x,(int)p.y+7,74,Fade((Color){218,192,128,255},.55f));
        DrawCircleLines((int)p.x,(int)p.y+7,58,Fade((Color){93,169,174,255},.32f));
        DrawAreaProp(11,p,190,194);
        // Four inset rune stones anchor the mechanism to the floor.
        for(int i=0;i<4;i++) {
            float a=(float)i*PI2/4.0f+.785f;
            Vector2 q={p.x+cosf(a)*79.0f,p.y+7.0f+sinf(a)*25.0f};
            DrawAreaProp(9,(Vector2){q.x,q.y+4},42,48);
            DrawCircleV((Vector2){q.x,q.y-13},3.5f,Fade(C_BLUE,.65f+.25f*sinf(t*2+i)));
        }
        float pulse=.5f+.5f*sinf(t*2.4f);
        DrawCircleV((Vector2){p.x,p.y-92-sinf(t*2)*4},30,Fade(C_BLUE,.10f+.10f*pulse));
        DrawCircleV((Vector2){p.x,p.y-92-sinf(t*2)*4},13,Fade((Color){117,225,235,255},.85f));
        DrawCircleV((Vector2){p.x,p.y-92-sinf(t*2)*4},5,WHITE);
        for(int i=0;i<8;i++) {
            float a=t*.42f+(float)i*PI2/8.0f;
            Vector2 q={p.x+cosf(a)*54.0f,p.y-38+sinf(a)*20.0f};
            DrawCircleV(q,3.0f,Fade(i%2?C_GOLD:C_BLUE,.55f+.35f*pulse));
        }
        if(solved) {
            DrawCircleV((Vector2){p.x,p.y-76},66,Fade(C_GOLD,.12f));
            for(int i=0;i<6;i++) {
                float a=t*.7f+(float)i*PI2/6.0f;
                DrawCircleV((Vector2){p.x+cosf(a)*88,p.y-40+sinf(a)*30},3.2f,Fade(C_GOLD,.85f));
            }
        }
        return;
    }
    DrawEllipse((int)p.x,(int)p.y+8,34,10,(Color){66,95,85,95});
    if(TEX_AREA_PROPS.id) DrawAreaProp(11,p,104,108);
    else if(TEX_ENV_PROPS.id) DrawEnvProp(4,p,82,82);
    else {
        DrawRectangle((int)p.x-14,(int)p.y-25,28,31,(Color){156,174,149,255});
        DrawRectangle((int)p.x-8,(int)p.y-45,16,22,(Color){198,209,171,255});
    }
    DrawCircleV((Vector2){p.x,p.y-52-sinf(t*2)*2},solved?9:6,c);
    DrawCircleV((Vector2){p.x,p.y-52-sinf(t*2)*2},2.5f,WHITE);
    if(solved) {
        DrawCircleV((Vector2){p.x,p.y-52},30,Fade(c,.12f));
        for(int i=0;i<4;i++){float a=t*.7f+i*PI2/4;DrawCircleV((Vector2){p.x+cosf(a)*28,p.y-40+sinf(a)*18},2.0f,Fade(C_GOLD,.8f));}
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
    RENDER_KEEPER, RENDER_LUMI, RENDER_MOATAIL, RENDER_GECKO, RENDER_FIREFLY, RENDER_LEAFLING, RENDER_MOTE, RENDER_BOSS, RENDER_GATE, RENDER_PLAYER
} RenderKind;
typedef struct { float depth; RenderKind kind; int index; Vector2 grid; float scale; } RenderItem;
static void PushRenderItem(RenderItem *items,int *count,RenderKind kind,int index,Vector2 grid,float scale) {
    if(*count>=512)return;
    items[*count]=(RenderItem){grid.y+((kind==RENDER_PLAYER||kind==RENDER_TREE||kind==RENDER_PROP||kind==RENDER_BOSS||kind==RENDER_GATE)?0.16f:0.0f),kind,index,grid,scale};
    (*count)++;
}
static void DrawMoatail(Vector2 p,float t) {
    const float footY=p.y+8.0f;
    float bob=sinf(t*3.8f+1.2f)*2.2f;
    DrawEllipse((int)p.x,(int)footY,12,4,(Color){48,66,57,82});
    if(TEX_MOATAIL_PIXEL.id) {
        int frame=(int)(t*4.4f+1.0f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-22.0f,footY-48.0f-bob,44.0f,48.0f};
        DrawTexturePro(TEX_MOATAIL_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_MOATAIL_ILLUSTRATION.id) {
        Rectangle src={0,0,(float)TEX_MOATAIL_ILLUSTRATION.width,(float)TEX_MOATAIL_ILLUSTRATION.height};
        Rectangle dst={p.x-22.0f,footY-48.0f-bob,44.0f,48.0f};
        DrawTexturePro(TEX_MOATAIL_ILLUSTRATION,src,dst,(Vector2){0,0},0,WHITE);
    }
}
static void DrawGecko(Vector2 p,float t) {
    const float footY=p.y+8.0f;
    float bob=sinf(t*4.8f+2.1f)*1.8f;
    DrawEllipse((int)p.x,(int)footY,18,6,(Color){48,66,57,82});
    if(TEX_GECKO_PIXEL.id) {
        int frame=(int)(t*5.0f+2.0f)%4;
        Rectangle src={(float)(frame*32),0,32,32};
        Rectangle dst={p.x-22.0f,footY-48.0f-bob,44.0f,48.0f};
        DrawTexturePro(TEX_GECKO_PIXEL,src,dst,(Vector2){0,0},0,WHITE);
    } else if(TEX_GECKO_ILLUSTRATION.id) {
        Rectangle src={0,0,(float)TEX_GECKO_ILLUSTRATION.width,(float)TEX_GECKO_ILLUSTRATION.height};
        Rectangle dst={p.x-22.0f,footY-48.0f-bob,44.0f,48.0f};
        DrawTexturePro(TEX_GECKO_ILLUSTRATION,src,dst,(Vector2){0,0},0,WHITE);
    }
}
static void DrawLighthouseGuardian(Vector2 p,float t,float scale,bool attacking) {
    float bob=sinf(t*1.35f)*1.6f*scale;
    float pulse=.5f+.5f*sinf(t*2.2f);
    DrawEllipse((int)p.x,(int)(p.y+7*scale),(int)(58*scale),(int)(15*scale),(Color){13,20,27,178});
    // Heavy shadow and concentric lantern sigils establish a boss-scale silhouette.
    DrawCircleLines((int)p.x,(int)(p.y-72*scale),(int)(54*scale),Fade((Color){255,190,91,255},.20f+.12f*pulse));
    DrawCircleLines((int)p.x,(int)(p.y-72*scale),(int)(67*scale),Fade((Color){113,204,211,255},.12f));
    if(TEX_GUARDIAN_APPROVED.id) {
        int frame=attacking?((int)(t*8.0f)%4):((int)(t*2.0f)%4);
        Rectangle src={(float)(frame*64),attacking?80.0f:0.0f,64,80};
        float w=112.0f*scale,h=140.0f*scale;
        Rectangle dst={p.x-w*.5f,p.y-h+bob,w,h};
        DrawTexturePro(TEX_GUARDIAN_APPROVED,src,dst,(Vector2){0,0},0,WHITE);
    } else {
        TraceLog(LOG_ERROR,"Mist Isle: pixel guardian sheet is missing; refusing to display the anime portrait as a world actor.");
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
    // The forest silhouette is authored in Tiled with hand-painted tree sprites.
    // Keep the underlay quiet so the actual canopy, flowers and props define the scene.
    DrawRectangle(0,0,WORLD_PIXEL_W,WORLD_PIXEL_H,(Color){49,91,66,255});
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
static void DrawLighthouseBackdrop(float t,bool beaconLit) {
    DrawRectangleGradientV(0,0,WORLD_PIXEL_W,WORLD_PIXEL_H,(Color){48,78,111,255},(Color){24,57,81,255});
    // A warm dusk horizon and distant headlands give the courtyard a clear sense of place.
    DrawCircle(260,205,116,Fade((Color){255,202,142,255},.23f));
    DrawCircle(260,205,72,Fade((Color){255,225,176,255},.22f));
    DrawTriangle((Vector2){0,360},(Vector2){175,248},(Vector2){380,360},(Color){46,80,87,255});
    DrawTriangle((Vector2){780,350},(Vector2){1030,235},(Vector2){WORLD_PIXEL_W,360},(Color){40,72,83,255});
    // Distant sea bands.
    for(int i=0;i<16;i++) {
        float y=350.0f+i*42.0f;
        for(int x=0;x<WORLD_PIXEL_W;x+=146) {
            float yy=y+sinf(t*.22f+i*.65f+x*.009f)*3.5f;
            DrawLineEx((Vector2){(float)x,yy},(Vector2){x+55.0f,yy-1.5f},1.8f,Fade((Color){194,224,220,255},.22f));
        }
    }
    DrawCloudPuff(220,130,.78f,(Color){237,241,216,115});
    DrawCloudPuff(760,190,.62f,(Color){237,241,216,85});
    if(beaconLit) {
        Vector2 lamp={1008,179};
        float sweep=sinf(t*.72f)*.5f;
        Vector2 a={-220.0f,lamp.y-220.0f+sweep*320.0f};
        Vector2 b={-220.0f,lamp.y+220.0f+sweep*320.0f};
        DrawTriangle(lamp,a,b,Fade((Color){255,226,164,255},.10f));
        DrawTriangle(lamp,(Vector2){-220.0f,lamp.y-92.0f+sweep*320.0f},
            (Vector2){-220.0f,lamp.y+92.0f+sweep*320.0f},Fade((Color){255,243,198,255},.12f));
        DrawCircleV(lamp,43,Fade((Color){255,226,155,255},.12f+.08f*sinf(t*3.0f)));
    } else {
        DrawCircleV((Vector2){1008,179},24,Fade((Color){100,133,158,255},.12f));
    }
}
static void DrawWorldBackdrop(Game *g) {
    Color surround=g->area==AREA_CAMP?(Color){83,163,178,255}:
        g->area==AREA_GROVE?(Color){66,112,78,255}:
        g->area==AREA_RUINS?(Color){84,101,101,255}:(Color){76,126,145,255};
    DrawRectangle(-SW,-SH,WORLD_PIXEL_W+2*SW,WORLD_PIXEL_H+2*SH,surround);
    // Decorative exterior texture remains visible when the camera looks beyond the island edge.
    for(int y=-SH;y<WORLD_PIXEL_H+SH;y+=54)for(int x=-SW;x<WORLD_PIXEL_W+SW;x+=112) {
        unsigned int h=TileHash(x/112,y/54,(int)g->area);
        if(g->area==AREA_CAMP || g->area==AREA_LIGHTHOUSE) {
            float yy=(float)y+fmodf((float)(h%31),31.0f);
            DrawLineEx((Vector2){(float)x,yy},(Vector2){(float)x+42+(h%26),yy-2},1.5f,Fade((Color){221,241,226,255},.22f));
        } else if(g->area==AREA_GROVE) {
            DrawCircle((int)(x+(h%35)),(int)(y+(h%29)),2+(h%3),Fade((Color){205,222,145,255},.24f));
        } else {
            DrawLine((int)x,(int)y,(int)x+12+(h%17),(int)y+4,Fade((Color){205,210,184,255},.18f));
            DrawCircle((int)(x+38),(int)(y+22),1+(h%2),Fade((Color){219,219,189,255},.18f));
        }
    }
    switch(g->area) {
        case AREA_CAMP: DrawSeaBackdrop(g->time); break;
        case AREA_GROVE: DrawForestBackdrop(g->time); break;
        case AREA_RUINS: DrawRuinsBackdrop(g->time); break;
        default: DrawLighthouseBackdrop(g->time,false); break;
    }
}

static void DrawObjectiveMarker(Vector2 grid,float time) {
    Vector2 p=WorldToScreen(grid.x,grid.y);
    float pulse=0.5f+0.5f*sinf(time*3.0f);
    DrawCircleV((Vector2){p.x,p.y+2},18.0f+5.0f*pulse,Fade(C_GOLD,.12f));
    DrawCircleLines((int)p.x,(int)p.y+2,(int)(15.0f+4.0f*pulse),Fade(C_GOLD,.9f));
    DrawLineEx((Vector2){p.x,p.y-58.0f-5.0f*pulse},(Vector2){p.x,p.y-19.0f},3.0f,Fade(C_GOLD,.72f));
    DrawTriangle((Vector2){p.x,p.y-66.0f-5.0f*pulse},(Vector2){p.x-8,p.y-51.0f-5.0f*pulse},(Vector2){p.x+8,p.y-51.0f-5.0f*pulse},Fade(C_GOLD,.95f));
  }
static void DrawGateMarker(Game *g,int gateId,Vector2 grid) {
    const char *name="返回海岸营地";
    const char *status="E  返回营地";
    Color accent=(Color){226,195,126,255};
    bool locked=false;
    if(gateId==0) {
        name="辉光林地";status="E  进入林地";accent=(Color){153,213,128,255};
    } else if(gateId==1) {
        name="潮汐遗迹";locked=!(g->lumi.owned&&g->splash.owned);
        status=locked?"需要两位伙伴":"E  进入遗迹";accent=(Color){117,197,220,255};
    } else if(gateId==2) {
        name="灯塔庭院";locked=!g->shrineSolved;
        status=locked?"封印尚未解除":"E  进入庭院";accent=(Color){245,198,112,255};
    }
    bool nearby=Dist(g->player.pos,grid)<2.35f;
    Vector2 p=WorldToScreen(grid.x,grid.y);
    float pulse=.5f+.5f*sinf(g->time*2.8f+gateId);
    // A grounded masonry plinth connects each portal to the path and removes the floating-object look.
    // Irregular embedded stone threshold: no floating rectangular slab.
    DrawEllipse((int)p.x,(int)p.y+12,66,17,(Color){24,43,42,135});
    DrawEllipse((int)p.x,(int)p.y+7,57,12,(Color){91,100,82,255});
    DrawEllipseLines((int)p.x,(int)p.y+7,57,12,(Color){169,158,119,230});
    for(int i=0;i<7;i++) {
        float a=(float)i*PI2/7.0f+.18f;
        Vector2 shard={p.x+cosf(a)*47.0f,p.y+7.0f+sinf(a)*10.0f};
        DrawCircleV(shard,2.2f,(Color){183,169,129,210});
    }
    if(gateId==0) {
        // Grove entrance: living stone arch, flowered feet and a soft green threshold.
        DrawAreaProp(8,(Vector2){p.x,p.y+2},160,168);
        DrawFloraProp(12,(Vector2){p.x-49,p.y+9},45,46);
        DrawFloraProp(13,(Vector2){p.x+48,p.y+10},44,45);
        DrawCircleV((Vector2){p.x,p.y-63},48,Fade((Color){150,220,132,255},.08f+.07f*pulse));
        DrawCircleLines((int)p.x,(int)p.y-63,37,Fade((Color){169,231,147,255},.55f));
    } else if(gateId==1) {
        // Ruins entrance: heavy carved portal with a clearly readable blue rune.
        DrawStructureProp(4,(Vector2){p.x,p.y+2},160,178);
        DrawCircleLines((int)p.x,(int)p.y-65,43,Fade((Color){104,194,214,255},.72f));
        for(int i=0;i<4;i++) {
            float a=(float)i*PI2/4.0f+g->time*.12f;
            DrawCircleV((Vector2){p.x+cosf(a)*43,p.y-65+sinf(a)*43},3.2f,Fade(C_BLUE,.8f));
        }
    } else if(gateId==2) {
        // Lighthouse route: the entrance is a beacon tower, not a generic arch.
        DrawAreaProp(13,(Vector2){p.x,p.y+1},142,178);
        DrawCircleV((Vector2){p.x,p.y-119},31,Fade(C_GOLD,.10f+.08f*pulse));
        DrawCircleV((Vector2){p.x,p.y-119},8,Fade((Color){255,229,157,255},.85f));
        DrawCircleLines((int)p.x,(int)p.y-119,23,Fade(C_GOLD,.75f));
    } else {
        // Return route keeps the same grounded stone-arch language as the entry portal.
        DrawAreaProp(8,(Vector2){p.x,p.y+2},160,168);
        DrawCircleLines((int)p.x,(int)p.y-63,37,Fade((Color){119,205,228,255},.62f));
        DrawCircleV((Vector2){p.x,p.y-63},8,Fade((Color){123,218,238,255},.72f));
        DrawCircleV((Vector2){p.x,p.y-63},3,WHITE);
        for(int i=0;i<4;i++) {
            float a=(float)i*PI2/4.0f+g->time*.18f;
            DrawCircleV((Vector2){p.x+cosf(a)*37,p.y-63+sinf(a)*37},2.6f,Fade(C_BLUE,.72f));
        }
    }
    if(locked)DrawCircleV((Vector2){p.x,p.y-69},5,(Color){138,144,137,255});
    else DrawCircleV((Vector2){p.x,p.y-69},5,Fade(accent,.75f+.25f*pulse));
    Rectangle sign={p.x-84.0f,p.y-188.0f,168.0f,42.0f};
    DrawRectangleRounded(sign,.16f,6,(Color){8,27,43,242});
    DrawRectangleRoundedLinesEx(sign,.16f,6,1.6f,Fade(accent,locked?.42f:.88f));
    float nameWidth=UI_FONT.texture.id?MeasureTextEx(UI_FONT,name,15.0f,1.0f).x:(float)MeasureText(name,15);
    DrawUIText(name,(int)(p.x-nameWidth*.5f),(int)sign.y+4,15,(Color){248,239,211,255});
    float statusWidth=UI_FONT.texture.id?MeasureTextEx(UI_FONT,status,11.0f,1.0f).x:(float)MeasureText(status,11);
    DrawUIText(status,(int)(p.x-statusWidth*.5f),(int)sign.y+23,11,locked?(Color){201,190,161,255}:(nearby?(Color){255,226,154,255}:(Color){177,202,194,255}));
    if(nearby&&!locked) {
        DrawCircleV((Vector2){p.x,p.y-201.0f-sinf(g->time*4.0f)*3.0f},4.0f,C_GOLD);
        DrawTriangle((Vector2){p.x,p.y-195.0f-sinf(g->time*4.0f)*3.0f},
                     (Vector2){p.x-6,p.y-206.0f-sinf(g->time*4.0f)*3.0f},
                     (Vector2){p.x+6,p.y-206.0f-sinf(g->time*4.0f)*3.0f},C_GOLD);
    }
}
static void GetVisibleTileBounds(Game *g,int *minX,int *minY,int *maxX,int *maxY) {
    Vector2 a=GetScreenToWorld2D((Vector2){0,0},g->cam);
    Vector2 b=GetScreenToWorld2D((Vector2){SW,SH},g->cam);
    float left=fminf(a.x,b.x),right=fmaxf(a.x,b.x);
    float top=fminf(a.y,b.y),bottom=fmaxf(a.y,b.y);
    *minX=(int)Clamp((float)floorf(left/TILE_SIZE)-1.0f,0.0f,(float)(MAPW-1));
    *maxX=(int)Clamp((float)floorf(right/TILE_SIZE)+1.0f,0.0f,(float)(MAPW-1));
    *minY=(int)Clamp((float)floorf(top/TILE_SIZE)-1.0f,0.0f,(float)(MAPH-1));
    *maxY=(int)Clamp((float)floorf(bottom/TILE_SIZE)+1.0f,0.0f,(float)(MAPH-1));
}
static void DrawCoastalCliffs(Game *g,int terrainKinds[MAPH][MAPW],int minX,int minY,int maxX,int maxY) {
    if(g->area!=AREA_CAMP&&g->area!=AREA_LIGHTHOUSE)return;
    if(!TEX_CLIFF_ATLAS.id)return;
    // Draw only the coastline segments inside the camera's visible tile bounds.
    for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++) {
        if(x<=0||y<=0||x>=MAPW-1||y>=MAPH-1)continue;
        if(terrainKinds[y][x]==4)continue;
        bool south=terrainKinds[y+1][x]==4;
        bool north=terrainKinds[y-1][x]==4;
        bool east=terrainKinds[y][x+1]==4;
        bool west=terrainKinds[y][x-1]==4;
        if(!south&&!north&&!east&&!west)continue;
        unsigned int h=TileHash(x,y,(int)g->area+73);
        const int cliffVariants[3]={0,6,7};
        int index=cliffVariants[h%3u];
        float rotation=0.0f,ox=0.0f,oy=0.0f;
        if(south){rotation=0.0f;oy=28.0f;}
        else if(north){rotation=180.0f;oy=-28.0f;}
        else if(east){rotation=270.0f;ox=28.0f;}
        else if(west){rotation=90.0f;ox=-28.0f;}
        Vector2 p=WorldToScreen((float)x,(float)y);
        p.x+=ox;p.y+=oy;
        float size=112.0f;
        Rectangle src={(float)((index%8)*128),(float)((index/8)*128),128.0f,128.0f};
        Rectangle dst={p.x-size*.5f,p.y-size*.5f,size,size};
        DrawTexturePro(TEX_CLIFF_ATLAS,src,dst,(Vector2){size*.5f,size*.5f},rotation,WHITE);
    }
}
#if defined(MISTISLE_PI_PROFILE)
#define MISTISLE_RAIN_DROPS 56
#define MISTISLE_RAIN_RIPPLES 6
#else
#define MISTISLE_RAIN_DROPS 190
#define MISTISLE_RAIN_RIPPLES 22
#endif
static void BuildGroundCache(Game *g) {
    if(!TEX_GROUND_CACHE.id || CACHED_GROUND_AREA==(int)g->area)return;
    int terrainKinds[MAPH][MAPW]={{0}};
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++)
        terrainKinds[y][x]=TerrainKindFromAtlasIndex(g->world.terrain[y][x]);
    BeginTextureMode(TEX_GROUND_CACHE);
    ClearBackground((Color){20,34,39,255});
    // The backdrop's gradients, distant scenery and static decorative marks are
    // also baked once. Repainting them per frame was a major source of Pi overdraw.
    DrawWorldBackdrop(g);
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++) {
        int kind=terrainKinds[y][x];
        DrawTopDownTile(x,y,g->world.terrain[y][x],kind,0.0f);
        if(TEX_TOPDOWN_TILES.id) {
            Vector2 p=WorldToScreen((float)x,(float)y);
            Rectangle dst={p.x-TILE_SIZE*.5f,p.y-TILE_SIZE*.5f,TILE_SIZE,TILE_SIZE};
            for(int direction=0;direction<4;direction++) {
                int overlay=AREA_OVERLAY[(int)g->area][direction][y][x];
                if(overlay<0)continue;
                Rectangle src={(float)((overlay%4)*56),(float)((overlay/4)*56),56.0f,56.0f};
                DrawTexturePro(TEX_TOPDOWN_TILES,src,dst,(Vector2){TILE_SIZE*.5f,TILE_SIZE*.5f},
                               (float)(direction*90),WHITE);
            }
        }
        if(kind!=4)DrawTerrainAccent(x,y,g->area,kind,0.0f);
    }
    for(int y=1;y<MAPH-1;y++)for(int x=1;x<MAPW-1;x++)if(terrainKinds[y][x]!=4) {
        Vector2 p=WorldToScreen((float)x,(float)y);
        unsigned int h=TileHash(x,y,(int)g->area+31);
        if(terrainKinds[y][x-1]==4) {
            float yy=p.y-13.0f+(float)(h%8u);
            DrawLineEx((Vector2){p.x-28,yy},(Vector2){p.x-20,yy+2},1.8f,Fade(WHITE,.38f));
        }
        if(terrainKinds[y][x+1]==4) {
            float yy=p.y+8.0f-(float)(h%7u);
            DrawLineEx((Vector2){p.x+20,yy},(Vector2){p.x+28,yy+1},1.8f,Fade(WHITE,.34f));
        }
        if(terrainKinds[y-1][x]==4) {
            float xx=p.x-11.0f+(float)(h%12u);
            DrawLineEx((Vector2){xx,p.y-28},(Vector2){xx+8,p.y-25},1.6f,Fade(WHITE,.32f));
        }
        if(terrainKinds[y+1][x]==4) {
            float xx=p.x+4.0f-(float)(h%11u);
            DrawLineEx((Vector2){xx,p.y+25},(Vector2){xx+8,p.y+28},1.6f,Fade(WHITE,.34f));
        }
    }
    DrawCoastalCliffs(g,terrainKinds,0,0,MAPW-1,MAPH-1);
    EndTextureMode();
    CACHED_GROUND_AREA=(int)g->area;
}
static void RenderWorld(Game *g) {
    BuildGroundCache(g);
    int terrainKinds[MAPH][MAPW]={{0}};
    for(int y=0;y<MAPH;y++)for(int x=0;x<MAPW;x++)
        terrainKinds[y][x]=TerrainKindFromAtlasIndex(g->world.terrain[y][x]);
    int minX,minY,maxX,maxY;
    GetVisibleTileBounds(g,&minX,&minY,&maxX,&maxY);
    BeginMode2D(g->cam);
    if(TEX_GROUND_CACHE.id) {
        Rectangle src={0,0,(float)TEX_GROUND_CACHE.texture.width,-(float)TEX_GROUND_CACHE.texture.height};
        Rectangle dst={0,0,(float)WORLD_PIXEL_W,(float)WORLD_PIXEL_H};
        DrawTexturePro(TEX_GROUND_CACHE.texture,src,dst,(Vector2){0,0},0,WHITE);
        for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
            if(terrainKinds[y][x]==4)DrawTerrainAccent(x,y,g->area,4,g->time);
    } else {
        // Safe fallback if the target GPU cannot allocate the single-area ground cache.
        DrawWorldBackdrop(g);
        for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++) {
            int kind=terrainKinds[y][x];
            DrawTopDownTile(x,y,g->world.terrain[y][x],kind,g->time);
            if(TEX_TOPDOWN_TILES.id) {
                Vector2 p=WorldToScreen((float)x,(float)y);
                Rectangle dst={p.x-TILE_SIZE*.5f,p.y-TILE_SIZE*.5f,TILE_SIZE,TILE_SIZE};
                for(int direction=0;direction<4;direction++) {
                    int overlay=AREA_OVERLAY[(int)g->area][direction][y][x];
                    if(overlay<0)continue;
                    Rectangle src={(float)((overlay%4)*56),(float)((overlay/4)*56),56.0f,56.0f};
                    DrawTexturePro(TEX_TOPDOWN_TILES,src,dst,(Vector2){TILE_SIZE*.5f,TILE_SIZE*.5f},
                                   (float)(direction*90),WHITE);
                }
            }
            DrawTerrainAccent(x,y,g->area,kind,g->time);
        }
        DrawCoastalCliffs(g,terrainKinds,minX,minY,maxX,maxY);
    }

    // Scene objects are authored in Tiled and depth-sorted by their ground point.
    RenderItem items[512]; int count=0;
    for(int i=0;i<g->world.objectCount;i++) {
        MapObject *obj=&g->world.objects[i];
        if(obj->type>=1000&&obj->type<1003)
            PushRenderItem(items,&count,RENDER_TREE,obj->type-1000,obj->pos,obj->scale);
        else if(obj->type==2000)
            PushRenderItem(items,&count,RENDER_FIRE,0,obj->pos,obj->scale);
        else
            PushRenderItem(items,&count,RENDER_PROP,obj->type,obj->pos,obj->scale);
    }
    for(int i=0;i<g->world.pickupCount;i++) if(!g->world.pickups[i].taken)
        PushRenderItem(items,&count,RENDER_PICKUP,i,g->world.pickups[i].tile,1.0f);
    if(g->area==AREA_RUINS||g->area==AREA_LIGHTHOUSE)PushRenderItem(items,&count,RENDER_SHRINE,0,g->world.shrine,1.0f);
    if(g->area==AREA_CAMP) {
        PushRenderItem(items,&count,RENDER_KEEPER,0,(Vector2){7.0f,7.0f},1.0f);
        if(!g->lumi.owned)PushRenderItem(items,&count,RENDER_LUMI,0,(Vector2){6.0f,8.0f},1.0f);
    }
    if(g->lumi.owned)PushRenderItem(items,&count,RENDER_LUMI,0,g->lumi.pos,1.0f);
    if(g->area==AREA_GROVE&&!g->splash.owned)PushRenderItem(items,&count,RENDER_MOATAIL,0,(Vector2){14,7},1.0f);
    else if(g->splash.owned)PushRenderItem(items,&count,RENDER_MOATAIL,0,g->splash.pos,1.0f);
    if(g->area==AREA_RUINS&&!g->spark.owned)PushRenderItem(items,&count,RENDER_GECKO,0,(Vector2){13,8},1.0f);
    else if(g->spark.owned)PushRenderItem(items,&count,RENDER_GECKO,0,g->spark.pos,1.0f);
    if(g->area==AREA_GROVE) {
        Vector2 fireflyGrid={10.6f+sinf(g->fireflyPhase*0.8f)*0.22f,5.4f+cosf(g->fireflyPhase*0.65f)*0.18f};
        if(!g->fireflyDiscovered)PushRenderItem(items,&count,RENDER_FIREFLY,0,fireflyGrid,1.0f);
        if(!g->leaflingFed)PushRenderItem(items,&count,RENDER_LEAFLING,0,(Vector2){12.4f,8.0f},1.0f);
        if(!g->groveMoteDefeated)PushRenderItem(items,&count,RENDER_MOTE,0,(Vector2){13.2f,9.2f},1.0f);
    }
    if(g->area==AREA_LIGHTHOUSE&&!g->bossDefeated)
        PushRenderItem(items,&count,RENDER_BOSS,0,g->world.bossPos,1.0f);
    if(g->area==AREA_CAMP) {
        PushRenderItem(items,&count,RENDER_GATE,0,g->world.groveGate,1.0f);
        PushRenderItem(items,&count,RENDER_GATE,1,g->world.ruinGate,1.0f);
        PushRenderItem(items,&count,RENDER_GATE,2,g->world.shrine,1.0f);
    } else {
        PushRenderItem(items,&count,RENDER_GATE,3,g->world.returnPoint,1.0f);
    }
    PushRenderItem(items,&count,RENDER_PLAYER,0,g->player.pos,1.0f);

    for(int i=1;i<count;i++) {
        RenderItem key=items[i]; int j=i-1;
        while(j>=0 && items[j].depth>key.depth) { items[j+1]=items[j]; j--; }
        items[j+1]=key;
    }
    for(int i=0;i<count;i++) {
        RenderItem *it=&items[i]; Vector2 p=WorldToScreen(it->grid.x,it->grid.y);
        Vector2 screen=GetWorldToScreen2D(p,g->cam);
        if(screen.x<-180.0f||screen.x>SW+180.0f||screen.y<-180.0f||screen.y>SH+180.0f)continue;
        switch(it->kind) {
            case RENDER_TREE: p.y+=8.0f; DrawEllipse((int)p.x,(int)p.y+7,(int)(34*it->scale),(int)(8*it->scale),(Color){35,67,49,72}); DrawEllipse((int)p.x,(int)p.y+5,(int)(25*it->scale),(int)(5*it->scale),(Color){71,111,70,48}); DrawFloraProp(8+(it->index%3),p,108*it->scale,120*it->scale); break;
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
                else if(it->index==33)DrawAreaPropTint(13,p,150*it->scale,190*it->scale,(Color){145,160,176,255});
                else if(it->index==20)DrawAreaProp(0,p,164*it->scale,112*it->scale);
                else if(it->index==21)DrawAreaProp(1,p,126*it->scale,112*it->scale);
                else if(it->index==22)DrawAreaProp(2,p,144*it->scale,116*it->scale);
                else if(it->index==24)DrawAreaProp(4,p,150*it->scale,158*it->scale);
                else if(it->index==25)DrawAreaProp(5,p,126*it->scale,126*it->scale);
                else if(it->index==26)DrawAreaProp(6,p,102*it->scale,94*it->scale);
                else if(it->index==27)DrawAreaProp(7,p,156*it->scale,168*it->scale);
                else if(it->index==28)DrawAreaProp(8,p,184*it->scale,184*it->scale);
                else if(it->index==29)DrawAreaProp(9,p,104*it->scale,122*it->scale);
                else if(it->index==30)DrawAreaProp(10,p,128*it->scale,112*it->scale);
                else if(it->index==31)DrawAreaProp(11,p,132*it->scale,132*it->scale);
                else if(it->index==32)DrawAreaProp(12,p,130*it->scale,100*it->scale);
                else if(it->index==34)DrawAreaProp(14,p,110*it->scale,118*it->scale);
                else if(it->index==35)DrawAreaProp(15,p,112*it->scale,116*it->scale);
                else if(it->index>=20&&it->index<=35)DrawAreaProp(it->index-20,p,76*it->scale,82*it->scale);
                else if(it->index>=100&&it->index<200)DrawFloraProp(it->index-100,p,72*it->scale,78*it->scale);
                else if(it->index>=200&&it->index<300)DrawCliffProp(it->index-200,p,88*it->scale,88*it->scale);
                else if(it->index==302)DrawCoastalHouse(p,it->scale);
                else if(it->index==303)DrawStructureProp(3,p,132*it->scale,116*it->scale);
                else if(it->index==304)DrawStructureProp(4,p,166*it->scale,180*it->scale);
                else if(it->index==305)DrawStructureProp(5,p,168*it->scale,152*it->scale);
                else if(it->index>=300&&it->index<400)DrawStructureProp(it->index-300,p,112*it->scale,104*it->scale);
                else if(it->index>=400&&it->index<416)DrawOriginalProp(it->index-400,p,92*it->scale,92*it->scale);
                else if(it->index==500&&TEX_GROVE_COTTAGE.id)DrawGroveCottage(p,it->scale);
                else if(it->index>=500&&it->index<504)DrawKenneyBuilding(it->index-500,p,it->scale);
                break;
            case RENDER_FIRE: DrawCampfire(p,g->time); break;
            case RENDER_PICKUP: DrawPickup(&g->world.pickups[it->index]); break;
            case RENDER_SHRINE: DrawShrine(p,g->time,g->shrineSolved,g->area==AREA_RUINS); break;
            case RENDER_KEEPER: DrawKeeper(p,g->time,1.0f); break;
            case RENDER_LUMI: DrawLumi(p,g->time,g->lumi.bob); break;
            case RENDER_MOATAIL:
                DrawMoatail(p,g->time);
                if(g->area==AREA_GROVE&&!g->splash.owned) {
                    DrawCircle((int)p.x,(int)p.y-58,11,C_GOLD);
                    DrawUIText("!",(int)p.x-4,(int)p.y-67,17,WHITE);
                }
                break;
            case RENDER_GECKO:
                DrawGecko(p,g->time);
                if(g->area==AREA_RUINS&&!g->spark.owned) {
                    DrawCircle((int)p.x,(int)p.y-58,11,C_GOLD);
                    DrawUIText("!",(int)p.x-4,(int)p.y-67,17,WHITE);
                }
                break;
            case RENDER_FIREFLY: DrawFirefly(p,g->time); break;
            case RENDER_LEAFLING: DrawLeafling(p,g->time); break;
            case RENDER_MOTE: DrawMote(p,g->time); break;
            case RENDER_BOSS:
                DrawLighthouseGuardian(p,g->time,1.0f,false);
                DrawRectangleRounded((Rectangle){p.x-52,p.y-168,104,22},.25f,5,(Color){8,24,38,230});
                DrawRectangleRoundedLinesEx((Rectangle){p.x-52,p.y-168,104,22},.25f,5,1.5f,(Color){255,205,115,220});
                DrawUIText("灯塔守卫",(int)p.x-28,(int)p.y-164,13,(Color){255,232,170,255});
                break;
            case RENDER_GATE: DrawGateMarker(g,it->index,it->grid); break;
            case RENDER_PLAYER: DrawPlayer(p,g->time,Vector2Length(g->player.vel)>.1f,g->player.facing); break;
        }
    }
    const char *objectiveTitle,*objectiveDetail;
    Vector2 objectiveTarget;
    bool hasObjectiveTarget=false;
    GetCurrentObjective(g,&objectiveTitle,&objectiveDetail,&objectiveTarget,&hasObjectiveTarget);
    if(hasObjectiveTarget && g->scene!=SCENE_END && g->scene!=SCENE_TITLE)DrawObjectiveMarker(objectiveTarget,g->time);
    for(int i=0;i<MAX_PARTICLES;i++)if(g->particles[i].life>0) {
        Particle *p=&g->particles[i];
        DrawCircleV(p->p,p->size*(p->life/p->maxLife),Fade(p->c,p->life/p->maxLife));
    }
    EndMode2D();
    float night=0.5f+0.5f*cosf(g->dayClock*PI2);
    if(night>0.62f)DrawRectangle(0,0,SW,SH,Fade((Color){43,59,99,255},(night-0.62f)*0.22f));
    if(g->weather==1) {
        // Cool overcast tint, three rain depths and visible impacts on water surfaces.
        DrawRectangle(0,0,SW,SH,Fade((Color){34,57,75,255},.105f));
        for(int i=0;i<MISTISLE_RAIN_DROPS;i++) {
            float speed=285.0f+(float)(i%5)*72.0f;
            float travel=fmodf(g->time*speed+(float)(i*83),SH+70.0f);
            float x=fmodf((float)(i*149)+g->time*(125.0f+(float)(i%4)*19.0f),SW+90.0f)-45.0f;
            float y=travel-35.0f;
            float length=12.0f+(float)(i%4)*4.5f;
            float alpha=.28f+(float)(i%4)*.075f;
            float width=(i%7==0)?1.8f:1.0f;
            DrawLineEx((Vector2){x,y},(Vector2){x-8.0f,y+length},width,Fade((Color){197,222,232,255},alpha));
        }
        for(int i=0;i<MISTISLE_RAIN_RIPPLES;i++) {
            float x=fmodf((float)(i*97)+g->time*31.0f,SW);
            float y=fmodf((float)(i*61)+g->time*19.0f,SH);
            float pulse=fmodf(g->time*1.8f+(float)i*.37f,1.0f);
            DrawCircleLines((int)x,(int)y,(int)(2.0f+pulse*4.0f),Fade((Color){196,225,231,255},.38f*(1.0f-pulse)));
        }
        // Ripples stay attached to the visible water tiles as the camera moves.
        int rippleCount=0;
        for(int y=minY;y<=maxY&&rippleCount<MISTISLE_RAIN_RIPPLES;y++)for(int x=minX;x<=maxX&&rippleCount<MISTISLE_RAIN_RIPPLES;x++) {
            if(terrainKinds[y][x]!=4)continue;
            unsigned int h=TileHash(x,y,91);
            if(h%23u!=0u)continue;
            Vector2 world=WorldToScreen((float)x,(float)y);
            Vector2 screen=GetWorldToScreen2D(world,g->cam);
            float pulse=fmodf(g->time*1.4f+(float)(h%19u)*.11f,1.0f);
            DrawCircleLines((int)screen.x,(int)screen.y,(int)(2.0f+pulse*5.0f),Fade((Color){204,231,231,255},.40f*(1.0f-pulse)));
            rippleCount++;
        }
    } else if(g->weather==2) {
        for(int i=0;i<24;i++){int x=(i*97+(int)(sinf(g->time+i)*18)+SW)%SW;int y=(i*53+(int)(cosf(g->time*.7f+i)*12)+SH)%SH;DrawCircle(x,y,2,Fade(C_GOLD,.6f));}
    }
}
static void Panel(Rectangle r) {
    Rectangle shadow={r.x+2.0f,r.y+4.0f,r.width,r.height};
    DrawRectangleRounded(shadow,.12f,18,Fade((Color){12,27,38,255},.28f));
    DrawRectangleRounded(r,.12f,18,C_PAPER);
    DrawRectangleRoundedLines(r,.12f,18,(Color){186,145,78,235});
    Rectangle inner={r.x+4.0f,r.y+4.0f,r.width-8.0f,r.height-8.0f};
    DrawRectangleRoundedLines(inner,.10f,16,Fade((Color){255,255,255,255},.38f));
}
static void PanelDark(Rectangle r) {
    Rectangle shadow={r.x+2.0f,r.y+4.0f,r.width,r.height};
    DrawRectangleRounded(shadow,.12f,18,Fade((Color){4,13,25,255},.48f));
    DrawRectangleRounded(r,.12f,18,(Color){12,29,47,238});
    DrawRectangleRoundedLines(r,.12f,18,(Color){197,157,91,238});
    Rectangle inner={r.x+4.0f,r.y+4.0f,r.width-8.0f,r.height-8.0f};
    DrawRectangleRoundedLines(inner,.10f,16,Fade((Color){240,221,179,255},.16f));
    DrawRectangle((int)(r.x+12),(int)(r.y+13),3,(int)(r.height-26),Fade(C_GOLD,.78f));
}
static void RenderHUD(Game *g) {
    PanelDark((Rectangle){20,18,405,112});
    DrawUIText("MIST ISLE  /  CHAPTER 01",40,32,15,(Color){205,214,198,255});
    const char *areaName=g->area==AREA_CAMP?"海岸营地":g->area==AREA_GROVE?"辉光林地":g->area==AREA_RUINS?"潮汐遗迹":"灯塔庭院";
    DrawUIText(areaName,276,32,18,(Color){255,221,158,255});
    DrawCircle(397,42,8,Fade(C_GOLD,.75f));
    DrawUIText("失落的灯塔",40,54,28,(Color){246,239,217,255});
    DrawUIText(TextFormat("生命 %d    果实 %d    贝壳 %d    星晶 %d",(int)g->player.hp,g->player.berries,g->player.shells,g->player.crystals),40,96,17,(Color){211,220,205,255});

    const char *objectiveTitle,*objectiveDetail;
    Vector2 objectiveTarget;
    bool hasObjectiveTarget=false;
    GetCurrentObjective(g,&objectiveTitle,&objectiveDetail,&objectiveTarget,&hasObjectiveTarget);
    int partyCount=(g->lumi.owned?1:0)+(g->splash.owned?1:0)+(g->spark.owned?1:0);
    const char *activeName=g->selectedBuddy==1&&g->splash.owned?"沫尾兽":g->selectedBuddy==2&&g->spark.owned?"鸣晶蜥":g->lumi.owned?"苔灯灵":partyCount>0?"伙伴":"尚无伙伴";
    Buddy *active=CurrentBuddy(g);
    if(g->objectiveCollapsed) {
        Rectangle panel={SW-300.0f,18.0f,280.0f,44.0f};
        Rectangle toggle={SW-88.0f,24.0f,68.0f,28.0f};
        PanelDark(panel);
        DrawUIText(objectiveTitle,(int)panel.x+15,(int)panel.y+14,14,(Color){246,239,217,255});
        DrawRectangleRounded(toggle,.18f,6,(Color){35,61,76,255});
        DrawRectangleRoundedLines(toggle,.18f,6,Fade(C_GOLD,.7f));
        DrawUIText("O 展开",(int)toggle.x+8,(int)toggle.y+7,11,(Color){255,226,173,255});
    } else {
        Rectangle panel={SW-350.0f,18.0f,330.0f,116.0f};
        Rectangle toggle={SW-92.0f,26.0f,72.0f,28.0f};
        PanelDark(panel);
        DrawUIText("当前目标",(int)panel.x+18,(int)panel.y+10,14,(Color){205,214,198,255});
        DrawRectangleRounded(toggle,.18f,6,(Color){35,61,76,255});
        DrawRectangleRoundedLines(toggle,.18f,6,Fade(C_GOLD,.7f));
        DrawUIText("O 收起",(int)toggle.x+7,(int)toggle.y+7,11,(Color){255,226,173,255});
        DrawUIText(objectiveTitle,(int)panel.x+18,(int)panel.y+34,18,(Color){246,239,217,255});
        DrawUITextWrapped(objectiveDetail,(int)panel.x+18,(int)panel.y+62,294,12,1,(Color){211,220,205,255});
        DrawUIText(TextFormat("伙伴 %d/3  ·  %s Lv.%d",partyCount,activeName,active->level),(int)panel.x+18,(int)panel.y+91,11,(Color){172,193,184,255});
    }

    PanelDark((Rectangle){20,652,970,48});
    DrawUIText("WASD 移动   SHIFT 冲刺   E 互动   TAB 伙伴   J 手记   I 背包   M 地图   O 任务栏   F7 天气   F5 保存",38,667,12,(Color){211,220,205,255});

    if(g->area==AREA_CAMP && Dist(g->player.pos,g->world.groveGate)<1.6f) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  前往辉光林地",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_CAMP && Dist(g->player.pos,g->world.ruinGate)<1.6f) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  前往潮汐遗迹",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_CAMP && Dist(g->player.pos,g->world.shrine)<1.6f && g->shrineSolved) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  前往灯塔庭院",550,596,17,(Color){211,220,205,255});
    } else if(g->area!=AREA_CAMP && Dist(g->player.pos,g->world.returnPoint)<1.8f) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  返回海岸营地",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_CAMP && !g->lumi.owned && Dist(g->player.pos,(Vector2){6,8})<1.65f) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  救助苔灯灵",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_CAMP && Dist(g->player.pos,(Vector2){7,7})<2.0f) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  与守望者交谈",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_CAMP && g->lumi.owned && Dist(g->player.pos,(Vector2){6.8f,6.7f})<1.5f) {
        PanelDark((Rectangle){530,585,220,40});DrawUIText("E  在营火旁休息",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_LIGHTHOUSE && !g->bossDefeated && Dist(g->player.pos,g->world.bossPos)<2.2f) {
        PanelDark((Rectangle){530,585,250,40});DrawUIText("E  挑战灯塔守卫",550,596,17,(Color){211,220,205,255});
    } else if(g->area==AREA_LIGHTHOUSE && g->bossDefeated && Dist(g->player.pos,g->world.shrine)<3.8f) {
        PanelDark((Rectangle){500,585,280,40});DrawUIText("E  点亮失落的灯塔",525,596,17,(Color){211,220,205,255});
    }
    if(g->toastTimer>0) {
        PanelDark((Rectangle){380,600,520,42});
        DrawUIText(g->toast,400,612,18,(Color){247,235,207,255});
    }
}
static void RenderJournal(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){35,55,48,255},.48f));
    Panel((Rectangle){250,105,780,510});
    DrawUIText("旅 人 手 记",300,135,34,(Color){74,105,83,255});
    DrawUIText("CHAPTER 01  /  THE LOST LIGHTHOUSE",303,179,15,(Color){132,145,112,255});
    DrawLine(300,207,980,207,(Color){220,211,183,255});
    const char *objectiveTitle,*objectiveDetail;
    Vector2 objectiveTarget;
    bool hasObjectiveTarget=false;
    GetCurrentObjective(g,&objectiveTitle,&objectiveDetail,&objectiveTarget,&hasObjectiveTarget);
    DrawUIText("当前目标",303,230,21,(Color){104,135,94,255});
    DrawUIText(objectiveTitle,303,265,21,C_INK);
    DrawUITextWrapped(objectiveDetail,303,298,620,16,2,C_INK);
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
    // The camp is the hub for the grove and ruins; the lighthouse is reached
    // through the solved tidal-ruins shrine, not by a direct camp connection.
    DrawLineEx(pts[0],pts[1],5,(Color){185,177,139,255});
    DrawLineEx(pts[0],pts[2],5,(Color){185,177,139,255});
    DrawLineEx(pts[2],pts[3],5,(Color){185,177,139,255});
    for(int i=0;i<4;i++) {
        bool unlocked=(i==0)||(i==1)||(i==2&&g->lumi.owned&&g->splash.owned)||(i==3&&g->shrineSolved);
        DrawCircleV(pts[i],g->area==i?36:28,g->area==i?C_GOLD:unlocked?(Color){144,190,132,255}:(Color){181,184,164,255});
        DrawCircleV(pts[i],13,(Color){255,240,196,255});
        if(g->area==i) {
            DrawCircleLines((int)pts[i].x,(int)pts[i].y,41,(Color){255,112,96,255});
            DrawCircleV((Vector2){pts[i].x+22.0f,pts[i].y-22.0f},6,(Color){231,82,76,255});
            DrawCircleV((Vector2){pts[i].x+22.0f,pts[i].y-22.0f},2,WHITE);
        }
        DrawUIText(names[i],(int)pts[i].x-45,(int)pts[i].y+42,17,C_INK);
    }
    DrawCircleV((Vector2){342,551},6,(Color){231,82,76,255});
    DrawUIText("当前位置",356,543,15,(Color){105,121,97,255});
    DrawCircleV((Vector2){500,551},6,(Color){144,190,132,255});
    DrawUIText("已开放",514,543,15,(Color){105,121,97,255});
    DrawCircleV((Vector2){640,551},6,(Color){181,184,164,255});
    DrawUIText("未开放",654,543,15,(Color){105,121,97,255});
    DrawUIText("主地图为枢纽；各区域通过入口往返。",320,580,17,(Color){105,121,97,255});
    DrawUIText("按 M / ESC 关闭",800,580,16,(Color){132,137,111,255});
    DrawUIText("按 M / ESC 关闭",800,580,16,(Color){132,137,111,255});
}
static void DrawDialoguePortrait(Game *g,int portrait,const char *speaker) {
    Rectangle card={22,372,246,314};
    DrawRectangleRounded(card,.10f,10,(Color){11,31,43,245});
    DrawRectangleRoundedLinesEx(card,.10f,10,2.0f,(Color){208,177,111,255});
    DrawRectangleGradientV(38,382,214,286,(Color){62,83,86,255},(Color){22,43,54,255});
    Texture2D *fallback=portrait==0?&TEX_HERO_PORTRAIT:
        portrait==1?&TEX_WATCHER_PORTRAIT:
        portrait==2?&TEX_GUARDIAN_PORTRAIT:
        portrait==3?&TEX_LUMI_ILLUSTRATION:
        portrait==4?&TEX_MOATAIL_ILLUSTRATION:
        portrait==5?&TEX_GECKO_ILLUSTRATION:&TEX_HERO_PORTRAIT;
    Rectangle dst={38,382,214,285};
    // Dedicated high-resolution illustrations always take priority over the old
    // low-detail portrait atlas. Pixel walk sheets are never used as dialogue portraits.
    if(fallback->id) {
        Rectangle src={0,0,(float)fallback->width,(float)fallback->height};
        DrawTexturePro(*fallback,src,dst,(Vector2){0,0},0,WHITE);
    } else if(portrait>=0&&portrait<=2&&TEX_CHARACTER_PORTRAITS.id) {
        Rectangle src={(float)(portrait*128),0,128,170};
        DrawTexturePro(TEX_CHARACTER_PORTRAITS,src,dst,(Vector2){0,0},0,WHITE);
    }
    const char *label=(speaker&&speaker[0])?speaker:"旅人";
    DrawRectangleRounded((Rectangle){42,670,206,25},.2f,5,(Color){8,25,36,225});
    DrawUIText(label,52,674,13,(Color){255,231,178,255});
}
static void RenderDialogue(Game *g) {
    DrawRectangle(0,0,SW,SH,Fade((Color){22,39,48,255},.46f));
    Panel((Rectangle){220,520,1032,160});
    int page=g->dialoguePage;
    if(page<0)page=0;
    if(page>=g->dialogueCount)page=g->dialogueCount-1;
    int portrait=page>=0?g->dialoguePortrait[page]:0;
    const char *speaker=page>=0?g->dialogueLineSpeaker[page]:g->dialogueSpeaker;
    if(strncmp(speaker,"旅人",strlen("旅人"))==0)speaker="旅人";
    DrawDialoguePortrait(g,portrait,speaker);
    DrawRectangleRounded((Rectangle){258,541,190,31},.16f,6,(Color){37,64,61,255});
    DrawUIText(speaker,274,547,17,(Color){255,224,165,255});
    DrawUITextWrapped(g->dialogue[page],270,592,930,23,2,(Color){44,61,55,255});
    DrawLineEx((Vector2){260,638},(Vector2){1210,638},1.0f,(Color){177,158,119,150});
    DrawUIText("按 E / Enter / 鼠标左键继续",886,650,14,(Color){112,124,103,255});
}
static void RenderBattle(Game *g) {
    Enemy *e=&g->enemies[g->battleEnemy];
    bool isBoss=e->type==ENEMY_BOSS;
    DrawRectangleGradientV(0,0,SW,SH,
        isBoss?(Color){20,26,38,255}:(Color){48,66,79,255},
        isBoss?(Color){67,53,54,255}:(Color){91,119,112,255});
    for(int i=0;i<8;i++) {
        int x=70+i*165;
        Color haze=isBoss?(Color){175,105,64,48}:(Color){77,111,102,80};
        DrawCircle(x,180+(i%3)*24,55,haze);
        DrawCircle(x+42,190+(i%2)*20,38,isBoss?(Color){255,191,102,24}:(Color){119,145,111,65});
    }
    if(isBoss) {
        // The guardian battle is a distinct, darker set-piece rather than a reskinned wild encounter.
        DrawCircle(880,390,220,Fade((Color){255,166,74,255},.055f));
        DrawCircle(880,390,150,Fade((Color){255,194,104,255},.07f));
        for(int i=0;i<7;i++) {
            float x=250.0f+i*210.0f;
            DrawLineEx((Vector2){x,70},(Vector2){880.0f+(x-880.0f)*.16f,370},2.0f,Fade((Color){255,190,108,255},.10f));
        }
    }
    DrawEllipse(640,435,470,118,(Color){35,55,57,100});
    DrawEllipse(640,422,420,92,(Color){120,150,128,255});
    DrawEllipse(640,410,350,65,(Color){148,174,142,255});
    DrawEllipse(395,455,78,20,(Color){37,54,56,130});
    DrawEllipse(880,455,100,25,(Color){37,54,56,130});

    const char *enemyName=e->type==ENEMY_MOTE?"林地微光兽":e->type==ENEMY_BOSS?"灯塔守卫":"遗迹守卫";
    Panel((Rectangle){28,22,350,112});
    DrawUIText(isBoss?"首领战":"遭遇战",50,34,25,isBoss?(Color){255,220,168,255}:C_INK);
    DrawUIText(TextFormat("第 %d 回合",g->battleRound),50,68,15,(Color){104,119,93,255});
    DrawUIText(TextFormat("旅人生命  %d / %d",(int)g->player.hp,(int)g->player.maxHp),50,94,15,C_INK);
    DrawRectangle(210,97,142,12,(Color){218,213,193,255});
    DrawRectangle(210,97,(int)(142.0f*fmaxf(0.0f,g->player.hp/g->player.maxHp)),12,(Color){111,174,112,255});

    Panel((Rectangle){902,22,350,112});
    DrawUIText(enemyName,925,35,22,C_INK);
    DrawUIText(TextFormat("%s  %d / %d",isBoss?"核心耐久":"生命",(int)fmaxf(0,e->hp),(int)e->maxHp),925,72,15,isBoss?(Color){235,197,143,255}:(Color){104,119,93,255});
    DrawRectangle(925,99,295,13,(Color){218,213,193,255});
    if(isBoss)DrawRectangleGradientH(925,99,(int)(295.0f*fmaxf(0.0f,e->hp/e->maxHp)),13,(Color){255,201,104,255},(Color){174,66,55,255});
    else DrawRectangle(925,99,(int)(295.0f*fmaxf(0.0f,e->hp/e->maxHp)),13,C_PINK);
    if(isBoss) {
        int phase=e->hp<=50.0f?3:(e->hp<=100.0f?2:1);
        bool heavy=phase==3?(g->battleRound%2==0):(g->battleRound%3==0);
        const char *phaseLabel=phase==3?"阶段三 · 最后的灯火":(phase==2?"阶段二 · 共鸣增强":"阶段一 · 灯火苏醒");
        DrawRectangleRounded((Rectangle){742,145,510,64},.18f,8,(Color){12,24,35,232});
        DrawRectangleRoundedLines((Rectangle){742,145,510,64},.18f,8,Fade((Color){255,198,113,255},.7f));
        DrawUIText(phaseLabel,760,151,17,(Color){255,219,166,255});
        const char *coreState=g->enemySoaked?"核心受潮：本次攻击伤害降低":
            (heavy?"核心蓄能：下一回合将发动重击":"核心稳定：可用潮湿技能打断");
        Color coreTint=g->enemySoaked?(Color){132,216,224,255}:
            (heavy?(Color){255,170,117,255}:(Color){154,220,211,255});
        DrawUIText(coreState,760,178,14,coreTint);
    }

    if(g->selectedBuddy==0 && g->lumi.owned)DrawLumi((Vector2){395,430},g->time,0);
    else if(g->selectedBuddy==1 && g->splash.owned)DrawMoatail((Vector2){395,430},g->time);
    else if(g->selectedBuddy==2 && g->spark.owned)DrawGecko((Vector2){395,430},g->time);
    else DrawPlayer((Vector2){395,430},g->time,false,0);

    if(e->type==ENEMY_MOTE) {
        DrawMote((Vector2){880,385},g->time);
    } else if(e->type==ENEMY_BOSS) {
        /* Boss arena uses the dedicated 64x80 pixel boss sheet, not the dialogue poster. */
        DrawLighthouseGuardian((Vector2){880,454},g->time,1.5f,g->battleTurn!=0);
    } else {
        DrawCircleV((Vector2){880,380},62,Fade(C_LILAC,.18f));
        DrawEllipse(880,395,61,48,(Color){163,139,160,255});
        DrawTriangle((Vector2){832,366},(Vector2){849,302},(Vector2){872,366},(Color){204,181,205,255});
        DrawTriangle((Vector2){879,366},(Vector2){913,314},(Vector2){930,375},(Color){183,160,192,255});
        DrawCircle(861,378,7,C_INK);DrawCircle(899,378,7,C_INK);
        DrawCircle(861,376,2.5f,WHITE);DrawCircle(899,376,2.5f,WHITE);
    }

    Panel((Rectangle){24,510,1232,185});
    DrawUIText("选择出战伙伴",50,520,20,C_INK);
    DrawUIText(g->battleTurn==0?"选择技能并按 Enter 确认":"敌方正在行动，按 Enter 继续",830,520,16,(Color){104,119,93,255});
    int ids[3]={0,1,2};
    bool owned[3]={g->lumi.owned&&g->lumi.hp>0.0f,g->splash.owned&&g->splash.hp>0.0f,g->spark.owned&&g->spark.hp>0.0f};
    int count=0;
    for(int i=0;i<3;i++)if(owned[i])ids[count++]=i;
    int optionCount=count>0?count:1;
    if(g->battleChoice>=optionCount)g->battleChoice=optionCount-1;
    for(int slot=0;slot<optionCount;slot++) {
        int id=count>0?ids[slot]:-1;
        Rectangle card={(float)(50+slot*245),555,230,112};
        Color fill=slot==g->battleChoice?(Color){255,235,190,255}:(Color){247,245,226,245};
        DrawRectangleRounded(card,.12f,12,fill);
        DrawRectangleRoundedLines(card,.12f,12,slot==g->battleChoice?(Color){201,157,82,255}:(Color){220,214,188,255});
        DrawUIText(TextFormat("%d",slot+1),(int)card.x+12,(int)card.y+10,15,(Color){127,145,111,255});
        if(id==0) {
            DrawIllustratedPortrait(TEX_LUMI_ILLUSTRATION,(Rectangle){card.x+10,card.y+17,56,75});
            DrawUIText("苔灯灵",(int)card.x+78,(int)card.y+22,17,C_INK);
            DrawUIText("星光治愈",(int)card.x+78,(int)card.y+45,14,(Color){104,119,93,255});
            DrawUIText("回复生命",(int)card.x+78,(int)card.y+63,13,(Color){104,119,93,255});
        } else if(id==1) {
            DrawIllustratedPortrait(TEX_MOATAIL_ILLUSTRATION,(Rectangle){card.x+15,card.y+14,48,78});
            DrawUIText("沫尾兽",(int)card.x+78,(int)card.y+22,17,C_INK);
            DrawUIText("泡泡水流",(int)card.x+78,(int)card.y+45,14,(Color){104,119,93,255});
            DrawUIText("施加潮湿",(int)card.x+78,(int)card.y+63,13,(Color){104,119,93,255});
        } else if(id==2) {
            DrawIllustratedPortrait(TEX_GECKO_ILLUSTRATION,(Rectangle){card.x+15,card.y+14,48,78});
            DrawUIText("鸣晶蜥",(int)card.x+78,(int)card.y+22,17,C_INK);
            DrawUIText("晶弧放电",(int)card.x+78,(int)card.y+45,14,(Color){104,119,93,255});
            DrawUIText("克制潮湿",(int)card.x+78,(int)card.y+63,13,(Color){104,119,93,255});
        } else {
            DrawPlayer((Vector2){card.x+43,card.y+78},g->time,false,0);
            DrawUIText("旅人",(int)card.x+78,(int)card.y+22,17,C_INK);
            DrawUIText("基础攻击",(int)card.x+78,(int)card.y+45,14,(Color){104,119,93,255});
            DrawUIText("造成普通伤害",(int)card.x+78,(int)card.y+63,13,(Color){104,119,93,255});
        }
        if(id>=0) {
            Buddy *buddy=id==0?&g->lumi:(id==1?&g->splash:&g->spark);
            DrawUIText(TextFormat("体力 %d/%d",(int)buddy->hp,(int)buddy->maxHp),(int)card.x+78,(int)card.y+81,11,(Color){104,119,93,255});
            DrawRectangle((int)card.x+78,(int)card.y+98,132,6,(Color){215,211,193,255});
            DrawRectangle((int)card.x+78,(int)card.y+98,(int)(132.0f*fmaxf(0.0f,buddy->hp/buddy->maxHp)),6,
                          buddy->hp>35.0f?(Color){111,174,112,255}:(Color){205,112,91,255});
        }
    }
    Panel((Rectangle){815,555,410,112});
    DrawUIText("战斗操作",838,568,16,(Color){119,145,116,255});
    DrawUIText(optionCount==1?"1 / 鼠标点击卡片":TextFormat("1-%d / ← → 选择伙伴",optionCount),838,594,15,C_INK);
    DrawUIText("Enter 确认   鼠标点击卡片选择",838,619,14,C_INK);
    const char *synergyHint=g->enemySoaked?"敌人处于潮湿状态":(g->splash.owned&&g->spark.owned?"联动：先喷水，再使用晶弧":"招募更多伙伴可解锁技能联动");
    DrawUIText(synergyHint,838,644,14,(Color){104,119,93,255});
}
static void RenderTitle(Game *g) {
    Vector2 oldTarget=g->cam.target;
    g->cam.target=WorldToScreen(g->player.pos.x,g->player.pos.y);
    RenderWorld(g);
    g->cam.target=oldTarget;
    DrawRectangle(0,0,SW,SH,Fade((Color){9,24,36,255},.42f));
    DrawRectangleGradientV(0,0,SW,210,Fade((Color){8,21,36,255},.86f),Fade((Color){8,21,36,255},0.0f));
    DrawRectangleRounded((Rectangle){260,105,760,440},.08f,18,Fade((Color){11,28,43,255},.86f));
    DrawRectangleRoundedLines((Rectangle){260,105,760,440},.08f,18,Fade(C_GOLD,.65f));
    DrawUIText("雾 屿 回 响",420,158,62,(Color){255,235,192,255});
    DrawUIText("M I S T   I S L E",500,238,17,(Color){212,191,151,255});
    DrawLineEx((Vector2){390,282},(Vector2){890,282},1.5f,Fade(C_GOLD,.55f));
    DrawUIText("第一章  ·  失落的灯塔",440,318,29,(Color){237,225,192,255});
    DrawUIText("一段关于伙伴、勇气与重新点亮世界的旅程",375,375,20,(Color){205,218,202,255});
    DrawRectangleRounded((Rectangle){475,435,330,62},.16f,16,(Color){244,218,163,250});
    DrawRectangleRoundedLines((Rectangle){475,435,330,62},.16f,16,(Color){255,246,211,255});
    DrawUIText("按 Enter 开始旅程",535,451,23,(Color){55,77,65,255});
    DrawUIText(FileExists("mist_isle_save.dat")?"F9 继续上次旅程":"F9 读取存档",560,520,17,(Color){225,220,193,255});
}
static void RenderEnd(Game *g) {
    float t=g->endingTime;
    float fadeIn=fminf(1.0f,t/.7f);
    float pulse=.62f+.38f*sinf(t*3.1f);
    ClearBackground((Color){7,17,31,255});
    DrawRectangleGradientV(0,0,SW,SH,(Color){9,22,39,255},(Color){17,38,54,255});
    for(int i=0;i<58;i++) {
        float x=(float)((i*197+31)%SW);
        float y=(float)((i*83+17)%(SH-92));
        float a=.14f+.24f*(.5f+.5f*sinf(t*1.6f+i*1.37f));
        DrawCircle((int)x,(int)y,1.0f+(i%3)*.4f,Fade((Color){229,222,190,255},a));
    }
    Rectangle scene={306,18,668,668};
    if(TEX_ENDING_CG.id) {
        Rectangle src={0,0,(float)TEX_ENDING_CG.width,(float)TEX_ENDING_CG.height};
        DrawTexturePro(TEX_ENDING_CG,src,scene,(Vector2){0,0},0,WHITE);
        // The lighthouse lantern sits near the upper centre of the illustration.
        Vector2 lamp={scene.x+scene.width*.535f,scene.y+scene.height*.205f};
        float sweep=sinf(t*.72f)*74.0f;
        Vector2 beamA={scene.x+scene.width-4.0f,lamp.y-66.0f+sweep};
        Vector2 beamB={scene.x+scene.width-4.0f,lamp.y+66.0f+sweep};
        DrawTriangle(lamp,beamA,beamB,Fade((Color){255,224,151,255},.11f+.045f*pulse));
        DrawTriangle(lamp,(Vector2){scene.x+scene.width-4.0f,lamp.y-27.0f+sweep*.72f},
            (Vector2){scene.x+scene.width-4.0f,lamp.y+27.0f+sweep*.72f},Fade((Color){255,244,205,255},.16f+.06f*pulse));
        DrawCircleV(lamp,43.0f,Fade((Color){255,222,148,255},.14f+.11f*pulse));
        DrawCircleV(lamp,23.0f,Fade((Color){255,240,190,255},.25f+.16f*pulse));
        DrawCircleV(lamp,9.0f,(Color){255,249,220,255});
        for(int i=0;i<12;i++) {
            float a=t*.8f+i*PI2/12.0f;
            Vector2 mote={lamp.x+cosf(a)*(34.0f+5.0f*sinf(t*2.0f+i)),lamp.y+sinf(a)*(28.0f+4.0f*cosf(t*2.0f+i))};
            DrawCircleV(mote,1.6f,Fade((Color){255,232,173,255},.55f));
        }
        DrawRectangleLinesEx((Rectangle){scene.x-4,scene.y-4,scene.width+8,scene.height+8},3.0f,Fade(C_GOLD,.82f));
        DrawRectangleLinesEx((Rectangle){scene.x+2,scene.y+2,scene.width-4,scene.height-4},1.0f,Fade((Color){255,246,217,255},.32f));
        // Place the traveller and companions on the path in the illustrated scene.
        DrawPlayer((Vector2){420,620},t,false,3);
        if(g->lumi.owned)DrawLumi((Vector2){485,620},t,0);
        if(g->splash.owned)DrawMoatail((Vector2){550,620},t);
        if(g->spark.owned)DrawGecko((Vector2){615,620},t);
    } else {
        DrawLighthouseBackdrop(t,true);
        DrawUIText("灯塔重新亮起",510,325,30,(Color){255,238,198,255});
    }
    // Side panels keep the CG unobstructed and give the ending a deliberate editorial layout.
    DrawRectangle(0,0,286,SH,(Color){8,20,36,242});
    DrawRectangle(SW-286,0,286,SH,(Color){8,20,36,242});
    DrawLineEx((Vector2){286,28},(Vector2){286,SH-28},2.0f,Fade(C_GOLD,.48f));
    DrawLineEx((Vector2){SW-286,28},(Vector2){SW-286,SH-28},2.0f,Fade(C_GOLD,.48f));
    DrawUIText("第一章",34,76,17,(Color){224,183,105,255});
    DrawUIText("失落的灯塔",34,112,29,(Color){255,242,213,255});
    DrawLineEx((Vector2){34,160},(Vector2){246,160},1.5f,Fade(C_GOLD,.55f));
    DrawUITextWrapped("沉睡的灯火终于苏醒。",34,198,214,18,2,(Color){239,232,209,255});
    DrawUITextWrapped("伙伴们的光，照亮了回家的路。",34,265,214,16,3,(Color){195,211,210,255});
    DrawUIText("同行伙伴",SW-250,76,17,(Color){224,183,105,255});
    int row=0;
    if(g->lumi.owned){DrawIllustratedPortrait(TEX_LUMI_ILLUSTRATION,(Rectangle){SW-248,158,48,64});DrawUIText("苔灯灵",SW-192,174,17,(Color){244,239,217,255});row++;}
    if(g->splash.owned){DrawIllustratedPortrait(TEX_MOATAIL_ILLUSTRATION,(Rectangle){SW-248,158+row*76,48,64});DrawUIText("沫尾兽",SW-192,174+row*76,17,(Color){244,239,217,255});row++;}
    if(g->spark.owned){DrawIllustratedPortrait(TEX_GECKO_ILLUSTRATION,(Rectangle){SW-248,158+row*76,48,64});DrawUIText("鸣晶蜥",SW-192,174+row*76,17,(Color){244,239,217,255});row++;}
    DrawLineEx((Vector2){SW-250,420},(Vector2){SW-34,420},1.5f,Fade(C_GOLD,.42f));
    DrawUITextWrapped("一座灯塔，一段旅程。",SW-250,450,216,16,2,(Color){220,226,207,255});
    DrawUITextWrapped("新的故事还在岛屿深处等待。",SW-250,506,216,15,3,(Color){188,207,203,255});
    DrawRectangle(0,SH-72,SW,72,Fade((Color){5,15,27,255},.94f));
    if(t<5.0f)DrawUIText("灯塔的光正在越过海面……",478,SH-47,18,(Color){255,232,181,255});
    else DrawUIText("第一章完成   ·   Enter 返回标题   ·   R 再次探索",390,SH-47,18,(Color){255,232,181,255});
    if(fadeIn<1.0f)DrawRectangle(0,0,SW,SH,Fade(BLACK,1.0f-fadeIn));
}
static int Base64Value(unsigned char c) {
    if(c>='A'&&c<='Z')return c-'A';
    if(c>='a'&&c<='z')return c-'a'+26;
    if(c>='0'&&c<='9')return c-'0'+52;
    if(c=='+')return 62;
    if(c=='/')return 63;
    return -1;
}
static unsigned char *DecodeBase64(const char *encoded,int *outSize) {
    size_t n=strlen(encoded);
    unsigned char *out=(unsigned char*)malloc(n*3/4+4);
    if(!out)return NULL;
    unsigned int acc=0;int bits=0,count=0;
    for(size_t i=0;i<n;i++) {
        unsigned char c=(unsigned char)encoded[i];
        if(c=='=')break;
        int v=Base64Value(c);if(v<0)continue;
        acc=(acc<<6)|(unsigned int)v;bits+=6;
        if(bits>=8){bits-=8;out[count++]=(unsigned char)((acc>>bits)&255u);}
    }
    *outSize=count;return out;
}
static void LoadEndingCG(void) {
    char *encoded=LoadFileText("assets/cinematics/lighthouse_final_scene.jpg.b64");
    if(!encoded){TraceLog(LOG_WARNING,"Mist Isle: lighthouse CG asset missing; using fallback ending.");return;}
    int byteCount=0;unsigned char *bytes=DecodeBase64(encoded,&byteCount);
    if(bytes&&byteCount>0) {
        Image image=LoadImageFromMemory(".jpg",bytes,byteCount);
        if(image.data) {
            if(image.width==448&&image.height==448) {
                TEX_ENDING_CG=LoadTextureFromImage(image);
                if(TEX_ENDING_CG.id)SetTextureFilter(TEX_ENDING_CG,TEXTURE_FILTER_POINT);
            } else TraceLog(LOG_WARNING,"Mist Isle: lighthouse CG illustration size mismatch (%dx%d).",image.width,image.height);
            UnloadImage(image);
        }
    }
    free(bytes);UnloadFileText(encoded);
    if(!TEX_ENDING_CG.id)TraceLog(LOG_WARNING,"Mist Isle: lighthouse CG failed to load; using fallback ending.");
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
        "assets/environment/topdown_terrain_atlas_v1.png",
        "assets/environment/flora_props_atlas_128.png",
        "assets/environment/cliffs_fences_atlas_128.png",
        "assets/environment/structures_atlas_128.png",
        "assets/environment/original_props_16_v1.png",
        "assets/environment/kenney_buildings_atlas.png",
        "assets/characters/png/camp_keeper.png",
        "assets/tiles/png/island_iso_tiles.png",
        "assets/tiles/png/environment_props.png",
        "assets/characters/portraits/heroine_portrait.png",
        "assets/characters/portraits/watcher_portrait.png",
        "assets/characters/portraits/guardian_portrait.png",
        "assets/characters/portraits/moss_lumi_portrait.png",
        "assets/characters/portraits/moatail_portrait.png",
        "assets/characters/portraits/crystal_gecko_portrait.png",
        "assets/characters/hero/heroine_approved_8x4.png",
        "assets/characters/boss/lighthouse_guardian_4x2.png",
        "assets/characters/portraits/character_portraits_atlas.jpg",
        "assets/characters/png/heroine_world_4x4_64x80.png",
        "assets/characters/portraits/watcher_world_cutout.png",
        "assets/environment/glowglade_cottage_cutout.png"
    };
    Texture2D *targets[]={&TEX_HERO_WALK,&TEX_LUMI_PIXEL,&TEX_MOATAIL_PIXEL,&TEX_GECKO_PIXEL,&TEX_LEAFLING,&TEX_GROVE_MOTE,&TEX_ENV_PROPS,&TEX_AREA_PROPS,&TEX_TOPDOWN_TILES,&TEX_FLORA_ATLAS,&TEX_CLIFF_ATLAS,&TEX_STRUCTURE_ATLAS,&TEX_ORIGINAL_PROPS,&TEX_KENNEY_BUILDINGS,&TEX_KEEPER,&TEX_ISO_TILES,&TEX_PROPS,&TEX_HERO_PORTRAIT,&TEX_WATCHER_PORTRAIT,&TEX_GUARDIAN_PORTRAIT,&TEX_LUMI_ILLUSTRATION,&TEX_MOATAIL_ILLUSTRATION,&TEX_GECKO_ILLUSTRATION,&TEX_HEROINE_APPROVED,&TEX_GUARDIAN_APPROVED,&TEX_CHARACTER_PORTRAITS,&TEX_HEROINE_WORLD,&TEX_WATCHER_WORLD,&TEX_GROVE_COTTAGE};
    for(int i=0;i<29;i++) {
        if(FileExists(paths[i])) {
            *targets[i]=LoadTexture(paths[i]);
            if(targets[i]->id) {
                bool pixel=i<17||i==23||i==24||i==26||i==27;
                SetTextureFilter(*targets[i],pixel?TEXTURE_FILTER_POINT:TEXTURE_FILTER_BILINEAR);
                TraceLog(LOG_INFO,"Mist Isle asset loaded: %s (%dx%d)",paths[i],targets[i]->width,targets[i]->height);
            } else TraceLog(LOG_WARNING,"Mist Isle asset failed to load: %s",paths[i]);
        } else TraceLog(LOG_WARNING,"Mist Isle asset missing: %s",paths[i]);
    }
    if(FileExists("assets/characters/hero/heroine_pixel_4x4_64x80.png")) {
        TEX_HEROINE_PIXEL=LoadTexture("assets/characters/hero/heroine_pixel_4x4_64x80.png");
        if(TEX_HEROINE_PIXEL.id)SetTextureFilter(TEX_HEROINE_PIXEL,TEXTURE_FILTER_POINT);
        else TraceLog(LOG_ERROR,"Mist Isle: approved heroine pixel sheet failed to load.");
    } else TraceLog(LOG_ERROR,"Mist Isle: approved heroine pixel sheet missing; refusing silent generated-hero fallback.");
    if(FileExists("assets/characters/npc/watcher_pixel_4x4_64x80.png")) {
        TEX_WATCHER_PIXEL=LoadTexture("assets/characters/npc/watcher_pixel_4x4_64x80.png");
        if(TEX_WATCHER_PIXEL.id) {
            SetTextureFilter(TEX_WATCHER_PIXEL,TEXTURE_FILTER_POINT);
            TraceLog(LOG_INFO,"Mist Isle: native pixel Watcher loaded (%dx%d).",TEX_WATCHER_PIXEL.width,TEX_WATCHER_PIXEL.height);
        } else TraceLog(LOG_ERROR,"Mist Isle: Watcher sheet exists but texture creation failed.");
    } else TraceLog(LOG_ERROR,"Mist Isle: Watcher pixel sheet missing; run the asset generator.");
    if(FileExists("assets/environment/coastal_keeper_house.png")) {
        TEX_COASTAL_HOUSE=LoadTexture("assets/environment/coastal_keeper_house.png");
        if(TEX_COASTAL_HOUSE.id)SetTextureFilter(TEX_COASTAL_HOUSE,TEXTURE_FILTER_POINT);
    } else TraceLog(LOG_ERROR,"Mist Isle: approved coastal keeper house asset missing.");
    /* Explicitly reload and validate the dedicated 4x2 pixel Boss sheet.
       Do not silently rely on the generic texture-array index for this critical asset. */
    if(TEX_GUARDIAN_APPROVED.id)UnloadTexture(TEX_GUARDIAN_APPROVED);
    TEX_GUARDIAN_APPROVED=(Texture2D){0};
    const char *bossPixelPath="assets/characters/boss/lighthouse_guardian_4x2.png";
    if(FileExists(bossPixelPath)) {
        TEX_GUARDIAN_APPROVED=LoadTexture(bossPixelPath);
        if(TEX_GUARDIAN_APPROVED.id) {
            SetTextureFilter(TEX_GUARDIAN_APPROVED,TEXTURE_FILTER_POINT);
            if(TEX_GUARDIAN_APPROVED.width==256&&TEX_GUARDIAN_APPROVED.height==160) {
                TraceLog(LOG_INFO,"Mist Isle: dedicated 4x2 pixel Boss sheet loaded (256x160; 64x80 cells).");
            } else {
                TraceLog(LOG_ERROR,"Mist Isle: Boss sheet dimensions are %dx%d; expected 256x160. Disabling invalid Boss texture.",TEX_GUARDIAN_APPROVED.width,TEX_GUARDIAN_APPROVED.height);
                UnloadTexture(TEX_GUARDIAN_APPROVED);
                TEX_GUARDIAN_APPROVED=(Texture2D){0};
            }
        } else TraceLog(LOG_ERROR,"Mist Isle: Boss sheet exists but LoadTexture failed.");
    } else TraceLog(LOG_ERROR,"Mist Isle: Boss pixel sheet missing at %s.",bossPixelPath);
    TEX_GROUND_CACHE=LoadRenderTexture((int)WORLD_PIXEL_W,(int)WORLD_PIXEL_H);
    if(TEX_GROUND_CACHE.id)SetTextureFilter(TEX_GROUND_CACHE.texture,TEXTURE_FILTER_POINT);
    CACHED_GROUND_AREA=-1;
    LoadEndingCG();
}
void RenderClose(void) {
    if(TEX_HEROINE_PIXEL.id)UnloadTexture(TEX_HEROINE_PIXEL);
    if(TEX_WATCHER_PIXEL.id)UnloadTexture(TEX_WATCHER_PIXEL);
    if(TEX_COASTAL_HOUSE.id)UnloadTexture(TEX_COASTAL_HOUSE);
    if(TEX_GROUND_CACHE.id)UnloadRenderTexture(TEX_GROUND_CACHE);
    TEX_GROUND_CACHE=(RenderTexture2D){0};
    CACHED_GROUND_AREA=-1;
    UnloadUIFont();
    Texture2D *textures[]={&TEX_HERO_WALK,&TEX_LUMI_PIXEL,&TEX_MOATAIL_PIXEL,&TEX_GECKO_PIXEL,&TEX_LEAFLING,&TEX_GROVE_MOTE,&TEX_ENV_PROPS,&TEX_AREA_PROPS,&TEX_TOPDOWN_TILES,&TEX_FLORA_ATLAS,&TEX_CLIFF_ATLAS,&TEX_STRUCTURE_ATLAS,&TEX_ORIGINAL_PROPS,&TEX_KENNEY_BUILDINGS,&TEX_KEEPER,&TEX_ISO_TILES,&TEX_PROPS,&TEX_HERO_PORTRAIT,&TEX_WATCHER_PORTRAIT,&TEX_GUARDIAN_PORTRAIT,&TEX_LUMI_ILLUSTRATION,&TEX_MOATAIL_ILLUSTRATION,&TEX_GECKO_ILLUSTRATION,&TEX_HEROINE_APPROVED,&TEX_GUARDIAN_APPROVED,&TEX_CHARACTER_PORTRAITS,&TEX_HEROINE_WORLD,&TEX_WATCHER_WORLD,&TEX_GROVE_COTTAGE};
    for(int i=0;i<29;i++)if(textures[i]->id)UnloadTexture(*textures[i]);
    if(TEX_ENDING_CG.id)UnloadTexture(TEX_ENDING_CG);
}
void GameRender(Game *g) {
    if(g->scene==SCENE_TITLE)RenderTitle(g);
    else if(g->scene==SCENE_BATTLE)RenderBattle(g);
    else if(g->scene==SCENE_END)RenderEnd(g);
    else {RenderWorld(g);RenderHUD(g);if(g->scene==SCENE_DIALOGUE)RenderDialogue(g);if(g->journalOpen)RenderJournal(g);if(g->inventoryOpen)RenderInventory(g);if(g->mapOpen)RenderMap(g);}
}