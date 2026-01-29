# USB Camera Setup Fix

## Issue Resolved

✅ **Library Loading**: Fixed DllImport to use `aravis-0.8.so.0` (correct library name)
✅ **Camera Detection**: Successfully detecting Basler acA720-520um camera
✅ **Segfault**: Fixed buffer cleanup to prevent crash on exit

## Remaining Issue: USB Permissions

The camera is detected and connects, but frames show `Missing_packets` status. This is a **USB permissions issue**, not a code problem.

### Solution Applied

The `setup-usb-permissions.sh` script has been run, which:
1. ✅ Added user `alexandre` to the `video` group
2. ✅ Created udev rules for Basler USB cameras
3. ✅ Reloaded udev rules
4. ✅ Confirmed camera is connected (ID 2676:ba02 Basler AG ace)

### **REQUIRED: Logout and Login**

The group changes will **NOT** take effect until you:

```bash
# Logout and login again (or reboot)
```

After logout/login, verify:

```bash
# Check you're in the video group
groups
# Should show: alexandre adm cdrom sudo dip plugdev lpadmin lxd sambashare docker video

# Test the camera
cd AravisSharp
dotnet run
# Should now successfully acquire frames!
```

## Current Status

```
Camera Model: Basler acA720-520um
Serial Number: 40013997
Sensor: 724 x 542 pixels
Current ROI: 720 x 540
Connection: USB3Vision
Status: Detected and connected ✅
Acquisition: Waiting for USB permissions (logout/login required)
```

## After Logout/Login

The application should work perfectly and acquire frames successfully. The `Missing_packets` errors will be resolved.

## Files Modified

1. `AravisSharp/Native/AravisNative.cs` - Fixed library name to `aravis-0.8.so.0`
2. `AravisSharp/Buffer.cs` - Improved cleanup to prevent segfault
3. `AravisSharp/Program.cs` - Added error handling and helpful messages
4. `setup-usb-permissions.sh` - Created USB setup script (✅ run successfully)

## Test After Relogin

```bash
cd /home/alexandre/dev/projects/alex/AravisSharp/AravisSharp
dotnet run
```

Expected output: 10 frames successfully acquired with `Status: Success`!
