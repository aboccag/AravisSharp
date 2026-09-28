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
| `AravisNativeDiscoveryTests.cs` | 4 | Hand-crafted discovery, interface, and version bindings |
| `CameraWrapperTests.cs` | 50 | High-level camera wrapper behavior |
| **Total** | **65** | |

## Shared Fixture

`NativeLibraryFixture.cs` calls `AravisLibrary.RegisterResolver()` once before all tests, ensuring the native library resolver is active.

## Camera-Optional

Tests that require native Aravis are guarded with `NativeFact` and skip cleanly when `libaravis-0.8` is unavailable.

By default the suite only ever opens the Aravis fake camera, even when real cameras are visible. Tests reconfigure the camera they open (ROI, exposure, auto exposure, triggers) and start acquisition, and a GigE camera on a shared network may belong to another application. To run the suite against one real camera, name it explicitly:

```bash
ARAVIS_TEST_DEVICE_ID="Opto Engineering-ITA24-GM-10C-600357" dotnet test AravisSharp.Tests/
```

Device IDs are listed by `arv-tool-0.8`; discovery alone never opens a device. `CameraNew_WithNullDeviceId_ShouldOpenFirstCamera` only runs when every visible device is fake, since a null ID opens whichever device enumerates first.

## Prerequisites

- .NET 10.0 SDK
- Aravis native library (`libaravis-0.8.so.0` on Linux, `libaravis-0.8-0.dll` on Windows)
- A USB3 Vision or GigE Vision camera, named with `ARAVIS_TEST_DEVICE_ID` (optional, for hardware coverage)
