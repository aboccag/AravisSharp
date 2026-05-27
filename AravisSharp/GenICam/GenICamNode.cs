using System;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.GenICam;

/// <summary>
/// Represents a GenICam feature node in the camera's node map
/// </summary>
public class GenICamNode : IDisposable
{
    private IntPtr _nodeHandle;
    private IntPtr _genicamHandle;
    private bool _disposed;

    public string Name { get; }
    public string? DisplayName { get; }
    public string? Description { get; }
    public string? Tooltip { get; }
    public GenICamNodeType NodeType { get; }

    internal GenICamNode(IntPtr nodeHandle, IntPtr genicamHandle)
    {
        _nodeHandle = nodeHandle;
        _genicamHandle = genicamHandle;

        // Get node name
        IntPtr namePtr = AravisNative.arv_gc_feature_node_get_name(_nodeHandle);
        Name = Marshal.PtrToStringUTF8(namePtr) ?? "Unknown";

        // Get display name
        IntPtr displayNamePtr = AravisNative.arv_gc_feature_node_get_display_name(_nodeHandle);
        DisplayName = Marshal.PtrToStringUTF8(displayNamePtr);

        // Get description
        IntPtr descPtr = AravisNative.arv_gc_feature_node_get_description(_nodeHandle);
        Description = Marshal.PtrToStringUTF8(descPtr);

        // Get tooltip
        IntPtr tooltipPtr = AravisNative.arv_gc_feature_node_get_tooltip(_nodeHandle);
        Tooltip = Marshal.PtrToStringUTF8(tooltipPtr);

        // Determine node type (simplified)
        NodeType = DetermineNodeType();
    }

    public bool IsAvailable()
    {
        IntPtr error = IntPtr.Zero;
        try
        {
            return AravisNative.arv_gc_feature_node_is_available(_nodeHandle, out error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public bool IsImplemented()
    {
        IntPtr error = IntPtr.Zero;
        try
        {
            return AravisNative.arv_gc_feature_node_is_implemented(_nodeHandle, out error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public bool IsLocked()
    {
        IntPtr error = IntPtr.Zero;
        try
        {
            return AravisNative.arv_gc_feature_node_is_locked(_nodeHandle, out error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public string? GetValueAsString()
    {
        IntPtr error = IntPtr.Zero;
        try
        {
            IntPtr valuePtr = AravisNative.arv_gc_feature_node_get_value_as_string(_nodeHandle, out error);
            return Marshal.PtrToStringUTF8(valuePtr);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public void SetValueFromString(string value)
    {
        IntPtr valuePtr = Marshal.StringToCoTaskMemUTF8(value);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_gc_feature_node_set_value_from_string(_nodeHandle, valuePtr, out error);
            if (error != IntPtr.Zero)
            {
                var gerror = Marshal.PtrToStructure<GError>(error);
                var message = Marshal.PtrToStringUTF8(gerror.Message) ?? "Unknown error";
                throw new AravisException(message);
            }
        }
        finally
        {
            GLibNative.ClearError(ref error);
            Marshal.FreeCoTaskMem(valuePtr);
        }
    }

    private GenICamNodeType DetermineNodeType()
    {
        // This is a simplified type detection
        // In reality, you'd check the actual GType of the node
        return GenICamNodeType.Unknown;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // GenICam nodes are managed by the Genicam object, don't unref
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public enum GenICamNodeType
{
    Unknown,
    Integer,
    Float,
    String,
    Boolean,
    Enumeration,
    Command,
    Category,
    Register
}
