#!/usr/bin/env bash
# Collects libaravis into _native/. Linux ships Aravis only: GLib, libxml2 and libusb come
# from the distribution. Bundling them dragged in ICU (29 MB, with a distro-specific
# soname), libstdc++, libudev and libselinux, and a second GLib in a process that also
# loads the system one (GStreamer, GTK) means two GObject type systems side by side.
#
# NuGet packages are zip files and cannot carry symlinks, so ship the SONAME alone.
set -euo pipefail

out=${1:-_native}
mkdir -p "$out"

aravis=$(find _install -name 'libaravis-0.8.so.0' | head -n 1)
if [ -z "$aravis" ]; then
    echo "::error::libaravis-0.8.so.0 not found under _install" >&2
    exit 1
fi
cp -L "$aravis" "$out/libaravis-0.8.so.0"
patchelf --set-rpath '$ORIGIN' "$out/libaravis-0.8.so.0"

ls -la "$out"
