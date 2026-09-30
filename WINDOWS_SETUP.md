# AravisSharp — Windows Setup Guide

## 1. Install the NuGet Package

In your application's project folder:

```powershell
dotnet add package AravisSharp
dotnet build
```

The `AravisSharp` NuGet package carries the native DLLs under `runtimes/win-x64/native/` and they land in your build output — the `DllImportResolver` in `AravisLibrary.cs` picks them up automatically.

### What's in the package (`runtimes/win-x64/native/`)

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

`libaravis-0.8-0.dll` is built by CI from the `aravis/` submodule with MSYS2 MinGW64 (64-bit, release); the other DLLs come from MSYS2 `mingw-w64-x86_64-*` packages. CI derives the set from the built DLL, so this table can lag behind: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) is the authoritative list, and `aravis-native-versions.txt` in the same folder records the exact versions shipped.

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

8. **Verify** (from a clone of the repository):
   ```powershell
   dotnet run --project AravisSharp.Examples -f net10.0
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
- **Option C**: Device Manager → the camera → Uninstall device, tick "Attempt to remove the driver for this device", then unplug and replug: Windows binds the vendor driver, which is still in the driver store.

### Using AravisSharp alongside a vendor SDK (pylon, Spinnaker, …)

Aravis implements the GigE Vision and USB3 Vision protocols itself. It does not go through a vendor driver or a GenTL producer (`.cti`), so what it can share with a vendor SDK depends on the transport:

| Transport | Vendor SDK installed at the same time | Notes |
|-----------|---------------------------------------|-------|
| GigE Vision | Works | Plain UDP sockets, no driver. Both stacks can discover every camera; a camera can be controlled by only one application at a time (GigE control privilege). Vendor filter drivers (e.g. the pylon GigE filter driver) do not have to be removed. |
| USB3 Vision | Per camera, exclusive | A USB camera is bound to one driver. With WinUSB (Zadig) it is visible to Aravis and hidden from the vendor SDK; other cameras keep the vendor driver. On a composite camera (e.g. Basler ace USB3: `USB\VID_2676&PID_BA02&MI_00`), pick **Interface 0** in Zadig. |

Without a vendor SDK, Windows binds WinUSB automatically only to cameras that expose Microsoft OS descriptors, which most USB3 Vision cameras do not: expect to need the Zadig step in both cases.

Verified on Windows 11 with a Basler acA2040-90um switched from pylon's driver to WinUSB (Zadig, Interface 0): discovery, feature access and acquisition work.

WinUSB lets only one handle open a device, and USB3 Vision discovery opens the device to read its identity: while an application holds a camera open (a `Camera` or `Stream` not yet disposed), a discovery in another application, or through another handle, lists it without an ID. Dispose cameras and streams deterministically.

Not supported today:

- **Sharing a USB camera with the vendor driver.** libusb's UsbDk backend, a filter driver that captures a device whatever driver it is bound to, could allow it: the bundled libusb has the UsbDk backend built in and looks for the UsbDk driver at startup, but AravisSharp does not enable that backend yet.
- **Using the vendor's own driver** (GenTL producer such as pylon's `ProducerU3V.cti`). That needs a GenTL consumer backend, which neither Aravis nor AravisSharp has.

---

## 3. GigE Camera Setup

GigE Vision cameras work over the network — no driver changes needed.

```powershell
# 1. Set a static IP on the network adapter connected to the camera
#    Example: PC = 192.168.1.100, Camera = 192.168.1.10, Mask = 255.255.255.0

# 2. Allow Aravis through Windows Firewall (or disable temporarily to test)
#    (your application's .exe, or AravisSharp.Examples.exe for the demo)
netsh advfirewall firewall add rule name="AravisSharp" dir=in action=allow program="path\to\YourApp.exe"

# 3. Test (from a clone of the repository)
dotnet run --project AravisSharp.Examples -f net10.0
```

---

## 4. Building the Windows NuGet Package (Maintainers)

The `win-x64` runtime is built by CI (`.github/workflows/build-and-publish.yml`, job **build-native**) from the `aravis/` submodule — not from MSYS2's own `mingw-w64-x86_64-aravis` package, whose version need not match 0.8.36. The same steps run locally in an MSYS2 **MINGW64** shell:

1. Install [MSYS2](https://www.msys2.org/) and, in the MINGW64 shell, the packages the workflow lists (`mingw-w64-x86_64-gcc`, `-glib2`, `-libxml2`, `-libusb`, `-meson`, `-ninja`, `-pkg-config`, `-binutils`).
2. Build and install Aravis into `/mingw64` with `meson setup _build aravis <MESON_OPTS> --prefix=/mingw64` and `ninja -C _build install` (the options are in the workflow's `MESON_OPTS`).
3. Collect the payload: `.github/scripts/collect-native-windows.sh _native` — copies `libaravis-0.8-0.dll` and every MinGW DLL `ldd` finds behind it.
4. Verify it: `.github/scripts/verify-native-windows.sh _native` — fails if a bundled DLL imports a MinGW DLL that is not bundled.
5. Record versions: `.github/scripts/record-native-versions.sh _native`.

The **pack** job then places the files under `AravisSharp/runtimes/win-x64/native/`, refuses any file missing from `THIRD-PARTY-NOTICES.md`, and runs `dotnet pack AravisSharp/AravisSharp.csproj -c Release`. Never replace the collect script with a hand-written DLL list: that is how a release once shipped without libxml2.

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

### GigE packet loss (buffers with `Timeout` status)

`stream.GetGigEStatistics()` tells what happened. `ResentPackets` counts lost packets that a resend request recovered. `MissingPackets` only counts packets of frames that failed, and for each of them it counts every packet after the first gap, so it overstates the loss.

**`ResentPackets` stays 0 while `MissingPackets` grows.** Before the published 0.8.36 (builds from source up to commit `a4f6489`), `ConfigureGigEDefaults()` set the initial packet timeout (how long a packet may be missing before the first resend request) to 1 s. Aravis closes an incomplete frame after 100 ms without packets (the frame retention), so no request was ever sent, and every lost packet cost a frame. Update, or call `stream.SetInitialPacketTimeout(1000)` after `ConfigureGigEDefaults()`. Measured on an acA1920-40gm at 41 fps with 4 buffers, 600 frames per run:

| Stream settings | Frames OK | Resent |
|---|---|---|
| `ConfigureGigEDefaults(8)`, before the fix (initial packet timeout 1 s) | 299 and 188 / 600 | 0 |
| `ConfigureGigEDefaults(8)`, fixed (1 ms) | 600 and 600 / 600 | 1207 and 863 |

**Why packets get lost.** That camera sends about 97 MB/s (780 Mbit/s) in 1500-byte packets. On the test PC (a Realtek USB GbE adapter with the pylon and Sapera GigE Vision filter drivers bound), 0.1 to 3.5 % of packets were lost, and the rate varied between runs. The UDP socket buffer was not the cause: `netstat -s -p udp` reported 0 receive errors, even with a 64 KiB socket buffer. The adapter reported no discards either. The loss grows with the packet rate: with 1000-byte packets, 95 of 200 frames timed out, and with 576-byte packets, 183 of 200. What limits the receive path is the cost per packet (adapter, drivers, other cameras on the same link), not the socket buffer.

**Recommended configuration**

1. `camera.GvAutoPacketSize()` and `stream.ConfigureGigEDefaults()`, before starting acquisition.
2. Push enough buffers. Frames are delivered in order, so a frame still waiting for resent packets holds back the complete frames behind it, for up to 100 ms. Use at least frame rate × 0.1 s + 2 buffers (6 at 40 fps; 8 to 16 leaves a margin). With 4 buffers, one stalled frame starves the stream (underruns) and drops the frames behind it.
3. Leave some headroom on the link. Lower the frame rate, or spread the packets with `camera.GvSetPacketDelay(ns)`: 2000 ns brought 41 fps down to 39 fps and resends down to 32 per 200 frames. Several cameras on one 1 GbE port add up.
4. Adapter (your choice, not done by AravisSharp): prefer a PCIe NIC to a USB adapter. Enable jumbo frames on the NIC and the switch, then rerun `GvAutoPacketSize()`. Raise the receive buffers, disable Energy-Efficient Ethernet, and unbind the GigE Vision filter drivers of SDKs that do not use this adapter.

---

## Expected Output

```
=== AravisSharp Platform Information ===
OS: Microsoft Windows 10.0.26200
Architecture: X64
Framework: .NET 10.0.x

Aravis Library: libaravis-0.8-0.dll
Aravis Status: ✓ Available (0.8.36)

=== Aravis Interfaces ===
Number of interfaces: 3
  [0] Fake
  [1] USB3Vision
  [2] GigEVision

Devices found: 1
```
