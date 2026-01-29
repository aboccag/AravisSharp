using System;
using AravisSharp.GenICam;

namespace AravisSharp.Examples;

/// <summary>
/// Quick working demonstration of GenICam features
/// </summary>
public static class QuickFeatureDemoExample
{
    public static void Run()
    {
        Console.WriteLine("=== Quick GenICam Feature Demo ===\n");
        
        CameraDiscovery.UpdateDeviceList();
        if (CameraDiscovery.GetDeviceCount() == 0)
        {
            Console.WriteLine("No cameras found!");
            return;
        }
        
        using var camera = new Camera(null);
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        
        Console.WriteLine("═══ Device Information (Read-Only) ═══");
        ShowFeature(nodeMap, "DeviceVendorName", "Vendor");
        ShowFeature(nodeMap, "DeviceModelName", "Model");
        ShowFeature(nodeMap, "DeviceSerialNumber", "Serial");
        ShowFeature(nodeMap, "DeviceFirmwareVersion", "Firmware");
        
        Console.WriteLine("\n═══ Image Format ═══");
        ShowIntFeature(nodeMap, "Width");
        ShowIntFeature(nodeMap, "Height");
        ShowFeature(nodeMap, "PixelFormat", "Pixel Format");
        
        Console.WriteLine("\n═══ Exposure & Gain ═══");
        ShowFloatFeature(nodeMap, "ExposureTime");
        ShowFloatFeature(nodeMap, "Gain");
        
        Console.WriteLine("\n═══ Acquisition ═══");
        ShowFloatFeature(nodeMap, "AcquisitionFrameRate");
        ShowFeature(nodeMap, "TriggerMode", "Trigger Mode");
        
        Console.WriteLine("\n═══ Feature Modification Test ═══");
        TestModification(nodeMap);
        
        Console.WriteLine("\n✓ Demo complete!");
        Console.WriteLine("\nThis demonstrates:");
        Console.WriteLine("  • String features (DeviceVendorName, PixelFormat, etc.)");
        Console.WriteLine("  • Integer features (Width, Height)");
        Console.WriteLine("  • Float features (ExposureTime, Gain, FrameRate)");
        Console.WriteLine("  • Read-only vs read-write access");
        Console.WriteLine("  • Feature modification with verification");
    }
    
    private static void ShowFeature(NodeMap nodeMap, string name, string? display = null)
    {
        try
        {
            var value = nodeMap.GetStringFeature(name);
            Console.WriteLine($"  {display ?? name,-25} = {value}");
        }
        catch
        {
            Console.WriteLine($"  {display ?? name,-25} = <not available>");
        }
    }
    
    private static void ShowIntFeature(NodeMap nodeMap, string name, string? display = null)
    {
        try
        {
            var value = nodeMap.GetIntegerFeature(name);
            Console.WriteLine($"  {display ?? name,-25} = {value}");
        }
        catch
        {
            Console.WriteLine($"  {display ?? name,-25} = <not available>");
        }
    }
    
    private static void ShowFloatFeature(NodeMap nodeMap, string name, string? display = null)
    {
        try
        {
            var value = nodeMap.GetFloatFeature(name);
            Console.WriteLine($"  {display ?? name,-25} = {value:F2}");
        }
        catch
        {
            Console.WriteLine($"  {display ?? name,-25} = <not available>");
        }
    }
    
    private static void TestModification(NodeMap nodeMap)
    {
        try
        {
            Console.WriteLine("Testing Gain modification:");
            
            var originalGain = nodeMap.GetFloatFeature("Gain");
            Console.WriteLine($"  Original Gain: {originalGain:F2}");
            
            nodeMap.SetFloatFeature("Gain", 6.0);
            var newGain = nodeMap.GetFloatFeature("Gain");
            Console.WriteLine($"  After setting to 6.0: {newGain:F2}");
            
            nodeMap.SetFloatFeature("Gain", originalGain);
            var restoredGain = nodeMap.GetFloatFeature("Gain");
            Console.WriteLine($"  Restored to original: {restoredGain:F2}");
            
            Console.WriteLine("  ✓ Modification successful!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Modification failed: {ex.Message}");
        }
    }
}
