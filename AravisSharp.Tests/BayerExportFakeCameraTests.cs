using System;
using System.IO;
using System.Linq;
using AravisSharp.Native;
using AravisSharp.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// PNG and JPEG export of a BayerBG8 frame (issue #6, AC-5), on the test camera: the fake
/// camera, or the device named by ARAVIS_TEST_DEVICE_ID. The class name contains
/// "FakeCameraTests" so that a <c>FullyQualifiedName~FakeCameraTests</c> filter runs it.
/// The pixel format is restored on dispose.
/// </summary>
[Collection("CameraAcquisition")]
public class BayerExportFakeCameraTests : IDisposable
{
    private const string BayerFormat = "BayerBG8";

    private readonly Camera _camera;
    private readonly string _originalPixelFormat;

    // Open and configure failures propagate: a camera that cannot be set up must fail the
    // test, not turn it into a silent pass.
    public BayerExportFakeCameraTests()
    {
        _camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            CameraTestHelpers.ConfigureForAcquisition(_camera);
            _originalPixelFormat = _camera.GetPixelFormat();
        }
        catch
        {
            _camera.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_camera.GetPixelFormat() != _originalPixelFormat)
                _camera.SetPixelFormat(_originalPixelFormat);
        }
        finally
        {
            _camera.Dispose();
        }
    }

    [NativeFact]
    public void BayerBG8_SaveToPngAndJpeg_ShouldWriteColourImagesOfTheBufferSize()
    {
        if (!_camera.GetAvailablePixelFormats().Contains(BayerFormat))
        {
            // The fake camera offers BayerBG8 (arv-fake-camera.xml): missing there is a
            // failure. A real camera without it (a mono camera) has nothing to test.
            Assert.False(CameraTestHelpers.UsesFakeCamera, $"The fake camera does not offer {BayerFormat}.");
            return;
        }

        _camera.SetPixelFormat(BayerFormat);
        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        var payloadSize = _camera.GetPayloadSize();
        using var stream = _camera.CreateStream();
        for (int i = 0; i < 3; i++)
            stream.PushBuffer(new Buffer((int)payloadSize));

        string baseName = Path.Combine(Path.GetTempPath(), $"aravis_bayer_{Guid.NewGuid():N}");
        string png = baseName + ".png", jpeg = baseName + ".jpg";
        try
        {
            int width, height;
            byte[] expectedRgb;
            _camera.StartAcquisition();
            try
            {
                var buffer = stream.PopBuffer(timeoutMs: 5000);
                Assert.NotNull(buffer);
                using (buffer)
                {
                    Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                    Assert.Equal(ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8, buffer.PixelFormat);
                    width = buffer.Width;
                    height = buffer.Height;

                    ImageHelper.SaveToPng(buffer, png);
                    ImageHelper.SaveToJpeg(buffer, jpeg);

                    // Reference: the demosaic of this very buffer, rows unpadded.
                    expectedRgb = ImageHelper.DemosaicBayer8(PackRows(buffer), width, height, buffer.PixelFormat);
                }
            }
            finally
            {
                _camera.StopAcquisition();
            }

            // Non-generic Load decodes to the file's own pixel type: L8 for a greyscale
            // file, Rgb24 for a colour one.
            using (var image = Image.Load(png))
            {
                var rgb = Assert.IsType<Image<Rgb24>>(image);
                Assert.Equal(width, rgb.Width);
                Assert.Equal(height, rgb.Height);

                // PNG is lossless: the file holds exactly the demosaiced pixels, in RGB order.
                var actual = new byte[expectedRgb.Length];
                rgb.CopyPixelDataTo(actual);
                int firstDifference = actual.AsSpan().CommonPrefixLength(expectedRgb);
                if (firstDifference == actual.Length)
                    firstDifference = -1;
                Assert.True(firstDifference < 0, firstDifference < 0 ? "" :
                    $"PNG differs from DemosaicBayer8 at pixel {firstDifference / 3}, channel {firstDifference % 3}: " +
                    $"{actual[firstDifference]} instead of {expectedRgb[firstDifference]}");
            }

            using (var image = Image.Load(jpeg))
            {
                var rgb = Assert.IsType<Image<Rgb24>>(image);
                Assert.Equal(width, rgb.Width);
                Assert.Equal(height, rgb.Height);
            }
        }
        finally
        {
            File.Delete(png);
            File.Delete(jpeg);
        }
    }

    private static byte[] PackRows(Buffer buffer)
    {
        int width = buffer.Width, height = buffer.Height;
        var (xPadding, _) = buffer.GetImagePadding();
        var data = buffer.GetDataSpan();
        var packed = new byte[width * height];
        for (int y = 0; y < height; y++)
            data.Slice(y * (width + xPadding), width).CopyTo(packed.AsSpan(y * width));
        GC.KeepAlive(buffer);
        return packed;
    }
}
