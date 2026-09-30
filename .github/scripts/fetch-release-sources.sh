#!/usr/bin/env bash
# Gathers the corresponding source of every native library a release bundles, to attach to
# its GitHub Release: MSYS2 and Homebrew drop old versions, so their repositories cannot be
# relied on to still have the source of what an older package shipped.
#
# usage: fetch-release-sources.sh <native artifacts dir> <out dir>
#
# Reads <native dir>/native-<rid>/aravis-native-versions.txt (see record-native-versions.sh)
# and writes to <out dir>:
#   aravis-<version>-<commit>.tar.gz   git archive of the aravis submodule commit
#   native-sources-<rid>.tar           the sources of what native-<rid> bundles, one folder
#                                      per package, with that rid's aravis-native-versions.txt
#   SHA256SUMS
# The same inputs give the same bytes, so a re-run of the tag can be told apart from a
# rebuild that bundled other versions (see attach-release-sources.sh).
# Fails closed: a bundled file without a recorded package, a package without a recorded
# source, a download error or a checksum mismatch all stop the release.
set -euo pipefail

native=${1:?usage: fetch-release-sources.sh <native artifacts dir> <out dir>}
mkdir -p "${2:?usage: fetch-release-sources.sh <native artifacts dir> <out dir>}"
out=$(cd "$2" && pwd)
rids=(linux-x64 linux-arm64 osx-arm64 win-x64)

fail() { echo "::error::$*" >&2; exit 1; }

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

fetch() {  # <url> <sha256 or empty> <destination>
    local url=$1 sha=$2 dest=$3 protocols
    [[ "$url" == https://?* ]] || fail "not an https URL: $url"
    # Without a checksum, https end to end is all that vouches for the file; with one, a
    # mirror redirector (ftpmirror.gnu.org) may hand over to a plain http mirror. --proto
    # covers redirects too, so both options carry the same list.
    if [ -n "$sha" ]; then protocols='=http,https'; else protocols='=https'; fi
    echo "fetching $url"
    curl -fsSL --proto "$protocols" --proto-redir "$protocols" --retry 5 --retry-all-errors \
        --connect-timeout 30 --max-time 900 -o "$dest" "$url" \
        || fail "cannot fetch $url"
    [ -s "$dest" ] || fail "$url is empty"
    if [ -n "$sha" ]; then
        echo "$sha  $dest" | sha256sum -c --quiet - || fail "$url does not match sha256 $sha"
    fi
}

aravis_version=''
aravis_commit=''
declare -A fetched=()   # url -> file already downloaded, for packages sharing a source

for rid in "${rids[@]}"; do
    dir="$native/native-$rid"
    versions="$dir/aravis-native-versions.txt"
    [ -f "$versions" ] || fail "no $versions"

    header=$(head -n 1 "$versions")
    [[ "$header" =~ ^Aravis\ ([0-9.]+),\ built\ from\ aravis\ commit\ ([0-9a-f]{40})$ ]] \
        || fail "$versions: unexpected first line: $header"
    if [ -z "$aravis_commit" ]; then
        aravis_version=${BASH_REMATCH[1]} aravis_commit=${BASH_REMATCH[2]}
    elif [ "${BASH_REMATCH[2]}" != "$aravis_commit" ]; then
        fail "$rid was built from aravis ${BASH_REMATCH[2]}, another platform from $aravis_commit"
    fi

    # Parse the three kinds of entries (format documented in record-native-versions.sh).
    declare -A package_of=() has_source=()
    required=() source_lines=()
    section=
    while IFS= read -r line; do
        case "$line" in
            "  "*)
                entry=${line#  }
                case "$section" in
                    bundled)
                        package_of[${entry%%: *}]=${entry#*: }
                        required+=("${entry#*: }") ;;
                    source)
                        has_source[${entry%%: *}]=1
                        source_lines+=("$entry") ;;
                esac ;;
            "Bundled "*) section=bundled ;;
            "Corresponding source"*) section=source ;;
            "Linked in statically: "*)
                entry=${line#Linked in statically: }
                required+=("${entry%% (*}")
                section= ;;
            *) section= ;;
        esac
    done < "$versions"

    # Every shipped file is libaravis, the versions record, or a library with a known package.
    for f in "$dir"/*; do
        name=${f##*/}
        case "$name" in aravis-native-versions.txt|libaravis-0.8[.-]*) continue ;; esac
        [ -n "${package_of[$name]:-}" ] || fail "$rid bundles $name but $versions names no package for it"
    done
    for package in "${required[@]}"; do
        [ -n "${has_source[$package]:-}" ] || fail "$rid: no corresponding source recorded for $package"
    done

    for entry in "${source_lines[@]}"; do
        package=${entry%%: *}
        rest=${entry#*: }
        url=${rest%% *}
        sha=
        if [[ "$rest" == *" sha256="* ]]; then
            sha=${rest##* sha256=}
            [[ "$sha" =~ ^[0-9a-f]{64}$ ]] || fail "$rid: bad sha256 for $url: $sha"
        fi
        folder=${package// /-}
        [[ "$folder" =~ ^[A-Za-z0-9._+~-]+$ ]] || fail "$rid: unexpected package name $package"
        file=${url%%[?#]*}
        file=${file##*/}
        [ -n "$file" ] || fail "$rid: no file name in $url"
        dest="$work/$rid/$folder/$file"
        mkdir -p "${dest%/*}"
        [ -e "$dest" ] && fail "$rid: two sources of $package are both named $file"
        if [ -n "${fetched[$url]:-}" ]; then
            cp "${fetched[$url]}" "$dest"
        else
            fetch "$url" "$sha" "$dest"
            fetched[$url]=$dest
        fi
    done

    cp "$versions" "$work/$rid/"
done

# The aravis/ checkout must be the commit the native libraries were built from. An empty
# aravis/ resolves to the superproject, which cannot match.
checkout=$(git -C aravis rev-parse HEAD)
[ "$checkout" = "$aravis_commit" ] \
    || fail "aravis/ is at $checkout but the native libraries were built from $aravis_commit"

# Reproducible archives: fixed entry order, owner, modes and time (the Aravis commit's), so
# a re-run of the tag produces the same bytes (attach-release-sources.sh relies on it).
mtime=$(git -C aravis log -1 --format=%ct "$aravis_commit")
for rid in "${rids[@]}"; do
    tar -C "$work" --format=gnu --sort=name --owner=0 --group=0 --numeric-owner \
        --mode='u=rwX,go=rX' --mtime="@$mtime" -cf "$out/native-sources-$rid.tar" "$rid"
done
# git archive's tar is reproducible; its built-in gzip has changed between git versions.
git -C aravis archive --format=tar --prefix="aravis-$aravis_version/" "$aravis_commit" \
    | gzip -9n > "$out/aravis-$aravis_version-${aravis_commit:0:12}.tar.gz"

(cd "$out" && LC_ALL=C sha256sum -- * > SHA256SUMS && cat SHA256SUMS)
