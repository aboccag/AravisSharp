# AravisSharp — Cross-Platform & Distribution Guide

## How Native Libraries Are Loaded

AravisSharp uses four logical library names in its P/Invoke declarations:

| Logical Name | C# File | Purpose |
|-------------|---------|---------|
| `aravis-0.8` | `AravisNative.cs` | Audited Aravis 0.8 camera API |
| `gobject-2.0` | `GLibNative.cs` | GObject ref-counting (`g_object_ref` / `g_object_unref`) and properties |
| `glib-2.0` | `GLibNative.cs` | GLib utilities (`g_error_free`, `g_free`) |
| `gio-2.0` | `GLibNative.cs` | GIO address helpers (GigE Vision IP configuration) |

`AravisLibrary` installs a `NativeLibrary.SetDllImportResolver` that maps each logical name to the correct file for the current OS. It registers itself when AravisSharp loads; calling `AravisLibrary.RegisterResolver()` yourself is optional and harmless.

| Logical Name | Windows | Linux | macOS |
|-------------|---------|-------|-------|
| `aravis-0.8` | `libaravis-0.8-0.dll` | `libaravis-0.8.so.0` | `libaravis-0.8.0.dylib` |
| `gobject-2.0` | `libgobject-2.0-0.dll` | `libgobject-2.0.so.0` | `libgobject-2.0.0.dylib` |
| `glib-2.0` | `libglib-2.0-0.dll` | `libglib-2.0.so.0` | `libglib-2.0.0.dylib` |
| `gio-2.0` | `libgio-2.0-0.dll` | `libgio-2.0.so.0` | `libgio-2.0.0.dylib` |

The resolver tries:
1. **Application directory** — native assets copied next to the managed assembly
2. **`runtimes/{rid}/native/`** — NuGet package layout next to the assembly
3. **Bare name** — lets the OS search system paths (`LD_LIBRARY_PATH`, `PATH`, the loader cache, …)

The copies shipped with the application come first on purpose: a system-wide `libaravis-0.8` has the same soname as the bundled one, and an older release lacks entry points the binding calls. Probing the system first would let it silently replace the Aravis the package was built and tested against. The system search path remains the fallback for platforms the package has no runtime for (`osx-x64`) and for applications that leave the bundled runtimes out to use a system install.

---

## NuGet Package Strategy

### One Package, Four Runtimes

A single `AravisSharp` package carries the managed library and the native runtimes under `runtimes/{rid}/native/`:

| RID | Bundled | Expected from the system |
|-----|---------|--------------------------|
| `win-x64` | `libaravis-0.8-0.dll` + all transitive DLLs (GLib, libxml2, libusb, …) | — |
| `osx-arm64` | `libaravis-0.8.0.dylib` + all non-system dylibs | — |
| `linux-x64`, `linux-arm64` | `libaravis-0.8.so.0`, with libxml2 linked in | GLib, libusb, zlib (distribution packages) |

`osx-x64` is not built: on Intel Macs, install Aravis with Homebrew and the resolver falls back to it.

### Why Linux bundles less

- **Windows and macOS** have no system copy of GLib or libusb to rely on — every dependency is bundled.
- **Linux** ships GLib, libusb and zlib as distribution packages. Bundling a second GLib next to the system one (loaded by GStreamer or GTK, for example) would put two GObject type systems in one process, so only `libaravis` is bundled.
- **libxml2** is linked statically into the Linux `libaravis`, with its symbols hidden, because distributions disagree on its soname (`libxml2.so.2` up to Ubuntu 24.04, `libxml2.so.16` from 26.04).

The Linux build needs glibc 2.29 and GLib 2.64 or later (Ubuntu 20.04, Debian 11, JetPack 5 and newer).

### Directory Layout

```
runtimes/
├── win-x64/native/
│   ├── libaravis-0.8-0.dll
│   ├── libgobject-2.0-0.dll, libglib-2.0-0.dll, libgio-2.0-0.dll, libgmodule-2.0-0.dll
│   ├── libxml2-16.dll, libusb-1.0.dll, zlib1.dll
│   ├── libintl-8.dll, libiconv-2.dll, libpcre2-8-0.dll, libffi-8.dll, libwinpthread-1.dll
│   └── aravis-native-versions.txt
├── osx-arm64/native/
│   ├── libaravis-0.8.0.dylib
│   ├── libglib-2.0.0.dylib, libgobject-2.0.0.dylib, libgio-2.0.0.dylib, libgmodule-2.0.0.dylib
│   ├── libxml2.16.dylib, libusb-1.0.0.dylib, libintl.8.dylib, libpcre2-8.0.dylib
│   └── aravis-native-versions.txt
├── linux-x64/native/
│   ├── libaravis-0.8.so.0
│   └── aravis-native-versions.txt
└── linux-arm64/native/
    ├── libaravis-0.8.so.0
    └── aravis-native-versions.txt
```

The Windows and macOS file sets are derived from the built binary by CI, not listed by hand; [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) is the authoritative list, and each `aravis-native-versions.txt` records the exact versions shipped.

---

## Building Native Libraries

The packaged runtimes are built by CI (`.github/workflows/build-and-publish.yml`) from the `aravis/` submodule with Meson and Ninja. The same scripts run off CI:

| Platform | Build | Collect payload | Verify payload |
|----------|-------|-----------------|----------------|
| Linux (x64, arm64) | `.github/scripts/build-native-linux.sh` (in an `ubuntu:20.04` container; builds libxml2 statically with `build-libxml2-static.sh`) | `collect-native-linux.sh` | `verify-native-linux.sh` |
| Windows (x64) | `meson setup` + `ninja install` into `/mingw64`, in an MSYS2 **MINGW64** shell | `collect-native-windows.sh` (ldd) | `verify-native-windows.sh` |
| macOS (arm64) | `meson setup` + `ninja install` against Homebrew GLib / libxml2 / libusb | `collect-native-macos.sh` (recursive otool walk, `install_name_tool`, ad-hoc `codesign`) | `verify-native-macos.sh` |

`record-native-versions.sh` then writes `aravis-native-versions.txt`. The Meson options, package lists and exact commands are in the workflow; the Windows scripts must run in an MSYS2 MINGW64 shell. The collect scripts derive the dependency set from the built binary and the verify scripts fail closed if anything is neither bundled nor provided by the system — never replace them with a hand-written file list.

On a Linux machine, `./build-native-linux-local.sh` runs the Linux row end to end, with the workflow's Meson options, and places the result in `AravisSharp/runtimes/linux-<arch>/native/` for `dotnet pack`. With docker available it builds in `ubuntu:20.04` like CI; `--host` builds on the machine itself, and the result then needs that machine's GLib and glibc or newer.

### Linux runtime dependencies

On the **target machine** (where the app runs), these shared libraries must be present:

```bash
# Ubuntu / Debian — with the NuGet package (libxml2 is inside libaravis)
sudo apt install -y libglib2.0-0 libusb-1.0-0 zlib1g

# Fedora / RHEL
sudo dnf install -y glib2 libusb1 zlib
```

The bundled libaravis also needs glibc 2.31 and GLib 2.64 or later (Ubuntu 20.04, Debian 11 and newer). A system-wide Aravis (`libaravis-0.8-0`) links libxml2 dynamically and pulls it in as a package dependency. Run `./check-setup.sh` to check these libraries, the glibc and GLib floors, the .NET runtime and the USB udev rules; it exits non-zero when something required is missing.

---

## Distribution Options

### 1. NuGet Package (Recommended)

```bash
dotnet add package AravisSharp
```

- Zero system-wide installation on Windows and Apple Silicon Macs.
- On Linux, install GLib and libusb from the distribution (usually already present).

### 2. System Package (Linux Development)

```bash
sudo apt install -y libaravis-0.8-0
```

- Libraries land in `/usr/lib/x86_64-linux-gnu/`.
- The DllImportResolver falls back to them when the application carries no bundled copy.
- The distribution's Aravis may be older than 0.8.36 and lack entry points the binding calls.

### 3. Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
# libaravis comes from the AravisSharp package; GLib and libusb from the distribution
RUN apt-get update && apt-get install -y --no-install-recommends libglib2.0-0 libusb-1.0-0 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
# Output of: dotnet publish -c Release -r linux-x64 --self-contained false
COPY bin/Release/net10.0/linux-x64/publish/ .
# Your application's assembly — AravisSharp.dll is a library and cannot be started
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

For USB3 Vision cameras, pass `--device /dev/bus/usb` to `docker run`.
For GigE Vision cameras, use `--network host`.

### 4. Self-Contained Publish

```bash
dotnet publish -c Release -r linux-x64 --self-contained
# Output includes the .NET runtime, managed code and runtimes/linux-x64/native/libaravis-0.8.so.0
# Linux still needs GLib and libusb from the distribution
```

---

## Platform-Specific Notes

### Linux x64
- Primary development platform, tested with hardware.
- USB3 Vision: needs udev rules (`./setup-usb-permissions.sh`).
- GigE Vision: works out of the box; tune `net.core.rmem_max` for high throughput.

### Linux ARM64
- `libaravis-0.8.so.0` is bundled in the package, built on an ARM64 runner.
- CI acquires frames from the fake camera through the package on Ubuntu 20.04, 22.04 and 26.04.
- Ideal for Raspberry Pi, Jetson, or embedded vision systems.

### Windows x64
- All DLLs bundled in the NuGet package.
- USB3 Vision cameras require WinUSB driver via Zadig (see [WINDOWS_SETUP.md](WINDOWS_SETUP.md)).
- GigE cameras work without driver changes.

### macOS
- **Apple Silicon (`osx-arm64`)**: Aravis and every non-system dylib are bundled; CI acquires frames from the fake camera through the package with the Homebrew copies removed.
- **Intel (`osx-x64`)**: not built — `brew install aravis` provides the dylib and the resolver falls back to it.

---

## Licensing

**AravisSharp** (the managed binding): MPL-2.0 — usable in proprietary applications; changes to its files must be published under the MPL-2.0.

**Aravis**: LGPL-2.1-or-later

- ✅ You can distribute Aravis binaries in NuGet packages.
- ✅ You can build proprietary applications that use AravisSharp.
- ⚠️ You must allow end users to replace the Aravis library with their own build.
- ⚠️ You must include the Aravis LGPL license notice in your distribution.

The AravisSharp package ships `THIRD-PARTY-NOTICES.md` and the license texts (`licenses/`) for every bundled library; carry them into your own distribution.
