# Spec: PopBuffer blocking test on real cameras

Issue #18, level standard, type fix, zone tests. Written on 2026-11-17 against `main` at
`c84d839`, before any change. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `c84d839`

- `InteropPinTests.PopBuffer_NoBufferQueued_ShouldBlockUntilOneIsPushed`
  (`AravisSharp.Tests/InteropPinTests.cs:40-98`) creates a stream with no buffer, starts the
  acquisition, checks that the pop blocks, and only then pushes one buffer. It ends with
  `Assert.Equal(ArvBufferStatus.Success, pop.Buffer!.Status)` (line 83).
- Observed failures of that assertion, all four cases each time:
  - GigE Basler acA1920-40gm (issue #18): status `Timeout`;
  - USB3 Basler acA2040-90uc, 17.11.2026, net8.0: status `Size_mismatch`. The net10.0 run
    of the same session passed. The failure is intermittent.
  - It passes on the Fake camera, and passed on the USB3 acA2440-75uc.
- The cause is in Aravis, not in the binding:
  - USB3 (default mode `ARV_UV_USB_MODE_ASYNC`, `aravis/src/arvenums.h:102`): a pushed
    buffer gets bulk transfers for leader, payload and trailer. The camera has been
    streaming with nobody reading, so the first transfers can land mid-frame. The trailer
    then reports a payload size different from the expected one, and Aravis closes the
    buffer as `SIZE_MISMATCH` (`aravis/src/arvuvstream.c:300-313`), or as `MISSING_PACKETS`
    when the leader is not a leader (`:178-181`).
  - GigE: the buffer is attached to a frame already in flight. Its first packets were
    dropped for lack of a buffer, so the frame never completes and is closed as `TIMEOUT`
    (`aravis/src/arvgvstream.c:833`). This is the hypothesis of the issue (`NON VÉRIFIÉ`
    with a packet trace).
  - The Fake camera generates a whole frame into the buffer it receives, so it always
    reports `SUCCESS`.
- What #13 pins (`docs/interop-pins-spec.md:12-13`, B1 and B2): the pop blocks until a
  buffer is in the output queue, and returns when one arrives. The status of that buffer
  is not part of B1 or B2.
- `Camera.SetTrigger`, `IsSoftwareTriggerSupported` and `SoftwareTrigger` exist
  (`AravisSharp/Camera.cs:574,664,706`). The software trigger is used only by
  `[FakeCameraFact]` tests today; it was never run on the GigE or USB3 cameras.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | The four cases pass on the Fake camera, on the USB3 camera and on one GigE camera from the allowed list. | A case skipped on a real camera. |
| AC-2 | The test still fails if `PopBuffer` returns before a buffer reaches the output queue (checked once by making the pop non-blocking in a throw-away change, then reverted). | An assertion weakened to the point where a non-blocking pop passes. |
| AC-3 | The test still fails if the pop returns nothing (null buffer or an exception) after the release. | A test that passes on any outcome. |
| AC-4 | The test still pins the timeout mapping: the four `InlineData` cases are unchanged. | A case removed. |
| AC-5 | No change to the library (`AravisSharp/`) or to `aravis/`. | A library change made only to pass the test. |

## Open question (gate 1)

1. **What the test asserts after the release.**
   - (a) Keep the push during acquisition, and accept every status Aravis gives a frame
     it closed: `Success`, `Timeout`, `Missing_packets`, `Wrong_packet_id`,
     `Size_mismatch`. Still reject `Cleared`, `Aborted`, `Filling` and `Unknown`, which
     would mean the buffer was not handled by the stream thread. This is the smallest
     change and works on every camera with no new dependency.
   - (b) Push the buffer before `StartAcquisition`, with the camera in software trigger
     mode, so no frame starts until the test calls `SoftwareTrigger()`. The pop is then
     released by a whole frame, and the test can still require `Success`. But the release
     comes from a trigger, not a push: the test is renamed, and it depends on the
     software trigger working on every real camera (`NON VÉRIFIÉ` on the Opto Engineering
     cameras).
   - (c) Keep the push, and after the first pop push buffers again until one comes back
     `Success`, with a bound. This keeps `Success`, but makes the test longer and its
     duration depend on the camera.

   Recommendation: (a). Status is not part of B1 and B2, and (a) is deterministic on
   every camera.

## Gate 1 decision

2026-11-17, alex: option (a). The test keeps the push during acquisition and accepts every
status of a frame closed by the stream thread (`Success`, `Timeout`, `Missing_packets`,
`Wrong_packet_id`, `Size_mismatch`); it rejects `Cleared`, `Aborted`, `Filling`, `Unknown`
and `PayloadNotSupported`.

## Plan

1. Red: none to write. The failure is reproduced by the hardware run of #7 (USB3, net8.0).
2. Change the final assertion of the test according to the decision, with a comment
   explaining why a real camera may close the first frame as incomplete.
3. Run the whole suite on the Fake camera (net8.0 and net10.0).
4. Hardware: run `InteropPinTests` on the USB3 camera, then on one GigE camera of the
   allowed list, one at a time, several times on USB3 since the failure is intermittent.
5. Reviews: OCR and `ecc:csharp-reviewer`. Merge `--no-ff` with `Closes #18`.
