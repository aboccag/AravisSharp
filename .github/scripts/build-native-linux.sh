#!/usr/bin/env bash
# Builds libaravis for Linux: libxml2 statically into _deps/, then Aravis against it into
# _install/. Expects MESON_OPTS in the environment.
#
# Runs in the ubuntu:20.04 build container. Aravis picks GLib APIs at compile time — it
# calls g_memdup2 only when built against GLib >= 2.68 — so the GLib it is built against is
# the oldest one it loads on. A build on Ubuntu 22.04 (GLib 2.72) died on Debian 11 (2.66)
# with "undefined symbol: g_memdup2". Ubuntu 20.04 has GLib 2.64 and glibc 2.31.
set -euo pipefail

.github/scripts/build-libxml2-static.sh "$PWD/_deps"

# --exclude-libs keeps libxml2's symbols out of libaravis's dynamic symbol table.
hide='-Wl,--exclude-libs,libxml2.a'
# shellcheck disable=SC2086  # MESON_OPTS is a list of options
PKG_CONFIG_PATH="$PWD/_deps/lib/pkgconfig" meson setup _build aravis ${MESON_OPTS:?} \
    -Dc_link_args="$hide" -Dcpp_link_args="$hide" --prefix="$PWD/_install"
ninja -C _build install
