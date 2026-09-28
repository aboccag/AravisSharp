#!/usr/bin/env bash
# Fails when libaravis has unresolved dependencies, resolves anything into the build tree,
# or carries a search path other than $ORIGIN. Prints the glibc floor, which decides the
# oldest distribution the package runs on.
set -euo pipefail

dir=${1:-_native}
lib="$dir/libaravis-0.8.so.0"
[ -f "$lib" ] || { echo "::error::$lib is missing" >&2; exit 1; }

bad=0
runpath=$(readelf -d "$lib" | awk '/\((RUNPATH|RPATH)\)/ { print $NF }')
if [ "$runpath" != '[$ORIGIN]' ]; then
    echo "::error::unexpected library search path '$runpath', expected [\$ORIGIN]" >&2
    bad=1
fi

deps=$(ldd "$lib")
if grep -q 'not found' <<<"$deps"; then
    grep 'not found' <<<"$deps" >&2
    echo "::error::libaravis has unresolved dependencies" >&2
    bad=1
fi
if grep -qE "$PWD/(_install|_build)" <<<"$deps"; then
    echo "::error::libaravis resolves libraries from the build tree" >&2
    bad=1
fi

needed=$(readelf -d "$lib" | awk '/\(NEEDED\)/ { gsub(/[][]/, "", $NF); print $NF }' | tr '\n' ' ')
glibc=$(objdump -T "$lib" | grep -o 'GLIBC_[0-9.]*' | sort -uV | tail -n 1)
echo "NEEDED: $needed"
echo "glibc floor: $glibc"
[ "$bad" -eq 0 ] && echo "linux payload is valid"
exit "$bad"
