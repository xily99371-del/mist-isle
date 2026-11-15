#ifndef MIST_ISLE_ENDING_H
#define MIST_ISLE_ENDING_H

#include "game.h"

/* Runtime sheets are shared with exploration, so the film cannot drift back
 * to a different generation of character art. No separate miniature actors. */
typedef struct {
    Texture2D heroine;
    Texture2D companions[3];
} EndingCast;

void EndingInit(void);
void EndingClose(void);
void EndingDraw(const Game *game, Font font, const EndingCast *cast);

#endif
