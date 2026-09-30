# Third-party notices

The AravisSharp NuGet package ships native libraries under `runtimes/{rid}/native/`.
They are not part of AravisSharp itself and remain under their own licenses. The license
texts are in the package's `licenses/` folder.

All bundled libraries are dynamically linked shared libraries, loaded at run time. Each of
them can be replaced by a compatible build, as the LGPL requires: put your own build in place
of the bundled file in the application's `runtimes/{rid}/native/` folder, or remove the
bundled files and install Aravis system-wide — the resolver tries the application's copies
first and the system search path when there are none.

## Components

| Component | License | License text | Source |
|---|---|---|---|
| Aravis | LGPL-2.1-or-later | `LGPL-2.1.txt` | <https://github.com/AravisProject/aravis> |
| GLib (glib, gobject, gio, gmodule) | LGPL-2.1-or-later | `LGPL-2.1.txt` | <https://gitlab.gnome.org/GNOME/glib> |
| libusb | LGPL-2.1-or-later | `LGPL-2.1.txt` | <https://github.com/libusb/libusb> |
| GNU gettext runtime (libintl) | LGPL-2.1-or-later | `LGPL-2.1.txt` | <https://www.gnu.org/software/gettext/> |
| GNU libiconv | LGPL-2.1-or-later | `LGPL-2.1.txt` | <https://www.gnu.org/software/libiconv/> |
| libxml2 | MIT | `libxml2.txt` | <https://gitlab.gnome.org/GNOME/libxml2> |
| libffi | MIT | `libffi.txt` | <https://github.com/libffi/libffi> |
| PCRE2 | BSD-3-Clause WITH PCRE2-exception | `pcre2.md` | <https://github.com/PCRE2Project/pcre2> |
| mingw-w64 winpthreads | MIT | `winpthreads.txt` | <https://www.mingw-w64.org/> |
| zlib | Zlib | `zlib.txt` | <https://zlib.net/> |

## Exact versions and corresponding source

Aravis is built from the `aravis/` git submodule, at the commit recorded in this repository
for the package version. The other libraries come from the platform's package manager at
build time: MSYS2 `mingw-w64-x86_64-*` packages on Windows, Homebrew bottles on macOS. Every
`runtimes/{rid}/native/` folder contains `aravis-native-versions.txt`, which records the
Aravis commit and the exact package versions bundled for that platform. The corresponding
source for each is published by MSYS2 (<https://github.com/msys2/MINGW-packages>) and
Homebrew (<https://github.com/Homebrew/homebrew-core>) for that version.

## Files per platform

Linux ships Aravis only. GLib, libxml2 and libusb come from the distribution
(`libglib2.0-0`, `libxml2`, `libusb-1.0-0`): bundling a second GLib into a process that may
also load the system one — through GStreamer or GTK, for example — puts two GObject type
systems in the same process.

| Platform | Files |
|---|---|
| `win-x64` | `libaravis-0.8-0.dll`, `libglib-2.0-0.dll`, `libgobject-2.0-0.dll`, `libgio-2.0-0.dll`, `libgmodule-2.0-0.dll`, `libusb-1.0.dll`, `libintl-8.dll`, `libiconv-2.dll`, `libxml2-16.dll`, `libffi-8.dll`, `libpcre2-8-0.dll`, `libwinpthread-1.dll`, `zlib1.dll` |
| `osx-arm64` | `libaravis-0.8.0.dylib`, `libglib-2.0.0.dylib`, `libgobject-2.0.0.dylib`, `libgio-2.0.0.dylib`, `libgmodule-2.0.0.dylib`, `libusb-1.0.0.dylib`, `libintl.8.dylib`, `libxml2.16.dylib`, `libpcre2-8.0.dylib` |
| `linux-x64`, `linux-arm64` | `libaravis-0.8.so.0` |

The CI refuses to pack a native file that is not listed in this table, so a new transitive
dependency cannot ship without its license being reviewed first.
