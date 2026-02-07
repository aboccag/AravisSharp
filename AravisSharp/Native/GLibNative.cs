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

    // --- GLib (libglib-2.0) ---

    [DllImport(GLibLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_error_free(IntPtr error);

    [DllImport(GLibLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_clear_error(ref IntPtr error);

    [DllImport(GLibLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void g_free(IntPtr ptr);
}
