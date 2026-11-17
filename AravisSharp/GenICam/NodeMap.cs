using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.GenICam;

/// <summary>
/// Provides access to the GenICam node map for exploring camera features
/// Uses the device API for feature access
/// </summary>
/// <remarks>
/// A node map belongs to the <see cref="Camera"/> its <see cref="Device"/> came from: its
/// calls are serialised with the camera's, and once the camera or the Device is disposed
/// every member throws <see cref="ObjectDisposedException"/>, including those that return
/// null or an empty list when Aravis reports an error. Members that walk several features
/// (<see cref="GetFeaturesInCategory"/>, <see cref="GetFeaturesByCategory"/>,
/// <see cref="GetAllFeatures"/>) can throw it part-way. <see cref="Dispose"/> does nothing:
/// a node map stays usable until its camera or Device is disposed.
/// </remarks>
public class NodeMap : IDisposable
{
    // The genicam object belongs to the device, itself owned by the camera: every native
    // call takes the Device's lease, which pins the camera and holds its lock.
    private readonly Device _device;
    private readonly IntPtr _genicam;

    internal NodeMap(Device device)
    {
        _device = device;
        using var lease = device.AcquireHandle();
        _genicam = AravisNative.arv_device_get_genicam(lease.Handle);
    }

    // The feature accessors below report failures as InvalidOperationException (their
    // existing contract); the native GError travels along as the AravisException inner
    // exception, so its message, domain and code are not lost.
    private static InvalidOperationException Failure(string message, ref IntPtr error)
    {
        var inner = AravisException.TakeGError(ref error);
        return new InvalidOperationException($"{message}: {inner.Message}", inner);
    }

    /// <summary>
    /// Gets detailed information about a feature
    /// </summary>
    public FeatureDetails? GetFeatureDetails(string featureName)
    {
        using var lease = _device.AcquireHandle();
        try
        {
            return FeatureDetails.FromNode(lease.Handle, featureName);
        }
        catch (Exception ex) when (FeatureDetails.IsOptionalMetadataFailure(ex))
        {
            // No details for this feature; anything else (a disposed device, out of memory,
            // a marshalling error) propagates.
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
        using var lease = _device.AcquireHandle();
        var genicam = _genicam;
        
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
        catch (Exception ex) when (FeatureDetails.IsOptionalMetadataFailure(ex))
        {
            // An Aravis error or a missing metadata entry point while walking the category:
            // return what was collected so far. Anything else propagates.
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
    /// Gets a feature node by name (legacy compatibility).
    /// </summary>
    /// <returns>
    /// <c>null</c> when the device has no node of that name. Otherwise a
    /// <see cref="FeatureInfo"/> whose <see cref="FeatureInfo.Value"/> is the string value of
    /// the feature, or <c>null</c> when it cannot be read as a string (an integer feature
    /// such as <c>Width</c>, for example).
    /// </returns>
    /// <exception cref="ObjectDisposedException">The device has been disposed.</exception>
    public FeatureInfo? GetNode(string nodeName)
    {
        if (!HasNode(nodeName))
            return null;

        return new FeatureInfo
        {
            Name = nodeName,
            Value = GetStringFeature(nodeName),
            IsAvailable = true,
            IsImplemented = true
        };
    }

    private bool HasNode(string nodeName)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(nodeName);
        try
        {
            // Transfer none: the node belongs to the device's ArvGc, never unref it.
            return AravisNative.arv_device_get_feature(lease.Handle, namePtr) != IntPtr.Zero;
        }
        finally
        {
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Gets string feature value using device API
    /// </summary>
    public string? GetStringFeature(string featureName)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            IntPtr valuePtr = AravisNative.arv_device_get_string_feature_value(lease.Handle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                return null;

            return Marshal.PtrToStringUTF8(valuePtr);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Sets string feature value using device API
    /// </summary>
    public void SetStringFeature(string featureName, string value)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr valuePtr = Marshal.StringToCoTaskMemUTF8(value);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_string_feature_value(lease.Handle, namePtr, valuePtr, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to set feature {featureName}", ref error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
            Marshal.FreeCoTaskMem(valuePtr);
        }
    }

    /// <summary>
    /// Gets integer feature value using device API
    /// </summary>
    public long GetIntegerFeature(string featureName)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            long value = AravisNative.arv_device_get_integer_feature_value(lease.Handle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to get feature {featureName}", ref error);

            return value;
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Sets integer feature value using device API
    /// </summary>
    public void SetIntegerFeature(string featureName, long value)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_integer_feature_value(lease.Handle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to set feature {featureName}", ref error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Gets float feature value using device API
    /// </summary>
    public double GetFloatFeature(string featureName)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            double value = AravisNative.arv_device_get_float_feature_value(lease.Handle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to get feature {featureName}", ref error);

            return value;
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Sets float feature value using device API
    /// </summary>
    public void SetFloatFeature(string featureName, double value)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_float_feature_value(lease.Handle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to set feature {featureName}", ref error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Gets boolean feature value using device API
    /// </summary>
    public bool GetBooleanFeature(string featureName)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            bool value = AravisNative.arv_device_get_boolean_feature_value(lease.Handle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to get feature {featureName}", ref error);

            return value;
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Sets boolean feature value using device API
    /// </summary>
    public void SetBooleanFeature(string featureName, bool value)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(featureName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_set_boolean_feature_value(lease.Handle, namePtr, value, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to set feature {featureName}", ref error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Executes a command feature
    /// </summary>
    public void ExecuteCommand(string commandName)
    {
        using var lease = _device.AcquireHandle();
        IntPtr namePtr = Marshal.StringToCoTaskMemUTF8(commandName);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_device_execute_command(lease.Handle, namePtr, out error);
            
            if (error != IntPtr.Zero)
                throw Failure($"Failed to execute command {commandName}", ref error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>
    /// Gets the GenICam XML description of the device, or null if it has none
    /// </summary>
    public string? GetGenicamXml()
    {
        using var lease = _device.AcquireHandle();
        var xmlPtr = AravisNative.arv_device_get_genicam_xml(lease.Handle, out UIntPtr size);
        return xmlPtr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(xmlPtr, checked((int)size)).TrimEnd('\0');
    }

    /// <summary>
    /// Does nothing native: the node map belongs to the camera, and stays usable until the
    /// camera or its Device is disposed.
    /// </summary>
    public void Dispose()
    {
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
