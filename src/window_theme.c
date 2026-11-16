#if defined(_WIN32)
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <dwmapi.h>

#define IDI_MIST_ISLE_ICON 101

void ApplyWindowsWindowTheme(void *nativeWindow) {
    HWND hwnd=(HWND)nativeWindow;
    if(!hwnd)return;
    BOOL darkMode=TRUE;
    HRESULT hr=DwmSetWindowAttribute(hwnd,20,&darkMode,sizeof(darkMode));
    if(FAILED(hr))DwmSetWindowAttribute(hwnd,19,&darkMode,sizeof(darkMode));
    COLORREF caption=RGB(15,27,43);
    COLORREF captionText=RGB(244,232,211);
    COLORREF border=RGB(58,82,105);
    DwmSetWindowAttribute(hwnd,35,&caption,sizeof(caption));
    DwmSetWindowAttribute(hwnd,36,&captionText,sizeof(captionText));
    DwmSetWindowAttribute(hwnd,34,&border,sizeof(border));
    HICON icon=(HICON)LoadImageW(GetModuleHandleW(NULL),MAKEINTRESOURCEW(IDI_MIST_ISLE_ICON),
                                 IMAGE_ICON,0,0,LR_DEFAULTSIZE|LR_SHARED);
    if(icon) {
        SendMessageW(hwnd,WM_SETICON,ICON_BIG,(LPARAM)icon);
        SendMessageW(hwnd,WM_SETICON,ICON_SMALL,(LPARAM)icon);
    }
}
#else
void ApplyWindowsWindowTheme(void *nativeWindow) {(void)nativeWindow;}
#endif
