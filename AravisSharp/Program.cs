using AravisSharp;
using AravisSharp.Native;
using AravisSharp.Utilities;
using AravisSharp.Examples;

// Display platform information
Console.WriteLine("=== AravisSharp Platform Information ===");
Console.WriteLine(AravisLibrary.GetPlatformInfo());
Console.WriteLine($"\nAravis Library: {AravisLibrary.GetLibraryName()}");
Console.Write("Aravis Status: ");

if (AravisLibrary.IsAravisAvailable())
{
    Console.WriteLine("✓ Available\n");
}
else
{
    Console.WriteLine("✗ Not Found\n");
    Console.WriteLine(AravisLibrary.GetInstallationInstructions());
    Console.WriteLine("\nPlease install Aravis and restart the application.");
    return;
}

Console.WriteLine("=== AravisSharp Demo Menu ===\n");
Console.WriteLine("1. Run binding verification tests");
Console.WriteLine("2. Run camera capture demo");
Console.WriteLine("3. GenICam node map demo (simple)");
Console.WriteLine("4. GenICam explorer (interactive)");
Console.WriteLine("5. Feature browser (comprehensive)");
Console.WriteLine("6. Simple feature lister (debug)");
Console.WriteLine("7. Feature overview (detailed)");
Console.WriteLine("8. Quick feature demo (recommended)");
Console.WriteLine("0. Exit");
Console.Write("\nChoice: ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        RunBindingTests();
        break;
    case "2":
        RunCameraDemo();
        break;
    case "3":
        SimpleNodeMapDemo.Run();
        break;
    case "4":
        GenICamExplorerExample.Run();
        break;
    case "5":
        FeatureBrowserExample.Run();
        break;
    case "6":
        SimpleFeatureListerExample.Run();
        break;
    case "7":
        FeatureOverviewExample.Run();
        break;
    case "8":
        QuickFeatureDemoExample.Run();
        break;
    case "0":
        return;
    default:
        Console.WriteLine("Invalid choice!");
        return;
}

static void RunBindingTests()
{
    Console.WriteLine("\n=== Aravis Binding Verification ===\n");
    try
    {
        BindingTests.RunTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ Binding test failed: {ex.Message}");
    }
}

static void RunCameraDemo()
{
    Console.WriteLine("\n=== Aravis Camera Demo ===\n");

try
{
    // Discover all available cameras
    Console.WriteLine("Discovering cameras...");
    var cameras = CameraDiscovery.DiscoverCameras();
    
    if (cameras.Count == 0)
    {
        Console.WriteLine("No cameras found!");
        Console.WriteLine("\nMake sure:");
        Console.WriteLine("  - Aravis library is installed (libaravis-0.8.so)");
        Console.WriteLine("  - Camera is connected (USB3/GigE)");
        Console.WriteLine("  - Proper permissions are set for USB/network devices");
        return;
    }

    Console.WriteLine($"Found {cameras.Count} camera(s):\n");
    for (int i = 0; i < cameras.Count; i++)
    {
        Console.WriteLine($"  [{i}] {cameras[i]}");
    }
    Console.WriteLine();

    // Connect to the first camera
    Console.WriteLine("Connecting to the first camera...");
    using var camera = new Camera();
    
    Console.WriteLine($"Connected to: {camera.GetVendorName()} {camera.GetModelName()}");
    Console.WriteLine($"Serial Number: {camera.GetSerialNumber()}");
    
    try
    {
        Console.WriteLine($"Device ID: {camera.GetDeviceId()}");
    }
    catch
    {
        // Device ID not supported on all cameras
    }
    Console.WriteLine();
    
    // Get camera capabilities
    var (minWidth, maxWidth) = camera.GetWidthBounds();
    var (minHeight, maxHeight) = camera.GetHeightBounds();
    Console.WriteLine($"Sensor Size: {maxWidth} x {maxHeight}");
    
    var (x, y, width, height) = camera.GetRegion();
    Console.WriteLine($"Current ROI: {width} x {height} at ({x}, {y})");
    
    Console.WriteLine($"Pixel Format: {camera.GetPixelFormat()}");
    
    var (minExp, maxExp) = camera.GetExposureTimeBounds();
    var currentExp = camera.GetExposureTime();
    Console.WriteLine($"Exposure Time: {currentExp:F2} µs (Range: {minExp:F2} - {maxExp:F2})");
    
    var (minGain, maxGain) = camera.GetGainBounds();
    var currentGain = camera.GetGain();
    Console.WriteLine($"Gain: {currentGain:F2} (Range: {minGain:F2} - {maxGain:F2})");
    
    var currentFps = camera.GetFrameRate();
    Console.WriteLine($"Frame Rate: {currentFps:F2} fps\n");

    // Configure camera for acquisition
    Console.WriteLine("Configuring camera for acquisition...");
    
    // Reduce frame rate to avoid USB bandwidth issues
    try
    {
        camera.SetFrameRate(30.0); // Lower frame rate for USB3
        Console.WriteLine($"Set frame rate to 30 fps");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not set frame rate: {ex.Message}");
    }
    
    // Set exposure time to 10ms (10000 microseconds)
    camera.SetExposureTime(10000);
    Console.WriteLine($"Set exposure time to 10 ms");
    
    // Get the actual payload size from the camera
    var device = camera.GetDevice();
    int payloadSize;
    try
    {
        payloadSize = (int)device.GetIntegerFeature("PayloadSize");
        Console.WriteLine($"Payload size: {payloadSize} bytes");
    }
    catch
    {
        // Fallback if PayloadSize not available
        payloadSize = width * height * 2;
        Console.WriteLine($"Using calculated payload size: {payloadSize} bytes");
    }
    
    // Create stream
    Console.WriteLine("Creating stream...");
    using var stream = camera.CreateStream();
    
    // Allocate and push buffers (use exact payload size)
    const int numBuffers = 10; // More buffers for USB3
    var buffers = new List<AravisSharp.Buffer>();
    
    Console.WriteLine($"Allocating {numBuffers} buffers of {payloadSize} bytes each...");
    for (int i = 0; i < numBuffers; i++)
    {
        var buffer = new AravisSharp.Buffer(new IntPtr(payloadSize));
        buffers.Add(buffer);
        stream.PushBuffer(buffer);
    }

    // Start acquisition
    Console.WriteLine("Starting acquisition...\n");
    camera.StartAcquisition();

    // Acquire frames
    const int framesToAcquire = 10;
    Console.WriteLine($"Acquiring {framesToAcquire} frames...\n");
    
    bool firstFrameSaved = false;
    
    for (int i = 0; i < framesToAcquire; i++)
    {
        // Pop buffer with 2 second timeout
        var buffer = stream.PopBuffer(2000);
        
        if (buffer != null)
        {
            if (buffer.Status == ArvBufferStatus.Success)
            {
                Console.WriteLine($"Frame {i + 1}/{framesToAcquire}:");
                Console.WriteLine($"  Frame ID: {buffer.FrameId}");
                Console.WriteLine($"  Timestamp: {buffer.Timestamp} ns");
                Console.WriteLine($"  Size: {buffer.Width} x {buffer.Height}");
                Console.WriteLine($"  Pixel Format: 0x{buffer.PixelFormat:X8}");
                
                var (data, size) = buffer.GetData();
                Console.WriteLine($"  Data Size: {size} bytes");
                
                // Save first frame as PNG
                if (!firstFrameSaved)
                {
                    var filename = "captured_frame.png";
                    ImageHelper.SaveToPng(buffer, filename);
                    Console.WriteLine($"  ✓ Saved to {filename}");
                    firstFrameSaved = true;
                }
            }
            else
            {
                Console.WriteLine($"Frame {i + 1}/{framesToAcquire}: Failed - Status: {buffer.Status}");
            }
            
            // Push buffer back to stream for reuse
            stream.PushBuffer(buffer);
        }
        else
        {
            Console.WriteLine($"Frame {i + 1}/{framesToAcquire}: Timeout!");
        }
    }

    // Stop acquisition
    Console.WriteLine("\nStopping acquisition...");
    camera.StopAcquisition();
    
    // Get statistics
    var (completed, failures, underruns) = stream.GetStatistics();
    Console.WriteLine($"\nStream Statistics:");
    Console.WriteLine($"  Completed Buffers: {completed}");
    Console.WriteLine($"  Failures: {failures}");
    Console.WriteLine($"  Underruns: {underruns}");
    
    Console.WriteLine("\nAcquisition completed!");
    Console.WriteLine("\nNote: If you see 'Missing_packets' errors, you may need:");
    Console.WriteLine("  1. Add user to video group: sudo usermod -aG video $USER");
    Console.WriteLine("  2. Create USB udev rules (see README.md)");
    Console.WriteLine("  3. Logout and login again for group changes to take effect");
}
catch (AravisException ex)
{
    Console.WriteLine($"\nAravis Error: {ex.Message}");
    Console.WriteLine("\nTroubleshooting:");
    Console.WriteLine("  - Install Aravis: sudo apt-get install libaravis-0.8-0");
    Console.WriteLine("  - Check camera connection and power");
    Console.WriteLine("  - For GigE cameras, check network settings");
    Console.WriteLine("  - For USB3 cameras, check USB permissions");
}
catch (Exception ex)
{
    Console.WriteLine($"\nUnexpected Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
}