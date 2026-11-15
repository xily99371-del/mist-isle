/* Reuse deterministic input/storage/audio stubs; exercise real production code. */
#define main ExistingPostBattleSuite
#include "test_postbattle.c"
#undef main

static Game Fight(void) {
    Release();Game g=BossFight();g.player.hp=70;
    g.lumi.owned=g.splash.owned=g.spark.owned=true;
    g.enemies[0].hp=150;BattleInit(&g);return g;
}
static void Advance(Game *g,float seconds) {
    for(float t=0;t<seconds;t+=1.0f/120.0f)GameUpdate(g,fminf(1.0f/120.0f,seconds-t));
}
static void ToChoice(Game *g) {
    for(int i=0;i<1500&&g->scene==SCENE_BATTLE&&g->battle.phase!=BATTLE_CHOOSE;i++)GameUpdate(g,1.0f/120.0f);
    CHECK(g->scene!=SCENE_BATTLE||g->battle.phase==BATTLE_CHOOSE,"action sequence terminates without confirmation");
}
static void ToEnemy(Game *g) {
    for(int i=0;i<1000&&g->battle.phase!=BATTLE_ENEMY;i++)GameUpdate(g,1.0f/120.0f);
    CHECK(g->battle.phase==BATTLE_ENEMY,"enemy begins automatically");
}
static void TestBossHealthScaling(void) {
    const float maxima[]={150,450,900};
    for(int i=0;i<3;i++) {
        float m=maxima[i],third=m/3;
        CHECK(BattleBossPhase(m,m)==1,"full health starts in phase one");
        CHECK(BattleBossPhase(2*third+.01f,m)==1,"above two-thirds is phase one");
        CHECK(BattleBossPhase(2*third,m)==2,"exact two-thirds begins phase two");
        CHECK(BattleBossPhase(third+.01f,m)==2,"above one-third remains phase two");
        CHECK(BattleBossPhase(third,m)==3,"exact one-third begins phase three");
        CHECK(BattleBossPhase(0,m)==3,"depleted core retains final phase for defeat animation");
        for(int phase=1;phase<=3;phase++) {
            Game g=Fight();g.enemies[0].maxHp=m;
            g.enemies[0].hp=phase==1?m:(phase==2?2*third+8:third+8);
            g.battleRound=3;BattleInit(&g);BattleStartAction(&g,0);ToEnemy(&g);
            CHECK(BattleBossPhase(g.enemies[0].hp,m)==phase,"attack logic enters ratio-selected phase");
            float expected=phase==1?23:phase==2?26:20;
            CHECK(NEAR(g.battle.damage,expected),"boss damage and heavy-attack cadence use percentage phase");
        }
    }
    CHECK(BattleBossPhase(20,0)==1,"invalid maximum health is safe");
    Release();Game g;GameStartBattlePreview(&g);
    CHECK(NEAR(g.enemies[0].hp,450)&&NEAR(g.enemies[0].maxHp,450),"preview uses the new 450 HP baseline");
    g.previewOnly=false;g.scene=SCENE_DIALOGUE;g.dialogueCount=1;g.dialoguePage=0;
    g.dialogueStartsBossBattle=true;g.dialogueInputBlocked=false;g.enemyCount=0;
    Confirm(&g);
    CHECK(g.scene==SCENE_BATTLE&&NEAR(g.enemies[0].hp,450)&&NEAR(g.enemies[0].maxHp,450),
          "normal story encounter shares the 450 HP baseline");
}
int main(void) {
    TestBossHealthScaling();
    Game g=Fight();
    CHECK(BattleStartAction(&g,1),"water starts");
    CHECK(NEAR(g.enemies[0].hp,150)&&!g.enemySoaked,"command does not damage or wet the enemy");
    GameUpdate(&g,g.battle.impactAt-.01f);
    CHECK(NEAR(g.enemies[0].hp,150),"HP unchanged immediately before impact");
    GameUpdate(&g,.02f);
    CHECK(NEAR(g.enemies[0].hp,130)&&g.enemySoaked,"impact applies damage and status together");
    float hp=g.enemies[0].hp,clock=g.battle.clock;
    CHECK(!BattleStartAction(&g,2),"commands cannot interrupt an action");
    GameUpdate(&g,-1);
    CHECK(NEAR(g.battle.clock,clock),"negative delta does not rewind an animation");
    Press(&g,KEY_ENTER,.01f);Press(&g,KEY_THREE,.01f);
    CHECK(NEAR(g.enemies[0].hp,hp)&&g.selectedBuddy==1,"repeated confirmation and switch inputs cannot duplicate damage");
    ToEnemy(&g);
    CHECK(NEAR(g.splash.hp,100),"enemy charge does not deal early damage");
    Press(&g,KEY_ONE,.01f);
    CHECK(g.battle.defender==1&&g.selectedBuddy==1,"incoming attack target cannot be changed by selecting a partner");
    ToChoice(&g);
    CHECK(NEAR(g.splash.hp,90.9f)&&NEAR(g.player.hp,70),"wet core damage hits the displayed partner, not traveller");
    CHECK(g.enemySoaked&&!g.battle.dampen&&g.battleRound==2,"wet status survives, one-use dampening does not");
    CHECK(BattleStartAction(&g,2)&&g.battle.combo,"next-turn spark can trigger conductive combo");
    GameUpdate(&g,g.battle.impactAt+.01f);
    CHECK(NEAR(g.enemies[0].hp,92)&&!g.enemySoaked,"conductive impact deals 38 and consumes wetness");
    ToChoice(&g);
    CHECK(NEAR(g.spark.hp,83),"phase-two boss deals normal non-damped 17 damage");
    CHECK(g.defeatedGuards==0&&g.lumi.xp==0,"nonlethal hits do not grant rewards");

    g=Fight();BattleStartAction(&g,0);GameUpdate(&g,.94f);
    CHECK(NEAR(g.player.hp,70),"healing waits for cast impact");
    GameUpdate(&g,.02f);
    CHECK(NEAR(g.player.hp,88)&&NEAR(g.enemies[0].hp,142),"heal and glow damage are synchronized");
    g=Fight();g.player.hp=99;BattleStartAction(&g,0);GameUpdate(&g,1);
    CHECK(NEAR(g.player.hp,100)&&NEAR(g.battle.heal,1),"heal popup reports actual clamped recovery");

    g=Fight();BattleStartAction(&g,0);ToEnemy(&g);
    Advance(&g,g.battle.impactAt-.15f);Press(&g,KEY_SPACE,.001f);
    CHECK(g.battle.guarded&&!g.battle.impacted,"one well-timed press arms guard before impact");
    ToChoice(&g);CHECK(NEAR(g.lumi.hp,93.7f),"guard reduces incoming 14 to 6.3");
    g=Fight();BattleStartAction(&g,0);ToEnemy(&g);Press(&g,KEY_SPACE,.01f);
    Advance(&g,g.battle.impactAt-.1f);Press(&g,KEY_SPACE,.001f);
    CHECK(!g.battle.guarded,"repeated early presses cannot spam perfect guard");
    ToChoice(&g);CHECK(NEAR(g.lumi.hp,86),"early guard keeps normal incoming damage");

    g=Fight();g.lumi.hp=1;BattleStartAction(&g,0);ToChoice(&g);
    CHECK(NEAR(g.lumi.hp,0)&&BattleBuddyCount(&g)==2&&g.selectedBuddy!=0,"exhausted partner is deselected after recovery");
    CHECK(!BattleStartAction(&g,0)&&!BattleStartAction(&g,3),"invalid and exhausted actors cannot act");
    g=Fight();g.lumi.hp=g.splash.hp=g.spark.hp=0;g.player.hp=5;BattleInit(&g);
    CHECK(BattleBuddyCount(&g)==0&&g.selectedBuddy==-1,"traveller fallback is selected when all partners are down");
    CHECK(BattleStartAction(&g,-1),"traveller fallback remains playable");
    for(int i=0;i<1000&&g.scene==SCENE_BATTLE;i++)GameUpdate(&g,1.0f/120.0f);
    CHECK(g.scene==SCENE_PLAY&&g.area==AREA_CAMP&&NEAR(g.player.hp,55),"loss actually returns to camp after the hit animation");

    g=Fight();mousePressed=true;GameUpdate(&g,.016f);Release();
    CHECK(g.battle.phase==BATTLE_CHOOSE,"clicking the stage or subtitles cannot cast a skill");
    g=Fight();g.enemies[0].hp=1;BattleInit(&g);BattleStartAction(&g,2);
    int before=saveWrites;
    Advance(&g,.6f);CHECK(!g.bossDefeated&&saveWrites==before,"no victory before the finishing hit");
    Advance(&g,.4f);CHECK(g.scene==SCENE_BATTLE&&!g.bossDefeated,"finishing impact stays on screen for recoil");
    for(int i=0;i<500&&g.scene==SCENE_BATTLE;i++)GameUpdate(&g,1.0f/120.0f);
    CHECK(g.scene==SCENE_POST_BATTLE_BLACKOUT&&g.spark.xp==20&&g.defeatedGuards==1,"victory callback and XP run exactly once");

    Release();GameStartBattlePreview(&g);before=saveWrites;g.enemies[0].hp=1;
    BattleStartAction(&g,2);
    for(int i=0;i<600&&g.battle.phase!=BATTLE_PREVIEW_END;i++)GameUpdate(&g,1.0f/120.0f);
    CHECK(g.battle.phase==BATTLE_PREVIEW_END&&g.previewOnly&&saveWrites==before,"preview completes without writing a save");
    Press(&g,KEY_R,.016f);
    CHECK(g.scene==SCENE_BATTLE&&NEAR(g.enemies[0].hp,LIGHTHOUSE_BOSS_MAX_HP)&&g.previewOnly,"preview replay starts a fresh isolated battle");
    Press(&g,KEY_ESCAPE,.016f);CHECK(g.scene==SCENE_TITLE&&saveWrites==before,"preview exit remains isolated");

    g=Fight();BattleStartAction(&g,0);ToEnemy(&g);
    down[KEY_ENTER]=true;ToChoice(&g);
    CHECK(g.battle.inputBlocked,"held confirm across enemy turn cannot queue an attack");
    pressed[KEY_ENTER]=true;GameUpdate(&g,.016f);CHECK(g.battle.phase==BATTLE_CHOOSE,"held confirm is ignored");
    Release();GameUpdate(&g,.016f);Confirm(&g);
    CHECK(g.battle.phase==BATTLE_PLAYER,"fresh confirm after release starts one action");
    printf("Impact-timed combat: %d checks passed.\n",checks);
    return 0;
}
