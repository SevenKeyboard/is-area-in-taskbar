# is-area-in-taskbar

Tests a point or a rectangle's source midpoint against the primary Windows
taskbar using .NET 10.

```csharp
bool point = SevenKeyboard.IsAreaInTaskbar.Check(120, 1040);
bool area = SevenKeyboard.IsAreaInTaskbar.Check(100, 1020, 180, 1060);
bool mixed = SevenKeyboard.IsAreaInTaskbar.Check(5, 2.75, x2: 2);
```

Four overloads accept `int/int`, `double/int`, `int/double` and `double/double`
start coordinates. Each optional endpoint uses its axis's type and independently
defaults to the start. A floating endpoint promotes that axis to `double`.

Version 1.0.1 preserves AHK v1's `Floor(start + (end - start) // 2)` on each
axis: integer division truncates toward zero; floating division floors.
Fractions are retained until these rounding steps. Integer differences use
64-bit arithmetic. Reversed integer `0..-3` samples `-1`; floating
`0.0..-3.0` samples `-2`. Mixed axes retain their individual rules.
See [AHK division semantics](https://github.com/AutoHotkey/AutoHotkeyDocs/blob/v1/docs/Variables.htm#FloorDivide).

The visibility heuristic must pass first. The midpoint's `WindowFromPoint`
result must have a nonzero `GA_ROOT` equal to `Shell_TrayWnd`. Any failed
lookup returns false. Only the midpoint is tested; rectangles are not normalized
or required to fit inside a taskbar, and secondary taskbars do not match.
Native hit-testing and the caller's unchanged DPI context determine boundaries.

External references: IsTaskbarVisible 1.x (>=1.0.1), GetTaskbarHwnd 1.x (>=1.0.0),
WindowFromPoint 1.x (>=1.0.0), and their transitive monitor libraries.
The shared `library-build` target checks minimum versions and matching majors.
Override the corresponding `*ProjectPath` or `LibraryBuildTargetsPath`
MSBuild properties when projects are elsewhere. No dependency sources are copied.

[Build and test instructions](tests/README.md).
