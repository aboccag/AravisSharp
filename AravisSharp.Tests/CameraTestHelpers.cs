namespace AravisSharp.Tests;

internal static class CameraTestHelpers
{
    /// <summary>
    /// Environment variable naming the one real device the suite may open, e.g.
    /// "Opto Engineering-ITA24-GM-10C-600357". Without it the suite only ever opens the
    /// Aravis fake camera.
    /// </summary>
    public const string DeviceIdVariable = "ARAVIS_TEST_DEVICE_ID";

    public static bool IsFakeCamera(CameraInfo camera) =>
        string.Equals(camera.Protocol, "Fake", StringComparison.OrdinalIgnoreCase) ||
        camera.DeviceId.StartsWith("Fake_", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the device the suite is allowed to open, or null when it is not present.
    /// Real hardware is strictly opt-in: tests reconfigure the camera (ROI, exposure,
    /// triggers) and start acquisition, and GigE cameras on a shared network may be in
    /// use by another application. Discovery alone never opens a device.
    /// </summary>
    public static string? ResolveTestDeviceId()
    {
        var cameras = CameraDiscovery.DiscoverCameras();
        var requested = Environment.GetEnvironmentVariable(DeviceIdVariable);

        if (!string.IsNullOrWhiteSpace(requested))
            return cameras.FirstOrDefault(c => c.DeviceId == requested)?.DeviceId;

        return cameras.FirstOrDefault(IsFakeCamera)?.DeviceId;
    }

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
