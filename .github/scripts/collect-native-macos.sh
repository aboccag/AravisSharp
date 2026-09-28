#!/usr/bin/env bash
# Collects libaravis and every non-system dylib it transitively needs into _native/, then
# rewrites their load paths to @loader_path so the folder works on a Mac without Homebrew.
#
# The dependency set is walked with otool, never listed by hand: a hardcoded list once
# expected libxml2.2.dylib while Homebrew shipped libxml2.16.dylib, and libaravis kept an
# absolute /opt/homebrew load path that exists on no user's machine.
set -euo pipefail

out=${1:-_native}
sources=${2:-_native-sources.txt}   # "<bundled name> <real source path>", for the versions record
brew_prefix=$(brew --prefix)
mkdir -p "$out"
: > "$sources"

is_system() { case "$1" in /usr/lib/*|/System/*) return 0 ;; *) return 1 ;; esac; }

load_commands() { otool -L "$1" | tail -n +2 | awk '{ print $1 }'; }

# Maps a load command to the file on this machine.
resolve() {
    case "$1" in
        @rpath/*|@loader_path/*|@executable_path/*)
            find "$PWD/_install/lib" "$brew_prefix/lib" "$brew_prefix"/opt/*/lib \
                -maxdepth 1 -name "${1##*/}" 2>/dev/null | head -n 1 ;;
        *)
            if [ -e "$1" ]; then echo "$1"; fi ;;
    esac
}

copy_in() {
    cp -L "$1" "$out/$2"
    chmod u+w "$out/$2"
    echo "$2 $(realpath "$1")" >> "$sources"
}

bundle() {
    local dep name src
    while read -r dep; do
        is_system "$dep" && continue
        name=${dep##*/}
        [ -e "$out/$name" ] && continue
        # find exits non-zero on absent search dirs; an empty result is reported below.
        src=$(resolve "$dep" || true)
        if [ -z "$src" ]; then
            echo "::error::cannot resolve $dep, needed by ${1##*/}" >&2
            exit 1
        fi
        copy_in "$src" "$name"
        bundle "$out/$name"
    done < <(load_commands "$1")
}

aravis=$(find "$PWD/_install" -name 'libaravis-0.8.0.dylib' | head -n 1)
if [ -z "$aravis" ]; then
    echo "::error::libaravis-0.8.0.dylib not found under _install" >&2
    exit 1
fi
copy_in "$aravis" libaravis-0.8.0.dylib
bundle "$out/libaravis-0.8.0.dylib"

for dylib in "$out"/*.dylib; do
    name=${dylib##*/}
    install_name_tool -id "@loader_path/$name" "$dylib"
    while read -r dep; do
        is_system "$dep" && continue
        [ "$dep" = "@loader_path/${dep##*/}" ] && continue
        install_name_tool -change "$dep" "@loader_path/${dep##*/}" "$dylib"
    done < <(load_commands "$dylib")
    # rpaths into the build machine's Homebrew or build tree are meaningless elsewhere.
    while read -r rpath; do
        install_name_tool -delete_rpath "$rpath" "$dylib"
    done < <(otool -l "$dylib" | awk '/cmd LC_RPATH/ { getline; getline; print $2 }')
    # Editing load commands invalidates the signature, and arm64 refuses to load unsigned code.
    codesign --force --sign - "$dylib"
done

ls -la "$out"
