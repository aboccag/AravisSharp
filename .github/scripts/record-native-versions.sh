#!/usr/bin/env bash
# Writes aravis-native-versions.txt next to the bundled libraries: the Aravis commit and the
# exact version of every third-party library, so its corresponding source can be found.
set -euo pipefail

dir=${1:-_native}
sources=${2:-_native-sources.txt}
out="$dir/aravis-native-versions.txt"

aravis_version=$(sed -nE "s|^project *\(.*[^_]version: *'([^']+)'.*|\1|p" aravis/meson.build | head -n 1)

{
    # safe.directory: the MSYS2 git on the Windows runner sees the checkout as owned by another user.
    echo "Aravis $aravis_version, built from aravis commit $(git -c safe.directory='*' -C aravis rev-parse HEAD)"
    case "$(uname -s)" in
        MINGW*|MSYS*)
            echo "Bundled MSYS2 mingw-w64-x86_64 packages:"
            for dll in "$dir"/*.dll; do
                name=${dll##*/}
                [ "$name" = libaravis-0.8-0.dll ] && continue
                echo "  $name: $(pacman -Qo "/mingw64/bin/$name" | sed 's/.* is owned by //')"
            done ;;
        Darwin)
            echo "Bundled Homebrew formulae:"
            # Sources are Cellar paths: <prefix>/Cellar/<formula>/<version>/lib/<file>
            while read -r name path; do
                [ "$name" = libaravis-0.8.0.dylib ] && continue
                formula=$(awk -F/ '{ for (i = 1; i < NF; i++) if ($i == "Cellar") print $(i+1), $(i+2) }' <<<"$path")
                echo "  $name: ${formula:-$path}"
            done < "$sources" ;;
        Linux)
            libxml2=$(awk '/^Version:/ { print $2 }' _deps/lib/pkgconfig/libxml-2.0.pc)
            echo "Linked in statically: libxml2 $libxml2 (see build-libxml2-static.sh)"
            echo "Not bundled; built against these distribution packages (minimum versions):"
            dpkg-query -W -f '  ${Package} ${Version}\n' libglib2.0-0 libusb-1.0-0 zlib1g ;;
    esac
} > "$out"

cat "$out"
