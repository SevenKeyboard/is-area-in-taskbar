//==============================================================
// isAreaInTaskbar — Checks whether a point/rectangle lies within the Windows taskbar area
//
// GitHub: https://github.com/SevenKeyboard/is-area-in-taskbar
// Author: SevenKeyboard Ltd. (2026)
// License: The Unlicense
//==============================================================

using System.Runtime.InteropServices;

namespace SevenKeyboard;

/// <summary>Tests the source midpoint's root window against the visible primary taskbar.</summary>
public static class IsAreaInTaskbar
{
    private static readonly TaskbarAreaQuery Query = new(TaskbarAreaNative.Instance);

    public static bool Check(int x1, int y1, int? x2 = null, int? y2 = null) =>
        Query.Check(x1, y1, x2, y2);

    public static bool Check(double x1, int y1, double? x2 = null, int? y2 = null) =>
        Query.Check(x1, y1, x2, y2);

    public static bool Check(int x1, double y1, int? x2 = null, double? y2 = null) =>
        Query.Check(x1, y1, x2, y2);

    public static bool Check(double x1, double y1, double? x2 = null, double? y2 = null) =>
        Query.Check(x1, y1, x2, y2);
}

internal sealed class TaskbarAreaQuery(ITaskbarAreaNative native)
{
    internal bool Check(int x1, int y1, int? x2 = null, int? y2 = null) =>
        CheckPoint(Midpoint(x1, x2), Midpoint(y1, y2));

    internal bool Check(double x1, int y1, double? x2 = null, int? y2 = null) =>
        CheckPoint(Midpoint(x1, x2), Midpoint(y1, y2));

    internal bool Check(int x1, double y1, int? x2 = null, double? y2 = null) =>
        CheckPoint(Midpoint(x1, x2), Midpoint(y1, y2));

    internal bool Check(double x1, double y1, double? x2 = null, double? y2 = null) =>
        CheckPoint(Midpoint(x1, x2), Midpoint(y1, y2));

    private static int Midpoint(int start, int? end) =>
        // AHK integer // truncates toward zero. Widen before subtraction.
        (int)(start + ((long)(end ?? start) - start) / 2);

    private static int Midpoint(double start, double? end) =>
        // AHK floating // floors first, then the outer Floor is applied.
        // NumPut(..., "Int") keeps the low 32 bits of the resulting integer.
        unchecked((int)(long)Math.Floor(start + Math.Floor(((end ?? start) - start) / 2)));

    private bool CheckPoint(int x, int y)
    {
        if (!native.IsVisible())
            return false;
        nint point = native.WindowAt(x, y);
        if (point == 0)
            return false;
        nint root = native.GetRoot(point);
        if (root == 0)
            return false;
        nint taskbar = native.FindTaskbar();
        return taskbar != 0 && root == taskbar;
    }
}

internal interface ITaskbarAreaNative
{
    bool IsVisible();
    nint WindowAt(int x, int y);
    nint GetRoot(nint hwnd);
    nint FindTaskbar();
}

internal sealed partial class TaskbarAreaNative : ITaskbarAreaNative
{
    internal static readonly TaskbarAreaNative Instance = new();

    public bool IsVisible() => IsTaskbarVisible.Check();
    public nint WindowAt(int x, int y) => WindowFromPoint.Get(x, y);
    public nint GetRoot(nint hwnd) => GetAncestor(hwnd, 2);
    public nint FindTaskbar() => GetTaskbarHwnd.Get();

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetAncestor(nint hwnd, uint flags);
}
