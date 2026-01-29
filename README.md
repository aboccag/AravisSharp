# AravisSharp - Cross-Platform .NET Bindings for Aravis

Industrial camera library for .NET supporting USB3Vision and GigE Vision cameras on Windows, Linux (x64/ARM), and macOS.

## Platform Support

| Platform | Status | Tested |
|----------|--------|--------|
| **Linux x64** | ✅ Full Support | ✅ Yes |
| **Linux ARM64** | ✅ Full Support | ⏳ Compatible |
| **Linux ARMv7** | ✅ Full Support | ⏳ Compatible |
| **Windows x64** | ✅ Compatible | ⏳ Not tested |
| **macOS x64/ARM64** | ⚠️ Untested | ⏳ Should work |

## Quick Start

### Prerequisites

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install libaravis-0.8-0
```

**Windows:**
Download Aravis from [GitHub Releases](https://github.com/AravisProject/aravis/releases) and install the MSI package.

**macOS:**
```bash
brew install aravis
```

### Running the Demo

```bash
cd AravisSharp
dotnet run
```

The application will automatically detect your platform and verify Aravis installation.

## Features

### ✅ Comprehensive Camera Control
- Device discovery (USB3Vision, GigE Vision)
- Image acquisition with frame callbacks
- GenICam feature access (475+ auto-generated + 80 manual bindings)
- Image export (PNG, JPEG via SixLabors.ImageSharp)

### ✅ GenICam Feature Browser
- **Feature introspection** - Display all camera features
- **Access modes** - ReadOnly, ReadWrite, WriteOnly, NotAvailable
- **Feature types** - Integer, Float, String, Boolean, Enumeration, Command
- **Enumeration choices** - Available options with display names
- **Numeric constraints** - Min/max/increment for numeric features
- **Category tree** - Hierarchical organization (DeviceControl, ImageFormat, Acquisition, etc.)
- **Interactive browser** - Search, modify, and explore features

### ✅ Cross-Platform
- Automatic platform detection
- Platform-specific library loading
- Installation guidance for each platform

## Distribution Strategies

See [CROSS_PLATFORM_GUIDE.md](CROSS_PLATFORM_GUIDE.md) for complete details on:
- ✅ **Windows**: NuGet package with embedded DLLs (recommended)
- ✅ **Linux**: System package dependency or Docker
- ✅ **ARM**: Build into custom image or Docker container
- ✅ **Development**: System-level installation (current)

**Answer: No installer needed!** Use NuGet package for Windows distribution, system packages for Linux.

## Documentation

- **[FEATURE_BROWSER_GUIDE.md](FEATURE_BROWSER_GUIDE.md)** - Complete GenICam feature browser documentation
- **[CROSS_PLATFORM_GUIDE.md](CROSS_PLATFORM_GUIDE.md)** - Platform support and distribution strategies

## Example Code

```csharp
using AravisSharp;

// Discover cameras
CameraDiscovery.UpdateDeviceList();
var cameras = CameraDiscovery.DiscoverCameras();

// Open camera and access features
using var camera = new Camera();
var device = camera.GetDevice();
var nodeMap = device.NodeMap;

// Read features
var vendor = nodeMap.GetStringFeature("DeviceVendorName");
long width = nodeMap.GetIntegerFeature("Width");

// Modify features
nodeMap.SetFloatFeature("ExposureTime", 10000.0);
nodeMap.SetIntegerFeature("Width", 640);

// Capture image
camera.StartAcquisition();
using var buffer = camera.AcquireImage(timeout: 5000);
BufferUtilities.SaveBufferToFile(buffer, "image.png");
camera.StopAcquisition();
```

## License

Aravis is LGPL 2.1+, which allows proprietary applications. AravisSharp license TBD.

---

**For complete documentation, see [FEATURE_BROWSER_GUIDE.md](FEATURE_BROWSER_GUIDE.md) and [CROSS_PLATFORM_GUIDE.md](CROSS_PLATFORM_GUIDE.md).**
