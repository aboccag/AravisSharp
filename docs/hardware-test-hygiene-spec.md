# Spec: leave a real camera as the suite found it

Issue #21, level standard, type fix, zone tests. Written on 2026-11-19 against `main` at
`2e04e3a`, before any change. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `2e04e3a`

- `NodeMapTests.NodeMap_ShouldReadAndWriteThroughTheDevice`
  (`AravisSharp.Tests/NodeMapTests.cs:181-192`) sets `Width` to its minimum plus one
  increment and does not restore it. After the whole-suite run of 19.11.2026 the Opto
  ITA24 was at `Width` 264 (full: 1936) and the ITA32 at 272 (full: 2064); the Basler
  acA1920-40gm read 1936 afterwards (`NON VÉRIFIÉ` why).
- That test is not the only writer. The tests call, on the camera they open:
  `SetAcquisitionMode` (23 calls), `SetGainAuto` (7), `SetPixelFormat` (5),
  `SetExposureTimeAuto` (5), `SetIntegerFeature("Width")` (4), `SetExposureTime` (3),
  `GvSetPacketSize` (3), `GvAutoPacketSize` (3), `UvSetBandwidth` (2), `SetFrameCount` (2),
  `SetIntegerFeature("AcquisitionFramePeriod")` (2), `SetGain`, `GvSetPacketDelay`,
  `ClearTriggers`, and `CameraTestHelpers.ConfigureForAcquisition` (exposure auto off,
  exposure 10 ms, triggers cleared, GigE packet size negotiated). Some restore what they
  change, some do not; which settings differ after a run was not measured
  (`NON VÉRIFIÉ`, step 1 of the plan measures it).
- Every camera open in the suite goes through `CameraTestHelpers.ResolveTestDeviceId()`
  (`AravisSharp.Tests/CameraTestHelpers.cs:65-84`), most through `OpenTestCamera()`
  (`:87`). Tests never run in parallel (`TestSettings.cs:4`). The two frameworks run one
  after the other, each in its own test host process.
- xunit 2.9.3 has no assembly fixture; code that must run once at the end of a test run
  goes in a class derived from `XunitTestFramework`, named by an assembly-level
  `TestFramework` attribute, whose `Dispose` runs when the run ends. It does not run when
  the test host is killed (hang, crash).
- `ImageAcquisitionTests.AcquireSingleFrame_DataSizeMatchesDimensions`
  (`AravisSharp.Tests/ImageAcquisitionTests.cs:117-140`) requires `Buffer.GetData()` to
  return exactly `ImageHelper.CalculateBufferSize(width, height, pixelFormat)` bytes. The
  Opto ITA32 rounds its payload up to a multiple of 512 bytes (420 352 for a 419 968-byte
  image; 3 187 200 for 3 186 816), with status `Success` and payload type `Image`.
- The library already accepts such a buffer: `ImageHelper` only rejects data that is too
  short (`AravisSharp/Utilities/ImageHelper.cs:191-194,248-249`). Only the test is too
  strict.
- `ResolveTestDeviceId` fails at the first discovery that does not list the requested
  device. On 19.11.2026 one discovery of a whole-suite run missed the Basler
  acA1920-40gm (net10.0, `CameraWrapperTests.DeviceType_ShouldBeEitherGigEOrUSB`).

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | After a whole-suite run on a real camera, every setting in the list decided at gate 1 has the value it had before the run. Shown by a dump of those settings before and after, on the ITA32, the Basler GigE and one USB3 camera. | A setting restored to a default instead of its previous value; a write to a camera the suite did not open. |
| AC-2 | The dump taken before any change shows the defect (the red evidence): at least `Width` differs after a run on the ITA32. | A fix claimed without the before-and-after dumps. |
| AC-3 | `NodeMap_ShouldReadAndWriteThroughTheDevice` restores `Width` itself, even when its assertions fail. | — |
| AC-4 | The frame size test passes on the ITA32, the ITA24, the Basler GigE and one USB3 camera, and still fails when the data is shorter than the image. | An assertion that accepts any size. |
| AC-5 | A discovery that misses the requested device once does not fail the test; a device that is really absent still fails it, with the list of visible devices. | A silent pass or an unbounded wait when the camera is absent. |
| AC-6 | On the Fake camera nothing changes: the whole suite passes on net8.0 and net10.0, and no restore code runs. | — |
| AC-7 | No change to the library (`AravisSharp/`) or to `aravis/`. | — |

## Open questions (gate 1)

1. **How the settings are restored.**
   - (a) **Per test.** Fix the `Width` test with `try`/`finally`, measure what else differs
     after a run, and add a restore to each test found. Small and local, but the next test
     that forgets to restore brings the defect back, unseen in CI.
   - (b) **Once per run.** The first time the suite opens a real camera it records a list of
     settings; when the test run ends it writes back the ones that changed (test framework
     hook). Covers present and future tests. Not run if the test host is killed; the
     record is then lost.
   - (c) (b), and the `Width` test also restores itself (AC-3), so that a killed run leaves
     less behind and later tests of the same run see the full image.

   Recommendation: (c).
2. **Which settings are recorded** (for 1b or 1c). Proposed list, each read and written
   only when the camera has it: `Width`, `Height`, `OffsetX`, `OffsetY`, `PixelFormat`,
   `AcquisitionMode`, `AcquisitionFrameCount`, `ExposureAuto`, exposure time, `GainAuto`,
   gain, frame rate and its enable, `AcquisitionFramePeriod`, `TriggerSelector`,
   `TriggerMode`, `TriggerSource`, GigE packet size and packet delay, USB3 bandwidth limit.
   Recommendation: this list, completed by whatever the measurement of step 1 shows.
3. **Frame size test.** (a) Require the data to be at least the image size, and keep the
   status check. (b) Also export the ITA32 frame through `ImageHelper` to a temporary file
   and check its dimensions. Recommendation: (a); the helpers already have their own
   padding tests.
4. **Discovery miss.** (a) `ResolveTestDeviceId` retries discovery, up to three times in
   all, before failing. (b) Leave it and note it in the tests README. Recommendation: (a).

## Gate 1 decision

2026-11-19, alex: 1c, the proposed list of settings completed by the measurement, 3a, 4a.

## Plan

1. Red evidence: a throw-away program dumps the settings of question 2 on the ITA32,
   before and after a whole-suite run at `2e04e3a`. Complete the list with what differs.
2. Record and restore in the test project; `try`/`finally` in the `Width` test; the frame
   size assertion; the discovery retry. Unit tests on the Fake camera for the record and
   restore logic, through an internal seam, since the Fake camera is never restored.
3. Whole suite on the Fake camera, net8.0 and net10.0.
4. Reviews: OCR and `ecc:csharp-reviewer`.
5. Hardware, one camera at a time: dumps before and after a whole-suite run on the ITA32,
   the Basler GigE and one USB3 camera (AC-1), and the frame size test on the ITA24.
6. Merge `--no-ff` with `Closes #21`.

## Red evidence

2026-11-19, Opto ITA32, whole suite at `2e04e3a` (net8.0), settings dumped before and
after by a throw-away program: `Width` 2064 became 272 and `AcquisitionMode` `Continuous`
became `SingleFrame`. Nothing else in the list differed. Both were put back by hand.

## Review findings and what changed

OCR and `ecc:csharp-reviewer` on `a82bd06`. Fixed in `298b7b8`:
- The restore ran from the test framework's disposal, which xunit does not await: on the
  ITA32 it did not happen (`AcquisitionMode` stayed on `SingleFrame`). It now runs in the
  assembly runner's `BeforeTestAssemblyFinishedAsync`, which the runner awaits.
- Floats were recorded from Aravis' text form (`%g`, six significant digits). They are now
  read as numbers and compared within one part in a million.
- A refused write threw out of the restore: `NodeMap` wraps the Aravis error in an
  `InvalidOperationException`. Found by the new tests; now caught and reported.
- The camera is read again after the writes; what still differs is reported, and makes the
  run fail as an assembly cleanup failure.
- A setting that can no longer be read is restored too; exposure and gain are not recorded
  while their auto mode drives them; putting the trigger selector back never throws.
The rewritten restore was not reviewed a second time; it is covered by the runs below.

## Verification

2026-11-19, branch at `298b7b8`, Debug.
- Fake camera, whole suite: 489 passed, 0 failed, 19 skipped out of 508, on net8.0 and on
  net10.0. No restore runs on the fake camera.
- Real cameras, whole suite, one at a time, settings dumped before and after:

  | Camera | Framework | Passed | Failed | Skipped | Settings after the run |
  |---|---|---|---|---|---|
  | Opto ITA32 (GigE) | net8.0 | 482 | 0 | 25 | identical |
  | Basler acA1920-40gm 24466563 (GigE) | net8.0 | 482 | 0 | 25 | identical |
  | Opto ITA24 (GigE) | net10.0 | 482 | 0 | 25 | identical |
  | Basler acA2440-75uc 22119564 (USB3) | net8.0 | 487 | 0 | 20 | identical |

  The frame size test passes on all four, the ITA32 included (AC-4).
- Not checked: a run killed half-way (documented as not restored); a camera whose
  exposure or gain auto mode is on; the report line of a successful restore, which
  `dotnet test` does not print (only a failed restore is visible, as a cleanup failure);
  the discovery retry on hardware (covered by its unit tests only).
- Seen once and not reproduced: during the red run on the ITA32,
  `InteropPinTests.PopBuffer_NoBufferQueued_ShouldBlockUntilOneIsPushed(600000)` failed
  with "did not return within 10 s after a buffer was pushed". It passed in the three
  other whole-suite runs on that camera the same day.
