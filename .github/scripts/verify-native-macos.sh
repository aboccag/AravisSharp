#!/usr/bin/env bash
# Fails when a bundled dylib loads anything but a system library or a bundled sibling, or
# carries an invalid signature. An absolute Homebrew path loads on the build machine and on
# any Mac with the same formula installed, and nowhere else.
#
# Fail-closed: a check that cannot read the load commands must not report success.
set -euo pipefail

dir=${1:-_native}
command -v otool >/dev/null || { echo "::error::otool is required" >&2; exit 1; }

bad=0
for dylib in "$dir"/*.dylib; do
    name=${dylib##*/}
    deps=$(otool -L "$dylib" | tail -n +2 | awk '{ print $1 }')
    if [ -z "$deps" ]; then
        echo "::error::could not read the load commands of $name" >&2
        exit 1
    fi
    for dep in $deps; do
        case "$dep" in
            /usr/lib/*|/System/*) ;;
            @loader_path/*)
                if [ ! -e "$dir/${dep#@loader_path/}" ]; then
                    echo "::error::$name loads $dep, which is not bundled" >&2
                    bad=1
                fi ;;
            *)
                echo "::error::$name loads $dep, a path that will not exist on a user's machine" >&2
                bad=1 ;;
        esac
    done
    if ! codesign --verify "$dylib" 2>/dev/null; then
        echo "::error::$name has an invalid code signature" >&2
        bad=1
    fi
done
[ "$bad" -eq 0 ] && echo "osx payload is self-contained: $(ls "$dir"/*.dylib | wc -l) dylibs"
exit "$bad"
