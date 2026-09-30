#!/usr/bin/env bash
# Checks that this machine can run an application using the AravisSharp NuGet package.
#
# The package bundles libaravis (libxml2 linked in) for linux-x64, linux-arm64, osx-arm64 and
# win-x64, and the binding loads that copy before any system one. On Linux, libaravis needs
# GLib, zlib and libusb from the distribution; USB3 Vision cameras also need udev rules
# (./setup-usb-permissions.sh). Exits 1 when something required is missing.
set -euo pipefail

if [ -t 1 ]; then
    red=$'\033[0;31m' green=$'\033[0;32m' yellow=$'\033[0;33m' reset=$'\033[0m'
else
    red='' green='' yellow='' reset=''
fi
failures=0
ok()   { echo "  ${green}ok${reset}    $*"; }
warn() { echo "  ${yellow}warn${reset}  $*"; }
info() { echo "  info  $*"; }
fail() { echo "  ${red}FAIL${reset}  $*"; failures=$((failures + 1)); }
finish() {
    echo
    if [ "$failures" -eq 0 ]; then
        echo "${green}Everything AravisSharp needs is present.${reset}"
        exit 0
    fi
    echo "${red}$failures required item(s) missing.${reset}"
    exit 1
}

# True when version $1 >= version $2.
version_ge() { [ "$(printf '%s\n' "$2" "$1" | sort -V | sed -n 1p)" = "$2" ]; }

echo "=== AravisSharp setup check ==="

echo
echo ".NET (the package targets net8.0 and net10.0)"
if command -v dotnet >/dev/null 2>&1; then
    runtimes=$(dotnet --list-runtimes 2>/dev/null | awk '$1 == "Microsoft.NETCore.App" { print $2 }' | sort -V || true)
    newest=$(tail -n 1 <<<"$runtimes")
    if [ -n "$newest" ] && version_ge "$newest" 8.0; then
        ok ".NET runtime: $(tr '\n' ' ' <<<"$runtimes")"
    else
        fail "no .NET runtime 8.0 or later (found: ${runtimes:-none}): https://dotnet.microsoft.com/download"
    fi
    sdks=$(dotnet --list-sdks 2>/dev/null | awk '{ print $1 }' | tr '\n' ' ' || true)
    if [ -n "$sdks" ]; then
        ok ".NET SDK: $sdks"
    else
        info "no .NET SDK: needed to build, not to run a published application"
    fi
else
    fail "dotnet not found: install .NET 8 or later from https://dotnet.microsoft.com/download"
fi

case "$(uname -s)" in
    Linux) ;;
    Darwin)
        echo
        if [ "$(uname -m)" = arm64 ]; then
            ok "macOS on Apple Silicon: the package bundles libaravis and its dylibs (osx-arm64)"
        elif [ "$(sysctl -n hw.optional.arm64 2>/dev/null || true)" = 1 ]; then
            warn "this shell runs under Rosetta: an x64 .NET process cannot load the osx-arm64 libaravis; use the arm64 .NET"
        else
            fail "Intel Mac: the package ships libaravis for Apple Silicon (osx-arm64) only"
        fi
        finish ;;
    MINGW* | MSYS* | CYGWIN*)
        echo
        info "Windows: the package bundles libaravis and its DLLs (win-x64)"
        info "USB3 Vision cameras need the WinUSB driver (Zadig): see WINDOWS_SETUP.md"
        finish ;;
    *)
        echo
        fail "$(uname -s): the package ships native libraries for Linux, macOS and Windows only"
        finish ;;
esac

echo
# ld_tag: this architecture's tag in ldconfig -p; multiarch: its Debian library directory.
ld_tag='' multiarch=''
case "$(uname -m)" in
    x86_64)
        ok "architecture x86_64: the package ships linux-x64"
        ld_tag=x86-64 multiarch=x86_64-linux-gnu ;;
    aarch64 | arm64)
        ok "architecture $(uname -m): the package ships linux-arm64"
        ld_tag=AArch64 multiarch=aarch64-linux-gnu ;;
    *)
        fail "architecture $(uname -m): the package ships linux-x64 and linux-arm64 only" ;;
esac

# The bundled libaravis is built on Ubuntu 20.04: it needs glibc 2.31 and GLib 2.64 or later.
glibc=$(getconf GNU_LIBC_VERSION 2>/dev/null | awk '{ print $2 }' || true)
if [ -z "$glibc" ]; then
    fail "glibc not found (musl distributions such as Alpine are not supported)"
elif version_ge "$glibc" 2.31; then
    ok "glibc $glibc (2.31 or later)"
else
    fail "glibc $glibc is older than 2.31, the glibc the bundled libaravis is built against"
fi

ldconfig=$(command -v ldconfig || echo /sbin/ldconfig)
ldcache=$("$ldconfig" -p 2>/dev/null || true)
if [ -z "$ldcache" ]; then
    info "no dynamic linker cache ($ldconfig -p): looking in LD_LIBRARY_PATH and the standard directories"
fi

# Prints where the dynamic linker finds soname $1 for this architecture: the ldconfig cache entry
# tagged with it (never a 32-bit multilib copy), else LD_LIBRARY_PATH and the standard
# directories, for a missing cache or libraries installed without running ldconfig.
find_lib() {
    local so=$1 path='' dir
    if [ -n "$ld_tag" ]; then
        path=$(awk -v so="$so" -v tag="$ld_tag" '$1 == so && index($2, tag) { print $NF; exit }' <<<"$ldcache")
    fi
    if [ -n "$path" ]; then
        echo "$path"
        return 0
    fi
    local IFS=:
    # shellcheck disable=SC2086  # split on ':' on purpose
    for dir in ${LD_LIBRARY_PATH:-} ${multiarch:+/lib/$multiarch:/usr/lib/$multiarch} /lib64 /usr/lib64 /usr/local/lib /lib /usr/lib; do
        if [ -n "$dir" ] && [ -e "$dir/$so" ]; then
            echo "$dir/$so"
            return 0
        fi
    done
    return 0
}

echo
echo "Distribution libraries libaravis links against"
apt_missing=()
check_lib() {
    local soname=$1 package=$2 path
    path=$(find_lib "$soname")
    if [ -n "$path" ]; then
        ok "$soname ($path)"
    else
        fail "$soname not found"
        apt_missing+=("$package")
    fi
}
check_lib libglib-2.0.so.0    libglib2.0-0
check_lib libgobject-2.0.so.0 libglib2.0-0
check_lib libgio-2.0.so.0     libglib2.0-0
check_lib libz.so.1           zlib1g
# Linked in, so libaravis does not load without it, GigE cameras included.
check_lib libusb-1.0.so.0     libusb-1.0-0

# libglib-2.0.so.0.6400.6 is GLib 2.64.6.
glib_path=$(find_lib libglib-2.0.so.0)
if [ -n "$glib_path" ]; then
    glib_minor=$(readlink -f "$glib_path" | sed -nE 's/.*\.so\.0\.([0-9]+)\.[0-9]+$/\1/p')
    if [ -n "$glib_minor" ]; then
        glib="2.$((10#$glib_minor / 100))"
        if version_ge "$glib" 2.64; then
            ok "GLib $glib (2.64 or later)"
        else
            fail "GLib $glib is older than 2.64, the GLib the bundled libaravis is built against"
        fi
    fi
fi

if [ ${#apt_missing[@]} -gt 0 ]; then
    echo
    echo "  Debian / Ubuntu:  sudo apt install -y $(printf '%s\n' "${apt_missing[@]}" | sort -u | tr '\n' ' ')"
    echo "                    (Ubuntu 24.04 and later name the GLib package libglib2.0-0t64)"
    echo "  Fedora / RHEL:    sudo dnf install -y glib2 zlib libusb1"
fi

echo
echo "USB3 Vision cameras (not needed for GigE Vision)"
if [ -f /etc/udev/rules.d/70-aravis.rules ]; then
    ok "udev rules installed (/etc/udev/rules.d/70-aravis.rules)"
else
    warn "Aravis udev rules not installed: run ./setup-usb-permissions.sh, then replug the camera"
fi
if [ -f /etc/udev/rules.d/99-usb-vision.rules ]; then
    warn "/etc/udev/rules.d/99-usb-vision.rules is superseded: ./setup-usb-permissions.sh removes it"
fi
usbfs=/sys/module/usbcore/parameters/usbfs_memory_mb
if [ -r "$usbfs" ]; then
    usbfs_mb=$(cat "$usbfs")
    if [ "$usbfs_mb" -eq 0 ] || [ "$usbfs_mb" -ge 1000 ]; then
        ok "usbfs memory limit: $usbfs_mb MB (0 means unlimited)"
    else
        warn "usbfs memory limit is $usbfs_mb MB: raise it with ./increase-usb-buffer.sh for high frame rates"
    fi
fi

echo
echo "System Aravis (optional)"
system_aravis=$(find_lib libaravis-0.8.so.0)
if [ -n "$system_aravis" ]; then
    info "system libaravis at $system_aravis: loaded only when the application carries no bundled copy"
else
    info "no system libaravis: the application loads the copy bundled in the package"
fi

finish
