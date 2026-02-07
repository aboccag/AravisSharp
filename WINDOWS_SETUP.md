# AravisSharp — Windows Setup Guide

## 1. Install the NuGet Runtime Package

```powershell
cd AravisSharp
dotnet add package AravisSharp.runtime.win-x64
dotnet build
```

The `AravisSharp.runtime.win-x64` NuGet package places all native DLLs into `runtimes/win-x64/native/` — the `DllImportResolver` in `AravisLibrary.cs` picks them up automatically.

### What's in the package

| DLL | Purpose |
|-----|---------|
| `libaravis-0.8-0.dll` | Aravis camera control library |
| `libgobject-2.0-0.dll` | GObject type system |
| `libglib-2.0-0.dll` | GLib core utilities |
| `libgio-2.0-0.dll` | GIO (I/O abstraction) |
| `libgmodule-2.0-0.dll` | GModule (dynamic loading) |
| `libxml2-16.dll` | XML parser (GenICam descriptions) |
| `libusb-1.0.dll` | USB access (USB3 Vision) |
| `zlib1.dll` | Compression |
| `libintl-8.dll` | Internationalisation |
| `libpcre2-8-0.dll` | Perl-compatible regex (GLib dep) |
| `libffi-8.dll` | Foreign function interface (GObject dep) |
| `libiconv-2.dll` | Character encoding conversion |

All DLLs are built from MSYS2 mingw64 packages (64-bit, release, no debug symbols).

---

## 2. USB3 Vision Camera — WinUSB Driver (One-Time)

> **GigE cameras work out of the box** — skip this section if you only use GigE.

### Why is this needed?

Aravis uses `libusb` to talk to USB cameras. On Windows, `libusb` can only access devices using the **WinUSB** generic driver. Industrial cameras ship with vendor-specific drivers (e.g. Basler "PylonUSB") that are not compatible.

### Steps

1. **Download [Zadig](https://zadig.akeo.ie/)** — free, portable, open-source.
2. **Connect your USB camera** and confirm it appears in Device Manager.
3. **Run Zadig as Administrator** (right-click → Run as administrator).
4. **Options → List All Devices**.
5. **Select your camera** from the dropdown (e.g. "Basler ace USB3 Vision Camera").
6. **Select WinUSB** as the target driver (usually pre-selected).
7. **Click "Replace Driver"** — wait 10–30 seconds.

```
┌──────────────────────────────────────┐
│ Zadig                            [×] │
├──────────────────────────────────────┤
│ [Basler ace USB3 Vision Camera  ▼]   │
│                                      │
│  PylonUSB  ───────►  WinUSB          │
│                                      │
│       [ Replace Driver ]             │
└──────────────────────────────────────┘
```

8. **Verify**:
   ```powershell
   cd AravisSharp
   dotnet run
   # Should show "Devices found: 1"
   ```

### Check current driver

```powershell
Get-PnpDevice | Where-Object { $_.FriendlyName -match 'camera|basler|vision' } |
  Format-Table Status, Class, FriendlyName

# Before Zadig:  Class = PylonUSB   → camera NOT visible to Aravis
# After  Zadig:  Class = USBDevice  → camera IS visible to Aravis
```

### Reverting to vendor driver

If you need Basler Pylon / vendor software again:

- **Option A**: Reinstall your vendor SDK — it will restore its driver.
- **Option B**: Open Zadig again, select the camera, choose the original driver, click "Replace Driver".

---

## 3. GigE Camera Setup

GigE Vision cameras work over the network — no driver changes needed.

```powershell
# 1. Set a static IP on the network adapter connected to the camera
#    Example: PC = 192.168.1.100, Camera = 192.168.1.10, Mask = 255.255.255.0

# 2. Allow Aravis through Windows Firewall (or disable temporarily to test)
netsh advfirewall firewall add rule name="AravisSharp" dir=in action=allow program="path\to\AravisSharp.exe"

# 3. Test
cd AravisSharp
dotnet run
```

---

## 4. Building the Windows NuGet Package (Maintainers)

The `copy-aravis-dlls.ps1` script extracts `libaravis-0.8-0.dll` and all its transitive dependencies from an MSYS2 installation:

### Prerequisites

1. Install [MSYS2](https://www.msys2.org/).
2. In the MSYS2 **MINGW64** terminal:
   ```bash
   pacman -Syu
   pacman -S mingw-w64-x86_64-aravis mingw-w64-x86_64-toolchain
   ```

### Extract DLLs

```powershell
.\copy-aravis-dlls.ps1 -MsysRoot "C:\msys64" -DestRoot "C:\dev\AravisSharpWindows"
```

The script:
1. Locates `libaravis-0.8-0.dll` in the MSYS2 mingw64 `bin/` folder.
2. Runs `ldd` (via MSYS2 bash) to discover every transitive dependency.
3. Filters out Windows system DLLs (`kernel32`, `ntdll`, `msvcrt`, …).
4. Copies all remaining DLLs to `runtimes/win-x64/native/`.
5. Generates a `.nuspec` file for NuGet Package Explorer.

### Pack into NuGet

Open the `.nuspec` in [NuGet Package Explorer](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer), verify contents, and save as `.nupkg`.

---

## 5. Troubleshooting

### "Devices found: 0" with USB camera connected

| Check | Action |
|-------|--------|
| Driver | Install WinUSB via Zadig (Section 2) |
| Cable | Use USB 3.0 cable in a USB 3.0 port |
| Power | Some cameras need powered USB hubs |
| Other software | Close Pylon Viewer or other apps that hold the device open |

### `DllNotFoundException: aravis-0.8`

```powershell
# Verify NuGet package is installed
dotnet list package

# Verify DLLs are in the output directory
ls bin\Debug\net10.0\runtimes\win-x64\native\
```

### `EntryPointNotFoundException: g_object_unref in DLL 'aravis-0.8'`

This was a bug in earlier versions where GLib functions were incorrectly mapped to the Aravis DLL. Update to the latest code — `g_object_unref` and `g_error_free` are now correctly routed to `libgobject-2.0-0.dll` and `libglib-2.0-0.dll` respectively (via `GLibNative.cs`).

### GigE camera not detected

1. Ensure PC and camera are on the same subnet.
2. Temporarily disable Windows Firewall to test.
3. Verify with `ping <camera-ip>`.

---

## Expected Output

```
=== AravisSharp Platform Information ===
OS: Microsoft Windows 10.0.26200
Architecture: X64
Framework: .NET 10.0.x

Aravis Library: libaravis-0.8-0.dll
Aravis Status: ✓ Available

=== Aravis Interfaces ===
Number of interfaces: 3
  [0] Fake
  [1] USB3Vision
  [2] GigEVision

Devices found: 1
```
