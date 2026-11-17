using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// The GIO and GLib functions the GObject property tests need (issue #14). GIO objects
/// carry int, uint, enum and bool properties without any camera.
/// </summary>
/// <remarks>
/// The functions are bound through <see cref="NativeLibrary.GetExport"/> on the library that
/// <see cref="AravisLibrary.ResolveDllImport"/> loads, not through <c>DllImport</c>: the
/// bundled library is used when there is one, as in the binding, and a <c>DllImport</c> in
/// this assembly would need a second DllImport resolver for it.
/// </remarks>
internal static class GioTestNative
{
    // GSocketFamily, GSocketType and GSocketProtocol values (gio/gioenums.h).
    public const int SocketFamilyInvalid = 0;
    public const int SocketFamilyIpv4 = 2;
    public const int SocketTypeStream = 1;
    public const int SocketProtocolDefault = 0;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr NoArgumentFn();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr SocketNewFn(int family, int type, int protocol, out IntPtr error);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr InetSocketAddressNewFromStringFn([MarshalAs(UnmanagedType.LPUTF8Str)] string address, uint port);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr StringArgumentFn([MarshalAs(UnmanagedType.LPUTF8Str)] string text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr PointerArgumentFn(IntPtr pointer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr FindPropertyFn(IntPtr objectClass, [MarshalAs(UnmanagedType.LPUTF8Str)] string propertyName);

    private static IntPtr Load(string logicalName)
    {
        var handle = AravisLibrary.ResolveDllImport(logicalName, typeof(GLibNative).Assembly, null);
        if (handle == IntPtr.Zero)
            throw new DllNotFoundException($"{logicalName} could not be loaded through AravisLibrary.");
        return handle;
    }

    private static readonly Lazy<IntPtr> Gio = new(() => Load(GLibNative.GioLibraryName));
    private static readonly Lazy<IntPtr> GObject = new(() => Load(GLibNative.GObjectLibraryName));

    private static T Bind<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(Gio.Value, name));

    private static T BindGObject<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(GObject.Value, name));

    private static readonly Lazy<NoArgumentFn> SocketClientNew = new(() => Bind<NoArgumentFn>("g_socket_client_new"));
    private static readonly Lazy<SocketNewFn> SocketNew = new(() => Bind<SocketNewFn>("g_socket_new"));
    private static readonly Lazy<InetSocketAddressNewFromStringFn> InetSocketAddressNewFromString =
        new(() => Bind<InetSocketAddressNewFromStringFn>("g_inet_socket_address_new_from_string"));
    private static readonly Lazy<StringArgumentFn> InetAddressNewFromString =
        new(() => Bind<StringArgumentFn>("g_inet_address_new_from_string"));
    private static readonly Lazy<StringArgumentFn> TypeFromName = new(() => BindGObject<StringArgumentFn>("g_type_from_name"));
    private static readonly Lazy<PointerArgumentFn> TypeClassPeek = new(() => BindGObject<PointerArgumentFn>("g_type_class_peek"));
    private static readonly Lazy<PointerArgumentFn> TypeClassRef = new(() => BindGObject<PointerArgumentFn>("g_type_class_ref"));
    private static readonly Lazy<PointerArgumentFn> TypeClassUnref = new(() => BindGObject<PointerArgumentFn>("g_type_class_unref"));
    private static readonly Lazy<FindPropertyFn> ObjectClassFindProperty =
        new(() => BindGObject<FindPropertyFn>("g_object_class_find_property"));
    private static readonly Lazy<NoArgumentFn> SocketFamilyGetType = new(() => Bind<NoArgumentFn>("g_socket_family_get_type"));
    private static readonly Lazy<NoArgumentFn> SocketTypeGetType = new(() => Bind<NoArgumentFn>("g_socket_type_get_type"));

    /// <summary><c>GSocketClient *g_socket_client_new (void)</c>; release with g_object_unref.</summary>
    public static IntPtr g_socket_client_new() => SocketClientNew.Value();

    /// <summary>
    /// <c>GSocket *g_socket_new (GSocketFamily, GSocketType, GSocketProtocol, GError **)</c>;
    /// release with g_object_unref, and free a non-null error with g_error_free.
    /// </summary>
    public static IntPtr g_socket_new(int family, int type, int protocol, out IntPtr error) =>
        SocketNew.Value(family, type, protocol, out error);

    /// <summary>
    /// <c>GSocketAddress *g_inet_socket_address_new_from_string (const char *address, guint port)</c>;
    /// null if the address cannot be parsed. Release with g_object_unref. Its "port" property
    /// is a construct-only guint, so GLib refuses any later write.
    /// </summary>
    public static IntPtr g_inet_socket_address_new_from_string(string address, uint port) =>
        InetSocketAddressNewFromString.Value(address, port);

    /// <summary>
    /// <c>GInetAddress *g_inet_address_new_from_string (const gchar *string)</c>; null if the
    /// string cannot be parsed. Release with g_object_unref. Its "is-loopback" property is a
    /// read-only gboolean, so GLib refuses any write.
    /// </summary>
    public static IntPtr g_inet_address_new_from_string(string address) => InetAddressNewFromString.Value(address);

    /// <summary>
    /// The GParamSpec of the property <paramref name="propertyName"/> of a live GObject's
    /// class (<c>g_object_class_find_property</c> on <c>g_type_class_peek</c> of its type),
    /// or zero. Owned by the class: do not unref. A GParamSpec is a GTypeInstance but no
    /// GObject.
    /// </summary>
    public static IntPtr FindPropertySpec(IntPtr obj, string propertyName)
    {
        var typeName = GLibNative.GetTypeName(obj) ?? throw new ArgumentException("No type name.", nameof(obj));
        var objectClass = TypeClassPeek.Value(TypeFromName.Value(typeName));
        return objectClass == IntPtr.Zero ? IntPtr.Zero : ObjectClassFindProperty.Value(objectClass, propertyName);
    }

    /// <summary>
    /// Calls <c>g_object_class_find_property</c> on the class of the GSocketFamily enum, which
    /// is no GObject class: its <c>g_return_val_if_fail (G_IS_OBJECT_CLASS (class))</c> logs a
    /// GLib-GObject critical, in every GLib version, and returns null.
    /// </summary>
    public static IntPtr FindPropertyOnANonObjectClass()
    {
        var enumClass = TypeClassRef.Value(g_socket_family_get_type());
        try
        {
            return ObjectClassFindProperty.Value(enumClass, "family");
        }
        finally
        {
            TypeClassUnref.Value(enumClass);
        }
    }

    /// <summary>The GType of the GSocketFamily enum.</summary>
    public static IntPtr g_socket_family_get_type() => SocketFamilyGetType.Value();

    /// <summary>The GType of the GSocketType enum.</summary>
    public static IntPtr g_socket_type_get_type() => SocketTypeGetType.Value();
}

/// <summary>
/// Records the warnings and criticals GLib logs in one domain ("GLib-GObject" by default) on
/// the calling thread while it is alive, through <c>g_log_set_handler</c>. Messages logged by
/// other threads in the meantime are dropped, not printed, unless <c>allThreads</c> is set:
/// the release of a native object can run on whichever thread returns its last lease.
/// </summary>
internal sealed class GObjectLogCapture : IDisposable
{
    private readonly string _domain;
    private readonly bool _allThreads;
    private const int LevelCritical = 1 << 3; // G_LOG_LEVEL_CRITICAL
    private const int LevelWarning = 1 << 4;  // G_LOG_LEVEL_WARNING
    private const int LevelMask = ~3;         // drops G_LOG_FLAG_RECURSION and G_LOG_FLAG_FATAL

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogFn(IntPtr logDomain, int logLevel, IntPtr message, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint LogSetHandlerFn([MarshalAs(UnmanagedType.LPUTF8Str)] string logDomain, int logLevels, LogFn logFunc, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LogRemoveHandlerFn([MarshalAs(UnmanagedType.LPUTF8Str)] string logDomain, uint handlerId);

    private static readonly Lazy<IntPtr> GLib = new(() =>
    {
        var handle = AravisLibrary.ResolveDllImport(GLibNative.GLibLibraryName, typeof(GLibNative).Assembly, null);
        if (handle == IntPtr.Zero)
            throw new DllNotFoundException("libglib-2.0 could not be loaded through AravisLibrary.");
        return handle;
    });

    private static readonly Lazy<LogSetHandlerFn> LogSetHandler = new(() =>
        Marshal.GetDelegateForFunctionPointer<LogSetHandlerFn>(NativeLibrary.GetExport(GLib.Value, "g_log_set_handler")));
    private static readonly Lazy<LogRemoveHandlerFn> LogRemoveHandler = new(() =>
        Marshal.GetDelegateForFunctionPointer<LogRemoveHandlerFn>(NativeLibrary.GetExport(GLib.Value, "g_log_remove_handler")));

    // GLib keeps the function pointer until the handler is removed: the delegate must outlive it.
    private readonly LogFn _callback;
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly List<(int Level, string Message)> _messages = new();
    private uint _handlerId;

    /// <param name="domain">The GLib log domain, such as "GLib-GObject" or "GLib".</param>
    /// <param name="allThreads">True to record the messages of every thread.</param>
    public GObjectLogCapture(string domain = "GLib-GObject", bool allThreads = false)
    {
        _domain = domain;
        _allThreads = allThreads;
        _callback = OnLog;
        _handlerId = LogSetHandler.Value(_domain, LevelCritical | LevelWarning, _callback, IntPtr.Zero);
    }

    public IReadOnlyList<string> Messages
    {
        get { lock (_messages) return _messages.Select(m => m.Message).ToArray(); }
    }

    /// <summary>The messages logged at G_LOG_LEVEL_CRITICAL.</summary>
    public IReadOnlyList<string> Criticals => MessagesAt(LevelCritical);

    /// <summary>The messages logged at G_LOG_LEVEL_WARNING.</summary>
    public IReadOnlyList<string> Warnings => MessagesAt(LevelWarning);

    private IReadOnlyList<string> MessagesAt(int level)
    {
        lock (_messages) return _messages.Where(m => m.Level == level).Select(m => m.Message).ToArray();
    }

    private void OnLog(IntPtr logDomain, int logLevel, IntPtr message, IntPtr userData)
    {
        if (!_allThreads && Environment.CurrentManagedThreadId != _threadId)
            return;
        lock (_messages)
            _messages.Add((logLevel & LevelMask, Marshal.PtrToStringUTF8(message) ?? "(null message)"));
    }

    public void Dispose()
    {
        if (_handlerId != 0)
        {
            LogRemoveHandler.Value(_domain, _handlerId);
            _handlerId = 0;
        }
        GC.KeepAlive(_callback);
    }
}
