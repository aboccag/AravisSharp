#!/usr/bin/env bash
set -euo pipefail

VERSION="0.8.33"

# Où tu veux sortir les fichiers prêts à packager
OUT_ROOT="$HOME/dev/AravisSharpLinux"
RID="linux-x64"
NATIVE_DIR="$OUT_ROOT/runtimes/$RID/native"

WORK="$OUT_ROOT/_work"
TARBALL="$WORK/aravis-$VERSION.tar.gz"
SRC="$WORK/aravis-$VERSION"
BUILD="$SRC/build"
STAGE="$SRC/stage"

echo "== Install build deps (minimal, no viewer/gst) =="
sudo apt update
sudo apt install -y \
  meson ninja-build pkg-config cmake \
  libxml2-dev libglib2.0-dev libusb-1.0-0-dev \
  gobject-introspection libgirepository1.0-dev gettext

mkdir -p "$NATIVE_DIR"
rm -rf "$WORK"
mkdir -p "$WORK"
cd "$WORK"

echo "== Download Aravis $VERSION sources =="
curl -L -o "$TARBALL" "https://github.com/AravisProject/aravis/archive/refs/tags/$VERSION.tar.gz"
tar -xzf "$TARBALL"

echo "== Configure + Build (no viewer, no gst plugin) =="
cd "$SRC"
meson setup "$BUILD" --prefix=/usr -Dviewer=disabled -Dgst-plugin=disabled -Dintrospection=disabled
ninja -C "$BUILD"

echo "== Staging install (no system install) =="
rm -rf "$STAGE"
DESTDIR="$STAGE" ninja -C "$BUILD" install

echo "== Copy lib to NuGet layout =="
# On récupère le fichier SONAME (libaravis-0.8.so.0*)
LIB="$(find "$STAGE" -type f -name "libaravis-0.8.so.0*" | head -n 1)"
if [[ -z "${LIB:-}" ]]; then
  echo "ERROR: libaravis-0.8.so.0 not found in $STAGE"
  exit 1
fi

cp -v "$LIB" "$NATIVE_DIR/libaravis-0.8.so.0"

echo "== Create minimal nuspec (for NuGet Package Explorer) =="
cat > "$OUT_ROOT/AravisSharp.runtime.$RID.nuspec" <<EOF
<?xml version="1.0"?>
<package>
  <metadata>
    <id>AravisSharp.runtime.$RID</id>
    <version>$VERSION.0</version>
    <authors>AravisSharp</authors>
    <description>Native Aravis $VERSION for $RID (GenICam camera control), packaged for AravisSharp.</description>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <tags>aravis genicam gigE usb3vision vision</tags>
  </metadata>
</package>
EOF

echo ""
echo "DONE"
echo "Native file: $NATIVE_DIR/libaravis-0.8.so.0"
echo "Nuspec:      $OUT_ROOT/AravisSharp.runtime.$RID.nuspec"
echo ""
echo "Runtime deps to document on Ubuntu:"
echo "  sudo apt install libglib2.0-0 libgobject-2.0-0 libgio-2.0-0 libxml2 libusb-1.0-0 zlib1g"
