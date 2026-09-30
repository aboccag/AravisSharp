using AravisSharp;
using AravisSharp.Native;

namespace AravisSharp.Examples;

/// <summary>
/// Example demonstrating software-triggered acquisition using the Aravis high-level trigger API.
/// arv_camera_set_trigger("Software") sets TriggerSelector to FrameStart (or AcquisitionStart
/// as fallback), TriggerMode to On, TriggerSource to Software, and TriggerActivation to
/// rising edge. All other triggers are disabled.
/// </summary>
public static class TriggeredAcquisitionExample
{
    public static void Run()
    {
        Console.WriteLine("=== Triggered Acquisition Example ===\n");

        var deviceId = CameraPicker.Choose();
        if (deviceId == null)
            return;

        using var camera = new Camera(deviceId);
        Console.WriteLine($"Connected to: {camera.GetModelName()}");

        // Check software trigger support
        if (!camera.IsSoftwareTriggerSupported())
        {
            Console.WriteLine("ERROR: Camera does not support software trigger!");
            return;
        }
        Console.WriteLine("Software trigger supported: yes");

        // Configure for software trigger using the Aravis high-level API.
        // This single call handles: TriggerSelector=FrameStart, TriggerMode=On,
        // TriggerSource=Software, TriggerActivation=RisingEdge, and disables all other triggers.
        camera.SetTrigger("Software");
        Console.WriteLine($"Trigger configured: source={camera.GetTriggerSource()}\n");

        // GigE Vision: negotiate packet size before stream creation
        if (camera.IsGigEVisionDevice())
        {
            try
            {
                camera.GvAutoPacketSize();
                Console.WriteLine($"[GigE] Packet size: {camera.GvGetPacketSize()} bytes");
            }
            catch { }
        }

        // Create stream and allocate buffers using the correct payload size
        using var stream = camera.CreateStream();

        // GigE Vision: configure stream socket buffers
        if (camera.IsGigEVisionDevice())
        {
            stream.ConfigureGigEDefaults();
        }

        var payloadSize = camera.GetPayloadSize();
        var (_, _, width, height) = camera.GetRegion();
        Console.WriteLine($"Image: {width}x{height}, payload: {payloadSize} bytes");

        for (int i = 0; i < 5; i++)
        {
            // PushBuffer hands the buffer to the stream: the stream frees whatever is
            // still queued when it is disposed.
            stream.PushBuffer(new AravisSharp.Buffer(new IntPtr(payloadSize)));
        }

        // Start acquisition
        camera.StartAcquisition();

        int successCount = 0;
        try
        {
            // Small delay to let the camera arm itself
            Thread.Sleep(200);

            // Acquire 10 triggered frames
            for (int i = 0; i < 10; i++)
            {
                Console.Write($"Trigger {i + 1}/10... ");

                // Send software trigger
                camera.SoftwareTrigger();

                // Wait for frame (5 second timeout)
                var buffer = stream.PopBuffer(5000);

                if (buffer == null)
                {
                    Console.WriteLine("TIMEOUT - no frame received");
                }
                else
                {
                    if (buffer.Status == ArvBufferStatus.Success)
                    {
                        successCount++;
                        Console.WriteLine($"frame {buffer.FrameId}, {buffer.Width}x{buffer.Height}");
                    }
                    else
                    {
                        Console.WriteLine($"frame failed: {buffer.Status}");
                    }

                    // Always give the buffer back, whatever its status: a buffer that is
                    // not requeued is lost to the stream, which eventually starves.
                    stream.PushBuffer(buffer);
                }

                Thread.Sleep(50); // Brief pause between triggers
            }
        }
        finally
        {
            camera.StopAcquisition();

            // Restore camera to free-running mode, even when acquisition failed
            camera.ClearTriggers();
        }

        Console.WriteLine($"\nTriggered acquisition completed: {successCount}/10 frames received");
    }
}
