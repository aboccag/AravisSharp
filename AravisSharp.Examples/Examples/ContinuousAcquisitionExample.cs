using AravisSharp;
using AravisSharp.Native;
using AravisSharp.Utilities;

namespace AravisSharp.Examples;

/// <summary>
/// Example demonstrating continuous high-performance image acquisition
/// with proper GigE Vision and USB3 Vision support.
/// </summary>
public static class ContinuousAcquisitionExample
{
    public static void Run()
    {
        Console.WriteLine("=== Continuous Acquisition Example ===\n");

        var deviceId = CameraPicker.Choose();
        if (deviceId == null)
            return;

        using var camera = new Camera(deviceId);
        Console.WriteLine($"Connected to: {camera.GetVendorName()} {camera.GetModelName()}\n");

        // --- GigE Vision: negotiate optimal packet size BEFORE creating stream ---
        if (camera.IsGigEVisionDevice())
        {
            Console.WriteLine("[GigE] Negotiating optimal packet size...");
            try
            {
                camera.GvAutoPacketSize();
                var packetSize = camera.GvGetPacketSize();
                Console.WriteLine($"[GigE] Packet size: {packetSize} bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GigE] Warning: Could not auto-negotiate packet size: {ex.Message}");
                Console.WriteLine("[GigE] Falling back to 1500 bytes (standard Ethernet MTU)");
                try { camera.GvSetPacketSize(1500); } catch { /* ignore */ }
            }
        }

        // Configure camera
        try { camera.SetExposureTime(5000); } catch { } // 5ms
        try { camera.SetFrameRate(30); } catch { }      // 30 fps (safe default)
        
        var (x, y, width, height) = camera.GetRegion();
        var pixelFormat = camera.GetPixelFormat();
        
        // Use camera-reported payload size (accounts for pixel format, ROI, chunk data, etc.)
        var payloadSize = camera.GetPayloadSize();
        Console.WriteLine($"Image: {width}x{height}, Format: {pixelFormat}, Payload: {payloadSize} bytes");

        // Create stream
        using var stream = camera.CreateStream();

        // --- GigE Vision: configure stream socket buffer for reliable transfer ---
        if (camera.IsGigEVisionDevice())
        {
            Console.WriteLine("[GigE] Configuring stream for reliable transfer...");
            stream.ConfigureGigEDefaults(socketBufferSizeMB: 4);
            Console.WriteLine($"[GigE] Socket buffer size: {stream.GetSocketBufferSize()} bytes");
        }

        // Allocate buffers using the correct payload size from the camera
        var buffers = new List<AravisSharp.Buffer>();
        for (int i = 0; i < 20; i++)
        {
            var buffer = new AravisSharp.Buffer(new IntPtr(payloadSize));
            buffers.Add(buffer);
            stream.PushBuffer(buffer);
        }

        // Setup statistics
        var stats = new AcquisitionStats();
        
        // Start acquisition
        camera.StartAcquisition();
        stats.Start();

        Console.WriteLine("\nAcquiring images... Press Ctrl+C to stop\n");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        int savedCount = 0;
        const int maxSaved = 5;

        while (!cts.Token.IsCancellationRequested)
        {
            var buffer = stream.PopBuffer(1000); // 1 second timeout

            if (buffer != null)
            {
                if (buffer.Status == ArvBufferStatus.Success)
                {
                    stats.RecordSuccess(buffer.GetData().Size);

                    // Save first few frames
                    if (savedCount < maxSaved)
                    {
                        var filename = $"frame_{buffer.FrameId:D6}.raw";
                        ImageHelper.SaveToRawFile(buffer, filename);
                        savedCount++;
                        Console.WriteLine($"Saved {filename}");
                    }
                    
                    // Print status every 100 frames
                    if (stats.SuccessCount % 100 == 0)
                    {
                        stats.PrintStatus();
                    }
                }
                else
                {
                    stats.RecordFailure();
                    
                    // Show failure reason for first few failures to aid debugging
                    if (stats.FailureCount <= 5)
                    {
                        Console.WriteLine($"[WARN] Buffer failed with status: {buffer.Status}");
                    }
                }

                stream.PushBuffer(buffer);
            }
            else
            {
                stats.RecordTimeout();
            }
        }

        // Stop acquisition
        stats.Stop();
        camera.StopAcquisition();

        // Print final statistics
        Console.WriteLine("\n\n" + stats.ToString());
        
        var (completed, failures, underruns) = stream.GetStatistics();
        Console.WriteLine($"\nStream Statistics:");
        Console.WriteLine($"  Completed: {completed}");
        Console.WriteLine($"  Failures: {failures}");
        Console.WriteLine($"  Underruns: {underruns}");

        // GigE-specific statistics
        if (camera.IsGigEVisionDevice())
        {
            try
            {
                var (port, resent, missing) = stream.GetGigEStatistics();
                Console.WriteLine($"\nGigE Statistics:");
                Console.WriteLine($"  Stream port: {port}");
                Console.WriteLine($"  Resent packets: {resent}");
                Console.WriteLine($"  Missing packets: {missing}");
            }
            catch { /* not a GigE stream */ }
        }

        // Cleanup: Stream.Dispose() will drain remaining buffers automatically
        // Now safe to dispose buffers
        foreach (var buf in buffers)
            buf.Dispose();
    }
}
