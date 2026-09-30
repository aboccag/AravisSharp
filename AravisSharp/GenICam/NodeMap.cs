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
    // The genicam object belongs to the device: holding the Device keeps both alive.
    private readonly Device _device;
    private bool _disposed;
    private IntPtr _genicam;

    internal NodeMap(Device device)
    {
        _device = device;
        _genicam = AravisNative.arv_device_get_genicam(device.Handle);
    }

    private IntPtr DeviceHandle => _device.Handle;

    // The genicam object is owned by the device: check the device first, so a disposed
    // Device throws ObjectDisposedException instead of handing out a dangling pointer.
    private IntPtr Genicam
    {
        get
        {
            _ = _device.Handle;
            return _genicam;
        }
    }

    /// <summary>
    /// Gets detailed information about a feature
    /// </summary>
    public FeatureDetails? GetFeatureDetails(string featureName)
    {
        var deviceHandle = DeviceHandle;
        try
        {
            var details = FeatureDetails.FromNode(deviceHandle, featureName);
            GC.KeepAlive(_device);
            return details;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all features organized by category, walking the category tree from "Root".
    /// Each category maps to the features directly under it; subcategories get their own entry.
    /// </summary>
    public Dictionary<string, List<FeatureDetails>> GetFeaturesByCategory()
    {
        var categories = new Dictionary<string, List<FeatureDetails>>();
        CollectCategory("Root", categories, new HashSet<string>());
        return categories;
    }

    private void CollectCategory(string categoryName, Dictionary<string, List<FeatureDetails>> categories, HashSet<string> visited)
    {
        if (!visited.Add(categoryName))
            return;

        var features = new List<FeatureDetails>();
        foreach (var details in GetFeaturesInCategory(categoryName))
        {
            if (details.Type == FeatureType.Category)
                CollectCategory(details.Name, categories, visited);
            else
                features.Add(details);
        }

        if (features.Count > 0)
            categories[categoryName] = features;
    }

    /// <summary>
    /// Gets the features directly in a specific category, subcategories included
    /// </summary>
    public List<FeatureDetails> GetFeaturesInCategory(string categoryName)
    {
        var features = new List<FeatureDetails>();
        var genicam = Genicam;
        
        try
        {
            if (genicam == IntPtr.Zero) return features;
            
            var categoryNamePtr = Marshal.StringToCoTaskMemUTF8(categoryName);
            IntPtr categoryPtr;
            try
            {
                categoryPtr = AravisNative.arv_gc_get_node(genicam, categoryNamePtr);
            }
            finally
            {
                Marshal.FreeCoTaskMem(categoryNamePtr);
            }
            if (categoryPtr == IntPtr.Zero) return features;
            if (!GLibNative.g_type_check_instance_is_a(categoryPtr, AravisNative.arv_gc_category_get_type())) return features;
            
            var featuresPtr = AravisNative.arv_gc_category_get_features(categoryPtr);
            if (featuresPtr == IntPtr.Zero) return features;

            // arv_gc_category_get_features returns a GSList of const char* (feature name strings),
            // NOT GObject/ArvGcFeatureNode pointers. Each data field is a UTF-8 feature name.
            // Copy the names first: the list belongs to the category node.
            var names = new List<string>();
            var current = featuresPtr;
            while (current != IntPtr.Zero)
            {
                var nameStringPtr = Marshal.ReadIntPtr(current, 0); // data = const char*
                var name = nameStringPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(nameStringPtr) : null;
                if (name != null)
                    names.Add(name);

                current = Marshal.ReadIntPtr(current, IntPtr.Size); // next field
            }

            foreach (var name in names)
            {
                var details = GetFeatureDetails(name);
                if (details != null && details.IsImplemented)
                {
                    features.Add(details);
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        finally
        {
            GC.KeepAlive(_device);
        }
        
        return features;
    }

    /// <summary>
    /// Gets all available features, walking every category reachable from "Root"
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
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            IntPtr valuePtr = AravisNative.arv_device_get_string_feature_value(DeviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                return null;

            return Marshal.PtrToStringUTF8(valuePtr);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Sets string feature value using device API
    /// </summary>
    public void SetStringFeature(string featureName, string value)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr valuePtr = Marshal.StringToCoTaskMemUTF8(value);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_string_feature_value(DeviceHandle, namePtr, valuePtr, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to set feature {featureName}");
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
            Marshal.FreeCoTaskMem(valuePtr);
        }
    }

    /// <summary>
    /// Gets integer feature value using device API
    /// </summary>
    public long GetIntegerFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            long value = AravisNative.arv_device_get_integer_feature_value(DeviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to get feature {featureName}");

            return value;
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Sets integer feature value using device API
    /// </summary>
    public void SetIntegerFeature(string featureName, long value)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_integer_feature_value(DeviceHandle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to set feature {featureName}");
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Gets float feature value using device API
    /// </summary>
    public double GetFloatFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            double value = AravisNative.arv_device_get_float_feature_value(DeviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to get feature {featureName}");

            return value;
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Sets float feature value using device API
    /// </summary>
    public void SetFloatFeature(string featureName, double value)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_float_feature_value(DeviceHandle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to set feature {featureName}");
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Gets boolean feature value using device API
    /// </summary>
    public bool GetBooleanFeature(string featureName)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            bool value = AravisNative.arv_device_get_boolean_feature_value(DeviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to get feature {featureName}");

            return value;
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Sets boolean feature value using device API
    /// </summary>
    public void SetBooleanFeature(string featureName, bool value)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_boolean_feature_value(DeviceHandle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to set feature {featureName}");
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Executes a command feature
    /// </summary>
    public void ExecuteCommand(string commandName)
    {
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(commandName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_execute_command(DeviceHandle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw new InvalidOperationException($"Failed to execute command {commandName}");
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            GC.KeepAlive(_device);
        }
    }

    /// <summary>
    /// Gets the GenICam XML description of the device, or null if it has none
    /// </summary>
    public string? GetGenicamXml()
    {
        var xmlPtr = AravisNative.arv_device_get_genicam_xml(DeviceHandle, out UIntPtr size);
        var xml = xmlPtr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(xmlPtr, checked((int)size)).TrimEnd('\0');
        GC.KeepAlive(_device);
        return xml;
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
