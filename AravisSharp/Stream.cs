using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Socket buffer policy for GigE Vision streams
/// </summary>
public enum ArvGvStreamSocketBuffer
{
    /// <summary>
    /// Use the socket buffer size set with <see cref="Stream.SetSocketBufferSize"/>; a size of
    /// 0 (the default) leaves the operating system's default receive buffer.
    /// </summary>
    Fixed = 0,
    /// <summary>
    /// Size the socket buffer to one payload (plus 1 KiB), capped by
    /// <see cref="Stream.SetSocketBufferSize"/> when that size is positive.
    /// </summary>
    Auto = 1
}

/// <summary>
/// Packet resend policy for GigE Vision streams
/// </summary>
public enum ArvGvStreamPacketResend
{
    /// <summary>Never request packet resend</summary>
    Never = 0,
    /// <summary>
    /// Request a resend of the packets still missing after the initial packet timeout.
    /// Aravis' default, unless the device reports no packet resend support.
    /// </summary>
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
    /// <see cref="ArvGvStreamSocketBuffer.Auto"/> sizes the receive buffer to one payload,
    /// capped by <see cref="SetSocketBufferSize"/>; <see cref="ArvGvStreamSocketBuffer.Fixed"/>
    /// (Aravis' default) uses <see cref="SetSocketBufferSize"/> as is.
    /// </summary>
    public void SetSocketBufferPolicy(ArvGvStreamSocketBuffer policy)
    {
        CheckDisposed();
        CheckGigE();
        GLibNative.SetEnumProperty(_handle, "socket-buffer", AravisNative.arv_gv_stream_socket_buffer_get_type(), (int)policy);
        GC.KeepAlive(this);
    }

    /// <summary>
    /// Sets the socket buffer size (in bytes) for GigE Vision streams: the receive buffer size
    /// with the <see cref="ArvGvStreamSocketBuffer.Fixed"/> policy, its upper bound with
    /// <see cref="ArvGvStreamSocketBuffer.Auto"/>. 0 or less means "not set": the operating
    /// system default with Fixed, one payload with Auto. Aravis applies it when the first
    /// frame arrives.
    /// </summary>
    public void SetSocketBufferSize(int sizeBytes)
    {
        CheckDisposed();
        CheckGigE();
        GLibNative.SetIntProperty(_handle, "socket-buffer-size", sizeBytes);
        GC.KeepAlive(this);
    }

    /// <summary>
    /// Gets the socket buffer size setting (the socket-buffer-size property, not the size the
    /// operating system actually granted).
    /// </summary>
    public int GetSocketBufferSize()
    {
        CheckDisposed();
        CheckGigE();
        var size = GLibNative.GetIntProperty(_handle, "socket-buffer-size");
        GC.KeepAlive(this);
        return size;
    }

    /// <summary>
    /// Sets the packet resend policy for GigE Vision streams.
    /// Aravis already selects <see cref="ArvGvStreamPacketResend.Always"/> when the device
    /// reports packet resend support and <see cref="ArvGvStreamPacketResend.Never"/> otherwise;
    /// with Never, an incomplete frame is closed as soon as the next one starts.
    /// </summary>
    public void SetPacketResend(ArvGvStreamPacketResend policy)
    {
        CheckDisposed();
        CheckGigE();
        GLibNative.SetEnumProperty(_handle, "packet-resend", AravisNative.arv_gv_stream_packet_resend_get_type(), (int)policy);
        GC.KeepAlive(this);
    }

    /// <summary>Gets the packet resend policy of the GigE Vision stream.</summary>
    public ArvGvStreamPacketResend GetPacketResend()
    {
        CheckDisposed();
        CheckGigE();
        var policy = GLibNative.GetEnumProperty(_handle, "packet-resend", AravisNative.arv_gv_stream_packet_resend_get_type());
        GC.KeepAlive(this);
        return (ArvGvStreamPacketResend)policy;
    }

    /// <summary>
    /// Sets the initial packet timeout in microseconds for GigE Vision streams: how long a
    /// packet may be missing before the first resend request is sent. It only absorbs packets
    /// arriving out of order; Aravis' default is 1000 (1 ms).
    /// Keep it well below the frame retention (<see cref="SetFrameRetention"/>, 100 ms by
    /// default): an incomplete frame is closed as <see cref="ArvBufferStatus.Timeout"/> once
    /// no packet arrived for it during the frame retention, so a longer initial timeout means
    /// no resend is ever requested.
    /// </summary>
    public void SetInitialPacketTimeout(uint timeoutUs)
    {
        CheckDisposed();
        CheckGigE();
        GLibNative.SetUIntProperty(_handle, "initial-packet-timeout", timeoutUs);
        GC.KeepAlive(this);
    }

    /// <summary>Gets the initial packet timeout of the GigE Vision stream, in microseconds.</summary>
    public uint GetInitialPacketTimeout()
    {
        CheckDisposed();
        CheckGigE();
        var timeout = GLibNative.GetUIntProperty(_handle, "initial-packet-timeout");
        GC.KeepAlive(this);
        return timeout;
    }

    /// <summary>
    /// Sets the packet timeout in microseconds for GigE Vision streams: how long to wait for a
    /// requested packet before asking for it again. Aravis' default is 20000 (20 ms). A frame
    /// gets roughly (frame retention - initial packet timeout) / packet timeout requests per
    /// missing packet before it times out.
    /// </summary>
    public void SetPacketTimeout(uint timeoutUs)
    {
        CheckDisposed();
        CheckGigE();
        GLibNative.SetUIntProperty(_handle, "packet-timeout", timeoutUs);
        GC.KeepAlive(this);
    }

    /// <summary>Gets the packet timeout of the GigE Vision stream, in microseconds.</summary>
    public uint GetPacketTimeout()
    {
        CheckDisposed();
        CheckGigE();
        var timeout = GLibNative.GetUIntProperty(_handle, "packet-timeout");
        GC.KeepAlive(this);
        return timeout;
    }

    /// <summary>
    /// Sets the frame retention in microseconds for GigE Vision streams: an incomplete frame
    /// that received no packet for this long is closed as <see cref="ArvBufferStatus.Timeout"/>.
    /// Aravis' default is 100000 (100 ms). Frames are delivered in order, so a frame waiting for
    /// packets also holds back the complete frames behind it, and their buffers.
    /// </summary>
    public void SetFrameRetention(uint timeoutUs)
    {
        CheckDisposed();
        CheckGigE();
        GLibNative.SetUIntProperty(_handle, "frame-retention", timeoutUs);
        GC.KeepAlive(this);
    }

    /// <summary>Gets the frame retention of the GigE Vision stream, in microseconds.</summary>
    public uint GetFrameRetention()
    {
        CheckDisposed();
        CheckGigE();
        var timeout = GLibNative.GetUIntProperty(_handle, "frame-retention");
        GC.KeepAlive(this);
        return timeout;
    }

    /// <summary>
    /// Configures the GigE Vision stream for reliable operation. Call this right after
    /// CreateStream() for GigE cameras, before starting acquisition.
    /// <list type="bullet">
    /// <item>Socket buffer <see cref="ArvGvStreamSocketBuffer.Auto"/>: one payload, capped at
    /// <paramref name="socketBufferSizeMB"/>. Aravis' own default keeps the operating system
    /// default (64 KiB on Windows); Aravis' viewer and arv-camera-test offer Auto as an
    /// option (-a).</item>
    /// <item>Resend timings at Aravis' defaults: initial packet timeout 1 ms, packet timeout
    /// 20 ms, frame retention 100 ms. A missing packet is requested after 1 ms, then again every
    /// 20 ms, about four times before its frame times out. The initial packet timeout must stay
    /// well below the frame retention: AravisSharp 0.8.36 set it to 1 s here, so no resend
    /// request was ever sent and every lost packet turned its frame into a Timeout.</item>
    /// <item>The packet resend policy is left as Aravis set it from the device's capabilities
    /// (see <see cref="SetPacketResend"/>): forcing Always on a device without packet resend
    /// only makes incomplete frames wait for the frame retention.</item>
    /// </list>
    /// Resends recover occasional losses. Sustained loss (a saturated or shared 1 GbE link, a
    /// NIC that drops packets) needs a lower frame rate or an inter-packet delay
    /// (<see cref="Camera.GvSetPacketDelay"/>); <see cref="GetGigEStatistics"/> shows both.
    /// </summary>
    /// <param name="socketBufferSizeMB">Upper bound of the socket buffer in megabytes (default: 4);
    /// 0 sizes it to one payload without a cap.</param>
    public void ConfigureGigEDefaults(int socketBufferSizeMB = 4)
    {
        CheckDisposed();
        if (socketBufferSizeMB < 0 || socketBufferSizeMB > int.MaxValue / (1024 * 1024))
            throw new ArgumentOutOfRangeException(nameof(socketBufferSizeMB), socketBufferSizeMB,
                "The socket buffer cap must be between 0 and 2047 MB.");

        SetSocketBufferPolicy(ArvGvStreamSocketBuffer.Auto);
        SetSocketBufferSize(socketBufferSizeMB * 1024 * 1024);
        SetInitialPacketTimeout(DefaultInitialPacketTimeoutUs);
        SetPacketTimeout(DefaultPacketTimeoutUs);
        SetFrameRetention(DefaultFrameRetentionUs);
    }

    // ARV_GV_STREAM_*_DEFAULT in Aravis' arvgvstreamprivate.h.
    internal const uint DefaultInitialPacketTimeoutUs = 1_000;
    internal const uint DefaultPacketTimeoutUs = 20_000;
    internal const uint DefaultFrameRetentionUs = 100_000;

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
        GC.KeepAlive(this);
    }

    /// <summary>
    /// Pops a buffer from the output queue (non-blocking, immediate return).
    /// Returns null if no buffer is ready.
    /// </summary>
    public Buffer? TryPopBuffer()
    {
        CheckDisposed();
        return WrapBuffer(AravisNative.arv_stream_try_pop_buffer(_handle));
    }

    /// <summary>
    /// Pops a buffer from the output queue, blocking until one is available.
    /// Use <see cref="PopBuffer(ulong)"/> or <see cref="TryPopBuffer"/> to avoid waiting forever.
    /// </summary>
    public Buffer? PopBuffer()
    {
        CheckDisposed();
        return WrapBuffer(AravisNative.arv_stream_pop_buffer(_handle));
    }

    /// <summary>
    /// Pops a buffer from the output queue with timeout
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (0 = non-blocking, ulong.MaxValue = infinite)</param>
    /// <returns>Buffer or null if timeout occurred</returns>
    public Buffer? PopBuffer(ulong timeoutMs)
    {
        CheckDisposed();
        if (timeoutMs == ulong.MaxValue)
            return WrapBuffer(AravisNative.arv_stream_pop_buffer(_handle));

        // Convert milliseconds to microseconds. GLib adds the timeout to a signed
        // 64-bit monotonic time, so clamp it well below overflow.
        ulong timeoutUs = Math.Min(timeoutMs, (ulong)long.MaxValue / 2000) * 1000;
        return WrapBuffer(AravisNative.arv_stream_timeout_pop_buffer(_handle, timeoutUs));
    }

    private Buffer? WrapBuffer(IntPtr bufferHandle)
    {
        GC.KeepAlive(this);
        return bufferHandle == IntPtr.Zero ? null : new Buffer(bufferHandle, true);
    }

    /// <summary>
    /// Gets the number of buffers currently in the input and output queues.
    /// Each count is a GLib async queue length: the items queued minus the threads waiting
    /// on that queue, so InputBuffers can be negative: a USB3 Vision stream thread waits on
    /// the input queue and takes buffers from it to prepare its transfers.
    /// </summary>
    public (int InputBuffers, int OutputBuffers) GetBufferCounts()
    {
        CheckDisposed();
        AravisNative.arv_stream_get_n_buffers(_handle, out int input, out int output);
        GC.KeepAlive(this);
        return (input, output);
    }

    /// <summary>
    /// Gets stream statistics
    /// </summary>
    public (ulong CompletedBuffers, ulong Failures, ulong Underruns) GetStatistics()
    {
        CheckDisposed();
        AravisNative.arv_stream_get_statistics(_handle, out ulong completed, out ulong failures, out ulong underruns);
        GC.KeepAlive(this);
        return (completed, failures, underruns);
    }

    /// <summary>
    /// Gets GigE stream diagnostics when the stream is an ArvGvStream: the local stream port,
    /// the packets received after a resend request (losses that were recovered), and the
    /// missing packets of the frames that failed. For each failed frame, MissingPackets counts
    /// every packet after the first gap, including packets that did arrive, so it
    /// overstates the loss; it stays 0 while resends recover every loss.
    /// </summary>
    public (ushort Port, ulong ResentPackets, ulong MissingPackets) GetGigEStatistics()
    {
        CheckDisposed();
        CheckGigE();
        var port = AravisNative.arv_gv_stream_get_port(_handle);
        AravisNative.arv_gv_stream_get_statistics(_handle, out ulong resent, out ulong missing);
        GC.KeepAlive(this);
        return (port, resent, missing);
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Stream));
        }
    }

    private void CheckGigE()
    {
        if (!GLibNative.g_type_check_instance_is_a(_handle, AravisNative.arv_gv_stream_get_type()))
            throw new InvalidOperationException("This operation requires a GigE Vision stream.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero)
            {
                // arv_stream_finalize releases the buffers left in both queues.
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
