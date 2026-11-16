# Spec: P/Invoke declarations checked against the C headers

Issue #11, level complet (zones `interop`, `tests`), type feature. Written on 2026-11-16
against `main` at `58e9012`, before any code. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `58e9012`

- **Declarations.** `AravisSharp/Native/AravisNative.cs` has 200 `static extern`
  declarations, `AravisSharp/Native/GLibNative.cs` 30. No test compares them with the C
  prototypes; the scan of 13.11.2026 was a one-off script that is not in the repository.
- **One-off scan (16.11.2026, a throwaway script, not the test).**
  - 188 of the 200 Aravis declarations have an `ARV_API` prototype in `aravis/src/*.h`. The
    12 others are `arv_*_get_type` functions declared through `G_DECLARE_*_TYPE` macros or
    `GType arv_*_get_type (void)` lines: the test must read those too.
  - Once typedefs (`ArvPixelFormat` is `guint32`), enums (32-bit), callbacks and
    `unsigned int` are resolved, two declarations differ from their prototype:
    - `arv_camera_gv_get_packet_size` returns `guint` in C, `int` in C#;
    - `arv_camera_gv_auto_packet_size` returns `guint` in C, `void` in C#.
  - The `arv_camera_uv_*bandwidth*` mismatches listed in the issue are already fixed on
    `main` (`uint` everywhere).
  - The 30 GLib declarations match the GLib headers (`/usr/include/glib-2.0`), including
    `bool` returns marshalled as 4-byte `gboolean`.
- **Public API.** `Camera.GvGetPacketSize()` returns `int` and `Camera.GvAutoPacketSize()`
  returns nothing, as in v0.8.36. `GvSetPacketSize(int)` matches `gint` in C.
- **Headers in CI.** The `test-dotnet` job checks out the submodule (`submodules:
  recursive`) and installs `libglib2.0-dev`, so both header sets are there. A git worktree
  has no submodule checkout until `git submodule update --init`.
- Test-side declarations (`GioTestNative.cs`, `GigEStreamTests.cs` and others) are out of
  scope: the issue names the two library files.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | A test reads every `static extern` of `AravisNative` and `GLibNative` by reflection, finds its C prototype (Aravis: `aravis/src/*.h`, `get_type` macros included; GLib: the GLib headers) and fails, naming the function, on a missing prototype, a different argument count, or a different type family for the return or any argument. | A declaration left unchecked without being listed, with a reason, in the test's explicit allow-list. |
| AC-2 | Type families: signed and unsigned integers of each width are distinct; `gboolean` is `bool` marshalled as 4 bytes; any pointer (`*`, `out`, `ref`, `string`, delegate, `IntPtr`) is one family; `GType`/`gsize` are pointer-sized; enums and typedefs are resolved from the headers. | `int` accepted for `guint`, or a 1-byte `bool` accepted for `gboolean`. |
| AC-3 | The two packet-size declarations match their prototypes; the public `Camera` API follows the decision of question 2. | — |
| AC-4 | Where headers cannot be found, the test fails in CI (`GITHUB_ACTIONS=true`) and skips with a message elsewhere. | A CI run that silently skips the check. |
| AC-5 | The test is shown red first: on the current two mismatches, and by a mutation (a changed argument type) that it must catch. | — |
| AC-6 | Changelog entry; whole suite passes on the Fake camera, net8.0 and net10.0, Debug and Release. | — |

## Risks

- A regex reader of C headers misses an unusual prototype. Mitigation: every declaration
  without a prototype fails unless allow-listed, so a parser gap shows as a red test, not a
  silent pass.
- The test depends on the GLib headers of the machine. GLib keeps these prototypes stable;
  the test only checks the 30 functions the binding imports.

## Open questions

1. **Where the test finds headers.** (a) `aravis/src` under the repository root, overridable
   by an `ARAVIS_SOURCE_DIR` variable, and the GLib headers through `pkg-config --cflags
   glib-2.0 gio-2.0` (fallback `/usr/include/glib-2.0`); fail in CI, skip elsewhere
   (AC-4). (b) A checked-in snapshot of the prototypes, compared with the declarations,
   plus a separate check that the snapshot matches the headers. Recommendation: (a), no
   generated file to keep in sync.
2. **The packet-size API.** (a) Fix only the P/Invoke (`uint` returns); `GvGetPacketSize`
   stays `int` with a checked conversion and `GvAutoPacketSize` stays `void`: no change for
   callers. (b) Also change the public API: `GvGetPacketSize` returns `uint`,
   `GvAutoPacketSize` returns the negotiated size. Breaks callers of v0.8.36.
   Recommendation: (a); a packet size fits an `int`, and the negotiated size is readable
   with `GvGetPacketSize`.

## Decisions

- **2026-11-16, gate 1, alex:** spec approved with both recommendations. (1) The test reads
  `aravis/src` (or `ARAVIS_SOURCE_DIR`) and the GLib headers through `pkg-config`; it fails
  in CI when headers are missing and skips elsewhere. (2) Only the two P/Invoke
  declarations change (`uint` returns); `GvGetPacketSize` stays `int` with a checked
  conversion and `GvAutoPacketSize` stays `void`. No question left open, so no council.
- **2026-11-16, review of #11:** four reviews of the branch tightened AC-2 and AC-1.
  - *Pointee check (AC-2).* "Any pointer is one family" let `out uint` pass for a
    `guint64 *`. When the managed parameter is by-ref, a pointer or an array and its element
    is a primitive, an enum, `bool`, `IntPtr`/`UIntPtr` or `CLong`/`CULong`, one `*` is
    stripped from the C type and the pointee's family must match the element's (an `out
    bool` is 4 bytes unless `MarshalAs` `U1`/`I1`). `IntPtr` by value, struct elements
    (`ref GValue`) and `out IntPtr` for `T **` stay plain pointers. Every by-ref scalar of
    the library matches; narrowing `out ulong` of `arv_stream_get_statistics` to `out uint`
    is caught.
  - *Exported only (AC-1).* Aravis builds with `-fvisibility=hidden`: an `arv_*` declaration
    must match a prototype preceded by `ARV_API` (or `ARV_API G_DECLARE_*`, or the generated
    `get_type` of a public enum), else it fails.
  - *Completeness (AC-1).* The check reads every DllImport and LibraryImport of the assembly,
    nested types included, instead of a list of classes; a test pins that set to
    `AravisNative` and `GLibNative`, at least 200 and 30 declarations with distinct names.
  - *Strings (AC-2).* A returned `string`/`StringBuilder` or an `out`/`ref` string is a
    mismatch unless an allow-list entry says who owns the memory; none exists.
  - Also: the reader leaves out what it cannot read (unbalanced parentheses, a parenthesised
    argument that is not a function pointer) and reports a name with two different
    prototypes; C# `char` matches nothing; CI is `GITHUB_ACTIONS=true` or `CI=true`;
    `pkg-config` is read without deadlock, with a timeout, its error kept.
  - **Not checked:** the GLib version floor (an import newer than GLib 2.64 passes if the
    build machine's headers have it), and the signatures of the delegates passed as
    callbacks.

## Plan

1. Red tests: the header reader and comparison test, red on the two packet-size
   declarations; a mutation shown red.
2. Fix the two declarations and the `Camera` wrappers per question 2.
3. XML docs of the test's rules, changelog, tests README.
4. Whole suite, Debug and Release, Fake camera. No hardware run needed: the change is the
   return type of two GigE calls, covered by the GigE simulator; one GigE packet-size
   read on hardware if a camera is free.
5. OCR review, C# review, silent-failure hunter and test analysis.
