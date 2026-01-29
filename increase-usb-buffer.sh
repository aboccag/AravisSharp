#!/bin/bash

echo "=== Increasing USB Memory Buffer for Cameras ==="
echo

# Check current value
current=$(cat /sys/module/usbcore/parameters/usbfs_memory_mb)
echo "Current USB memory buffer: ${current} MB"

# Set to 1000MB for USB3 cameras
echo "Setting USB memory buffer to 1000 MB..."
echo 1000 | sudo tee /sys/module/usbcore/parameters/usbfs_memory_mb > /dev/null

# Make it permanent
echo "Making change permanent..."
if ! grep -q "usbcore.usbfs_memory_mb=1000" /etc/default/grub; then
    sudo sed -i 's/GRUB_CMDLINE_LINUX_DEFAULT="/GRUB_CMDLINE_LINUX_DEFAULT="usbcore.usbfs_memory_mb=1000 /' /etc/default/grub
    sudo update-grub
    echo "✓ Added to GRUB configuration"
else
    echo "✓ Already in GRUB configuration"
fi

new=$(cat /sys/module/usbcore/parameters/usbfs_memory_mb)
echo
echo "New USB memory buffer: ${new} MB"
echo "✓ Done"
echo
echo "You can now run: dotnet run"
