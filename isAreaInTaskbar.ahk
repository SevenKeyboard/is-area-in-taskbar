#Requires AutoHotkey v1.1.37+
#Include %A_ScriptDir%
#Include .\lib\isTaskbarVisible.ahk
;==============================================================
; isAreaInTaskbar — Checks whether a point/rectangle lies within the Windows taskbar area
;
; GitHub: https://github.com/SevenKeyboard/is-area-in-taskbar
; Author: SevenKeyboard Ltd. (2026)
; License: The Unlicense
;==============================================================

/*
Example Usage:
    coordMode Mouse, Screen
    F5::
        mouseGetPos x, y
        tooltip % isAreaInTaskbar(x, y)
        return
*/

class VersionManager_isAreaInTaskbar
{
    static _ := VersionManager_isAreaInTaskbar._init()
    _init()    {
        global
        ISAREAINTASKBAR_VERSION := "1.0.1"
        if (!this._verCheck(ISTASKBARVISIBLE_VERSION, "1.0.1"))
            throw exception("isTaskbarVisible version 1.x is required (minimum 1.0.1).")
        return true
    }
    _verCheck(byRef actual, required)    {
        if !isSet(actual)
            return false
        actualMajor     := strSplit(actual, ".",, 2)[1]
        requiredMajor   := strSplit(required, ".",, 2)[1]
        if (actualMajor !== requiredMajor)
            return false
        return verCompare(actual, ">=" required)
    }
}
isAreaInTaskbar(x1, y1, x2:="", y2:="")    {
    static GA_ROOT:=2
    if (x2=="")
        x2:=x1
    if (y2=="")
        y2:=y1
    if (!isTaskbarVisible())
        return false
    x:=x1, y:=y1, w:=x2-x1, h:=y2-y1
    varSetCapacity(POINT,8,0)
    numput(floor(x+w//2),POINT,0,"Int")
    numput(floor(y+h//2),POINT,4,"Int")
    if (hPoint:=dllCall("User32.dll\WindowFromPoint", "Int64",numGet(POINT,0,"Int64")))
    && (hPointRoot:=dllCall("User32.dll\GetAncestor", "Ptr",hPoint, "UInt",GA_ROOT, "Ptr"))
    && (hTaskbar:=dllCall("User32.dll\FindWindowEx", "Ptr",0, "Ptr",0, "Str","Shell_TrayWnd", "Ptr",0, "Ptr"))
        return (hPointRoot==hTaskbar)
    return false
}