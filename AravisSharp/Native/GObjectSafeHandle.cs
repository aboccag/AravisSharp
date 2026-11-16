using System.Runtime.InteropServices;

namespace AravisSharp.Native;

/// <summary>
/// Owns one reference on a GObject and drops it with <c>g_object_unref</c> exactly once.
/// </summary>
/// <remarks>
/// <para>
/// The P/Invoke layer takes raw <see cref="IntPtr"/> arguments, so wrappers do not pass this
/// handle to native code directly. They call <see cref="Acquire"/> and keep the returned
/// <see cref="GObjectLease"/> for the duration of the native call. While a lease is held the
/// reference cannot be released, neither by a concurrent <c>Dispose</c> nor by the finalizer
/// thread: the unref is deferred until the last lease is returned.
/// </para>
/// <para>
/// Closing is a two-step operation so that it is atomic for callers: <see cref="BeginClose"/>
/// makes every later <see cref="Acquire"/> throw at once, even while leases taken earlier
/// are still in use on other threads, then <see cref="SafeHandle.Dispose()"/> drops the
/// reference as soon as those leases are returned, on the thread that returns the last one.
/// </para>
/// </remarks>
internal sealed class GObjectSafeHandle : SafeHandle
{
    // How long a thread waits for the lock before looking at the closing flag again.
    private const int LockPollMilliseconds = 50;

    private int _closing;
    private object? _syncRoot;

    /// <summary>
    /// Wraps <paramref name="handle"/> and takes over the reference the caller holds on it
    /// (a <c>transfer full</c> return value).
    /// </summary>
    public GObjectSafeHandle(IntPtr handle)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(handle);
    }

    /// <summary>
    /// Serialises native calls on objects that Aravis does not protect itself (a camera, its
    /// device and its GenICam object share one lock). Only serialised leases take it.
    /// </summary>
    /// <remarks>
    /// Created on first use: buffers and streams never take it, and a buffer wrapper is
    /// allocated for every frame.
    /// </remarks>
    public object SyncRoot
        => Volatile.Read(ref _syncRoot)
           ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null)
           ?? _syncRoot!;

    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <summary>True once <see cref="BeginClose"/> was called.</summary>
    public bool IsClosing => Volatile.Read(ref _closing) != 0;

    protected override bool ReleaseHandle()
    {
        GLibNative.g_object_unref(handle);
        return true;
    }

    /// <summary>
    /// Marks the handle as closing: from now on <see cref="Acquire"/> throws.
    /// </summary>
    /// <returns>True for the one caller that closed it, false if it was closing already.</returns>
    public bool BeginClose() => Interlocked.Exchange(ref _closing, 1) == 0;

    /// <summary>
    /// Pins the native object for the duration of a native call.
    /// </summary>
    /// <param name="objectName">Name reported by the <see cref="ObjectDisposedException"/>.</param>
    /// <param name="message">Optional message for the <see cref="ObjectDisposedException"/>.</param>
    /// <param name="serialized">
    /// True to also hold <see cref="SyncRoot"/> until the lease is disposed, so that only one
    /// thread at a time is inside the native object.
    /// </param>
    /// <exception cref="ObjectDisposedException">The handle is closing or closed.</exception>
    public GObjectLease Acquire(string objectName, string? message = null, bool serialized = false)
        => AcquireCore(IntPtr.Zero, borrowed: false, objectName, message, serialized);

    /// <summary>
    /// Pins this handle's object while a call uses <paramref name="borrowed"/>, a pointer the
    /// object owns (a <c>transfer none</c> device, GenICam object or node): the lease's
    /// <see cref="GObjectLease.Handle"/> is <paramref name="borrowed"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The handle is closing or closed.</exception>
    public GObjectLease AcquireBorrowed(IntPtr borrowed, string objectName, string? message = null, bool serialized = false)
        => AcquireCore(borrowed, borrowed: true, objectName, message, serialized);

    private GObjectLease AcquireCore(IntPtr borrowedPointer, bool borrowed, string objectName, string? message, bool serialized)
    {
        if (IsClosing)
            throw Disposed(objectName, message);

        bool acquired = false;
        try
        {
            DangerousAddRef(ref acquired);
        }
        catch (ObjectDisposedException)
        {
            throw Disposed(objectName, message);
        }

        // BeginClose() may have run between the check above and the add-ref: honour it,
        // so that no call starts after Dispose() has returned on another thread.
        if (IsClosing)
        {
            if (acquired)
                DangerousRelease();
            throw Disposed(objectName, message);
        }

        bool locked = false;
        if (serialized)
        {
            var syncRoot = SyncRoot;
            try
            {
                // The wait for the lock can be long, even unbounded (another thread inside a
                // native call that blocks), and BeginClose() does not take the lock. So the
                // wait is cut in slices: a queued call gives up with ObjectDisposedException
                // shortly after Dispose() instead of starting late or waiting for ever.
                while (true)
                {
                    Monitor.TryEnter(syncRoot, LockPollMilliseconds, ref locked);
                    if (IsClosing)
                        throw Disposed(objectName, message);
                    if (locked)
                        break;
                }
            }
            catch
            {
                // Clean-up only: the exception (ObjectDisposedException or a thread abort of
                // the wait) propagates.
                if (locked)
                    Monitor.Exit(syncRoot);
                if (acquired)
                    DangerousRelease();
                throw;
            }
        }

        return new GObjectLease(this, borrowed ? borrowedPointer : DangerousGetHandle(), acquired, locked);
    }

    /// <summary>
    /// Pins the native object for the owner's own clean-up, after <see cref="BeginClose"/>.
    /// </summary>
    public GObjectLease AcquireForClose()
    {
        bool acquired = false;
        DangerousAddRef(ref acquired);
        return new GObjectLease(this, DangerousGetHandle(), acquired, locked: false);
    }

    private static ObjectDisposedException Disposed(string objectName, string? message)
        => message is null
            ? new ObjectDisposedException(objectName)
            : new ObjectDisposedException(objectName, message);
}

/// <summary>
/// A short-lived hold on a <see cref="GObjectSafeHandle"/>. Dispose it once the native call returned.
/// </summary>
internal readonly ref struct GObjectLease
{
    private readonly GObjectSafeHandle? _owner;
    private readonly bool _acquired;
    private readonly bool _locked;

    internal GObjectLease(GObjectSafeHandle owner, IntPtr handle, bool acquired, bool locked)
    {
        _owner = owner;
        _acquired = acquired;
        _locked = locked;
        Handle = handle;
    }

    /// <summary>The raw GObject pointer, valid until the lease is disposed.</summary>
    public IntPtr Handle { get; }

    public void Dispose()
    {
        if (_owner is null)
            return;

        // Leave the lock first: returning the last lease of a closed handle runs
        // g_object_unref, which must not happen under the lock.
        if (_locked)
            Monitor.Exit(_owner.SyncRoot);
        if (_acquired)
            _owner.DangerousRelease();
    }
}
