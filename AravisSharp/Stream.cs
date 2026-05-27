using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Socket buffer policy for GigE Vision streams
/// </summary>
public enum ArvGvStreamSocketBuffer
{
    /// <summary>Use a fixed socket buffer size (set via SocketBufferSize)</summary>
    Fixed = 0,
    /// <summary>Automatically size the socket buffer based on payload</summary>
    Auto = 1
}

/// <summary>
/// Packet resend policy for GigE Vision streams
/// </summary>
public enum ArvGvStreamPacketResend
{
    /// <summary>Never request packet resend</summary>
    Never = 0,
    /// <summary>Always request packet resend when a gap is detected</summary>
    Always = 1
}

/// <summary>
/// Represents a video stream from a camera
/// </summary>
public class Stream : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    internal IntPtr Handle => _handle;

    internal Stream(IntPtr handle)
    {
        _handle = handle;
        if (_handle != IntPtr.Zero)
        {
            // Don't emit signals by default (we'll use polling)
            AravisNative.arv_stream_set_emit_signals(_handle, false);
        }
    }

    // === GigE Vision Stream Configuration ===
    // These properties use GObject property access on the underlying ArvGvStream.
    // Only valid when the stream was created from a GigE Vision camera.

    /// <summary>
    /// Sets the socket buffer policy for GigE Vision streams.
    /// Use Auto to let Aravis size the receive buffer based on payload size.
    /// </summary>
    public void SetSocketBufferPolicy(ArvGvStreamSocketBuffer policy)
    {
        CheckDisposed();
        GLibNative.g_object_set_int(_handle, "socket-buffer", (int)policy, IntPtr.Zero);
    }

    /// <summary>
    /// Sets the socket buffer size (in bytes) for GigE Vision streams.
    /// A larger buffer reduces the chance of packet loss at high frame rates.
    /// Typical values: 1MB (1048576) to 16MB (16777216).
    /// </summary>
    public void SetSocketBufferSize(int sizeBytes)
    {
        CheckDisposed();
        GLibNative.g_object_set_int(_handle, "socket-buffer-size", sizeBytes, IntPtr.Zero);
    }

    /// <summary>
    /// Gets the current socket buffer size for GigE Vision streams.
    /// </summary>
    public int GetSocketBufferSize()
    {
        CheckDisposed();
        GLibNative.g_object_get_int(_handle, "socket-buffer-size", out int size, IntPtr.Zero);
        return size;
    }

    /// <summary>
    /// Sets the packet resend policy for GigE Vision streams.
    /// When set to Always, Aravis will request the camera to resend missing packets,
    /// which is critical for reliable GigE Vision operation.
    /// </summary>
    public void SetPacketResend(ArvGvStreamPacketResend policy)
    {
        CheckDisposed();
        GLibNative.g_object_set_int(_handle, "packet-resend", (int)policy, IntPtr.Zero);
    }

    /// <summary>
    /// Sets the initial packet timeout in microseconds for GigE Vision streams.
    /// This is the maximum time to wait for the first packet of a frame.
    /// Default is typically 1000000 (1 second).
    /// </summary>
    public void SetInitialPacketTimeout(uint timeoutUs)
    {
        CheckDisposed();
        GLibNative.g_object_set_uint(_handle, "initial-packet-timeout", timeoutUs, IntPtr.Zero);
    }

    /// <summary>
    /// Sets the packet timeout in microseconds for GigE Vision streams.
    /// This is the maximum time to wait between consecutive packets in a frame.
    /// Default is typically 40000 (40ms). Lower for faster detection of missing packets.
    /// </summary>
    public void SetPacketTimeout(uint timeoutUs)
    {
        CheckDisposed();
        GLibNative.g_object_set_uint(_handle, "packet-timeout", timeoutUs, IntPtr.Zero);
    }

    /// <summary>
    /// Configures the GigE Vision stream with recommended settings for reliable operation.
    /// Call this immediately after CreateStream() for GigE cameras.
    /// </summary>
    /// <param name="socketBufferSizeMB">Socket buffer size in megabytes (default: 4)</param>
    public void ConfigureGigEDefaults(int socketBufferSizeMB = 4)
    {
        CheckDisposed();
        SetSocketBufferPolicy(ArvGvStreamSocketBuffer.Auto);
        SetSocketBufferSize(socketBufferSizeMB * 1024 * 1024);
        SetPacketResend(ArvGvStreamPacketResend.Always);
        SetPacketTimeout(40000);           // 40ms
        SetInitialPacketTimeout(1000000);  // 1s
    }

    /// <summary>
    /// Pushes a buffer to the input queue for filling and transfers ownership to the stream.
    /// </summary>
    public void PushBuffer(Buffer buffer)
    {
        CheckDisposed();
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        
        AravisNative.arv_stream_push_buffer(_handle, buffer.Handle);
        buffer.ReleaseOwnership();
    }

    /// <summary>
    /// Pops a buffer from the output queue (non-blocking, immediate return).
    /// Returns null if no buffer is ready.
    /// </summary>
    public Buffer? TryPopBuffer()
    {
        CheckDisposed();
        var bufferHandle = AravisNative.arv_stream_try_pop_buffer(_handle);
        if (bufferHandle == IntPtr.Zero)
            return null;
        return new Buffer(bufferHandle, true);
    }

    /// <summary>
    /// Pops a buffer from the output queue (non-blocking)
    /// </summary>
    /// <returns>Buffer or null if no buffer is available</returns>
    public Buffer? PopBuffer()
    {
        CheckDisposed();
        var bufferHandle = AravisNative.arv_stream_pop_buffer(_handle);
        if (bufferHandle == IntPtr.Zero)
            return null;

        return new Buffer(bufferHandle, true);
    }

    /// <summary>
    /// Pops a buffer from the output queue with timeout
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (0 = non-blocking, ulong.MaxValue = infinite)</param>
    /// <returns>Buffer or null if timeout occurred</returns>
    public Buffer? PopBuffer(ulong timeoutMs)
    {
        CheckDisposed();
        // Convert milliseconds to microseconds
        ulong timeoutUs = timeoutMs * 1000;
        var bufferHandle = AravisNative.arv_stream_timeout_pop_buffer(_handle, timeoutUs);
        if (bufferHandle == IntPtr.Zero)
            return null;

        return new Buffer(bufferHandle, true);
    }

    /// <summary>
    /// Gets the number of buffers currently in the input and output queues.
    /// </summary>
    public (int InputBuffers, int OutputBuffers) GetBufferCounts()
    {
        CheckDisposed();
        AravisNative.arv_stream_get_n_buffers(_handle, out int input, out int output);
        return (input, output);
    }

    /// <summary>
    /// Gets stream statistics
    /// </summary>
    public (ulong CompletedBuffers, ulong Failures, ulong Underruns) GetStatistics()
    {
        CheckDisposed();
        AravisNative.arv_stream_get_statistics(_handle, out ulong completed, out ulong failures, out ulong underruns);
        return (completed, failures, underruns);
    }

    /// <summary>
    /// Gets GigE stream diagnostics when the stream is an ArvGvStream.
    /// </summary>
    public (ushort Port, ulong ResentPackets, ulong MissingPackets) GetGigEStatistics()
    {
        CheckDisposed();
        var port = AravisNative.arv_gv_stream_get_port(_handle);
        AravisNative.arv_gv_stream_get_statistics(_handle, out ulong resent, out ulong missing);
        return (port, resent, missing);
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Stream));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero)
            {
                // Drain remaining buffers with timeout to avoid hanging
                // Give up after reasonable attempts (max 1 second total)
                const int maxAttempts = 10;
                const ulong timeoutMs = 100; // 100ms per attempt
                
                for (int i = 0; i < maxAttempts; i++)
                {
                    var bufferHandle = AravisNative.arv_stream_timeout_pop_buffer(_handle, timeoutMs * 1000);
                    if (bufferHandle == IntPtr.Zero)
                        break; // No more buffers
                    // Release ownership of the buffer popped from the output queue.
                    // arv_stream_timeout_pop_buffer transfers ownership to the caller.
                    GLibNative.g_object_unref(bufferHandle);
                }
                
                GLibNative.g_object_unref(_handle);
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~Stream()
    {
        Dispose();
    }
}
