# Console tests

Nine dependency-free tests cover normalization order and early exit, all five
matches, zero/unknown contexts, full-width opaque values, native equality and
failure returns, and current-thread raw get/set/restore.

The native smoke test explicitly sets per-monitor awareness on its own thread
and restores the setter's raw return in `finally`. A direct native call checks
that the public setter preserves its raw result. Tests create no windows and
perform no desktop interaction.

## Build

Run from the repository root with the .NET 10 SDK. NativeAOT also requires the
Windows C++ build tools and Windows SDK.

```powershell
dotnet publish tests/DpiAwarenessContextUtils.Tests/DpiAwarenessContextUtils.Tests.csproj -c Release -r win-x64 -p:Platform=x64 -p:PlatformTarget=x64 -p:PublishAot=true -o artifacts/aot-win-x64
dotnet build tests/DpiAwarenessContextUtils.Tests/DpiAwarenessContextUtils.Tests.csproj -c Release -r win-x64 -p:Platform=x64 -p:PlatformTarget=x64
dotnet build tests/DpiAwarenessContextUtils.Tests/DpiAwarenessContextUtils.Tests.csproj -c Release -r win-x86 -p:Platform=x86 -p:PlatformTarget=x86
```

The platform properties separate x64/x86 intermediate and output directories.
Build managed x64 after publishing: NativeAOT publishing removes its apphost.
Managed runs need the matching x64 or x86 .NET 10 runtime.

## Run

Apply required local executable signing before running. If Application Control
blocks execution, have the parent sign the outputs; do not bypass the policy.

```powershell
& ./tests/DpiAwarenessContextUtils.Tests/bin/x64/Release/net10.0-windows/win-x64/DpiAwarenessContextUtils.Tests.exe
& ./tests/DpiAwarenessContextUtils.Tests/bin/x86/Release/net10.0-windows/win-x86/DpiAwarenessContextUtils.Tests.exe
& ./artifacts/aot-win-x64/DpiAwarenessContextUtils.Tests.exe
```

Each successful run reports its process architecture, the smoke test's raw
values, `9/9 tests passed.`, and exit code `0`. NativeAOT reports dynamic code
support as `False`. A build or publish success alone does not validate native
runtime behavior.

## Port compatibility

The six public helpers preserve the AHK v1.0.0 lookup and return logic.
C# adaptations are PascalCase static methods, pointer-width `nint`, a `bool`
equality result, and numeric `0` in place of AHK `false` for normalization
failure. Version metadata replaces the AHK version-manager global.
