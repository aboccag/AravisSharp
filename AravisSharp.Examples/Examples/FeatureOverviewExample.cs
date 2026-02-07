using System;
using System.Linq;
using AravisSharp.GenICam;

namespace AravisSharp.Examples;

/// <summary>
/// Demonstrates comprehensive feature exploration with categorized display
/// Shows all feature capabilities: access modes, types, choices, constraints, and categories
/// </summary>
public static class FeatureOverviewExample
{
    public static void Run()
    {
        Console.WriteLine("=== GenICam Feature Overview ===\n");
        
        // Find camera
        CameraDiscovery.UpdateDeviceList();
        var count = CameraDiscovery.GetDeviceCount();
        
        if (count == 0)
        {
            Console.WriteLine("No cameras found!");
            return;
        }
        
        using var camera = new Camera(null);
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        
        // Device information
        Console.WriteLine("═══ CAMERA INFORMATION ═══");
        ShowDeviceInfo(nodeMap);
        
        // Category: Image Format Control
        Console.WriteLine("\n═══ IMAGE FORMAT CONTROL ═══");
        ShowImageFormat(nodeMap);
        
        // Category: Analog Control (Exposure & Gain)
        Console.WriteLine("\n═══ ANALOG CONTROL (Exposure & Gain) ═══");
        ShowAnalogControl(nodeMap);
        
        // Category: Acquisition Control
        Console.WriteLine("\n═══ ACQUISITION CONTROL ═══");
        ShowAcquisitionControl(nodeMap);
        
        // Example: Enumeration features with choices
        Console.WriteLine("\n═══ ENUMERATION FEATURES (with choices) ═══");
        ShowEnumerationFeatures(nodeMap);
        
        // Example: Feature modification
        Console.WriteLine("\n═══ FEATURE MODIFICATION EXAMPLE ═══");
        DemonstrateFeatureModification(nodeMap);
        
        Console.WriteLine("\n✓ Feature overview complete!");
        Console.WriteLine("\nThis demonstrates:");
        Console.WriteLine("  ✓ Access modes (RO, RW, WO)");
        Console.WriteLine("  ✓ Feature types (Integer, Float, String, Boolean, Enumeration)");
        Console.WriteLine("  ✓ Enumeration choices with display names");
        Console.WriteLine("  ✓ Numeric constraints (min/max/increment)");
        Console.WriteLine("  ✓ Category organization");
        Console.WriteLine("  ✓ Feature metadata (display name, description)");
        Console.WriteLine("  ✓ Current values");
        Console.WriteLine("  ✓ Feature modification (read and write)");
    }
    
    private static void ShowDeviceInfo(NodeMap nodeMap)
    {
        var features = new[]
        {
            "DeviceVendorName",
            "DeviceModelName",
            "DeviceSerialNumber",
            "DeviceFirmwareVersion"
        };
        
        foreach (var featureName in features)
        {
            var details = nodeMap.GetFeatureDetails(featureName);
            if (details != null && details.IsImplemented)
            {
                var accessStr = GetAccessString(details.AccessMode);
                var value = details.CurrentValue ?? nodeMap.GetStringFeature(featureName);
                Console.WriteLine($"  [{accessStr}] {details.DisplayName,-30} = {value}");
            }
        }
    }
    
    private static void ShowImageFormat(NodeMap nodeMap)
    {
        var features = new[]
        {
            new { Name = "Width", IsInt = true },
            new { Name = "Height", IsInt = true },
            new { Name = "OffsetX", IsInt = true },
            new { Name = "OffsetY", IsInt = true },
            new { Name = "PixelFormat", IsInt = false }
        };
        
        foreach (var feature in features)
        {
            var details = nodeMap.GetFeatureDetails(feature.Name);
            if (details != null && details.IsImplemented)
            {
                var accessStr = GetAccessString(details.AccessMode);
                string value;
                string constraints = "";
                
                if (feature.IsInt)
                {
                    try
                    {
                        var intValue = nodeMap.GetIntegerFeature(feature.Name);
                        value = intValue.ToString();
                        
                        if (details.IntMin.HasValue && details.IntMax.HasValue)
                        {
                            constraints = $" (range: {details.IntMin}..{details.IntMax}, step: {details.IntIncrement})";
                        }
                    }
                    catch
                    {
                        value = "<n/a>";
                    }
                }
                else
                {
                    value = details.CurrentValue ?? nodeMap.GetStringFeature(feature.Name) ?? "<n/a>";
                }
                
                Console.WriteLine($"  [{accessStr}] {details.DisplayName,-30} = {value}{constraints}");
            }
        }
    }
    
    private static void ShowAnalogControl(NodeMap nodeMap)
    {
        var floatFeatures = new[] { "ExposureTime", "Gain" };
        
        foreach (var featureName in floatFeatures)
        {
            var details = nodeMap.GetFeatureDetails(featureName);
            if (details != null && details.IsImplemented)
            {
                var accessStr = GetAccessString(details.AccessMode);
                string value = "";
                string constraints = "";
                
                try
                {
                    var floatValue = nodeMap.GetFloatFeature(featureName);
                    value = $"{floatValue:F2}";
                    
                    if (details.FloatMin.HasValue && details.FloatMax.HasValue)
                    {
                        constraints = $" (range: {details.FloatMin:F2}..{details.FloatMax:F2})";
                    }
                }
                catch
                {
                    value = "<n/a>";
                }
                
                Console.WriteLine($"  [{accessStr}] {details.DisplayName,-30} = {value}{constraints}");
            }
        }
        
        // Auto features (enumerations)
        var enumFeatures = new[] { "ExposureAuto", "GainAuto" };
        foreach (var featureName in enumFeatures)
        {
            var details = nodeMap.GetFeatureDetails(featureName);
            if (details != null && details.IsImplemented && details.EnumChoices.Count > 0)
            {
                var accessStr = GetAccessString(details.AccessMode);
                var value = nodeMap.GetStringFeature(featureName) ?? "<n/a>";
                var choices = string.Join(", ", details.EnumChoices);
                Console.WriteLine($"  [{accessStr}] {details.DisplayName,-30} = {value} (choices: {choices})");
            }
        }
    }
    
    private static void ShowAcquisitionControl(NodeMap nodeMap)
    {
        var features = new[]
        {
            "AcquisitionMode",
            "AcquisitionFrameRate"
        };
        
        foreach (var featureName in features)
        {
            var details = nodeMap.GetFeatureDetails(featureName);
            if (details != null && details.IsImplemented)
            {
                var accessStr = GetAccessString(details.AccessMode);
                string value = "";
                
                if (details.Type == FeatureType.Float)
                {
                    try
                    {
                        var floatValue = nodeMap.GetFloatFeature(featureName);
                        value = $"{floatValue:F2}";
                    }
                    catch
                    {
                        value = "<n/a>";
                    }
                }
                else
                {
                    value = nodeMap.GetStringFeature(featureName) ?? "<n/a>";
                }
                
                if (details.EnumChoices.Count > 0)
                {
                    var choices = string.Join(", ", details.EnumChoices);
                    Console.WriteLine($"  [{accessStr}] {details.DisplayName,-30} = {value} (choices: {choices})");
                }
                else
                {
                    Console.WriteLine($"  [{accessStr}] {details.DisplayName,-30} = {value}");
                }
            }
        }
    }
    
    private static void ShowEnumerationFeatures(NodeMap nodeMap)
    {
        var enumFeatures = new[] { "PixelFormat", "TriggerMode", "TriggerSource" };
        
        foreach (var featureName in enumFeatures)
        {
            var details = nodeMap.GetFeatureDetails(featureName);
            if (details != null && details.IsImplemented && details.EnumChoices.Count > 0)
            {
                var accessStr = GetAccessString(details.AccessMode);
                var currentValue = nodeMap.GetStringFeature(featureName) ?? details.CurrentValue ?? "<n/a>";
                
                Console.WriteLine($"\n  {details.DisplayName} [{accessStr}]");
                Console.WriteLine($"    Current: {currentValue}");
                Console.WriteLine($"    Choices ({details.EnumChoices.Count}):");
                
                for (int i = 0; i < details.EnumChoices.Count; i++)
                {
                    var choice = details.EnumChoices[i];
                    var isCurrent = choice == currentValue;
                    var marker = isCurrent ? " ← current" : "";
                    
                    if (i < details.EnumDisplayNames.Count && details.EnumDisplayNames[i] != choice)
                    {
                        Console.WriteLine($"      • {choice} ({details.EnumDisplayNames[i]}){marker}");
                    }
                    else
                    {
                        Console.WriteLine($"      • {choice}{marker}");
                    }
                }
            }
        }
    }
    
    private static void DemonstrateFeatureModification(NodeMap nodeMap)
    {
        // Test 1: Modify gain
        var gainDetails = nodeMap.GetFeatureDetails("Gain");
        if (gainDetails != null && gainDetails.AccessMode == FeatureAccessMode.ReadWrite)
        {
            var originalGain = nodeMap.GetFloatFeature("Gain");
            Console.WriteLine($"\n  Gain modification test:");
            Console.WriteLine($"    Original value: {originalGain:F2}");
            Console.WriteLine($"    Range: {gainDetails.FloatMin:F2} to {gainDetails.FloatMax:F2}");
            
            var newGain = Math.Min(6.0, gainDetails.FloatMax ?? 6.0);
            nodeMap.SetFloatFeature("Gain", newGain);
            var readBack = nodeMap.GetFloatFeature("Gain");
            Console.WriteLine($"    Set to: {newGain:F2}");
            Console.WriteLine($"    Read back: {readBack:F2}");
            
            // Restore original
            nodeMap.SetFloatFeature("Gain", originalGain);
            Console.WriteLine($"    Restored to: {originalGain:F2}");
            Console.WriteLine($"    ✓ Modification successful!");
        }
        
        // Test 2: Show read-only feature
        var vendorDetails = nodeMap.GetFeatureDetails("DeviceVendorName");
        if (vendorDetails != null)
        {
            Console.WriteLine($"\n  Read-only feature example:");
            Console.WriteLine($"    {vendorDetails.DisplayName}: {nodeMap.GetStringFeature(vendorDetails.Name)}");
            Console.WriteLine($"    Access mode: {vendorDetails.AccessMode} (cannot be modified)");
        }
    }
    
    private static string GetAccessString(FeatureAccessMode mode)
    {
        return mode switch
        {
            FeatureAccessMode.ReadWrite => "RW",
            FeatureAccessMode.ReadOnly => "RO",
            FeatureAccessMode.WriteOnly => "WO",
            FeatureAccessMode.NotAvailable => "NA",
            FeatureAccessMode.NotImplemented => "NI",
            _ => "??"
        };
    }
}
