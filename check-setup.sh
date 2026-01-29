#!/bin/bash

echo "=== Aravis Installation Check ==="
echo

# Check if Aravis library is installed
echo "1. Checking for Aravis library..."
if ldconfig -p | grep -q aravis; then
    echo "   ✓ Aravis library found:"
    ldconfig -p | grep aravis | head -3
else
    echo "   ✗ Aravis library NOT found"
    echo "   Install with: sudo apt-get install libaravis-0.8-0 libaravis-0.8-dev"
fi
echo

# Check for arv-tool
echo "2. Checking for arv-tool..."
if command -v arv-tool-0.8 &> /dev/null; then
    echo "   ✓ arv-tool-0.8 found"
else
    echo "   ✗ arv-tool-0.8 NOT found"
    echo "   Install with: sudo apt-get install aravis-tools-0.8"
fi
echo

# List available cameras
echo "3. Scanning for cameras..."
if command -v arv-tool-0.8 &> /dev/null; then
    arv-tool-0.8 -l
else
    echo "   Cannot scan - arv-tool not installed"
fi
echo

# Check USB permissions
echo "4. Checking USB permissions..."
if [ -f /etc/udev/rules.d/99-aravis.rules ]; then
    echo "   ✓ USB udev rules found"
else
    echo "   ✗ USB udev rules NOT found"
    echo "   This may prevent USB camera access"
    echo "   See README.md for setup instructions"
fi
echo

# Check network buffer settings (for GigE)
echo "5. Checking network buffer settings (for GigE cameras)..."
rmem_max=$(sysctl -n net.core.rmem_max 2>/dev/null || echo "0")
if [ "$rmem_max" -ge 33554432 ]; then
    echo "   ✓ Network buffer size is adequate: $rmem_max bytes"
else
    echo "   ⚠ Network buffer size may be too small: $rmem_max bytes"
    echo "   Recommended: 33554432 bytes (32 MB)"
    echo "   Set with: sudo sysctl -w net.core.rmem_max=33554432"
fi
echo

# Check for .NET SDK
echo "6. Checking for .NET SDK..."
if command -v dotnet &> /dev/null; then
    echo "   ✓ .NET SDK found:"
    dotnet --version
else
    echo "   ✗ .NET SDK NOT found"
fi
echo

echo "=== Check Complete ==="
