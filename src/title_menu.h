#ifndef MIST_ISLE_TITLE_MENU_H
#define MIST_ISLE_TITLE_MENU_H
/* Transient UI only. No SaveData fields or battle rules change. */
typedef enum { TITLE_MAIN, TITLE_SETTINGS, TITLE_NEW_CONFIRM, TITLE_EXIT_CONFIRM } TitlePage;
typedef struct {
    TitlePage page;
    int selected, hover;
    bool initialized, hasSave;
    float noticeTime;
    Vector2 lastMouse;
    char notice[160];
} TitleMenu;
static inline Rectangle TitleButtonRect(int i) {
    const Rectangle r[4]={{536,294,284,50},{559,357,250,45},
                          {559,414,250,45},{559,473,250,45}};
    return r[i>=0&&i<4?i:0];
}
static inline Rectangle TitleSettingsRect(int i) {
    return (Rectangle){432,278+i*62.0f,416,46};
}
static inline Rectangle TitleConfirmRect(int i) {
    return (Rectangle){420+i*230.0f,414,210,52};
}
/* Physical-window input and native-resolution rendering share one 16:9 map.
 * Outside/letterboxed coordinates never activate a button. */
static inline bool TitleMapMouse(Vector2 point,Rectangle viewport,Vector2 *logical) {
    if(viewport.width<=0||viewport.height<=0||point.x<viewport.x||point.y<viewport.y||
       point.x>=viewport.x+viewport.width||point.y>=viewport.y+viewport.height)return false;
    *logical=(Vector2){(point.x-viewport.x)*1280.0f/viewport.width,
                      (point.y-viewport.y)*720.0f/viewport.height};
    return true;
}
#endif
