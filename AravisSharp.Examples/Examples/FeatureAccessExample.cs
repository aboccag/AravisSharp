using AravisSharp;

namespace AravisSharp.Examples;

/// <summary>
/// Example demonstrating GenICam feature access
/// </summary>
public static class FeatureAccessExample
{
    public static void Run()
    {
        Console.WriteLine("=== GenICam Feature Access Example ===\n");

        using var camera = new Camera();
        var device = camera.GetDevice();
        
        Console.WriteLine($"Camera: {camera.GetModelName()}\n");

        // Read various features
        Console.WriteLine("Camera Features:");
        
        try
        {
            var deviceVersion = device.GetStringFeature("DeviceVersion");
            Console.WriteLine($"  Device Version: {deviceVersion}");
        }
        catch { Console.WriteLine("  Device Version: N/A"); }

        try
        {
            var sensorWidth = device.GetIntegerFeature("SensorWidth");
            var sensorHeight = device.GetIntegerFeature("SensorHeight");
            Console.WriteLine($"  Sensor: {sensorWidth} x {sensorHeight}");
        }
        catch { Console.WriteLine("  Sensor size: N/A"); }

        try
        {
            var temperature = device.GetFloatFeature("DeviceTemperature");
            Console.WriteLine($"  Temperature: {temperature:F1}°C");
        }
        catch { Console.WriteLine("  Temperature: N/A"); }

        // Set features
        Console.WriteLine("\nSetting features:");
        
        try
        {
            device.SetFloatFeature("AcquisitionFrameRate", 50.0);
            var actualRate = device.GetFloatFeature("AcquisitionFrameRate");
            Console.WriteLine($"  Frame rate set to: {actualRate} fps");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not set frame rate: {ex.Message}");
        }

        try
        {
            // Enable auto exposure if available
            device.SetStringFeature("ExposureAuto", "Continuous");
            Console.WriteLine("  Auto exposure: Enabled");
        }
        catch
        {
            Console.WriteLine("  Auto exposure: Not available");
        }

        Console.WriteLine("\nFeature access completed!");
    }
}
