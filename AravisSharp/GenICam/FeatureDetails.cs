using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.GenICam;

/// <summary>
/// Detailed information about a GenICam feature including metadata, constraints, and choices
/// </summary>
public class FeatureDetails
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tooltip { get; set; } = string.Empty;
    public FeatureType Type { get; set; }
    public FeatureAccessMode AccessMode { get; set; }
    public FeatureVisibility Visibility { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsImplemented { get; set; }
    public bool IsLocked { get; set; }
    public string? CurrentValue { get; set; }
    
    // For numeric features
    public long? IntMin { get; set; }
    public long? IntMax { get; set; }
    public long? IntIncrement { get; set; }
    public double? FloatMin { get; set; }
    public double? FloatMax { get; set; }
    public double? FloatIncrement { get; set; }
    
    // For enumeration features
    public List<string> EnumChoices { get; set; } = new();
    public List<string> EnumDisplayNames { get; set; } = new();
    
    /// <summary>
    /// Get detailed information about a feature from its GenICam node
    /// </summary>
    public static FeatureDetails FromNode(IntPtr device, string featureName)
    {
        var details = new FeatureDetails { Name = featureName };
        
        try
        {
            var gc = AravisNative.arv_device_get_genicam(device);
            if (gc == IntPtr.Zero) return details;
            
            var nodePtr = AravisNative.arv_gc_get_node(gc, Marshal.StringToHGlobalAnsi(featureName));
            if (nodePtr == IntPtr.Zero) return details;
            
            // Get display name
            var displayNamePtr = AravisNative.arv_gc_feature_node_get_display_name(nodePtr);
            if (displayNamePtr != IntPtr.Zero)
                details.DisplayName = Marshal.PtrToStringAnsi(displayNamePtr) ?? featureName;
            else
                details.DisplayName = featureName;
            
            // Get description
            var descPtr = AravisNative.arv_gc_feature_node_get_description(nodePtr);
            if (descPtr != IntPtr.Zero)
                details.Description = Marshal.PtrToStringAnsi(descPtr) ?? string.Empty;
            
            // Get tooltip
            var tooltipPtr = AravisNative.arv_gc_feature_node_get_tooltip(nodePtr);
            if (tooltipPtr != IntPtr.Zero)
                details.Tooltip = Marshal.PtrToStringAnsi(tooltipPtr) ?? string.Empty;
            
            // Get access mode (returns ArvGcAccessMode enum: RO=0, WO=1, RW=2, UNDEFINED=-1)
            var accessModeValue = AravisNative.arv_gc_feature_node_get_actual_access_mode(nodePtr);
            details.AccessMode = ParseAccessMode((int)accessModeValue);
            
            // Get visibility (returns ArvGcVisibility enum: INVISIBLE=0, GURU=1, EXPERT=2, BEGINNER=3, UNDEFINED=-1)
            var visibilityValue = AravisNative.arv_gc_feature_node_get_visibility(nodePtr);
            details.Visibility = ParseVisibility((int)visibilityValue);
            
            // Get availability
            IntPtr error = IntPtr.Zero;
            details.IsAvailable = AravisNative.arv_gc_feature_node_is_available(nodePtr, out error);
            details.IsImplemented = AravisNative.arv_gc_feature_node_is_implemented(nodePtr, out error);
            details.IsLocked = AravisNative.arv_gc_feature_node_is_locked(nodePtr, out error);
            
            // Get current value as string
            if (details.IsAvailable)
            {
                var valuePtr = AravisNative.arv_gc_feature_node_get_value_as_string(nodePtr, out error);
                if (valuePtr != IntPtr.Zero)
                    details.CurrentValue = Marshal.PtrToStringAnsi(valuePtr);
            }
            
            // Determine feature type and get type-specific info
            DetermineTypeAndConstraints(device, featureName, details);
        }
        catch
        {
            // Ignore errors for individual features
        }
        
        return details;
    }
    
    private static void DetermineTypeAndConstraints(IntPtr device, string featureName, FeatureDetails details)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        IntPtr error = IntPtr.Zero;
        
        try
        {
            // Try integer
            try
            {
                var value = AravisNative.arv_device_get_integer_feature_value(device, namePtr, out error);
                if (error == IntPtr.Zero)
                {
                    details.Type = FeatureType.Integer;
                    AravisNative.arv_device_get_integer_feature_bounds(device, namePtr, out long min, out long max, out error);
                    if (error == IntPtr.Zero)
                    {
                        details.IntMin = min;
                        details.IntMax = max;
                    }
                    details.IntIncrement = AravisNative.arv_device_get_integer_feature_increment(device, namePtr, out error);
                    return;
                }
            }
            catch { }
            
            // Try float
            try
            {
                var value = AravisNative.arv_device_get_float_feature_value(device, namePtr, out error);
                if (error == IntPtr.Zero)
                {
                    details.Type = FeatureType.Float;
                    AravisNative.arv_device_get_float_feature_bounds(device, namePtr, out double min, out double max, out error);
                    if (error == IntPtr.Zero)
                    {
                        details.FloatMin = min;
                        details.FloatMax = max;
                    }
                    details.FloatIncrement = AravisNative.arv_device_get_float_feature_increment(device, namePtr, out error);
                    return;
                }
            }
            catch { }
            
            // Try boolean
            try
            {
                var value = AravisNative.arv_device_get_boolean_feature_value(device, namePtr, out error);
                if (error == IntPtr.Zero)
                {
                    details.Type = FeatureType.Boolean;
                    return;
                }
            }
            catch { }
            
            // Try enumeration (check for available choices)
            try
            {
                uint count = 0;
                var choicesPtr = AravisNative.arv_device_dup_available_enumeration_feature_values_as_strings(device, namePtr, out count, out error);
                if (choicesPtr != IntPtr.Zero && count > 0 && error == IntPtr.Zero)
                {
                    details.Type = FeatureType.Enumeration;
                    
                    // Read string choices
                    for (uint i = 0; i < count; i++)
                    {
                        var strPtr = Marshal.ReadIntPtr(choicesPtr, (int)i * IntPtr.Size);
                        if (strPtr != IntPtr.Zero)
                        {
                            var choice = Marshal.PtrToStringAnsi(strPtr);
                            if (choice != null)
                                details.EnumChoices.Add(choice);
                        }
                    }
                    
                    // Try to get display names
                    var displayNamesPtr = AravisNative.arv_device_dup_available_enumeration_feature_values_as_display_names(device, namePtr, out count, out error);
                    if (displayNamesPtr != IntPtr.Zero && error == IntPtr.Zero)
                    {
                        for (uint i = 0; i < count; i++)
                        {
                            var strPtr = Marshal.ReadIntPtr(displayNamesPtr, (int)i * IntPtr.Size);
                            if (strPtr != IntPtr.Zero)
                            {
                                var displayName = Marshal.PtrToStringAnsi(strPtr);
                                if (displayName != null)
                                    details.EnumDisplayNames.Add(displayName);
                            }
                        }
                    }
                    
                    return;
                }
            }
            catch { }
            
            // Default to string or command
            if (details.CurrentValue != null)
                details.Type = FeatureType.String;
            else
                details.Type = FeatureType.Command;
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }
    
    private static FeatureAccessMode ParseAccessMode(int arvAccessMode)
    {
        // ArvGcAccessMode: UNDEFINED=-1, RO=0, WO=1, RW=2
        return arvAccessMode switch
        {
            0 => FeatureAccessMode.ReadOnly,
            1 => FeatureAccessMode.WriteOnly,
            2 => FeatureAccessMode.ReadWrite,
            _ => FeatureAccessMode.Undefined
        };
    }
    
    private static FeatureVisibility ParseVisibility(int arvVisibility)
    {
        // ArvGcVisibility: UNDEFINED=-1, INVISIBLE=0, GURU=1, EXPERT=2, BEGINNER=3
        return arvVisibility switch
        {
            0 => FeatureVisibility.Invisible,
            1 => FeatureVisibility.Guru,
            2 => FeatureVisibility.Expert,
            3 => FeatureVisibility.Beginner,
            _ => FeatureVisibility.Undefined
        };
    }
    
    public override string ToString()
    {
        var accessStr = AccessMode switch
        {
            FeatureAccessMode.ReadWrite => "RW",
            FeatureAccessMode.ReadOnly => "RO",
            FeatureAccessMode.WriteOnly => "WO",
            FeatureAccessMode.NotAvailable => "NA",
            FeatureAccessMode.NotImplemented => "NI",
            _ => "??"
        };
        
        var typeStr = Type.ToString().PadRight(12);
        var valueStr = CurrentValue ?? "<n/a>";
        
        return $"[{accessStr}] {typeStr} {DisplayName.PadRight(30)} = {valueStr}";
    }
}
