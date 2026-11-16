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

    private static IntPtr Load(string logicalName)
    {
        var handle = AravisLibrary.ResolveDllImport(logicalName, typeof(GLibNative).Assembly, null);
        if (handle == IntPtr.Zero)
            throw new DllNotFoundException($"{logicalName} could not be loaded through AravisLibrary.");
        return handle;
    }

    private static readonly Lazy<IntPtr> Gio = new(() => Load(GLibNative.GioLibraryName));

    private static T Bind<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(Gio.Value, name));

    private static readonly Lazy<NoArgumentFn> SocketClientNew = new(() => Bind<NoArgumentFn>("g_socket_client_new"));
    private static readonly Lazy<SocketNewFn> SocketNew = new(() => Bind<SocketNewFn>("g_socket_new"));
    private static readonly Lazy<InetSocketAddressNewFromStringFn> InetSocketAddressNewFromString =
        new(() => Bind<InetSocketAddressNewFromStringFn>("g_inet_socket_address_new_from_string"));
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

    /// <summary>The GType of the GSocketFamily enum.</summary>
    public static IntPtr g_socket_family_get_type() => SocketFamilyGetType.Value();

    /// <summary>The GType of the GSocketType enum.</summary>
    public static IntPtr g_socket_type_get_type() => SocketTypeGetType.Value();
}

/// <summary>
/// Records the warnings and criticals GObject logs (domain "GLib-GObject") on the calling
/// thread while it is alive, through <c>g_log_set_handler</c>. Messages logged by other
/// threads in the meantime are dropped, not printed.
/// </summary>
internal sealed class GObjectLogCapture : IDisposable
{
    private const string Domain = "GLib-GObject";
    private const int LevelCritical = 1 << 3; // G_LOG_LEVEL_CRITICAL
    private const int LevelWarning = 1 << 4;  // G_LOG_LEVEL_WARNING

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
    private readonly List<string> _messages = new();
    private uint _handlerId;

    public GObjectLogCapture()
    {
        _callback = OnLog;
        _handlerId = LogSetHandler.Value(Domain, LevelCritical | LevelWarning, _callback, IntPtr.Zero);
    }

    public IReadOnlyList<string> Messages
    {
        get { lock (_messages) return _messages.ToArray(); }
    }

    private void OnLog(IntPtr logDomain, int logLevel, IntPtr message, IntPtr userData)
    {
        if (Environment.CurrentManagedThreadId != _threadId)
            return;
        lock (_messages)
            _messages.Add(Marshal.PtrToStringUTF8(message) ?? "(null message)");
    }

    public void Dispose()
    {
        if (_handlerId != 0)
        {
            LogRemoveHandler.Value(Domain, _handlerId);
            _handlerId = 0;
        }
        GC.KeepAlive(_callback);
    }
}
