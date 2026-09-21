namespace AravisSharp.Tests;

internal static class CameraTestHelpers
{
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
