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

        using var camera = new Camera();
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

        // Create stream and allocate buffers using the correct payload size
        using var stream = camera.CreateStream();
        var payloadSize = camera.GetPayloadSize();
        var (_, _, width, height) = camera.GetRegion();
        Console.WriteLine($"Image: {width}x{height}, payload: {payloadSize} bytes");

        var buffers = new List<AravisSharp.Buffer>();
        for (int i = 0; i < 5; i++)
        {
            var buffer = new AravisSharp.Buffer(new IntPtr(payloadSize));
            buffers.Add(buffer);
            stream.PushBuffer(buffer);
        }

        // Start acquisition
        camera.StartAcquisition();

        // Small delay to let the camera arm itself
        Thread.Sleep(200);

        // Acquire 10 triggered frames
        int successCount = 0;
        for (int i = 0; i < 10; i++)
        {
            Console.Write($"Trigger {i + 1}/10... ");

            // Send software trigger
            camera.SoftwareTrigger();

            // Wait for frame (5 second timeout)
            var buffer = stream.PopBuffer(5000);

            if (buffer != null && buffer.Status == ArvBufferStatus.Success)
            {
                successCount++;
                Console.WriteLine($"frame {buffer.FrameId}, {buffer.Width}x{buffer.Height}");
                stream.PushBuffer(buffer);
            }
            else
            {
                Console.WriteLine("TIMEOUT - no frame received");
            }

            Thread.Sleep(50); // Brief pause between triggers
        }

        camera.StopAcquisition();

        // Restore camera to free-running mode
        camera.ClearTriggers();

        // Cleanup: Stream.Dispose() will drain remaining buffers automatically
        foreach (var buf in buffers)
            buf.Dispose();

        Console.WriteLine($"\nTriggered acquisition completed: {successCount}/10 frames received");
    }
}
