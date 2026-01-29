using AravisSharp;
using AravisSharp.GenICam;

namespace AravisSharp.Examples;

/// <summary>
/// Example program demonstrating GenICam node map exploration
/// </summary>
public static class GenICamExplorerExample
{
    public static void Run()
    {
        Console.WriteLine("=== GenICam Feature Node Map Explorer ===\n");

        try
        {
            // Discover cameras
            Console.WriteLine("Discovering cameras...");
            var cameras = CameraDiscovery.DiscoverCameras();
            
            if (cameras.Count == 0)
            {
                Console.WriteLine("No cameras found!");
                return;
            }

            Console.WriteLine($"Found {cameras.Count} camera(s)\n");

            // Connect to first camera
            Console.WriteLine("Connecting to camera...");
            using var camera = new Camera();
            
            Console.WriteLine($"Connected to: {camera.GetVendorName()} {camera.GetModelName()}");
            Console.WriteLine($"Serial: {camera.GetSerialNumber()}\n");

            // Get the device for GenICam access
            var device = camera.GetDevice();
            var nodeMap = device.NodeMap;

            // Display menu
            while (true)
            {
                Console.WriteLine("\n=== GenICam Node Map Menu ===");
                Console.WriteLine("1. Read common camera features");
                Console.WriteLine("2. Explore specific feature by name");
                Console.WriteLine("3. List pixel format options");
                Console.WriteLine("4. List trigger modes");
                Console.WriteLine("5. Get GenICam XML (first 500 chars)");
                Console.WriteLine("6. Test integer feature (Width)");
                Console.WriteLine("7. Test float feature (Gain)");
                Console.WriteLine("8. Test boolean feature (AcquisitionFrameRateEnable)");
                Console.WriteLine("9. Test enumeration feature (PixelFormat)");
                Console.WriteLine("0. Exit");
                Console.Write("\nChoice: ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            ReadCommonFeatures(nodeMap);
                            break;
                        case "2":
                            ExploreFeature(nodeMap);
                            break;
                        case "3":
                            ListPixelFormats(camera);
                            break;
                        case "4":
                            ListTriggerModes(nodeMap);
                            break;
                        case "5":
                            ShowGenicamXml(nodeMap);
                            break;
                        case "6":
                            TestIntegerFeature(nodeMap);
                            break;
                        case "7":
                            TestFloatFeature(nodeMap);
                            break;
                        case "8":
                            TestBooleanFeature(nodeMap);
                            break;
                        case "9":
                            TestEnumerationFeature(nodeMap);
                            break;
                        case "0":
                            Console.WriteLine("Exiting...");
                            return;
                        default:
                            Console.WriteLine("Invalid choice!");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

static void ReadCommonFeatures(NodeMap nodeMap)
{
    Console.WriteLine("\n=== Common Camera Features ===\n");

    var commonFeatures = new[]
    {
        "DeviceVendorName",
        "DeviceModelName",
        "DeviceFirmwareVersion",
        "DeviceSerialNumber",
        "DeviceTemperature",
        "Width",
        "Height",
        "PixelFormat",
        "AcquisitionMode",
        "AcquisitionFrameRate",
        "ExposureTime",
        "Gain",
        "TriggerMode",
        "TriggerSource"
    };

    foreach (var featureName in commonFeatures)
    {
        try
        {
            var value = nodeMap.GetStringFeature(featureName);
            Console.WriteLine($"  {featureName,-30}: {value}");
        }
        catch
        {
            try
            {
                // Try as integer
                var value = nodeMap.GetIntegerFeature(featureName);
                Console.WriteLine($"  {featureName,-30}: {value}");
            }
            catch
            {
                try
                {
                    // Try as float
                    var value = nodeMap.GetFloatFeature(featureName);
                    Console.WriteLine($"  {featureName,-30}: {value:F2}");
                }
                catch
                {
                    Console.WriteLine($"  {featureName,-30}: <not available>");
                }
            }
        }
    }
}

static void ExploreFeature(NodeMap nodeMap)
{
    Console.Write("\nEnter feature name: ");
    var featureName = Console.ReadLine();
    
    if (string.IsNullOrEmpty(featureName))
        return;

    Console.WriteLine($"\nExploring feature: {featureName}");
    
    var node = nodeMap.GetNode(featureName);
    if (node == null)
    {
        Console.WriteLine("Feature not found or not readable!");
        return;
    }

    Console.WriteLine($"  Name: {node.Name}");
    Console.WriteLine($"  Value: {node.Value ?? "N/A"}");
    Console.WriteLine($"  Available: {node.IsAvailable}");
    Console.WriteLine($"  Implemented: {node.IsImplemented}");
}

static void ListPixelFormats(Camera camera)
{
    Console.WriteLine("\n=== Available Pixel Formats ===\n");
    
    try
    {
        var currentFormat = camera.GetPixelFormat();
        Console.WriteLine($"Current format: {currentFormat}");
        
        Console.WriteLine("\nCommon formats:");
        Console.WriteLine("  - Mono8");
        Console.WriteLine("  - Mono10");
        Console.WriteLine("  - Mono12");
        Console.WriteLine("  - RGB8");
        Console.WriteLine("  - BGR8");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static void ListTriggerModes(NodeMap nodeMap)
{
    Console.WriteLine("\n=== Trigger Configuration ===\n");
    
    try
    {
        var triggerMode = nodeMap.GetStringFeature("TriggerMode");
        Console.WriteLine($"  Trigger Mode: {triggerMode}");
        
        var triggerSource = nodeMap.GetStringFeature("TriggerSource");
        Console.WriteLine($"  Trigger Source: {triggerSource}");
        
        var triggerActivation = nodeMap.GetStringFeature("TriggerActivation");
        Console.WriteLine($"  Trigger Activation: {triggerActivation}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading trigger settings: {ex.Message}");
    }
}

static void ShowGenicamXml(NodeMap nodeMap)
{
    Console.WriteLine("\n=== GenICam XML ===\n");
    Console.WriteLine("GenICam XML retrieval not yet implemented via device API.");
    Console.WriteLine("This would require direct access to the Genicam object.");
}

static void TestIntegerFeature(NodeMap nodeMap)
{
    Console.WriteLine("\n=== Testing Integer Feature (Width) ===\n");
    
    try
    {
        var currentWidth = nodeMap.GetIntegerFeature("Width");
        Console.WriteLine($"Current Width: {currentWidth}");
        
        Console.Write("Enter new width (or press Enter to skip): ");
        var input = Console.ReadLine();
        
        if (!string.IsNullOrEmpty(input) && long.TryParse(input, out long newWidth))
        {
            nodeMap.SetIntegerFeature("Width", newWidth);
            Console.WriteLine($"Width set to: {newWidth}");
            
            var verifyWidth = nodeMap.GetIntegerFeature("Width");
            Console.WriteLine($"Verified Width: {verifyWidth}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static void TestFloatFeature(NodeMap nodeMap)
{
    Console.WriteLine("\n=== Testing Float Feature (Gain) ===\n");
    
    try
    {
        var currentGain = nodeMap.GetFloatFeature("Gain");
        Console.WriteLine($"Current Gain: {currentGain:F2}");
        
        Console.Write("Enter new gain (or press Enter to skip): ");
        var input = Console.ReadLine();
        
        if (!string.IsNullOrEmpty(input) && double.TryParse(input, out double newGain))
        {
            nodeMap.SetFloatFeature("Gain", newGain);
            Console.WriteLine($"Gain set to: {newGain:F2}");
            
            var verifyGain = nodeMap.GetFloatFeature("Gain");
            Console.WriteLine($"Verified Gain: {verifyGain:F2}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static void TestBooleanFeature(NodeMap nodeMap)
{
    Console.WriteLine("\n=== Testing Boolean Feature ===\n");
    
    try
    {
        var featureName = "AcquisitionFrameRateEnable";
        var currentValue = nodeMap.GetBooleanFeature(featureName);
        Console.WriteLine($"Current {featureName}: {currentValue}");
        
        Console.Write($"Toggle to {!currentValue}? (y/n): ");
        var input = Console.ReadLine();
        
        if (input?.ToLower() == "y")
        {
            nodeMap.SetBooleanFeature(featureName, !currentValue);
            Console.WriteLine($"{featureName} set to: {!currentValue}");
            
            var verifyValue = nodeMap.GetBooleanFeature(featureName);
            Console.WriteLine($"Verified: {verifyValue}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine("Note: This feature might not be available on your camera");
    }
}

static void TestEnumerationFeature(NodeMap nodeMap)
{
    Console.WriteLine("\n=== Testing Enumeration Feature (PixelFormat) ===\n");
    
    try
    {
        var currentFormat = nodeMap.GetStringFeature("PixelFormat");
        Console.WriteLine($"Current PixelFormat: {currentFormat}");
        
        // Common pixel formats to try
        var commonFormats = new[] { "Mono8", "Mono10", "Mono12", "RGB8", "BGR8", "YUV422" };
        
        Console.WriteLine("\nCommon formats you might try:");
        foreach (var format in commonFormats)
        {
            Console.WriteLine($"  - {format}");
        }
        
        Console.Write("\nEnter new pixel format (or press Enter to skip): ");
        var input = Console.ReadLine();
        
        if (!string.IsNullOrEmpty(input))
        {
            nodeMap.SetStringFeature("PixelFormat", input);
            Console.WriteLine($"PixelFormat set to: {input}");
            
            var verifyFormat = nodeMap.GetStringFeature("PixelFormat");
            Console.WriteLine($"Verified: {verifyFormat}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
}
