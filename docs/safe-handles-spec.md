# Spec: SafeHandle and lifetime of native objects

Issue #7, level complet (zones `interop`, `camera`, `stream`, `genicam`), type refactor.
Written on 2026-11-16 against `main` at `8fd1bff`, before any code. It replaces the spec of
13.11.2026 on `claude/safe-handles-90b248` (`docs/safe-handles-spec.md` at `cc0aedd`,
base `ec7b30b`, 99 commits behind `main`). That branch is source material only.
`NON VÉRIFIÉ` marks what was not checked.

## What `main` already fixed

Since the review of 13.11.2026, the #4–#6 fixes (`128d025` and later) and #10 changed the
ground:

- **Single-thread lifetimes.** Every wrapper calls `GC.KeepAlive(this)` after its native
  calls, so the finalizer cannot run during a call made by the same thread. Pinned by
  `CameraLifecycleTests` (dispose twice, use after dispose, `PushBuffer` ownership),
  `StreamFinalizationDuringPopTests` and `GObjectReleaseProbe`.
- **`Device`.** `Device(IntPtr)` now takes its own `g_object_ref` (`Device.cs:19`): a
  `Device` and its `NodeMap` stay usable after the camera is disposed
  (`Device_ShouldStayUsable_AfterCameraIsDisposed`). `v0.8.36` wrapped the borrowed
  pointer without a reference: this change is not released.
- **Moved to #10 and done:** GigE guard and `Stream.IsGigEVision`, `GError` domain and
  code, narrowed catch blocks.
- **Docs:** `Buffer.GetDataSpan()` and `GetData()` say until when the memory is valid
  and point to `CopyData` / `CopyDataTo`.

## Premises checked on `8fd1bff` (by reading the code)

1. **Dispose is not atomic.** `Camera`, `Stream`, `Buffer` and `Device` test `_disposed`,
   unref, then set it (`Camera.cs:2247`, `Stream.cs:432`, `Buffer.cs:323`,
   `Device.cs:304`): two threads disposing one wrapper can both call `g_object_unref`.
2. **Dispose during a call on another thread.** `GC.KeepAlive` only holds off the
   finalizer. An explicit `Dispose()` on thread A while thread B is inside a native call
   on the same object unrefs it under B: a use-after-free. `Stream.PopBuffer()` documents
   this for pops only (`Stream.cs:322`: GLib critical, leaked queue, a waiter that never
   returns).
3. **Concurrent `PushBuffer` of one `Buffer`.** Both threads read `buffer.Handle` before
   either calls `ReleaseOwnership()` (`Stream.cs:302`): the native buffer can be queued
   twice and released twice by the stream.
4. **`Device` holds the connection.** With its own reference, a `Device` that is not
   disposed keeps the `ArvDevice`, and so the USB3 interface or GigE control channel,
   until its finalizer runs. On 14.11.2026 a live `Stream` blocked reopening the Basler
   USB3 camera with `LIBUSB_ERROR_BUSY` (spec of 13.11, addendum D); a live `Device` holds
   the same reference, so the same effect is expected (`NON VÉRIFIÉ` on `main`).
   `Camera.GetDevice()` does not say so.
5. **`KeepAlive` is per call site and unchecked.** A forgotten `GC.KeepAlive` is a
   use-after-free that only finalizer timing reveals; no test checks the call sites.
6. **`GenICamNode`** stores borrowed node and genicam pointers with no owner
   (`GenICamNode.cs:22`). Its constructor is internal and only `ErrorInfoTests` calls it.
7. **`Buffer.Dispose`** wraps `g_object_unref` in a bare `catch` (`Buffer.cs:333`).

## Decisions that still stand (13.11 and 14.11.2026, alex)

- Gate 1 of 13.11: the wrappers are fully thread-safe; one internal `GObjectSafeHandle`
  with a lease taken around each native call; P/Invoke signatures keep `IntPtr`; calls
  on one camera (and its `Device` / `NodeMap`) are serialised by a lock owned by the
  camera's handle, released before the unref; `GenICamNode` gets the same owner pinning.
- 14.11, decision 3: reopening a camera is a documented limit. The native release waits
  for calls in flight, and a live `Stream` keeps the device open.
- 14.11, decision 4: every behaviour change visible to callers goes in the changelog.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | Each owned reference of `Camera`, `Stream`, `Buffer` (and `Device`, depending on question 2) is released exactly once, whether `Dispose()` is called never, once, several times, or by several threads at once. | A second `g_object_unref`; a GLib critical on release. |
| AC-2 | A native object is never released while a native call on it is in flight on another thread: the last unref runs when the leases taken before `Dispose()` are returned. | `Dispose()` or the finalizer unrefing an object a native function is still using. |
| AC-3 | Once `Dispose()` has returned on one thread, every later call on that wrapper on any thread throws `ObjectDisposedException`. Calls already inside native code complete normally. | A call accepted after `Dispose()` returned; another exception type caused by the disposal. |
| AC-4 | Several threads pushing the same `Buffer` queue it exactly once; the others get `ObjectDisposedException`. | One native buffer queued twice; a lost buffer. |
| AC-5 | `Device` and `NodeMap` follow the decision of question 2, documented on `Camera.GetDevice()` and `Device`. | A `Device` silently blocking the camera from being reopened without that being documented. |
| AC-6 | Blocking pops follow the decision of question 3. | A thread left waiting forever, or a GLib critical, because the stream was disposed. |
| AC-7 | A test scans the library sources and fails when a method reaches `AravisNative.` without taking a lease, except for entries of an explicit allow-list that each give a reason. | A native call outside a lease that no test reports. |
| AC-8 | `GenICamNode` holds its owner (decision of 13.11); the bare `catch` of `Buffer.Dispose` goes, or is narrowed with a reason. | — |
| AC-9 | P/Invoke signatures unchanged (the header check of #11 stays green); every public API change is listed in this spec and approved at gate 1; changelog entries for each behaviour change. | A public member removed or re-typed without approval. |
| AC-10 | Each new test is shown red on `main` first, where a red is possible (a race test can only show the absence of a crash: said so in the test). Whole suite passes on the Fake camera, net8.0 and net10.0, Debug and Release; then one hardware run on the USB3 camera (reopen, create/acquire/dispose cycles, dispose while in use) and on a GigE camera if one is free, one at a time. | A test that needs hardware to pass in CI. |

## Open questions

1. **How far thread safety goes.** (a) As decided on 13.11: `GObjectSafeHandle` plus a
   lease around every native call and the camera lock, ported from
   `claude/safe-handles-90b248` onto `main`'s code. (b) Minimal: atomic `Dispose()`
   (`Interlocked`) and an atomic `PushBuffer`, and document that a wrapper must not be
   disposed while another thread uses it; AC-2 and AC-7 are dropped. (c) Close #7 with
   documentation only. Recommendation: (a). It is what gate 1 asked for; the design went
   through a council and a USB3 hardware run; (b) leaves premise 2 open as a
   use-after-free.
2. **`Device` after the camera is disposed.** (a) Old design: `Device` and `NodeMap`
   share the camera's handle, throw `ObjectDisposedException` once the camera is disposed,
   and never hold the connection; `Device.Dispose()` only marks the wrapper. The test
   `Device_ShouldStayUsable_AfterCameraIsDisposed` is inverted to expect the exception.
   (b) `main`'s design: `Device` keeps its own reference and stays usable; the connection
   stays open until the `Device` is disposed; documented on `GetDevice()`. Neither is
   released. Recommendation: (a). With (b), code that drops `camera.GetDevice().NodeMap`
   without disposing the `Device` cannot reopen the camera until a garbage collection, a
   failure that comes and goes; (a) was checked on 14.11 to reopen five times in a row
   with old `Device` wrappers still referenced.
3. **A thread blocked in `PopBuffer` when the stream is disposed.** (a) `PopBuffer()` and
   `PopBuffer(timeout)` wait in short timed pops (100 ms) and throw
   `ObjectDisposedException` once `Dispose()` is called; a buffer still arrives as soon as
   it is ready. Done on the old branch (`9efc426`) and tested there. (b) Keep `main`'s
   documented limit (`Stream.cs:322`); the lease then delays the release until the pop
   returns, possibly never. Recommendation: (a). It removes the last case where disposal
   leaves a thread waiting forever.

## Decisions

- **2026-11-16, gate 1, alex:** spec approved with the three recommendations. (1) Full
  thread safety as decided on 13.11: `GObjectSafeHandle`, a lease around every native
  call, the camera lock, ported onto `main`'s code (not merged from the old branch).
  (2) `Device` and `NodeMap` share the camera's handle and throw
  `ObjectDisposedException` once the camera is disposed; `Device.Dispose()` only marks the
  wrapper; `Device_ShouldStayUsable_AfterCameraIsDisposed` is inverted. (3) Blocking pops
  wait in 100 ms timed pops and throw `ObjectDisposedException` once the stream is
  disposed. No question left open, so no council.

- **2026-11-17, review of #7, alex:** the five reviews found that `AcquireSingleFrame`
  held the camera lock for its whole wait: with `timeoutUs = 0` and a software trigger
  sent from another thread, both threads block for good, and `Dispose()` cannot release
  the camera. Decision: the wait leaves the camera lock. `AcquireSingleFrame` drives its
  own stream and waits in the 100 ms slices of decision 3; only the camera calls that start
  and stop the acquisition take the lock, briefly. A `Dispose()` wakes the waiter with
  `ObjectDisposedException`.

- **2026-11-17, hardware run (AC-10):** whole suite, Debug, on the USB3 camera
  `Basler-2676014B32AF-21705391` (acA2040-90uc, approved by alex for this run; the
  acA2440-75uc was not connected). net8.0: 465 passed, 4 failed, 12 skipped; net10.0: 469
  passed, 12 skipped. The 4 failures are the four cases of
  `InteropPinTests.PopBuffer_NoBufferQueued_ShouldBlockUntilOneIsPushed` (`Size_mismatch`
  instead of `Success`), issue #18, seen on `main` before this branch and on one TFM only
  here. Every test of `HandleLifetimeTests`, `ConcurrencyTests` and `CameraReopenTests`
  passed on both TFMs (the two `[FakeCameraFact]` tests skip by design). **Not checked:**
  a GigE stream; `AcquireSingleFrame(0)` with a software trigger sent from another thread;
  the SingleFrame-to-Continuous fallback.

## Plan

1. Red tests on the Fake camera for AC-1 to AC-8, ported where possible from
   `LifetimeFakeCameraTests.cs` and `ConcurrencyFakeCameraTests.cs` of the old branch.
2. `Native/GObjectSafeHandle.cs` and the lease, from `cc0aedd`; then `Buffer`, `Stream`,
   `Camera`, `Device` / `NodeMap` / `GenICamNode`, each on `main`'s code rather than by
   merging the old branch. The `GC.KeepAlive` calls a lease makes useless go.
3. The lease check of AC-7; XML docs of the limits (camera lock, finalizer-thread unref of
   an abandoned stream, reopening); changelog.
4. Whole suite Debug and Release; hardware run on the USB3 camera
   (`HardwareLifetimeTests` from the old branch, adapted), GigE if free; one at a time.
5. OCR review, C# review, silent-failure hunter, test analysis and red team (issue #7
   lists it).
6. After integration, and only with alex's agreement: delete `claude/safe-handles-90b248`
   and `feat/10-gige-guard-gerror-catches`.
