using System;
using System.Linq;
using AravisSharp.GenICam;

namespace AravisSharp.Examples;

/// <summary>
/// Comprehensive GenICam feature explorer showing all features with details
/// </summary>
public static class FeatureBrowserExample
{
    public static void Run()
    {
        Console.WriteLine("=== GenICam Feature Browser ===\n");
        
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
        
        // Display device info
        Console.WriteLine("Camera Information:");
        Console.WriteLine($"  Vendor: {nodeMap.GetStringFeature("DeviceVendorName")}");
        Console.WriteLine($"  Model: {nodeMap.GetStringFeature("DeviceModelName")}");
        Console.WriteLine($"  Serial: {nodeMap.GetStringFeature("DeviceSerialNumber")}\n");
        
        while (true)
        {
            Console.WriteLine("\n=== Feature Browser Menu ===");
            Console.WriteLine("1. Browse by category (tree view)");
            Console.WriteLine("2. List all features (flat view)");
            Console.WriteLine("3. Search for feature");
            Console.WriteLine("4. Show feature details");
            Console.WriteLine("5. Modify feature value");
            Console.WriteLine("0. Exit");
            Console.Write("\nChoice: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    BrowseByCategory(nodeMap);
                    break;
                case "2":
                    ListAllFeatures(nodeMap);
                    break;
                case "3":
                    SearchFeature(nodeMap);
                    break;
                case "4":
                    ShowFeatureDetails(nodeMap);
                    break;
                case "5":
                    ModifyFeature(nodeMap);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid choice");
                    break;
            }
        }
    }
    
    private static void BrowseByCategory(NodeMap nodeMap)
    {
        Console.WriteLine("\n=== Features by Category (Tree View) ===\n");
        
        var categories = nodeMap.GetFeaturesByCategory();
        
        foreach (var (categoryName, features) in categories.OrderBy(c => c.Key))
        {
            Console.WriteLine($"📁 {categoryName}");
            Console.WriteLine(new string('─', 80));
            
            if (features.Count == 0)
            {
                Console.WriteLine("  (no features)");
            }
            else
            {
                foreach (var feature in features.OrderBy(f => f.DisplayName))
                {
                    var icon = GetFeatureIcon(feature);
                    var accessStr = GetAccessString(feature.AccessMode);
                    var valueStr = GetValueString(feature);
                    
                    Console.WriteLine($"  {icon} {feature.DisplayName,-35} [{accessStr}] {feature.Type,-12} = {valueStr}");
                }
            }
            Console.WriteLine();
        }
        
        Console.WriteLine($"\nTotal categories: {categories.Count}");
        Console.WriteLine($"Total features: {categories.Values.Sum(f => f.Count)}");
    }
    
    private static void ListAllFeatures(NodeMap nodeMap)
    {
        Console.WriteLine("\n=== All Features (Flat View) ===\n");
        Console.WriteLine($"{"Feature",-40} {"Access",-8} {"Type",-12} {"Value",-20}");
        Console.WriteLine(new string('─', 100));
        
        var features = nodeMap.GetAllFeatures().OrderBy(f => f.DisplayName).ToList();
        
        foreach (var feature in features)
        {
            var icon = GetFeatureIcon(feature);
            var accessStr = GetAccessString(feature.AccessMode);
            var valueStr = GetValueString(feature);
            
            Console.WriteLine($"{icon} {feature.DisplayName,-38} {accessStr,-8} {feature.Type,-12} {valueStr}");
        }
        
        Console.WriteLine($"\nTotal: {features.Count} features");
        
        // Statistics
        var byType = features.GroupBy(f => f.Type).OrderBy(g => g.Key);
        Console.WriteLine("\nBy Type:");
        foreach (var group in byType)
        {
            Console.WriteLine($"  {group.Key,-15} {group.Count(),3} features");
        }
        
        var byAccess = features.GroupBy(f => f.AccessMode).OrderBy(g => g.Key);
        Console.WriteLine("\nBy Access Mode:");
        foreach (var group in byAccess)
        {
            Console.WriteLine($"  {group.Key,-15} {group.Count(),3} features");
        }
    }
    
    private static void SearchFeature(NodeMap nodeMap)
    {
        Console.Write("\nEnter search term: ");
        var searchTerm = Console.ReadLine()?.ToLower() ?? "";
        
        if (string.IsNullOrEmpty(searchTerm))
            return;
        
        var features = nodeMap.GetAllFeatures()
            .Where(f => f.Name.ToLower().Contains(searchTerm) || 
                       f.DisplayName.ToLower().Contains(searchTerm) ||
                       f.Description.ToLower().Contains(searchTerm))
            .OrderBy(f => f.DisplayName)
            .ToList();
        
        Console.WriteLine($"\nFound {features.Count} matching features:\n");
        
        foreach (var feature in features)
        {
            var icon = GetFeatureIcon(feature);
            var accessStr = GetAccessString(feature.AccessMode);
            var valueStr = GetValueString(feature);
            
            Console.WriteLine($"{icon} {feature.DisplayName,-35} [{accessStr}] = {valueStr}");
            if (!string.IsNullOrEmpty(feature.Description))
            {
                Console.WriteLine($"    {feature.Description}");
            }
            Console.WriteLine();
        }
    }
    
    private static void ShowFeatureDetails(NodeMap nodeMap)
    {
        Console.Write("\nEnter feature name: ");
        var featureName = Console.ReadLine();
        
        if (string.IsNullOrEmpty(featureName))
            return;
        
        var feature = nodeMap.GetFeatureDetails(featureName);
        
        if (feature == null)
        {
            Console.WriteLine($"Feature '{featureName}' not found!");
            return;
        }
        
        Console.WriteLine($"\n=== Feature Details: {feature.DisplayName} ===\n");
        Console.WriteLine($"Name:          {feature.Name}");
        Console.WriteLine($"Display Name:  {feature.DisplayName}");
        Console.WriteLine($"Type:          {feature.Type}");
        Console.WriteLine($"Access Mode:   {feature.AccessMode}");
        Console.WriteLine($"Visibility:    {feature.Visibility}");
        Console.WriteLine($"Available:     {feature.IsAvailable}");
        Console.WriteLine($"Implemented:   {feature.IsImplemented}");
        Console.WriteLine($"Locked:        {feature.IsLocked}");
        Console.WriteLine($"Current Value: {feature.CurrentValue ?? "<n/a>"}");
        
        if (!string.IsNullOrEmpty(feature.Description))
            Console.WriteLine($"Description:   {feature.Description}");
        
        if (!string.IsNullOrEmpty(feature.Tooltip))
            Console.WriteLine($"Tooltip:       {feature.Tooltip}");
        
        // Type-specific constraints
        if (feature.Type == FeatureType.Integer && feature.IntMin.HasValue)
        {
            Console.WriteLine($"\nInteger Constraints:");
            Console.WriteLine($"  Min:       {feature.IntMin}");
            Console.WriteLine($"  Max:       {feature.IntMax}");
            Console.WriteLine($"  Increment: {feature.IntIncrement}");
        }
        
        if (feature.Type == FeatureType.Float && feature.FloatMin.HasValue)
        {
            Console.WriteLine($"\nFloat Constraints:");
            Console.WriteLine($"  Min:       {feature.FloatMin:F2}");
            Console.WriteLine($"  Max:       {feature.FloatMax:F2}");
            Console.WriteLine($"  Increment: {feature.FloatIncrement:F6}");
        }
        
        if (feature.Type == FeatureType.Enumeration && feature.EnumChoices.Count > 0)
        {
            Console.WriteLine($"\nEnumeration Choices ({feature.EnumChoices.Count}):");
            for (int i = 0; i < feature.EnumChoices.Count; i++)
            {
                var choice = feature.EnumChoices[i];
                var isCurrent = choice == feature.CurrentValue;
                var marker = isCurrent ? " ← current" : "";
                
                if (i < feature.EnumDisplayNames.Count && feature.EnumDisplayNames[i] != choice)
                {
                    Console.WriteLine($"  {choice,-25} ({feature.EnumDisplayNames[i]}){marker}");
                }
                else
                {
                    Console.WriteLine($"  {choice}{marker}");
                }
            }
        }
    }
    
    private static void ModifyFeature(NodeMap nodeMap)
    {
        Console.Write("\nEnter feature name: ");
        var featureName = Console.ReadLine();
        
        if (string.IsNullOrEmpty(featureName))
            return;
        
        var feature = nodeMap.GetFeatureDetails(featureName);
        
        if (feature == null)
        {
            Console.WriteLine($"Feature '{featureName}' not found!");
            return;
        }
        
        if (feature.AccessMode == FeatureAccessMode.ReadOnly)
        {
            Console.WriteLine($"Feature '{featureName}' is read-only!");
            return;
        }
        
        if (!feature.IsAvailable)
        {
            Console.WriteLine($"Feature '{featureName}' is not available!");
            return;
        }
        
        Console.WriteLine($"\nCurrent value: {feature.CurrentValue}");
        
        if (feature.Type == FeatureType.Enumeration && feature.EnumChoices.Count > 0)
        {
            Console.WriteLine("\nAvailable choices:");
            for (int i = 0; i < feature.EnumChoices.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {feature.EnumChoices[i]}");
            }
            Console.Write("\nSelect choice (number): ");
            var choiceStr = Console.ReadLine();
            if (int.TryParse(choiceStr, out int choiceIdx) && choiceIdx > 0 && choiceIdx <= feature.EnumChoices.Count)
            {
                var newValue = feature.EnumChoices[choiceIdx - 1];
                try
                {
                    nodeMap.SetStringFeature(featureName, newValue);
                    Console.WriteLine($"✓ Set {featureName} = {newValue}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
        }
        else if (feature.Type == FeatureType.Integer)
        {
            if (feature.IntMin.HasValue)
                Console.WriteLine($"Range: {feature.IntMin} to {feature.IntMax}, increment: {feature.IntIncrement}");
            
            Console.Write("New value: ");
            var valueStr = Console.ReadLine();
            if (long.TryParse(valueStr, out long value))
            {
                try
                {
                    nodeMap.SetIntegerFeature(featureName, value);
                    Console.WriteLine($"✓ Set {featureName} = {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
        }
        else if (feature.Type == FeatureType.Float)
        {
            if (feature.FloatMin.HasValue)
                Console.WriteLine($"Range: {feature.FloatMin:F2} to {feature.FloatMax:F2}");
            
            Console.Write("New value: ");
            var valueStr = Console.ReadLine();
            if (double.TryParse(valueStr, out double value))
            {
                try
                {
                    nodeMap.SetFloatFeature(featureName, value);
                    Console.WriteLine($"✓ Set {featureName} = {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
        }
        else if (feature.Type == FeatureType.Boolean)
        {
            Console.Write("New value (true/false): ");
            var valueStr = Console.ReadLine()?.ToLower();
            if (valueStr == "true" || valueStr == "false")
            {
                try
                {
                    nodeMap.SetBooleanFeature(featureName, valueStr == "true");
                    Console.WriteLine($"✓ Set {featureName} = {valueStr}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
        }
        else
        {
            Console.Write("New value: ");
            var newValue = Console.ReadLine();
            if (!string.IsNullOrEmpty(newValue))
            {
                try
                {
                    nodeMap.SetStringFeature(featureName, newValue);
                    Console.WriteLine($"✓ Set {featureName} = {newValue}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }
        }
    }
    
    private static string GetFeatureIcon(FeatureDetails feature)
    {
        if (!feature.IsAvailable) return "○";
        if (feature.IsLocked) return "🔒";
        
        return feature.Type switch
        {
            FeatureType.Integer => "🔢",
            FeatureType.Float => "📊",
            FeatureType.String => "📝",
            FeatureType.Boolean => "☑️",
            FeatureType.Command => "▶️",
            FeatureType.Enumeration => "📋",
            FeatureType.Category => "📁",
            _ => "•"
        };
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
    
    private static string GetValueString(FeatureDetails feature)
    {
        if (!feature.IsAvailable)
            return "<not available>";
        
        if (string.IsNullOrEmpty(feature.CurrentValue))
            return "<n/a>";
        
        if (feature.Type == FeatureType.Enumeration && feature.EnumChoices.Count > 0)
        {
            var idx = feature.EnumChoices.IndexOf(feature.CurrentValue);
            if (idx >= 0 && idx < feature.EnumDisplayNames.Count && feature.EnumDisplayNames[idx] != feature.CurrentValue)
            {
                return $"{feature.CurrentValue} ({feature.EnumDisplayNames[idx]})";
            }
        }
        
        return feature.CurrentValue;
    }
}
