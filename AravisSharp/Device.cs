using System.Runtime.InteropServices;
using AravisSharp.Native;
using AravisSharp.GenICam;

namespace AravisSharp;

/// <summary>
/// Represents a low-level Aravis device for GenICam feature access
/// </summary>
public class Device
{
    private readonly IntPtr _handle;
    private NodeMap? _nodeMap;

    internal Device(IntPtr handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets the GenICam node map for exploring camera features
    /// </summary>
    public NodeMap NodeMap
    {
        get
        {
            if (_nodeMap == null)
            {
                _nodeMap = new NodeMap(_handle);
            }
            return _nodeMap;
        }
    }

    /// <summary>
    /// Gets a string feature value
    /// </summary>
    public string GetStringFeature(string featureName)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var valuePtr = AravisNative.arv_device_get_string_feature_value(_handle, featurePtr, out error);
            CheckError(error);
            return MarshalString(valuePtr);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a string feature value
    /// </summary>
    public void SetStringFeature(string featureName, string value)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        IntPtr valuePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            valuePtr = Marshal.StringToCoTaskMemUTF8(value);
            AravisNative.arv_device_set_string_feature_value(_handle, featurePtr, valuePtr, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (valuePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(valuePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets an integer feature value
    /// </summary>
    public long GetIntegerFeature(string featureName)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var value = AravisNative.arv_device_get_integer_feature_value(_handle, featurePtr, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets an integer feature value
    /// </summary>
    public void SetIntegerFeature(string featureName, long value)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_set_integer_feature_value(_handle, featurePtr, value, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets a float feature value
    /// </summary>
    public double GetFloatFeature(string featureName)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var value = AravisNative.arv_device_get_float_feature_value(_handle, featurePtr, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a float feature value
    /// </summary>
    public void SetFloatFeature(string featureName, double value)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_set_float_feature_value(_handle, featurePtr, value, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets a boolean feature value
    /// </summary>
    public bool GetBooleanFeature(string featureName)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var value = AravisNative.arv_device_get_boolean_feature_value(_handle, featurePtr, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a boolean feature value
    /// </summary>
    public void SetBooleanFeature(string featureName, bool value)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_set_boolean_feature_value(_handle, featurePtr, value, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Executes a command feature
    /// </summary>
    public void ExecuteCommand(string featureName)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_execute_command(_handle, featurePtr, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    private void CheckError(IntPtr error)
    {
        if (error != IntPtr.Zero)
        {
            var gerror = Marshal.PtrToStructure<GError>(error);
            var message = Marshal.PtrToStringUTF8(gerror.Message) ?? "Unknown error";
            throw new AravisException(message);
        }
    }

    private static string MarshalString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}
