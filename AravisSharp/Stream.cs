using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Represents a video stream from a camera
/// </summary>
public class Stream : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    internal Stream(IntPtr handle)
    {
        _handle = handle;
        if (_handle != IntPtr.Zero)
        {
            // Don't emit signals by default (we'll use polling)
            AravisNative.arv_stream_set_emit_signals(_handle, false);
        }
    }

    /// <summary>
    /// Pushes a buffer to the input queue for filling
    /// </summary>
    public void PushBuffer(Buffer buffer)
    {
        CheckDisposed();
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        
        AravisNative.arv_stream_push_buffer(_handle, buffer.Handle);
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

        return new Buffer(bufferHandle, false);
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

        return new Buffer(bufferHandle, false);
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
