#!/usr/bin/env bash
# Runs the self-contained smoke app (mounted at /app) inside a bare distribution image,
# with only what a user must install: GLib and libusb. No libxml2 — the package links it
# in — and no Aravis, so nothing on the system can stand in for the packaged one.
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends libglib2.0-0 libusb-1.0-0 >/dev/null

# grep without -q: under pipefail, -q exits on the first match, ldconfig takes SIGPIPE and
# the pipeline reports failure exactly when the library is present.
for lib in libxml2 libaravis; do
    if ldconfig -p | grep "$lib" >/dev/null; then
        echo "::error::$lib is installed in the image and would mask what the package ships" >&2
        exit 1
    fi
done

. /etc/os-release
echo "Running on $PRETTY_NAME, GLib $(dpkg-query -W -f '${Version}' 'libglib2.0-0*' | head -c 40)"
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 /app/PackageSmoke
