using System;
using AravisSharp.Native;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// #10 AC-1 and AC-2: the GigE Vision members of <see cref="Camera"/> and <see cref="Stream"/>
/// throw <see cref="NotSupportedException"/> on a camera or stream that is not GigE Vision,
/// before calling Aravis (which would log a GLib CRITICAL and return 0).
/// Runs on the fake camera, or on a USB3 Vision camera named by ARAVIS_TEST_DEVICE_ID.
/// </summary>
public class GigEGuardTests
{
    [NonGigECameraFact]
    public void Stream_IsGigEVision_OnNonGigEStream_ShouldBeFalse()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();

        Assert.False(stream.IsGigEVision);
    }

    [NonGigECameraFact]
    public void Stream_IsGigEVision_AfterDispose_ShouldThrowObjectDisposedException()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var stream = camera.CreateStream();
        stream.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.IsGigEVision);
    }

    [NonGigECameraFact]
    public void Camera_GigEMembers_OnNonGigECamera_ShouldThrowNotSupportedException()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        Assert.False(camera.IsGigEVisionDevice());

        var calls = new (string Name, Action Call)[]
        {
            (nameof(Camera.GvAutoPacketSize), () => camera.GvAutoPacketSize()),
            (nameof(Camera.GvGetPacketSize), () => camera.GvGetPacketSize()),
            (nameof(Camera.GvSetPacketSize), () => camera.GvSetPacketSize(1500)),
            (nameof(Camera.GvGetIpConfigurationMode), () => camera.GvGetIpConfigurationMode()),
            (nameof(Camera.GvSetIpConfigurationMode), () => camera.GvSetIpConfigurationMode(ArvGvIpConfigurationMode.Dhcp)),
            (nameof(Camera.GvGetPersistentIp), () => camera.GvGetPersistentIp()),
            (nameof(Camera.GvSetPersistentIp), () => camera.GvSetPersistentIp("192.168.1.100", "255.255.255.0", null)),
            (nameof(Camera.GvGetNetworkInterfaceCount), () => camera.GvGetNetworkInterfaceCount()),
            (nameof(Camera.GvGetPacketDelay), () => camera.GvGetPacketDelay()),
            (nameof(Camera.GvSetPacketDelay), () => camera.GvSetPacketDelay(1000)),
            (nameof(Camera.GvGetStreamChannelCount), () => camera.GvGetStreamChannelCount()),
        };

        Assert.All(calls, c =>
        {
            var ex = Assert.Throws<NotSupportedException>(c.Call);
            Assert.Contains(c.Name, ex.Message);
            Assert.Contains("requires a GigE Vision camera", ex.Message);
        });

        // The camera is still usable afterwards.
        Assert.True(camera.GetIntegerFeature("Width") > 0);
    }

    [NonGigECameraFact]
    public void Stream_GigEMembers_ShouldUseTheSameMessagePatternAsCamera()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();

        var ex = Assert.Throws<NotSupportedException>(() => stream.GetPacketTimeout());

        Assert.Contains(nameof(Stream.GetPacketTimeout), ex.Message);
        Assert.Contains("requires a GigE Vision stream", ex.Message);
    }

    [NativeFact]
    public void Camera_GigEMembers_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        camera.Dispose();

        // Disposal is checked before the GigE guard.
        Assert.Throws<ObjectDisposedException>(() => camera.GvGetPacketSize());
    }
}
