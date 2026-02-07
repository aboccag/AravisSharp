# AravisSharp.Tests

Unit tests for the AravisSharp bindings. Uses **xUnit** on **.NET 10.0**.

## Running Tests

```bash
cd AravisSharp.Tests
dotnet test

# Verbose output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~AravisNativeTests"
```

## Test Files

| File | Tests | What It Covers |
|------|-------|----------------|
| `AravisNativeTests.cs` | 11 | Hand-crafted P/Invoke: device enumeration, camera info, buffer allocation, pixel format constants |
| `AravisGeneratedTests.cs` | 11 | Auto-generated bindings: device enumeration, extended info, function count (≥400) |
| `BindingCompatibilityTests.cs` | 7 | Cross-checks manual vs generated bindings return consistent results |
| **Total** | **29** | |

## Shared Fixture

`NativeLibraryFixture.cs` calls `AravisLibrary.RegisterResolver()` once before all tests, ensuring the native library resolver is active.

## Camera-Optional

Tests that require a connected camera are guarded with `Skip` conditions — they pass (skip) gracefully when no camera is plugged in. Device enumeration and constant-validation tests always run.

## Prerequisites

- .NET 10.0 SDK
- Aravis native library (`libaravis-0.8.so.0` on Linux, `libaravis-0.8-0.dll` on Windows)
- A connected USB3 Vision or GigE Vision camera (optional, for full coverage)
