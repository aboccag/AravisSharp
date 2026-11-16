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

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int g_value_get_enum(ref GValue value);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void g_value_set_boolean(ref GValue value, [MarshalAs(UnmanagedType.Bool)] bool v);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool g_value_get_boolean(ref GValue value);

    // --- Property lookup (libgobject-2.0), through GLib functions only (#14, AC-5) ---
    // GParamSpec, GTypeInstance and GTypeClass are not mirrored: the instance's GType comes
    // from its type name, its class from g_type_class_peek, the property's value type from
    // the GValue that g_param_spec_get_default_value returns (GLib 2.38).

    // Fundamental GType ids: G_TYPE_MAKE_FUNDAMENTAL(n) = n << 2 (gobject/gtype.h).
    private static readonly IntPtr GTypeBoolean = 5 << 2;
    private static readonly IntPtr GTypeEnum = 12 << 2;
    private static readonly IntPtr GTypeObject = 20 << 2;

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_type_from_name(IntPtr name);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_type_name(IntPtr type);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_type_fundamental(IntPtr type);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_type_class_peek(IntPtr type);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_object_class_find_property(IntPtr objectClass,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr g_param_spec_get_default_value(IntPtr pspec);

    [DllImport(GObjectLibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool g_value_type_compatible(IntPtr srcType, IntPtr destType);

    private static string TypeName(IntPtr type) =>
        Marshal.PtrToStringUTF8(g_type_name(type)) ?? $"GType {type}";

    /// <summary>
    /// Returns the value type of the property <paramref name="propertyName"/> of the GObject
    /// <paramref name="obj"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or its class has no such property.</exception>
    private static IntPtr LookupPropertyValueType(IntPtr obj, string propertyName)
    {
        if (obj == IntPtr.Zero)
            throw new ArgumentNullException(nameof(obj));
        ArgumentNullException.ThrowIfNull(propertyName);
        if (!g_type_check_instance_is_a(obj, GTypeObject))
            throw new ArgumentException("The instance is not a GObject.", nameof(obj));

        var typeNamePtr = g_type_name_from_instance(obj);
        var typeName = Marshal.PtrToStringUTF8(typeNamePtr) ?? "(unknown type)";
        // The instance is alive, so its class is referenced and g_type_class_peek finds it.
        var objectClass = g_type_class_peek(g_type_from_name(typeNamePtr));
        var pspec = objectClass == IntPtr.Zero ? IntPtr.Zero : g_object_class_find_property(objectClass, propertyName);
        if (pspec == IntPtr.Zero)
            throw new ArgumentException($"{typeName} has no property '{propertyName}'.", nameof(propertyName));

        // A GValue of the property's value type, owned by the pspec; its GType is the first
        // field (GValue.GType, offset 0, pinned by GValue_ShouldMatchCLayoutOn64Bit).
        var defaultValue = g_param_spec_get_default_value(pspec);
        if (defaultValue == IntPtr.Zero)
            throw new InvalidOperationException($"GLib returned no default value for property '{propertyName}' of {typeName}.");
        return Marshal.ReadIntPtr(defaultValue);
    }

    /// <summary>
    /// Checks that the property exists and that a GValue of <paramref name="helperType"/> is
    /// copied into it (<paramref name="write"/>) or out of it without a GLib transform: GLib
    /// would convert between gint and guint silently (#14, AC-2).
    /// </summary>
    private static void CheckPropertyType(IntPtr obj, string propertyName, IntPtr helperType, bool write)
    {
        var valueType = LookupPropertyValueType(obj, propertyName);
        bool compatible = write
            ? g_value_type_compatible(helperType, valueType)
            : g_value_type_compatible(valueType, helperType);
        if (!compatible)
            throw new ArgumentException(
                $"Property '{propertyName}' of {GetTypeName(obj)} holds {TypeName(valueType)}, not {TypeName(helperType)}.",
                nameof(propertyName));
    }

    private static void CheckEnumType(IntPtr enumType)
    {
        if (enumType == IntPtr.Zero || g_type_fundamental(enumType) != GTypeEnum)
            throw new ArgumentException($"GType {enumType} is not an enum type.", nameof(enumType));
    }

    /// <summary>
    /// Read-back after a write (#14, AC-3): GLib only logs a warning and keeps the old value
    /// when a value fails the property's validation (out of range, not an enum member) or the
    /// property is read-only or construct-only. Access flags are not checked (spec, known
    /// gap): a write-only property cannot be read back. Every ArvGvStream property that
    /// <see cref="AravisSharp.Stream"/> sets is read-write (aravis/src/arvgvstream.c).
    /// </summary>
    private static void ThrowIfNotApplied<T>(IntPtr obj, string propertyName, T written, T readBack)
        where T : struct, IEquatable<T>
    {
        if (!written.Equals(readBack))
            throw new ArgumentOutOfRangeException("value", written,
                $"Property '{propertyName}' of {GetTypeName(obj)} read back {readBack} after writing {written}: " +
                "the object refused the value (out of range, read-only or construct-only), or the property " +
                "is write-only and cannot be read back, in which case the write may have been applied.");
    }

    /// <summary>Sets a gint property on a GObject, then reads it back.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or it has no such property, or the property is not a gint.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value read back differs from <paramref name="value"/>: the object refused it (outside the property's range, read-only or construct-only property), or the property is write-only, in which case the write may have been applied.</exception>
    internal static void SetIntProperty(IntPtr obj, string propertyName, int value)
    {
        CheckPropertyType(obj, propertyName, G_TYPE_INT, write: true);
        var gvalue = new GValue();
        g_value_init(ref gvalue, G_TYPE_INT);
        try
        {
            g_value_set_int(ref gvalue, value);
            g_object_set_property(obj, propertyName, ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }

        ThrowIfNotApplied(obj, propertyName, value, GetIntProperty(obj, propertyName));
    }

    /// <summary>Sets a guint property on a GObject, then reads it back.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or it has no such property, or the property is not a guint.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value read back differs from <paramref name="value"/>: the object refused it (outside the property's range, read-only or construct-only property), or the property is write-only, in which case the write may have been applied.</exception>
    internal static void SetUIntProperty(IntPtr obj, string propertyName, uint value)
    {
        CheckPropertyType(obj, propertyName, G_TYPE_UINT, write: true);
        var gvalue = new GValue();
        g_value_init(ref gvalue, G_TYPE_UINT);
        try
        {
            g_value_set_uint(ref gvalue, value);
            g_object_set_property(obj, propertyName, ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }

        ThrowIfNotApplied(obj, propertyName, value, GetUIntProperty(obj, propertyName));
    }

    /// <summary>
    /// Sets an enum property on a GObject from its integer value, then reads it back;
    /// <paramref name="enumType"/> is the property's enum GType.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enum GType, or <paramref name="obj"/> is not a GObject, or it has no such property, or the property is not of <paramref name="enumType"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value read back differs from <paramref name="value"/>: the object refused it (not a member of the enum, read-only or construct-only property), or the property is write-only, in which case the write may have been applied.</exception>
    internal static void SetEnumProperty(IntPtr obj, string propertyName, IntPtr enumType, int value)
    {
        CheckPropertyType(obj, propertyName, enumType, write: true);
        CheckEnumType(enumType);
        var gvalue = new GValue();
        g_value_init(ref gvalue, enumType);
        try
        {
            g_value_set_enum(ref gvalue, value);
            g_object_set_property(obj, propertyName, ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }

        ThrowIfNotApplied(obj, propertyName, value, GetEnumProperty(obj, propertyName, enumType));
    }

    /// <summary>Sets a gboolean property on a GObject, then reads it back.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or it has no such property, or the property is not a gboolean.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value read back differs from <paramref name="value"/>: the object refused it (read-only or construct-only property), or the property is write-only, in which case the write may have been applied.</exception>
    internal static void SetBoolProperty(IntPtr obj, string propertyName, bool value)
    {
        CheckPropertyType(obj, propertyName, GTypeBoolean, write: true);
        var gvalue = new GValue();
        g_value_init(ref gvalue, GTypeBoolean);
        try
        {
            g_value_set_boolean(ref gvalue, value);
            g_object_set_property(obj, propertyName, ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }

        ThrowIfNotApplied(obj, propertyName, value, GetBoolProperty(obj, propertyName));
    }

    /// <summary>Gets a gint property from a GObject.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or it has no such property, or the property is not a gint.</exception>
    internal static int GetIntProperty(IntPtr obj, string propertyName)
    {
        CheckPropertyType(obj, propertyName, G_TYPE_INT, write: false);
        var gvalue = new GValue();
        g_value_init(ref gvalue, G_TYPE_INT);
        try
        {
            g_object_get_property(obj, propertyName, ref gvalue);
            return g_value_get_int(ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }
    }

    /// <summary>Gets a guint property from a GObject.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or it has no such property, or the property is not a guint.</exception>
    internal static uint GetUIntProperty(IntPtr obj, string propertyName)
    {
        CheckPropertyType(obj, propertyName, G_TYPE_UINT, write: false);
        var gvalue = new GValue();
        g_value_init(ref gvalue, G_TYPE_UINT);
        try
        {
            g_object_get_property(obj, propertyName, ref gvalue);
            return g_value_get_uint(ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }
    }

    /// <summary>
    /// Gets the integer value of an enum property from a GObject; <paramref name="enumType"/>
    /// is the property's enum GType.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enum GType, or <paramref name="obj"/> is not a GObject, or it has no such property, or the property is not of <paramref name="enumType"/>.</exception>
    internal static int GetEnumProperty(IntPtr obj, string propertyName, IntPtr enumType)
    {
        CheckPropertyType(obj, propertyName, enumType, write: false);
        CheckEnumType(enumType);
        var gvalue = new GValue();
        g_value_init(ref gvalue, enumType);
        try
        {
            g_object_get_property(obj, propertyName, ref gvalue);
            return g_value_get_enum(ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }
    }

    /// <summary>Gets a gboolean property from a GObject.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is zero or <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is not a GObject, or it has no such property, or the property is not a gboolean.</exception>
    internal static bool GetBoolProperty(IntPtr obj, string propertyName)
    {
        CheckPropertyType(obj, propertyName, GTypeBoolean, write: false);
        var gvalue = new GValue();
        g_value_init(ref gvalue, GTypeBoolean);
        try
        {
            g_object_get_property(obj, propertyName, ref gvalue);
            return g_value_get_boolean(ref gvalue);
        }
        finally
        {
            g_value_unset(ref gvalue);
        }
    }

    // --- v0.8.36 entry points, kept as managed shims (#14, spec question 3) ---
    // v0.8.36 declared these as typed DllImports of the variadic g_object_set / g_object_get,
    // which break on Apple arm64. They now forward to the checked helpers so that assemblies
    // built against v0.8.36 still bind; the terminator argument is ignored. Like the variadic
    // call they replace, the int shims also accept an enum property (v0.8.36's Stream set
    // "socket-buffer" and "packet-resend" through g_object_set_int).

    private const string LegacyPropertyShimMessage =
        "Kept for binary compatibility with v0.8.36 only. Use the GigE methods of AravisSharp.Stream " +
        "(SetSocketBufferPolicy, SetSocketBufferSize, SetPacketResend, SetInitialPacketTimeout, SetPacketTimeout, SetFrameRetention).";

    /// <summary>Returns the property's enum GType if it is an enum property, else zero.</summary>
    private static IntPtr LegacyEnumType(IntPtr obj, string propertyName)
    {
        var valueType = LookupPropertyValueType(obj, propertyName);
        return g_type_fundamental(valueType) == GTypeEnum ? valueType : IntPtr.Zero;
    }

    /// <summary>Sets a gint or enum property; same checks and exceptions as the internal property helpers.</summary>
    [Obsolete(LegacyPropertyShimMessage)]
    public static void g_object_set_int(IntPtr obj, string propertyName, int value, IntPtr terminator)
    {
        var enumType = LegacyEnumType(obj, propertyName);
        if (enumType != IntPtr.Zero)
            SetEnumProperty(obj, propertyName, enumType, value);
        else
            SetIntProperty(obj, propertyName, value);
    }

    /// <summary>Sets a guint property; same checks and exceptions as the internal property helpers.</summary>
    [Obsolete(LegacyPropertyShimMessage)]
    public static void g_object_set_uint(IntPtr obj, string propertyName, uint value, IntPtr terminator)
        => SetUIntProperty(obj, propertyName, value);

    /// <summary>Sets a gboolean property; same checks and exceptions as the internal property helpers.</summary>
    [Obsolete(LegacyPropertyShimMessage)]
    public static void g_object_set_bool(IntPtr obj, string propertyName, bool value, IntPtr terminator)
        => SetBoolProperty(obj, propertyName, value);

    /// <summary>Gets a gint or enum property; same checks and exceptions as the internal property helpers.</summary>
    [Obsolete(LegacyPropertyShimMessage)]
    public static void g_object_get_int(IntPtr obj, string propertyName, out int value, IntPtr terminator)
    {
        var enumType = LegacyEnumType(obj, propertyName);
        value = enumType != IntPtr.Zero
            ? GetEnumProperty(obj, propertyName, enumType)
            : GetIntProperty(obj, propertyName);
    }

    /// <summary>Gets a guint property; same checks and exceptions as the internal property helpers.</summary>
    [Obsolete(LegacyPropertyShimMessage)]
    public static void g_object_get_uint(IntPtr obj, string propertyName, out uint value, IntPtr terminator)
        => value = GetUIntProperty(obj, propertyName);

    /// <summary>Gets a gboolean property; same checks and exceptions as the internal property helpers.</summary>
    [Obsolete(LegacyPropertyShimMessage)]
    public static void g_object_get_bool(IntPtr obj, string propertyName, out bool value, IntPtr terminator)
        => value = GetBoolProperty(obj, propertyName);

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
