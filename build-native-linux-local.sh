#!/usr/bin/env bash
# Builds the Linux native runtime of the AravisSharp package the way CI does, into
# AravisSharp/runtimes/linux-<arch>/native/ (linux-x64 or linux-arm64, from this machine's
# architecture), where dotnet pack bundles it. To run the tests against it instead of a system
# libaravis: LD_LIBRARY_PATH=$PWD/AravisSharp/runtimes/linux-x64/native dotnet test AravisSharp.Tests/
#
# A thin wrapper around the scripts of the build-native-linux job in
# .github/workflows/build-and-publish.yml: same steps, same Meson options, so the result is the
# libaravis-0.8.so.0 CI ships (libxml2 linked in statically with its symbols hidden, stripped,
# runpath $ORIGIN; GLib, libusb and zlib come from the distribution).
#
# CI builds in an ubuntu:20.04 container on purpose. Aravis picks GLib APIs at compile time, so
# the GLib a build is made against is the oldest one it loads on: built on Ubuntu 22.04 (GLib
# 2.72), libaravis dies on Debian 11 (GLib 2.66) with "undefined symbol: g_memdup2", and the
# glibc it links against sets the same kind of floor. A build on a newer distribution therefore
# only runs on that distribution and newer ones. With docker available, this script builds in
# ubuntu:20.04 like CI; a --host build is for trying things on this machine, not for shipping.
#
# Usage: ./build-native-linux-local.sh [--docker | --host]
#   --docker  build in an ubuntu:20.04 container (the default when docker is available)
#   --host    build on this machine: Debian or Ubuntu with the CI job's build packages (the
#             script lists any that are missing) and meson >= 0.57 (the default without docker)
#
# The build trees are _deps/, _build/, _install/ and _native/ at the repository root, the paths
# the CI scripts use; they are deleted and recreated on every run.
set -euo pipefail

repo=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)
cd "$repo"

# The Meson options, build packages, meson version and image are read from the workflow, so this
# script cannot drift from the build-native-linux job; it stops if the workflow's layout changes.
workflow=.github/workflows/build-and-publish.yml
[ -f "$workflow" ] || { echo "error: $workflow not found" >&2; exit 1; }
# awk reads the file itself: a producer piped into an awk that exits early dies of SIGPIPE, which
# pipefail turns into a failure.
MESON_OPTS=$(awk '
    { sub(/\r$/, "") }
    /^  MESON_OPTS: >-$/ { on = 1; next }
    on && /^    -/ { sub(/^ +/, ""); printf "%s%s", sep, $0; sep = " "; next }
    on { exit }' "$workflow")
job=$(awk '{ sub(/\r$/, "") } /^  build-native-linux:$/ { on = 1; next } on && /^  [a-z]/ { exit } on' "$workflow")
APT_PACKAGES=$(sed -nE 's/.*apt-get install -y --no-install-recommends +//p' <<<"$job" | tr -s ' ' | sed 's/ *$//')
MESON_VERSION=$(sed -nE 's/.*pip3 install meson==([0-9.]+).*/\1/p' <<<"$job")
IMAGE=$(sed -nE 's/^    container: *([^ ]+).*/\1/p' <<<"$job")
for setting in MESON_OPTS APT_PACKAGES MESON_VERSION IMAGE; do
    if [ -z "${!setting}" ]; then
        echo "error: could not read $setting from the build-native-linux job in $workflow;" >&2
        echo "       update the parsing in $0 to the workflow's new layout" >&2
        exit 1
    fi
done
GLIB_FLOOR=2.64   # GLib of ubuntu:20.04, the image above: update both together

mode=${1:-auto}
case "$mode" in
    # docker info, not just the CLI: the daemon must be reachable by this user.
    auto) if docker info >/dev/null 2>&1; then mode=--docker; else mode=--host; fi ;;
    --docker | --host) ;;
    -h | --help) sed -n '2,/^set -euo pipefail/{/^set /d; s/^# \{0,1\}//; p}' "$0"; exit 0 ;;
    *) echo "usage: $0 [--docker | --host]" >&2; exit 2 ;;
esac

case "$(uname -m)" in
    x86_64) rid=linux-x64 ;;
    aarch64 | arm64) rid=linux-arm64 ;;
    *) echo "error: unsupported architecture $(uname -m): the package ships linux-x64 and linux-arm64" >&2; exit 1 ;;
esac

if [ ! -f aravis/meson.build ]; then
    echo "error: the aravis/ submodule is empty; run: git submodule update --init" >&2
    exit 1
fi

if [ "$mode" = --docker ]; then
    # The repository is mounted at the same path, and git's common directory too when it lives
    # outside it (a worktree), so git in the container resolves the submodule's commit.
    # Not rev-parse --path-format=absolute: git before 2.31 (Ubuntu 20.04 has 2.25) echoes the
    # unknown option back instead of failing.
    mounts=(-v "$repo:$repo")
    common=$(git rev-parse --git-common-dir 2>/dev/null || true)
    if [ -n "$common" ] && [ -d "$common" ]; then
        common=$(cd "$common" && pwd -P)
        case "$common" in
            "$repo" | "$repo"/*) ;;
            *) mounts+=(-v "$common:$common:ro") ;;
        esac
    fi
    echo "== Building $rid in $IMAGE, as CI does =="
    # Root in the container installs the build packages, then gives the outputs the owner the
    # repository has as seen from the container: the caller's uid with rootful docker, root with
    # rootless docker or podman, where root is the caller remapped and chowning to the caller's
    # uid would hand the files to a subordinate uid instead.
    # shellcheck disable=SC2016  # expanded by the container's shell
    exec docker run --rm "${mounts[@]}" -w "$repo" \
        -e DEBIAN_FRONTEND=noninteractive \
        -e APT_PACKAGES="$APT_PACKAGES" -e MESON_VERSION="$MESON_VERSION" \
        -e BUILD_NATIVE_LINUX_IN_IMAGE=1 \
        "$IMAGE" bash -c '
            set -euo pipefail
            owner=$(stat -c %u:%g .)
            trap "chown -R $owner _deps _build _install _native AravisSharp/runtimes 2>/dev/null || true" EXIT
            apt-get update -qq
            apt-get install -y -qq --no-install-recommends $APT_PACKAGES >/dev/null
            pip3 install -q "meson==$MESON_VERSION"
            # The checkout belongs to the caller, not to root.
            git config --global --add safe.directory "*"
            ./build-native-linux-local.sh --host'
fi

# --host: check the build requirements up front rather than failing halfway through.
missing=()
for tool in meson ninja pkg-config gcc g++ make curl xz sha256sum patchelf strip readelf nm objdump ldd git dpkg-query; do
    command -v "$tool" >/dev/null 2>&1 || missing+=("$tool")
done
for module in glib-2.0 gobject-2.0 gio-2.0 libusb-1.0 zlib; do
    pkg-config --exists "$module" 2>/dev/null || missing+=("$module (development files)")
done
if [ ${#missing[@]} -gt 0 ]; then
    echo "error: missing: ${missing[*]}" >&2
    echo "On Debian / Ubuntu:" >&2
    echo "  sudo apt-get install -y --no-install-recommends $APT_PACKAGES" >&2
    echo "  pip3 install meson==$MESON_VERSION   # when the distribution's meson is older than 0.57" >&2
    exit 1
fi
meson_version=$(meson --version)
if [ "$(printf '%s\n' 0.57.0 "$meson_version" | sort -V | sed -n 1p)" != 0.57.0 ]; then
    echo "error: meson $meson_version is older than 0.57, which Aravis requires: pip3 install meson==$MESON_VERSION" >&2
    exit 1
fi

glib=$(pkg-config --modversion glib-2.0)
if [ "$(printf '%s\n' "$GLIB_FLOOR" "$glib" | sort -V | tail -n 1)" != "$GLIB_FLOOR" ]; then
    echo "warning: building against GLib $glib: the result does not load where GLib is older than $glib." >&2
    echo "         The package's floor is GLib $GLIB_FLOOR ($IMAGE); build with --docker for a shippable binary." >&2
fi

export MESON_OPTS
rm -rf _deps _build _install _native

.github/scripts/check-aravis-version.sh
.github/scripts/build-native-linux.sh
.github/scripts/collect-native-linux.sh _native
.github/scripts/verify-native-linux.sh _native
# Queries the GLib, libusb and zlib packages by their Ubuntu 20.04 names, which newer releases
# renamed (libglib2.0-0t64): a host build keeps its library without the record, a build in the
# CI image does not.
if ! .github/scripts/record-native-versions.sh _native; then
    if [ "${BUILD_NATIVE_LINUX_IN_IMAGE:-}" = 1 ]; then
        echo "error: could not record the build's package versions" >&2
        exit 1
    fi
    echo "warning: could not record the build's package versions; aravis-native-versions.txt is incomplete" >&2
fi

dest=AravisSharp/runtimes/$rid/native
rm -rf "$dest"
mkdir -p "$dest"
cp -a _native/. "$dest/"

echo
echo "== $rid runtime ready in $dest =="
ls -l "$dest"
echo "Test against it: LD_LIBRARY_PATH=$repo/$dest dotnet test AravisSharp.Tests/"
echo "dotnet pack bundles it, with only the runtimes present under AravisSharp/runtimes/:"
echo "release packages come from CI."
