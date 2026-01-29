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

    internal NodeMap(IntPtr deviceHandle)
    {
        _deviceHandle = deviceHandle;
    }

    /// <summary>
    /// Gets a feature node by name (simplified - returns feature info)
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
    /// Gets all available features (simplified list)
    /// </summary>
    public List<FeatureInfo> GetAllFeatures()
    {
        var features = new List<FeatureInfo>();
        
        // Common GenICam features
        var commonFeatures = new[]
        {
            "DeviceVendorName", "DeviceModelName", "DeviceFirmwareVersion",
            "DeviceSerialNumber", "Width", "Height", "PixelFormat",
            "AcquisitionMode", "ExposureTime", "Gain", "TriggerMode"
        };

        foreach (var featureName in commonFeatures)
        {
            var node = GetNode(featureName);
            if (node != null)
            {
                features.Add(node);
            }
        }

        return features;
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
