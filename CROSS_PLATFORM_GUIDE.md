# AravisSharp — Cross-Platform & Distribution Guide

## How Native Libraries Are Loaded

AravisSharp uses three logical library names in its P/Invoke declarations:

| Logical Name | C# File | Purpose |
|-------------|---------|---------|
| `aravis-0.8` | `AravisNative.cs`, `AravisGenerated.cs` | Aravis camera API |
| `gobject-2.0` | `GLibNative.cs` | GObject ref-counting (`g_object_ref` / `g_object_unref`) |
| `glib-2.0` | `GLibNative.cs` | GLib utilities (`g_error_free`, `g_free`) |

At startup, `AravisLibrary.RegisterResolver()` installs a `NativeLibrary.SetDllImportResolver` that maps each logical name to the correct file for the current OS:

| Logical Name | Windows | Linux | macOS |
|-------------|---------|-------|-------|
| `aravis-0.8` | `libaravis-0.8-0.dll` | `libaravis-0.8.so.0` | `libaravis-0.8.dylib` |
| `gobject-2.0` | `libgobject-2.0-0.dll` | `libgobject-2.0.so.0` | `libgobject-2.0.dylib` |
| `glib-2.0` | `libglib-2.0-0.dll` | `libglib-2.0.so.0` | `libglib-2.0.dylib` |

The resolver tries:
1. **Bare name** — lets the OS search system paths (`LD_LIBRARY_PATH`, `PATH`, etc.)
2. **`runtimes/{rid}/native/`** — NuGet package layout next to the assembly

---

## NuGet Package Strategy

### Package Split

| Package | Contents | Notes |
|---------|----------|-------|
| `AravisSharp` | Managed DLL only | Camera, Stream, Buffer, NodeMap, … |
| `AravisSharp.runtime.win-x64` | All native DLLs for Windows | libaravis + GLib + libxml2 + libusb + zlib + … |
| `AravisSharp.runtime.linux-x64` | `libaravis-0.8.so.0` only | GLib / libxml2 / libusb / zlib come from OS packages |

### Why the split?

- **Windows** has no system package manager — every dependency must be bundled.
- **Linux** ships GLib, libxml2, libusb, and zlib as OS packages (always present on desktop). Only `libaravis` itself needs bundling.
- Keeping runtimes in separate packages avoids bloating the managed package.

### Directory Layout

```
runtimes/
├── win-x64/
│   └── native/
│       ├── libaravis-0.8-0.dll
│       ├── libgobject-2.0-0.dll
│       ├── libglib-2.0-0.dll
│       ├── libgio-2.0-0.dll
│       ├── libgmodule-2.0-0.dll
│       ├── libxml2-16.dll
│       ├── libusb-1.0.dll
│       ├── zlib1.dll
│       ├── libintl-8.dll
│       ├── libpcre2-8-0.dll
│       ├── libffi-8.dll
│       └── libiconv-2.dll
└── linux-x64/
    └── native/
        └── libaravis-0.8.so.0
```

---

## Building Native Libraries

### Windows (via MSYS2)

```powershell
# 1. Install MSYS2 from https://www.msys2.org/
# 2. In MSYS2 MINGW64 terminal:
pacman -Syu
pacman -S mingw-w64-x86_64-aravis mingw-w64-x86_64-toolchain

# 3. Back in PowerShell — extract DLLs:
.\copy-aravis-dlls.ps1 -MsysRoot "C:\msys64" -DestRoot "C:\dev\AravisSharpWindows"

# 4. Output: runtimes/win-x64/native/*.dll + .nuspec
```

### Linux (from source)

```bash
# Build Aravis 0.8.33 with minimal dependencies (no viewer, no GStreamer):
./build_aravis_linux_nuget.sh

# Output: ~/dev/AravisSharpLinux/runtimes/linux-x64/native/libaravis-0.8.so.0
# + .nuspec for NuGet Package Explorer
```

The build script:
1. Installs build dependencies (`meson`, `ninja`, `libglib2.0-dev`, `libxml2-dev`, `libusb-1.0-0-dev`).
2. Downloads Aravis 0.8.33 source tarball.
3. Configures with `meson setup -Dviewer=disabled -Dgst-plugin=disabled -Dintrospection=disabled`.
4. Builds with `ninja` and stages into a temporary DESTDIR.
5. Copies `libaravis-0.8.so.0` into the NuGet `runtimes/linux-x64/native/` layout.

### Linux Runtime Dependencies

On the **target machine** (where the app runs), these shared libraries must be present:

```bash
# Ubuntu / Debian
sudo apt install -y libglib2.0-0 libxml2 libusb-1.0-0 zlib1g

# Fedora / RHEL
sudo dnf install -y glib2 libxml2 libusb1 zlib
```

Run `./check-setup.sh` to verify all 8 dependencies are found in the ldconfig cache.

---

## Distribution Options

### 1. NuGet Runtime Packages (Recommended)

```bash
dotnet add package AravisSharp
dotnet add package AravisSharp.runtime.win-x64    # Windows users
dotnet add package AravisSharp.runtime.linux-x64   # Linux users (optional)
```

- Zero system-wide installation on Windows.
- Linux users may skip the runtime package if Aravis is installed via apt.

### 2. System Package (Linux Development)

```bash
sudo apt install -y libaravis-0.8-0
```

- Libraries land in `/usr/lib/x86_64-linux-gnu/`.
- The DllImportResolver finds them automatically.
- Best for development and CI.

### 3. Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update && apt-get install -y libaravis-0.8-0 libusb-1.0-0 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY bin/Release/net10.0/publish/ .
ENTRYPOINT ["dotnet", "AravisSharp.dll"]
```

For USB3 Vision cameras, pass `--device /dev/bus/usb` to `docker run`.
For GigE Vision cameras, use `--network host`.

### 4. Self-Contained Publish

```bash
dotnet publish -c Release -r linux-x64 --self-contained
# Output includes .NET runtime + managed code
# Still needs native Aravis library (system package or in runtimes/ folder)
```

---

## Platform-Specific Notes

### Linux x64
- Primary development platform, fully tested.
- USB3 Vision: needs udev rules (`./setup-usb-permissions.sh`).
- GigE Vision: works out of the box; tune `net.core.rmem_max` for high throughput.

### Linux ARM64
- Same `libaravis-0.8.so.0` API — cross-compile or build on-device.
- Ideal for Raspberry Pi, Jetson, or embedded vision systems.
- Use `linux-arm64` as the RuntimeIdentifier.

### Windows x64
- All DLLs bundled in the NuGet runtime package.
- USB3 Vision cameras require WinUSB driver via Zadig (see [WINDOWS_SETUP.md](WINDOWS_SETUP.md)).
- GigE cameras work without driver changes.

### macOS
- `brew install aravis` provides the dylib.
- Untested — should work but no CI coverage yet.

---

## Licensing

**Aravis**: LGPL-2.1-or-later

- ✅ You can distribute Aravis binaries in NuGet packages.
- ✅ You can build proprietary applications that use AravisSharp.
- ⚠️ You must allow end users to replace the Aravis library with their own build.
- ⚠️ You must include the Aravis LGPL license notice in your distribution.

Best practice: include `THIRD_PARTY_LICENSES.txt` in your NuGet package with the Aravis LGPL text.
