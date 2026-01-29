using System;
using System.Runtime.InteropServices;
using AravisSharp.Generated;

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
        IntPtr namePtr = AravisGenerated.arv_gc_feature_node_get_name();
        Name = Marshal.PtrToStringAnsi(namePtr) ?? "Unknown";

        // Get display name
        IntPtr displayNamePtr = AravisGenerated.arv_gc_feature_node_get_display_name();
        DisplayName = Marshal.PtrToStringAnsi(displayNamePtr);

        // Get description
        IntPtr descPtr = AravisGenerated.arv_gc_feature_node_get_description();
        Description = Marshal.PtrToStringAnsi(descPtr);

        // Get tooltip
        IntPtr tooltipPtr = AravisGenerated.arv_gc_feature_node_get_tooltip();
        Tooltip = Marshal.PtrToStringAnsi(tooltipPtr);

        // Determine node type (simplified)
        NodeType = DetermineNodeType();
    }

    public bool IsAvailable()
    {
        return AravisGenerated.arv_gc_feature_node_is_available();
    }

    public bool IsImplemented()
    {
        return AravisGenerated.arv_gc_feature_node_is_implemented();
    }

    public bool IsLocked()
    {
        return AravisGenerated.arv_gc_feature_node_is_locked();
    }

    public string? GetValueAsString()
    {
        IntPtr valuePtr = AravisGenerated.arv_gc_feature_node_get_value_as_string();
        return Marshal.PtrToStringAnsi(valuePtr);
    }

    public void SetValueFromString(string value)
    {
        IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);
        try
        {
            AravisGenerated.arv_gc_feature_node_set_value_from_string(valuePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(valuePtr);
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
