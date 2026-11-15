#!/usr/bin/env bash
# Collects libaravis and every MinGW DLL it transitively needs into _native/.
# Runs in an MSYS2 MinGW-type shell (UCRT64 in CI), after Aravis was installed into
# $MINGW_PREFIX.
#
# The dependency set is derived with ldd, never listed by hand: a hardcoded list silently
# shipped without libxml2 once MSYS2 renamed libxml2-2.dll to libxml2-16.dll.
set -euo pipefail

out=${1:-_native}
prefix=${MINGW_PREFIX:?run in an MSYS2 MinGW-type shell (UCRT64 in CI): MINGW_PREFIX is not set}
aravis=$prefix/bin/libaravis-0.8-0.dll
mkdir -p "$out"
cp "$aravis" "$out/"

# ldd resolves the whole tree. Keep what lives in $prefix/bin; everything else is a
# Windows system DLL that every machine already has.
ldd "$aravis" | awk -v bin="$prefix/bin/" 'index($3, bin) == 1 { print $3 }' | sort -u | while read -r dll; do
    cp "$dll" "$out/"
done

if ldd "$aravis" | grep 'not found' >/dev/null; then
    ldd "$aravis" | grep 'not found' >&2
    echo "::error::libaravis has unresolved dependencies" >&2
    exit 1
fi

ls -la "$out"
