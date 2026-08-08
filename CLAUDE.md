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

**Native library resolution**: `AravisLibrary.RegisterResolver()` must be called once before any P/Invoke. It registers a `NativeLibrary.SetDllImportResolver` that maps logical names (`aravis-0.8`, `gobject-2.0`, `glib-2.0`, `gio-2.0`) to platform-specific filenames, probing system paths first and then `runtimes/{rid}/native/` (NuGet layout).

**GError pattern**: Every P/Invoke that can fail takes an `out IntPtr error` parameter. The wrappers check for non-zero error pointers, extract the message, and call `g_error_free` before throwing `AravisException`.

**Fake camera**: Aravis ships a software fake camera (`Protocol: "Fake"`). `FakeCameraTests.cs` runs against it — these are the CI-gated integration tests that require no hardware. Always keep them passing.

### Versioning scheme

Package versions mirror the native Aravis target: `v0.8.36` targets `libaravis-0.8` ABI at version 0.8.36. Managed-only fixes use a fourth component (`v0.8.36.1`). Controlled by `AravisNativeVersion` and `AravisSharpPatchVersion` properties in [AravisSharp.csproj](AravisSharp/AravisSharp.csproj). The CI pipeline validates that the `aravis/` submodule version matches `AravisNativeVersion` on every build.

### CI pipeline (`.github/workflows/build-and-publish.yml`)

Three jobs run in order:
1. **build-native** — builds Aravis from the submodule on each platform (linux-x64, linux-arm64, osx-arm64, win-x64) using Meson/Ninja, collects `.so`/`.dll`/`.dylib` files with their transitive deps, and patches rpaths/install names for self-contained bundling.
2. **test-dotnet** — installs Aravis system-wide on Ubuntu, runs `FakeCameraTests` (no hardware required).
3. **pack-and-publish** — downloads all native artifacts, arranges them under `AravisSharp/runtimes/{rid}/native/`, then calls `dotnet pack`. NuGet publish is currently disabled in the workflow.

Windows builds use MSYS2/MinGW64. macOS builds use Homebrew and `install_name_tool` to rewrite dylib load paths.
