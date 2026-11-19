using AravisSharp.Native;
namespace AravisSharp.Tests;

internal static class NativeTestEnvironment
{
    private static readonly Lazy<bool> AravisAvailable = new(AravisLibrary.IsAravisAvailable);
    public const string MissingAravisMessage = "Native Aravis 0.8 library is not installed or not available in the test output.";

    public static bool IsAravisAvailable => AravisAvailable.Value;
}

/// <summary>
/// A test that needs the native Aravis library. Skipped when it cannot be loaded.
/// </summary>
internal class NativeFactAttribute : FactAttribute
{
    public NativeFactAttribute()
    {
        if (!NativeTestEnvironment.IsAravisAvailable)
        {
            Skip = NativeTestEnvironment.MissingAravisMessage;
        }
    }
}

/// <summary>
/// A theory that needs the native Aravis library. Skipped when it cannot be loaded.
/// </summary>
internal sealed class NativeTheoryAttribute : TheoryAttribute
{
    public NativeTheoryAttribute()
    {
        if (!NativeTestEnvironment.IsAravisAvailable)
        {
            Skip = NativeTestEnvironment.MissingAravisMessage;
        }
    }
}

// xunit v2 has no runtime skip: a test can only be skipped by its attribute, when the
// test is discovered. The attributes below decide from ARAVIS_TEST_DEVICE_ID and from
// discovery data (protocol) alone, and never open a device.

/// <summary>
/// A test that exercises something the Aravis fake camera does not implement (for
/// example AcquisitionFrameCount or ExposureAuto). Skipped unless
/// ARAVIS_TEST_DEVICE_ID names a real camera.
/// </summary>
internal sealed class RealCameraFactAttribute : NativeFactAttribute
{
    public RealCameraFactAttribute(string fakeCameraLacks)
    {
        if (Skip == null && CameraTestHelpers.UsesFakeCamera)
        {
            Skip = $"The fake camera has no {fakeCameraLacks}; set {CameraTestHelpers.DeviceIdVariable} to a real camera to run this test.";
        }
    }
}

/// <summary>
/// A test that relies on a behaviour of the Aravis fake camera itself, for example its
/// stream thread filling queued buffers even without acquisition
/// (aravis/src/arvfakestream.c). Skipped when ARAVIS_TEST_DEVICE_ID names a real camera.
/// </summary>
internal sealed class FakeCameraFactAttribute : NativeFactAttribute
{
    public FakeCameraFactAttribute()
    {
        if (Skip == null && !CameraTestHelpers.UsesFakeCamera)
        {
            Skip = $"Relies on the fake camera's stream thread; {CameraTestHelpers.DeviceIdVariable} names a real camera.";
        }
    }
}

/// <summary>
/// A theory that must only run on the Aravis fake camera, for example because it writes
/// settings no real camera should receive. Skipped without the native library and when
/// ARAVIS_TEST_DEVICE_ID names a real camera.
/// </summary>
internal sealed class FakeCameraTheoryAttribute : TheoryAttribute
{
    public FakeCameraTheoryAttribute()
    {
        if (!NativeTestEnvironment.IsAravisAvailable)
        {
            Skip = NativeTestEnvironment.MissingAravisMessage;
        }
        else if (!CameraTestHelpers.UsesFakeCamera)
        {
            Skip = $"Runs on the fake camera only; {CameraTestHelpers.DeviceIdVariable} names a real camera.";
        }
    }
}

/// <summary>
/// A test that needs a GigE Vision camera. Skipped unless ARAVIS_TEST_DEVICE_ID names one.
/// </summary>
internal sealed class GigECameraFactAttribute : NativeFactAttribute
{
    public GigECameraFactAttribute()
    {
        Skip ??= CameraTestHelpers.SkipUnlessTestCameraProtocol(CameraTestHelpers.GigEVisionProtocol);
    }
}

/// <summary>
/// A test that needs a USB3 Vision camera. Skipped unless ARAVIS_TEST_DEVICE_ID names one.
/// </summary>
internal sealed class Usb3CameraFactAttribute : NativeFactAttribute
{
    public Usb3CameraFactAttribute()
    {
        Skip ??= CameraTestHelpers.SkipUnlessTestCameraProtocol(CameraTestHelpers.Usb3VisionProtocol);
    }
}

/// <summary>
/// A test that needs a camera that is not GigE Vision (the fake camera, or a USB3 Vision
/// camera named by ARAVIS_TEST_DEVICE_ID).
/// </summary>
internal sealed class NonGigECameraFactAttribute : NativeFactAttribute
{
    public NonGigECameraFactAttribute()
    {
        if (Skip == null && CameraTestHelpers.TestCameraProtocol == CameraTestHelpers.GigEVisionProtocol)
        {
            Skip = $"{CameraTestHelpers.DeviceIdVariable} names a GigE Vision camera; this test needs a non-GigE stream.";
        }
    }
}
