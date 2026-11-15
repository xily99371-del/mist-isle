#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include "viewport.h"
#include <math.h>
#include <stdio.h>
#include <string.h>

static Buddy *Partner(Game *g,int id) {
    return id==0?&g->lumi:id==1?&g->splash:id==2?&g->spark:NULL;
}
int BattleBuddyCount(const Game *g) {
    return (g->lumi.owned&&g->lumi.hp>0)+(g->splash.owned&&g->splash.hp>0)+(g->spark.owned&&g->spark.hp>0);
}
int BattleBuddyId(const Game *g,int slot) {
    const Buddy *party[3]={&g->lumi,&g->splash,&g->spark};
    for(int i=0;i<3;i++)if(party[i]->owned&&party[i]->hp>0) {
        if(slot--==0)return i;
    }
    return -1;
}
static bool Held(void) {
    return GameInputDown(KEY_ENTER)||GameInputDown(KEY_SPACE)||GameInputDown(KEY_E)||IsMouseButtonDown(MOUSE_BUTTON_LEFT);
}
static void Message(Game *g,const char *s) {
    snprintf(g->toast,sizeof(g->toast),"%s",s);g->toastTimer=3.0f;
}
static float Towards(float current,float target,float dt,float speed) {
    float next=current+(target-current)*(1.0f-expf(-dt*speed));
    return fabsf(next-target)<.05f?target:next;
}
void BattleInit(Game *g) {
    BattleState *b=&g->battle;
    bool preview=g->previewOnly;
    memset(b,0,sizeof(*b));
    b->initialized=true;b->preview=preview;b->phase=BATTLE_CHOOSE;
    b->actor=b->defender=-1;b->inputBlocked=Held();
    b->enemyHp=b->enemyTrail=g->enemies[g->battleEnemy].hp;
    b->playerHp=g->player.hp;
    b->buddyHp[0]=g->lumi.hp;b->buddyHp[1]=g->splash.hp;b->buddyHp[2]=g->spark.hp;
    b->wetTurns=g->enemySoaked?2:0;b->dampen=g->enemySoaked;
    if(g->battleRound<1)g->battleRound=1;
    g->battleTurn=0;
    int count=BattleBuddyCount(g);g->battleChoice=0;
    for(int i=0;i<count;i++)if(BattleBuddyId(g,i)==g->selectedBuddy)g->battleChoice=i;
    g->selectedBuddy=count?BattleBuddyId(g,g->battleChoice):-1;
    Message(g,"选择伙伴技能，按 Enter 出招。敌人会自动回击。");
}
static void Action(Game *g,BattlePhase phase,BattleAction action,int actor) {
    BattleState *b=&g->battle;
    b->phase=phase;b->action=action;b->actor=actor;
    b->clock=0;b->hitStop=0;b->impacted=false;b->guarded=false;b->guardAttempted=false;
    b->combo=false;b->damped=false;b->damage=0;b->heal=0;
    g->battleTurn=phase==BATTLE_ENEMY?1:0;
}
bool BattleStartAction(Game *g,int actor) {
    BattleState *b=&g->battle;
    if(g->scene!=SCENE_BATTLE||b->phase!=BATTLE_CHOOSE)return false;
    Buddy *p=Partner(g,actor);
    if(actor>=0&&(!p||!p->owned||p->hp<=0))return false;
    if(actor<-1||actor>2)return false;
    Action(g,BATTLE_PLAYER,(BattleAction)(actor+1),actor);
    b->defender=actor;g->selectedBuddy=actor;
    int level=p?p->level:1;if(level<1)level=1;
    b->duration=1.45f;b->impactAt=.84f;
    switch(b->action) {
        case ACTION_GLOW:
            b->duration=1.60f;b->impactAt=.95f;
            b->damage=8+(level-1)*2;b->heal=18+(level-1)*3;
            Message(g,"星光治愈 · 光点集中，照亮旅人与伙伴。");break;
        case ACTION_WATER:
            b->damage=20+(level-1)*3;
            Message(g,"泡泡水流 · 水流将使敌人潮湿。");break;
        case ACTION_SPARK:
            b->duration=1.50f;b->impactAt=.78f;b->combo=g->enemySoaked;
            b->damage=(b->combo?38:26)+(level-1)*4;
            Message(g,b->combo?"潮湿导电 · 晶弧共鸣！":"晶弧放电 · 电光正在集中。");break;
        default:
            b->duration=1.20f;b->impactAt=.58f;b->damage=10;
            Message(g,"旅人出击 · 向前冲刺！");break;
    }
    AudioPlay(AUDIO_DASH);
    return true;
}
static void EnemyAction(Game *g) {
    BattleState *b=&g->battle;Enemy *e=&g->enemies[g->battleEnemy];
    int defender=b->defender;
    int phase=e->hp<=50?3:e->hp<=100?2:1;
    bool heavy=e->type==ENEMY_BOSS&&(phase==3?g->battleRound%2==0:g->battleRound%3==0);
    Action(g,BATTLE_ENEMY,heavy?ACTION_RESONANCE:ACTION_ENEMY,-1);
    b->defender=defender;
    b->duration=heavy?1.95f:1.45f;b->impactAt=heavy?1.20f:.88f;
    b->damage=e->type==ENEMY_BOSS?(phase==3?20:phase==2?17:14):10;
    if(heavy)b->damage+=phase==3?12:9;
    b->damped=b->dampen;
    if(b->damped)b->damage*=e->type==ENEMY_BOSS?.65f:.8f;
    Message(g,heavy?"共鸣重击 · 光圈接近中心时按 Space 格挡！":"敌方攻击 · 光圈接近中心时按 Space 格挡！");
}
static void Impact(Game *g) {
    BattleState *b=&g->battle;Enemy *e=&g->enemies[g->battleEnemy];
    if(b->impacted)return;
    b->impacted=true;b->hitStop=b->action==ACTION_RESONANCE||b->combo?.10f:.065f;
    if(b->phase==BATTLE_PLAYER) {
        e->hp=fmaxf(0,e->hp-b->damage);
        b->trailDelay=.32f;
        if(b->action==ACTION_GLOW) {
            float before=g->player.hp;
            g->player.hp=fminf(g->player.maxHp,g->player.hp+b->heal);
            b->heal=g->player.hp-before;
        }
        if(b->action==ACTION_WATER) {
            g->enemySoaked=true;b->wetTurns=2;b->dampen=true;
        } else if(b->action==ACTION_SPARK) {
            g->enemySoaked=false;b->wetTurns=0;b->dampen=false;
        }
        AudioPlay(AUDIO_ATTACK);
    } else {
        if(b->guarded)b->damage*=.45f;
        Buddy *p=Partner(g,b->defender);
        if(p&&p->owned&&p->hp>0)p->hp=fmaxf(0,p->hp-b->damage);
        else {b->defender=-1;g->player.hp=fmaxf(0,g->player.hp-b->damage);}
        /* Only the one-use damage reduction is consumed. Wetness remains for
           the following player turn, so WATER -> SPARK is actually possible. */
        b->dampen=false;
        AudioPlay(b->guarded?AUDIO_COLLISION:AUDIO_HURT);
        if(b->guarded)Message(g,"精准格挡！本次受到的伤害降低。");
        else if(b->damped)Message(g,"核心受潮！本次攻击伤害降低。");
    }
    g->battleDamage=b->damage;
}
static void ChooseInput(Game *g) {
    BattleState *b=&g->battle;
    if(b->inputBlocked){if(!Held())b->inputBlocked=false;return;}
    int count=BattleBuddyCount(g),options=count?count:1;
    if(g->battleChoice>=options||g->battleChoice<0)g->battleChoice=0;
    if(GameInputPressed(KEY_ONE)||GameInputPressed(KEY_Q))g->battleChoice=0;
    if((GameInputPressed(KEY_TWO)||GameInputPressed(KEY_W))&&options>1)g->battleChoice=1;
    if((GameInputPressed(KEY_THREE)||GameInputPressed(KEY_E))&&options>2)g->battleChoice=2;
    if(GameInputPressed(KEY_LEFT)||GameInputPressed(KEY_A))g->battleChoice=(g->battleChoice+options-1)%options;
    if(GameInputPressed(KEY_RIGHT)||GameInputPressed(KEY_D))g->battleChoice=(g->battleChoice+1)%options;
    bool confirm=GameInputPressed(KEY_ENTER);
    if(IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
        Vector2 m=GetMousePosition();Rectangle v=CurrentGameViewport().destination;
        m=(Vector2){(m.x-v.x)*SW/v.width,(m.y-v.y)*SH/v.height};
        for(int i=0;i<options;i++)if(CheckCollisionPointRec(m,(Rectangle){32+i*254.0f,570,240,112}))g->battleChoice=i;
        confirm=confirm||CheckCollisionPointRec(m,(Rectangle){838,613,392,56});
    }
    int id=count?BattleBuddyId(g,g->battleChoice):-1;g->selectedBuddy=id;
    if(confirm)BattleStartAction(g,id);
}
void BattleUpdate(Game *g,float dt) {
    if(g->battleEnemy<0||g->battleEnemy>=g->enemyCount){g->scene=SCENE_PLAY;return;}
    BattleState *b=&g->battle;Enemy *e=&g->enemies[g->battleEnemy];
    if(!b->initialized)BattleInit(g);
    dt=isfinite(dt)?fmaxf(0,dt):0;
    g->time+=dt;g->toastTimer=fmaxf(0,g->toastTimer-dt);
    b->enemyHp=Towards(b->enemyHp,e->hp,dt,10);
    b->playerHp=Towards(b->playerHp,g->player.hp,dt,10);
    for(int i=0;i<3;i++)b->buddyHp[i]=Towards(b->buddyHp[i],Partner(g,i)->hp,dt,10);
    b->trailDelay=fmaxf(0,b->trailDelay-dt);
    if(b->trailDelay<=0)b->enemyTrail=Towards(b->enemyTrail,e->hp,dt,3);
    if(b->preview&&GameInputPressed(KEY_ESCAPE)){g->scene=SCENE_TITLE;return;}
    if(b->phase==BATTLE_PREVIEW_END) {
        if(GameInputPressed(KEY_R))GameStartBattlePreview(g);
        return;
    }
    if(b->phase==BATTLE_CHOOSE){ChooseInput(g);return;}
    /* Guard input does not select partners or queue another command. Only
       one deliberate press is evaluated, before the hit is committed. */
    if(b->phase==BATTLE_ENEMY&&!b->impacted&&!b->guardAttempted&&GameInputPressed(KEY_SPACE)) {
        b->guardAttempted=true;
        b->guarded=b->clock>=b->impactAt-.20f&&b->clock<=b->impactAt;
    }
    if(b->hitStop>0) {
        float used=fminf(b->hitStop,dt);b->hitStop-=used;dt-=used;
    }
    b->clock+=dt;
    if((b->phase==BATTLE_PLAYER||b->phase==BATTLE_ENEMY)&&!b->impacted&&b->clock>=b->impactAt)Impact(g);
    if(b->clock<b->duration)return;
    if(b->phase==BATTLE_PLAYER) {
        if(e->hp<=0){b->phase=BATTLE_DEFEAT;b->clock=0;b->duration=.70f;b->hitStop=0;}
        else EnemyAction(g);
    } else if(b->phase==BATTLE_ENEMY) {
        if(g->player.hp<=0){b->phase=BATTLE_LOSS;b->clock=0;b->duration=.70f;return;}
        if(b->wetTurns>0&&--b->wetTurns==0)g->enemySoaked=false;
        g->battleRound++;g->battleTurn=0;b->phase=BATTLE_CHOOSE;b->clock=0;b->inputBlocked=Held();
        int count=BattleBuddyCount(g);g->battleChoice=0;
        for(int i=0;i<count;i++)if(BattleBuddyId(g,i)==b->defender)g->battleChoice=i;
        g->selectedBuddy=count?BattleBuddyId(g,g->battleChoice):-1;
        Message(g,g->enemySoaked?"敌人仍然潮湿！现在可使用晶弧放电。":"选择下一招行动。Space 可在敌人命中前格挡。");
    } else if(b->phase==BATTLE_DEFEAT||b->phase==BATTLE_LOSS) {
        bool won=b->phase==BATTLE_DEFEAT;
        if(b->preview) {
            b->phase=BATTLE_PREVIEW_END;b->clock=0;
            Message(g,won?"战斗体验完成！R 重来，Esc 退出。":"体验完成。R 重来，Esc 退出。");
        } else if(won)GameBattleVictory(g,b->actor);
        else GameBattleRetreat(g);
    }
}
