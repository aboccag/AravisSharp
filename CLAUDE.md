# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AravisSharp is a C# binding for the [Aravis](https://github.com/AravisProject/aravis) industrial camera library. It targets the Aravis 0.8.36 / `libaravis-0.8` ABI and supports USB3 Vision and GigE Vision cameras on Windows, Linux, and macOS.

The `aravis/` directory is a **git submodule** containing the Aravis C source. It is built into native libraries by CI — do not modify it.

## Build & Test Commands

```bash
# Build the managed library
dotnet build AravisSharp/

# Build the entire solution
dotnet build AravisSharp.slnx

# Run all tests
dotnet test AravisSharp.Tests/

# Run tests with verbose output
dotnet test AravisSharp.Tests/ --logger "console;verbosity=detailed"

# Run a specific test class
dotnet test AravisSharp.Tests/ --filter "FullyQualifiedName~FakeCameraTests"

# Run a single test method
dotnet test AravisSharp.Tests/ --filter "FullyQualifiedName~FakeCameraTests.FakeCamera_ShouldAcquireImageBuffer"

# Pack the NuGet package (requires native runtimes/ to be present for bundling)
dotnet pack AravisSharp/AravisSharp.csproj -c Release -o _packages/
```

The test project targets `net8.0;net10.0`. Set `TestTfmsInParallel=false` is already configured to prevent parallel TFM execution (a known flakiness source on ARM64).

## Prerequisites for Running Tests

Tests require the native Aravis library to be installed. Tests decorated with `[NativeFact]` skip gracefully when `libaravis-0.8` is not available.

```bash
# Linux: install system-wide
sudo apt install libaravis-0.8-0

# Or build from the submodule
./build_aravis_linux_nuget.sh

# Linux USB3 cameras: set udev permissions
./setup-usb-permissions.sh   # then log out and back in

# Check all runtime dependencies are satisfied
./check-setup.sh
```

## Architecture

### Layer structure

```
AravisSharp/
├── Native/           ← P/Invoke surface (do not call these from user code)
│   ├── AravisNative.cs     # Hand-crafted DllImport declarations (aravis-0.8 ABI)
│   ├── GLibNative.cs       # GLib/GObject P/Invoke (ref-counting, GError)
│   ├── AravisLibrary.cs    # Cross-platform DLL resolver (call RegisterResolver() once at startup)
│   └── GErrorStructure.cs  # GError marshalling struct
├── GenICam/          ← GenICam feature access layer
│   ├── NodeMap.cs          # Read/write/browse all camera features
│   ├── GenICamNode.cs      # Individual feature node wrapper
│   └── FeatureDetails.cs   # Feature introspection (type, range, choices, access mode)
├── Camera.cs         ← High-level camera API (most code should use only this)
├── CameraDiscovery.cs ← Device enumeration
├── Stream.cs         ← Video stream management (PushBuffer / PopBuffer)
├── Buffer.cs         ← Image buffer with zero-copy Span<byte> access
├── Device.cs         ← Low-level device; exposes NodeMap
└── Utilities/
    ├── ImageHelper.cs      # PNG/JPEG (via ImageSharp), raw, and PGM export
    └── AcquisitionStats.cs # Real-time FPS and throughput monitoring
```

### Key design rules

**GObject ownership**: Aravis objects are GObject reference-counted. `Camera`, `Stream`, and `Buffer` all implement `IDisposable` and call `g_object_unref` on dispose. Never store raw `IntPtr` handles beyond the lifetime of the owning wrapper.

**Native library resolution**: `AravisLibrary.RegisterResolver()` must be called once before any P/Invoke. It registers a `NativeLibrary.SetDllImportResolver` that maps logical names (`aravis-0.8`, `gobject-2.0`, `glib-2.0`, `gio-2.0`) to platform-specific filenames, probing the app directory and `runtimes/{rid}/native/` (NuGet layout) first and the system search path last (`AravisLibrary.GetProbeOrder`). Bundled first is deliberate: a system libaravis-0.8 has the same soname, and an older one lacks entry points the binding calls.

**GError pattern**: Every P/Invoke that can fail takes an `out IntPtr error` parameter. The wrappers check for non-zero error pointers, extract the message, and call `g_error_free` before throwing `AravisException`.

**Fake camera**: Aravis ships a software fake camera (`Protocol: "Fake"`). The whole test suite runs against it in CI, no hardware required. Always keep it passing.

**Real hardware is opt-in**: tests reconfigure and stream from the camera they open, and GigE cameras on a shared network may belong to another application. The suite opens only the fake camera unless `ARAVIS_TEST_DEVICE_ID` names a device; route every camera open in tests through `CameraTestHelpers.ResolveTestDeviceId()`, never `new Camera(null)` or "first non-fake camera".

### Versioning scheme

Package versions mirror the native Aravis target: `v0.8.36` targets `libaravis-0.8` ABI at version 0.8.36. Managed-only fixes use a fourth component (`v0.8.36.1`). Controlled by `AravisNativeVersion` and `AravisSharpPatchVersion` properties in [AravisSharp.csproj](AravisSharp/AravisSharp.csproj). The CI pipeline validates that the `aravis/` submodule version matches `AravisNativeVersion` on every build.

### CI pipeline (`.github/workflows/build-and-publish.yml`)

Jobs, in order:
1. **build-native** — builds Aravis from the submodule on osx-arm64 and win-x64 using Meson/Ninja; **build-native-linux** does linux-x64 and linux-arm64 in an `ubuntu:20.04` container, because Aravis picks GLib APIs at compile time (a GLib 2.72 build dies on GLib 2.66 with `undefined symbol: g_memdup2`), and links libxml2 in statically (`build-libxml2-static.sh`) because its soname is `.so.2` up to Ubuntu 24.04 and `.so.16` from 26.04. `.github/scripts/collect-native-<os>.sh` derives the payload from the built binary (ldd on MSYS2, a recursive otool walk on macOS; Linux ships `libaravis-0.8.so.0` only), `verify-native-<os>.sh` fails closed if anything is not bundled or system-provided, and `record-native-versions.sh` writes `aravis-native-versions.txt`. Never hardcode dependency lists: that is how v0.8.36 shipped without libxml2 on win-x64 and osx-arm64.
2. **test-dotnet** — installs Aravis system-wide on Ubuntu and runs the whole suite against the fake camera.
3. **pack** — arranges the native artifacts under `AravisSharp/runtimes/{rid}/native/`, refuses any bundled file missing from `THIRD-PARTY-NOTICES.md`, then calls `dotnet pack`.
4. **package-smoke** — restores the `.nupkg` on each of the four runners and acquires frames from the fake camera through it (`.github/package-smoke/`), checking libaravis loads from the app's `runtimes/` folder. **package-smoke-distros** runs the self-contained app in bare `ubuntu:20.04` (the floor) and `ubuntu:26.04` (newest LTS) images on both Linux architectures.
5. **publish** — pushes to NuGet.org on release tags, only when the `NUGET_PUBLISH` repository variable is `true`.

Windows builds use MSYS2/MinGW64. macOS builds use Homebrew, `install_name_tool` to rewrite dylib load paths and ad-hoc `codesign`. The scripts run off CI too (the Windows ones in an MSYS2 MINGW64 shell).
