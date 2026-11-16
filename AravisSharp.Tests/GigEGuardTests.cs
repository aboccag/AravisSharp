using System;
using System.Linq;
using System.Reflection;
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
    /// <summary>
    /// Every GigE member of <see cref="Camera"/>, with a call that reaches its guard.
    /// <see cref="Camera_GvMembers_ShouldAllBeInTheGuardTable"/> keeps it complete.
    /// </summary>
    internal static readonly (string Name, Action<Camera> Call)[] CameraGigEMembers =
    {
        (nameof(Camera.GvAutoPacketSize), c => c.GvAutoPacketSize()),
        (nameof(Camera.GvGetPacketSize), c => c.GvGetPacketSize()),
        (nameof(Camera.GvSetPacketSize), c => c.GvSetPacketSize(1500)),
        (nameof(Camera.GvGetIpConfigurationMode), c => c.GvGetIpConfigurationMode()),
        (nameof(Camera.GvSetIpConfigurationMode), c => c.GvSetIpConfigurationMode(ArvGvIpConfigurationMode.Dhcp)),
        (nameof(Camera.GvGetPersistentIp), c => c.GvGetPersistentIp()),
        (nameof(Camera.GvSetPersistentIp), c => c.GvSetPersistentIp("192.168.1.100", "255.255.255.0", null)),
        (nameof(Camera.GvGetNetworkInterfaceCount), c => c.GvGetNetworkInterfaceCount()),
        (nameof(Camera.GvGetPacketDelay), c => c.GvGetPacketDelay()),
        (nameof(Camera.GvSetPacketDelay), c => c.GvSetPacketDelay(1000)),
        (nameof(Camera.GvGetStreamChannelCount), c => c.GvGetStreamChannelCount()),
    };

    /// <summary>
    /// Every GigE member of <see cref="Stream"/>, with a call that reaches its guard.
    /// With <see cref="StreamNonGigEMembers"/> it lists every public member of
    /// <see cref="Stream"/>: <see cref="Stream_PublicMembers_ShouldEachBeClassifiedGigEOrNot"/>
    /// fails on a new one until it is put in one list or the other.
    /// </summary>
    internal static readonly (string Name, Action<Stream> Call)[] StreamGigEMembers =
    {
        (nameof(Stream.SetSocketBufferPolicy), s => s.SetSocketBufferPolicy(ArvGvStreamSocketBuffer.Auto)),
        (nameof(Stream.SetSocketBufferSize), s => s.SetSocketBufferSize(1024 * 1024)),
        (nameof(Stream.GetSocketBufferSize), s => s.GetSocketBufferSize()),
        (nameof(Stream.SetPacketResend), s => s.SetPacketResend(ArvGvStreamPacketResend.Always)),
        (nameof(Stream.SetInitialPacketTimeout), s => s.SetInitialPacketTimeout(1_000_000)),
        (nameof(Stream.SetPacketTimeout), s => s.SetPacketTimeout(40_000)),
        (nameof(Stream.SetFrameRetention), s => s.SetFrameRetention(100_000)),
        (nameof(Stream.GetPacketResend), s => s.GetPacketResend()),
        (nameof(Stream.GetInitialPacketTimeout), s => s.GetInitialPacketTimeout()),
        (nameof(Stream.GetPacketTimeout), s => s.GetPacketTimeout()),
        (nameof(Stream.GetFrameRetention), s => s.GetFrameRetention()),
        (nameof(Stream.ConfigureGigEDefaults), s => s.ConfigureGigEDefaults()),
        (nameof(Stream.GetGigEStatistics), s => s.GetGigEStatistics()),
    };

    /// <summary>The public members of <see cref="Stream"/> that work on any stream.</summary>
    internal static readonly string[] StreamNonGigEMembers =
    {
        nameof(Stream.IsGigEVision),
        nameof(Stream.PushBuffer),
        nameof(Stream.TryPopBuffer),
        nameof(Stream.PopBuffer),
        nameof(Stream.GetBufferCounts),
        nameof(Stream.GetStatistics),
        nameof(Stream.Dispose),
    };

    private const BindingFlags PublicDeclared =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void Camera_GvMembers_ShouldAllBeInTheGuardTable()
    {
        var gvMethods = typeof(Camera).GetMethods(PublicDeclared)
            .Where(m => m.Name.StartsWith("Gv", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToHashSet();

        Assert.Equal(11, CameraGigEMembers.Length);
        Assert.Equal(gvMethods.OrderBy(n => n), CameraGigEMembers.Select(m => m.Name).OrderBy(n => n));
    }

    [Fact]
    public void Stream_PublicMembers_ShouldEachBeClassifiedGigEOrNot()
    {
        // Property and event accessors count through their property or event.
        var members = typeof(Stream).GetMembers(PublicDeclared)
            .Where(m => m.MemberType != MemberTypes.Constructor)
            .Where(m => m is not MethodInfo { IsSpecialName: true })
            .Select(m => m.Name)
            .ToHashSet();
        var gigE = StreamGigEMembers.Select(m => m.Name).ToHashSet();

        Assert.Equal(13, StreamGigEMembers.Length);
        Assert.Empty(gigE.Intersect(StreamNonGigEMembers));
        Assert.Equal(members.OrderBy(n => n), gigE.Concat(StreamNonGigEMembers).OrderBy(n => n));
    }

    [NonGigECameraFact]
    public void Stream_IsGigEVision_OnNonGigEStream_ShouldBeFalse()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();

        Assert.False(stream.IsGigEVision);
    }

    [NativeFact]
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

        Assert.All(CameraGigEMembers, m =>
        {
            var ex = Assert.Throws<NotSupportedException>(() => m.Call(camera));
            Assert.Contains(m.Name, ex.Message);
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
