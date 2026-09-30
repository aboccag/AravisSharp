#!/usr/bin/env bash
# Writes aravis-native-versions.txt next to the bundled libraries: the Aravis commit, the
# exact version of every third-party library, and where the corresponding source of each
# version is, resolved here at build time while the package manager still knows it.
#
# fetch-release-sources.sh parses this file on a release tag, so keep its shape:
#   - "Bundled ...:"               then "  <file>: <package> <version>" lines;
#   - "Linked in statically: <package> <version> (...)";
#   - "Corresponding source ...:"  then "  <package> <version>: <https url> [sha256=<hex>]"
#     lines, one or more per package.
set -euo pipefail

dir=${1:-_native}
sources=${2:-_native-sources.txt}
out="$dir/aravis-native-versions.txt"
packages=$(mktemp)
trap 'rm -f "$packages"' EXIT

fail() { echo "::error::$*" >&2; exit 1; }

aravis_version=$(sed -nE "s|^project *\(.*[^_]version: *'([^']+)'.*|\1|p" aravis/meson.build | head -n 1)
# safe.directory: the MSYS2 git on the Windows runner sees the checkout as owned by another user.
aravis_commit=$(git -c safe.directory='*' -C aravis rev-parse HEAD)

# The MSYS2 source package carries the PKGBUILD, its patches and the upstream sources. It is
# named after the pkgbase (mingw-w64-gettext for mingw-w64-x86_64-gettext-runtime), which
# only the local package database records.
msys2_sources() {
    local package=$1 version=$2 base
    base=$(awk '/^%BASE%$/ { getline; print; exit }' "/var/lib/pacman/local/$package-$version/desc")
    [ -n "$base" ] || fail "no pkgbase recorded for $package $version"
    echo "  $package $version: https://repo.msys2.org/mingw/sources/$base-$version.src.tar.zst"
}

# Upstream archive, the formula file (build steps, inreplace edits, inline patches) at the
# homebrew-core commit the installed bottle came from, and every patch the formula applies.
homebrew_sources() {
    local formula=$1 version=$2
    brew info --json=v2 "$formula" | jq -r --arg f "$formula" --arg v "$version" '
        def sha: if . then " sha256=\(.)" else "" end;
        .formulae[0] as $x
        | ($x.versions.stable + (if $x.revision > 0 then "_\($x.revision)" else "" end)) as $tap_version
        | if $tap_version != $v then error("\($f): \($v) is installed but the formula is at \($tap_version)") else . end
        | if $x.tap != "homebrew/core" then error("\($f) comes from tap \($x.tap), not homebrew/core") else . end
        | if $x.urls.stable.using != null or $x.urls.stable.checksum == null
          then error("\($f): stable source is not a checksummed archive (\($x.urls.stable))") else . end
        | if $x.tap_git_head == null or $x.ruby_source_path == null
          then error("\($f): no homebrew-core commit recorded for the formula") else . end
        | "https://raw.githubusercontent.com/Homebrew/homebrew-core/\($x.tap_git_head)" as $core
        | "  \($f) \($v): \($x.urls.stable.url)\($x.urls.stable.checksum | sha)",
          "  \($f) \($v): \($core)/\($x.ruby_source_path)\($x.ruby_source_checksum.sha256 | sha)",
          ($x.patches // [] | .[]
            | if .url then "  \($f) \($v): \(.url)\(.sha256 | sha)"
              elif .file then "  \($f) \($v): \($core)/\(.file)"
              else empty end)   # inline (DATA) patches are in the formula file
    '
}

{
    echo "Aravis $aravis_version, built from aravis commit $aravis_commit"
    case "$(uname -s)" in
        MINGW*|MSYS*)
            echo "Bundled MSYS2 mingw-w64-x86_64 packages:"
            for dll in "$dir"/*.dll; do
                name=${dll##*/}
                [ "$name" = libaravis-0.8-0.dll ] && continue
                owner=$(pacman -Qo "/mingw64/bin/$name" | sed 's/.* is owned by //')
                echo "  $name: $owner"
                echo "$owner" >> "$packages"
            done
            echo "Corresponding source (MSYS2 source packages):"
            while read -r package version; do
                msys2_sources "$package" "$version"
            done < <(sort -u "$packages") ;;
        Darwin)
            echo "Bundled Homebrew formulae:"
            # Sources are Cellar paths: <prefix>/Cellar/<formula>/<version>/lib/<file>
            while read -r name path; do
                [ "$name" = libaravis-0.8.0.dylib ] && continue
                formula=$(awk -F/ '{ for (i = 1; i < NF; i++) if ($i == "Cellar") print $(i+1), $(i+2) }' <<<"$path")
                [ -n "$formula" ] || fail "$name comes from $path, outside the Homebrew Cellar: its source cannot be resolved"
                echo "  $name: $formula"
                echo "$formula" >> "$packages"
            done < "$sources"
            echo "Corresponding source (upstream archive, Homebrew formula and patches):"
            while read -r formula version; do
                homebrew_sources "$formula" "$version"
            done < <(sort -u "$packages") ;;
        Linux)
            libxml2=$(awk '/^Version:/ { print $2 }' _deps/lib/pkgconfig/libxml-2.0.pc)
            echo "Linked in statically: libxml2 $libxml2 (see build-libxml2-static.sh)"
            echo "Not bundled; built against these distribution packages (minimum versions):"
            dpkg-query -W -f '  ${Package} ${Version}\n' libglib2.0-0 libusb-1.0-0 zlib1g
            echo "Corresponding source of the statically linked libraries:"
            sed 's/^/  /' _deps/libxml2-source.txt ;;
        *)
            fail "unsupported build platform $(uname -s)" ;;
    esac
} > "$out"

cat "$out"
