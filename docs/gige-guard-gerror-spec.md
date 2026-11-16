# Spec: GigE guard, GError domain and code, narrowed catch blocks

Issue #10, level complet (zones `interop`, `stream`, `genicam`, `camera`), type changement.
Written on 2026-11-16 against `main` at `3a8347d`, before any code. Source material: commit
`46f34be` and `docs/safe-handles-spec.md` on `claude/safe-handles-90b248` (not merged, older
base). `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `3a8347d`

- **Stream.** The eleven GigE members of `Stream` already call a private `CheckGigE()`
  (`AravisSharp/Stream.cs:70-360`) that throws `InvalidOperationException`; the test
  `Stream_GigESetters_OnNonGigEStream_ShouldThrowInvalidOperationException`
  (`AravisSharp.Tests/CameraLifecycleTests.cs:261`) pins it. No public property tells a
  caller whether a stream is GigE. v0.8.36 had no guard at all: the guard and its exception
  type were never released.
- **Camera.** Its eleven public `Gv*` members (`AravisSharp/Camera.cs:1444-2080`) have no
  guard. On a non-GigE camera Aravis fails its `g_return_val_if_fail
  (arv_camera_is_gv_device (camera), …)` (`aravis/src/arvcamera.c:3331` and following):
  GLib logs a CRITICAL, no `GError` is set, and the member returns 0 or does nothing.
  `Camera.IsGigEVisionDevice()` exists (`Camera.cs:1420`).
- **AravisException.** It carries a message only (`AravisSharp/AravisException.cs`). Three
  places read a `GError` and drop its `domain` (a GQuark) and `code`: `Device.cs:285`,
  `Camera.cs:2163` (`GetErrorMessage`), `GenICam/GenICamNode.cs:109`.
- **Catch blocks.** Seven bare `catch` blocks swallow every exception:
  `GenICam/NodeMap.cs:50, 137, 188` and `GenICam/FeatureDetails.cs:107, 191, 215, 264`.
  `Buffer.cs:333` is in a finalizer path and out of scope (`NON VÉRIFIÉ` beyond reading it).
- **Resolver.** Since `c8fed52` the bundled `libaravis-0.8` is probed before a system one;
  a system library is still loaded when nothing is bundled, so an older one can lack entry
  points. The ruling of 14.11.2026 stands: `EntryPointNotFoundException` stays absorbed for
  optional feature metadata.
- **Hardware.** GigE (Basler acA1920-40gm 24466563, Opto Engineering ITA24 and ITA32) and
  USB3 (Basler acA2440-75uc) cameras are available on the bench, one run at a time.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | Every GigE member of `Stream` and of `Camera` throws the exception decided at gate 1 (question 1) on a non-GigE stream or camera, before any native call; one shared check per class, with the same exception type and message pattern. | A GLib CRITICAL; a silent 0; two different exception types for the same situation. |
| AC-2 | A public `Stream.IsGigEVision` tells whether the GigE members are usable, true on a GigE stream, false on the Fake and USB3 streams. | — |
| AC-3 | `AravisException` exposes the `GError` domain (as its string name, from `g_quark_to_string`) and code; every place that turns a `GError` into an `AravisException` sets them, through one shared helper that also frees the `GError`. Exceptions created without a `GError` leave them empty. | A `GError` leaked or freed twice; a domain shown as a raw number. |
| AC-4 | The seven catch blocks catch only `AravisException`, plus `EntryPointNotFoundException` where the code reads optional feature metadata; every other exception propagates. | `ObjectDisposedException`, `OutOfMemoryException` or a P/Invoke marshalling error swallowed. |
| AC-5 | Tests on the Fake camera cover AC-1 to AC-4 (a real `GError` from the Fake camera for AC-3); each is shown red against `main` first. | A test that needs hardware to pass in CI. |
| AC-6 | The changelog lists the behaviour changes (exception type, new throws on `Camera`, narrowed catches). | — |
| AC-7 | The whole suite passes on the Fake camera, net8.0 and net10.0, Debug and Release; then once with `ARAVIS_TEST_DEVICE_ID` on a GigE camera and once on the USB3 camera, one at a time. | — |

## Risks

- Narrowing catch blocks can surface exceptions that callers of `NodeMap` never saw
  (for example a disposed device): that is the point, and it is listed in the changelog.
- `Camera` members that returned 0 now throw on non-GigE cameras: a behaviour change for
  released code (v0.8.36 had these members).

## Open questions

1. **Exception type.** (a) `NotSupportedException`, with `Stream.IsGigEVision` as its test,
   the pattern of `System.IO.Stream` (`CanSeek` and `NotSupportedException`), and the
   ruling of 14.11.2026 (old addendum A.4). (b) Keep `main`'s `InvalidOperationException`.
   Neither was released. Recommendation: (a).
2. **Camera's GigE members.** (a) Guard the eleven `Gv*` members of `Camera` with the same
   exception, through `IsGigEVisionDevice()`. (b) Leave `Camera` as it is and guard only
   `Stream`. Recommendation: (a): today they log a CRITICAL and return 0, the same silent
   failure as the stream members.
3. **Socket buffer cap** (found by the review of #14). On Linux the kernel caps the socket
   buffer at `net.core.rmem_max` (about 208 KB by default), so the 4 MB of
   `ConfigureGigEDefaults` are silently reduced; Aravis only logs it in debug. (a) Document
   the cap in the XML docs of `SetSocketBufferSize` and `ConfigureGigEDefaults` here, and
   open a separate issue to expose the size actually granted. (b) Both here. (c) Neither.
   Recommendation: (a).

## Plan

1. Red tests on the Fake camera for AC-1 to AC-4; then the shared guard, `IsGigEVision`,
   the `GError` helper and the narrowed catches.
2. Port what still applies from `46f34be`; drop what `main` already has.
3. XML docs, changelog, docs of question 3.
4. Whole suite Debug and Release; one hardware run on a GigE camera and one on the USB3
   camera, one at a time.
5. OCR review, C# review, silent-failure hunter and test analysis.
