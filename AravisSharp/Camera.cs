using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Represents a GenICam-compatible camera
/// </summary>
public class Camera : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    internal IntPtr Handle => _handle;

    /// <summary>
    /// Opens a camera by device ID
    /// </summary>
    /// <param name="deviceId">Device ID (null for first available camera)</param>
    public Camera(string? deviceId = null)
    {
        IntPtr error = IntPtr.Zero;
        IntPtr deviceIdPtr = IntPtr.Zero;

        try
        {
            if (!string.IsNullOrEmpty(deviceId))
            {
                deviceIdPtr = Marshal.StringToCoTaskMemUTF8(deviceId);
            }

            _handle = AravisNative.arv_camera_new(deviceIdPtr, out error);
            
            if (error != IntPtr.Zero)
            {
                throw new AravisException(GetErrorMessage(error));
            }

            if (_handle == IntPtr.Zero)
            {
                throw new AravisException("Failed to open camera");
            }
        }
        finally
        {
            if (deviceIdPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(deviceIdPtr);
            }
            if (error != IntPtr.Zero)
            {
                AravisNative.g_error_free(error);
            }
        }
    }

    /// <summary>
    /// Gets the camera vendor name
    /// </summary>
    public string GetVendorName()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_vendor_name(_handle, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the camera model name
    /// </summary>
    public string GetModelName()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_model_name(_handle, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the camera serial number
    /// </summary>
    public string GetSerialNumber()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_device_serial_number(_handle, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the camera device ID
    /// </summary>
    public string GetDeviceId()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_device_id(_handle, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current region of interest (ROI)
    /// </summary>
    public (int X, int Y, int Width, int Height) GetRegion()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_region(_handle, out int x, out int y, out int width, out int height, out error);
            CheckError(error);
            return (x, y, width, height);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the region of interest (ROI)
    /// </summary>
    public void SetRegion(int x, int y, int width, int height)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_region(_handle, x, y, width, height, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the minimum and maximum allowed width
    /// </summary>
    public (int Min, int Max) GetWidthBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_width_bounds(_handle, out int min, out int max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the minimum and maximum allowed height
    /// </summary>
    public (int Min, int Max) GetHeightBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_height_bounds(_handle, out int min, out int max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current exposure time in microseconds
    /// </summary>
    public double GetExposureTime()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var value = AravisNative.arv_camera_get_exposure_time(_handle, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the exposure time in microseconds
    /// </summary>
    public void SetExposureTime(double exposureTimeUs)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_exposure_time(_handle, exposureTimeUs, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the minimum and maximum exposure time bounds in microseconds
    /// </summary>
    public (double Min, double Max) GetExposureTimeBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_exposure_time_bounds(_handle, out double min, out double max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current gain value
    /// </summary>
    public double GetGain()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var value = AravisNative.arv_camera_get_gain(_handle, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the gain value
    /// </summary>
    public void SetGain(double gain)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_gain(_handle, gain, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the minimum and maximum gain bounds
    /// </summary>
    public (double Min, double Max) GetGainBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_gain_bounds(_handle, out double min, out double max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current frame rate in frames per second
    /// </summary>
    public double GetFrameRate()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var value = AravisNative.arv_camera_get_frame_rate(_handle, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the frame rate in frames per second
    /// </summary>
    public void SetFrameRate(double frameRate)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_frame_rate(_handle, frameRate, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current pixel format as a string
    /// </summary>
    public string GetPixelFormat()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_pixel_format_as_string(_handle, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the pixel format from a string
    /// </summary>
    public void SetPixelFormat(string format)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr formatPtr = IntPtr.Zero;
        try
        {
            formatPtr = Marshal.StringToCoTaskMemUTF8(format);
            AravisNative.arv_camera_set_pixel_format_from_string(_handle, formatPtr, out error);
            CheckError(error);
        }
        finally
        {
            if (formatPtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(formatPtr);
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Starts image acquisition
    /// </summary>
    public void StartAcquisition()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_start_acquisition(_handle, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Stops image acquisition
    /// </summary>
    public void StopAcquisition()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_stop_acquisition(_handle, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Aborts ongoing acquisition
    /// </summary>
    public void AbortAcquisition()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_abort_acquisition(_handle, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Triggers software trigger
    /// </summary>
    public void SoftwareTrigger()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_software_trigger(_handle, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Creates a stream for acquiring images from the camera
    /// </summary>
    public Stream CreateStream()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var streamHandle = AravisNative.arv_camera_create_stream(_handle, IntPtr.Zero, IntPtr.Zero, out error);
            CheckError(error);
            
            if (streamHandle == IntPtr.Zero)
            {
                throw new AravisException("Failed to create stream");
            }

            return new Stream(streamHandle);
        }
        finally
        {
            if (error != IntPtr.Zero)
                AravisNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the underlying device handle for low-level access
    /// </summary>
    public Device GetDevice()
    {
        CheckDisposed();
        var deviceHandle = AravisNative.arv_camera_get_device(_handle);
        if (deviceHandle == IntPtr.Zero)
        {
            throw new AravisException("Failed to get device");
        }
        return new Device(deviceHandle);
    }

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Camera));
        }
    }

    private void CheckError(IntPtr error)
    {
        if (error != IntPtr.Zero)
        {
            throw new AravisException(GetErrorMessage(error));
        }
    }

    private static string GetErrorMessage(IntPtr error)
    {
        if (error == IntPtr.Zero)
            return "Unknown error";

        var gerror = Marshal.PtrToStructure<GError>(error);
        return Marshal.PtrToStringUTF8(gerror.Message) ?? "Unknown error";
    }

    private static string MarshalString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != IntPtr.Zero)
            {
                AravisNative.g_object_unref(_handle);
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~Camera()
    {
        Dispose();
    }
}
