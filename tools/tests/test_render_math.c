#include "weather.h"
#include "viewport.h"
#include <assert.h>
#include <stdio.h>

int main(void) {
    const float times[]={0.0f,10.0f,30.0f,60.0f,90.0f,600.0f,3600.0f,86400.0f};
    assert(WeatherWrap(-1.0f,720.0f)==719.0f);
    assert(WeatherWrap(721.0f,720.0f)==1.0f);
    for(unsigned t=0;t<sizeof(times)/sizeof(times[0]);t++) {
        int visible=0;
        for(int i=0;i<120;i++) {
            Vector2 p=WeatherFireflyPosition(i,times[t],1280.0f,720.0f);
            if(p.x>=0.0f&&p.x<1280.0f&&p.y>=0.0f&&p.y<720.0f)visible++;
        }
        assert(visible==120);
        printf("PASS: 120/120 fireflies remain onscreen at %.0f seconds\n",times[t]);
    }
    const int sizes[][4]={{1280,720,1280,720},{1600,900,1600,900},
        {1920,1080,1920,1080},{2048,1095,2048,1095},
        {1280,720,1920,1080},{1920,1080,3840,2160}};
    for(unsigned i=0;i<sizeof(sizes)/sizeof(sizes[0]);i++) {
        GameViewport v=CalculateGameViewport(sizes[i][0],sizes[i][1],sizes[i][2],sizes[i][3]);
        assert(v.pixelWidth*9==v.pixelHeight*16);
        assert(v.pixelWidth<=sizes[i][2]&&v.pixelHeight<=sizes[i][3]);
        float dpiX=(float)sizes[i][2]/sizes[i][0],dpiY=(float)sizes[i][3]/sizes[i][1];
        assert(fabsf(v.destination.width*dpiX-v.pixelWidth)<.001f);
        assert(fabsf(v.destination.height*dpiY-v.pixelHeight)<.001f);
        assert(fabsf(v.destination.x*dpiX-roundf(v.destination.x*dpiX))<.001f);
        assert(fabsf(v.destination.y*dpiY-roundf(v.destination.y*dpiY))<.001f);
        printf("PASS: native viewport %dx%d at window %dx%d\n",v.pixelWidth,v.pixelHeight,sizes[i][0],sizes[i][1]);
    }
    puts("Render math tests passed.");
    return 0;
}
