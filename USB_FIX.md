# AravisSharp — Linux USB3 Vision Camera Setup

## Quick Fix

```bash
# 1. Set up udev permissions (one-time)
./setup-usb-permissions.sh

# 2. Log out and back in (required for group changes)

# 3. Optionally increase USB buffer size for high-speed capture
./increase-usb-buffer.sh

# 4. Run
cd AravisSharp
dotnet run
```

---

## What `setup-usb-permissions.sh` Does

1. Adds your user to the `video` and `plugdev` groups.
2. Creates udev rules at `/etc/udev/rules.d/99-aravis.rules` so USB3 Vision cameras are accessible without root.
3. Reloads udev rules.

**You must log out and back in** (or reboot) for the group membership to take effect.

### Verify

```bash
# Check groups
groups
# Should include: video plugdev

# Check camera is visible
lsusb | grep -i basler
# Example: Bus 002 Device 004: ID 2676:ba02 Basler AG ace

# Run the app
cd AravisSharp && dotnet run
# Should show "Devices found: 1"
```

---

## Common USB Issues

### Missing Packets / Incomplete Frames

**Symptom**: Frames arrive with `Status: Missing_packets`

**Causes & Fixes**:

| Cause | Fix |
|-------|-----|
| USB buffer too small | `./increase-usb-buffer.sh` or `sudo sysctl -w net.core.rmem_max=33554432` |
| USB 2.0 port / cable | Use a USB 3.0 port and a proper USB 3.0 cable |
| Hub bottleneck | Connect camera directly to the motherboard USB 3.0 port |
| Permissions | Run `./setup-usb-permissions.sh` and re-login |

### Permission Denied

**Symptom**: Camera detected by `lsusb` but not by AravisSharp (or `arv-tool-0.8`)

```bash
# Fix: set up udev rules
./setup-usb-permissions.sh
# Then log out / back in
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

If the script doesn't cover your camera, create a rule manually:

```bash
# Find your camera's vendor:product ID
lsusb
# Example output: Bus 002 Device 004: ID 2676:ba02 Basler AG

# Create rule
sudo tee /etc/udev/rules.d/99-my-camera.rules << EOF
SUBSYSTEM=="usb", ATTR{idVendor}=="2676", MODE="0666"
EOF

sudo udevadm control --reload-rules
sudo udevadm trigger
```

Common USB3 Vision camera vendor IDs:

| Vendor | USB ID |
|--------|--------|
| Basler | `2676` |
| FLIR / Teledyne | `1e10` |
| Allied Vision | `1ab2` |
| IDS | `1409` |
| Ximea | `20f7` |

---

## USB Buffer Size

For high-speed acquisition (>100 fps or large images), increase the USB buffer:

```bash
# Temporary (lost on reboot)
sudo sysctl -w net.core.rmem_max=33554432

# Permanent
echo "net.core.rmem_max=33554432" | sudo tee -a /etc/sysctl.d/99-usb-buffer.conf
sudo sysctl --system
```
