# Spec: PNG and JPEG export of 8-bit Bayer buffers

Issue #6, level standard, type feature. Written on 2026-11-14 against `main` at
`5c6ce4d`, before any code. Source material: the spec, code and tests of the branch
`fix/native-leak-and-stream-pop` (not merged, built on an older base). `NON VÉRIFIÉ` marks
what was not checked.

## Premises checked on `5c6ce4d`

- `ImageHelper.SaveToPng` rejects every Bayer format: its guard `IsImageSharpFormat` is
  `IsMonoFormat || IsColorFormat`, and neither includes Bayer
  (`AravisSharp/Utilities/ImageHelper.cs:58-59`, `:88-91`). `SaveToJpeg` accepts only
  `Mono8` and `RGB8` (`:72-74`). Observed on 13.11.2026 on the Basler USB3 colour camera,
  whose default format is `BayerBG8`: `AcquireAndSavePng_ShouldCreateValidFile` fails.
- `LoadImage` copies the pixels through `GetPackedPixels`, which removes row padding and
  checks the buffer holds a whole image (`:97-185`). The old branch read the raw span and
  ignored padding; this path does not.
- Every ImageSharp image is already disposed (`using var` at `:61`, `:76`). The disposal
  part of the issue is done on `main`.
- `GetBytesPerPixel` already returns 1 for the four Bayer 8 formats and `IsBayerFormat`
  exists (`:225-228`, `:251-257`).
- The Fake camera offers `BayerBG8`, `BayerGB8`, `BayerGR8` and `BayerRG8`
  (`AravisSharp.Tests/CameraLifecycleTests.cs:191`).
- No test of `main` saves a Bayer buffer. `AcquireAndSavePng_ShouldCreateValidFile`
  (`AravisSharp.Tests/ImageAcquisitionTests.cs:142-181`) saves whatever format the opened
  camera has, `Mono8` on the Fake camera.

## Definitions

*Bayer 8*: one byte per pixel, the colour given by the 2 x 2 cell; red at (x, y) = (0, 0)
for RG, (1, 0) for GR, (0, 1) for GB, (1, 1) for BG (GenICam PFNC naming).
*Bilinear*: each missing channel is the rounded mean of the pixels carrying it in the
3 x 3 window, clipped at the image border; the pixel's own channel is kept as measured.

## Inputs and outputs

`public static byte[] ImageHelper.DemosaicBayer8(ReadOnlySpan<byte> data, int width, int
height, uint pixelFormat)` takes packed pixels (no row padding) and returns a new
`byte[width * height * 3]`, R, G, B. `SaveToPng` and `SaveToJpeg` write an RGB file for a
Bayer 8 buffer, going through `GetPackedPixels`.

## Acceptance criteria

| # | Must happen | Must NOT happen |
|---|---|---|
| AC-1 | A uniform scene mosaicked with each of the four patterns is recovered exactly; output length `width * height * 3`. | Red and blue swapped; the pattern shifted by one pixel. |
| AC-2 | On a random mosaic, every pixel's own channel equals its input byte. | A measured sample altered. |
| AC-3 | Non-Bayer-8 format: `NotSupportedException`. `width <= 0` or `height <= 0`: `ArgumentOutOfRangeException` naming the faulty parameter. `data` shorter than `width * height`: `ArgumentException`. | A read past `data`; a partial image; an overflow of `width * height * 3`. |
| AC-4 | Images one pixel wide or one pixel high: no exception; a channel absent from the window is 0, as documented. | An exception or an out-of-range read. |
| AC-5 | On the Fake camera set to `BayerBG8`, `SaveToPng` and `SaveToJpeg` write files that ImageSharp loads back as colour images with the buffer's width and height. | `NotSupportedException`; a greyscale file. |
| AC-6 | `AcquireAndSavePng_ShouldCreateValidFile` passes on the Basler camera (`ARAVIS_TEST_DEVICE_ID` set, one run, nobody else on the camera). | — |
| AC-7 | The whole suite passes on the Fake camera on net8.0 and net10.0; new tests open a camera only through `CameraTestHelpers` and restore the pixel format they change. | A test that depends on a physical camera to pass in CI. |

## Public API changes

New `ImageHelper.DemosaicBayer8`. `SaveToPng` and `SaveToJpeg` accept the four Bayer 8
formats instead of throwing. Their XML summaries list the formats they accept and say that
Bayer is demosaiced bilinearly, with no performance budget.

## Risks

Speed unmeasured: about nine reads per pixel in managed code, some 45 million for a 5 MP
frame (`NON VÉRIFIÉ`). One extra `width * height * 3` allocation per save, on top of the
packed copy when the buffer has row padding.

## Open questions

The old branch passed gate 1 on 14.11.2026 with three rulings. They still apply here and
are proposed again as recommendations:

1. **Performance budget.** Recommendation: none, the helper is for diagnostics; say so in
   the XML documentation.
2. **Signature.** Recommendation: `DemosaicBayer8` returns `byte[]`; a `Span<byte>`
   overload waits until a caller needs it.
3. **JPEG formats.** `SaveToJpeg` accepts only `Mono8` and `RGB8` although `LoadImage`
   handles more (Mono10 to Mono16, BGR8, RGBA8, BGRA8). (a) Add only the Bayer 8 formats,
   as the issue asks. (b) Align `SaveToJpeg` on `SaveToPng`'s accepted formats plus Bayer.
   Recommendation: (a); (b) changes behaviour the issue does not cover and would need its
   own tests.

## Plan

1. Port the unit tests of `DemosaicBayer8` from the old branch
   (`InteropRegressionTests.cs`) into a new `AravisSharp.Tests/BayerDemosaicTests.cs`,
   completed for AC-2 to AC-4 (red: does not compile), then port `DemosaicBayer8` itself.
2. A Fake camera test in a new `AravisSharp.Tests/BayerExportFakeCameraTests.cs`, ported
   from the old branch, for AC-5: red with `NotSupportedException`, then the Bayer branches
   in `LoadImage`, `SaveToPng` and `SaveToJpeg`.
3. XML documentation; the test table of `AravisSharp.Tests/README.md`.
4. Whole suite on both frameworks; then the Basler run for AC-6, alone on the camera.
5. OCR review and C# review on the diff.
