/* Included by render.c to share the approved textures and native-pixel text.
 * No scene/music ownership, persistent state writes, or asset substitution here. */
#if defined(MISTISLE_PI_PROFILE)
#define BATTLE_MOTES 18
#else
#define BATTLE_MOTES 36
#endif
static float BattleEase(float t) { t=Clamp(t,0,1);return t*t*(3-2*t); }
static float BattleAttackReach(const BattleState *b) {
    if(b->clock<b->impactAt)return BattleEase((b->clock-.22f)/fmaxf(.1f,b->impactAt-.30f));
    return 1-BattleEase((b->clock-b->impactAt-.14f)/fmaxf(.1f,b->duration-b->impactAt-.14f));
}
static Color BattleColor(BattleAction a) {
    return a==ACTION_WATER?(Color){104,225,251,255}:a==ACTION_SPARK?(Color){184,157,255,255}:
           a==ACTION_GLOW?(Color){220,247,156,255}:a==ACTION_RESONANCE?(Color){255,119,86,255}:(Color){255,215,158,255};
}
static const char *BattleSkill(BattleAction a) {
    const char *names[]={"待命","星光治愈","泡泡水流","晶弧放电","守卫出击","共鸣重击"};
    return names[(int)a>=0&&(int)a<6?a:0];
}
static void BattleGlow(Vector2 p,float radius,Color c,float alpha) {
    DrawCircleGradient((int)p.x,(int)p.y,radius,Fade(c,alpha),Fade(c,0));
}
static void BattleStar(Vector2 p,float size,Color c) {
    DrawLineEx((Vector2){p.x-size,p.y},(Vector2){p.x+size,p.y},2,c);
    DrawLineEx((Vector2){p.x,p.y-size},(Vector2){p.x,p.y+size},2,c);
    DrawCircleV(p,2.5f,c);
}
static void BattleActor(Texture2D texture,Rectangle src,Vector2 foot,float width,float height,float tilt,Color tint) {
    if(!texture.id)return;
    DrawTexturePro(texture,src,(Rectangle){foot.x,foot.y,width,height},(Vector2){width*.5f,height},tilt,tint);
}
static void BattleBuddy(int id,Vector2 foot,float t,float reach,bool hurt,Color tint) {
    Texture2D tex=id==0?TEX_LUMI_PIXEL:id==1?TEX_MOATAIL_PIXEL:TEX_GECKO_PIXEL;
    float cw=tex.width/4.0f,ch=(float)tex.height;
    int frame=(int)(t*(reach>0?12:4))%4;
    float squash=1+.12f*sinf(reach*PI2);
    BattleActor(tex,(Rectangle){frame*cw,0,cw,ch},foot,94*squash,102/squash,hurt?-9:reach*-11,tint);
}
static void BattleStage(const Game *g) {
    bool boss=g->enemies[g->battleEnemy].type==ENEMY_BOSS;
    Color high=boss?(Color){12,21,36,255}:(Color){23,49,52,255};
    Color low=boss?(Color){44,54,72,255}:(Color){64,104,86,255};
    DrawRectangleGradientV(0,0,SW,SH,high,low);
    /* Authored stone/grass tiles replace the former flat green oval arena. */
    if(TEX_TOPDOWN_TILES.id)for(int y=0;y<5;y++)for(int x=0;x<16;x++) {
        int tile=(boss?24:0)+(x+y*3)%4;
        Rectangle src={(float)((tile%4)*56),(float)((tile/4)*56),56,56};
        DrawTexturePro(TEX_TOPDOWN_TILES,src,(Rectangle){x*84.0f,312+y*48.0f,84,48},(Vector2){0},0,
            boss?(Color){139,159,186,255}:(Color){173,204,182,255});
    }
    DrawRectangleGradientV(0,280,SW,84,high,Fade(high,0));
    if(boss) {
        DrawGeneratedMapAsset(663,(Vector2){82,400},.95f);
        DrawGeneratedMapAsset(665,(Vector2){1190,411},.95f);
        DrawGeneratedMapAsset(660,(Vector2){210,335},.48f);
        DrawGeneratedMapAsset(661,(Vector2){1100,340},.46f);
        DrawGeneratedMapAsset(675,(Vector2){150,437},.44f);
        DrawGeneratedMapAsset(675,(Vector2){1154,451},.44f);
        for(int i=0;i<2;i++) {
            Vector2 flame={i?1154:150,i?374:360};
            BattleGlow(flame,64,(Color){80,183,255,255},.18f+.04f*sinf(g->time*4));
        }
    } else {
        DrawGeneratedMapAsset(622,(Vector2){90,393},.9f);
        DrawGeneratedMapAsset(624,(Vector2){1200,408},.9f);
        DrawGeneratedMapAsset(625,(Vector2){190,356},.55f);
        DrawGeneratedMapAsset(627,(Vector2){1100,359},.55f);
    }
    for(int i=0;i<BATTLE_MOTES;i++) {
        float x=fmodf(i*137.31f+g->time*(3+i%4),SW);
        float y=290+fmodf(i*41.4f-g->time*7+10000,210);
        DrawCircleV((Vector2){x,y},i%3==0?2:1,Fade(boss?C_BLUE:C_GOLD,.32f));
    }
    DrawRectangleGradientV(0,478,SW,70,Fade(high,0),high);
}
static void BattleBolt(Vector2 from,Vector2 to,float time,Color c,float alpha,int seed) {
    Vector2 last=from;
    for(int i=1;i<=12;i++) {
        float u=i/12.0f;
        Vector2 p=Vector2Lerp(from,to,u);
        if(i!=12)p.y+=sinf((float)i*2.23f+floorf(time*28)*1.7f+seed)*22;
        DrawLineEx(last,p,8,Fade(c,alpha*.20f));
        DrawLineEx(last,p,3.2f,Fade(c,alpha));
        DrawLineEx(last,p,1.2f,Fade(WHITE,alpha*.90f));last=p;
    }
}
static void BattleBurst(Vector2 center,Color c,float age,int salt) {
    if(age<0||age>.55f)return;
    float u=age/.55f,alpha=(1-u)*(1-u);
    DrawRing(center,10+u*100,14+u*105,0,360,32,Fade(c,alpha*.75f));
    for(int i=0;i<BATTLE_MOTES;i++) {
        float angle=i*2.39996f+salt;
        float radius=18+u*(55+(i%7)*11);
        Vector2 p={center.x+cosf(angle)*radius,center.y+sinf(angle)*radius*.65f+u*u*45};
        Vector2 tail={p.x-cosf(angle)*12*(1-u),p.y-sinf(angle)*9*(1-u)};
        DrawLineEx(tail,p,i%3==0?3:1.5f,Fade(i%3?c:WHITE,alpha));
    }
    BattleGlow(center,100,c,alpha*.30f);
}
static void BattleBar(Rectangle rect,float hp,float maxHp,float trail,Color c) {
    float value=Clamp(hp/fmaxf(1,maxHp),0,1),old=Clamp(trail/fmaxf(1,maxHp),0,1);
    DrawRectangleRounded(rect,.6f,8,(Color){24,34,47,255});
    if(old>value)DrawRectangleRounded((Rectangle){rect.x,rect.y,rect.width*old,rect.height},.4f,8,(Color){244,209,161,255});
    if(value>0)DrawRectangleRounded((Rectangle){rect.x,rect.y,rect.width*value,rect.height},.4f,8,c);
}
static Vector2 BattlePetFoot(int id,int active) {
    if(active<0||active>2) {
        const Vector2 resting[3]={{230,407},{448,476},{285,500}};
        return resting[id];
    }
    if(id==active)return (Vector2){448,476};
    int rear=0;
    for(int i=0;i<id;i++)if(i!=active)rear++;
    return (Vector2){rear?285:230,rear?500:407};
}
static void RenderBattle(Game *g) {
    const BattleState *b=&g->battle;const Enemy *e=&g->enemies[g->battleEnemy];
    bool boss=e->type==ENEMY_BOSS,player=b->phase==BATTLE_PLAYER,enemy=b->phase==BATTLE_ENEMY;
    bool action=player||enemy;
    float age=b->clock-b->impactAt;
    float impact=action&&b->impacted?1-Clamp(age/.35f,0,1):0;
    float reach=action?BattleAttackReach(b):0;
    int active=player?b->actor:enemy?b->defender:g->selectedBuddy;
    const Buddy *pets[3]={&g->lumi,&g->splash,&g->spark};
    Vector2 petFeet[3];
    for(int id=0;id<3;id++)petFeet[id]=BattlePetFoot(id,active);
    Vector2 buddy=active>=0?petFeet[active]:(Vector2){448,476},foe={972,470};
    float healRise=player&&b->action==ACTION_GLOW?sinf(Clamp(b->clock/b->duration,0,1)*PI)*28:0;
    if(player) {
        buddy.x+=reach*(b->action==ACTION_WATER?86:32);buddy.y-=healRise+reach*10;
        if(b->impacted)foe.x+=impact*23;
    }
    if(enemy) {
        if(b->action==ACTION_ENEMY)foe.x-=reach*384;
        else foe.y-=!b->impacted?reach*28:0;
        if(b->impacted)buddy.x-=impact*27;
    }
    if(active>=0)petFeet[active]=buddy;
    Vector2 caster=player?buddy:foe;
    Vector2 target=player?foe:buddy;
    caster.y-=65;target.y-=70;
    Color skill=BattleColor(b->action);
    float shake=impact*(b->action==ACTION_RESONANCE||b->combo?7:3);
    rlPushMatrix();
    rlTranslatef(sinf(b->clock*97)*shake,cosf(b->clock*83)*shake*.55f,0);
    float zoom=1+impact*.018f;
    rlTranslatef(SW*.5f,SH*.55f,0);rlScalef(zoom,zoom,1);rlTranslatef(-SW*.5f,-SH*.55f,0);
    BattleStage(g);
    for(int id=0;id<3;id++)if(pets[id]->owned)
        DrawEllipse((int)petFeet[id].x,(int)petFeet[id].y+7,42,11,Fade(BLACK,.24f));
    DrawEllipse((int)foe.x,478,boss?80:50,15,Fade(BLACK,.30f));
    if(action&&!b->impacted) {
        float charge=Clamp(b->clock/(b->impactAt*.55f),0,1);
        BattleGlow(caster,40+charge*40,skill,.15f+charge*.18f);
        for(int i=0;i<BATTLE_MOTES/2;i++) {
            float a=i*2.39996f+b->clock*4;
            float r=(1-charge)*55+18;
            BattleStar((Vector2){caster.x+cosf(a)*r,caster.y+sinf(a)*r*.75f},2,Fade(skill,charge*.8f));
        }
    }
    bool hurtBuddy=enemy&&b->impacted&&b->defender>=0;
    Color allyTint=impact>.12f&&((int)(age*30)%2)==0?(Color){255,164,147,255}:WHITE;
    for(int id=0;id<3;id++)if(pets[id]->owned) {
        bool hurt=id==active&&hurtBuddy;
        Color tint=pets[id]->hp>0?(hurt?allyTint:WHITE):(Color){140,153,170,130};
        BattleBuddy(id,petFeet[id],id==active&&action?b->clock:g->time,
                    id==active&&player?reach:0,hurt,tint);
    }
    float fade=b->phase==BATTLE_DEFEAT?1-Clamp(b->clock/.70f,0,1):1;
    if(b->phase==BATTLE_PREVIEW_END&&e->hp<=0)fade=0;
    Color foeTint=player&&impact>.12f&&((int)(age*30)%2)==0?(Color){255,155,132,255}:WHITE;
    foeTint=Fade(foeTint,fade);
    if(boss) {
        int bossFrame=enemy?(int)(b->clock*10)%4:(int)(g->time*2)%4;
        BattleActor(TEX_GUARDIAN_APPROVED,(Rectangle){bossFrame*64.0f,enemy?80:0,64,80},foe,174,216,
            player?impact*8:(enemy&&b->action==ACTION_ENEMY?-reach*9:0),foeTint);
    } else {
        int f=(int)(g->time*5)%4;
        BattleActor(TEX_GROVE_MOTE,(Rectangle){f*32.0f,0,32,32},foe,132,138,player?impact*10:0,foeTint);
    }
    if(g->enemySoaked)for(int i=0;i<5;i++) {
        float y=foe.y-130+fmodf(g->time*32+i*24,120);
        DrawLineEx((Vector2){foe.x-38+i*19.0f,y},(Vector2){foe.x-40+i*19.0f,y+9},2,Fade(C_BLUE,.75f));
    }
    if(action) {
        float travel=Clamp((b->clock-(b->impactAt-.42f))/.42f,0,1);
        if(player&&b->action==ACTION_WATER&&b->clock>=b->impactAt-.42f&&!b->impacted) {
            Vector2 from={520,407};
            for(int i=6;i>=0;i--) {
                float u=Clamp(travel-i*.025f,0,1);
                Vector2 p=Vector2Lerp(from,target,u);p.y-=sinf(u*PI)*48;
                float radius=i?9+(6-i)*1.1f:23;
                BattleGlow(p,radius*2,skill,.18f);
                DrawCircleV(p,radius,Fade(skill,i?.40f:.8f));
                DrawCircleLinesV(p,radius,Fade(WHITE,.75f));
                DrawCircleV((Vector2){p.x-6,p.y-7},3,Fade(WHITE,.8f));
            }
        } else if(player&&b->action==ACTION_SPARK&&b->clock>b->impactAt-.12f&&age<.28f) {
            BattleBolt((Vector2){buddy.x+35,buddy.y-65},target,b->clock,skill,1,0);
            if(b->combo) {
                BattleBolt((Vector2){target.x-130,target.y-80},target,b->clock,C_BLUE,.9f,7);
                BattleBolt((Vector2){target.x+110,target.y-90},target,b->clock,C_BLUE,.9f,13);
            }
        } else if(player&&b->action==ACTION_GLOW) {
            float u=Clamp(b->clock/b->impactAt,0,1);
            for(int id=0;id<3;id++)if(pets[id]->owned) {
                Vector2 heart={petFeet[id].x,petFeet[id].y-54};
                BattleGlow(heart,76,skill,(1-fabsf(u-.6f))*.22f);
                DrawRing(heart,31+u*13,33+u*13,0,360,32,Fade(skill,.65f*(1-Clamp(age/.6f,0,1))));
                for(int i=0;i<6;i++) {
                    float a=b->clock*3+i*PI2/6;
                    BattleStar((Vector2){heart.x+cosf(a)*43,heart.y+sinf(a)*30-b->clock*6},4,Fade(skill,.7f));
                }
            }
            if(!b->impacted&&travel>0) {
                Vector2 p=Vector2Lerp((Vector2){buddy.x+20,buddy.y-55},target,travel);
                BattleGlow(p,32,skill,.4f);BattleStar(p,10,WHITE);
            }
        } else if(enemy&&b->action==ACTION_RESONANCE&&b->impacted) {
            float u=Clamp(age/.35f,0,1);
            DrawEllipseLines((int)foe.x,477,70+u*640,18+u*65,Fade(skill,1-u));
            for(int i=0;i<7;i++)DrawLineEx((Vector2){target.x-80+i*23.0f,486},
                (Vector2){target.x-70+i*23.0f,400-age*120},3,Fade(skill,(1-u)*.65f));
        } else if(enemy&&b->impacted) {
            DrawRing(target,28,65,40,250,24,Fade(skill,impact*.7f));
        }
        if(b->impacted)BattleBurst(target,skill,age,b->action);
        if(enemy&&!b->impacted) {
            /* Contracting timing circle reaches the fixed target at impact.
             * It is a timing aid, never an area attack or movement mechanic. */
            float left=Clamp((b->impactAt-b->clock)/.7f,0,1);
            DrawCircleLinesV(target,42,Fade(C_BLUE,.75f));
            DrawCircleLinesV(target,42+left*80,Fade(b->guarded?C_BLUE:skill,.9f));
            if(b->guarded)BattleGlow(target,70,C_BLUE,.25f);
        }
        if(enemy&&b->guarded&&b->impacted)DrawRing(target,45,51,-90,90,32,Fade(C_BLUE,impact));
    }
    if(b->phase==BATTLE_DEFEAT)BattleBurst((Vector2){foe.x,foe.y-100},C_GOLD,b->clock,3);
    rlPopMatrix();

    /* Native-pixel HUD stays still while only the battle stage shakes. */
    PanelDark((Rectangle){24,22,354,120});
    DrawUIText("宠物战斗 · IMPACT-02.2",43,35,17,(Color){247,224,179,255});
    DrawUIText(TextFormat("第 %d 回合  /  %s",g->battleRound,boss?"首领战":"遭遇战"),43,63,16,(Color){183,203,216,255});
    DrawUIText(TextFormat("可战斗宠物  %d / %d",BattleBuddyCount(g),
        g->lumi.owned+g->splash.owned+g->spark.owned),43,90,16,(Color){226,238,229,255});
    DrawUIText("星光治愈 · 全队宠物恢复",43,117,13,(Color){172,215,178,255});
    PanelDark((Rectangle){904,22,350,120});
    DrawUIText(boss?"灯塔守卫":"林地微光兽",925,35,24,(Color){255,229,185,255});
    DrawUIText(TextFormat("%s   %d / %d",boss?"核心耐久":"生命",(int)ceilf(b->enemyHp),(int)e->maxHp),925,76,17,(Color){206,222,232,255});
    BattleBar((Rectangle){925,111,306,13},b->enemyHp,e->maxHp,b->enemyTrail,(Color){243,154,101,255});
    if(boss) {
        /* The ticks mark 1/3 and 2/3 HP, independent of balance changes. */
        for(int i=1;i<=2;i++)DrawRectangle(925+i*102,109,2,17,(Color){23,32,43,235});
        DrawUIText(TextFormat("%.0f%%",100.0f*Clamp(e->hp/fmaxf(1,e->maxHp),0,1)),1189,43,16,(Color){206,222,232,255});
        int phase=BattleBossPhase(e->hp,e->maxHp);
        DrawRectangleRounded((Rectangle){790,178,464,65},.14f,6,(Color){12,24,37,236});
        DrawUIText(phase==3?"阶段三 · 最后的灯火":phase==2?"阶段二 · 共鸣增强":"阶段一 · 灯火苏醒",806,188,17,C_GOLD);
        DrawUIText(g->enemySoaked?"核心受潮：下回合可以触发导电":enemy&&b->action==ACTION_RESONANCE?"核心蓄能：共鸣重击即将命中":"核心稳定：可用潮湿技能打断",806,215,14,g->enemySoaked?C_BLUE:(Color){189,211,224,255});
    }
    if(action) {
        DrawRectangleRounded((Rectangle){34,181,640,59},.12f,6,(Color){9,20,33,225});
        DrawRectangle(34,192,4,36,skill);
        DrawUIText(BattleSkill(b->action),51,190,24,skill);
        const char *caption=b->impacted?(b->guarded?"精准格挡！":b->combo?"潮湿导电 · 共鸣命中！":"命中！"):
            (b->clock<b->impactAt*.5f?"蓄力":"出招");
        DrawUIText(caption,288,197,17,(Color){225,235,243,255});
    }
    if(action&&b->impacted&&age<.65f) {
        float alpha=1-Clamp(age/.65f,0,1),y=target.y-42-age*48;
        const char *n=TextFormat("-%.0f",b->damage);
        DrawUIText(n,(int)target.x-26,(int)y+2,34,(Color){10,16,26,(unsigned char)(alpha*255)});
        DrawUIText(n,(int)target.x-28,(int)y,34,Fade(enemy?(b->guarded?C_BLUE:C_PINK):C_GOLD,alpha));
        if(player&&b->action==ACTION_GLOW)for(int id=0;id<3;id++)if(b->healByBuddy[id]>0)
            DrawUIText(TextFormat("+%.0f",b->healByBuddy[id]),(int)petFeet[id].x-22,
                (int)(petFeet[id].y-126-age*32),28,Fade(C_GREEN,alpha));
    }
    if(enemy&&!b->impacted)DrawUIText(b->guardAttempted?(b->guarded?"格挡生效":"过早格挡"):
        "光圈接近中心时按 Space",350,492,19,b->guarded?C_BLUE:C_GOLD);
    DrawRectangle(0,537,SW,183,(Color){11,23,35,252});
    DrawLine(24,538,1256,538,(Color){116,144,158,255});
    const char *label=b->phase==BATTLE_CHOOSE?"选择出战伙伴 · 点击卡片后确认":
        b->phase==BATTLE_PREVIEW_END?"战斗体验完成 · R 重来 / Esc 退出":
        enemy?"敌方自动行动 · 无需按 Enter":b->phase==BATTLE_LOSS?"全队宠物体力耗尽 · 正在撤离":b->phase==BATTLE_DEFEAT?"战斗完成 · 灯火正在散去":"技能演出中 · 伤害在命中时生效";
    DrawUIText(label,33,547,16,(Color){195,217,232,255});
    /* All three identities keep their own card, HP and number key even at 0 HP. */
    for(int id=0;id<3;id++) {
        const Buddy *p=pets[id];bool owned=p->owned,able=owned&&p->hp>0;
        Rectangle card={32+id*254.0f,570,240,112};
        bool chosen=able&&id==g->selectedBuddy;
        Color tint=able?BattleColor((BattleAction)(id+1)):(Color){126,143,156,255};
        DrawRectangleRounded(card,.12f,8,chosen?(Color){39,61,74,255}:(Color){22,39,53,255});
        DrawRectangleRoundedLinesEx(card,.12f,8,chosen?2:1,chosen?tint:(Color){65,87,102,255});
        if(owned) {
            if(id==0)DrawIllustratedPortrait(TEX_LUMI_ILLUSTRATION,(Rectangle){card.x+10,card.y+14,51,68});
            if(id==1)DrawIllustratedPortrait(TEX_MOATAIL_ILLUSTRATION,(Rectangle){card.x+10,card.y+14,51,68});
            if(id==2)DrawIllustratedPortrait(TEX_GECKO_ILLUSTRATION,(Rectangle){card.x+10,card.y+14,51,68});
            if(!able)DrawRectangleRounded((Rectangle){card.x+9,card.y+12,54,72},.1f,6,Fade((Color){12,23,35,255},.60f));
        }
        DrawUIText(TextFormat("%d  %s",id+1,id==0?"苔灯灵":id==1?"沫尾兽":"鸣晶蜥"),(int)card.x+72,580,17,(Color){236,239,229,255});
        DrawUIText(able?BattleSkill((BattleAction)(id+1)):owned?"体力耗尽":"尚未加入",(int)card.x+72,604,16,tint);
        DrawUIText(id==0?"全队恢复 / 微光伤害":id==1?"潮湿 / 降低攻击":"导电 / 共鸣伤害",(int)card.x+72,628,12,(Color){175,200,216,255});
        if(owned) {
            DrawUIText(TextFormat("体力 %d / %d",(int)ceilf(b->buddyHp[id]),(int)p->maxHp),(int)card.x+72,646,12,tint);
            BattleBar((Rectangle){card.x+72,668,152,6},b->buddyHp[id],p->maxHp,b->buddyHp[id],tint);
        }
    }
    DrawUIText(g->enemySoaked?"潮湿仍在！下一招可用晶弧放电":"联动：泡泡水流 → 晶弧放电",838,578,15,(Color){173,211,227,255});
    Rectangle confirm={838,613,392,56};bool ready=b->phase==BATTLE_CHOOSE&&BattleBuddyCount(g)>0;
    DrawRectangleRounded(confirm,.2f,8,ready?(Color){212,185,129,255}:(Color){39,56,71,255});
    DrawUIText(ready?"Enter / 点击此处出招":b->phase==BATTLE_PREVIEW_END?"R 重来   Esc 退出":"行动进行中",899,630,20,ready?(Color){22,40,44,255}:(Color){167,192,211,255});
    DrawUIText(b->preview?"战斗体验 · 不读取或写入存档":"IMPACT-02.2  |  1-3 / 方向键选择  ·  Space 精准格挡",33,697,12,(Color){143,168,184,255});
}
