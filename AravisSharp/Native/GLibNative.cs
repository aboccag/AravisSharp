using System.Runtime.InteropServices;

namespace AravisSharp.Native;

/// <summary>
/// P/Invoke declarations for GLib / GObject functions.
/// These live in libgobject-2.0 and libglib-2.0, NOT in the aravis library.
/// </summary>
public static class GLibNative
{
    // Logical library names — resolved at runtime by AravisLibrary.RegisterResolver()
    internal const string GObjectLibraryName = "gobject-2.0";
    internal const string GLibLibraryName = "glib-2.0";
    internal const string GioLibraryName = "gio-2.0";

    // --- GObject (libgobject-2.0) ---

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr g_object_ref(IntPtr obj);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_object_unref(IntPtr obj);

    // --- GObject type introspection (libgobject-2.0) ---

    /// <summary>
    /// Returns the GType of a GObject instance.
    /// Equivalent to G_OBJECT_TYPE(obj) macro.
    /// The GType is stored as the first field of the GTypeInstance pointed to by obj.
    /// </summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr g_type_name_from_instance(IntPtr instance);

    /// <summary>
    /// Checks if a GObject instance is an instance of a given type or a subtype of it.
    /// Equivalent to G_TYPE_CHECK_INSTANCE_TYPE(instance, type).
    /// </summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool g_type_check_instance_is_a(IntPtr instance, IntPtr iface_type);

    // --- GLib (libglib-2.0) ---

    [DllImport(GLibLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_error_free(IntPtr error);

    [DllImport(GLibLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_clear_error(ref IntPtr error);

    [DllImport(GLibLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_free(IntPtr ptr);

    // --- Helper methods ---

    /// <summary>
    /// Safely clears a GError pointer: frees the error if set, then resets to IntPtr.Zero
    /// </summary>
    public static void ClearError(ref IntPtr error)
    {
        if (error != IntPtr.Zero)
        {
            g_error_free(error);
            error = IntPtr.Zero;
        }
    }

    // --- GIO (libgio-2.0) — GInetAddress / GInetAddressMask helpers ---

    /// <summary>
    /// Converts a GInetAddress to a dotted-decimal (IPv4) or colon-separated (IPv6) string.
    /// The returned string must be freed with <see cref="g_free"/>.
    /// </summary>
    [DllImport(GioLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr g_inet_address_to_string(IntPtr address);

    /// <summary>
    /// Returns the prefix length (e.g. 24 for /24) of a GInetAddressMask.
    /// Note: Aravis always creates masks with prefix=32 — use <see cref="g_inet_address_mask_get_address"/> instead.
    /// </summary>
    [DllImport(GioLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint g_inet_address_mask_get_length(IntPtr mask);

    /// <summary>
    /// Returns the GInetAddress* embedded in a GInetAddressMask.
    /// For Aravis-created masks this holds the actual subnet mask bytes.
    /// The returned object is owned by the mask — do NOT unref it.
    /// </summary>
    [DllImport(GioLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr g_inet_address_mask_get_address(IntPtr mask);

    // --- GObject property access (libgobject-2.0) ---
    // g_object_set / g_object_get are variadic C functions.
    // We declare typed overloads for the signatures we need.

    /// <summary>Sets a single integer property on a GObject.</summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "g_object_set")]
    public static extern void g_object_set_int(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName,
        int value, IntPtr terminator);

    /// <summary>Sets a single unsigned integer property on a GObject.</summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "g_object_set")]
    public static extern void g_object_set_uint(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName,
        uint value, IntPtr terminator);

    /// <summary>Sets a single boolean property on a GObject.</summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "g_object_set")]
    public static extern void g_object_set_bool(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName,
        [MarshalAs(UnmanagedType.Bool)] bool value, IntPtr terminator);

    /// <summary>Gets a single integer property from a GObject.</summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "g_object_get")]
    public static extern void g_object_get_int(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName,
        out int value, IntPtr terminator);

    /// <summary>Gets a single unsigned integer property from a GObject.</summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "g_object_get")]
    public static extern void g_object_get_uint(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName,
        out uint value, IntPtr terminator);

    /// <summary>Gets a single boolean property from a GObject.</summary>
    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "g_object_get")]
    public static extern void g_object_get_bool(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName,
        [MarshalAs(UnmanagedType.Bool)] out bool value, IntPtr terminator);

    // --- Helper methods ---

    /// <summary>
    /// Gets the GObject type name for a GObject instance (e.g. "ArvGcInteger", "ArvGcFloat")
    /// </summary>
    public static string? GetTypeName(IntPtr instance)
    {
        if (instance == IntPtr.Zero) return null;
        var namePtr = g_type_name_from_instance(instance);
        if (namePtr == IntPtr.Zero) return null;
        return Marshal.PtrToStringUTF8(namePtr);
    }
}
