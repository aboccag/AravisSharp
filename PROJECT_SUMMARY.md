# AravisSharp — Project Summary

## Overview

AravisSharp provides C# bindings for the [Aravis](https://github.com/AravisProject/aravis) industrial camera library, currently pinned to the stable Aravis 0.8.36 / `libaravis-0.8` ABI. It supports USB3 Vision and GigE Vision cameras on Windows, Linux, and macOS through a cross-platform `DllImportResolver` and a single NuGet package that bundles the native runtimes.

**Tested camera**: Basler acA720-520um (USB3 Vision, 724×542, up to 520 fps)

---

## Project Structure

```
AravisSharp/
├── AravisSharp.slnx                # Solution file
├── README.md                       # Main documentation
├── QUICKSTART.md                   # Quick reference
├── WINDOWS_SETUP.md                # Windows + WinUSB guide
├── CROSS_PLATFORM_GUIDE.md         # NuGet packaging & distribution
├── FEATURE_BROWSER_GUIDE.md        # GenICam feature browser guide
├── PROJECT_SUMMARY.md              # This file
├── THIRD-PARTY-NOTICES.md          # Licenses of the bundled native libraries
│
├── aravis/                         # Aravis C source (git submodule, built by CI)
├── .github/
│   ├── workflows/build-and-publish.yml  # Native builds, tests, pack, package smoke, publish
│   └── scripts/                    # build-native-linux / collect-native-* / verify-native-* / record-native-versions
│
├── build-native-linux-local.sh     # Run the CI Linux build locally into AravisSharp/runtimes/
├── check-setup.sh                  # Check a machine can run the package (.NET, GLib, libusb, udev)
├── setup-usb-permissions.sh        # Install Aravis's udev rules for USB3 Vision
├── increase-usb-buffer.sh          # Raise the usbfs memory limit
│
├── AravisSharp/                    # Library (the NuGet package)
│   ├── AravisSharp.csproj          # net8.0/net10.0, multi-RID, unsafe enabled
│   │
│   ├── Native/
│   │   ├── AravisNative.cs         # Audited hand-crafted P/Invoke (aravis-0.8)
│   │   ├── GLibNative.cs           # GLib/GObject P/Invoke (gobject-2.0, glib-2.0)
│   │   ├── AravisLibrary.cs        # DllImportResolver + platform detection
│   │   └── GErrorStructure.cs      # GError struct marshalling
│   │
│   ├── GenICam/
│   │   ├── NodeMap.cs              # Feature read/write/browse
│   │   ├── GenICamNode.cs          # Individual node wrapper
│   │   ├── FeatureDetails.cs       # Feature introspection (type, range, choices)
│   │   └── FeatureAccessMode.cs    # RO / RW / WO / NA enums
│   │
│   ├── Camera.cs                   # High-level wrapper (IDisposable)
│   ├── CameraDiscovery.cs          # Enumerate cameras
│   ├── Stream.cs                   # Video stream management
│   ├── Buffer.cs                   # Image buffer (zero-copy Span<byte>)
│   ├── Device.cs                   # Low-level GenICam device
│   ├── AravisException.cs          # Aravis-specific exceptions
│   │
│   └── Utilities/
│       ├── ImageHelper.cs          # PNG / JPEG (ImageSharp), PGM, raw export
│       └── AcquisitionStats.cs     # FPS + throughput monitor
│
├── AravisSharp.Examples/           # Demo console app (not packaged)
│   ├── AravisSharp.Examples.csproj # net8.0/net10.0
│   ├── Program.cs                  # Interactive demo menu (14 options)
│   ├── Examples.cs                 # Earlier continuous / triggered / feature access examples (namespace Examples)
│   └── Examples/
│       ├── BindingTests.cs
│       ├── CameraNetworkConfiguratorExample.cs
│       ├── ContinuousAcquisitionExample.cs
│       ├── FeatureAccessExample.cs
│       ├── FeatureBrowserExample.cs
│       ├── FeatureOverviewExample.cs
│       ├── GenICamExplorerExample.cs
│       ├── GigEDiagnosticExample.cs
│       ├── MultiCameraSoftwareTriggerCheckExample.cs
│       ├── QuickFeatureDemoExample.cs
│       ├── SimpleFeatureListerExample.cs
│       ├── SimpleNodeMapDemo.cs
│       └── TriggeredAcquisitionExample.cs
│
└── AravisSharp.Tests/              # xUnit test project (net8.0/net10.0)
    ├── AravisSharp.Tests.csproj
    ├── NativeLibraryFixture.cs     # Shared test fixture
    ├── NativeTestEnvironment.cs    # [NativeFact]: skips when libaravis-0.8 is missing
    ├── CameraTestHelpers.cs        # Picks the device tests may open (fake camera by default)
    ├── AravisLibraryTests.cs       # Native library probe order
    ├── AravisNativeTests.cs        # Tests for hand-crafted bindings
    ├── AravisNativeDiscoveryTests.cs # Tests for discovery/interface bindings
    ├── CameraWrapperTests.cs       # High-level wrapper tests
    ├── FakeCameraTests.cs          # Fake camera connect / configure / acquire
    └── ImageAcquisitionTests.cs    # Frame acquisition, PNG export, stream statistics
```

---

## Architecture

### Native Binding Layers

| Layer | File | Functions | Library |
|-------|------|-----------|---------|
| Hand-crafted | `AravisNative.cs` | Audited high-value `arv_*` functions | `aravis-0.8` |
| GLib/GObject/GIO | `GLibNative.cs` | Ref-counting (`g_object_ref/unref`), `g_error_free`, `g_free`, GObject properties, GIO address helpers | `gobject-2.0`, `glib-2.0`, `gio-2.0` |

**Key design decision**: GLib functions (`g_object_unref`, `g_error_free`) are declared in `GLibNative.cs` pointing to `gobject-2.0` / `glib-2.0`, **not** in `AravisNative.cs`. On Linux, calling them from the wrong DLL might work (the dynamic linker resolves symbols globally), but on Windows each DLL has its own export table — calling `g_object_unref` from `libaravis-0.8-0.dll` causes `EntryPointNotFoundException`.

### DllImportResolver

`AravisLibrary.cs` registers a `NativeLibrary.SetDllImportResolver` that maps logical library names to platform-specific filenames:

- `aravis-0.8` → `libaravis-0.8-0.dll` / `libaravis-0.8.so.0` / `libaravis-0.8.0.dylib`
- `gobject-2.0` → `libgobject-2.0-0.dll` / `libgobject-2.0.so.0` / `libgobject-2.0.0.dylib`
- `glib-2.0` → `libglib-2.0-0.dll` / `libglib-2.0.so.0` / `libglib-2.0.0.dylib`
- `gio-2.0` → `libgio-2.0-0.dll` / `libgio-2.0.so.0` / `libgio-2.0.0.dylib`

The resolver probes the app directory and `runtimes/{rid}/native/` (NuGet layout) first, then the system search path: a system libaravis-0.8 has the same soname, and an older one lacks entry points the binding calls. It registers itself when AravisSharp loads; `AravisLibrary.RegisterResolver()` remains callable but is optional.

### High-Level API

```
Camera ──── CreateStream() ──── Stream ──── PopBuffer() ──── Buffer
  │                                                            │
  ├── GetDevice() ──── Device ──── NodeMap                     ├── GetDataSpan()
  │                      │                                     ├── Width / Height
  ├── SetExposureTime()  ├── Get/SetStringFeature()            ├── PixelFormat
  ├── SetGain()          ├── Get/SetIntegerFeature()           └── Status / FrameId
  └── SetFrameRate()     └── GetFeatureDetails()
```

All wrapper classes use `IDisposable` and call `GLibNative.g_object_unref()` in `Dispose()`.

---

## NuGet Packages

A single `AravisSharp` package carries the managed assembly and the native runtimes under `runtimes/{rid}/native/`:

| RID | Bundled | Expected from the system |
|-----|---------|--------------------------|
| `win-x64` | `libaravis-0.8-0.dll` + all transitive DLLs (GLib, libxml2, libusb, …) | — |
| `osx-arm64` | `libaravis-0.8.0.dylib` + all non-system dylibs | — |
| `linux-x64`, `linux-arm64` | `libaravis-0.8.so.0`, with libxml2 linked in | GLib, libusb, zlib (distribution packages) |

The exact file list per platform is in `THIRD-PARTY-NOTICES.md`; CI derives it from the built binary.

---

## Build Scripts

| Script | Platform | Purpose |
|--------|----------|---------|
| `.github/scripts/build-native-linux.sh` | Linux (CI, `ubuntu:20.04` container) | Build libxml2 statically, then Aravis from the submodule |
| `.github/scripts/collect-native-{linux,windows,macos}.sh` | CI (MSYS2 MINGW64 shell on Windows) | Derive the native payload from the built binary |
| `.github/scripts/verify-native-{linux,windows,macos}.sh` | CI | Fail if anything is neither bundled nor system-provided |
| `.github/scripts/record-native-versions.sh` | CI | Write `aravis-native-versions.txt` |
| `build-native-linux-local.sh` | Linux | Run the Linux CI scripts above locally, in an `ubuntu:20.04` container when docker is available (`--host` builds on the machine, against its newer GLib), into `AravisSharp/runtimes/linux-<arch>/native/` |
| `check-setup.sh` | Linux, macOS, Windows | Check a machine can run the package: .NET 8+, and on Linux GLib, zlib, libusb, the glibc 2.31 / GLib 2.64 floors and the udev rules |
| `setup-usb-permissions.sh` | Linux | Install Aravis's udev rules (`aravis/src/aravis.rules`) for USB3 Vision cameras |
| `increase-usb-buffer.sh` | Linux | Raise the usbfs memory limit for high-speed capture |

---

## Test Suite

**Framework**: xUnit, net8.0 and net10.0

| Test File | Tests | Coverage |
|-----------|-------|----------|
| `AravisLibraryTests.cs` | 2 | Native library probe order (no native library needed) |
| `AravisNativeTests.cs` | 11 | Device enumeration, camera info, buffer allocation |
| `AravisNativeDiscoveryTests.cs` | 4 | Discovery, interface, and version bindings |
| `CameraWrapperTests.cs` | 50 | High-level camera wrapper behavior |
| `FakeCameraTests.cs` | 4 | Fake camera connect, configure, acquire |
| `ImageAcquisitionTests.cs` | 10 | Frame acquisition, PNG export, stream statistics |
| **Total** | **81** | |

Tests marked `[NativeFact]` skip when `libaravis-0.8` is not installed. The suite runs against the Aravis fake camera — in CI, with no hardware. Real hardware is opt-in: it opens a real camera only when `ARAVIS_TEST_DEVICE_ID` names it, because the tests reconfigure and stream from the camera they open.

---

## Platform Status

| Platform | Build | Camera Tested | NuGet Runtime |
|----------|-------|---------------|---------------|
| Linux x64 | ✅ | Basler acA720-520um | ✅ `linux-x64` |
| Windows x64 | ✅ | Basler acA720-520um | ✅ `win-x64` |
| Linux ARM64 | ✅ CI | Fake camera (CI package smoke) | ✅ `linux-arm64` |
| macOS ARM64 | ✅ CI | Fake camera (CI package smoke) | ✅ `osx-arm64` |
| macOS x64 | — | — | ❌ not built (use Homebrew Aravis) |

---

## Dependencies

### Build-Time
- .NET 10.0 SDK
- SixLabors.ImageSharp 3.1.12 (NuGet)

### Runtime (Linux)
- `libaravis-0.8.so.0` — from system package or NuGet
- `libglib-2.0.so.0`, `libgobject-2.0.so.0`, `libgio-2.0.so.0`, `libgmodule-2.0.so.0` — from `libglib2.0-0`
- `libxml2.so` — from `libxml2` (system Aravis only: the NuGet build links it in)
- `libusb-1.0.so.0` — from `libusb-1.0-0`
- `libz.so` — from `zlib1g`

### Runtime (Windows)
- All DLLs bundled in the `AravisSharp` NuGet package (`runtimes/win-x64/native/`)
- USB3 Vision cameras require WinUSB driver (Zadig)
