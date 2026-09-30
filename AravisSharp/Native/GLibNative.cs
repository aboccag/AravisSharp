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
    // g_object_set / g_object_get are variadic: declaring them with fixed arguments
    // breaks on Apple arm64, where variadic arguments go on the stack. Use the
    // non-variadic g_object_set_property / g_object_get_property with a GValue.

    /// <summary>Fundamental GType of gint (G_TYPE_INT).</summary>
    public static readonly IntPtr G_TYPE_INT = 6 << 2;

    /// <summary>Fundamental GType of guint (G_TYPE_UINT).</summary>
    public static readonly IntPtr G_TYPE_UINT = 7 << 2;

    /// <summary>GValue layout: a GType followed by a two-slot 64-bit data union.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GValue
    {
        public IntPtr GType;
        public long Data0;
        public long Data1;
    }

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr g_value_init(ref GValue value, IntPtr gtype);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_value_unset(ref GValue value);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_value_set_int(ref GValue value, int v);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_value_set_uint(ref GValue value, uint v);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_value_set_enum(ref GValue value, int v);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int g_value_get_int(ref GValue value);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint g_value_get_uint(ref GValue value);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_object_set_property(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName, ref GValue value);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_object_get_property(IntPtr obj,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName, ref GValue value);

    /// <summary>Sets a gint property on a GObject.</summary>
    public static void SetIntProperty(IntPtr obj, string propertyName, int v)
    {
        var value = new GValue();
        g_value_init(ref value, G_TYPE_INT);
        g_value_set_int(ref value, v);
        g_object_set_property(obj, propertyName, ref value);
        g_value_unset(ref value);
    }

    /// <summary>Sets a guint property on a GObject.</summary>
    public static void SetUIntProperty(IntPtr obj, string propertyName, uint v)
    {
        var value = new GValue();
        g_value_init(ref value, G_TYPE_UINT);
        g_value_set_uint(ref value, v);
        g_object_set_property(obj, propertyName, ref value);
        g_value_unset(ref value);
    }

    /// <summary>Sets an enum property on a GObject; <paramref name="enumType"/> is the property's enum GType.</summary>
    public static void SetEnumProperty(IntPtr obj, string propertyName, IntPtr enumType, int v)
    {
        var value = new GValue();
        g_value_init(ref value, enumType);
        g_value_set_enum(ref value, v);
        g_object_set_property(obj, propertyName, ref value);
        g_value_unset(ref value);
    }

    /// <summary>Gets a gint property from a GObject.</summary>
    public static int GetIntProperty(IntPtr obj, string propertyName)
    {
        var value = new GValue();
        g_value_init(ref value, G_TYPE_INT);
        g_object_get_property(obj, propertyName, ref value);
        var result = g_value_get_int(ref value);
        g_value_unset(ref value);
        return result;
    }

    /// <summary>Gets a guint property from a GObject.</summary>
    public static uint GetUIntProperty(IntPtr obj, string propertyName)
    {
        var value = new GValue();
        g_value_init(ref value, G_TYPE_UINT);
        g_object_get_property(obj, propertyName, ref value);
        var result = g_value_get_uint(ref value);
        g_value_unset(ref value);
        return result;
    }

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int g_value_get_enum(ref GValue value);

    /// <summary>Gets an enum property from a GObject; <paramref name="enumType"/> is the property's enum GType.</summary>
    public static int GetEnumProperty(IntPtr obj, string propertyName, IntPtr enumType)
    {
        var value = new GValue();
        g_value_init(ref value, enumType);
        g_object_get_property(obj, propertyName, ref value);
        var result = g_value_get_enum(ref value);
        g_value_unset(ref value);
        return result;
    }

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
