#ifndef MIST_ISLE_BATTLE_H
#define MIST_ISLE_BATTLE_H
#define LIGHTHOUSE_BOSS_MAX_HP 450.0f
/* Shared by attack logic and HUD; phase boundaries scale with maximum HP. */
int BattleBossPhase(float hp, float maxHp);
/* Transient presentation state only: never serialized into SaveData. */
typedef enum { BATTLE_CHOOSE, BATTLE_PLAYER, BATTLE_ENEMY,
               BATTLE_DEFEAT, BATTLE_LOSS, BATTLE_PREVIEW_END } BattlePhase;
typedef enum { ACTION_STRIKE, ACTION_GLOW, ACTION_WATER, ACTION_SPARK,
               ACTION_ENEMY, ACTION_RESONANCE } BattleAction;
typedef struct {
    BattlePhase phase;
    BattleAction action;
    float clock, impactAt, duration, hitStop;
    float damage, heal, enemyHp, enemyTrail, playerHp, buddyHp[3], trailDelay;
    int actor, defender, wetTurns;
    bool initialized, impacted, combo, dampen, damped, inputBlocked;
    bool guardAttempted, guarded, preview;
} BattleState;
#endif
