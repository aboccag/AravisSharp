#!/usr/bin/env bash
# Collects libaravis and every MinGW DLL it transitively needs into _native/.
# Runs in the MSYS2 MINGW64 shell, after Aravis was installed into /mingw64.
#
# The dependency set is derived with ldd, never listed by hand: a hardcoded list silently
# shipped without libxml2 once MSYS2 renamed libxml2-2.dll to libxml2-16.dll.
set -euo pipefail

out=${1:-_native}
aravis=/mingw64/bin/libaravis-0.8-0.dll
mkdir -p "$out"
cp "$aravis" "$out/"

# ldd resolves the whole tree. Keep what lives in /mingw64/bin; everything else is a
# Windows system DLL that every machine already has.
ldd "$aravis" | awk '$3 ~ "^/mingw64/bin/" { print $3 }' | sort -u | while read -r dll; do
    cp "$dll" "$out/"
done

if ldd "$aravis" | grep -q 'not found'; then
    ldd "$aravis" | grep 'not found' >&2
    echo "::error::libaravis has unresolved dependencies" >&2
    exit 1
fi

ls -la "$out"
