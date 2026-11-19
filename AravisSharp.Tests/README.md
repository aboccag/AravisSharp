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
| `NodeMapTests.cs` | 11 | `NodeMap`: GenICam XML, category tree, feature details (Integer, Float, Enumeration, Command); `GetNode` is null for a missing node, and carries the value of a string node (#10) |
| `ImageAcquisitionTests.cs` | 10 | Frame acquisition: status, dimensions, data, PNG export, frame IDs, statistics |
| `CameraArgumentTests.cs` | 17 | A null feature name throws `ArgumentNullException` before any native call, on each of the 16 `Camera` members that take one; a reflection check that the list is complete |
| `StreamTimeoutTests.cs` | 15 | `PopBuffer(ulong)` timeout conversion from milliseconds to microseconds and its cap (pure, no native library needed) |
| `GigEStreamTests.cs` | 13 | GigE stream settings (`ConfigureGigEDefaults`, resend timings), `Stream.IsGigEVision` and the GigE guard letting the GigE members through (a packet size of 1 to 65535), acquisition, and the socket buffer the kernel grants compared with `Stream.GetSocketBufferSizeLimit()` (Linux with `ss`, 2 cases, #17), against the Aravis GigE Vision simulator on 127.0.0.1; `Camera.ToPacketSize`, the checked conversion of the `guint` packet size (pure, 4 tests) |
| `SocketBufferLimitTests.cs` | 14 | `Stream.GetSocketBufferSizeLimit()`: `net.core.rmem_max` on Linux, null elsewhere, and null on a missing or malformed file (pure, #17) |
| `GigEGuardTests.cs` | 7 | The GigE members of `Camera` (the eleven `Gv*`) and `Stream` throw `NotSupportedException` naming the member on a non-GigE camera or stream; `Stream.IsGigEVision` is false there; reflection checks that the tables list every public `Gv*` member of `Camera` and classify every public member of `Stream` as GigE or not (#10) |
| `ErrorInfoTests.cs` | 18 | `AravisException.Domain` and `Code` from real GErrors of the test camera (`Camera`, its constructor, `Device`, `GenICamNode`, the inner exception of `NodeMap`), null without a GError; `TakeGError` on a GError built with `g_error_new_literal`; the narrowed catch blocks of `NodeMap` and `FeatureDetails`, and a scan of the `GenICam/` sources for an unfiltered `catch` (#10) |
| `BayerDemosaicTests.cs` | 32 | `ImageHelper.DemosaicBayer8` on synthetic mosaics: the four Bayer 8 patterns, measured samples kept, rounded bilinear means, border and one-pixel-wide or -high images, argument errors (pure, no native library needed) |
| `BayerExportFakeCameraTests.cs` | 1 | `SaveToPng` and `SaveToJpeg` of a `BayerBG8` frame: colour files of the buffer size, PNG equal to the demosaiced buffer; restores the pixel format |
| `InteropPinTests.cs` | 10 | Pins of interop behaviours (issue #13): `PopBuffer()`, `PopBuffer(ulong.MaxValue)` and finite timeouts block until a buffer is pushed, `PopBuffer(0)` on an empty queue returns `null` at once (every wait bounded, a blocked worker released by a push), the native stream released exactly once by `Dispose()` called twice and by the finalizer of a stream never disposed (counted with `g_object_weak_ref` by `GObjectReleaseProbe.cs`, the test holding its own reference), 10 000 calls of each `dup_available` wrapper on the fake camera, 200 on a real device (a double or wrong free aborts the process; a leak is not seen), no `DllImport` of a variadic function, `GValue` layout and field types on 64-bit |
| `MonoExportFakeCameraTests.cs` | 1 | `SaveToPng` of a `Mono16` frame: each sample scaled to 8 bits (`sample * 255 / 65535`); restores the pixel format. Exercised only on a camera that offers Mono16: the fake camera always does (missing there is a failure); on a real device without it the test writes "Not run" to its output and passes, as `BayerExportFakeCameraTests` does without a Bayer 8 format |
| `StreamFinalizationDuringPopTests.cs` | 4 | A `Stream` the caller no longer references is not collected while its own `PopBuffer()` or `PopBuffer(ulong)` is blocked. Two parent tests each run one child scenario in a child test host with `DOTNET_TieredCompilation=0`; the two child tests run only there |
| `PInvokeHeaderTests.cs` | 15 | Every `DllImport`/`LibraryImport` of the AravisSharp assembly (any class, nested or not, public or not, read by reflection; today `AravisNative` and `GLibNative`, at least 200 and 30 of them, with distinct names) against its C prototype in the Aravis and GLib headers: a missing prototype, a different argument count, or a different type family for the return or any argument fails the test, every mismatch listed in one message; an explicit allow-list (empty) with a reason per entry, an entry no longer needed is itself a failure (issue #11); when it skips (outside CI only) and how CI is detected. No native library needed; see *P/Invoke header check* below |
| `HeaderPrototypesTests.cs` | 106 | The header reader and comparison rules of `HeaderPrototypes.cs` on synthetic snippets: multi-line `ARV_API` prototypes with comments and attributes, GLib availability macros and parenthesised names, inline bodies, `G_DECLARE_*` and mkenums `get_type` functions, typedef, enum and callback resolution, parameter names, variadic prototypes, prototypes the reader leaves out (unbalanced parentheses, a lowercase `__attribute__`), two different prototypes of one name; accepted and rejected declarations (`int` for `guint`, a 1-byte `bool` for `gboolean`, `uint` for `gsize`, `long` for `glong`, `char`, argument count, missing prototype, the pointee of `out`/`ref`/array parameters, a returned or `out` string, an `arv_*` prototype without `ARV_API`), `LibraryImport` entry points and `MarshalAs`, the allow-list (pure, no header files or native library needed) |
| **Total** | **405** | per target framework |

Against the fake camera, 387 run and 18 are skipped in Debug, 389 run and 16 are skipped in Release (see below).

## Native library

Every test except the two probe-order tests, the pixel format constants, the timeout conversion tests, the demosaic tests of `BayerDemosaicTests.cs`, the reflection check of `CameraArgumentTests.cs`, the variadic-import and `GValue` layout checks of `InteropPinTests.cs`, and the P/Invoke header check (`PInvokeHeaderTests.cs`, `HeaderPrototypesTests.cs`) needs `libaravis-0.8`. Those tests use `[NativeFact]` (or one of the attributes derived from it) or `[NativeTheory]` and are skipped when the library cannot be loaded.

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
| `[HeaderFact]` (`HeaderPrototypes.cs`) | the Aravis or GLib headers cannot be found, except in CI (`GITHUB_ACTIONS=true` or `CI=true`), where the test runs and fails instead (1 test, the P/Invoke header check) |

Against the fake camera, a missing optional feature is a failure: its features are known from `aravis/src/arv-fake-camera.xml`. On a real camera, a test that needs an optional feature the camera lacks (for example USB3 bandwidth control) returns without asserting anything, because xUnit v2 cannot skip it at run time.

## P/Invoke header check

`PInvokeHeaderTests` reads the Aravis headers from `aravis/src/*.h` of the submodule (run `git submodule update --init aravis` in a fresh clone or worktree), or from `ARAVIS_SOURCE_DIR` (the Aravis source tree or its `src` directory), and the GLib headers from the `glib-2.0` include directories that `pkg-config --cflags-only-I glib-2.0 gobject-2.0 gio-2.0` lists, else `/usr/include/glib-2.0` (package `libglib2.0-dev`; why `pkg-config` gave nothing is written to the test output). Without them the test is skipped with the reason; in CI (`GITHUB_ACTIONS=true` or `CI=true`) it fails.

The type families it compares (`TypeFamily`, `PInvokeSignatureCheck` in `HeaderPrototypes.cs`):

- signed and unsigned integers of each width are distinct (`gint`/`int` is `int`, `guint`/`unsigned int` is `uint`, `gint64` is `long`, `guint64` is `ulong`, ...); typedefs such as `ArvPixelFormat` (`guint32`) are resolved from the headers;
- a C enum is a 32-bit integer: `int`, `uint`, or a C# enum whose underlying type is one of them;
- `gboolean` is a `bool` marshalled as 4 bytes (no `MarshalAs`, or `UnmanagedType.Bool`); `U1`/`I1` fails;
- every pointer (`*`, arrays, callbacks; `out`/`ref`, `string`, delegates, `IntPtr`) is one family; `GType`, `gsize` and `gssize` are pointer-sized (`IntPtr`, `UIntPtr`, `nint`, `nuint`);
- `glong`/`gulong` are `CLong`/`CULong` only, their width differing between Windows and the other targets;
- the pointee of an `out`/`ref`, pointer or array parameter whose element is a primitive, an enum, `bool`, `IntPtr`/`UIntPtr` or `CLong`/`CULong` is compared too (`out uint` for a `guint64 *` fails; `out IntPtr` for a `T **` passes; `ref GValue` is only an address);
- C# `char` matches nothing (its width depends on the `CharSet`);
- a returned `string`/`StringBuilder` or an `out`/`ref` string fails: the marshaller frees memory C may own; such a declaration needs an allow-list entry saying who owns it;
- an `arv_*` function needs an `ARV_API` prototype (or `ARV_API G_DECLARE_*`, or a public enum's generated `get_type`): Aravis builds with `-fvisibility=hidden`;
- a name with two different prototypes (both branches of an `#if`) fails; a prototype the reader cannot read is left out and fails as missing.

Not checked: the GLib version that introduced an imported function (an import newer than the GLib 2.64 floor), and the signatures of the delegates passed as callbacks.

## Prerequisites

- .NET 8.0 and .NET 10.0 SDKs (or run a single framework with `-f`)
- Aravis native library (`libaravis-0.8.so.0` on Linux, `libaravis-0.8-0.dll` on Windows), installed system-wide or copied under `bin/<config>/<tfm>/runtimes/<rid>/native/`
- Optionally, a USB3 Vision or GigE Vision camera named with `ARAVIS_TEST_DEVICE_ID`, for hardware coverage
