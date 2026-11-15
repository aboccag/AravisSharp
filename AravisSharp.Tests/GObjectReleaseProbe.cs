using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Counts how many times a GObject is released (its last reference dropped), through a
/// <c>g_object_weak_ref</c> notify. Proves that a disposed or finalized wrapper gave its
/// native object back exactly once, which a timing or a log line cannot.
/// </summary>
/// <remarks>
/// Prototypes from gobject/gobject.h:
/// <c>void g_object_weak_ref (GObject *object, GWeakNotify notify, gpointer data)</c> and
/// <c>typedef void (*GWeakNotify) (gpointer data, GObject *where_the_object_was)</c>. The
/// library does not import <c>g_object_weak_ref</c>, so it is bound here through
/// <see cref="NativeLibrary.GetExport"/>, on the libgobject handle the library's own
/// resolver returns: the same copy (bundled or system) the bindings call into.
/// <see cref="NativeLibrary.Load(string, System.Reflection.Assembly, DllImportSearchPath?)"/>
/// does not call that resolver. The notify may run on the finalizer thread.
/// </remarks>
internal static class GObjectReleaseProbe
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WeakNotifyFn(IntPtr data, IntPtr whereTheObjectWas);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WeakRefFn(IntPtr obj, IntPtr notify, IntPtr data);

    private static readonly ConcurrentDictionary<long, int> Releases = new();
    private static long _lastId;

    // Rooted for the life of the process: GLib may call it at any later time.
    private static readonly WeakNotifyFn Notify = OnReleased;
    private static readonly IntPtr NotifyPointer = Marshal.GetFunctionPointerForDelegate(Notify);

    private static readonly Lazy<WeakRefFn> WeakRef = new(() =>
    {
        var gobject = AravisLibrary.ResolveDllImport(GLibNative.GObjectLibraryName, typeof(GLibNative).Assembly, null);
        if (gobject == IntPtr.Zero)
            throw new DllNotFoundException("libgobject-2.0 could not be loaded through AravisLibrary.ResolveDllImport.");
        return Marshal.GetDelegateForFunctionPointer<WeakRefFn>(NativeLibrary.GetExport(gobject, "g_object_weak_ref"));
    });

    private static void OnReleased(IntPtr data, IntPtr whereTheObjectWas)
    {
        Releases.AddOrUpdate((long)data, 1, (_, n) => n + 1);
    }

    /// <summary>Starts counting the releases of <paramref name="obj"/>; returns the watch id.</summary>
    public static long Watch(IntPtr obj)
    {
        if (obj == IntPtr.Zero)
            throw new ArgumentNullException(nameof(obj));
        long id = Interlocked.Increment(ref _lastId);
        Releases[id] = 0;
        WeakRef.Value(obj, NotifyPointer, (IntPtr)id);
        return id;
    }

    /// <summary>How many times the object watched as <paramref name="id"/> was released.</summary>
    public static int ReleaseCount(long id) => Releases.TryGetValue(id, out var n) ? n : 0;
}
