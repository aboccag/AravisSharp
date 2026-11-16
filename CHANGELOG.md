# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Package versions mirror the native Aravis version they target (`0.8.36` targets
`libaravis-0.8` at 0.8.36; managed-only fixes use a fourth component such as
`0.8.36.1`), so this project does not follow plain Semantic Versioning. See the
*Versioning* section of the README.

## [Unreleased]

### Added
- `ImageHelper.DemosaicBayer8`, and PNG and JPEG export of `BayerRG8`, `BayerGR8`,
  `BayerGB8` and `BayerBG8` buffers (bilinear demosaicing, a diagnostics path) (#6).
- XML documentation file shipped in the package (#9).
- Symbol package (`.snupkg`) and Source Link metadata.
- Third-party notices, license texts and `aravis-native-versions.txt` in every
  `runtimes/{rid}/native/` folder; the corresponding sources are attached to each release.
- `CHANGELOG.md` and `.editorconfig`; the guides moved to `docs/` (#9).
- `AravisException.Domain` and `AravisException.Code`: the domain (the string of its
  GQuark, such as `arv-device-error-quark`) and code of the native `GError`; both are
  `null` on an exception that does not come from a `GError` (#10).
- `Stream.IsGigEVision`: true on a GigE Vision stream, the one on which the GigE members of
  `Stream` can be used (#10).

### Changed
- The Aravis bundled with the application is loaded before a system one.
- Linux natives are built on Ubuntu 20.04 with libxml2 linked in statically; only
  `libaravis-0.8.so.0` ships, GLib and libusb come from the distribution.
- win-x64 natives are built with MSYS2 UCRT64 and need the Universal C Runtime (Windows 10
  and later) (#16).
- The binding is licensed under MPL-2.0; packages are published through nuget.org
  Trusted Publishing.
- Tests open real hardware only when `ARAVIS_TEST_DEVICE_ID` names a device; CI runs the
  whole suite on the Fake camera and smoke-tests the packed NuGet on every platform.
- The GObject property helpers of `GLibNative` (`SetIntProperty`, `SetUIntProperty`,
  `SetEnumProperty`, `GetIntProperty`, `GetUIntProperty`, `GetEnumProperty`), never
  released, are internal. The v0.8.36 entry points `g_object_set_int`, `g_object_set_uint`,
  `g_object_set_bool`, `g_object_get_int`, `g_object_get_uint` and `g_object_get_bool` are
  back as `[Obsolete]` managed methods with the same signatures, forwarding to the checked
  helpers instead of calling the variadic `g_object_set` / `g_object_get`. They are strict:
  where v0.8.36's variadic calls converted between int and uint or only logged a GLib
  warning, they throw `ArgumentException` on an unknown property or a type mismatch and
  `ArgumentOutOfRangeException` on a refused value (#14).
- The GigE members of `Stream` throw `NotSupportedException` on a stream that is not GigE
  Vision (see `Stream.IsGigEVision`), instead of the unreleased `InvalidOperationException`
  (#10).
- `NodeMap.GetFeatureDetails`, `NodeMap.GetFeaturesInCategory` and
  `FeatureDetails.FromNode` no longer swallow every exception: they absorb an Aravis error,
  and a missing entry point of an older system libaravis where they read optional feature
  metadata; anything else, such as `ObjectDisposedException` on a disposed `Device`,
  propagates. `NodeMap.GetNode` no longer catches anything (#10).
- The `NodeMap` feature accessors keep throwing `InvalidOperationException`, now with the
  native error message and an `AravisException` inner exception carrying its domain and
  code (#10).
- `Camera`, `Device`, `NodeMap`, `GenICamNode`, `Stream` and `Buffer` may be used from
  several threads. Calls on one camera and on the `Device`, `NodeMap` and `GenICamNode`
  objects obtained from it are serialised by a lock of the camera (single calls, not
  sequences); `Stream` and `Buffer` calls are not serialised. `Dispose()` is atomic and may
  be called from any thread: from that moment every member throws
  `ObjectDisposedException`, calls waiting for the camera's lock give up with it, and the
  native object is released once the calls already in progress have returned (#7).
- `Device` and `NodeMap` belong to their `Camera`: they hold no native reference and throw
  `ObjectDisposedException` once the camera is disposed, so an undisposed `Device` no longer
  keeps the camera's connection open. `Device.Dispose()` only closes that wrapper. This
  replaces the unreleased change that kept a `Device` usable after its camera was disposed;
  in v0.8.36 such a `Device` used a released native device (#7).
- `Stream.PopBuffer()` and `Stream.PopBuffer(ulong)` wait in timed native pops of at most
  100 ms: a buffer is still returned as soon as it is ready, and `Stream.Dispose()` called
  on another thread ends the wait with `ObjectDisposedException` within about 100 ms. The
  rule documented in #12, not to dispose a stream while a thread waits in a pop, is gone (#7).
- Of several threads pushing the same `Buffer` with `Stream.PushBuffer`, one queues it and
  the others get `ObjectDisposedException` (#7).
- `Camera`, `Stream`, `Buffer` and `Device` no longer declare finalizers: an internal
  `SafeHandle` releases the native reference of an abandoned wrapper. A `Stream` that is
  never disposed is therefore released on the finalizer thread, where Aravis stops its
  stream thread (#7).
- `Buffer.Dispose()` no longer swallows exceptions (#7).
- `GenICamNode` members throw `ObjectDisposedException` once its camera or the node itself
  is disposed (#7).

### Fixed
- The declarations of `arv_camera_gv_get_packet_size` and `arv_camera_gv_auto_packet_size`
  return `uint`, as the `guint` of Aravis; `Camera.GvGetPacketSize` still returns `int`
  (a checked conversion) and `Camera.GvAutoPacketSize` still returns nothing. A test now
  compares every P/Invoke declaration of the assembly with its C prototype in the Aravis
  and GLib headers, the pointee of `out`/`ref` and array parameters and the `ARV_API`
  export of each Aravis function included (#11).
- **Breaking for direct callers of the public `AravisNative` class:**
  `AravisNative.arv_camera_gv_get_packet_size` and
  `AravisNative.arv_camera_gv_auto_packet_size` now return `uint` (they returned `int` and
  `void`); recompile, and prefer `Camera.GvGetPacketSize` (#11).
- Native signatures, ownership and object lifetimes in the binding; the `dup_*` arrays
  are freed once.
- The `Camera` members that take a feature name throw `ArgumentNullException` on `null`
  instead of calling Aravis with it (#12).
- `Stream.PopBuffer(ulong)` caps its timeout conversion and documents the cap and the
  disposal rule (#12).
- A double free of the `GError` when `arv_camera_new` returns null.
- A native camera, stream or buffer released twice when `Dispose()` ran on several threads
  at once, and released under a native call still running on another thread (#7).
- One buffer queued, and later released, twice by concurrent `Stream.PushBuffer` calls,
  which crashed the process (#7).
- `Device.NodeMap` read from several threads at once could create several node maps (#7).
- GObject property access throws `ArgumentException` on an unknown property or one of
  another type, instead of a GLib warning and a silent no-op or 0, and reads back each
  write: a value Aravis refuses (outside the property's range, not an enum member) throws
  `ArgumentOutOfRangeException` from the GigE setters of `Stream`, naming the setter's
  parameter (#14).
- The eleven `Gv*` members of `Camera` throw `NotSupportedException` on a camera that is
  not GigE Vision (see `Camera.IsGigEVisionDevice`). They used to call Aravis anyway, which
  logged a GLib CRITICAL and returned 0 or did nothing (#10).
- The XML documentation of `Stream.SetSocketBufferSize` and `Stream.ConfigureGigEDefaults`
  states that Linux silently caps the socket buffer at `net.core.rmem_max` (#10).
- `NodeMap.GetNode` returns `null` for a node the device does not have, checked through
  `arv_device_get_feature`. It used to return a `FeatureInfo` for any name, because a
  failed string read returns `null` rather than throwing; an existing node that is not a
  string (such as `Width`) still gives a `FeatureInfo` with a `null` `Value` (#10).

## [0.8.36] - 2026-08-21

First package version derived from the native Aravis version.

### Added
- `CLAUDE.md` with build, test and architecture notes.

### Changed
- The package version is derived from `AravisNativeVersion` and
  `AravisSharpPatchVersion` in `AravisSharp.csproj`.

### Fixed
- CI check that the `aravis/` submodule matches `AravisNativeVersion` failed
  because of a `sed` delimiter clash.
- Tests turn auto exposure off before setting an exposure time.

## [0.1.0] - 2026-05-27

### Added
- Initial C# binding for Aravis: `Camera`, `CameraDiscovery`, `Stream`, `Buffer`,
  `Device`, zero-copy buffer access and acquisition statistics.
- GenICam node map access and feature introspection (`NodeMap`, `FeatureDetails`),
  with feature browser examples.
- Cross-platform native library resolver (`AravisLibrary`) and Windows setup guide.
- GigE Vision support: stream tuning, diagnostics, IP configuration and a
  network configurator example.

[Unreleased]: https://github.com/aboccag/AravisSharp/compare/v0.8.36...HEAD
[0.8.36]: https://github.com/aboccag/AravisSharp/compare/v0.1.0...v0.8.36
[0.1.0]: https://github.com/aboccag/AravisSharp/releases/tag/v0.1.0
