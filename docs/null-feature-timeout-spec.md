# Spec: null feature name and PopBuffer timeout conversion

Issue #12, level standard, type fix. Written on 2026-11-14 against `main` at `366455b`,
before any code. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `366455b`

- `Camera.GetAvailableEnumerationValues(string feature)` has no null check
  (`AravisSharp/Camera.cs:1794-1812`). `Marshal.StringToCoTaskMemUTF8(null)` gives a null
  pointer; Aravis then fails its argument check and returns `NULL` without a `GError`
  (`aravis/src/arvdevice.c:438`), and `MarshalStringArray` turns that into an empty array.
- `Camera.cs` has no `ArgumentNullException.ThrowIfNull` at all, and fifteen other public
  members take a feature name the same way (`ExecuteCommand`, the `Get*`/`Set*` feature
  accessors, the bounds and increment getters, `IsFeatureAvailable`,
  `IsFeatureImplemented`). What each of them does with `null`: `NON VÉRIFIÉ`.
- `Stream.PopBuffer(ulong)` converts inline with
  `Math.Min(timeoutMs, (ulong)long.MaxValue / 2000) * 1000` (`AravisSharp/Stream.cs:290`).
  Its XML documentation does not mention the cap (`:277-281`). No test covers the
  conversion.
- Neither `PopBuffer` overload says anything about disposing the stream while a pop is
  blocked (`Stream.cs:267-281`).
- Nothing in `git log` or in the closed issues already covers these three points.

## Inputs and outputs

Inputs: a feature name, possibly `null`; a timeout in milliseconds from `0` to
`ulong.MaxValue`. Outputs: an exception or an array of names; a timeout in microseconds
passed to `arv_stream_timeout_pop_buffer`.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | `GetAvailableEnumerationValues(null)` throws `ArgumentNullException` with `ParamName` `feature`, before any native call. | An empty array; a GLib critical on stderr. |
| AC-2 | The conversion is one `internal static` pure function of `Stream`, used by `PopBuffer(ulong)`. For every input it returns exactly what `366455b` computes today: `0` gives `0`, a value below the cap gives the value times 1000, a value above it gives the cap. | Any change of the timeout actually passed to Aravis; a result above `long.MaxValue / 2` microseconds; a result below 100 years for the largest inputs. |
| AC-3 | A test pins AC-2 without any native call, including monotonicity over a sample of values. | A wrong cap that can only be seen by calling the native pop (GLib aborts the process for part of the range). |
| AC-4 | The XML documentation of `PopBuffer(ulong)` states the cap; both overloads state that the stream must not be disposed while another thread is blocked in a pop, and what happens otherwise. | A statement that is not backed by the Aravis or GLib behaviour observed. |
| AC-5 | The whole suite passes on the Fake camera on net8.0 and net10.0. | A new test that opens a camera other than through `CameraTestHelpers`. |

`PopBuffer(ulong.MaxValue)` keeps going to the blocking pop and is out of the conversion.

## Open questions

1. **Scope of the null check.** (a) Only `GetAvailableEnumerationValues`, as the issue says.
   (b) All sixteen public members of `Camera` that take a feature name, with one theory
   test over them, after checking what each does with `null` today. Recommendation: (b);
   it is the same defect class, the fix is one line per member, and leaving fifteen
   members unexamined invites the same finding again. It makes the change larger
   (one file, sixteen sites) but stays at the standard level.
2. **Wording of the disposal rule** (AC-4). Observed on GLib 2.88 during the reviews of
   14.11.2026: the queue is not freed while a thread waits on it, GLib logs a critical,
   the queue leaks and a waiter without timeout never returns. Recommendation: state
   exactly that, and that the behaviour on a GLib built without checks is undefined.

## Decisions

- **14.11.2026, gate 1, alex.** Spec and plan approved with both recommendations.
  Question 1: option (b), all sixteen public members of `Camera` that take a feature
  name get the null check, after checking what each does with `null` today; AC-1 extends
  to them, each throwing `ArgumentNullException` with `ParamName` `feature` (or the
  member's actual parameter name) before any native call. Question 2: the disposal rule
  states the GLib 2.88 behaviour observed and calls the behaviour on a GLib built without
  checks undefined. Replaces: the open questions above. Recorded here and on issue #12.

## Plan

1. Red test for AC-1 (and for the other members if question 1 is settled as (b)) in a new
   file `AravisSharp.Tests/CameraArgumentTests.cs`, opening the camera through
   `CameraTestHelpers.OpenTestCamera()`; then the null checks in `Camera.cs`.
2. Tests for AC-2 and AC-3 in a new file `AravisSharp.Tests/StreamTimeoutTests.cs`; they
   do not compile until `Stream.ToTimeoutMicroseconds` exists, which is their red; then
   the function, with `PopBuffer(ulong)` calling it. `InternalsVisibleTo` for the test
   project is already in `AravisSharp.csproj`.
3. XML documentation for AC-4.
4. The test table of `AravisSharp.Tests/README.md` gains the two new files.
5. Whole suite on both frameworks, then OCR and the C# review on the diff.
