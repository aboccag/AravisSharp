using System;
using System.IO;
using System.Linq;
using System.Text;
using AravisSharp;
using AravisSharp.Native;
using AravisSharp.Utilities;
using AravisBuffer = AravisSharp.Buffer;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Error reporting, ownership and disposal of the Camera, Stream, Buffer and Device
/// wrappers, against the test camera (the fake camera unless ARAVIS_TEST_DEVICE_ID is set).
/// </summary>
public class CameraLifecycleTests
{
    // An ID no interface can match. The GigE interface also tries a device ID as a host
    // name: spaces make that lookup fail locally instead of reaching the network.
    private const string MissingDeviceId = "AravisSharp test: no such device";

    #region GError

    [NativeFact]
    public void GetIntegerFeature_UnknownFeature_ShouldThrowAravisException()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();

        var ex = Assert.Throws<AravisException>(() => camera.GetIntegerFeature("NoSuchFeature"));

        Assert.Contains("NoSuchFeature", ex.Message);
    }

    [NativeFact]
    public void SetStringFeature_UnknownFeature_ShouldThrowAravisException()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();

        Assert.Throws<AravisException>(() => camera.SetStringFeature("NoSuchFeature", "value"));
    }

    [NativeFact]
    public void CameraNew_WithUnknownDeviceId_ShouldThrowAravisException()
    {
        Assert.Throws<AravisException>(() => new Camera(MissingDeviceId));
    }

    #endregion

    #region Dispose

    [NativeFact]
    public void Camera_DisposeTwice_ShouldNotThrow_AndUseAfterDisposeShouldThrow()
    {
        var camera = CameraTestHelpers.OpenTestCamera();

        camera.Dispose();
        camera.Dispose();

        Assert.Throws<ObjectDisposedException>(() => camera.GetModelName());
        Assert.Throws<ObjectDisposedException>(() => camera.GetIntegerFeature("Width"));
        Assert.Throws<ObjectDisposedException>(() => camera.CreateStream());
        Assert.Throws<ObjectDisposedException>(() => camera.GetDevice());
    }

    [NativeFact]
    public void Stream_DisposeTwice_ShouldNotThrow_AndUseAfterDisposeShouldThrow()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var stream = camera.CreateStream();

        stream.Dispose();
        stream.Dispose();

        using var buffer = new AravisBuffer((int)camera.GetPayloadSize());
        Assert.Throws<ObjectDisposedException>(() => stream.PushBuffer(buffer));
        Assert.Throws<ObjectDisposedException>(() => stream.TryPopBuffer());
        Assert.Throws<ObjectDisposedException>(() => stream.GetStatistics());
    }

    [NativeFact]
    public void Buffer_DisposeTwice_ShouldNotThrow_AndUseAfterDisposeShouldThrow()
    {
        var buffer = new AravisBuffer(1024);

        buffer.Dispose();
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.Status);
        Assert.Throws<ObjectDisposedException>(() => buffer.CopyData());
    }

    [NativeFact]
    public void Stream_Dispose_WithQueuedBuffers_ShouldNotThrow()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var payload = (int)camera.GetPayloadSize();
        var stream = camera.CreateStream();
        try
        {
            for (int i = 0; i < 3; i++)
                stream.PushBuffer(new AravisBuffer(payload));

            AssertInputBuffers(3, stream);
        }
        finally
        {
            // Always released: a leaked USB3 Vision stream keeps the device open, and
            // WinUSB then hides the camera from every later discovery in this process.
            stream.Dispose();
        }
    }

    #endregion

    #region Buffer ownership

    [NativeFact]
    public void PushBuffer_ShouldTransferOwnership_ToTheStream()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();
        var buffer = new AravisBuffer((int)camera.GetPayloadSize());

        stream.PushBuffer(buffer);

        // The wrapper no longer owns the native buffer: using it must fail cleanly.
        Assert.Throws<ObjectDisposedException>(() => buffer.Status);
        Assert.Throws<ObjectDisposedException>(() => buffer.GetDataSpan().Length);
        Assert.Throws<ObjectDisposedException>(() => stream.PushBuffer(buffer));

        // Disposing it (twice) must not release the buffer the stream now owns.
        buffer.Dispose();
        buffer.Dispose();
        AssertInputBuffers(1, stream);
    }

    /// <summary>
    /// The input count is a GLib queue length, minus the stream threads waiting on the queue.
    /// On a real camera the receive thread may also have taken buffers to prepare transfers
    /// (USB3 Vision: 1 pushed buffer reads -1), so only the fake camera gives an exact count.
    /// </summary>
    private static void AssertInputBuffers(int queued, AravisSharp.Stream stream)
    {
        var input = stream.GetBufferCounts().InputBuffers;
        if (CameraTestHelpers.UsesFakeCamera)
            Assert.Equal(queued, input);
        else
            Assert.True(input <= queued, $"{input} input buffers reported, {queued} pushed");
    }

    #endregion

    #region Acquisition helpers

    [NativeFact]
    public void AcquireSingleFrame_ShouldReturnSuccessBufferOfPayloadSize()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var payload = camera.GetPayloadSize();

        using var buffer = camera.AcquireSingleFrame();

        Assert.Equal(ArvBufferStatus.Success, buffer.Status);
        Assert.Equal((int)payload, buffer.GetData().Size);

        var (_, _, width, height) = camera.GetRegion();
        Assert.Equal(width, buffer.Width);
        Assert.Equal(height, buffer.Height);
    }

    [NativeFact]
    public void GetAvailablePixelFormats_ShouldBeNonEmpty_AndStableAcrossCalls()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();

        var first = camera.GetAvailablePixelFormats();
        var second = camera.GetAvailablePixelFormats();

        Assert.NotEmpty(first);
        Assert.All(first, f => Assert.False(string.IsNullOrWhiteSpace(f)));
        Assert.Equal(first, second);
        Assert.Contains(camera.GetPixelFormat(), first);

        if (CameraTestHelpers.UsesFakeCamera)
        {
            // arv-fake-camera.xml: PixelFormat enumeration entries
            Assert.Equal(
                new[] { "BayerBG8", "BayerGB8", "BayerGR8", "BayerRG8", "Mono8", "RGB8", "Mono16" }.OrderBy(f => f),
                first.OrderBy(f => f));
        }
    }

    #endregion

    #region Device lifetime

    [NativeFact]
    public void Device_ShouldStayUsable_AfterCameraIsDisposed()
    {
        Device device;
        long widthFromCamera;
        using (var camera = CameraTestHelpers.OpenTestCamera())
        {
            widthFromCamera = camera.GetIntegerFeature("Width");
            device = camera.GetDevice();
        }

        using (device)
        {
            Assert.Equal(widthFromCamera, device.GetIntegerFeature("Width"));
            Assert.False(string.IsNullOrEmpty(device.GetStringFeature("PixelFormat")));
            Assert.NotNull(device.NodeMap.GetFeatureDetails("Width"));
        }
    }

    [NativeFact]
    public void Device_DisposeTwice_ShouldNotThrow_AndUseAfterDisposeShouldThrow()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();

        device.Dispose();
        device.Dispose();

        Assert.Throws<ObjectDisposedException>(() => device.GetIntegerFeature("Width"));
        Assert.Throws<ObjectDisposedException>(() => device.NodeMap);

        // The camera keeps its own reference on the device.
        Assert.True(camera.GetIntegerFeature("Width") > 0);
    }

    #endregion

    #region Stream GigE settings

    [NonGigECameraFact]
    public void Stream_GigESetters_OnNonGigEStream_ShouldThrowInvalidOperationException()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        Assert.False(camera.IsGigEVisionDevice());
        using var stream = camera.CreateStream();

        Assert.Throws<InvalidOperationException>(() => stream.SetSocketBufferPolicy(ArvGvStreamSocketBuffer.Auto));
        Assert.Throws<InvalidOperationException>(() => stream.SetSocketBufferSize(1024 * 1024));
        Assert.Throws<InvalidOperationException>(() => stream.GetSocketBufferSize());
        Assert.Throws<InvalidOperationException>(() => stream.SetPacketResend(ArvGvStreamPacketResend.Always));
        Assert.Throws<InvalidOperationException>(() => stream.SetInitialPacketTimeout(1_000_000));
        Assert.Throws<InvalidOperationException>(() => stream.SetPacketTimeout(40_000));
        Assert.Throws<InvalidOperationException>(() => stream.SetFrameRetention(100_000));
        Assert.Throws<InvalidOperationException>(() => stream.GetPacketResend());
        Assert.Throws<InvalidOperationException>(() => stream.GetInitialPacketTimeout());
        Assert.Throws<InvalidOperationException>(() => stream.GetPacketTimeout());
        Assert.Throws<InvalidOperationException>(() => stream.GetFrameRetention());
        Assert.Throws<InvalidOperationException>(() => stream.ConfigureGigEDefaults());
        Assert.Throws<InvalidOperationException>(() => stream.GetGigEStatistics());

        // The stream is still usable afterwards.
        AssertInputBuffers(0, stream);
        Assert.Equal(0, stream.GetBufferCounts().OutputBuffers);
    }

    #endregion

    #region Image export

    [NativeFact]
    public void SaveToPgm_Mono8Frame_ShouldWriteHeaderAndPixels()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        if (!camera.GetAvailablePixelFormats().Contains("Mono8"))
        {
            // The fake camera has Mono8 (its default); a colour-only real camera may not.
            Assert.False(CameraTestHelpers.UsesFakeCamera, "The fake camera should support Mono8.");
            return;
        }
        camera.SetPixelFormat("Mono8");

        var path = Path.Combine(Path.GetTempPath(), $"aravis_test_{Guid.NewGuid():N}.pgm");
        try
        {
            int width, height;
            byte[] pixels;
            using (var buffer = camera.AcquireSingleFrame())
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                width = buffer.Width;
                height = buffer.Height;
                pixels = buffer.CopyData();

                ImageHelper.SaveToPgm(buffer, path);
            }

            if (CameraTestHelpers.UsesFakeCamera)
            {
                // arvfakecamera.h: 512 x 512 by default
                Assert.Equal(512, width);
                Assert.Equal(512, height);
            }

            var header = Encoding.ASCII.GetBytes($"P5\n{width} {height}\n255\n");
            var file = File.ReadAllBytes(path);

            Assert.Equal(header.Length + width * height, file.Length);
            Assert.Equal(header, file.AsSpan(0, header.Length).ToArray());
            if (pixels.Length == width * height) // no padding: the pixels are written as-is
                Assert.True(file.AsSpan(header.Length).SequenceEqual(pixels));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    #endregion
}
