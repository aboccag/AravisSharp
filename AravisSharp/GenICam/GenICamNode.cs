using System;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.GenICam;

/// <summary>
/// Represents a GenICam feature node in the camera's node map
/// </summary>
/// <remarks>
/// A node belongs to the camera's GenICam object: its calls are serialised with the
/// camera's, and once the camera is disposed (or the node itself) its methods throw
/// <see cref="ObjectDisposedException"/>.
/// </remarks>
public class GenICamNode : IDisposable
{
    private const string OwnerDisposedMessage =
        "The Camera this GenICamNode was obtained from has been disposed.";

    // The node is borrowed (transfer none) from the ArvGc, itself owned by the camera's
    // device: the camera's handle is pinned around every native call.
    private readonly GObjectSafeHandle _owner;
    private readonly IntPtr _nodeHandle;
    private int _disposed;

    public string Name { get; }
    public string? DisplayName { get; }
    public string? Description { get; }
    public string? Tooltip { get; }
    public GenICamNodeType NodeType { get; }

    /// <param name="device">The Device of the camera that owns the node.</param>
    /// <param name="nodeHandle">An ArvGcFeatureNode of that camera's GenICam object.</param>
    internal GenICamNode(Device device, IntPtr nodeHandle)
    {
        _owner = device.Owner;
        _nodeHandle = nodeHandle;
        using var lease = AcquireOwner();

        // Get node name
        IntPtr namePtr = AravisNative.arv_gc_feature_node_get_name(lease.Handle);
        Name = Marshal.PtrToStringUTF8(namePtr) ?? "Unknown";

        // Get display name
        IntPtr displayNamePtr = AravisNative.arv_gc_feature_node_get_display_name(lease.Handle);
        DisplayName = Marshal.PtrToStringUTF8(displayNamePtr);

        // Get description
        IntPtr descPtr = AravisNative.arv_gc_feature_node_get_description(lease.Handle);
        Description = Marshal.PtrToStringUTF8(descPtr);

        // Get tooltip
        IntPtr tooltipPtr = AravisNative.arv_gc_feature_node_get_tooltip(lease.Handle);
        Tooltip = Marshal.PtrToStringUTF8(tooltipPtr);

        // Determine node type (simplified)
        NodeType = DetermineNodeType();
    }

    public bool IsAvailable()
    {
        using var lease = AcquireOwner();
        IntPtr error = IntPtr.Zero;
        try
        {
            return AravisNative.arv_gc_feature_node_is_available(lease.Handle, out error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public bool IsImplemented()
    {
        using var lease = AcquireOwner();
        IntPtr error = IntPtr.Zero;
        try
        {
            return AravisNative.arv_gc_feature_node_is_implemented(lease.Handle, out error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public bool IsLocked()
    {
        using var lease = AcquireOwner();
        IntPtr error = IntPtr.Zero;
        try
        {
            return AravisNative.arv_gc_feature_node_is_locked(lease.Handle, out error);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public string? GetValueAsString()
    {
        using var lease = AcquireOwner();
        IntPtr error = IntPtr.Zero;
        try
        {
            IntPtr valuePtr = AravisNative.arv_gc_feature_node_get_value_as_string(lease.Handle, out error);
            return Marshal.PtrToStringUTF8(valuePtr);
        }
        finally
        {
            GLibNative.ClearError(ref error);
        }
    }

    public void SetValueFromString(string value)
    {
        using var lease = AcquireOwner();
        IntPtr valuePtr = Marshal.StringToCoTaskMemUTF8(value);
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_gc_feature_node_set_value_from_string(lease.Handle, valuePtr, out error);
            if (error != IntPtr.Zero)
                throw AravisException.TakeGError(ref error);
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

    /// <summary>
    /// Pins the camera for one native call on the node and takes the camera's lock. The
    /// lease's <see cref="GObjectLease.Handle"/> is the node pointer.
    /// </summary>
    private GObjectLease AcquireOwner()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(GenICamNode));
        var lease = _owner.AcquireBorrowed(_nodeHandle, nameof(GenICamNode), OwnerDisposedMessage, serialized: true);
        // Dispose() may have run while this call waited for the camera's lock: honour it.
        if (Volatile.Read(ref _disposed) != 0)
        {
            lease.Dispose();
            throw new ObjectDisposedException(nameof(GenICamNode));
        }
        return lease;
    }

    /// <summary>
    /// Closes this wrapper. Releases nothing native: GenICam nodes belong to the camera's
    /// GenICam object.
    /// </summary>
    public void Dispose()
    {
        Volatile.Write(ref _disposed, 1);
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
