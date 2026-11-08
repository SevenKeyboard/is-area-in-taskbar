# Build and tests

Run these commands from the repository root.

```powershell
dotnet build tests/IsAreaInTaskbar.Tests -c Release
dotnet run --project tests/IsAreaInTaskbar.Tests -c Release --no-build
dotnet run --project tests/IsAreaInTaskbar.Tests -c Release --no-build -- --native-read-only
dotnet publish tests/IsAreaInTaskbar.Tests -c Release -r win-x64 -p:PublishAot=true
dotnet publish tests/IsAreaInTaskbar.Tests -c Release -r win-x86 -p:PublishAot=true
```

Default tests use deterministic native-call substitutes. Opt-in native checks
only query existing windows and monitor data; no taskbar changes or input.
Supports x64, x86 and NativeAOT.
