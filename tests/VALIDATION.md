# Validation

Verified 2026-11-10 on Windows 11 x64 with SDK 10.0.403.

Managed x64, managed x86 and NativeAOT x64 built without warnings/errors.
After Authenticode signing and signature verification, all three targets passed
all nine test groups: normalization order, first match, opaque pointer width,
zero/unknown contexts, native equality, invalid HWND/setter and exact raw
current-thread get/set/restore. NativeAOT reported dynamic code unsupported.

No windows were created and no desktop input was sent.
Windows 10 execution was not tested. See tests/README.md for build instructions.