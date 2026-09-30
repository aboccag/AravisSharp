#!/bin/bash
# Gives the logged-in user access to USB3 Vision cameras by installing the udev rules that
# ship with Aravis (aravis/src/aravis.rules): MODE 0666 and the uaccess tag, per vendor ID.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
rules_src="$script_dir/aravis/src/aravis.rules"
# Numbered before systemd's 73-seat-late.rules, which is where the uaccess tag is applied.
rules_dest="/etc/udev/rules.d/70-aravis.rules"
# Written by earlier versions of this script; superseded by the Aravis rules.
legacy_rules="/etc/udev/rules.d/99-usb-vision.rules"

echo "=== USB Camera Permissions Setup ==="
echo

if [ "$EUID" -eq 0 ]; then
    echo "Please do not run this script as root. Run as normal user."
    exit 1
fi

if [ ! -f "$rules_src" ]; then
    echo "Aravis udev rules not found at $rules_src"
    echo "Fetch the submodule first: git submodule update --init"
    exit 1
fi

echo "1. Installing Aravis udev rules..."
sudo install -m 644 "$rules_src" "$rules_dest"
echo "   Installed $rules_dest"
if [ -f "$legacy_rules" ]; then
    sudo rm -f "$legacy_rules"
    echo "   Removed superseded $legacy_rules"
fi
echo

echo "2. Reloading udev rules..."
sudo udevadm control --reload-rules
sudo udevadm trigger --subsystem-match=usb
echo "   Done"
echo

echo "3. Checking for connected cameras..."
if command -v lsusb > /dev/null 2>&1; then
    vendor_ids="$(grep -o 'idVendor}=="[0-9a-fA-F]*"' "$rules_src" | cut -d'"' -f2 | paste -sd'|' -)"
    lsusb | grep -iE "ID (${vendor_ids}):" || echo "   No camera from a vendor listed in the rules is connected"
else
    echo "   lsusb not available, skipping"
fi
echo

echo "=== Setup Complete ==="
echo
echo "The rules apply to cameras plugged in from now on: unplug and replug a connected camera."
echo "No group membership or logout is needed."
echo
