#!/usr/bin/env bash
# Fails when a bundled DLL imports a MinGW DLL that is not bundled next to it.
# Such a payload loads on the build machine and on any machine with MSYS2 on PATH, and
# nowhere else. Runs in the MSYS2 MINGW64 shell.
#
# Fail-closed: a check that cannot read the imports must not report success.
set -euo pipefail

dir=${1:-_native}
command -v objdump >/dev/null || { echo "::error::objdump is required (mingw-w64-x86_64-binutils)" >&2; exit 1; }

missing=0
for dll in "$dir"/*.dll; do
    imports=$(objdump -p "$dll" | awk '/DLL Name:/ { print $3 }')
    if [ -z "$imports" ]; then
        # Every Windows DLL imports at least KERNEL32; an empty list means the parse failed.
        echo "::error::could not read the imports of $(basename "$dll")" >&2
        exit 1
    fi
    for import in $imports; do
        [ -e "$dir/$import" ] && continue
        if [ -e "/mingw64/bin/$import" ]; then
            echo "::error::$(basename "$dll") imports $import, which is not bundled" >&2
            missing=1
        fi
    done
done
[ "$missing" -eq 0 ] && echo "win-x64 payload is self-contained: $(ls "$dir"/*.dll | wc -l) DLLs"
exit "$missing"
