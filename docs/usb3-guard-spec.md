# Spec: guard the USB3 Vision members of Camera

Issue #19, level standard, type change, zone camera. Written on 2026-11-19 against `main`
at `ed9003c`, before any change. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `ed9003c`

- `Camera` has four USB3 Vision members, all released in v0.8.36 and none guarded:
  `UvGetBandwidth`, `UvSetBandwidth`, `UvGetBandwidthBounds` and
  `UvIsBandwidthControlAvailable` (`AravisSharp/Camera.cs:1554-1626`). No other public
  member of `Camera` starts with `Uv`.
- Aravis starts each of the four functions with
  `g_return_*_if_fail (arv_camera_is_uv_device (camera))`
  (`aravis/src/arvcamera.c:3831, 3858, 3885, 3908`): on a GigE or Fake camera GLib logs a
  CRITICAL, no `GError` is set, and the function returns 0, FALSE, or leaves its output
  arguments untouched. The binding then returns 0, `false` or `(0, 0)`, and
  `UvSetBandwidth` returns as if it had worked. Not run before writing this spec
  (`NON VÉRIFIÉ`); the red tests of step 1 show it.
- #10 fixed the same defect on the eleven `Gv*` members: `Camera.CheckGigE()`
  (`Camera.cs:2278-2287`) throws `NotSupportedException` naming the member and pointing
  to `IsGigEVisionDevice()`, before any native call, after the disposal check of the
  lease. `GigEGuardTests` holds a table of the members and a reflection test that keeps
  the table complete (`AravisSharp.Tests/GigEGuardTests.cs:19-34, 76-85, 128`).
- `Camera.IsUSB3VisionDevice()` exists (`Camera.cs:1463`).
- The existing USB3 tests (`CameraWrapperTests.cs:663-727`) run only on a USB3 camera and
  first ask `UvIsBandwidthControlAvailable()`.
- The settings recorded by the suite since #21 include the USB3 bandwidth limit, so a
  hardware run leaves it as it was.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | On a camera that is not USB3 Vision, the guarded members throw `NotSupportedException` naming the member and pointing to `IsUSB3VisionDevice()`, before any native call. | A GLib CRITICAL; a silent 0, `false` or `(0, 0)`; a `UvSetBandwidth` that returns normally. |
| AC-2 | One shared check, `CheckUsb3()`, with the same message pattern as `CheckGigE()`; a disposed camera still throws `ObjectDisposedException` first. | Two different exception types or message patterns for the same situation. |
| AC-3 | A table of the `Uv*` members and a reflection test that fails when a public `Uv*` member of `Camera` is missing from it. Tests on the Fake camera, shown red against `main` first, with GLib criticals captured to show there is none. | A test that needs hardware to pass in CI. |
| AC-4 | The four members still work on the USB3 camera: the existing USB3 tests pass in a hardware run. | — |
| AC-5 | Changelog entry next to the one of #10, under Fixed (the silent failure is the defect; the entry states the behaviour change of released members); the XML docs of the three throwing members name the exception, and those of `UvIsBandwidthControlAvailable` its result on another camera. | — |
| AC-6 | The whole suite passes on the Fake camera, net8.0 and net10.0. No change to `aravis/` or to the P/Invoke declarations. | — |

## Open question (gate 1)

1. **`UvIsBandwidthControlAvailable()` on a camera that is not USB3 Vision.**
   - (a) It returns `false`, with no native call: bandwidth control is indeed not available
     on that camera, and a caller can ask without checking the camera type first. The three
     other members throw.
   - (b) It throws like the others, as the issue proposes: one rule for the four members,
     the same as for the `Gv*` ones, none of which is a yes-or-no question.

   Recommendation: (a). A member named "is ... available" that throws on the cameras where
   the answer is no forces every caller to test `IsUSB3VisionDevice()` before asking.

## Gate 1 decision

2026-11-19, alex: (a). `UvIsBandwidthControlAvailable()` returns `false` on a camera that is
not USB3 Vision, with no native call; the three other members throw.

## Plan

1. Red tests: `Usb3GuardTests` on the Fake camera (exception, message, no CRITICAL, the
   reflection table), failing against `main`.
2. `CheckUsb3()` and the four members; XML docs; changelog; `docs/COMPREHENSIVE_API.md` if
   it lists these members.
3. Whole suite on the Fake camera, net8.0 and net10.0.
4. Reviews: OCR and `ecc:csharp-reviewer`.
5. Hardware: whole suite on the USB3 camera (the four members), and on one GigE camera
   (the guard on a real non-USB3 camera).
6. Merge `--no-ff` with `Closes #19`.

## Red evidence

2026-11-19, Fake camera, tests of `c915108` against the unchanged library: the three
bandwidth members returned without throwing, `UvIsBandwidthControlAvailable` returned
`false` after a critical, and GLib printed four criticals, one per member
(`arv_camera_uv_get_bandwidth: assertion 'arv_camera_is_uv_device (camera)' failed`, and
the same for the three others). libaravis logs them in GLib's default domain, not in an
"Aravis" domain: `GObjectLogCapture` now accepts a null domain.

## Reviews

OCR and `ecc:csharp-reviewer` on `ee059ba`: no Critical or High finding. Applied: a test
that calls the native function directly and checks that the capture sees its critical (so
the "Aravis logged nothing" assertions can fail), the non-USB3 precondition in the
`UvIsBandwidthControlAvailable` test, the member count pinned and overloads counted, the
`<returns>` and `<remarks>` of `UvIsBandwidthControlAvailable`, and AC-5 and one line
reference of this spec.

## Verification

2026-11-19, Debug.
- Whole suite at `ee059ba`:

  | Camera | Framework | Passed | Failed | Skipped |
  |---|---|---|---|---|
  | Fake | net8.0 and net10.0 | 493 | 0 | 19 |
  | Basler acA2440-75uc 22119564 (USB3) | net8.0 | 489 | 0 | 22 |
  | Basler acA1920-40gm 24466563 (GigE) | net10.0 | 486 | 0 | 25 |

  On the USB3 camera the four existing `Uv*` tests of `CameraWrapperTests` pass (the camera
  has `DeviceLinkThroughputLimit`); on the GigE camera the guard tests pass.
- After the review fixes: the guard tests and their neighbours (`Usb3GuardTests`,
  `GigEGuardTests`, `LeaseCoverageTests`, `GObjectPropertyTests`, `ConcurrencyTests`) on the
  Fake camera, 131 passed on each framework; `Usb3GuardTests` on the GigE camera, 5 passed
  on each framework. The whole suite was not run again after these fixes (tests and XML
  docs only).
- Not checked: a USB3 camera without bandwidth control; the Opto Engineering cameras.
