#!/usr/bin/env bash
# check-setup.sh — Verify that all runtime dependencies for AravisSharp are present on Linux
set -u

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

echo "=== AravisSharp Linux Dependency Check ==="
echo ""

MISSING=()
OK=()

# Cache ldconfig output to a temp file to avoid pipefail issues
LDCACHE_FILE="$(mktemp)"
trap "rm -f '$LDCACHE_FILE'" EXIT
ldconfig -p 2>/dev/null > "$LDCACHE_FILE" || true

check_lib() {
    local name="$1"
    local soname="$2"
    if grep -q "$soname" "$LDCACHE_FILE"; then
        OK+=("$name ($soname)")
    else
        MISSING+=("$name ($soname)")
    fi
}

# Core Aravis library
check_lib "Aravis"          "libaravis-0.8.so.0"

# GLib / GObject / GIO (required by Aravis)
check_lib "GLib"            "libglib-2.0.so.0"
check_lib "GObject"         "libgobject-2.0.so.0"
check_lib "GIO"             "libgio-2.0.so.0"
check_lib "GModule"         "libgmodule-2.0.so.0"

# Other transitive deps
check_lib "libxml2"         "libxml2.so"
check_lib "libusb-1.0"      "libusb-1.0.so.0"
check_lib "zlib"            "libz.so"

echo "Found (${#OK[@]}):"
for item in "${OK[@]}"; do
    echo -e "  ${GREEN}✓${NC} $item"
done

echo ""

if [ ${#MISSING[@]} -gt 0 ]; then
    echo -e "${RED}Missing (${#MISSING[@]}):${NC}"
    for item in "${MISSING[@]}"; do
        echo -e "  ${RED}✗${NC} $item"
    done
    echo ""
    echo -e "${YELLOW}Install missing dependencies on Ubuntu/Debian:${NC}"
    echo "  sudo apt update"
    echo "  sudo apt install -y libglib2.0-0 libxml2 libusb-1.0-0 zlib1g"
    echo ""
    echo "On Fedora/RHEL:"
    echo "  sudo dnf install -y glib2 libxml2 libusb1 zlib"
    echo ""
    echo "If libaravis-0.8.so.0 is missing, install the AravisSharp.runtime.linux-x64 NuGet package"
    echo "or build Aravis from source (see build_aravis_linux_nuget.sh)."
else
    echo -e "${GREEN}All dependencies found! AravisSharp should work correctly.${NC}"
fi
