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
    private readonly bool _ownsHandle;

    internal IntPtr Handle => _handle;

    /// <summary>
    /// Creates a new buffer with the specified size
    /// </summary>
    public Buffer(IntPtr size)
    {
        _handle = AravisNative.arv_buffer_new_allocate(size);
        _ownsHandle = true;
        
        if (_handle == IntPtr.Zero)
        {
            throw new AravisException("Failed to allocate buffer");
        }
    }

    internal Buffer(IntPtr handle, bool ownsHandle)
    {
        _handle = handle;
        _ownsHandle = ownsHandle;
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
    public unsafe (IntPtr Data, int Size) GetData()
    {
        CheckDisposed();
        var dataPtr = AravisNative.arv_buffer_get_data(_handle, out IntPtr sizePtr);
        int size = (int)sizePtr.ToInt64();
        return (dataPtr, size);
    }

    /// <summary>
    /// Copies the buffer data to a byte array
    /// </summary>
    public unsafe byte[] CopyData()
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
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~Buffer()
    {
        // Don't call Dispose in finalizer to avoid issues
        if (_handle != IntPtr.Zero && _ownsHandle)
        {
            _handle = IntPtr.Zero;
        }
    }
}
