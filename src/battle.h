#ifndef MIST_ISLE_BATTLE_H
#define MIST_ISLE_BATTLE_H
#define LIGHTHOUSE_BOSS_MAX_HP 450.0f
/* Shared by attack logic and HUD; phase boundaries scale with maximum HP. */
int BattleBossPhase(float hp, float maxHp);
int BattleBossHitCount(int phase, bool heavy);
float BattleBossAttackDamage(int phase, bool heavy);
#define BATTLE_MAX_STRIKES 4
/* Transient presentation state only: never serialized into SaveData. */
typedef enum { BATTLE_CHOOSE, BATTLE_PLAYER, BATTLE_ENEMY,
               BATTLE_DEFEAT, BATTLE_LOSS, BATTLE_PREVIEW_END } BattlePhase;
typedef enum { ACTION_NONE, ACTION_GLOW, ACTION_WATER, ACTION_SPARK,
               ACTION_ENEMY, ACTION_RESONANCE } BattleAction;
typedef struct {
    BattlePhase phase;
    BattleAction action;
    float clock, impactAt, duration, hitStop;
    float damage, heal, enemyHp, enemyTrail, buddyHp[3], healByBuddy[3], trailDelay;
    int actor, defender, wetTurns;
    bool initialized, impacted, combo, dampen, damped, inputBlocked;
    bool guardAttempted, guarded, preview;
    /* One enemy action may contain several independently guarded strikes. */
    int attackPhase, strikeIndex, strikeCount, strikesDone, guardsSucceeded;
    int guardResults[BATTLE_MAX_STRIKES]; /* 0 pending, 1 guarded, -1 missed */
    float attackDamage, guardWindow, guardTargetRadius, guardOuterRadius, guardCurve;
    bool guardNeedsRelease;
} BattleState;
/* The renderer uses the same timing function as the guard judgement. */
float BattleGuardRingRadius(const BattleState *battle, float clock);
#endif
