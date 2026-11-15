# AravisSharp.Tests

Tests for the AravisSharp bindings. Uses **xUnit v2** and targets **net8.0** and **net10.0** (the two target frameworks run one after the other, `TestTfmsInParallel=false`).

## Running Tests

```bash
dotnet test AravisSharp.Tests/

# Verbose output
dotnet test AravisSharp.Tests/ --logger "console;verbosity=detailed"

# One target framework, one test class
dotnet test AravisSharp.Tests/ -f net10.0 --filter "FullyQualifiedName~AravisNativeTests"
```

## Test Files

| File | Tests | What It Covers |
|------|-------|----------------|
| `AravisLibraryTests.cs` | 2 | Native library probe order (pure path logic, no native library needed) |
| `AravisNativeTests.cs` | 10 | Hand-crafted P/Invoke: device enumeration, device info, buffer allocation, pixel format constants |
| `AravisNativeDiscoveryTests.cs` | 4 | Hand-crafted discovery, interface, and version bindings |
| `FakeCameraTests.cs` | 4 | Fake interface discovery, metadata, parameters, one acquired frame |
| `CameraWrapperTests.cs` | 50 | High-level `Camera` API: features, bounds, auto modes, device type, GigE and USB3 specifics |
| `CameraLifecycleTests.cs` | 14 | GError to `AravisException`, dispose idempotence and `ObjectDisposedException`, buffer ownership after `PushBuffer`, `AcquireSingleFrame`, `Device` lifetime, GigE-only stream setters, PGM export |
| `NodeMapTests.cs` | 8 | `NodeMap`: GenICam XML, category tree, feature details (Integer, Float, Enumeration, Command) |
| `ImageAcquisitionTests.cs` | 10 | Frame acquisition: status, dimensions, data, PNG export, frame IDs, statistics |
| `CameraArgumentTests.cs` | 17 | A null feature name throws `ArgumentNullException` before any native call, on each of the 16 `Camera` members that take one; a reflection check that the list is complete |
| `StreamTimeoutTests.cs` | 15 | `PopBuffer(ulong)` timeout conversion from milliseconds to microseconds and its cap (pure, no native library needed) |
| `GigEStreamTests.cs` | 5 | GigE stream settings (`ConfigureGigEDefaults`, resend timings) and acquisition, against the Aravis GigE Vision simulator on 127.0.0.1 |
| `BayerDemosaicTests.cs` | 32 | `ImageHelper.DemosaicBayer8` on synthetic mosaics: the four Bayer 8 patterns, measured samples kept, rounded bilinear means, border and one-pixel-wide or -high images, argument errors (pure, no native library needed) |
| `BayerExportFakeCameraTests.cs` | 1 | `SaveToPng` and `SaveToJpeg` of a `BayerBG8` frame: colour files of the buffer size, PNG equal to the demosaiced buffer; restores the pixel format |
| `InteropPinTests.cs` | 10 | Pins of interop behaviours (issue #13): `PopBuffer()`, `PopBuffer(ulong.MaxValue)` and finite timeouts block until a buffer is pushed, `PopBuffer(0)` on an empty queue returns `null` at once (every wait bounded, a blocked worker released by a push), the native stream released exactly once by `Dispose()` called twice and by the finalizer of a stream never disposed (counted with `g_object_weak_ref` by `GObjectReleaseProbe.cs`, the test holding its own reference), 10 000 calls of each `dup_available` wrapper on the fake camera, 200 on a real device (a double or wrong free aborts the process; a leak is not seen), no `DllImport` of a variadic function, `GValue` layout and field types on 64-bit |
| `MonoExportFakeCameraTests.cs` | 1 | `SaveToPng` of a `Mono16` frame: each sample scaled to 8 bits (`sample * 255 / 65535`); restores the pixel format. Exercised only on a camera that offers Mono16: the fake camera always does (missing there is a failure); on a real device without it the test writes "Not run" to its output and passes, as `BayerExportFakeCameraTests` does without a Bayer 8 format |
| `StreamFinalizationDuringPopTests.cs` | 4 | A `Stream` the caller no longer references is not collected while its own `PopBuffer()` or `PopBuffer(ulong)` is blocked. Two parent tests each run one child scenario in a child test host with `DOTNET_TieredCompilation=0`; the two child tests run only there |
| **Total** | **187** | per target framework |

Against the fake camera, 169 run and 18 are skipped in Debug, 171 run and 16 are skipped in Release (see below).

## Native library

Every test except the two probe-order tests, the pixel format constants, the timeout conversion tests, the demosaic tests of `BayerDemosaicTests.cs`, the reflection check of `CameraArgumentTests.cs` and the variadic-import and `GValue` layout checks of `InteropPinTests.cs` needs `libaravis-0.8`. Those tests use `[NativeFact]` (or one of the attributes derived from it) or `[NativeTheory]` and are skipped when the library cannot be loaded.

`NativeLibraryFixture.cs` enables the Aravis `Fake` interface once, from a module initializer, before any test runs. The fake camera is therefore always discoverable when the native library is present, and the device-list tests assert that at least one device is listed.

## Which camera the tests open

By default the suite opens **only the Aravis fake camera**, even when real cameras are visible. Tests reconfigure the camera they open (ROI, pixel format, exposure, gain, triggers) and start acquisition, and a GigE camera on a shared network may belong to another application. Every camera open goes through `CameraTestHelpers.ResolveTestDeviceId()`:

- `ARAVIS_TEST_DEVICE_ID` unset: the fake camera. If it is not discovered, the test fails.
- `ARAVIS_TEST_DEVICE_ID` set: that device. If it is not discovered, the test fails and lists the visible device IDs.

`GigEStreamTests` are the one exception: they need a real `ArvGvStream`, which the fake interface does not provide, so they start the Aravis GigE Vision simulator (`ArvGvFakeCamera`) on 127.0.0.1 and open only that device, whatever `ARAVIS_TEST_DEVICE_ID` says.

Camera tests never return early because a camera is missing. Open and configuration errors propagate and fail the test.

To run the suite against one real camera, name it explicitly:

```bash
ARAVIS_TEST_DEVICE_ID="Opto Engineering-ITA24-GM-10C-600357" dotnet test AravisSharp.Tests/
```

Device IDs are listed by `arv-tool-0.8`. Discovery alone never opens a device.

## Skipped tests

xUnit v2 can only skip a test from its attribute, when the test is discovered. The attributes in `NativeTestEnvironment.cs` decide from `ARAVIS_TEST_DEVICE_ID` and from discovery data (the device protocol). They never open a device:

| Attribute | Skipped when |
|-----------|--------------|
| `[NativeFact]`, `[NativeTheory]` | `libaravis-0.8` cannot be loaded |
| `[RealCameraFact("...")]` | the fake camera is used; it has no `AcquisitionFrameCount` or `ExposureAuto` (7 tests) |
| `[GigECameraFact]` | the test camera is not GigE Vision (3 tests) |
| `[Usb3CameraFact]` | the test camera is not USB3 Vision (4 tests) |
| `[NonGigECameraFact]` | the test camera is GigE Vision (1 test, which checks that GigE-only stream setters throw on other streams) |
| `[OptimizedNativeFact]` (`OptimizedNativeFactAttribute.cs`) | `libaravis-0.8` cannot be loaded, or AravisSharp is built with the JIT optimizer disabled (Debug): the 2 parent tests of `StreamFinalizationDuringPopTests`, since a lifetime defect only exists in optimized code. CI runs them (`-c Release`) |
| child-only fact (`StreamFinalizationDuringPopTests`) | always, except in the child test host its parent test starts (2 tests) |
| `[SixtyFourBitFact]` (`SixtyFourBitFactAttribute.cs`) | the process is 32-bit (1 test, the `GValue` layout) |

Against the fake camera, a missing optional feature is a failure: its features are known from `aravis/src/arv-fake-camera.xml`. On a real camera, a test that needs an optional feature the camera lacks (for example USB3 bandwidth control) returns without asserting anything, because xUnit v2 cannot skip it at run time.

## Prerequisites

- .NET 8.0 and .NET 10.0 SDKs (or run a single framework with `-f`)
- Aravis native library (`libaravis-0.8.so.0` on Linux, `libaravis-0.8-0.dll` on Windows), installed system-wide or copied under `bin/<config>/<tfm>/runtimes/<rid>/native/`
- Optionally, a USB3 Vision or GigE Vision camera named with `ARAVIS_TEST_DEVICE_ID`, for hardware coverage
