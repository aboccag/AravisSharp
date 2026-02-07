using AravisSharp;
using AravisSharp.Native;
using AravisSharp.Utilities;

namespace AravisSharp.Examples;

/// <summary>
/// Example demonstrating continuous high-performance image acquisition
/// </summary>
public static class ContinuousAcquisitionExample
{
    public static void Run()
    {
        Console.WriteLine("=== Continuous Acquisition Example ===\n");

        using var camera = new Camera();
        Console.WriteLine($"Connected to: {camera.GetModelName()}\n");

        // Configure camera
        camera.SetExposureTime(5000); // 5ms
        camera.SetFrameRate(100); // 100 fps
        
        var (x, y, width, height) = camera.GetRegion();
        var pixelFormat = camera.GetPixelFormat();
        
        Console.WriteLine($"Image: {width}x{height}, Format: {pixelFormat}");

        // Create stream
        using var stream = camera.CreateStream();

        // Allocate buffers
        var buffers = new List<AravisSharp.Buffer>();
        var bufferSize = ImageHelper.CalculateBufferSize(width, height, 
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8);

        for (int i = 0; i < 20; i++) // More buffers for high-speed capture
        {
            var buffer = new AravisSharp.Buffer(new IntPtr(bufferSize));
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

        // Cleanup: Stream.Dispose() will drain remaining buffers automatically
        // Now safe to dispose buffers
        foreach (var buf in buffers)
            buf.Dispose();
    }
}
