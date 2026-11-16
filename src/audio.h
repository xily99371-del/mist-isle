#ifndef MIST_ISLE_AUDIO_H
#define MIST_ISLE_AUDIO_H

#include "game.h"

typedef enum {
    AUDIO_STEP_GRASS_A, AUDIO_STEP_GRASS_B,
    AUDIO_STEP_STONE_A, AUDIO_STEP_STONE_B,
    AUDIO_STEP_WET_A, AUDIO_STEP_WET_B,
    AUDIO_DIALOGUE, AUDIO_INTERACT, AUDIO_COLLISION, AUDIO_DASH,
    AUDIO_PICKUP, AUDIO_ATTACK, AUDIO_HURT, AUDIO_VICTORY,
    /* Keep existing cue IDs stable; add recorded left/right variations. */
    AUDIO_STEP_GRASS_C, AUDIO_STEP_GRASS_D, AUDIO_STEP_GRASS_E, AUDIO_STEP_GRASS_F,
    AUDIO_STEP_STONE_C, AUDIO_STEP_STONE_D, AUDIO_STEP_STONE_E, AUDIO_STEP_STONE_F,
    AUDIO_CUE_COUNT
} AudioCue;

typedef struct {
    const char *english;
    const char *chinese;
    float start, end, progress, intensity;
} AudioSubtitle;

void AudioInit(void);
void AudioClose(void);
void AudioUpdate(const Game *game, float dt, bool focused);
void AudioPlay(AudioCue cue);
/* Map identity deliberately wins over rain and decorative terrain tiles. */
static inline int AudioFootstepSurface(Area area) {
    return (area==AREA_RUINS||area==AREA_LIGHTHOUSE)?1:0;
}
/* Distances are actual world-space travel, never requested velocity. */
void AudioMovement(float distance, bool blocked, int surface, float dt);
void AudioResetMovement(void);
void AudioToggleMute(void);
void AudioToggleSubtitles(void);
bool AudioIsMuted(void);
bool AudioSubtitlesEnabled(void);
bool AudioBossTrackActive(void);
bool AudioGetSubtitle(AudioSubtitle *out);
float AudioGetMusicTime(void);
float AudioGetBossEnergy(void);

#endif
