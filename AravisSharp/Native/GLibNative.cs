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

    /// <summary>
    /// Gets the GObject type name for a GObject instance (e.g. "ArvGcInteger", "ArvGcFloat")
    /// </summary>
    public static string? GetTypeName(IntPtr instance)
    {
        if (instance == IntPtr.Zero) return null;
        var namePtr = g_type_name_from_instance(instance);
        if (namePtr == IntPtr.Zero) return null;
        return Marshal.PtrToStringAnsi(namePtr);
    }
}
