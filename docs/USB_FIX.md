# AravisSharp — Linux USB3 Vision Camera Setup

## Quick Fix

```bash
# 1. Set up udev permissions (one-time)
./setup-usb-permissions.sh

# 2. Unplug and replug the camera (the rules apply to devices added from now on)

# 3. Optionally raise the usbfs memory limit for high-speed capture
./increase-usb-buffer.sh

# 4. Run the examples (from the repository root)
dotnet run --project AravisSharp.Examples -f net10.0
```

---

## What `setup-usb-permissions.sh` Does

1. Installs the udev rules that ship with Aravis (`aravis/src/aravis.rules`) as `/etc/udev/rules.d/70-aravis.rules`. They match USB3 Vision cameras by vendor ID, open them to all users (`MODE:="0666"`) and tag them `uaccess`, so no root access is needed.
2. Removes `/etc/udev/rules.d/99-usb-vision.rules`, written by earlier versions of the script.
3. Reloads udev rules and lists connected cameras from the vendors the rules cover.

No group membership or logout is needed: **unplug and replug** a camera that was already connected.

The `aravis/` submodule must be checked out (`git submodule update --init`).

### Verify

```bash
# Check the rules are installed
ls /etc/udev/rules.d/70-aravis.rules

# Check camera is visible
lsusb | grep -i basler
# Example: Bus 002 Device 004: ID 2676:ba02 Basler AG ace

# Run the examples
dotnet run --project AravisSharp.Examples -f net10.0
# Should show "Devices found: 1"
```

---

## Common USB Issues

### Missing Packets / Incomplete Frames

**Symptom**: Frames arrive with `Status: Missing_packets`

**Causes & Fixes**:

| Cause | Fix |
|-------|-----|
| usbfs memory limit too small (16 MB by default) | `./increase-usb-buffer.sh` (see [USB Buffer Size](#usb-buffer-size)) |
| USB 2.0 port / cable | Use a USB 3.0 port and a proper USB 3.0 cable |
| Hub bottleneck | Connect camera directly to the motherboard USB 3.0 port |
| Permissions | Run `./setup-usb-permissions.sh` and replug the camera |

### Permission Denied

**Symptom**: Camera detected by `lsusb` but not by AravisSharp (or `arv-tool-0.8`)

```bash
# Fix: set up udev rules
./setup-usb-permissions.sh
# Then unplug / replug the camera
```

### Camera Not Detected at All

```bash
# Check USB connection
lsusb | grep -i "basler\|vision\|2676"

# Check Aravis can see it (if arv-tool is installed)
arv-tool-0.8 -l

# Check dmesg for USB errors
dmesg | tail -20
```

---

## Manual udev Rule

If your camera's vendor is not in `aravis/src/aravis.rules`, add a rule for it in the same style:

```bash
# Find your camera's vendor:product ID
lsusb
# Example output: Bus 002 Device 004: ID 1ab2:0001 Allied Vision

# Create rule (numbered before systemd's 73-seat-late.rules, which applies uaccess)
sudo tee /etc/udev/rules.d/70-my-camera.rules << EOF
SUBSYSTEM=="usb", ATTRS{idVendor}=="1ab2", TAG+="uaccess"
EOF

sudo udevadm control --reload-rules
sudo udevadm trigger --subsystem-match=usb
```

`uaccess` grants access to the user logged in at the machine. For a headless or SSH-only machine, use `GROUP="plugdev", MODE="0660"` instead of the tag and add the user to `plugdev`.

USB3 Vision camera vendor IDs covered by the Aravis rules:

| Vendor | USB ID |
|--------|--------|
| Basler | `2676` |
| The Imaging Source | `199e` |
| Point Grey / FLIR / Teledyne | `1e10` |
| Daheng Imaging | `2ba2` |
| Dahua Technology | `2e03` |
| Omron Sentech | `1421` |
| IDS | `1409` |
| Hikrobot | `2bdf` |

Other vendors, such as Allied Vision (`1ab2`) or Ximea (`20f7`), need a manual rule.

---

## USB Buffer Size

For high-speed acquisition (>100 fps or large images), raise the kernel's usbfs memory limit (16 MB by default), which caps the USB transfers in flight for all devices. `./increase-usb-buffer.sh` does both steps below.

```bash
# Temporary (lost on reboot)
echo 1000 | sudo tee /sys/module/usbcore/parameters/usbfs_memory_mb

# Permanent: add the kernel parameter usbcore.usbfs_memory_mb=1000,
# e.g. to GRUB_CMDLINE_LINUX_DEFAULT in /etc/default/grub, then:
sudo update-grub
```

`net.core.rmem_max` is a UDP socket buffer limit: it matters for GigE Vision cameras, not USB.
