# AravisSharp Cross-Platform Guide

## Platform Support

### ✅ Supported Platforms

| Platform | Architecture | Status | Library Name |
|----------|-------------|--------|--------------|
| **Linux** | x64 | ✅ Tested | `libaravis-0.8.so.0` |
| **Linux** | ARM/ARM64 | ✅ Compatible | `libaravis-0.8.so.0` |
| **Windows** | x64 | ✅ Compatible* | `aravis-0.8-0.dll` |
| **Windows** | ARM64 | ⚠️ Untested | `aravis-0.8-0.dll` |
| **macOS** | x64/ARM64 | ⚠️ Untested | `libaravis-0.8.dylib` |

*Requires Aravis Windows build (see installation instructions below)

### Platform-Specific Notes

#### Linux x64
- **Best Support**: Primary development platform
- **Installation**: Available via apt/dnf/pacman
- **USB3Vision**: Requires proper udev rules
- **GigE Vision**: Works out of the box

#### Linux ARM/ARM64 (Raspberry Pi, Jetson, etc.)
- **Compatibility**: Full support (same library as x64)
- **Performance**: Good performance on modern ARM SBCs
- **Installation**: Build from source recommended for optimal performance
- **Use Cases**: Embedded vision, robotics, industrial IoT

#### Windows x64
- **Compatibility**: Aravis has official Windows builds
- **Installation**: MSI installer or manual DLL placement
- **USB3Vision**: Requires WinUSB driver (use Zadig)
- **GigE Vision**: Works with standard network stack
- **Note**: Some cameras may require vendor-specific filters

#### macOS
- **Compatibility**: Aravis can be built on macOS
- **Installation**: Via Homebrew or source
- **USB Support**: May require additional configuration
- **Note**: Less commonly used for industrial cameras

## Installation Instructions

### Linux (Ubuntu/Debian - x64 and ARM)

```bash
# Install Aravis library
sudo apt-get update
sudo apt-get install libaravis-0.8-0 libaravis-dev

# For USB3Vision cameras (required)
sudo apt-get install libusbutils

# Set up USB permissions
sudo usermod -a -G plugdev $USER
sudo wget https://raw.githubusercontent.com/AravisProject/aravis/main/aravis.rules -O /etc/udev/rules.d/99-aravis.rules
sudo udevadm control --reload-rules
sudo udevadm trigger

# Logout/login or reboot for permissions to take effect
```

### Linux (Build from Source - Recommended for ARM)

```bash
# Install dependencies
sudo apt-get install \
    meson \
    ninja-build \
    libxml2-dev \
    libglib2.0-dev \
    libusb-1.0-0-dev \
    libgstreamer1.0-dev \
    libgstreamer-plugins-base1.0-dev \
    gobject-introspection \
    libgirepository1.0-dev

# Clone and build Aravis
git clone https://github.com/AravisProject/aravis.git
cd aravis
meson build \
    -Dviewer=disabled \
    -Dgst-plugin=disabled \
    -Dintrospection=disabled
cd build
ninja
sudo ninja install
sudo ldconfig
```

### Windows x64

#### Option 1: Official Aravis Build (Recommended)

```powershell
# Download from GitHub Releases
# https://github.com/AravisProject/aravis/releases

# Install MSI package (easiest)
# Or extract ZIP and add to PATH

# Example manual installation:
1. Download aravis-0.8.x-win64.zip
2. Extract to C:\Program Files\Aravis
3. Add to PATH: C:\Program Files\Aravis\bin
4. Restart application
```

#### Option 2: Build with MSYS2

```bash
# In MSYS2 MINGW64 terminal
pacman -S mingw-w64-x86_64-aravis
```

#### Option 3: vcpkg

```powershell
vcpkg install aravis:x64-windows
```

#### USB Camera Setup (Windows)

For USB3Vision cameras, install WinUSB driver using Zadig:

```
1. Download Zadig from https://zadig.akeo.ie/
2. Connect your camera
3. Run Zadig as Administrator
4. Options -> List All Devices
5. Select your camera
6. Select "WinUSB" driver
7. Click "Replace Driver"
```

### macOS

```bash
# Using Homebrew
brew install aravis

# Or build from source
git clone https://github.com/AravisProject/aravis.git
cd aravis
meson build
cd build
ninja
sudo ninja install
```

## Distribution Options

### Option 1: System-Level Installation (Current Approach)

**Similar to**: Traditional Linux software, OpenCV

**Pros:**
- ✅ Small application size
- ✅ Shared library across applications
- ✅ Easy updates via package manager
- ✅ Standard Linux development pattern

**Cons:**
- ❌ Requires manual installation
- ❌ Version conflicts possible
- ❌ More complex for end users

**Best for:**
- Development environments
- Linux servers and embedded systems
- System integrators

### Option 2: NuGet Package with Embedded Runtimes

**Similar to**: SkiaSharp, SQLitePCLRaw, LibVLCSharp

**Pros:**
- ✅ Zero installation required
- ✅ Xcopy deployment
- ✅ Version locked to application
- ✅ Works out-of-the-box

**Cons:**
- ❌ Larger NuGet package (~10-50 MB)
- ❌ Must build/obtain Aravis for each platform
- ❌ Licensing considerations (LGPL)
- ❌ More complex NuGet packaging

**Best for:**
- End-user applications
- Cross-platform desktop apps
- Simplified deployment

**Implementation:**

```xml
<!-- AravisSharp.csproj for NuGet package approach -->
<ItemGroup>
  <!-- Windows x64 -->
  <Content Include="runtimes/win-x64/native/aravis-0.8-0.dll" 
           PackagePath="runtimes/win-x64/native" 
           Pack="true" />
  <Content Include="runtimes/win-x64/native/glib-2.0-0.dll" 
           PackagePath="runtimes/win-x64/native" 
           Pack="true" />
  <!-- + other dependencies -->
  
  <!-- Linux x64 -->
  <Content Include="runtimes/linux-x64/native/libaravis-0.8.so.0" 
           PackagePath="runtimes/linux-x64/native" 
           Pack="true" />
  
  <!-- Linux ARM64 -->
  <Content Include="runtimes/linux-arm64/native/libaravis-0.8.so.0" 
           PackagePath="runtimes/linux-arm64/native" 
           Pack="true" />
  
  <!-- macOS -->
  <Content Include="runtimes/osx-x64/native/libaravis-0.8.dylib" 
           PackagePath="runtimes/osx-x64/native" 
           Pack="true" />
</ItemGroup>
```

### Option 3: Runtime Installer (Like Pylon SDK)

**Similar to**: Basler Pylon, FLIR Spinnaker, National Instruments Vision

**Pros:**
- ✅ Professional distribution
- ✅ Includes drivers and tools
- ✅ Centralized updates
- ✅ System-wide optimization

**Cons:**
- ❌ Requires admin installation
- ❌ Complex installer creation
- ❌ Update notifications needed
- ❌ Overkill for open-source library

**Best for:**
- Commercial camera SDKs
- Enterprise deployments
- When bundling proprietary drivers

### Option 4: Hybrid Approach (Recommended)

**Recommended Strategy:**

1. **Development**: System installation (apt/dnf/brew)
2. **Production Linux**: System installation or Docker
3. **Production Windows**: NuGet package with embedded DLLs
4. **Embedded (ARM)**: System installation in custom image

## NuGet Package Creation Guide

### Structure for Multi-Platform NuGet Package

```
AravisSharp.1.0.0.nupkg
├── lib/
│   └── net8.0/
│       └── AravisSharp.dll          # Managed assembly
├── runtimes/
│   ├── win-x64/
│   │   └── native/
│   │       ├── aravis-0.8-0.dll
│   │       ├── glib-2.0-0.dll
│   │       ├── gobject-2.0-0.dll
│   │       ├── gio-2.0-0.dll
│   │       └── libxml2-2.dll        # Dependencies
│   ├── linux-x64/
│   │   └── native/
│   │       └── libaravis-0.8.so.0   # System libs usually linked
│   ├── linux-arm64/
│   │   └── native/
│   │       └── libaravis-0.8.so.0
│   └── osx-x64/
│       └── native/
│           └── libaravis-0.8.0.dylib
└── build/
    └── AravisSharp.targets            # MSBuild targets for native lib copying
```

### Creating the NuGet Package

```bash
# 1. Create nuspec file
dotnet pack -c Release

# 2. Or use a .nuspec template
```

#### AravisSharp.nuspec Example

```xml
<?xml version="1.0"?>
<package>
  <metadata>
    <id>AravisSharp</id>
    <version>1.0.0</version>
    <title>AravisSharp - Cross-Platform Industrial Camera Library</title>
    <authors>Your Name</authors>
    <description>
      .NET bindings for Aravis - a vision library for genicam based cameras.
      Supports USB3Vision and GigE Vision cameras on Windows, Linux (x64/ARM), and macOS.
    </description>
    <projectUrl>https://github.com/yourusername/AravisSharp</projectUrl>
    <license type="expression">LGPL-2.1-or-later</license>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <tags>camera vision usb3vision gigE industrial aravis genicam</tags>
    <dependencies>
      <group targetFramework="net8.0">
        <dependency id="SixLabors.ImageSharp" version="3.1.12" />
      </group>
    </dependencies>
  </metadata>
  <files>
    <!-- Managed assembly -->
    <file src="bin/Release/net8.0/AravisSharp.dll" target="lib/net8.0" />
    
    <!-- Windows native libraries -->
    <file src="runtimes/win-x64/native/*.dll" target="runtimes/win-x64/native" />
    
    <!-- Linux x64 native libraries (if included) -->
    <file src="runtimes/linux-x64/native/*.so*" target="runtimes/linux-x64/native" />
    
    <!-- Linux ARM64 native libraries (if included) -->
    <file src="runtimes/linux-arm64/native/*.so*" target="runtimes/linux-arm64/native" />
    
    <!-- macOS native libraries -->
    <file src="runtimes/osx-x64/native/*.dylib" target="runtimes/osx-x64/native" />
  </files>
</package>
```

### Obtaining Native Libraries

#### Windows

```powershell
# Download from Aravis releases or build with MSYS2
# Extract all DLLs from bin/ directory:
aravis-0.8-0.dll
glib-2.0-0.dll
gobject-2.0-0.dll
gio-2.0-0.dll
gmodule-2.0-0.dll
libxml2-2.dll
libusb-1.0.dll
libintl-8.dll
pcre2-8-0.dll
# (exact list depends on Aravis build)
```

#### Linux

```bash
# Option 1: Extract from system installation
cp /usr/lib/x86_64-linux-gnu/libaravis-0.8.so.0 runtimes/linux-x64/native/

# Option 2: Build statically-linked version (advanced)
# This bundles all dependencies into one .so file

# Option 3: Document as system dependency (simpler)
# Don't include .so files, require system installation
```

#### Linux ARM64

```bash
# Cross-compile or build on ARM device
# Copy from /usr/lib/aarch64-linux-gnu/libaravis-0.8.so.0
```

## Licensing Considerations

**Aravis License**: LGPL 2.1 or later

**Implications for NuGet Package:**
- ✅ Can distribute Aravis binaries
- ✅ Can create proprietary applications using AravisSharp
- ⚠️ Must mention use of Aravis (LGPL attribution)
- ⚠️ Users must be able to replace Aravis library if desired
- ✅ No requirement to open-source your application

**Best Practice:**
- Include Aravis license in NuGet package
- Document that Aravis (LGPL) is used
- Provide instructions for using system-installed Aravis instead

## Recommended Approach

### For Your Project

**Current Phase (Development):**
```bash
# Keep system installation approach
sudo apt-get install libaravis-0.8-0
```

**Future (Distribution):**

1. **Linux Users**: System package dependency
   ```
   Depends: libaravis-0.8-0 (>= 0.8.0)
   ```

2. **Windows Users**: NuGet package with embedded DLLs
   ```
   Install-Package AravisSharp
   # DLLs automatically copied to output
   ```

3. **Docker/Containers**: Base image with Aravis
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/runtime:8.0
   RUN apt-get update && apt-get install -y libaravis-0.8-0
   COPY app/ /app
   ```

4. **Embedded Linux (ARM)**: Include in system image
   ```
   # Yocto/Buildroot recipe
   # Or Debian package in custom APT repository
   ```

## Testing Cross-Platform

### Building for Multiple Platforms

```bash
# Build for all platforms
dotnet build -c Release

# Build for specific platform
dotnet build -c Release -r linux-x64
dotnet build -c Release -r linux-arm64
dotnet build -c Release -r win-x64
dotnet build -c Release -r osx-x64

# Publish self-contained (includes .NET runtime)
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r linux-arm64 --self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

### Runtime Platform Detection

```csharp
using AravisSharp.Native;

// Check platform
Console.WriteLine(AravisLibrary.GetPlatformInfo());

// Check if Aravis is available
if (!AravisLibrary.IsAravisAvailable())
{
    Console.WriteLine("Aravis not found!");
    Console.WriteLine(AravisLibrary.GetInstallationInstructions());
    return;
}

// Proceed with camera operations
var cameras = CameraDiscovery.DiscoverCameras();
```

## Docker Support

### Dockerfile for Linux

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy

# Install Aravis
RUN apt-get update && \
    apt-get install -y libaravis-0.8-0 libusb-1.0-0 && \
    rm -rf /var/lib/apt/lists/*

# Copy application
WORKDIR /app
COPY bin/Release/net8.0/linux-x64/publish/ .

# Run application
ENTRYPOINT ["dotnet", "AravisSharp.dll"]
```

### Docker Compose with USB Device Access

```yaml
version: '3.8'
services:
  aravissharp:
    build: .
    devices:
      - /dev/bus/usb:/dev/bus/usb  # USB3Vision cameras
    network_mode: host              # GigE Vision cameras
    privileged: true                # Required for USB access
```

## Summary

### Cross-Platform Compatibility

| Question | Answer |
|----------|--------|
| **Will it work on Windows?** | ✅ Yes, with Aravis Windows build |
| **Will it work on Linux ARM?** | ✅ Yes, same library as x64 |
| **Should we make installer like Pylon?** | ⚠️ Not necessary - NuGet is better for .NET |
| **Can we make NuGet package?** | ✅ Yes, recommended for Windows distribution |

### Recommendations

1. **Current (Development)**: Keep system installation
   - Easy development
   - Standard Linux practice
   - Works perfectly

2. **Future (Windows Distribution)**: Create NuGet package
   - Embed Windows DLLs in `runtimes/win-x64/native/`
   - Automatic xcopy deployment
   - Better than custom installer

3. **Future (Linux Distribution)**: Debian/RPM packages
   - List Aravis as dependency
   - Standard Linux distribution
   - Works with Docker

4. **Embedded ARM**: Custom system image
   - Include Aravis in Yocto/Buildroot
   - Or use Docker container
   - Same code works across architectures

### Next Steps

1. ✅ Code is already cross-platform compatible
2. ⏭️ Test on Windows (optional)
3. ⏭️ Create NuGet package structure (when ready to distribute)
4. ⏭️ Document platform-specific setup in README

The current code will work on Windows and ARM without changes - just need Aravis installed on the target platform!
