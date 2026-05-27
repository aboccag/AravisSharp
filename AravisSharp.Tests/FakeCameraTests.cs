using System;
using System.Linq;
using AravisSharp;
using AravisSharp.Native;
using AravisBuffer = AravisSharp.Buffer;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Integration-style unit tests against the Aravis fake interface ("Fake").
/// </summary>
public class FakeCameraTests
{
    private static CameraInfo GetFakeCameraInfo()
    {
        var cameras = CameraDiscovery.DiscoverCameras();
        var fake = cameras.FirstOrDefault(c =>
            string.Equals(c.Protocol, "Fake", StringComparison.OrdinalIgnoreCase) ||
            c.DeviceId.StartsWith("Fake_", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Model, "Fake", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(fake);
        return fake!;
    }

    [Fact]
    public void FakeInterface_ShouldExposeAtLeastOneCamera()
    {
        var cameras = CameraDiscovery.DiscoverCameras();

        Assert.NotEmpty(cameras);
        Assert.Contains(cameras, c =>
            string.Equals(c.Protocol, "Fake", StringComparison.OrdinalIgnoreCase) ||
            c.DeviceId.StartsWith("Fake_", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Model, "Fake", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FakeCamera_ShouldConnectAndExposeCoreMetadata()
    {
        var fake = GetFakeCameraInfo();

        using var camera = new Camera(fake.DeviceId);

        Assert.False(string.IsNullOrWhiteSpace(camera.GetDeviceId()));
        Assert.False(string.IsNullOrWhiteSpace(camera.GetModelName()));
        Assert.False(string.IsNullOrWhiteSpace(camera.GetVendorName()));
        Assert.False(string.IsNullOrWhiteSpace(camera.GetSerialNumber()));
    }

    [Fact]
    public void FakeCamera_ShouldReadAndWriteCoreParameters()
    {
        var fake = GetFakeCameraInfo();

        using var camera = new Camera(fake.DeviceId);

        var (minWidth, maxWidth) = camera.GetWidthBounds();
        Assert.True(minWidth > 0);
        Assert.True(maxWidth >= minWidth);

        var widthToSet = maxWidth > minWidth ? minWidth + 1 : minWidth;
        camera.SetIntegerFeature("Width", widthToSet);
        Assert.Equal(widthToSet, camera.GetIntegerFeature("Width"));

        var (sensorWidth, sensorHeight) = camera.GetSensorSize();
        Assert.True(sensorWidth > 0);
        Assert.True(sensorHeight > 0);

        Assert.True(camera.IsFeatureAvailable("PixelFormat"));
        Assert.False(string.IsNullOrWhiteSpace(camera.GetStringFeature("PixelFormat")));
    }

    [Fact]
    public void FakeCamera_ShouldAcquireImageBuffer()
    {
        var fake = GetFakeCameraInfo();

        using var camera = new Camera(fake.DeviceId);
        using var stream = camera.CreateStream();

        uint payload = camera.GetPayloadSize();
        Assert.True(payload > 0);

        using var inputBuffer = new AravisBuffer((IntPtr)payload);
        stream.PushBuffer(inputBuffer);

        camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);

        AravisBuffer? outputBuffer = null;
        try
        {
            camera.StartAcquisition();
            outputBuffer = stream.PopBuffer(2000);
        }
        finally
        {
            camera.StopAcquisition();
        }

        Assert.NotNull(outputBuffer);
        Assert.Equal(ArvBufferStatus.Success, outputBuffer!.Status);
        Assert.True(outputBuffer.Width > 0);
        Assert.True(outputBuffer.Height > 0);
        Assert.NotEmpty(outputBuffer.CopyData());
        outputBuffer.Dispose();
    }
}
