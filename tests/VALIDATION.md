# Validation

Verified 2026-11-10 on Windows 11 x64 with SDK 10.0.403.

Version 1.0.1: managed x64, managed x86 and NativeAOT x64 built without
warnings/errors. After Authenticode signing with the SevenKeyboard certificate
and signature verification, all three targets passed all nine test groups.
These cover normalization order, first match, opaque pointer width, zero/unknown
contexts, native equality, invalid HWND/setter and exact raw current-thread
get/set/restore. NativeAOT reported dynamic code unsupported.

The invalid-setter regression seeds a stale .NET P/Invoke error and compares the
public setter's captured error with a separate direct native binding. This checks
that consumers retain the actual Windows failure code after sharing the helper.

No windows were created and no desktop input was sent.
Windows 10 execution was not tested. See tests/README.md for build instructions.
