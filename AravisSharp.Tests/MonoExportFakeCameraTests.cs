using AravisSharp.Native;
using AravisSharp.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit.Abstractions;

namespace AravisSharp.Tests;

/// <summary>
/// PNG export of a Mono16 frame (issue #13, B10): each sample is scaled to 8 bits. The fake
/// camera offers Mono16 (aravis/src/arv-fake-camera.xml) and fills it with a ramp over the
/// whole 16-bit range (aravis/src/arvfakecamera.c). A device named by ARAVIS_TEST_DEVICE_ID
/// that offers no Mono16 has nothing to test. The class name contains "FakeCameraTests" so
/// that a <c>FullyQualifiedName~FakeCameraTests</c> filter runs it. The pixel format is
/// restored on dispose.
/// </summary>
[Collection("CameraAcquisition")]
public class MonoExportFakeCameraTests : IDisposable
{
    private readonly Camera _camera;
    private readonly string _originalPixelFormat;
    private readonly ITestOutputHelper _output;

    // Open and configure failures propagate: a camera that cannot be set up must fail the
    // test, not turn it into a silent pass.
    public MonoExportFakeCameraTests(ITestOutputHelper output)
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
    public void Mono16_SaveToPng_ShouldHoldEachSampleScaledTo8Bits()
    {
        if (!_camera.GetAvailablePixelFormats().Contains("Mono16"))
        {
            // Missing on the fake camera is a failure; xUnit 2 cannot skip at run time.
            Assert.False(CameraTestHelpers.UsesFakeCamera, "The fake camera does not offer Mono16.");
            _output.WriteLine("Not run: the camera offers no Mono16 pixel format.");
            return;
        }

        _camera.SetPixelFormat("Mono16");
        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        var payloadSize = _camera.GetPayloadSize();
        using var stream = _camera.CreateStream();
        for (int i = 0; i < 3; i++)
            stream.PushBuffer(new Buffer((int)payloadSize));

        string png = Path.Combine(Path.GetTempPath(), $"aravis_mono16_{Guid.NewGuid():N}.png");
        try
        {
            int width, height;
            byte[] expected;
            _camera.StartAcquisition();
            try
            {
                var buffer = stream.PopBuffer(timeoutMs: 5000);
                Assert.NotNull(buffer);
                using (buffer)
                {
                    Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                    Assert.Equal(ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16, buffer.PixelFormat);
                    width = buffer.Width;
                    height = buffer.Height;

                    ImageHelper.SaveToPng(buffer, png);
                    expected = ExpectedL8(buffer);
                }
            }
            finally
            {
                _camera.StopAcquisition();
            }

            // A flat frame would prove little: the ramp must cover many 8-bit levels.
            Assert.True(expected.Distinct().Count() > 16, "the Mono16 frame is nearly flat");

            using var image = Image.Load(png);
            var grey = Assert.IsType<Image<L8>>(image);
            Assert.Equal(width, grey.Width);
            Assert.Equal(height, grey.Height);

            var actual = new byte[expected.Length];
            grey.CopyPixelDataTo(actual);
            int firstDifference = actual.AsSpan().CommonPrefixLength(expected);
            if (firstDifference == actual.Length)
                firstDifference = -1;
            Assert.True(firstDifference < 0, firstDifference < 0 ? "" :
                $"PNG differs at pixel {firstDifference} (x {firstDifference % width}, y {firstDifference / width}): " +
                $"{actual[firstDifference]} instead of {expected[firstDifference]}");
        }
        finally
        {
            File.Delete(png);
        }
    }

    /// <summary>
    /// The 8-bit value of each Mono16 sample: little-endian 16-bit, scaled by 255 / 65535 and
    /// truncated, rows unpadded.
    /// </summary>
    private static byte[] ExpectedL8(Buffer buffer)
    {
        int width = buffer.Width, height = buffer.Height;
        var (xPadding, _) = buffer.GetImagePadding();
        var data = buffer.GetDataSpan();
        int stride = width * 2 + xPadding;
        var expected = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = y * stride + 2 * x;
                int sample = data[offset] | (data[offset + 1] << 8);
                expected[y * width + x] = (byte)(sample * 255 / 65535);
            }
        }
        GC.KeepAlive(buffer);
        return expected;
    }
}
