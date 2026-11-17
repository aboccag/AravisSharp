using System.Diagnostics;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #7, review decision of 2026-11-17: <see cref="Camera.AcquireSingleFrame"/> does not
/// hold the camera's lock while it waits for the frame, so other threads can still use the
/// camera (to send the software trigger the frame waits for, for example), and
/// <see cref="Camera.Dispose"/> wakes the wait with <see cref="ObjectDisposedException"/>.
/// </summary>
/// <remarks>
/// The Aravis fake camera ignores triggers: its stream thread fills a buffer each time the
/// wall clock (<c>g_get_real_time</c>) crosses a multiple of <c>AcquisitionFramePeriod</c>
/// (aravis/src/arvfakecamera.c, <c>arv_fake_camera_get_sleep_time_for_next_frame</c>). So
/// the tests set a 5 s period and start just after such a multiple: the frame is then due
/// about 4.5 s later, which leaves a window in which <c>AcquireSingleFrame</c> is known to be
/// waiting. The same thread sleeps until the next multiple before it can be stopped, so the
/// clean-up of the stream may take up to one period on this camera.
/// </remarks>
[Collection("CameraAcquisition")]
public class AcquireSingleFrameTests
{
    private const long FramePeriodUs = 5_000_000;
    private const long FramePeriodMs = FramePeriodUs / 1000;

    private static Camera OpenSlowCamera()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            CameraTestHelpers.ConfigureForAcquisition(camera);
            camera.SetIntegerFeature("AcquisitionFramePeriod", FramePeriodUs);
            return camera;
        }
        catch
        {
            camera.Dispose();
            throw;
        }
    }

    /// <summary>Waits until the wall clock is 100 to 600 ms past a multiple of the frame period.</summary>
    private static void WaitForStartOfFramePeriod()
    {
        long phase = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % FramePeriodMs;
        if (phase < 100)
            Thread.Sleep((int)(100 - phase));
        else if (phase > 600)
            Thread.Sleep((int)(FramePeriodMs - phase + 100));
    }

    // On a camera locked for the whole wait, the other calls wait for the frame (~4.5 s).
    // timeoutUs = 0 waits without limit (arv_camera_acquisition pops with no timeout): on a
    // real camera in software trigger mode, a SoftwareTrigger() waiting for that lock is a
    // deadlock.
    [FakeCameraFact]
    public void AcquireSingleFrame_WhileWaiting_ShouldLetOtherThreadsUseTheCamera()
    {
        var camera = OpenSlowCamera();
        try
        {
            WaitForStartOfFramePeriod();
            var acquisition = Task.Run(() => camera.AcquireSingleFrame(0));
            Thread.Sleep(300);
            Assert.False(acquisition.IsCompleted, "the frame arrived early: nothing was checked");

            var payload = Task.Run(() => camera.GetPayloadSize());
            Assert.True(payload.Wait(TimeSpan.FromSeconds(1)), "a camera call from another thread waited for AcquireSingleFrame");
            Assert.True(payload.Result > 0);

            var trigger = Task.Run(camera.SoftwareTrigger);
            Assert.True(trigger.Wait(TimeSpan.FromSeconds(1)), "SoftwareTrigger() waited for AcquireSingleFrame");
            Assert.False(acquisition.IsCompleted, "the frame arrived early: nothing was checked");

            Assert.True(acquisition.Wait(TimeSpan.FromMilliseconds(2 * FramePeriodMs + 2000)), "the frame did not arrive");
            using var frame = acquisition.Result;
            Assert.Equal(ArvBufferStatus.Success, frame.Status);
            Assert.Equal((int)payload.Result, frame.CopyData().Length);
        }
        finally
        {
            camera.Dispose();
        }
    }

    // On code that does not watch the camera during the wait, the call returns the frame
    // once it arrives (~4.5 s later) instead of throwing.
    [FakeCameraFact]
    public void AcquireSingleFrame_CameraDisposedWhileWaiting_ShouldThrowObjectDisposed_AndReleaseTheCamera()
    {
        var camera = OpenSlowCamera();
        long id = GObjectReleaseProbe.Watch(camera.Handle);
        try
        {
            WaitForStartOfFramePeriod();
            var acquisition = Task.Run(() => Record.Exception(() => camera.AcquireSingleFrame(0)?.Dispose()));
            Thread.Sleep(300);
            Assert.False(acquisition.IsCompleted, "the frame arrived early: nothing was checked");

            camera.Dispose();

            // Woken within one 100 ms slice; the stream clean-up then waits for the fake
            // stream thread, asleep until the next frame (see the class remarks).
            Assert.True(acquisition.Wait(TimeSpan.FromMilliseconds(FramePeriodMs + 2000)), "Dispose() did not wake AcquireSingleFrame(0)");
            Assert.IsType<ObjectDisposedException>(acquisition.Result);
            // The wait gave its lease back: the native camera is released.
            Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id));
        }
        finally
        {
            camera.Dispose();
        }
    }

    [FakeCameraFact]
    public void AcquireSingleFrame_Timeout_ShouldThrowAravisException_AndLeaveTheCameraUsable()
    {
        var camera = OpenSlowCamera();
        try
        {
            WaitForStartOfFramePeriod();
            var clock = Stopwatch.StartNew();
            var ex = Assert.Throws<AravisException>(() => camera.AcquireSingleFrame(300_000));
            Assert.InRange(clock.ElapsedMilliseconds, 250, FramePeriodMs + 2000);
            Assert.Null(ex.Domain); // no frame within the timeout is not a native error

            camera.SetIntegerFeature("AcquisitionFramePeriod", 40_000);
            using var frame = camera.AcquireSingleFrame();
            Assert.Equal(ArvBufferStatus.Success, frame.Status);
        }
        finally
        {
            camera.Dispose();
        }
    }
}
