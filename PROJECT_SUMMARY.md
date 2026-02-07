# AravisSharp — Project Summary

## Overview

AravisSharp provides complete C# (.NET 10.0) bindings for the [Aravis](https://github.com/AravisProject/aravis) industrial camera library. It supports USB3 Vision and GigE Vision cameras on Windows, Linux, and macOS through a cross-platform `DllImportResolver` and NuGet runtime packages.

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
│
├── build_aravis_linux_nuget.sh     # Build Aravis 0.8.33 for linux-x64 NuGet
├── copy-aravis-dlls.ps1            # Extract Windows DLLs from MSYS2
├── check-setup.sh                  # Verify 8 Linux runtime dependencies
├── setup-usb-permissions.sh        # Configure udev rules for USB3 Vision
├── increase-usb-buffer.sh          # Increase USB buffer size
├── generate-bindings.py            # Auto-generate P/Invoke from GIR data
│
├── AravisSharp/                    # Main library + demo app
│   ├── AravisSharp.csproj          # .NET 10.0, multi-RID, unsafe enabled
│   ├── Program.cs                  # Interactive demo menu (8 options)
│   │
│   ├── Native/
│   │   ├── AravisNative.cs         # ~80 hand-crafted P/Invoke (aravis-0.8)
│   │   ├── GLibNative.cs           # GLib/GObject P/Invoke (gobject-2.0, glib-2.0)
│   │   ├── AravisLibrary.cs        # DllImportResolver + platform detection
│   │   └── GErrorStructure.cs      # GError struct marshalling
│   │
│   ├── Generated/
│   │   └── AravisGenerated.cs      # 475 auto-generated bindings from GIR
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
│   ├── Utilities/
│   │   ├── ImageHelper.cs          # Image format helpers (PGM, raw)
│   │   └── AcquisitionStats.cs     # FPS + throughput monitor
│   │
│   └── Examples/
│       ├── FeatureBrowserExample.cs
│       ├── FeatureOverviewExample.cs
│       ├── GenICamExplorerExample.cs
│       ├── QuickFeatureDemoExample.cs
│       ├── SimpleFeatureListerExample.cs
│       └── SimpleNodeMapDemo.cs
│
└── AravisSharp.Tests/              # xUnit test project
    ├── AravisSharp.Tests.csproj
    ├── NativeLibraryFixture.cs     # Shared test fixture
    ├── AravisNativeTests.cs        # Tests for hand-crafted bindings
    ├── AravisGeneratedTests.cs     # Tests for auto-generated bindings
    └── BindingCompatibilityTests.cs # Cross-check manual vs generated
```

---

## Architecture

### Native Binding Layers

| Layer | File | Functions | Library |
|-------|------|-----------|---------|
| Hand-crafted | `AravisNative.cs` | ~80 `arv_*` functions | `aravis-0.8` |
| Auto-generated | `AravisGenerated.cs` | 475 `arv_*` functions | `aravis-0.8` |
| GLib/GObject | `GLibNative.cs` | 5 functions (`g_object_ref/unref`, `g_error_free`, `g_clear_error`, `g_free`) | `gobject-2.0`, `glib-2.0` |

**Key design decision**: GLib functions (`g_object_unref`, `g_error_free`) are declared in `GLibNative.cs` pointing to `gobject-2.0` / `glib-2.0`, **not** in `AravisNative.cs`. On Linux, calling them from the wrong DLL might work (the dynamic linker resolves symbols globally), but on Windows each DLL has its own export table — calling `g_object_unref` from `libaravis-0.8-0.dll` causes `EntryPointNotFoundException`.

### DllImportResolver

`AravisLibrary.cs` registers a `NativeLibrary.SetDllImportResolver` that maps logical library names to platform-specific filenames:

- `aravis-0.8` → `libaravis-0.8-0.dll` / `libaravis-0.8.so.0` / `libaravis-0.8.dylib`
- `gobject-2.0` → `libgobject-2.0-0.dll` / `libgobject-2.0.so.0` / `libgobject-2.0.dylib`
- `glib-2.0` → `libglib-2.0-0.dll` / `libglib-2.0.so.0` / `libglib-2.0.dylib`

The resolver probes system paths first, then `runtimes/{rid}/native/` (NuGet layout).

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

| Package | Contents |
|---------|----------|
| `AravisSharp` | Managed assembly |
| `AravisSharp.runtime.win-x64` | `libaravis-0.8-0.dll` + 11 transitive DLLs |
| `AravisSharp.runtime.linux-x64` | `libaravis-0.8.so.0` (OS provides GLib etc.) |

---

## Build Scripts

| Script | Platform | Purpose |
|--------|----------|---------|
| `build_aravis_linux_nuget.sh` | Linux | Build Aravis 0.8.33 from source, stage `libaravis-0.8.so.0` into NuGet layout |
| `copy-aravis-dlls.ps1` | Windows | Extract Aravis + transitive DLLs from MSYS2 into NuGet layout |
| `check-setup.sh` | Linux | Verify all 8 runtime shared libraries are present |
| `setup-usb-permissions.sh` | Linux | Configure udev rules for USB3 Vision cameras |
| `increase-usb-buffer.sh` | Linux | Increase USB buffer size for high-speed capture |
| `generate-bindings.py` | Any | Parse GIR XML and generate `AravisGenerated.cs` |

---

## Test Suite

**Framework**: xUnit, .NET 10.0

| Test File | Tests | Coverage |
|-----------|-------|----------|
| `AravisNativeTests.cs` | 11 | Device enumeration, camera info, buffer allocation |
| `AravisGeneratedTests.cs` | 11 | Generated binding validation, function count |
| `BindingCompatibilityTests.cs` | 7 | Manual vs generated binding consistency |
| **Total** | **29** | |

Tests auto-skip camera-specific checks if no camera is connected.

---

## Platform Status

| Platform | Build | Camera Tested | NuGet Runtime |
|----------|-------|---------------|---------------|
| Linux x64 | ✅ | Basler acA720-520um | ✅ `linux-x64` |
| Windows x64 | ✅ | Basler acA720-520um | ✅ `win-x64` |
| Linux ARM64 | ✅ (untested) | — | ⏳ planned |
| macOS | ✅ (untested) | — | ⏳ planned |

---

## Dependencies

### Build-Time
- .NET 10.0 SDK
- SixLabors.ImageSharp 3.1.12 (NuGet)

### Runtime (Linux)
- `libaravis-0.8.so.0` — from system package or NuGet
- `libglib-2.0.so.0`, `libgobject-2.0.so.0`, `libgio-2.0.so.0`, `libgmodule-2.0.so.0` — from `libglib2.0-0`
- `libxml2.so` — from `libxml2`
- `libusb-1.0.so.0` — from `libusb-1.0-0`
- `libz.so` — from `zlib1g`

### Runtime (Windows)
- All DLLs bundled in `AravisSharp.runtime.win-x64` NuGet package
- USB3 Vision cameras require WinUSB driver (Zadig)
