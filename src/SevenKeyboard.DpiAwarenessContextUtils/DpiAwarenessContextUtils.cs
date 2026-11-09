// DpiAwarenessContextUtils 1.0.0
// Author: SevenKeyboard Ltd. (2025)
// License: The Unlicense
// https://github.com/SevenKeyboard/dpi-awareness-context-utils

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SevenKeyboard;

/// <summary>Helpers for raw Win32 DPI awareness contexts.</summary>
[SupportedOSPlatform("windows10.0.14393")]
public static partial class DpiAwarenessContextUtils
{
    /// <summary>Compares contexts using Win32, which ignores informational flags.</summary>
    public static bool AreDpiAwarenessContextsEqual(nint dpiContextA, nint dpiContextB) =>
        NativeMethods.AreDpiAwarenessContextsEqual(dpiContextA, dpiContextB) != 0;

    /// <summary>Returns the current thread's raw DPI awareness context.</summary>
    public static nint GetThreadDpiAwarenessContext() =>
        NativeMethods.GetThreadDpiAwarenessContext();

    /// <summary>Returns the first equivalent context in -1 through -5 order, or zero.</summary>
    public static nint GetThreadDpiAwarenessContextIgnoringInfoFlag() =>
        NormalizeDpiAwarenessContext(GetThreadDpiAwarenessContext(), AreDpiAwarenessContextsEqual);

    /// <summary>Returns the window's raw DPI awareness context, or zero for an invalid window.</summary>
    public static nint GetWindowDpiAwarenessContext(nint hWnd) =>
        NativeMethods.GetWindowDpiAwarenessContext(hWnd);

    /// <summary>Returns the window's first equivalent context in -1 through -5 order, or zero.</summary>
    public static nint GetWindowDpiAwarenessContextIgnoringInfoFlag(nint hWnd) =>
        NormalizeDpiAwarenessContext(GetWindowDpiAwarenessContext(hWnd), AreDpiAwarenessContextsEqual);

    /// <summary>Sets the current thread's context and returns its previous raw context, or zero on failure.</summary>
    /// <remarks>
    /// This does not change the process DPI awareness. Restore the returned raw context in a finally
    /// block on the same thread; do not await between setting and restoring.
    /// </remarks>
    public static nint SetThreadDpiAwarenessContext(nint dpiContext) =>
        NativeMethods.SetThreadDpiAwarenessContext(dpiContext);

    // The injected comparison keeps order, early exit, and opaque pointer handling testable
    // without changing native bindings or storing mutable process-wide hooks.
    internal static nint NormalizeDpiAwarenessContext(
        nint rawContext, Func<nint, nint, bool> areEqual)
    {
        if (rawContext != 0)
        {
            for (int index = 1; index <= 5; index++)
            {
                nint candidate = -index;
                if (areEqual(rawContext, candidate))
                {
                    return candidate;
                }
            }
        }

        return 0;
    }

    private static partial class NativeMethods
    {
        [LibraryImport("user32.dll", EntryPoint = "AreDpiAwarenessContextsEqual")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int AreDpiAwarenessContextsEqual(nint dpiContextA, nint dpiContextB);

        [LibraryImport("user32.dll", EntryPoint = "GetThreadDpiAwarenessContext")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GetThreadDpiAwarenessContext();

        [LibraryImport("user32.dll", EntryPoint = "GetWindowDpiAwarenessContext")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GetWindowDpiAwarenessContext(nint hWnd);

        [LibraryImport("user32.dll", EntryPoint = "SetThreadDpiAwarenessContext")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint SetThreadDpiAwarenessContext(nint dpiContext);
    }
}
