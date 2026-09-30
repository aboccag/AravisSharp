#!/usr/bin/env bash
# Builds libxml2 as a static, position-independent library into <prefix>, for the Linux
# libaravis to link in.
#
# Distributions disagree on libxml2's soname: libxml2.so.2 up to Ubuntu 24.04 and
# Debian 12, libxml2.so.16 from Ubuntu 25.10 / 26.04 LTS, which no longer ship .so.2 at
# all. A libaravis linked against the shared library loads on one side of that line only.
# Linked statically with its symbols hidden (see the -Wl,--exclude-libs flag at the meson
# step), it needs neither and cannot clash with a system libxml2 another component loads.
#
# Minimal feature set: Aravis only parses GenICam XML from memory.
set -euo pipefail

prefix=${1:?usage: build-libxml2-static.sh <prefix>}
version=2.13.9
sha256=a2c9ae7b770da34860050c309f903221c67830c86e4a7e760692b803df95143a  # from download.gnome.org

work=$(mktemp -d)
curl -fsSL -o "$work/libxml2.tar.xz" \
    "https://download.gnome.org/sources/libxml2/${version%.*}/libxml2-$version.tar.xz"
echo "$sha256  $work/libxml2.tar.xz" | sha256sum -c -
tar -xJf "$work/libxml2.tar.xz" -C "$work"

cd "$work/libxml2-$version"
./configure --prefix="$prefix" --disable-shared --enable-static --with-pic \
    --without-python --without-icu --without-lzma --without-zlib --without-http
make -j"$(nproc)"
make install

echo "libxml2 $version (static) installed into $prefix"
