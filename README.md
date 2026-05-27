# AravisSharp — Cross-Platform .NET Bindings for Aravis

**AravisSharp** is a C# binding for the [Aravis](https://github.com/AravisProject/aravis) industrial camera library, targeting the stable **Aravis 0.8.36 / libaravis-0.8** ABI and supporting **USB3 Vision** and **GigE Vision** cameras on Windows, Linux, and macOS.

> Think of it as an open-source alternative to vendor SDKs like Basler Pylon — one library, any GenICam camera.

## Platform Support

| Platform | Runtime ID | Status | Tested Camera |
|----------|-----------|--------|---------------|
| **Linux x64** | `linux-x64` | ✅ Full support | Basler acA720-520um |
| **Linux ARM64** | `linux-arm64` | ✅ Compatible | — |
| **Windows x64** | `win-x64` | ✅ Full support | Basler acA720-520um |
| **macOS x64/ARM** | `osx-x64` | ⚠️ Untested | — |

## Quick Start

### 1. Install Dependencies

<details>
<summary><strong>🐧 Linux (Ubuntu / Debian)</strong></summary>

**Option A — System package** (easiest for development):
```bash
sudo apt update
sudo apt install -y libaravis-0.8-0
```

**Option B — NuGet runtime package** (no system install needed):
```bash
dotnet add package AravisSharp.runtime.linux-x64
```

**Option C — Build Aravis 0.8.36 from source:**
```bash
./build_aravis_linux_nuget.sh
```

**Verify all runtime deps are present:**
```bash
./check-setup.sh
```

The following shared libraries are required at runtime (most are pre-installed on desktop Ubuntu):

| Library | Package (apt) |
|---------|---------------|
| `libaravis-0.8.so.0` | `libaravis-0.8-0` or NuGet |
| `libglib-2.0.so.0` | `libglib2.0-0` |
| `libgobject-2.0.so.0` | `libglib2.0-0` |
| `libgio-2.0.so.0` | `libglib2.0-0` |
| `libgmodule-2.0.so.0` | `libglib2.0-0` |
| `libxml2.so` | `libxml2` |
| `libusb-1.0.so.0` | `libusb-1.0-0` |
| `libz.so` | `zlib1g` |

**USB3 Vision cameras — udev permissions:**
```bash
./setup-usb-permissions.sh
# Log out and back in for group changes to take effect
```

</details>

<details>
<summary><strong>🪟 Windows</strong></summary>

```powershell
dotnet add package AravisSharp.runtime.win-x64
```

The NuGet package bundles **all** required native DLLs (`libaravis-0.8-0.dll`, GLib, GObject, libxml2, libusb, zlib, …) — no system-wide install necessary.

**USB3 Vision cameras require the WinUSB driver** (one-time setup via [Zadig](https://zadig.akeo.ie/)). GigE cameras work without driver changes. See [WINDOWS_SETUP.md](WINDOWS_SETUP.md) for step-by-step instructions.

</details>

<details>
<summary><strong>🍎 macOS</strong></summary>

```bash
brew install aravis
```

</details>

### 2. Build & Run

```bash
cd AravisSharp
dotnet run
```

## Example Code

```csharp
using AravisSharp;
using AravisSharp.Native;

// Register the cross-platform native library resolver (call once at startup)
AravisLibrary.RegisterResolver();

// Discover cameras
CameraDiscovery.UpdateDeviceList();
var cameras = CameraDiscovery.DiscoverCameras();
foreach (var cam in cameras)
    Console.WriteLine(cam);

// Open the first camera
using var camera = new Camera();

// Read device info
Console.WriteLine(camera.GetVendorName());
Console.WriteLine(camera.GetModelName());

// Configure
camera.SetExposureTime(10000);   // 10 ms
camera.SetGain(5.0);
camera.SetRegion(0, 0, 640, 480);

// Access GenICam features via the NodeMap
var device = camera.GetDevice();
var nodeMap = device.NodeMap;
nodeMap.SetStringFeature("PixelFormat", "Mono8");
long width = nodeMap.GetIntegerFeature("Width");

// Acquire a single frame
camera.StartAcquisition();
using var buffer = camera.AcquireImage(timeout: 5000);
if (buffer?.Status == ArvBufferStatus.Success)
{
    var data = buffer.GetDataSpan();   // zero-copy
    Console.WriteLine($"Frame {buffer.FrameId}: {buffer.Width}x{buffer.Height}, {data.Length} bytes");
}
camera.StopAcquisition();
```

## Architecture

```
AravisSharp/
├── Native/
│   ├── AravisNative.cs        # Audited hand-written P/Invoke bindings (aravis-0.8)
│   ├── GLibNative.cs          # GLib / GObject P/Invoke (gobject-2.0, glib-2.0)
│   ├── AravisLibrary.cs       # Cross-platform DllImportResolver
│   └── GErrorStructure.cs     # GError marshalling
├── GenICam/
│   ├── NodeMap.cs             # GenICam feature access (read/write/browse)
│   ├── GenICamNode.cs         # Individual node wrapper
│   ├── FeatureDetails.cs      # Feature introspection (type, range, choices)
│   └── FeatureAccessMode.cs   # RO / RW / WO / NA enums
├── Camera.cs                  # High-level camera wrapper
├── CameraDiscovery.cs         # Device enumeration
├── Stream.cs                  # Video stream management
├── Buffer.cs                  # Image buffer (zero-copy via Span<byte>)
├── Device.cs                  # Low-level GenICam device access
├── AravisException.cs         # Aravis-specific exceptions
├── Utilities/
│   ├── ImageHelper.cs         # Image format helpers (save PGM / raw)
│   └── AcquisitionStats.cs    # Real-time FPS & throughput monitor
└── Examples/                  # Interactive demo examples
```

### Native Library Resolution

AravisSharp uses a `NativeLibrary.SetDllImportResolver` to map logical library names to platform-specific files at runtime:

| Logical Name | Windows | Linux | macOS |
|-------------|---------|-------|-------|
| `aravis-0.8` | `libaravis-0.8-0.dll` | `libaravis-0.8.so.0` | `libaravis-0.8.dylib` |
| `gobject-2.0` | `libgobject-2.0-0.dll` | `libgobject-2.0.so.0` | `libgobject-2.0.dylib` |
| `glib-2.0` | `libglib-2.0-0.dll` | `libglib-2.0.so.0` | `libglib-2.0.dylib` |

The resolver probes system paths first, then falls back to `runtimes/{rid}/native/` (NuGet layout).

## NuGet Packages

| Package | Contents |
|---------|----------|
| `AravisSharp` | Managed library (Camera, Stream, Buffer, NodeMap, …) |
| `AravisSharp.runtime.win-x64` | `libaravis-0.8-0.dll` + all transitive DLLs (GLib, libxml2, …) |
| `AravisSharp.runtime.linux-x64` | `libaravis-0.8.so.0` (GLib etc. come from OS packages) |

On **Windows**, the runtime package includes every dependency. On **Linux**, only `libaravis` is bundled — GLib / libxml2 / libusb / zlib are expected from the OS package manager.

## Features

- **Camera discovery** — enumerate USB3 Vision and GigE Vision devices
- **High-level API** — `Camera`, `Stream`, `Buffer` with `IDisposable` and proper GObject ref-counting
- **GenICam feature browser** — introspect features with type, access mode, range, and enumeration choices
- **Audited native surface** — high-value 0.8.36 calls are hand checked; broad GIR generation is kept out of the public API until regenerated safely
- **80 hand-crafted bindings** — curated, documented, with correct error handling
- **Zero-copy image access** — `ReadOnlySpan<byte>` via `buffer.GetDataSpan()`
- **Image export** — PNG / JPEG via SixLabors.ImageSharp, raw / PGM via `ImageHelper`
- **Performance monitoring** — `AcquisitionStats` for real-time FPS and throughput
- **Cross-platform** — single codebase, platform-specific loading at runtime

## Documentation

| Guide | Description |
|-------|-------------|
| [QUICKSTART.md](QUICKSTART.md) | Install, connect, capture — quick reference |
| [WINDOWS_SETUP.md](WINDOWS_SETUP.md) | Windows installation + WinUSB driver setup |
| [CROSS_PLATFORM_GUIDE.md](CROSS_PLATFORM_GUIDE.md) | NuGet packaging, Docker, distribution strategies |
| [FEATURE_BROWSER_GUIDE.md](FEATURE_BROWSER_GUIDE.md) | GenICam feature introspection & interactive browser |
| [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) | Project architecture and status |

## Build Requirements

| Requirement | Version |
|-------------|---------|
| .NET SDK | **10.0** |
| Aravis | **0.8.36 target** (`libaravis-0.8`; compatible with the 0.8 ABI) |
| SixLabors.ImageSharp | 3.1.12 (NuGet) |

## License

Aravis is licensed under [LGPL-2.1-or-later](https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html), which permits use in proprietary applications as long as the LGPL library can be replaced by the end user. AravisSharp license TBD.
