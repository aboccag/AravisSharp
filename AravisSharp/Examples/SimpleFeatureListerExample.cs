using System;
using AravisSharp.GenICam;

namespace AravisSharp.Examples;

public static class SimpleFeatureListerExample
{
    public static void Run()
    {
        Console.WriteLine("=== Simple Feature Lister ===\n");
        
        // Find camera
        CameraDiscovery.UpdateDeviceList();
        var count = CameraDiscovery.GetDeviceCount();
        
        if (count == 0)
        {
            Console.WriteLine("No cameras found!");
            return;
        }
        
        Console.WriteLine($"Found {count} camera(s)\n");
        
        using var camera = new Camera(null);
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        
        // Test individual features
        Console.WriteLine("Testing individual feature access:\n");
        
        var testFeatures = new[]
        {
            "DeviceVendorName",
            "DeviceModelName",
            "DeviceSerialNumber",
            "Width",
            "Height",
            "PixelFormat",
            "ExposureTime",
            "Gain"
        };
        
        foreach (var featureName in testFeatures)
        {
            Console.WriteLine($"Getting details for: {featureName}");
            try
            {
                var details = nodeMap.GetFeatureDetails(featureName);
                if (details != null)
                {
                    Console.WriteLine($"  Name: {details.Name}");
                    Console.WriteLine($"  Display: {details.DisplayName}");
                    Console.WriteLine($"  Type: {details.Type}");
                    Console.WriteLine($"  Access: {details.AccessMode}");
                    Console.WriteLine($"  Value: {details.CurrentValue}");
                    
                    if (details.Type == FeatureType.Enumeration)
                    {
                        Console.WriteLine($"  Choices: {string.Join(", ", details.EnumChoices)}");
                    }
                    else if (details.Type == FeatureType.Integer)
                    {
                        Console.WriteLine($"  Range: {details.IntMin} to {details.IntMax}");
                    }
                    else if (details.Type == FeatureType.Float)
                    {
                        Console.WriteLine($"  Range: {details.FloatMin} to {details.FloatMax}");
                    }
                }
                else
                {
                    Console.WriteLine("  <not found>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
