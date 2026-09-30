# AravisSharp — Cross-Platform .NET Bindings for Aravis

**AravisSharp** is a C# binding for the [Aravis](https://github.com/AravisProject/aravis) industrial camera library, targeting the stable **Aravis 0.8.36 / libaravis-0.8** ABI and supporting **USB3 Vision** and **GigE Vision** cameras on Windows, Linux, and macOS.

> Think of it as an open-source alternative to vendor SDKs like Basler Pylon — one library, any GenICam camera.

## Platform Support

| Platform | Runtime ID | Status | Tested Camera |
|----------|-----------|--------|---------------|
| **Linux x64** | `linux-x64` | ✅ Full support | Basler acA720-520um |
| **Linux ARM64** | `linux-arm64` | ✅ Bundled, CI smoke-tested | — |
| **Windows x64** | `win-x64` | ✅ Full support | Basler acA720-520um |
| **macOS ARM64** | `osx-arm64` | ✅ Bundled, CI smoke-tested | — |
| **macOS x64** | `osx-x64` | ⚠️ Not built — use a Homebrew Aravis | — |

On every bundled runtime, CI restores the packed `.nupkg` and acquires frames from the Aravis fake camera through it.

## Quick Start

### 1. Install Dependencies

<details>
<summary><strong>🐧 Linux (Ubuntu / Debian)</strong></summary>

**Option A — System package** (easiest for development):
```bash
sudo apt update
sudo apt install -y libaravis-0.8-0
```

**Option B — NuGet package** (bundles Aravis itself):
```bash
dotnet add package AravisSharp
sudo apt install -y libglib2.0-0 libusb-1.0-0   # usually already present
```

**Option C — Build the packaged Aravis 0.8.36 from the submodule** (the CI steps, in an `ubuntu:20.04` container when docker is available):
```bash
./build-native-linux-local.sh   # into AravisSharp/runtimes/linux-<arch>/native/, which dotnet pack bundles
# A project reference does not copy it to the output: point the loader at it
LD_LIBRARY_PATH=$PWD/AravisSharp/runtimes/linux-x64/native dotnet run --project <your app>
```

**Check the runtime requirements** (.NET, the libraries below, glibc / GLib floors, udev rules):
```bash
./check-setup.sh
```

The NuGet package's `libaravis-0.8.so.0` has libxml2 linked in and needs glibc 2.31 and GLib 2.64 or later (Ubuntu 20.04, Debian 11 and newer). It loads these distribution libraries, usually pre-installed on desktop Ubuntu:

| Library | Package (apt) |
|---------|---------------|
| `libglib-2.0.so.0`, `libgobject-2.0.so.0`, `libgio-2.0.so.0` | `libglib2.0-0` (`libglib2.0-0t64` from Ubuntu 24.04) |
| `libusb-1.0.so.0` | `libusb-1.0-0` (libaravis depends on it, so GigE cameras need it too) |
| `libz.so.1` | `zlib1g` |

A system-wide `libaravis-0.8-0` pulls in its own dependencies, libxml2 included.

**USB3 Vision cameras — udev permissions:**
```bash
./setup-usb-permissions.sh
# Installs the udev rules shipped with Aravis; then unplug and replug the camera
```

</details>

<details>
<summary><strong>🪟 Windows</strong></summary>

```powershell
dotnet add package AravisSharp
```

The NuGet package bundles **all** required native DLLs (`libaravis-0.8-0.dll`, GLib, GObject, libxml2, libusb, zlib, …) — no system-wide install necessary.

**USB3 Vision cameras require the WinUSB driver** (one-time setup via [Zadig](https://zadig.akeo.ie/)), with or without a vendor SDK installed. A USB camera switched to WinUSB is no longer visible to the vendor SDK (pylon, …); other cameras are not affected. GigE cameras work without driver changes and alongside vendor SDKs. See [WINDOWS_SETUP.md](https://github.com/aboccag/AravisSharp/blob/main/WINDOWS_SETUP.md#using-aravissharp-alongside-a-vendor-sdk-pylon-spinnaker-) for step-by-step instructions and what can coexist.

</details>

<details>
<summary><strong>🍎 macOS</strong></summary>

On Apple Silicon the NuGet package bundles Aravis and every non-system dylib it needs:

```bash
dotnet add package AravisSharp
```

Intel Macs are not covered by the package (`osx-x64` is not built); install Aravis with Homebrew instead:

```bash
brew install aravis
```

</details>

### 2. Build & Run the Examples

`AravisSharp` is a library; the interactive demo menu is the [`AravisSharp.Examples`](https://github.com/aboccag/AravisSharp/tree/main/AravisSharp.Examples) project. From a clone of the repository:

```bash
dotnet run --project AravisSharp.Examples -f net10.0
```

The examples never pick a camera on their own: they list the cameras and ask which one to open, since on a shared network the first camera Aravis finds may belong to another application. To skip the prompt, name the device ID with `--device`, or set `ARAVIS_EXAMPLE_DEVICE_ID`:

```bash
dotnet run --project AravisSharp.Examples -f net10.0 -- --device "<device id>"
```

## Example Code

```csharp
using AravisSharp;
using AravisSharp.Native;

// Optional: the native library resolver registers itself when AravisSharp loads
AravisLibrary.RegisterResolver();

// Discover cameras
CameraDiscovery.UpdateDeviceList();
var cameras = CameraDiscovery.DiscoverCameras();
foreach (var cam in cameras)
    Console.WriteLine(cam);

// Open the first camera Aravis finds; on a shared network, pass a DeviceId from
// the list instead, as another application's camera may enumerate first
using var camera = new Camera();

// Read device info
Console.WriteLine(camera.GetVendorName());
Console.WriteLine(camera.GetModelName());

// Configure
camera.SetExposureTime(10000);   // 10 ms
camera.SetGain(5.0);
camera.SetRegion(0, 0, 640, 480);

// Access GenICam features via the NodeMap
using var device = camera.GetDevice();
var nodeMap = device.NodeMap;
nodeMap.SetStringFeature("PixelFormat", "Mono8");
long width = nodeMap.GetIntegerFeature("Width");

// Acquire a single frame: starts and stops acquisition itself (timeout in µs)
using var buffer = camera.AcquireSingleFrame(5_000_000);
if (buffer.Status == ArvBufferStatus.Success)
{
    var data = buffer.GetDataSpan();   // zero-copy
    Console.WriteLine($"Frame {buffer.FrameId}: {buffer.Width}x{buffer.Height}, {data.Length} bytes");
}
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
│   ├── ImageHelper.cs         # PNG / JPEG (ImageSharp), PGM and raw export
│   └── AcquisitionStats.cs    # Real-time FPS & throughput monitor
AravisSharp.Examples/          # Interactive demo menu (Program.cs + Examples/)
AravisSharp.Tests/             # xUnit suite, runs against the Aravis fake camera
```

### Native Library Resolution

AravisSharp uses a `NativeLibrary.SetDllImportResolver` to map logical library names to platform-specific files at runtime:

| Logical Name | Windows | Linux | macOS |
|-------------|---------|-------|-------|
| `aravis-0.8` | `libaravis-0.8-0.dll` | `libaravis-0.8.so.0` | `libaravis-0.8.0.dylib` |
| `gobject-2.0` | `libgobject-2.0-0.dll` | `libgobject-2.0.so.0` | `libgobject-2.0.0.dylib` |
| `glib-2.0` | `libglib-2.0-0.dll` | `libglib-2.0.so.0` | `libglib-2.0.0.dylib` |
| `gio-2.0` | `libgio-2.0-0.dll` | `libgio-2.0.so.0` | `libgio-2.0.0.dylib` |

The resolver tries the copies shipped with the application first — its directory, then `runtimes/{rid}/native/` (NuGet layout) — and falls back to the system search path, so a system-wide Aravis never silently replaces the bundled one.

## NuGet Packages

A single `AravisSharp` package carries the managed library and the native runtimes under `runtimes/{rid}/native/`:

| RID | Bundled | Expected from the system |
|-----|---------|--------------------------|
| `win-x64` | `libaravis-0.8-0.dll` + all transitive DLLs (GLib, libxml2, libusb, …) | — |
| `osx-arm64` | `libaravis-0.8.0.dylib` + all non-system dylibs | — |
| `linux-x64`, `linux-arm64` | `libaravis-0.8.so.0`, with libxml2 linked in | GLib, libusb, zlib (distribution packages) |

On **Linux**, only `libaravis` is bundled: a second GLib next to the system one (loaded by GStreamer or GTK, for example) would put two GObject type systems in one process. libxml2 is linked into it, with its symbols hidden, because distributions disagree on its soname (`libxml2.so.2` up to Ubuntu 24.04, `libxml2.so.16` from 26.04). The Linux build needs glibc 2.29 and GLib 2.64 or later: Ubuntu 20.04, Debian 11, JetPack 5 and newer; CI runs it on Ubuntu 20.04, 22.04 and 26.04.

The bundled libraries keep their own licenses: see [THIRD-PARTY-NOTICES.md](https://github.com/aboccag/AravisSharp/blob/main/THIRD-PARTY-NOTICES.md). Each `runtimes/{rid}/native/aravis-native-versions.txt` records the exact versions shipped.

### Versioning

AravisSharp package versions intentionally start with the target native Aravis release. A package or tag named `v0.8.36` targets Aravis `0.8.36` / `libaravis-0.8`; same-native fixes use a fourth component such as `v0.8.36.1`.

Use `v0.8.36.N` for managed binding fixes, bundled dependency updates, or native security backports that keep the public Aravis target at `0.8.36`. If a security issue affects every supported native line, release one patched package per supported line, for example `v0.8.36.1` and `v0.8.35.2`, so consumers can update without changing native ABI expectations. When the upstream Aravis version changes, start a new line such as `v0.8.37`.

## Features

- **Camera discovery** — enumerate USB3 Vision and GigE Vision devices
- **High-level API** — `Camera`, `Stream`, `Buffer` with `IDisposable` and proper GObject ref-counting
- **GenICam feature browser** — introspect features with type, access mode, range, and enumeration choices
- **Audited native surface** — high-value 0.8.36 calls are hand checked; broad GIR generation is kept out of the public API until regenerated safely
- **Hand-crafted bindings** — curated, documented, with correct error handling
- **Zero-copy image access** — `ReadOnlySpan<byte>` via `buffer.GetDataSpan()`
- **Image export** — PNG / JPEG via SixLabors.ImageSharp, raw / PGM via `ImageHelper`
- **Performance monitoring** — `AcquisitionStats` for real-time FPS and throughput
- **Cross-platform** — single codebase, platform-specific loading at runtime

## Documentation

| Guide | Description |
|-------|-------------|
| [QUICKSTART.md](https://github.com/aboccag/AravisSharp/blob/main/QUICKSTART.md) | Install, connect, capture — quick reference |
| [WINDOWS_SETUP.md](https://github.com/aboccag/AravisSharp/blob/main/WINDOWS_SETUP.md) | Windows installation + WinUSB driver setup |
| [CROSS_PLATFORM_GUIDE.md](https://github.com/aboccag/AravisSharp/blob/main/CROSS_PLATFORM_GUIDE.md) | NuGet packaging, Docker, distribution strategies |
| [FEATURE_BROWSER_GUIDE.md](https://github.com/aboccag/AravisSharp/blob/main/FEATURE_BROWSER_GUIDE.md) | GenICam feature introspection & interactive browser |
| [PROJECT_SUMMARY.md](https://github.com/aboccag/AravisSharp/blob/main/PROJECT_SUMMARY.md) | Project architecture and status |

## Build Requirements

| Requirement | Version |
|-------------|---------|
| .NET SDK | **10.0** |
| Aravis | **0.8.36 target** (`libaravis-0.8`; compatible with the 0.8 ABI) |
| SixLabors.ImageSharp | 3.1.12 (NuGet) |

## License

AravisSharp is licensed under the [Mozilla Public License 2.0](https://github.com/aboccag/AravisSharp/blob/main/LICENSE): you can use it in proprietary applications, and changes to AravisSharp's own files must be published under the MPL-2.0.

The native libraries bundled in the package keep their own licenses. Aravis is licensed under [LGPL-2.1-or-later](https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html), which permits use in proprietary applications as long as the LGPL library can be replaced by the end user; the package ships it as separate files under `runtimes/`. See [THIRD-PARTY-NOTICES.md](https://github.com/aboccag/AravisSharp/blob/main/THIRD-PARTY-NOTICES.md).
