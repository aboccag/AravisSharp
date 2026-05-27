using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Represents an image buffer from a camera stream
/// </summary>
public class Buffer : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;
    private bool _ownsHandle;

    internal IntPtr Handle
    {
        get
        {
            CheckDisposed();
            return _handle;
        }
    }

    /// <summary>
    /// Creates a new buffer with the specified size
    /// </summary>
    public Buffer(IntPtr size)
    {
        _handle = AravisNative.arv_buffer_new_allocate(ToUIntPtr(size));
        _ownsHandle = true;
        
        if (_handle == IntPtr.Zero)
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

    internal Buffer(IntPtr handle, bool ownsHandle)
    {
        _handle = handle;
        _ownsHandle = ownsHandle;
    }

    internal void ReleaseOwnership()
    {
        CheckDisposed();
        _ownsHandle = false;
        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the buffer status
    /// </summary>
    public ArvBufferStatus Status
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_status(_handle);
        }
    }

    /// <summary>
    /// Gets the image width in pixels
    /// </summary>
    public int Width
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_image_width(_handle);
        }
    }

    /// <summary>
    /// Gets the image height in pixels
    /// </summary>
    public int Height
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_image_height(_handle);
        }
    }

    /// <summary>
    /// Gets the pixel format
    /// </summary>
    public uint PixelFormat
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_image_pixel_format(_handle);
        }
    }

    /// <summary>
    /// Gets the buffer timestamp in nanoseconds
    /// </summary>
    public ulong Timestamp
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_timestamp(_handle);
        }
    }

    /// <summary>
    /// Gets the frame ID
    /// </summary>
    public ulong FrameId
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_frame_id(_handle);
        }
    }

    /// <summary>
    /// Gets the image origin X offset within the sensor
    /// </summary>
    public int ImageX
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_image_x(_handle);
        }
    }

    /// <summary>
    /// Gets the image origin Y offset within the sensor
    /// </summary>
    public int ImageY
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_image_y(_handle);
        }
    }

    /// <summary>
    /// Gets the row padding (extra bytes at end of each row)
    /// </summary>
    public (int XPadding, int YPadding) GetImagePadding()
    {
        CheckDisposed();
        AravisNative.arv_buffer_get_image_padding(_handle, out int x, out int y);
        return (x, y);
    }

    /// <summary>
    /// Gets the payload type (Image, ChunkData, RawData, etc.)
    /// </summary>
    public ArvBufferPayloadType PayloadType
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_payload_type(_handle);
        }
    }

    /// <summary>
    /// Gets the system (wall-clock) timestamp in nanoseconds
    /// </summary>
    public ulong SystemTimestamp
    {
        get
        {
            CheckDisposed();
            return AravisNative.arv_buffer_get_system_timestamp(_handle);
        }
    }

    /// <summary>
    /// Gets the image region
    /// </summary>
    public (int X, int Y, int Width, int Height) GetImageRegion()
    {
        CheckDisposed();
        AravisNative.arv_buffer_get_image_region(_handle, out int x, out int y, out int width, out int height);
        return (x, y, width, height);
    }

    /// <summary>
    /// Gets the raw buffer data
    /// </summary>
    /// <returns>Pointer to buffer data and size</returns>
    public (IntPtr Data, int Size) GetData()
    {
        CheckDisposed();
        var dataPtr = AravisNative.arv_buffer_get_data(_handle, out UIntPtr sizePtr);
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
    public byte[] CopyData()
    {
        CheckDisposed();
        var (dataPtr, size) = GetData();
        
        if (dataPtr == IntPtr.Zero || size <= 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[size];
        Marshal.Copy(dataPtr, buffer, 0, size);
        return buffer;
    }

    /// <summary>
    /// Copies the buffer data to a provided span
    /// </summary>
    public unsafe void CopyDataTo(Span<byte> destination)
    {
        CheckDisposed();
        var (dataPtr, size) = GetData();
        
        if (dataPtr == IntPtr.Zero || size <= 0)
        {
            return;
        }

        if (destination.Length < size)
        {
            throw new ArgumentException($"Destination buffer is too small. Required: {size}, Available: {destination.Length}");
        }

        var source = new Span<byte>((void*)dataPtr, size);
        source.CopyTo(destination);
    }

    /// <summary>
    /// Gets a read-only span of the buffer data (zero-copy access)
    /// </summary>
    /// <returns>Read-only span of the buffer data</returns>
    public unsafe ReadOnlySpan<byte> GetDataSpan()
    {
        CheckDisposed();
        var (dataPtr, size) = GetData();
        
        if (dataPtr == IntPtr.Zero || size <= 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return new ReadOnlySpan<byte>((void*)dataPtr, size);
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Buffer));
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero && _ownsHandle)
            {
                try
                {
                    GLibNative.g_object_unref(_handle);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            _handle = IntPtr.Zero;
            _disposed = true;
        }
    }

    ~Buffer()
    {
        Dispose(disposing: false);
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
