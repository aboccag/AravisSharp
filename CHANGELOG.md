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
- XML documentation file shipped in the package, next to the symbol package (`.snupkg`)
  and Source Link metadata (#9).
- Third-party notices, license texts and `aravis-native-versions.txt` in every
  `runtimes/{rid}/native/` folder; the corresponding sources are attached to each release.
- `CHANGELOG.md` and `.editorconfig`; the guides moved to `docs/` (#9).

### Changed
- One all-in-one `AravisSharp` package bundles the native runtimes of linux-x64,
  linux-arm64, win-x64 and osx-arm64; the Aravis bundled with the application is loaded
  before a system one.
- Linux natives are built on Ubuntu 20.04 with libxml2 linked in statically; only
  `libaravis-0.8.so.0` ships, GLib and libusb come from the distribution.
- win-x64 natives are built with MSYS2 UCRT64 and need the Universal C Runtime (Windows 10
  and later) (#16).
- The binding is licensed under MPL-2.0; packages are published through nuget.org
  Trusted Publishing.
- Tests open real hardware only when `ARAVIS_TEST_DEVICE_ID` names a device; CI runs the
  whole suite on the Fake camera and smoke-tests the packed NuGet on every platform.

### Fixed
- Native signatures, ownership and object lifetimes in the binding; the `dup_*` arrays
  are freed once.
- The `Camera` members that take a feature name throw `ArgumentNullException` on `null`
  instead of calling Aravis with it (#12).
- `Stream.PopBuffer(ulong)` caps its timeout conversion and documents the cap and the
  disposal rule (#12).
- A double free of the `GError` when `arv_camera_new` returns null.

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
