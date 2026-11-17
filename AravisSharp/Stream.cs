using System.Diagnostics;
using System.Runtime.CompilerServices;
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
/// <remarks>
/// <para>
/// Members may be called from several threads, for example one thread waiting in
/// <see cref="PopBuffer()"/> while another pushes buffers; calls are not serialised (the
/// stream's queues are thread-safe in Aravis). Once <see cref="Dispose"/> has been called,
/// every member throws <see cref="ObjectDisposedException"/>; a native call already in
/// progress on another thread completes normally, and the native stream is released when it
/// returns. A member made of several native calls (<see cref="ConfigureGigEDefaults"/>, and
/// the GigE members, which check the stream type first) can therefore throw
/// <see cref="ObjectDisposedException"/> part-way, with only some of its calls made.
/// </para>
/// <para>
/// A stream holds its own native reference on the camera's device, so it stays usable after
/// the <see cref="Camera"/> is disposed, and keeps the camera's connection open until it is
/// disposed itself: dispose the stream as well as the camera before opening the same camera
/// again. A stream that is never disposed is released by the garbage collector, on the
/// finalizer thread; Aravis then stops the stream thread there and releases the buffers
/// still queued.
/// </para>
/// </remarks>
public class Stream : IDisposable
{
    private readonly GObjectSafeHandle _safeHandle;

    /// <summary>The native stream pointer, for tests. Library code takes a lease instead.</summary>
    internal IntPtr Handle
    {
        get
        {
            using var lease = AcquireHandle();
            return lease.Handle;
        }
    }

    /// <summary>
    /// True when this is a GigE Vision stream (an ArvGvStream), on which the GigE members
    /// (<see cref="SetSocketBufferPolicy"/>, <see cref="SetSocketBufferSize"/>, the packet
    /// resend and timing members, <see cref="ConfigureGigEDefaults"/> and
    /// <see cref="GetGigEStatistics"/>) can be used. On any other stream (USB3 Vision, the
    /// Aravis fake camera) they throw <see cref="NotSupportedException"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
    public bool IsGigEVision
    {
        get
        {
            using var lease = AcquireHandle();
            return GLibNative.g_type_check_instance_is_a(lease.Handle, AravisNative.arv_gv_stream_get_type());
        }
    }

    /// <param name="handle">A stream the caller owns a reference on (<c>transfer full</c>); the wrapper takes it over.</param>
    /// <exception cref="ArgumentException"><paramref name="handle"/> is null.</exception>
    internal Stream(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            throw new ArgumentException("A null native stream cannot be wrapped.", nameof(handle));

        _safeHandle = new GObjectSafeHandle(handle);
        try
        {
            // Don't emit signals by default (we'll use polling)
            using var lease = AcquireHandle();
            AravisNative.arv_stream_set_emit_signals(lease.Handle, false);
        }
        catch
        {
            // No wrapper is returned: release the stream now rather than in a finalizer.
            _safeHandle.Dispose();
            throw;
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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy"/> is not a member of the enum: Aravis refused it and kept the previous policy.</exception>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void SetSocketBufferPolicy(ArvGvStreamSocketBuffer policy)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        GLibNative.SetEnumProperty(lease.Handle, "socket-buffer", AravisNative.arv_gv_stream_socket_buffer_get_type(), (int)policy, nameof(policy));
    }

    /// <summary>
    /// Sets the socket buffer size (in bytes) for GigE Vision streams: the receive buffer size
    /// with the <see cref="ArvGvStreamSocketBuffer.Fixed"/> policy, its upper bound with
    /// <see cref="ArvGvStreamSocketBuffer.Auto"/>. 0 or less means "not set": the operating
    /// system default with Fixed, one payload with Auto. Aravis applies it when the first
    /// frame arrives.
    /// <para>
    /// On Linux the kernel silently caps the receive buffer at <c>net.core.rmem_max</c>
    /// (about 208 KiB by default): a larger size is reduced without an error, and Aravis only
    /// reports it as a warning of its debug output, shown when that is enabled
    /// (<c>ARV_DEBUG=interface</c>). Raise the cap
    /// (<c>sysctl -w net.core.rmem_max=&lt;bytes&gt;</c>) for the requested size to take effect.
    /// <see cref="GetSocketBufferSize"/> returns the requested size, not the granted one.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeBytes"/> is below -1, the property's minimum: Aravis refused it and kept the previous size.</exception>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void SetSocketBufferSize(int sizeBytes)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        GLibNative.SetIntProperty(lease.Handle, "socket-buffer-size", sizeBytes, nameof(sizeBytes));
    }

    /// <summary>
    /// Gets the socket buffer size setting (the socket-buffer-size property, not the size the
    /// operating system actually granted).
    /// </summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public int GetSocketBufferSize()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        var size = GLibNative.GetIntProperty(lease.Handle, "socket-buffer-size");
        return size;
    }

    /// <summary>
    /// Sets the packet resend policy for GigE Vision streams.
    /// Aravis already selects <see cref="ArvGvStreamPacketResend.Always"/> when the device
    /// reports packet resend support and <see cref="ArvGvStreamPacketResend.Never"/> otherwise;
    /// with Never, an incomplete frame is closed as soon as the next one starts.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy"/> is not a member of the enum: Aravis refused it and kept the previous policy.</exception>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void SetPacketResend(ArvGvStreamPacketResend policy)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        GLibNative.SetEnumProperty(lease.Handle, "packet-resend", AravisNative.arv_gv_stream_packet_resend_get_type(), (int)policy, nameof(policy));
    }

    /// <summary>Gets the packet resend policy of the GigE Vision stream.</summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public ArvGvStreamPacketResend GetPacketResend()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        var policy = GLibNative.GetEnumProperty(lease.Handle, "packet-resend", AravisNative.arv_gv_stream_packet_resend_get_type());
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
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void SetInitialPacketTimeout(uint timeoutUs)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        GLibNative.SetUIntProperty(lease.Handle, "initial-packet-timeout", timeoutUs, nameof(timeoutUs));
    }

    /// <summary>Gets the initial packet timeout of the GigE Vision stream, in microseconds.</summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public uint GetInitialPacketTimeout()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        var timeout = GLibNative.GetUIntProperty(lease.Handle, "initial-packet-timeout");
        return timeout;
    }

    /// <summary>
    /// Sets the packet timeout in microseconds for GigE Vision streams: how long to wait for a
    /// requested packet before asking for it again. Aravis' default is 20000 (20 ms). A frame
    /// gets roughly (frame retention - initial packet timeout) / packet timeout requests per
    /// missing packet before it times out.
    /// </summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void SetPacketTimeout(uint timeoutUs)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        GLibNative.SetUIntProperty(lease.Handle, "packet-timeout", timeoutUs, nameof(timeoutUs));
    }

    /// <summary>Gets the packet timeout of the GigE Vision stream, in microseconds.</summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public uint GetPacketTimeout()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        var timeout = GLibNative.GetUIntProperty(lease.Handle, "packet-timeout");
        return timeout;
    }

    /// <summary>
    /// Sets the frame retention in microseconds for GigE Vision streams: an incomplete frame
    /// that received no packet for this long is closed as <see cref="ArvBufferStatus.Timeout"/>.
    /// Aravis' default is 100000 (100 ms). Frames are delivered in order, so a frame waiting for
    /// packets also holds back the complete frames behind it, and their buffers.
    /// </summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void SetFrameRetention(uint timeoutUs)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        GLibNative.SetUIntProperty(lease.Handle, "frame-retention", timeoutUs, nameof(timeoutUs));
    }

    /// <summary>Gets the frame retention of the GigE Vision stream, in microseconds.</summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public uint GetFrameRetention()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        var timeout = GLibNative.GetUIntProperty(lease.Handle, "frame-retention");
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
    /// <remarks>
    /// On Linux the kernel silently caps the socket buffer at <c>net.core.rmem_max</c> (about
    /// 208 KiB by default), so the 4 MB default is reduced to that cap unless it is raised
    /// (<c>sysctl -w net.core.rmem_max=&lt;bytes&gt;</c>); Aravis only reports it when its debug
    /// output is enabled (<c>ARV_DEBUG=interface</c>). See <see cref="SetSocketBufferSize"/>.
    /// </remarks>
    /// <param name="socketBufferSizeMB">Upper bound of the socket buffer in megabytes (default: 4);
    /// 0 sizes it to one payload without a cap.</param>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public void ConfigureGigEDefaults(int socketBufferSizeMB = 4)
    {
        using var lease = AcquireHandle();
        CheckGigE();
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
    /// The <paramref name="buffer"/> wrapper is closed: its members throw
    /// <see cref="ObjectDisposedException"/> from now on, and disposing it does nothing.
    /// </summary>
    /// <remarks>
    /// A read of the buffer already in progress on another thread
    /// (<see cref="Buffer.CopyData"/>, for example) keeps its memory valid, but the stream
    /// thread may already be filling it with a new frame: that read returns a mix of two
    /// frames. Push a buffer only once nothing reads it any more.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// The stream is disposed, or <paramref name="buffer"/> was already disposed or pushed. Of
    /// several threads pushing the same buffer at once, one queues it and the others get this
    /// exception.
    /// </exception>
    public void PushBuffer(Buffer buffer)
    {
        using var lease = AcquireHandle();
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        buffer.PushTo(lease.Handle);
    }

    /// <summary>
    /// Pops a buffer from the output queue (non-blocking, immediate return).
    /// Returns null if no buffer is ready.
    /// </summary>
    public Buffer? TryPopBuffer()
    {
        using var lease = AcquireHandle();
        return WrapBuffer(AravisNative.arv_stream_try_pop_buffer(lease.Handle));
    }

    /// <summary>
    /// Pops a buffer from the output queue, blocking until one is available.
    /// Use <see cref="PopBuffer(ulong)"/> or <see cref="TryPopBuffer"/> to avoid waiting forever.
    /// </summary>
    /// <remarks>
    /// The wait is made of timed native pops of at most 100 ms: a buffer is returned as soon
    /// as it is ready, and when <see cref="Dispose"/> is called on another thread the wait
    /// ends with <see cref="ObjectDisposedException"/> within about 100 ms.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// The stream was disposed, possibly by another thread while this call was waiting.
    /// </exception>
    public Buffer? PopBuffer() => WaitForBuffer(timeoutUs: null);

    /// <summary>
    /// Pops a buffer from the output queue with timeout
    /// </summary>
    /// <param name="timeoutMs">
    /// Timeout in milliseconds (0 = non-blocking, ulong.MaxValue = infinite). Any other value
    /// above 4,611,686,018,427,387 ms (<c>long.MaxValue / 2000</c>, about 146,000 years) is
    /// capped to it.
    /// </param>
    /// <returns>Buffer or null if timeout occurred</returns>
    /// <remarks>
    /// The wait is made of timed native pops of at most 100 ms: a buffer is returned as soon
    /// as it is ready, and when <see cref="Dispose"/> is called on another thread the wait
    /// ends with <see cref="ObjectDisposedException"/> within about 100 ms.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// The stream was disposed, possibly by another thread while this call was waiting.
    /// </exception>
    public Buffer? PopBuffer(ulong timeoutMs) =>
        WaitForBuffer(timeoutMs == ulong.MaxValue ? null : ToTimeoutMicroseconds(timeoutMs));

    /// <summary>
    /// Converts a <see cref="PopBuffer(ulong)"/> timeout from milliseconds to the
    /// microseconds passed to <c>arv_stream_timeout_pop_buffer</c>. GLib adds the timeout
    /// to a signed 64-bit monotonic time, so it is capped at <c>long.MaxValue / 2000</c> ms
    /// (about 146,000 years), which keeps the result at or below <c>long.MaxValue / 2</c> µs.
    /// </summary>
    internal static ulong ToTimeoutMicroseconds(ulong timeoutMs) =>
        Math.Min(timeoutMs, (ulong)long.MaxValue / 2000) * 1000;

    // A native pop cannot be interrupted, and disposing the stream while a thread waits on
    // its output queue makes GLib log a critical and leak the queue. So waits are cut in
    // slices: a waiting thread notices Dispose() within one slice and returns its lease.
    internal const ulong WaitSliceUs = 100_000;

    /// <summary>
    /// Waits for a buffer for <paramref name="timeoutUs"/> microseconds (null: no limit), in
    /// slices of at most <see cref="WaitSliceUs"/>, giving up with
    /// <see cref="ObjectDisposedException"/> once the stream is closing, or once
    /// <paramref name="watched"/> is (named <paramref name="watchedName"/> in the exception).
    /// </summary>
    internal Buffer? WaitForBuffer(ulong? timeoutUs, GObjectSafeHandle? watched = null, string? watchedName = null)
    {
        using var lease = AcquireHandle();
        var clock = Stopwatch.StartNew();
        while (true)
        {
            ulong slice = WaitSliceUs;
            if (timeoutUs is ulong limit)
            {
                ulong spent = ElapsedMicroseconds(clock);
                slice = spent >= limit ? 0 : Math.Min(slice, limit - spent);
            }

            var bufferHandle = AravisNative.arv_stream_timeout_pop_buffer(lease.Handle, slice);
            if (bufferHandle != IntPtr.Zero)
                return Buffer.FromOwnedHandle(bufferHandle);

            if (_safeHandle.IsClosing)
                throw new ObjectDisposedException(nameof(Stream));
            if (watched is not null && watched.IsClosing)
                throw new ObjectDisposedException(watchedName);
            if (timeoutUs is ulong total && ElapsedMicroseconds(clock) >= total)
                return null;
        }
    }

    private static ulong ElapsedMicroseconds(Stopwatch clock) =>
        (ulong)clock.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);

    private static Buffer? WrapBuffer(IntPtr bufferHandle) =>
        bufferHandle == IntPtr.Zero ? null : Buffer.FromOwnedHandle(bufferHandle);

    /// <summary>
    /// Gets the number of buffers currently in the input and output queues.
    /// Each count is a GLib async queue length: the items queued minus the threads waiting
    /// on that queue, so InputBuffers can be negative: a USB3 Vision stream thread waits on
    /// the input queue and takes buffers from it to prepare its transfers.
    /// </summary>
    public (int InputBuffers, int OutputBuffers) GetBufferCounts()
    {
        using var lease = AcquireHandle();
        AravisNative.arv_stream_get_n_buffers(lease.Handle, out int input, out int output);
        return (input, output);
    }

    /// <summary>
    /// Gets stream statistics
    /// </summary>
    public (ulong CompletedBuffers, ulong Failures, ulong Underruns) GetStatistics()
    {
        using var lease = AcquireHandle();
        AravisNative.arv_stream_get_statistics(lease.Handle, out ulong completed, out ulong failures, out ulong underruns);
        return (completed, failures, underruns);
    }

    /// <summary>
    /// Gets GigE stream diagnostics when the stream is an ArvGvStream: the local stream port,
    /// the packets received after a resend request (losses that were recovered), and the
    /// missing packets of the frames that failed. For each failed frame, MissingPackets counts
    /// every packet after the first gap, including packets that did arrive, so it
    /// overstates the loss; it stays 0 while resends recover every loss.
    /// </summary>
    /// <exception cref="NotSupportedException">The stream is not a GigE Vision stream (see <see cref="IsGigEVision"/>).</exception>
    public (ushort Port, ulong ResentPackets, ulong MissingPackets) GetGigEStatistics()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        var port = AravisNative.arv_gv_stream_get_port(lease.Handle);
        AravisNative.arv_gv_stream_get_statistics(lease.Handle, out ulong resent, out ulong missing);
        return (port, resent, missing);
    }

    // Not serialised: the stream queues are thread-safe in Aravis (GAsyncQueue), and a
    // blocking PopBuffer() must not keep other threads out.
    internal GObjectLease AcquireHandle() => _safeHandle.Acquire(nameof(Stream));

    // The one GigE guard of Stream: called under the member's lease and before any native call
    // of a GigE member. Same message pattern as Camera.CheckGigE.
    private void CheckGigE([CallerMemberName] string member = "")
    {
        if (!IsGigEVision)
            throw new NotSupportedException(
                $"{member} requires a GigE Vision stream; check {nameof(Stream)}.{nameof(IsGigEVision)} first.");
    }

    /// <summary>
    /// Releases the stream. Safe to call more than once and from any thread: from the moment
    /// it is called every member throws <see cref="ObjectDisposedException"/>, a
    /// <see cref="PopBuffer()"/> waiting on another thread gives up with the same exception
    /// within about 100 ms, and the native stream is released once the calls in progress have
    /// returned. Aravis then releases the buffers left in both queues.
    /// </summary>
    /// <remarks>
    /// The stream keeps the camera's connection open on its own: dispose the stream as well as
    /// the camera before opening the same camera again.
    /// </remarks>
    public void Dispose()
    {
        _safeHandle.BeginClose();
        _safeHandle.Dispose();
        GC.SuppressFinalize(this);
    }
}
