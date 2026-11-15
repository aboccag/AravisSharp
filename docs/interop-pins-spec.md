# Spec: tests pinning the untested interop behaviours of `main`

Issue #13, level standard, type feature (tests only). Written on 2026-11-15 against `main`
at `6da311a`, before any code. Source material: the tests of the branch
`fix/native-leak-and-stream-pop` (not merged, built on an older base). `NON VÉRIFIÉ` marks
what was not checked.

## Premises checked on `6da311a`

| # | Behaviour on `main` | Where | Test on `main` |
|---|---|---|---|
| B1 | `PopBuffer()` blocks until a buffer is in the output queue. | `AravisSharp/Stream.cs:277-281` | none (`CameraLifecycleTests.cs:78` only checks `ObjectDisposedException` after dispose) |
| B2 | `PopBuffer(ulong)` with a finite timeout, and with `ulong.MaxValue`, blocks until a buffer arrives. | `Stream.cs:298-305` | none; `StreamTimeoutTests` covers only the pure conversion |
| B3 | `PopBuffer(0)` on an empty queue returns `null` at once. | `Stream.cs:304` | none |
| B4 | The array returned by each `arv_camera_dup_available_*` call is freed once, by `g_free`, after copying the strings. | `Camera.cs:1772`, `:1793`, `:1817`; `MarshalStringArray` at `:2171-2189` | `GetAvailablePixelFormats` twice (`CameraLifecycleTests.cs:175-191`); display names never |
| B5 | `Stream.Dispose()` called twice unrefs the native stream once. | `Stream.cs:378-391` | none |
| B6 | A stream never disposed is released once by its finalizer. | `Stream.cs:393-396` | none |
| B7 | A stream is not finalized while its own pop is blocked: `WrapBuffer` calls `GC.KeepAlive(this)` after the native call returns. | `Stream.cs:316-320` | none |
| B8 | No `DllImport` binds a variadic C function. `g_object_set`/`get` are replaced by the GValue property calls. | `GLibNative.cs:91-93` | none |
| B9 | `GLibNative.GValue` is `IntPtr GType; long Data0; long Data1`: 24 bytes on 64-bit, offsets 0, 8, 16, like `struct _GValue`. | `GLibNative.cs:101-108` | none |
| B10 | A Mono16 buffer saves to PNG with each sample scaled to 8 bits. | `AravisSharp/Utilities/ImageHelper.cs:122-136` | none; the Fake camera offers Mono16 (`CameraLifecycleTests.cs:191`) |

Other premises:

- CI runs the suite with `-c Release` (`.github/workflows/build-and-publish.yml:223`). B7 can
  only be seen when the library code is optimized, so a test of B7 can run in CI.
- `AravisLibrary.ResolveDllImport` is private (`AravisLibrary.cs:42`). A test that needs
  `g_object_weak_ref` can still reach libgobject through
  `NativeLibrary.Load(GLibNative.GObjectLibraryName, assembly, null)`, which calls the
  registered resolver (`NON VÉRIFIÉ`). `InternalsVisibleTo` gives the tests
  `Stream.Handle` and `GLibNative.GObjectLibraryName`. No library change is needed.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | One test per behaviour B1 to B10, in the conventions of `main`: a camera is opened only through `CameraTestHelpers`, the Fake camera by default, `[NativeFact]`/`[NativeTheory]` for tests that need native Aravis. | A test that needs a physical camera to pass; a test that passes without exercising its behaviour (for example a reflection scan that finds no import at all). |
| AC-2 | Each test fails when its behaviour is broken: a temporary mutation of the library, the red run, then the restore. The mutation and the failure seen are listed in the final report and in the issue. | A test whose mutation was not run; a mutation left in the code. |
| AC-3 | B1 to B3: timing assertions with margins that hold on a loaded CI runner. A blocking pop is released by a buffer pushed from another thread, and the test can never hang: every wait has an upper bound. | A test that hangs the test host when the behaviour breaks. |
| AC-4 | B5 and B6 count the releases of the native stream with `g_object_weak_ref`. The test holds its own native reference, so a double unref shows as a failed count, not as a native crash. | A test that frees memory it does not own. |
| AC-5 | B7 runs the scenario in a child test host, with `DOTNET_TieredCompilation=0` on a Release build. The test is skipped, not passed, on a Debug build. | A crash or hang of the child that takes down the parent test host. |
| AC-6 | No library code changes. | Any change under `AravisSharp/` other than a visibility change listed at gate 2. |
| AC-7 | The whole suite passes on the Fake camera, on net8.0 and net10.0, in Debug and in Release. | — |

## Risks

- Timing tests can be flaky on CI runners. Wide margins and upper-bounded waits keep that
  risk down.
- B4: a missing `g_free` only leaks, and no test sees a leak. The test calls every
  `dup_available` wrapper many times. It catches a double free or a wrong free, because
  glibc aborts the process. A leak is not covered (see question 2).
- B7: the child test host makes the test slower, by about the time of one `dotnet test`
  start (`NON VÉRIFIÉ`).

## Open questions

1. **B7, finalization during a blocked pop.** (a) Port the old branch's child-host test
   (`RedTeam/RT1_StreamFinalizedDuringBlockingPopTests.cs`): it observes the real defect,
   but it runs only in Release and costs one extra test-host start. (b) A reflection test
   that `WrapBuffer` keeps `this` alive: cheap, but it tests the code's shape, not its
   behaviour. Recommendation: (a). CI runs in Release, and (b) would not catch another
   path that loses the reference.
2. **B4, missing free.** No test sees a leak. (a) Accept it: the test pins a double or wrong
   free, and the spec says the leak is not covered. (b) Measure the process's memory over
   many calls: noisy and slow. Recommendation: (a).

## Decisions

- **15.11.2026, gate 1, alex.** Spec and plan approved with both recommendations.
  Question 1: option (a), B7 is pinned by the child test host ported from the old branch,
  run on Release builds and skipped on Debug builds. Question 2: option (a), the B4 test
  pins a double or wrong free, and a leak is stated as not covered. Replaces: the open
  questions above. Recorded here and on issue #13.
- **15.11.2026, alex.** The premise that `NativeLibrary.Load(name, assembly, null)` goes
  through the registered resolver is false: strace shows it does only the runtime's default
  probing, so it would miss a bundled libgobject. For B5 and B6, `AravisLibrary.ResolveDllImport`
  goes from `private` to `internal`, so the release probe loads libgobject exactly as the
  library does. This is the only library change; it changes no behaviour. Replaces: the
  second "Other premises" item above and the "no library change" of AC-6, for this one
  member. Recorded here and on issue #13.

## Plan

1. Test helpers ported from the old branch, adapted to `main`: the `g_object_weak_ref`
   probe (`GObjectReleaseProbe.cs`) and the 64-bit attribute (`SixtyFourBitFactAttribute`).
   The old `TestCleanup.cs` only if a test needs it.
2. In a new file `AravisSharp.Tests/InteropPinTests.cs`: B1 to B6 and B8 to B9. Each test
   is run against a temporary mutation of the library, then the mutation is restored.
3. B10 in a new file `AravisSharp.Tests/MonoExportFakeCameraTests.cs`.
4. B7 in its own file, with the attribute that skips it on Debug builds.
5. The test table in `AravisSharp.Tests/README.md`.
6. Whole suite on both frameworks, in Debug and in Release. Then the OCR review, and the C#
   review on the diff.
