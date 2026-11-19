# Spec: expose the GigE socket buffer size the system grants

Issue #17, level standard, type feature, zone stream. Written on 2026-11-19 against `main`
at `8594ab7`, before any change. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `8594ab7`

- `Stream.SetSocketBufferSize` sets the Aravis property `socket-buffer-size`;
  `GetSocketBufferSize` reads it back, so it returns the requested size, not the granted
  one (`AravisSharp/Stream.cs:133-168`). `ConfigureGigEDefaults` requests 4 MiB with the
  `Auto` policy (`Stream.cs:300-309`). Both XML docs already describe the Linux cap (#10).
- Aravis 0.8.36 has no public API that returns the granted size or the stream's socket:
  `arvgvstream.h` exposes only `arv_gv_stream_get_port` and `arv_gv_stream_get_statistics`.
- Aravis applies the size inside its stream thread, at the start of each frame, only when
  it differs from the last size applied (`_update_socket`, `aravis/src/arvgvstream.c:225-262`,
  called at `:402`). With `Auto`, the size is one payload plus protocol overhead, capped by
  `socket-buffer-size`, and never below `ARV_GV_STREAM_MIN_BUFFER_SIZE`. The setter calls
  `setsockopt(SO_RCVBUF)` and reads the value back; when the value read back is smaller
  than the one requested, it logs `arv_warning_interface` and returns FALSE
  (`aravis/src/arvnetwork.c:451-503`).
- Linux, checked on this machine with a UDP socket: `SO_RCVBUF` set to 100 000 reads back
  200 000; set to 4 MiB with `net.core.rmem_max` = 4 194 304, it reads back 8 388 608. The
  kernel caps the requested value at `rmem_max` and then doubles it. So Aravis' own warning
  fires only when the request exceeds twice `rmem_max`, and a request between `rmem_max`
  and twice `rmem_max` is reduced with no warning at all.
- The cap is readable without privilege in `/proc/sys/net/core/rmem_max`. Aravis uses
  `SO_RCVBUF`, not `SO_RCVBUFFORCE`, so the cap applies to every process.
- On this machine `rmem_max` is 4 194 304 (raised from the default 212 992). A hardware
  check of the cap therefore needs a request above 4 MiB; changing `rmem_max` is a system
  setting and is left to alex.
- Windows and macOS: Windows has no comparable system-wide cap on `SO_RCVBUF`
  (`NON VÉRIFIÉ`); macOS caps socket buffers with `kern.ipc.maxsockbuf` (`NON VÉRIFIÉ`). No
  Windows or macOS machine is available here; CI runs only the package smoke on them.
- The test suite already has a GigE simulator on 127.0.0.1 (`GvFakeCameraFixture`,
  `AravisSharp.Tests/GigEStreamTests.cs:7-53`), so a GigE stream is available in CI.
  `ss -uamn` shows the real receive buffer (`skmem rb`) of a UDP socket by port, which a
  test can use to check the library's answer against the kernel.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | On Linux a caller can tell, with no privilege and no debug output, that a requested socket buffer size was reduced and to what size. | An answer that needs `ARV_DEBUG`, root, or reading Aravis' log. |
| AC-2 | On Windows and macOS the new member has a documented result (see question 3), and the XML docs of `SetSocketBufferSize` and `ConfigureGigEDefaults` point to it. | A member that throws or returns a made-up number on those systems. |
| AC-3 | The answer matches the kernel: on the GigE simulator, during acquisition, the size the library reports times two equals `skmem rb` of the stream's socket, for one request below and one above `rmem_max`. | An answer only tested against itself. |
| AC-4 | A test covers an unreadable or malformed cap file (internal seam), with the documented result. | An exception escaping from a read failure. |
| AC-5 | No change to `aravis/`; no new native dependency other than what question 1 decides. | — |
| AC-6 | Changelog entry; the whole suite passes on the Fake camera, net8.0 and net10.0; hardware check on one allowed GigE camera with a request above `rmem_max`. | — |

## Open questions (gate 1)

1. **Where the answer comes from.**
   - (a) **Predict from the cap.** Read `/proc/sys/net/core/rmem_max`; the granted size is
     `min(requested, rmem_max)`. Managed code only, no native call, works before the first
     frame. It is a prediction, but the kernel rule it relies on is fixed and AC-3 checks it.
   - (b) **Measure the socket.** Find the stream's socket among the process' file
     descriptors by its local port (`arv_gv_stream_get_port`, `/proc/self/fd`,
     `/proc/net/udp`), then call `getsockopt(SO_RCVBUF)` through libc and halve the result.
     It reads what the kernel holds, but only after the first frame (before it, the socket
     has the system default), needs a new libc P/Invoke and file-descriptor scanning, and is
     Linux-only as well.

   Recommendation: (a).
2. **API shape.**
   - (a) A static method `Stream.GetSocketBufferSizeLimit()` returning `int?`: the cap in
     bytes, or null when the system has no known cap. A method, not a property, since the
     cap can change while the process runs. The caller compares it with
     `GetSocketBufferSize()`; the XML docs show how.
   - (b) (a) plus an instance method `GetGrantedSocketBufferSize()` returning
     `min(GetSocketBufferSize(), limit)` for the `Fixed` policy. For `Auto` the granted size
     also depends on the payload, so the method would need the payload or would be
     misleading.
   - (c) Change `SetSocketBufferSize` to return the granted size: a breaking change of a
     published signature.

   Recommendation: (a).
3. **Windows and macOS.**
   - (a) Return null and document why: no known cap on Windows, macOS not checked.
   - (b) Also read `kern.ipc.maxsockbuf` on macOS through `sysctlbyname` (new libc P/Invoke,
     no macOS machine to check it).

   Recommendation: (a), with macOS left to a follow-up issue if wanted.

## Plan (if the recommendations are kept)

1. Red tests: the cap read on Linux equals `/proc/sys/net/core/rmem_max`; null on other
   systems; malformed or missing file through an internal seam gives null; AC-3 on the
   GigE simulator with `ss`, skipped when `ss` is missing.
2. `Stream.GetSocketBufferSizeLimit()`, XML docs of the three members, changelog,
   `docs/COMPREHENSIVE_API.md`.
3. Whole suite on the Fake camera, net8.0 and net10.0.
4. Reviews: OCR and `ecc:csharp-reviewer`.
5. Hardware: one allowed GigE camera, request 8 MiB, check the reported limit against `ss`.
6. Merge `--no-ff` with `Closes #17`.
