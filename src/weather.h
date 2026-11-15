#ifndef MISTISLE_WEATHER_H
#define MISTISLE_WEATHER_H

#include "raylib.h"
#include <math.h>

static inline float WeatherNoise(int i,int salt) {
    float value=sinf((float)(i*127+salt*311)*12.9898f)*43758.5453f;
    return value-floorf(value);
}

static inline float WeatherWrap(float value,float extent) {
    float wrapped=fmodf(value,extent);
    return wrapped<0.0f?wrapped+extent:wrapped;
}

/* Preserve the release's positions, speed and drift while wrapping particles
 * moving upward back onto the screen. fmodf alone returns a negative remainder. */
static inline Vector2 WeatherFireflyPosition(int index,float time,float width,float height) {
    return (Vector2){
        WeatherWrap(WeatherNoise(index,41)*width+sinf(time*.55f+index)*25.0f,width),
        WeatherWrap(WeatherNoise(index,42)*height-time*(9.0f+WeatherNoise(index,43)*18.0f),height)
    };
}

#endif
