# AravisSharp Windows Setup Guide

## Quick Start

### 1. Install the NuGet Package (Recommended)

```powershell
cd AravisSharp
dotnet add package AravisSharp.runtime.win-x64
dotnet run
```

The NuGet package includes all required DLLs (libaravis, GLib, GObject, libxml2, libusb, etc.)

### 2. Setup USB3Vision Cameras (One-Time)

**⚠️ REQUIRED for USB cameras** - GigE cameras can skip this step

#### Why?

Windows USB cameras need the **WinUSB** generic driver to work with Aravis. Most industrial cameras ship with vendor-specific drivers (Basler uses "PylonUSB", FLIR uses their own, etc.) which are not compatible.

#### Steps

1. **Download Zadig** from https://zadig.akeo.ie/ (free, portable, open source)

2. **Connect your USB camera** and verify it appears in Windows Device Manager

3. **Run Zadig as Administrator** (right-click → "Run as administrator")

4. **Enable device listing**:
   - Go to **Options** → **List All Devices**

5. **Find your camera** in the dropdown list:
   - Example: "Basler ace USB3 Vision Camera"
   - Example: "USB3 Vision Camera" 
   - May also show vendor ID/PID like "2676:BA02"

6. **Select WinUSB driver**:
   - In the middle section, WinUSB should be pre-selected
   - If not, choose it from the dropdown

7. **Click "Replace Driver"**:
   - Wait for installation (10-30 seconds)
   - You'll see a success message

8. **Verify installation**:
   ```powershell
   cd AravisSharp
   dotnet run
   ```
   
   You should see:
   ```
   Devices found: 1
   ```

#### Visual Guide

```
┌─────────────────────────────────────┐
│ Zadig 2.8                       [×] │
├─────────────────────────────────────┤
│ Device:                             │
│ ┌─────────────────────────────────┐ │
│ │ Basler ace USB3 Vision Camera ▼ │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Driver:                             │
│ ┌──────────┐      ┌──────────────┐ │
│ │ PylonUSB │  =>  │   WinUSB     │ │
│ └──────────┘      └──────────────┘ │
│                                     │
│      [ Replace Driver ]             │
└─────────────────────────────────────┘
```

### 3. GigE Camera Setup (No Driver Needed)

GigE cameras work over the network with no driver installation required.

**Network Configuration:**

1. **Set static IP on your network adapter**:
   - Control Panel → Network Connections
   - Right-click adapter → Properties → IPv4
   - Use static IP: `192.168.1.100`
   - Subnet mask: `255.255.255.0`

2. **Configure camera IP** (if needed):
   - Use vendor tool to set camera IP: `192.168.1.10`
   - Or enable DHCP on your network

3. **Test connection**:
   ```powershell
   ping 192.168.1.10
   ```

4. **Run AravisSharp**:
   ```powershell
   dotnet run
   ```

## Troubleshooting

### ❌ "Devices found: 0" with USB Camera

**Cause**: Camera is still using vendor driver (e.g., PylonUSB)

**Solution**: Follow the Zadig steps above to install WinUSB driver

**Verify current driver**:
```powershell
Get-PnpDevice | Where-Object { $_.FriendlyName -match 'camera|basler|vision' } | Format-Table Status, Class, FriendlyName
```

Expected output **BEFORE** Zadig:
```
Status  Class      FriendlyName
OK      PylonUSB   Basler ace USB3 Vision Camera
```

Expected output **AFTER** Zadig:
```
Status  Class      FriendlyName
OK      USBDevice  Basler ace USB3 Vision Camera
```

### ❌ "Unable to load DLL 'aravis-0.8'"

**Cause**: NuGet package not installed or DLLs not copied to output

**Solution 1** (Recommended):
```powershell
dotnet add package AravisSharp.runtime.win-x64
dotnet build
```

**Solution 2** (Manual):
Copy all DLLs from the NuGet package to your application's directory:
```
YourApp.exe
libaravis-0.8-0.dll
libgobject-2.0-0.dll
libglib-2.0-0.dll
libxml2-16.dll
libusb-1.0.dll
zlib1.dll
... (and other dependencies)
```

**Verify DLLs are present**:
```powershell
ls bin\Debug\net10.0\runtimes\win-x64\native\
```

### ❌ GigE Camera Not Found

**Check 1**: Firewall
```powershell
# Temporarily disable firewall to test
netsh advfirewall set allprofiles state off

# After testing, re-enable
netsh advfirewall set allprofiles state on
```

**Check 2**: Network configuration
```powershell
# Verify IP settings
ipconfig /all

# Ping camera
ping 192.168.1.10  # Use your camera's IP
```

**Check 3**: Camera network settings
- Use vendor software to verify camera IP
- Ensure camera and PC are on same subnet
- Example: PC=192.168.1.100, Camera=192.168.1.10, Subnet=255.255.255.0

### ⚠️ Need to Use Vendor Software Again?

If you need to switch back to Basler Pylon or other vendor software:

**Option 1** - Reinstall vendor software (easiest)

**Option 2** - Use Zadig to restore original driver:
1. Open Zadig as Administrator
2. Select your camera
3. Choose the original driver (e.g., "PylonUSB")
4. Click "Replace Driver"

## Testing Your Setup

### Basic Test

```powershell
cd AravisSharp
dotnet run
```

Expected output:
```
=== AravisSharp Platform Information ===
OS: Microsoft Windows 10.0.26200
Architecture: X64
Framework: .NET 10.0.2

Aravis Library: libaravis-0.8-0.dll
Aravis Status: ✓ Available

=== Aravis Interfaces ===
Number of interfaces: 3
  [0] Fake
  [1] USB3Vision
  [2] GigEVision

Devices found: 1

=== AravisSharp Demo Menu ===
[...]
```

### Run Quick Demo

```powershell
# Type "8" and press Enter to run Quick Feature Demo
Choice: 8
```

Should show camera info:
```
═══ Device Information (Read-Only) ═══
  Vendor                    = Basler
  Model                     = acA720-520um
  Serial                    = 40013997
  [...]
```

### Run Unit Tests

```powershell
cd AravisSharp.Tests
dotnet test
```

Expected: All 29 tests pass

## Performance Notes

- **USB3Vision**: Supports up to 400 MB/s (USB 3.0 bandwidth)
- **GigE Vision**: Up to 125 MB/s (1 Gigabit Ethernet)
- **Frame Rate**: Depends on camera sensor and resolution
- **Latency**: Typical ~10-50ms from trigger to frame available

## Tested Hardware

| Camera | Interface | Status | Notes |
|--------|-----------|--------|-------|
| Basler acA720-520um | USB3Vision | ✅ Tested | Requires WinUSB driver |
| Generic GigE | GigEVision | ✅ Compatible | No driver needed |

## Next Steps

1. ✅ Install NuGet package
2. ✅ Setup WinUSB driver (USB cameras only)
3. ✅ Run demo application
4. 📖 Read [FEATURE_BROWSER_GUIDE.md](FEATURE_BROWSER_GUIDE.md) for GenICam features
5. 🔧 Integrate into your application

## Support

- **Documentation**: See [README.md](README.md) and [CROSS_PLATFORM_GUIDE.md](CROSS_PLATFORM_GUIDE.md)
- **Issues**: GitHub Issues
- **Aravis**: https://github.com/AravisProject/aravis
- **Zadig**: https://zadig.akeo.ie/

---

**Windows setup complete!** Your camera should now be accessible via AravisSharp.
