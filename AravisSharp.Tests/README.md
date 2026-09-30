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
| `GigEStreamTests.cs` | 5 | GigE stream settings (`ConfigureGigEDefaults`, resend timings) and acquisition, against the Aravis GigE Vision simulator on 127.0.0.1 |
| **Total** | **107** | per target framework |

Against the fake camera, 93 run and 14 are skipped (see below).

## Native library

Every test except the two probe-order tests and the pixel format constants needs `libaravis-0.8`. Those tests use `[NativeFact]` (or one of the attributes derived from it) and are skipped when the library cannot be loaded.

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
| `[NativeFact]` | `libaravis-0.8` cannot be loaded |
| `[RealCameraFact("...")]` | the fake camera is used; it has no `AcquisitionFrameCount` or `ExposureAuto` (7 tests) |
| `[GigECameraFact]` | the test camera is not GigE Vision (3 tests) |
| `[Usb3CameraFact]` | the test camera is not USB3 Vision (4 tests) |
| `[NonGigECameraFact]` | the test camera is GigE Vision (1 test, which checks that GigE-only stream setters throw on other streams) |

Against the fake camera, a missing optional feature is a failure: its features are known from `aravis/src/arv-fake-camera.xml`. On a real camera, a test that needs an optional feature the camera lacks (for example USB3 bandwidth control) returns without asserting anything, because xUnit v2 cannot skip it at run time.

## Prerequisites

- .NET 8.0 and .NET 10.0 SDKs (or run a single framework with `-f`)
- Aravis native library (`libaravis-0.8.so.0` on Linux, `libaravis-0.8-0.dll` on Windows), installed system-wide or copied under `bin/<config>/<tfm>/runtimes/<rid>/native/`
- Optionally, a USB3 Vision or GigE Vision camera named with `ARAVIS_TEST_DEVICE_ID`, for hardware coverage
