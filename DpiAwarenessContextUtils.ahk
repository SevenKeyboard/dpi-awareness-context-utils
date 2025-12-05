#Requires AutoHotkey v1.1.0+
;==============================================================
; DpiAwarenessContextUtils — DPI awareness context helpers
;
; GitHub: https://github.com/SevenKeyboard/dpi-awareness-context-utils
; Author: SevenKeyboard Ltd. (2025)
; License: The Unlicense
;
; Documentation / References:
;   DPI_AWARENESS_CONTEXT handle
;     https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context
;==============================================================
class VersionManager_DpiAwarenessContextUtils
{
    static _ := VersionManager_DpiAwarenessContextUtils._init()
    _init()    {
        global
        DPIAWARENESSCONTEXTUTILS_VERSION := "1.0.0"
    }
}
areDpiAwarenessContextsEqual(dpiContextA, dpiContextB)    {
    return dllCall("User32.dll\AreDpiAwarenessContextsEqual", "Ptr",dpiContextA, "Ptr",dpiContextB)
}
getThreadDpiAwarenessContext()    {
    return dllCall("User32.dll\GetThreadDpiAwarenessContext", "Ptr")
}
getThreadDpiAwarenessContextIgnoringInfoFlag()    {
    if (dpiContextA:=getThreadDpiAwarenessContext())    {
        loop 5    {
            dpiContextB:=-1*A_Index
            if (areDpiAwarenessContextsEqual(dpiContextA,dpiContextB))
                return dpiContextB
        }
    }
    return false
}
getWindowDpiAwarenessContext(hWnd)    {
    return dllCall("User32.dll\GetWindowDpiAwarenessContext", "Ptr",hWnd, "Ptr")
}
getWindowDpiAwarenessContextIgnoringInfoFlag(hWnd)    {
    if (dpiContextA:=getWindowDpiAwarenessContext(hWnd))    {
        loop 5    {
            dpiContextB:=-1*A_Index
            if (areDpiAwarenessContextsEqual(dpiContextA,dpiContextB))
                return dpiContextB
        }
    }
    return false
}
setThreadDpiAwarenessContext(dpiContext)    {
    return dllCall("User32.dll\SetThreadDpiAwarenessContext", "Ptr",dpiContext, "Ptr")
}