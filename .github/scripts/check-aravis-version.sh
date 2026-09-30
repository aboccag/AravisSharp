#!/usr/bin/env bash
# Fails when the aravis/ submodule is not the Aravis version the package declares.
set -euo pipefail

expected="$(sed -nE 's:.*<AravisNativeVersion>([^<]+)</AravisNativeVersion>.*:\1:p' AravisSharp/AravisSharp.csproj | head -n 1)"
actual="$(sed -nE "s|^project *\(.*[^_]version: *'([^']+)'.*|\1|p" aravis/meson.build | head -n 1)"
if [[ -z "$expected" || -z "$actual" ]]; then
    echo "::error::Could not determine expected or actual Aravis version"
    exit 1
fi
if [[ "$actual" != "$expected" ]]; then
    echo "::error::Aravis submodule version $actual does not match AravisNativeVersion $expected"
    exit 1
fi
echo "aravis/ submodule is Aravis $actual"
