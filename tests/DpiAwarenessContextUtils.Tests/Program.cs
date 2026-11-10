using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SevenKeyboard;

namespace DpiAwarenessContextUtilsTests;

[SupportedOSPlatform("windows10.0.14393")]
internal static partial class Program
{
    private static int Main()
    {
        (string Name, Action Run)[] tests =
        [
            ("zero skips comparison", ZeroSkipsComparison),
            ("unknown checks all five contexts in order", UnknownChecksAllCandidates),
            ("each match stops at the first match", EachMatchStopsSearch),
            ("overlapping matches preserve original precedence", FirstMatchWins),
            ("canonical inputs still use native comparison", CanonicalInputUsesComparison),
            ("native comparison returns bool", NativeComparison),
            ("invalid window returns raw and normalized zero", InvalidWindow),
            ("invalid setter returns zero and preserves context", InvalidSetter),
            ("current thread raw get/set/restore", CurrentThreadSmoke),
        ];

        Console.WriteLine(
            $"Architecture: {RuntimeInformation.ProcessArchitecture}; " +
            $"dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}");

        int failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {exception}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
        return failures == 0 ? 0 : 1;
    }

    // High bits must survive on both 32-bit and 64-bit runtimes.
    private static nint OpaqueContext => IntPtr.Size == 8
        ? unchecked((nint)0xA123456787654321UL)
        : unchecked((nint)0x87654321U);

    private static void ZeroSkipsComparison()
    {
        Equal(0, DpiAwarenessContextUtils.NormalizeDpiAwarenessContext(
            0, (_, _) => throw new InvalidOperationException("Zero must not be compared.")),
            "A failed raw getter must normalize to zero.");
    }

    private static void UnknownChecksAllCandidates()
    {
        int comparisons = 0;
        nint result = DpiAwarenessContextUtils.NormalizeDpiAwarenessContext(
            OpaqueContext, (raw, candidate) =>
            {
                Equal(OpaqueContext, raw, "The full opaque pointer must reach the comparer.");
                Equal(-++comparisons, candidate, "Candidates must be -1, -2, -3, -4, -5.");
                return false;
            });

        Equal(0, result, "Unknown contexts must normalize to zero.");
        Equal(5, comparisons, "Every known context must be considered.");
    }

    private static void EachMatchStopsSearch()
    {
        for (int matchIndex = 1; matchIndex <= 5; matchIndex++)
        {
            int comparisons = 0;
            nint expected = -matchIndex;
            nint result = DpiAwarenessContextUtils.NormalizeDpiAwarenessContext(
                OpaqueContext, (raw, candidate) =>
                {
                    Equal(OpaqueContext, raw, "Normalization must preserve all raw bits.");
                    Equal(-++comparisons, candidate, "Search order changed.");
                    return candidate == expected;
                });

            Equal(expected, result, "The matched canonical context must be returned.");
            Equal(matchIndex, comparisons, "Search must stop immediately after a match.");
        }
    }

    private static void FirstMatchWins()
    {
        int comparisons = 0;
        nint result = DpiAwarenessContextUtils.NormalizeDpiAwarenessContext(
            OpaqueContext, (_, candidate) =>
            {
                comparisons++;
                return candidate is -1 or -5;
            });

        Equal(-1, result, "An earlier equivalent context must win.");
        Equal(1, comparisons, "Later equivalent contexts must not be examined.");
    }

    private static void CanonicalInputUsesComparison()
    {
        int comparisons = 0;
        nint result = DpiAwarenessContextUtils.NormalizeDpiAwarenessContext(
            -5, (raw, _) =>
            {
                Equal(-5, raw, "Even predefined values must be passed through unchanged.");
                comparisons++;
                return false;
            });

        Equal(0, result, "A negative value is not automatically accepted by normalization.");
        Equal(5, comparisons, "Win32 equality must decide every match.");
    }

    private static void NativeComparison()
    {
        Check(DpiAwarenessContextUtils.AreDpiAwarenessContextsEqual(-1, -1),
            "Equal native contexts must compare true.");
        Check(!DpiAwarenessContextUtils.AreDpiAwarenessContextsEqual(-1, -2),
            "Different native contexts must compare false.");
    }

    private static void InvalidWindow()
    {
        Equal(0, DpiAwarenessContextUtils.GetWindowDpiAwarenessContext(0),
            "An invalid HWND must return raw zero.");
        Equal(0, DpiAwarenessContextUtils.GetWindowDpiAwarenessContextIgnoringInfoFlag(0),
            "An invalid HWND must normalize to zero.");
    }

    private static void InvalidSetter()
    {
        nint original = DpiAwarenessContextUtils.GetThreadDpiAwarenessContext();
        Check(original != 0, "The current thread must have a context.");

        // Compare against a direct binding, after poisoning .NET's cached error code.
        nint referencePrevious = NativeReference.SetThreadDpiAwarenessContext(0);
        int expectedError = Marshal.GetLastPInvokeError();
        if (referencePrevious != 0)
        {
            NativeReference.SetThreadDpiAwarenessContext(referencePrevious);
            throw new InvalidOperationException("The direct invalid setter unexpectedly succeeded.");
        }
        const int staleError = 0x5A17;
        Check(expectedError != staleError, "The native error must differ from the sentinel.");
        Marshal.SetLastPInvokeError(staleError);
        nint previous = DpiAwarenessContextUtils.SetThreadDpiAwarenessContext(0);
        int actualError = Marshal.GetLastPInvokeError();
        try
        {
            Equal(0, previous, "An invalid setter input must return zero.");
            Equal(expectedError, actualError, "The public setter must capture the native error, not a stale value.");
            Equal(original, DpiAwarenessContextUtils.GetThreadDpiAwarenessContext(),
                "A failed setter must leave the raw context unchanged.");
        }
        finally
        {
            // If the assertion detects an unexpected successful set, still restore its raw result.
            if (previous != 0)
            {
                Check(DpiAwarenessContextUtils.SetThreadDpiAwarenessContext(previous) != 0,
                    "Restoring after an unexpected successful set failed.");
            }
        }
    }

    private static void CurrentThreadSmoke()
    {
        nint original = DpiAwarenessContextUtils.GetThreadDpiAwarenessContext();
        Equal(NativeReference.GetThreadDpiAwarenessContext(), original,
            "The public getter must preserve the native raw value.");
        Check(original != 0, "The current thread must have a context.");

        // An explicit test context; the library never chooses a DPI policy.
        nint previous = DpiAwarenessContextUtils.SetThreadDpiAwarenessContext(-3);
        Check(previous != 0, "Setting a supported thread context failed.");
        try
        {
            Check(DpiAwarenessContextUtils.AreDpiAwarenessContextsEqual(original, previous),
                "The setter must return the previous context.");
            Check(DpiAwarenessContextUtils.AreDpiAwarenessContextsEqual(
                DpiAwarenessContextUtils.GetThreadDpiAwarenessContext(), -3),
                "The current thread must use the requested context.");
            Equal(-3, DpiAwarenessContextUtils.GetThreadDpiAwarenessContextIgnoringInfoFlag(),
                "The thread context must normalize to per-monitor aware.");

            // Repeated sets of the same explicit context give an independent raw return baseline.
            nint expectedRaw = NativeReference.SetThreadDpiAwarenessContext(-3);
            Check(expectedRaw != 0, "The direct native setter failed.");
            nint actualRaw = DpiAwarenessContextUtils.SetThreadDpiAwarenessContext(-3);
            Equal(expectedRaw, actualRaw, "The setter must return the raw value, not its normalization.");

            Console.WriteLine($"SMOKE original=0x{original:X}, previous=0x{previous:X}, " +
                $"native previous=0x{expectedRaw:X}, helper previous=0x{actualRaw:X}");
        }
        finally
        {
            // Preserve the exact setter return, including informational/inherited flags.
            Check(DpiAwarenessContextUtils.SetThreadDpiAwarenessContext(previous) != 0,
                "Restoring the original raw context failed.");
        }

        Equal(original, DpiAwarenessContextUtils.GetThreadDpiAwarenessContext(),
            "Restoration must recover the original raw thread context.");
    }

    private static void Equal(nint expected, nint actual, string message) =>
        Check(expected == actual, $"{message} Expected 0x{expected:X}; actual 0x{actual:X}.");

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static partial class NativeReference
    {
        [LibraryImport("user32.dll", EntryPoint = "GetThreadDpiAwarenessContext")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint GetThreadDpiAwarenessContext();

        [LibraryImport("user32.dll", EntryPoint = "SetThreadDpiAwarenessContext", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint SetThreadDpiAwarenessContext(nint context);
    }
}
