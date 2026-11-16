using System.Runtime.InteropServices;
using AravisSharp.Native;
using AravisSharp.GenICam;

namespace AravisSharp;

/// <summary>
/// Represents a low-level Aravis device for GenICam feature access.
/// </summary>
/// <remarks>
/// A Device belongs to the <see cref="Camera"/> it was obtained from
/// (<see cref="Camera.GetDevice"/>): it holds no native reference of its own, its calls are
/// serialised with the camera's (one call at a time; the lock covers single calls, not
/// sequences), and once the camera is disposed every member, <see cref="NodeMap"/> included,
/// throws <see cref="ObjectDisposedException"/>. So an undisposed Device never keeps the
/// camera's connection open once the camera is disposed. <see cref="Dispose"/> only closes
/// this wrapper; the camera and other Device wrappers from it stay usable. A camera that is
/// never disposed is not released by the garbage collector while one of its Device or
/// node map wrappers is still reachable.
/// </remarks>
public class Device : IDisposable
{
    private const string CameraDisposedMessage =
        "The Camera this Device was obtained from has been disposed.";

    // The camera's handle: ArvDevice is borrowed (transfer none) from the ArvCamera, which
    // the lease pins around each native call.
    private readonly GObjectSafeHandle _owner;
    private readonly IntPtr _handle;
    private int _disposed;
    private NodeMap? _nodeMap;

    /// <param name="owner">The camera's handle.</param>
    /// <param name="handle">The ArvDevice pointer the camera owns.</param>
    internal Device(GObjectSafeHandle owner, IntPtr handle)
    {
        _owner = owner;
        _handle = handle;
    }

    /// <summary>The camera's handle, which owns the device and its GenICam nodes.</summary>
    internal GObjectSafeHandle Owner => _owner;

    /// <summary>The native device pointer, for tests. Library code takes a lease instead.</summary>
    internal IntPtr Handle
    {
        get
        {
            using var lease = AcquireHandle();
            return lease.Handle;
        }
    }

    /// <summary>
    /// Gets the GenICam node map for exploring camera features
    /// </summary>
    /// <remarks>Like the Device, the node map throws <see cref="ObjectDisposedException"/> once the camera is disposed.</remarks>
    public NodeMap NodeMap
    {
        get
        {
            // The lease holds the camera's lock: two threads cannot both create a node map.
            using var lease = AcquireHandle();
            return _nodeMap ??= new NodeMap(this);
        }
    }

    /// <summary>
    /// Gets a string feature value
    /// </summary>
    public string GetStringFeature(string featureName)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var valuePtr = AravisNative.arv_device_get_string_feature_value(lease.Handle, featurePtr, out error);
            CheckError(ref error);
            return MarshalString(valuePtr);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a string feature value
    /// </summary>
    public void SetStringFeature(string featureName, string value)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        IntPtr valuePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            valuePtr = Marshal.StringToCoTaskMemUTF8(value);
            AravisNative.arv_device_set_string_feature_value(lease.Handle, featurePtr, valuePtr, out error);
            CheckError(ref error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (valuePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(valuePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets an integer feature value
    /// </summary>
    public long GetIntegerFeature(string featureName)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var value = AravisNative.arv_device_get_integer_feature_value(lease.Handle, featurePtr, out error);
            CheckError(ref error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets an integer feature value
    /// </summary>
    public void SetIntegerFeature(string featureName, long value)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_set_integer_feature_value(lease.Handle, featurePtr, value, out error);
            CheckError(ref error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets a float feature value
    /// </summary>
    public double GetFloatFeature(string featureName)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var value = AravisNative.arv_device_get_float_feature_value(lease.Handle, featurePtr, out error);
            CheckError(ref error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a float feature value
    /// </summary>
    public void SetFloatFeature(string featureName, double value)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_set_float_feature_value(lease.Handle, featurePtr, value, out error);
            CheckError(ref error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets a boolean feature value
    /// </summary>
    public bool GetBooleanFeature(string featureName)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            var value = AravisNative.arv_device_get_boolean_feature_value(lease.Handle, featurePtr, out error);
            CheckError(ref error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a boolean feature value
    /// </summary>
    public void SetBooleanFeature(string featureName, bool value)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_set_boolean_feature_value(lease.Handle, featurePtr, value, out error);
            CheckError(ref error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Executes a command feature
    /// </summary>
    public void ExecuteCommand(string featureName)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(featureName);
            AravisNative.arv_device_execute_command(lease.Handle, featurePtr, out error);
            CheckError(ref error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Pins the camera for one native call on the device and takes the camera's lock. The
    /// lease's <see cref="GObjectLease.Handle"/> is the ArvDevice pointer.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This Device or its camera has been disposed.</exception>
    internal GObjectLease AcquireHandle()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(Device));
        return _owner.AcquireBorrowed(_handle, nameof(Device), CameraDisposedMessage, serialized: true);
    }

    private static void CheckError(ref IntPtr error)
    {
        if (error != IntPtr.Zero)
        {
            // Frees the GError and zeroes error: the callers' finally blocks then skip it.
            throw AravisException.TakeGError(ref error);
        }
    }

    /// <summary>
    /// Copies a (transfer none) string owned by the native device. Call it under the
    /// member's lease: the string is freed with the camera.
    /// </summary>
    private static string MarshalString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    /// <summary>
    /// Closes this wrapper: its members throw <see cref="ObjectDisposedException"/> from now on.
    /// Releases nothing native: the device belongs to the camera, which stays usable.
    /// </summary>
    public void Dispose()
    {
        Volatile.Write(ref _disposed, 1);
        GC.SuppressFinalize(this);
    }
}
