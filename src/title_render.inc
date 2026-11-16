/* Exact user-selected cover; interactive UI is drawn over, never baked into
 * a replacement illustration. The image is loaded once, not every frame. */
static Texture2D TEX_TITLE_COVER={0};
static void TitleCentered(const char *text,float center,float y,int size,Color color) {
    Vector2 dim=UITextMeasure(UI_FONT,text,(float)size,1);
    DrawUIText(text,(int)(center-dim.x*.5f),(int)y,size,color);
}
static void TitleOutline(Rectangle r,float width,Color color) {
    float bevel=18;
    Vector2 p[7]={{r.x+bevel,r.y},{r.x+r.width-bevel,r.y},
        {r.x+r.width,r.y+r.height/2},{r.x+r.width-bevel,r.y+r.height},
        {r.x+bevel,r.y+r.height},{r.x,r.y+r.height/2},{r.x+bevel,r.y}};
    for(int i=0;i<6;i++)DrawLineEx(p[i],p[i+1],width,color);
}
static void TitleControl(Rectangle r,const char *text,bool selected) {
    DrawRectangleRounded(r,.22f,12,selected?(Color){240,219,171,255}:(Color){28,44,70,248});
    DrawRectangleRoundedLinesEx(r,.22f,12,1.5f,selected?(Color){255,237,191,255}:(Color){129,154,188,220});
    TitleCentered(text,r.x+r.width*.5f,r.y+12,20,selected?(Color){37,39,50,255}:(Color){227,237,248,255});
}
static void RenderTitle(Game *g) {
    TitleMenu *t=&g->title;
    Color gold={255,224,160,255},paper={238,243,255,255};
    if(TEX_TITLE_COVER.id) {
        DrawTexturePro(TEX_TITLE_COVER,(Rectangle){0,0,TEX_TITLE_COVER.width,TEX_TITLE_COVER.height},
            (Rectangle){0,0,SW,SH},(Vector2){0},0,WHITE);
    } else {
        /* Explicit missing-asset fallback; functional menu, never silently
         * substitute another cover or any of the ending CGs. */
        DrawRectangleGradientV(0,0,SW,SH,(Color){16,29,58,255},(Color){42,62,88,255});
        TitleCentered("雾屿回响",684,105,62,gold);
        TitleCentered("Mist Isle",684,183,24,paper);
        const char *names[]={"开始冒险","继续旅程","设置","退出"};
        for(int i=0;i<4;i++)TitleControl(TitleButtonRect(i),names[i],t->selected==i);
        TitleCentered("封面文件未安装",684,555,18,gold);
    }
    /* The gold primary-action styling in the approved artwork is preserved.
     * A separate moving diamond + outline unambiguously marks input focus. */
    if(t->page==TITLE_MAIN) {
        Rectangle selected=TitleButtonRect(t->selected);
        float pulse=.5f+.5f*sinf(g->time*3);
        TitleOutline(selected,6,Fade(gold,.10f+.08f*pulse));
        TitleOutline(selected,1.5f,Fade(paper,.72f+.18f*pulse));
        float x=selected.x-18,y=selected.y+selected.height*.5f;
        DrawPoly((Vector2){x,y},4,6,0,gold);
        if(!t->hasSave) {
            Rectangle disabled=TitleButtonRect(1);
            DrawRectangleRounded((Rectangle){disabled.x+16,disabled.y+4,disabled.width-32,disabled.height-8},.15f,8,Fade((Color){20,34,58,255},.42f));
        }
        const char *hint=t->selected==1&&!t->hasSave?"暂无存档 · 先开始冒险":
            t->selected==1?"继续上次保存的旅程 · F9":
            t->selected==2?"声音、字幕与显示设置":
            t->selected==3?"结束本次游戏":"开始新的海岛旅程";
        DrawRectangleRounded((Rectangle){448,563,474,38},.2f,8,Fade((Color){12,27,49,255},.68f));
        TitleCentered(hint,685,573,16,paper);
    } else {
        DrawRectangle(0,0,SW,SH,Fade((Color){5,12,27,255},.70f));
        Rectangle panel=t->page==TITLE_SETTINGS?(Rectangle){388,185,504,430}:(Rectangle){358,236,564,266};
        DrawRectangleRounded(panel,.06f,16,(Color){17,32,55,248});
        DrawRectangleRoundedLinesEx(panel,.06f,16,1.7f,Fade(gold,.8f));
        if(t->page==TITLE_SETTINGS) {
            TitleCentered("设置",640,215,32,gold);
            const char *sound=AudioIsMuted()?"音乐与音效   关闭":"音乐与音效   开启";
            const char *sub=AudioSubtitlesEnabled()?"歌词字幕   开启":"歌词字幕   关闭";
            const char *screen=IsWindowState(FLAG_BORDERLESS_WINDOWED_MODE)?"显示模式   无边框":"显示模式   窗口";
            const char *labels[4]={sound,sub,screen,"返回"};
            for(int i=0;i<4;i++)TitleControl(TitleSettingsRect(i),labels[i],i==t->selected);
            TitleCentered("F6 声音   F8 字幕   Esc 返回",640,550,15,(Color){175,199,222,255});
            TitleCentered("设置在本次运行中生效",640,576,13,(Color){151,175,198,255});
        } else {
            bool fresh=t->page==TITLE_NEW_CONFIRM;
            TitleCentered(fresh?"开始新的冒险？":"退出游戏？",640,266,30,gold);
            TitleCentered(fresh?"已有旅程存档，开始前将自动保留一份备份。":"确定结束本次游戏并关闭窗口？",640,328,17,paper);
            TitleCentered(fresh?"原存档不会直接删除。":"尚未保存的进度不会自动保存。",640,360,16,(Color){175,199,222,255});
            TitleControl(TitleConfirmRect(0),"返回",t->selected==0);
            TitleControl(TitleConfirmRect(1),fresh?"备份并开始":"退出游戏",t->selected==1);
        }
    }
    if(t->noticeTime>0) {
        DrawRectangleRounded((Rectangle){330,620,620,42},.18f,8,(Color){13,28,48,238});
        TitleCentered(t->notice,640,632,17,gold);
    }
    DrawRectangleGradientV(0,672,SW,48,Fade((Color){9,20,37,255},0),Fade((Color){9,20,37,255},.75f));
    TitleCentered("↑ ↓ 选择   Enter 确认   鼠标点击",680,690,14,(Color){212,226,244,255});
    DrawUIText("v" MISTISLE_VERSION " · TITLE-01",25,691,12,(Color){183,203,226,255});
}
