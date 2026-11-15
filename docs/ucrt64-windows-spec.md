# Spec: build the win-x64 natives in MSYS2 UCRT64

Issue #16, level standard, type fix, zone ci. Written on 2026-11-15 against `main` at
`e6c1af0`, before any change. `NON VÉRIFIÉ` marks what was not checked.

## Premises checked on `e6c1af0`

- Every CI run on `main` since `5c6ce4d` fails in **Native (win-x64)** at the MSYS2 setup
  step: `error: target not found: mingw-w64-x86_64-libusb` (runs 43636506527 to
  43710915039). The last green run is `366455b`, 30.09.2026. Pack, both package-smoke jobs,
  release sources and publish are skipped.
- packages.msys2.org, 15.11.2026: `mingw-w64-libusb` 1.0.30 is published for `clang64`,
  `clangarm64` and `ucrt64` only. The `mingw64` environment no longer has it.
- The workflow builds in `msystem: MINGW64` with eight `mingw-w64-x86_64-*` packages and
  installs Aravis into `/mingw64` (`.github/workflows/build-and-publish.yml:81-107`).
- `/mingw64` is hardcoded in `collect-native-windows.sh:10,14-16`,
  `verify-native-windows.sh:22` and `record-native-versions.sh:62-67`, and the text
  "mingw-w64-x86_64" in `verify-native-windows.sh:10`, `record-native-versions.sh:63` and
  `THIRD-PARTY-NOTICES.md:33`. The docs name MinGW64 in `CLAUDE.md:109`,
  `WINDOWS_SETUP.md:31,131-136`, `CROSS_PLATFORM_GUIDE.md:88,91` and
  `PROJECT_SUMMARY.md:151`.
- MSYS2 sets `MINGW_PREFIX` (for example `/ucrt64`) and `MINGW_PACKAGE_PREFIX` (for example
  `mingw-w64-ucrt-x86_64`) in every MinGW-type shell. `setup-msys2` has a `pacboy` input that
  adds the environment's package prefix (`NON VÉRIFIÉ` on v2.33.0, to check when writing).
- No Windows machine is available here: the win-x64 job can only be checked in CI, which
  needs the branch pushed to `origin`.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | The win-x64 job builds Aravis 0.8.36 from the submodule in the environment decided at gate 1, and installs into that environment's prefix. | A dependency taken from another MSYS2 environment; a change to `aravis/`. |
| AC-2 | The three Windows scripts take the prefix from `MINGW_PREFIX` and fail with a clear error when it is not set; no environment path or package prefix is hardcoded in them. | A script that silently checks `/mingw64` while the build is in `/ucrt64`. |
| AC-3 | `verify-native-windows.sh` still fails closed: shown once in CI by removing one bundled DLL from `_native` before the verify step, then restored. | A verify step that passes because it looks in the wrong prefix. |
| AC-4 | `record-native-versions.sh` names the owning package of every bundled DLL, and `fetch-release-sources.sh` resolves a source URL for each. | A bundled DLL with no recorded source. |
| AC-5 | `THIRD-PARTY-NOTICES.md` lists every bundled file; the pack job's check passes. | A new DLL bundled without a notice. |
| AC-6 | A CI run of the branch is green through Native (all four), Test .NET, Pack, Package smoke (four runners) and Package smoke distros. | Publish running: `NUGET_PUBLISH` and the tag condition are unchanged. |
| AC-7 | The docs listed in the premises say UCRT64 (or the environment decided), and state that the win-x64 natives need the Universal C Runtime (part of Windows 10 and later). | A doc that still tells a contributor to use a MINGW64 shell. |

## Risks

- The payload changes: the DLLs are linked against the UCRT instead of msvcrt. The UCRT is
  part of Windows 10 and later; older Windows versions need it installed. .NET 8 already
  requires Windows 10 or later, so no supported user loses anything (`NON VÉRIFIÉ` for
  every .NET 8 Windows SKU).
- The set of bundled DLLs may change (for example a new `libgcc` or a renamed DLL). The
  collect script derives the set and the notices check catches a missing notice.
- Several CI round trips may be needed, each a push of the branch to the public repository.

## Open questions

1. **Environment.** (a) UCRT64: same GCC toolchain as today, MSYS2's recommended default.
   (b) CLANG64: Clang and libc++, a larger change of toolchain. Recommendation: (a).
2. **Prefix in the scripts.** (a) Read `MINGW_PREFIX` and use `pacboy` for the package list,
   so a future change of environment is one line of the workflow. (b) Replace `/mingw64` by
   `/ucrt64` everywhere. Recommendation: (a).
3. **Pushing the branch.** The fix can only be checked in CI. Recommendation: allow pushes of
   `fix/16-ucrt64-windows` to `origin` for CI runs during this issue; `main` stays under the
   usual confirmation.

## Plan

1. Workflow: `msystem` and the package list (questions 1 and 2), install prefix
   `$MINGW_PREFIX`.
2. The three scripts on `MINGW_PREFIX`, with a guard when it is unset.
3. Docs and `THIRD-PARTY-NOTICES.md` text.
4. Push the branch; read the CI run; fix until green (AC-6); the fail-closed check of AC-3
   once, by a temporary workflow step on the branch, removed afterwards.
5. Review of the shell and YAML by a general Opus agent (the profile's rule for CI files),
   then the notices from the payload actually built.
