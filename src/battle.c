#include "game.h"
#include "audio.h"
#include "platform_input.h"
#include "viewport.h"
#include <math.h>
#include <stdio.h>
#include <string.h>

int BattleBossPhase(float hp,float maxHp) {
    if(!isfinite(hp)||!isfinite(maxHp)||maxHp<=0)return 1;
    /* Compare cross-products, keeping exact 1/3 and 2/3 boundaries consistent. */
    float scaled=fmaxf(0,hp)*3.0f;
    return scaled<=maxHp?3:scaled<=maxHp*2.0f?2:1;
}
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
static bool CanAct(Game *g,int id) {
    Buddy *p=Partner(g,id);
    return p&&p->owned&&p->hp>0;
}
static void SelectPartner(Game *g,int id) {
    for(int slot=0;slot<BattleBuddyCount(g);slot++)if(BattleBuddyId(g,slot)==id) {
        g->battleChoice=slot;g->selectedBuddy=id;return;
    }
}
static void PartyDefeated(Game *g) {
    BattleState *b=&g->battle;
    b->phase=BATTLE_LOSS;b->clock=0;b->duration=.70f;b->hitStop=0;
    b->actor=b->defender=-1;g->selectedBuddy=-1;g->battleTurn=0;
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
    b->buddyHp[0]=g->lumi.hp;b->buddyHp[1]=g->splash.hp;b->buddyHp[2]=g->spark.hp;
    b->wetTurns=g->enemySoaked?2:0;b->dampen=g->enemySoaked;
    if(g->battleRound<1)g->battleRound=1;
    g->battleTurn=0;
    int count=BattleBuddyCount(g);g->battleChoice=0;
    for(int i=0;i<count;i++)if(BattleBuddyId(g,i)==g->selectedBuddy)g->battleChoice=i;
    g->selectedBuddy=count?BattleBuddyId(g,g->battleChoice):-1;
    if(!count){PartyDefeated(g);Message(g,"全队伙伴体力耗尽，正在返回营地。");return;}
    Message(g,"选择伙伴技能，按 Enter 出招。只有伙伴进行战斗。");
}
static void Action(Game *g,BattlePhase phase,BattleAction action,int actor) {
    BattleState *b=&g->battle;
    b->phase=phase;b->action=action;b->actor=actor;
    b->clock=0;b->hitStop=0;b->impacted=false;b->guarded=false;b->guardAttempted=false;
    b->combo=false;b->damped=false;b->damage=0;b->heal=0;
    memset(b->healByBuddy,0,sizeof(b->healByBuddy));
    g->battleTurn=phase==BATTLE_ENEMY?1:0;
}
bool BattleStartAction(Game *g,int actor) {
    BattleState *b=&g->battle;
    if(g->scene!=SCENE_BATTLE||b->phase!=BATTLE_CHOOSE)return false;
    Buddy *p=Partner(g,actor);
    /* The traveller is a commander, never a fallback combatant. */
    if(actor<0||actor>2||!CanAct(g,actor))return false;
    Action(g,BATTLE_PLAYER,(BattleAction)(actor+1),actor);
    b->defender=actor;g->selectedBuddy=actor;
    int level=p?p->level:1;if(level<1)level=1;
    b->duration=1.45f;b->impactAt=.84f;
    switch(b->action) {
        case ACTION_GLOW:
            b->duration=1.60f;b->impactAt=.95f;
            b->damage=8+(level-1)*2;b->heal=18+(level-1)*3;
            Message(g,"星光治愈 · 为全队伙伴恢复体力。");break;
        case ACTION_WATER:
            b->damage=20+(level-1)*3;
            Message(g,"泡泡水流 · 水流将使敌人潮湿。");break;
        case ACTION_SPARK:
            b->duration=1.50f;b->impactAt=.78f;b->combo=g->enemySoaked;
            b->damage=(b->combo?38:26)+(level-1)*4;
            Message(g,b->combo?"潮湿导电 · 晶弧共鸣！":"晶弧放电 · 电光正在集中。");break;
        default:return false; /* Only the three pet skill IDs are accepted. */
    }
    AudioPlay(AUDIO_DASH);
    return true;
}
static void EnemyAction(Game *g) {
    BattleState *b=&g->battle;Enemy *e=&g->enemies[g->battleEnemy];
    int defender=b->defender;
    if(!CanAct(g,defender))defender=BattleBuddyId(g,0);
    if(defender<0){PartyDefeated(g);return;}
    int phase=BattleBossPhase(e->hp,e->maxHp);
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
            const float amount=fmaxf(0,b->heal);
            b->heal=0;
            /* Team recovery includes recruited exhausted pets; it cannot be
             * cast after a wipe because the caster must still be active. */
            for(int id=0;id<3;id++) {
                Buddy *pet=Partner(g,id);
                if(!pet->owned)continue;
                float before=pet->hp;
                pet->hp=fminf(pet->maxHp,pet->hp+amount);
                b->healByBuddy[id]=fmaxf(0,pet->hp-before);
                b->heal+=b->healByBuddy[id];
            }
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
        else b->damage=0; /* A vanished pet target never redirects to traveller. */
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
    int count=BattleBuddyCount(g);
    if(count==0){PartyDefeated(g);return;}
    if(g->battleChoice>=count||g->battleChoice<0)g->battleChoice=0;
    /* Fixed number keys and fixed cards: a fainted pet never changes another
     * pet's hotkey, and disabled cards cannot select the traveller. */
    if(GameInputPressed(KEY_ONE)||GameInputPressed(KEY_Q))SelectPartner(g,0);
    if(GameInputPressed(KEY_TWO)||GameInputPressed(KEY_W))SelectPartner(g,1);
    if(GameInputPressed(KEY_THREE)||GameInputPressed(KEY_E))SelectPartner(g,2);
    if(GameInputPressed(KEY_LEFT)||GameInputPressed(KEY_A))g->battleChoice=(g->battleChoice+count-1)%count;
    if(GameInputPressed(KEY_RIGHT)||GameInputPressed(KEY_D))g->battleChoice=(g->battleChoice+1)%count;
    bool confirm=GameInputPressed(KEY_ENTER);
    if(IsMouseButtonPressed(MOUSE_BUTTON_LEFT)) {
        Vector2 m=GetMousePosition();Rectangle v=CurrentGameViewport().destination;
        m=(Vector2){(m.x-v.x)*SW/v.width,(m.y-v.y)*SH/v.height};
        for(int id=0;id<3;id++)if(CheckCollisionPointRec(m,(Rectangle){32+id*254.0f,570,240,112}))SelectPartner(g,id);
        confirm=confirm||CheckCollisionPointRec(m,(Rectangle){838,613,392,56});
    }
    int id=BattleBuddyId(g,g->battleChoice);g->selectedBuddy=id;
    if(confirm)BattleStartAction(g,id);
}
void BattleUpdate(Game *g,float dt) {
    if(g->battleEnemy<0||g->battleEnemy>=g->enemyCount){g->scene=SCENE_PLAY;return;}
    BattleState *b=&g->battle;Enemy *e=&g->enemies[g->battleEnemy];
    if(!b->initialized)BattleInit(g);
    dt=isfinite(dt)?fmaxf(0,dt):0;
    g->time+=dt;g->toastTimer=fmaxf(0,g->toastTimer-dt);
    b->enemyHp=Towards(b->enemyHp,e->hp,dt,10);
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
        if(BattleBuddyCount(g)==0){PartyDefeated(g);return;}
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
