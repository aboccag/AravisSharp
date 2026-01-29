using AravisSharp;
using AravisSharp.GenICam;

namespace AravisSharp.Examples;

/// <summary>
/// Simple demonstration of GenICam node map feature access
/// </summary>
public static class SimpleNodeMapDemo
{
    public static void Run()
    {
        Console.WriteLine("=== GenICam Node Map Feature Access Demo ===\n");

        try
        {
            // Discover and connect to camera
            Console.WriteLine("Connecting to camera...");
            var cameras = CameraDiscovery.DiscoverCameras();
            
            if (cameras.Count == 0)
            {
                Console.WriteLine("No cameras found!");
                return;
            }

            using var camera = new Camera();
            Console.WriteLine($"Connected to: {camera.GetVendorName()} {camera.GetModelName()}\n");

            // Get the device node map
            var device = camera.GetDevice();
            var nodeMap = device.NodeMap;

            // Demonstrate reading common camera features
            Console.WriteLine("=== Reading Camera Features ===\n");
            DemonstrateReadFeatures(nodeMap);

            Console.WriteLine("\n=== Modifying Camera Features ===\n");
            DemonstrateModifyFeatures(nodeMap, camera);

            Console.WriteLine("\n=== Feature Access Complete ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static void DemonstrateReadFeatures(NodeMap nodeMap)
    {
        // String features
        Console.WriteLine("Device Information:");
        TryReadString(nodeMap, "DeviceVendorName");
        TryReadString(nodeMap, "DeviceModelName");
        TryReadString(nodeMap, "DeviceFirmwareVersion");
        TryReadString(nodeMap, "DeviceSerialNumber");

        Console.WriteLine("\nImage Configuration:");
        TryReadInteger(nodeMap, "Width");
        TryReadInteger(nodeMap, "Height");
        TryReadString(nodeMap, "PixelFormat");

        Console.WriteLine("\nAcquisition Settings:");
        TryReadFloat(nodeMap, "ExposureTime");
        TryReadFloat(nodeMap, "Gain");
        TryReadFloat(nodeMap, "AcquisitionFrameRate");
        TryReadString(nodeMap, "TriggerMode");
    }

    private static void DemonstrateModifyFeatures(NodeMap nodeMap, Camera camera)
    {
        Console.WriteLine("Reading current gain...");
        var currentGain = nodeMap.GetFloatFeature("Gain");
        Console.WriteLine($"  Current Gain: {currentGain:F2}");

        Console.WriteLine("\nSetting gain to 6.0...");
        nodeMap.SetFloatFeature("Gain", 6.0);
        
        var newGain = nodeMap.GetFloatFeature("Gain");
        Console.WriteLine($"  New Gain: {newGain:F2}");

        Console.WriteLine("\nRestoring original gain...");
        nodeMap.SetFloatFeature("Gain", currentGain);
        Console.WriteLine($"  Restored Gain: {currentGain:F2}");

        // Demonstrate pixel format change
        Console.WriteLine("\nCurrent pixel format:");
        var currentFormat = camera.GetPixelFormat();
        Console.WriteLine($"  {currentFormat}");
    }

    private static void TryReadString(NodeMap nodeMap, string featureName)
    {
        try
        {
            var value = nodeMap.GetStringFeature(featureName);
            Console.WriteLine($"  {featureName,-30}: {value}");
        }
        catch
        {
            Console.WriteLine($"  {featureName,-30}: <not available>");
        }
    }

    private static void TryReadInteger(NodeMap nodeMap, string featureName)
    {
        try
        {
            var value = nodeMap.GetIntegerFeature(featureName);
            Console.WriteLine($"  {featureName,-30}: {value}");
        }
        catch
        {
            Console.WriteLine($"  {featureName,-30}: <not available>");
        }
    }

    private static void TryReadFloat(NodeMap nodeMap, string featureName)
    {
        try
        {
            var value = nodeMap.GetFloatFeature(featureName);
            Console.WriteLine($"  {featureName,-30}: {value:F2}");
        }
        catch
        {
            Console.WriteLine($"  {featureName,-30}: <not available>");
        }
    }
}
