namespace AravisSharp.Tests;

internal static class CameraTestHelpers
{
    /// <summary>
    /// Environment variable naming the one real device the suite may open, e.g.
    /// "Opto Engineering-ITA24-GM-10C-600357". Without it the suite only ever opens the
    /// Aravis fake camera.
    /// </summary>
    public const string DeviceIdVariable = "ARAVIS_TEST_DEVICE_ID";

    public const string GigEVisionProtocol = "GigEVision";
    public const string Usb3VisionProtocol = "USB3Vision";

    private static string? RequestedDeviceId
    {
        get
        {
            var requested = Environment.GetEnvironmentVariable(DeviceIdVariable);
            return string.IsNullOrWhiteSpace(requested) ? null : requested;
        }
    }

    /// <summary>True when the suite runs against the fake camera (ARAVIS_TEST_DEVICE_ID unset).</summary>
    public static bool UsesFakeCamera => RequestedDeviceId == null;

    public static bool IsFakeCamera(CameraInfo camera) =>
        string.Equals(camera.Protocol, "Fake", StringComparison.OrdinalIgnoreCase) ||
        camera.DeviceId.StartsWith("Fake_", StringComparison.OrdinalIgnoreCase);

    // Discovery only, never opens a device. Cached: skip attributes ask once per test.
    private static readonly Lazy<string?> RequestedDeviceProtocol = new(() =>
        CameraDiscovery.DiscoverCameras().FirstOrDefault(c => c.DeviceId == RequestedDeviceId)?.Protocol);

    /// <summary>
    /// Protocol of the camera the suite opens ("Fake", "GigEVision", "USB3Vision"), from
    /// discovery data, or null when ARAVIS_TEST_DEVICE_ID names a device that is not present
    /// (the test then fails in <see cref="ResolveTestDeviceId"/>).
    /// </summary>
    public static string? TestCameraProtocol => UsesFakeCamera ? "Fake" : RequestedDeviceProtocol.Value;

    /// <summary>Skip reason for a test that needs a camera of the given protocol, or null to run it.</summary>
    public static string? SkipUnlessTestCameraProtocol(string protocol)
    {
        if (UsesFakeCamera)
            return $"Needs a {protocol} camera; set {DeviceIdVariable} to one to run this test.";

        var actual = TestCameraProtocol;
        // An unknown device is not skipped: ResolveTestDeviceId fails the test loudly.
        if (actual == null || actual == protocol)
            return null;

        return $"Needs a {protocol} camera; {DeviceIdVariable} names a {actual} camera.";
    }

    /// <summary>
    /// Returns the device the suite is allowed to open: the device named by
    /// ARAVIS_TEST_DEVICE_ID, or the Aravis fake camera when it is unset.
    /// Real hardware is strictly opt-in: tests reconfigure the camera (ROI, exposure,
    /// triggers) and start acquisition, and GigE cameras on a shared network may be in
    /// use by another application. Discovery alone never opens a device.
    /// Fails the calling test when the device is not present: a missing camera must not
    /// turn camera tests into silent passes.
    /// </summary>
    public static string ResolveTestDeviceId()
    {
        var cameras = CameraDiscovery.DiscoverCameras();
        var requested = RequestedDeviceId;

        if (requested != null)
        {
            var match = cameras.FirstOrDefault(c => c.DeviceId == requested);
            if (match == null)
            {
                Assert.Fail($"{DeviceIdVariable}=\"{requested}\" but no such device was discovered. " +
                            $"Visible devices: {string.Join(", ", cameras.Select(c => c.DeviceId))}");
            }
            return match!.DeviceId;
        }

        var fake = cameras.FirstOrDefault(IsFakeCamera);
        Assert.True(fake != null, "The Aravis fake camera was not discovered; the \"Fake\" interface should be enabled by NativeLibraryInitializer.");
        return fake!.DeviceId;
    }

    /// <summary>Opens the camera returned by <see cref="ResolveTestDeviceId"/>.</summary>
    public static Camera OpenTestCamera() => new(ResolveTestDeviceId());

    /// <summary>
    /// GenICam feature names for the exposure time, SFNC spelling first. Modern
    /// SFNC-compliant firmware exposes "ExposureTime", while older firmware and the
    /// Aravis fake camera (still true of arv-fake-camera.xml in 0.8.36) only expose the
    /// legacy "ExposureTimeAbs". Tests resolve the name at runtime so they work against
    /// either, on any native version.
    /// </summary>
    private static readonly string[] ExposureTimeFeatureNames = { "ExposureTime", "ExposureTimeAbs" };

    /// <summary>
    /// Returns the exposure time feature name this camera actually implements,
    /// or null when it exposes none of the known spellings.
    /// </summary>
    public static string? ResolveExposureTimeFeature(Camera camera)
    {
        foreach (var name in ExposureTimeFeatureNames)
        {
            try
            {
                if (camera.IsFeatureAvailable(name))
                    return name;
            }
            catch (AravisException)
            {
                // Unknown feature: Aravis reports "[<name>] Not found" rather than false.
            }
        }

        return null;
    }

    public static bool TryDisableExposureAuto(Camera camera)
    {
        try
        {
            if (!camera.IsExposureAutoAvailable())
                return true;

            camera.SetExposureTimeAuto(ArvAuto.Off);
            return camera.GetExposureTimeAuto() == ArvAuto.Off;
        }
        catch
        {
            return false;
        }
    }
}
