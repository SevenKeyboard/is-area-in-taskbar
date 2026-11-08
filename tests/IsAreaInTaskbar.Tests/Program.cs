using System.Runtime.InteropServices;
using SevenKeyboard;

internal static partial class Program
{
    private static int _checks;

    private static void Main(string[] args)
    {
        Midpoints();
        NativeHitTestBoundaries();
        ShortCircuitFailures();
        if (args.Contains("--native-read-only"))
            NativeReadOnly();
        Console.WriteLine($"PASS: {_checks} area checks.");
    }

    private static void Midpoints()
    {
        var fake = new Fake();
        var query = new TaskbarAreaQuery(fake);
        Sample(fake, () => query.Check(7, -9), (7, -9), "integer point defaults both ends");
        Sample(fake, () => query.Check(7, -9, x2: 12), (9, -9), "integer y end independently omitted");
        Sample(fake, () => query.Check(7, -9, y2: -4), (7, -7), "integer x end independently omitted");
        Sample(fake, () => query.Check(0, 0, -3, -3), (-1, -1), "integer reverse odd spans truncate toward zero");
        Sample(fake, () => query.Check(0.0, 0.0, -3.0, -3.0), (-2, -2), "double reverse odd spans floor");
        Sample(fake, () => query.Check(0, 0.0, -3, -3.0), (-1, -2), "integer x with double y");
        Sample(fake, () => query.Check(0.0, 0, -3.0, -3), (-2, -1), "double x with integer y");
        Sample(fake, () => query.Check(0, 0, x2: -3.0), (-2, 0), "floating end promotes only x");
        Sample(fake, () => query.Check(0, 0, y2: -3.0), (0, -2), "floating end promotes only y");
        Sample(fake, () => query.Check(-5, -5, -2, -2), (-4, -4), "negative start with positive odd span");
        Sample(fake, () => query.Check(-2, -2, -5, -5), (-3, -3), "no endpoint normalization");
        Sample(fake, () => query.Check(-0.25, 2.75), (-1, 2), "fractional point floors after defaults");
        Sample(fake, () => query.Check(0.75, 0.75, 2.25, 2.25), (0, 0),
            "inner division floors before outer floor, not ordinary average");
        Sample(fake, () => query.Check(0.75, -0.25, -0.75, -1.75), (-1, -2),
            "fractional reverse span floors leftward");
        Sample(fake, () => query.Check(5, 2.75, x2: 2), (4, 2), "mixed y end independently omitted");
        Sample(fake, () => query.Check(2.75, 5, y2: 2), (2, 4), "mixed x end independently omitted");
        Sample(fake, () => query.Check(0.75, 2.75, y2: -0.25), (0, 0),
            "double x end independently omitted");
        Sample(fake, () => query.Check(0.75, 2.75, x2: -2.25), (-2, 2),
            "double y end independently omitted");
        Sample(fake, () => query.Check(int.MinValue, int.MaxValue, int.MaxValue, int.MinValue),
            (-1, 0), "full signed coordinate spans widen subtraction");
        Sample(fake, () => query.Check(2147483648.25, -2147483649.25),
            (int.MinValue, int.MaxValue - 1), "NumPut signed Int keeps low 32 bits");
    }

    private static void NativeHitTestBoundaries()
    {
        var fake = new Fake
        {
            PointAt = (x, y) => x >= 100 && x < 200 && y >= 50 && y < 70 ? 11 : 99,
            RootFor = hwnd => hwnd == 11 ? 10 : 99
        };
        var query = new TaskbarAreaQuery(fake);
        Check(query.Check(100, 50), "half-open hit-test includes left/top");
        Check(query.Check(199, 69), "last interior pixel");
        Check(!query.Check(200, 60), "native right boundary does not match");
        Check(!query.Check(150, 70), "native bottom boundary does not match");
        Check(!query.Check(99, 60) && !query.Check(150, 49), "outside left/top");
        Check(query.Check(0, 0, 300, 120), "large rectangle passes when only its center hits taskbar");
        Check(!query.Check(0, 60, 120, 60), "partial overlap fails when its center misses");
        Check(!query.Check(200, 60, 199), "integer reversed odd span stays on excluded edge");
        Check(query.Check(200.0, 60, 199.0), "double reversed odd span moves inside edge");
        Check(!query.Check(200, 60.0, 199), "floating y must not alter integer x boundary");
        Check(query.Check(150, 70.0, y2: 69.0), "floating y reversed span moves inside bottom");
        Check(!query.Check(150.0, 70, y2: 69), "floating x must not alter integer y boundary");

        fake.PointAt = (_, _) => 11;
        fake.RootFor = _ => 30;
        Check(!query.Check(150, 60), "secondary taskbar root differs from primary");
        fake.RootFor = _ => 10;
        Check(query.Check(150, 60), "child window root, not point window itself, identifies taskbar");
    }

    private static void ShortCircuitFailures()
    {
        var hidden = new Fake { Visible = false };
        Check(!new TaskbarAreaQuery(hidden).Check(1, 2), "visibility gates hit testing");
        Calls(hidden, "visible");

        var noPoint = new Fake { PointAt = (_, _) => 0 };
        Check(!new TaskbarAreaQuery(noPoint).Check(1, 2), "WindowFromPoint failure");
        Calls(noPoint, "visible", "point");

        var noRoot = new Fake { RootFor = _ => 0 };
        Check(!new TaskbarAreaQuery(noRoot).Check(1, 2), "GetAncestor failure");
        Calls(noRoot, "visible", "point", "root");

        var noTaskbar = new Fake { Taskbar = 0 };
        Check(!new TaskbarAreaQuery(noTaskbar).Check(1, 2), "fresh Shell_TrayWnd lookup failure");
        Calls(noTaskbar, "visible", "point", "root", "taskbar");

        var replaced = new Fake { AfterVisibility = self => self.Taskbar = 30 };
        Check(!new TaskbarAreaQuery(replaced).Check(1, 2),
            "fresh lookup does not reuse the visibility check's former primary taskbar");
        Calls(replaced, "visible", "point", "root", "taskbar");
    }

    private static void NativeReadOnly()
    {
        nint dpi = GetThreadDpiAwarenessContext();
        var points = new List<(int X, int Y)> { (0, 0), (-1, -1) };
        nint taskbar = GetTaskbarHwnd.Get();
        if (taskbar != 0 && GetWindowRect(taskbar, out NativeRect rect) != 0)
        {
            points.Add((rect.Left, rect.Top));
            points.Add((rect.Right, rect.Bottom));
            points.Add(((int)(rect.Left + ((long)rect.Right - rect.Left) / 2),
                (int)(rect.Top + ((long)rect.Bottom - rect.Top) / 2)));
        }

        foreach ((int x, int y) in points)
        {
            var before = Probe(x, y);
            bool actual = IsAreaInTaskbar.Check(x, y);
            var after = Probe(x, y);
            if (before == after)
                Check(actual == before.Matches, "native point agrees with stable visibility/root/primary observations");
            else
                Console.WriteLine($"Native state changed while sampling ({x}, {y}); comparison skipped.");
        }

        // Exercise all public numeric overloads without changing any window.
        _ = IsAreaInTaskbar.Check(0.0, 0, -3.0, -3);
        _ = IsAreaInTaskbar.Check(0, 0.0, -3, -3.0);
        _ = IsAreaInTaskbar.Check(0.0, 0.0, -3.0, -3.0);
        Check(GetThreadDpiAwarenessContext() == dpi, "public queries leave caller DPI context unchanged");
        Console.WriteLine($"Native: sampled {points.Count} existing desktop points.");
    }

    private static Observation Probe(int x, int y)
    {
        bool visible = IsTaskbarVisible.Check();
        nint point = WindowFromPoint.Get(x, y);
        nint root = point != 0 ? GetAncestor(point, 2) : 0;
        return new(visible, point, root, GetTaskbarHwnd.Get());
    }

    private readonly record struct Observation(bool Visible, nint Point, nint Root, nint Taskbar)
    {
        internal bool Matches => Visible && Point != 0 && Root != 0 && Taskbar != 0 && Root == Taskbar;
    }

    private static void Sample(Fake fake, Func<bool> invoke, (int, int) expected, string message)
    {
        fake.Calls.Clear();
        Check(invoke(), message + ": matching root");
        Check(fake.LastPoint == expected, message + ": computed point");
        Calls(fake, "visible", "point", "root", "taskbar");
    }

    private static void Calls(Fake fake, params string[] expected) =>
        Check(fake.Calls.SequenceEqual(expected), "native short-circuit order");

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        internal int Left, Top, Right, Bottom;
    }

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetWindowRect(nint hwnd, out NativeRect rect);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetAncestor(nint hwnd, uint flags);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetThreadDpiAwarenessContext();

    private sealed class Fake : ITaskbarAreaNative
    {
        internal bool Visible = true;
        internal nint Taskbar = 10;
        internal Func<int, int, nint> PointAt = (_, _) => 11;
        internal Func<nint, nint> RootFor = _ => 10;
        internal Action<Fake>? AfterVisibility;
        internal (int, int) LastPoint;
        internal nint LastWindow;
        internal readonly List<string> Calls = [];

        public bool IsVisible()
        {
            Calls.Add("visible");
            AfterVisibility?.Invoke(this);
            return Visible;
        }
        public nint WindowAt(int x, int y)
        {
            Calls.Add("point");
            LastPoint = (x, y);
            return LastWindow = PointAt(x, y);
        }
        public nint GetRoot(nint hwnd)
        {
            Check(hwnd == LastWindow, "ancestor lookup uses exact WindowFromPoint handle");
            Calls.Add("root");
            return RootFor(hwnd);
        }
        public nint FindTaskbar() { Calls.Add("taskbar"); return Taskbar; }
    }
}
