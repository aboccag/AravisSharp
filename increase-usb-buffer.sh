#!/bin/bash
# Raises the usbfs memory limit (16 MB by default) that USB3 Vision streaming needs.
set -euo pipefail

param=/sys/module/usbcore/parameters/usbfs_memory_mb
target_mb=1000

echo "=== Increasing USB Memory Buffer for Cameras ==="
echo

if [ ! -f "$param" ]; then
    echo "$param not found: usbcore is not loaded on this system."
    exit 1
fi

echo "Current USB memory buffer: $(cat "$param") MB"

echo "Setting USB memory buffer to $target_mb MB..."
echo "$target_mb" | sudo tee "$param" > /dev/null
echo "New USB memory buffer: $(cat "$param") MB"
echo

echo "Making change permanent..."
if [ -f /etc/default/grub ] && command -v update-grub > /dev/null 2>&1; then
    if grep -q "usbcore.usbfs_memory_mb=$target_mb" /etc/default/grub; then
        echo "Already in GRUB configuration"
    else
        sudo sed -i "s/GRUB_CMDLINE_LINUX_DEFAULT=\"/GRUB_CMDLINE_LINUX_DEFAULT=\"usbcore.usbfs_memory_mb=$target_mb /" /etc/default/grub
        sudo update-grub
        echo "Added usbcore.usbfs_memory_mb=$target_mb to the GRUB kernel command line"
    fi
else
    echo "GRUB with update-grub not found; the setting above lasts until reboot."
    echo "To make it permanent, add a modprobe option instead:"
    echo "  echo 'options usbcore usbfs_memory_mb=$target_mb' | sudo tee /etc/modprobe.d/usbcore.conf"
    echo "(if usbcore is built into the kernel, use the kernel parameter usbcore.usbfs_memory_mb=$target_mb)"
fi
echo

echo "Done."
