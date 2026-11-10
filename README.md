# DpiAwarenessContextUtils

C# version **1.0.1** of SevenKeyboard's six DPI context helpers.
Targets `net10.0-windows` and supports NativeAOT. The Win32 APIs require
[Windows 10 1607+](https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context);
use a Windows version supported by your .NET 10 runtime. Contexts `-4`/`-5` need 1703+/1809+.

All methods belong to `SevenKeyboard.DpiAwarenessContextUtils`; contexts and HWNDs use `nint`.

| Helper | Return |
| --- | --- |
| `AreDpiAwarenessContextsEqual(nint, nint)` | `bool`, ignoring informational flags |
| `GetThreadDpiAwarenessContext()` | Raw context |
| `GetThreadDpiAwarenessContextIgnoringInfoFlag()` | Normalized context |
| `GetWindowDpiAwarenessContext(nint)` | Raw context; `0` for an invalid window |
| `GetWindowDpiAwarenessContextIgnoringInfoFlag(nint)` | Normalized context |
| `SetThreadDpiAwarenessContext(nint)` | Previous **raw** context; `0` on failure |

Normalization checks `-1, -2, -3, -4, -5` in order; the first native match wins.
Zero or unknown contexts normalize to `0`. The caller chooses the context.
Setting changes the current thread, not process DPI awareness. DPI arithmetic stays separate.
The setter captures the native error code for `Marshal.GetLastPInvokeError()`; it does not throw on failure.
Restore the returned raw value in `finally` on the **same OS thread**, with **no `await`** between calls.

```csharp
using static SevenKeyboard.DpiAwarenessContextUtils;

nint current = GetThreadDpiAwarenessContext();
nint previous = SetThreadDpiAwarenessContext(-3); // Explicit per-monitor context.
if (previous == 0) throw new InvalidOperationException("Could not set DPI context.");
try
{
    // Synchronous work.
}
finally
{
    if (SetThreadDpiAwarenessContext(previous) == 0)
        throw new InvalidOperationException("Could not restore DPI context.");
}
```

Build with the .NET 10 SDK:

```powershell
dotnet build src/SevenKeyboard.DpiAwarenessContextUtils/SevenKeyboard.DpiAwarenessContextUtils.csproj -c Release
```

[Test builds and validation](tests/README.md). SevenKeyboard Ltd. (2025). [The Unlicense](LICENSE).
