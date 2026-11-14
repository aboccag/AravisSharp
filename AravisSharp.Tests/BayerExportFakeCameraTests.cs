using System;
using System.IO;
using System.Linq;
using AravisSharp.Native;
using AravisSharp.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace AravisSharp.Tests;

/// <summary>
/// PNG and JPEG export of a Bayer 8 frame (issue #6, AC-5), on the test camera: BayerBG8 on
/// the fake camera, the first Bayer 8 format offered by the device named by
/// ARAVIS_TEST_DEVICE_ID. The class name contains
/// "FakeCameraTests" so that a <c>FullyQualifiedName~FakeCameraTests</c> filter runs it.
/// The pixel format is restored on dispose.
/// </summary>
[Collection("CameraAcquisition")]
public class BayerExportFakeCameraTests : IDisposable
{
    private static readonly (string Name, uint Value)[] BayerFormats =
    {
        ("BayerRG8", ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8),
        ("BayerGR8", ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8),
        ("BayerGB8", ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GB_8),
        ("BayerBG8", ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8),
    };

    private readonly Camera _camera;
    private readonly string _originalPixelFormat;
    private readonly ITestOutputHelper _output;

    // Open and configure failures propagate: a camera that cannot be set up must fail the
    // test, not turn it into a silent pass.
    public BayerExportFakeCameraTests(ITestOutputHelper output)
    {
        _output = output;
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
    public void Bayer8_SaveToPngAndJpeg_ShouldWriteColourImagesOfTheBufferSize()
    {
        // The fake camera must offer BayerBG8 (arv-fake-camera.xml). A real colour camera
        // often offers another Bayer 8 pattern (BayerRG8 on many Basler models): test the
        // first one it offers.
        var available = _camera.GetAvailablePixelFormats();
        var candidates = CameraTestHelpers.UsesFakeCamera
            ? BayerFormats.Where(f => f.Name == "BayerBG8")
            : BayerFormats;
        var format = candidates.FirstOrDefault(f => available.Contains(f.Name));
        if (format.Name is null)
        {
            // Missing on the fake camera is a failure. A real camera without any Bayer 8
            // format (a mono camera) has nothing to test; say so, xUnit 2 cannot skip here.
            Assert.False(CameraTestHelpers.UsesFakeCamera, "The fake camera does not offer BayerBG8.");
            _output.WriteLine("Not run: the camera offers no Bayer 8 pixel format.");
            return;
        }

        _output.WriteLine($"Testing {format.Name}.");
        _camera.SetPixelFormat(format.Name);
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
                    Assert.Equal(format.Value, buffer.PixelFormat);
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
