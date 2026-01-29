#!/bin/bash

echo "=== USB Camera Permissions Setup ==="
echo

# Check if running as root
if [ "$EUID" -eq 0 ]; then 
    echo "Please do not run this script as root. Run as normal user."
    exit 1
fi

# Add user to video group
echo "1. Adding $USER to video group..."
sudo usermod -aG video $USER
echo "   ✓ Done"
echo

# Create udev rules for Basler cameras
echo "2. Creating udev rules for USB cameras..."
sudo tee /etc/udev/rules.d/99-usb-vision.rules > /dev/null <<EOF
# Basler USB3 cameras (vendor ID: 2676)
SUBSYSTEM=="usb", ATTRS{idVendor}=="2676", MODE="0666", GROUP="video"

# Generic USB3 Vision cameras
SUBSYSTEM=="usb", ENV{ID_VENDOR_ID}=="2676", MODE="0666", GROUP="video"

# USB device permissions
KERNEL=="video[0-9]*", MODE="0666", GROUP="video"
EOF
echo "   ✓ Created /etc/udev/rules.d/99-usb-vision.rules"
echo

# Reload udev rules
echo "3. Reloading udev rules..."
sudo udevadm control --reload-rules
sudo udevadm trigger
echo "   ✓ Done"
echo

# Check if camera is connected
echo "4. Checking for connected cameras..."
if command -v lsusb &> /dev/null; then
    echo "   USB devices:"
    lsusb | grep -i "basler\|vision" || echo "   No Basler cameras found in lsusb"
fi
echo

echo "=== Setup Complete ==="
echo
echo "IMPORTANT: You must LOGOUT and LOGIN again for group changes to take effect!"
echo
echo "After logout/login, verify with:"
echo "  groups  # Should show 'video' group"
echo "  dotnet run  # Should work without missing_packets errors"
echo
