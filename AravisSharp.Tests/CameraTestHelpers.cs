namespace AravisSharp.Tests;

internal static class CameraTestHelpers
{
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
