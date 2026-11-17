using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Represents an image buffer from a camera stream
/// </summary>
/// <remarks>
/// Members may be called from several threads; calls are not serialised. Once
/// <see cref="Dispose()"/> has returned, or the buffer has been pushed to a stream, every
/// member throws <see cref="ObjectDisposedException"/>; a call already in progress on another
/// thread completes normally, and the native buffer is released when it returns. A buffer that
/// is never disposed is released by the garbage collector.
/// </remarks>
public class Buffer : IDisposable
{
    private readonly GObjectSafeHandle _safeHandle;

    /// <summary>The native buffer pointer, for tests. Library code takes a lease instead.</summary>
    internal IntPtr Handle
    {
        get
        {
            using var lease = AcquireHandle();
            return lease.Handle;
        }
    }

    /// <summary>
    /// Creates a new buffer with the specified size
    /// </summary>
    public Buffer(IntPtr size)
    {
        _safeHandle = new GObjectSafeHandle(AravisNative.arv_buffer_new_allocate(ToUIntPtr(size)));

        if (_safeHandle.IsInvalid)
        {
            throw new AravisException("Failed to allocate buffer");
        }
    }

    /// <summary>
    /// Creates a new buffer with the specified size in bytes.
    /// </summary>
    public Buffer(int size)
        : this(new IntPtr(size))
    {
    }

    private Buffer(GObjectSafeHandle safeHandle)
    {
        _safeHandle = safeHandle;
    }

    /// <summary>Wraps a buffer popped from a stream.</summary>
    /// <param name="handle">A buffer the caller owns a reference on (<c>transfer full</c>); the wrapper takes it over.</param>
    /// <exception cref="ArgumentException"><paramref name="handle"/> is null.</exception>
    internal static Buffer FromOwnedHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            throw new ArgumentException("A null native buffer cannot be wrapped.", nameof(handle));
        return new Buffer(new GObjectSafeHandle(handle));
    }

    /// <summary>
    /// Queues this buffer on <paramref name="streamHandle"/> and closes the wrapper: the stream
    /// gets a reference of its own, and the wrapper's reference is dropped once the calls in
    /// progress on other threads have returned, whatever the stream does with the buffer
    /// meanwhile.
    /// </summary>
    /// <param name="streamHandle">A stream pointer the caller holds a lease on.</param>
    /// <exception cref="ObjectDisposedException">
    /// The buffer was already disposed or pushed (only one of several concurrent pushes wins).
    /// </exception>
    internal void PushTo(IntPtr streamHandle)
    {
        using var lease = AcquireHandle();
        if (!_safeHandle.BeginClose())
            throw new ObjectDisposedException(nameof(Buffer));

        try
        {
            // arv_stream_push_buffer takes over a reference (transfer full): give it a new
            // one, so that the wrapper's own reference is still released exactly once, below.
            GLibNative.g_object_ref(lease.Handle);
            try
            {
                AravisNative.arv_stream_push_buffer(streamHandle, lease.Handle);
            }
            catch
            {
                // Not queued (the call itself failed, a missing entry point for example):
                // the stream did not take the new reference.
                GLibNative.g_object_unref(lease.Handle);
                throw;
            }
        }
        finally
        {
            // Closed either way: the wrapper's reference goes once the leases are returned.
            _safeHandle.Dispose();
        }
    }

    /// <summary>
    /// Gets the buffer status
    /// </summary>
    public ArvBufferStatus Status
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_status(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the image width in pixels
    /// </summary>
    public int Width
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_image_width(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the image height in pixels
    /// </summary>
    public int Height
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_image_height(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the pixel format
    /// </summary>
    public uint PixelFormat
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_image_pixel_format(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the buffer timestamp in nanoseconds
    /// </summary>
    public ulong Timestamp
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_timestamp(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the frame ID
    /// </summary>
    public ulong FrameId
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_frame_id(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the image origin X offset within the sensor
    /// </summary>
    public int ImageX
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_image_x(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the image origin Y offset within the sensor
    /// </summary>
    public int ImageY
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_image_y(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the row padding (extra bytes at end of each row)
    /// </summary>
    public (int XPadding, int YPadding) GetImagePadding()
    {
        using var lease = AcquireHandle();
        AravisNative.arv_buffer_get_image_padding(lease.Handle, out int x, out int y);
        return (x, y);
    }

    /// <summary>
    /// Gets the payload type (Image, ChunkData, RawData, etc.)
    /// </summary>
    public ArvBufferPayloadType PayloadType
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_payload_type(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the system (wall-clock) timestamp in nanoseconds
    /// </summary>
    public ulong SystemTimestamp
    {
        get
        {
            using var lease = AcquireHandle();
            return AravisNative.arv_buffer_get_system_timestamp(lease.Handle);
        }
    }

    /// <summary>
    /// Gets the image region
    /// </summary>
    public (int X, int Y, int Width, int Height) GetImageRegion()
    {
        using var lease = AcquireHandle();
        AravisNative.arv_buffer_get_image_region(lease.Handle, out int x, out int y, out int width, out int height);
        return (x, y, width, height);
    }

    /// <summary>
    /// Gets the raw buffer data.
    /// </summary>
    /// <returns>Pointer to buffer data and size</returns>
    /// <remarks>
    /// <para>
    /// The pointer refers to native memory owned by this Buffer, and nothing pins it once this
    /// method has returned: it is valid only while this Buffer is reachable and has been
    /// neither disposed nor pushed back to a stream, by any thread. Once the Buffer is no
    /// longer referenced, the garbage collector may release the memory even while the pointer
    /// is still in use: in <c>stream.PopBuffer()!.GetData()</c>, for example, nothing keeps the
    /// Buffer alive. Keep it in a variable (a <c>using</c> declaration) and call
    /// <see cref="GC.KeepAlive(object)"/> on it after the last read.
    /// </para>
    /// <para>
    /// Prefer <see cref="CopyData"/> or <see cref="CopyDataTo"/>, which hold the buffer for the
    /// whole copy.
    /// </para>
    /// </remarks>
    public (IntPtr Data, int Size) GetData()
    {
        using var lease = AcquireHandle();
        var dataPtr = AravisNative.arv_buffer_get_data(lease.Handle, out UIntPtr sizePtr);
        ulong nativeSize = sizePtr.ToUInt64();
        if (nativeSize > int.MaxValue)
        {
            throw new InvalidOperationException($"Buffer data is too large for a managed span or array: {nativeSize} bytes.");
        }

        int size = (int)nativeSize;
        return (dataPtr, size);
    }

    /// <summary>
    /// Copies the buffer data to a byte array
    /// </summary>
    /// <remarks>
    /// The buffer is held for the whole copy: disposing or pushing it on another thread
    /// meanwhile cannot free the memory being read. Pushing it gives it back to the stream
    /// thread, though, which may start filling it with the next frame while the copy runs:
    /// the copy then holds a mix of two frames. Do not push a buffer that is still being read.
    /// </remarks>
    public byte[] CopyData() => ReadData(static data => data.ToArray());

    /// <summary>
    /// Copies the buffer data to a provided span
    /// </summary>
    /// <remarks>Held for the whole copy, with the same limit as <see cref="CopyData"/>: do not push a buffer that is still being read.</remarks>
    public unsafe void CopyDataTo(Span<byte> destination)
    {
        // The lease holds the buffer until the copy is done.
        using var lease = AcquireHandle();
        var (dataPtr, size) = GetData();

        if (dataPtr == IntPtr.Zero || size <= 0)
        {
            return;
        }

        if (destination.Length < size)
        {
            throw new ArgumentException($"Destination buffer is too small. Required: {size}, Available: {destination.Length}");
        }

        new ReadOnlySpan<byte>((void*)dataPtr, size).CopyTo(destination);
    }

    /// <summary>
    /// Gets a read-only span of the buffer data (zero-copy access).
    /// </summary>
    /// <returns>Read-only span of the buffer data</returns>
    /// <remarks>
    /// The span points into native memory that nothing pins once this method has returned,
    /// with the limits of <see cref="GetData"/>: it is valid only while this Buffer is
    /// reachable and has been neither disposed nor pushed back to a stream. In
    /// <c>stream.PopBuffer()!.GetDataSpan()</c> nothing keeps the Buffer alive, and the
    /// garbage collector may release the memory while the span is still read. Keep the Buffer
    /// in a variable (a <c>using</c> declaration) and call <see cref="GC.KeepAlive(object)"/>
    /// on it after the last read, or use <see cref="CopyData"/> / <see cref="CopyDataTo"/>.
    /// </remarks>
    public unsafe ReadOnlySpan<byte> GetDataSpan()
    {
        var (dataPtr, size) = GetData();

        if (dataPtr == IntPtr.Zero || size <= 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return new ReadOnlySpan<byte>((void*)dataPtr, size);
    }

    /// <summary>Reads the data of <see cref="ReadData{TResult}"/>, while the buffer is held.</summary>
    internal delegate TResult DataReader<TResult>(ReadOnlySpan<byte> data);

    /// <summary>
    /// Runs <paramref name="read"/> on the buffer data while holding the buffer, so that no
    /// Dispose(), push or finalizer can free the memory before it returns. The span must not
    /// escape <paramref name="read"/>.
    /// </summary>
    internal unsafe TResult ReadData<TResult>(DataReader<TResult> read)
    {
        using var lease = AcquireHandle();
        var (dataPtr, size) = GetData();
        var data = dataPtr == IntPtr.Zero || size <= 0
            ? ReadOnlySpan<byte>.Empty
            : new ReadOnlySpan<byte>((void*)dataPtr, size);
        return read(data);
    }

    // Not serialised: a buffer's accessors only read it. The stream thread writes into a
    // buffer once it has been pushed, which closes this wrapper first; a read already in
    // progress then sees a mix of two frames (see CopyData).
    internal GObjectLease AcquireHandle() => _safeHandle.Acquire(nameof(Buffer));

    /// <summary>
    /// Releases the native buffer. Safe to call more than once and from any thread: from the
    /// moment it is called every member throws <see cref="ObjectDisposedException"/>, and the
    /// native buffer is released once the calls already in progress have returned. Does
    /// nothing to a buffer that was pushed to a stream: the stream owns it.
    /// </summary>
    public void Dispose()
    {
        // No catch: g_object_unref reports nothing to catch, and an exception here (a missing
        // native library) is a real failure, not a clean-up detail to hide.
        _safeHandle.BeginClose();
        _safeHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    private static UIntPtr ToUIntPtr(IntPtr size)
    {
        long value = size.ToInt64();
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Buffer size must be non-negative.");
        }

        return new UIntPtr((ulong)value);
    }
}
