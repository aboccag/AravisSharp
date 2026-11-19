using System.Reflection;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #19: the USB3 Vision members of <see cref="Camera"/> on a camera that is not USB3
/// Vision. The three that read or write the bandwidth limit throw
/// <see cref="NotSupportedException"/> before calling Aravis, which would log a CRITICAL and
/// return 0; <see cref="Camera.UvIsBandwidthControlAvailable"/> answers <c>false</c>.
/// Runs on the fake camera, or on a GigE Vision camera named by ARAVIS_TEST_DEVICE_ID.
/// </summary>
public class Usb3GuardTests
{
    // libaravis is built without G_LOG_DOMAIN: its g_return_if_fail criticals go to GLib's
    // default domain ("** (process:N): CRITICAL **: arv_camera_uv_get_bandwidth: assertion ...").
    private const string? AravisLogDomain = null;

    /// <summary>
    /// The USB3 members of <see cref="Camera"/> that throw on another camera, with a call that
    /// reaches their guard. <see cref="Camera_UvMembers_ShouldAllBeCovered"/> keeps the list complete.
    /// </summary>
    internal static readonly (string Name, Action<Camera> Call)[] ThrowingMembers =
    {
        (nameof(Camera.UvGetBandwidth), c => c.UvGetBandwidth()),
        (nameof(Camera.UvSetBandwidth), c => c.UvSetBandwidth(0)),
        (nameof(Camera.UvGetBandwidthBounds), c => c.UvGetBandwidthBounds()),
    };

    [Fact]
    public void Camera_UvMembers_ShouldAllBeCovered()
    {
        // A list, not a set: an overload added later counts as one more member to cover.
        var uvMethods = typeof(Camera)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.StartsWith("Uv", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToList();
        var covered = ThrowingMembers.Select(m => m.Name).Append(nameof(Camera.UvIsBandwidthControlAvailable));

        Assert.Equal(3, ThrowingMembers.Length);
        Assert.Equal(uvMethods.OrderBy(n => n), covered.OrderBy(n => n));
    }

    [NonUsb3CameraFact]
    public void BandwidthMembers_OnNonUsb3Camera_ShouldThrowNotSupportedException_WithoutCallingAravis()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        Assert.False(camera.IsUSB3VisionDevice());
        using var log = new GObjectLogCapture(AravisLogDomain);

        Assert.All(ThrowingMembers, m =>
        {
            var ex = Assert.Throws<NotSupportedException>(() => m.Call(camera));
            Assert.Contains(m.Name, ex.Message);
            Assert.Contains("requires a USB3 Vision camera", ex.Message);
            Assert.Contains(nameof(Camera.IsUSB3VisionDevice), ex.Message);
        });

        Assert.True(log.Messages.Count == 0, "Aravis logged: " + string.Join(" | ", log.Messages));
        // The camera is still usable afterwards.
        Assert.True(camera.GetIntegerFeature("Width") > 0);
    }

    [NonUsb3CameraFact]
    public void UvIsBandwidthControlAvailable_OnNonUsb3Camera_ShouldBeFalse_WithoutCallingAravis()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        Assert.False(camera.IsUSB3VisionDevice());
        using var log = new GObjectLogCapture(AravisLogDomain);

        bool available = camera.UvIsBandwidthControlAvailable();

        Assert.False(available);
        Assert.True(log.Messages.Count == 0, "Aravis logged: " + string.Join(" | ", log.Messages));
    }

    // Proves the two "Aravis logged nothing" assertions above can fail: the native call the
    // guard prevents does log a critical, and the capture sees it.
    [NonUsb3CameraFact]
    public void TheNativeCallTheGuardPrevents_ShouldLogACriticalThatTheCaptureSees()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var log = new GObjectLogCapture(AravisLogDomain);

        uint bandwidth = AravisNative.arv_camera_uv_get_bandwidth(camera.Handle, out IntPtr error);

        Assert.Equal(0u, bandwidth);
        Assert.Equal(IntPtr.Zero, error); // no GError either: the failure was silent
        var critical = Assert.Single(log.Criticals);
        Assert.Contains("arv_camera_is_uv_device", critical);
    }

    [NativeFact]
    public void UvMembers_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        camera.Dispose();

        // Disposal is checked before the camera type, for the question too.
        Assert.All(ThrowingMembers, m => Assert.Throws<ObjectDisposedException>(() => m.Call(camera)));
        Assert.Throws<ObjectDisposedException>(() => camera.UvIsBandwidthControlAvailable());
    }
}
