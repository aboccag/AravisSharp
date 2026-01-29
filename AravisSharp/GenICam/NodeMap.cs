using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.GenICam;

/// <summary>
/// Provides access to the GenICam node map for exploring camera features
/// Uses the device API for feature access
/// </summary>
public class NodeMap : IDisposable
{
    private IntPtr _deviceHandle;
    private bool _disposed;
    private IntPtr _genicam;

    internal NodeMap(IntPtr deviceHandle)
    {
        _deviceHandle = deviceHandle;
        _genicam = AravisNative.arv_device_get_genicam(deviceHandle);
    }

    /// <summary>
    /// Gets detailed information about a feature
    /// </summary>
    public FeatureDetails? GetFeatureDetails(string featureName)
    {
        try
        {
            return FeatureDetails.FromNode(_deviceHandle, featureName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all features organized by category
    /// </summary>
    public Dictionary<string, List<FeatureDetails>> GetFeaturesByCategory()
    {
        var categories = new Dictionary<string, List<FeatureDetails>>();
        
        // Standard GenICam categories
        var categoryNames = new[]
        {
            "Root",
            "DeviceControl",
            "ImageFormatControl",
            "AcquisitionControl",
            "AnalogControl",
            "TransportLayerControl",
            "DigitalIOControl",
            "CounterAndTimerControl",
            "LUTControl",
            "AutoFunctionControl",
            "UserSetControl",
            "EventControl",
            "FileAccessControl"
        };

        foreach (var categoryName in categoryNames)
        {
            var features = GetFeaturesInCategory(categoryName);
            if (features.Count > 0)
            {
                categories[categoryName] = features;
            }
        }

        return categories;
    }

    /// <summary>
    /// Gets all features in a specific category
    /// </summary>
    public List<FeatureDetails> GetFeaturesInCategory(string categoryName)
    {
        var features = new List<FeatureDetails>();
        
        try
        {
            if (_genicam == IntPtr.Zero) return features;
            
            var categoryPtr = AravisNative.arv_gc_get_node(_genicam, Marshal.StringToHGlobalAnsi(categoryName));
            if (categoryPtr == IntPtr.Zero) return features;
            
            var featuresPtr = AravisNative.arv_gc_category_get_features(categoryPtr);
            if (featuresPtr == IntPtr.Zero) return features;
            
            // GSList iteration
            var current = featuresPtr;
            while (current != IntPtr.Zero)
            {
                var nodePtr = Marshal.ReadIntPtr(current, 0); // data field
                if (nodePtr != IntPtr.Zero)
                {
                    var namePtr = AravisNative.arv_gc_feature_node_get_name(nodePtr);
                    if (namePtr != IntPtr.Zero)
                    {
                        var name = Marshal.PtrToStringAnsi(namePtr);
                        if (name != null)
                        {
                            var details = GetFeatureDetails(name);
                            if (details != null && details.IsImplemented)
                            {
                                features.Add(details);
                            }
                        }
                    }
                }
                
                // Move to next in list
                current = Marshal.ReadIntPtr(current, IntPtr.Size); // next field
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return features;
    }

    /// <summary>
    /// Gets all available features (comprehensive search)
    /// </summary>
    public List<FeatureDetails> GetAllFeatures()
    {
        var allFeatures = new List<FeatureDetails>();
        var seenNames = new HashSet<string>();
        
        // Get features from all categories
        foreach (var (category, features) in GetFeaturesByCategory())
        {
            foreach (var feature in features)
            {
                if (seenNames.Add(feature.Name))
                {
                    allFeatures.Add(feature);
                }
            }
        }
        
        return allFeatures;
    }

    /// <summary>
    /// Gets a feature node by name (legacy compatibility)
    /// </summary>
    public FeatureInfo? GetNode(string nodeName)
    {
        try
        {
            var value = GetStringFeature(nodeName);
            return new FeatureInfo
            {
                Name = nodeName,
                Value = value,
                IsAvailable = true,
                IsImplemented = true
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets string feature value using device API
    /// </summary>
    public string? GetStringFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            IntPtr valuePtr = AravisNative.arv_device_get_string_feature_value(_deviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
            {
                return null;
            }

            return Marshal.PtrToStringAnsi(valuePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Sets string feature value using device API
    /// </summary>
    public void SetStringFeature(string featureName, string value)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);
        try
        {
            IntPtr error = IntPtr.Zero;
            AravisNative.arv_device_set_string_feature_value(_deviceHandle, namePtr, valuePtr, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to set feature {featureName}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
            Marshal.FreeHGlobal(valuePtr);
        }
    }

    /// <summary>
    /// Gets integer feature value using device API
    /// </summary>
    public long GetIntegerFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            long value = AravisNative.arv_device_get_integer_feature_value(_deviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get feature {featureName}");
            }

            return value;
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Sets integer feature value using device API
    /// </summary>
    public void SetIntegerFeature(string featureName, long value)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            AravisNative.arv_device_set_integer_feature_value(_deviceHandle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to set feature {featureName}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Gets float feature value using device API
    /// </summary>
    public double GetFloatFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            double value = AravisNative.arv_device_get_float_feature_value(_deviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get feature {featureName}");
            }

            return value;
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Sets float feature value using device API
    /// </summary>
    public void SetFloatFeature(string featureName, double value)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            AravisNative.arv_device_set_float_feature_value(_deviceHandle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to set feature {featureName}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Gets boolean feature value using device API
    /// </summary>
    public bool GetBooleanFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            bool value = AravisNative.arv_device_get_boolean_feature_value(_deviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to get feature {featureName}");
            }

            return value;
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Sets boolean feature value using device API
    /// </summary>
    public void SetBooleanFeature(string featureName, bool value)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(featureName);
        try
        {
            IntPtr error = IntPtr.Zero;
            AravisNative.arv_device_set_boolean_feature_value(_deviceHandle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to set feature {featureName}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Executes a command feature
    /// </summary>
    public void ExecuteCommand(string commandName)
    {
        IntPtr namePtr = Marshal.StringToHGlobalAnsi(commandName);
        try
        {
            IntPtr error = IntPtr.Zero;
            AravisNative.arv_device_execute_command(_deviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to execute command {commandName}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// Gets the GenICam XML description (not implemented via device API)
    /// </summary>
    public string? GetGenicamXml()
    {
        // This would require using the genicam object directly
        // For now, return a placeholder
        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Device handle is owned by Camera, don't free it
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Information about a camera feature
/// </summary>
public class FeatureInfo
{
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Value { get; set; }
    public string? Category { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsImplemented { get; set; }
    public bool IsLocked { get; set; }
    public int Depth { get; set; }

    public override string ToString()
    {
        var indent = new string(' ', Depth * 2);
        return $"{indent}{DisplayName ?? Name}: {Value}";
    }
}
