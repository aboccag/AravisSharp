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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the minimum and maximum frame rate bounds in Hz
    /// </summary>
    public (double Min, double Max) GetFrameRateBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_frame_rate_bounds(_handle, out double min, out double max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current binning settings
    /// </summary>
    public (int Horizontal, int Vertical) GetBinning()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_binning(_handle, out int dx, out int dy, out error);
            CheckError(error);
            return (dx, dy);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the binning in both horizontal and vertical directions.
    /// Not all cameras support this feature. Negative values are ignored.
    /// </summary>
    public void SetBinning(int horizontal, int vertical)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_binning(_handle, horizontal, vertical, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Configures the camera in trigger mode.
    /// Sets TriggerSelector to "FrameStart" (falls back to "AcquisitionStart"),
    /// TriggerMode to "On", TriggerSource to the given source, and
    /// TriggerActivation to rising edge. All other triggers are disabled.
    /// Typical sources: "Software", "Line1", "Line2".
    /// </summary>
    /// <param name="source">Trigger source (e.g. "Software", "Line1")</param>
    public void SetTrigger(string source)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr sourcePtr = IntPtr.Zero;
        try
        {
            sourcePtr = Marshal.StringToCoTaskMemUTF8(source);
            AravisNative.arv_camera_set_trigger(_handle, sourcePtr, out error);
            CheckError(error);
        }
        finally
        {
            if (sourcePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(sourcePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the trigger source without changing the trigger mode.
    /// This does not check if the camera is configured to actually use this source as a trigger.
    /// </summary>
    /// <param name="source">Trigger source name</param>
    public void SetTriggerSource(string source)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr sourcePtr = IntPtr.Zero;
        try
        {
            sourcePtr = Marshal.StringToCoTaskMemUTF8(source);
            AravisNative.arv_camera_set_trigger_source(_handle, sourcePtr, out error);
            CheckError(error);
        }
        finally
        {
            if (sourcePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(sourcePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the current trigger source.
    /// This does not check if the camera is configured to actually use this source as a trigger.
    /// </summary>
    /// <returns>Trigger source name</returns>
    public string GetTriggerSource()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_trigger_source(_handle, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Disables all triggers, returning the camera to free-running mode.
    /// </summary>
    public void ClearTriggers()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_clear_triggers(_handle, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks whether the camera supports software trigger.
    /// </summary>
    /// <returns>true if software trigger is supported</returns>
    public bool IsSoftwareTriggerSupported()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var result = AravisNative.arv_camera_is_software_trigger_supported(_handle, out error);
            CheckError(error);
            return result;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the payload size needed for buffer allocation.
    /// This value accounts for the current pixel format, region of interest, and any chunk data.
    /// </summary>
    /// <returns>Payload size in bytes</returns>
    public uint GetPayloadSize()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var size = AravisNative.arv_camera_get_payload(_handle, out error);
            CheckError(error);
            return size;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
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
                GLibNative.g_error_free(error);
        }
    }

    // === Sensor Size ===

    /// <summary>
    /// Gets the sensor size (maximum width and height)
    /// </summary>
    public (int Width, int Height) GetSensorSize()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_sensor_size(_handle, out int width, out int height, out error);
            CheckError(error);
            return (width, height);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Command Execution ===

    /// <summary>
    /// Executes a GenICam command
    /// </summary>
    /// <param name=\"feature\">Command feature name</param>
    public void ExecuteCommand(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_execute_command(_handle, featurePtr, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Acquisition Mode ===

    /// <summary>
    /// Gets the current acquisition mode
    /// </summary>
    public ArvAcquisitionMode GetAcquisitionMode()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_get_acquisition_mode(_handle, out error);
            CheckError(error);
            return (ArvAcquisitionMode)mode;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the acquisition mode
    /// </summary>
    public void SetAcquisitionMode(ArvAcquisitionMode mode)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_acquisition_mode(_handle, (int)mode, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Frame Count (for MultiFrame mode) ===

    /// <summary>
    /// Gets the frame count for MultiFrame acquisition mode
    /// </summary>
    public long GetFrameCount()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var count = AravisNative.arv_camera_get_frame_count(_handle, out error);
            CheckError(error);
            return count;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the number of frames to capture in MultiFrame mode
    /// </summary>
    public void SetFrameCount(long count)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_frame_count(_handle, count, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the allowed range for frame count
    /// </summary>
    public (long Min, long Max) GetFrameCountBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_frame_count_bounds(_handle, out long min, out long max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Auto Exposure ===

    /// <summary>
    /// Gets the automatic exposure mode
    /// </summary>
    public ArvAuto GetExposureTimeAuto()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_get_exposure_time_auto(_handle, out error);
            CheckError(error);
            return (ArvAuto)mode;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the automatic exposure mode
    /// </summary>
    public void SetExposureTimeAuto(ArvAuto mode)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_exposure_time_auto(_handle, (int)mode, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Auto Gain ===

    /// <summary>
    /// Gets the automatic gain mode
    /// </summary>
    public ArvAuto GetGainAuto()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_get_gain_auto(_handle, out error);
            CheckError(error);
            return (ArvAuto)mode;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the automatic gain mode
    /// </summary>
    public void SetGainAuto(ArvAuto mode)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_gain_auto(_handle, (int)mode, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Generic Feature Access (for advanced use) ===

    /// <summary>
    /// Gets a string feature value
    /// </summary>
    public string GetStringFeature(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var ptr = AravisNative.arv_camera_get_string(_handle, featurePtr, out error);
            CheckError(error);
            return MarshalString(ptr);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a string feature value
    /// </summary>
    public void SetStringFeature(string feature, string value)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        IntPtr valuePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            valuePtr = Marshal.StringToCoTaskMemUTF8(value);
            AravisNative.arv_camera_set_string(_handle, featurePtr, valuePtr, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (valuePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(valuePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets an integer feature value
    /// </summary>
    public long GetIntegerFeature(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var value = AravisNative.arv_camera_get_integer(_handle, featurePtr, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets an integer feature value
    /// </summary>
    public void SetIntegerFeature(string feature, long value)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_set_integer(_handle, featurePtr, value, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets a float feature value
    /// </summary>
    public double GetFloatFeature(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var value = AravisNative.arv_camera_get_float(_handle, featurePtr, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a float feature value
    /// </summary>
    public void SetFloatFeature(string feature, double value)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_set_float(_handle, featurePtr, value, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets a boolean feature value
    /// </summary>
    public bool GetBooleanFeature(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var value = AravisNative.arv_camera_get_boolean(_handle, featurePtr, out error);
            CheckError(error);
            return value;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets a boolean feature value
    /// </summary>
    public void SetBooleanFeature(string feature, bool value)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_set_boolean(_handle, featurePtr, value, out error);
            CheckError(error);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Feature Bounds ===

    /// <summary>
    /// Gets the bounds for an integer feature
    /// </summary>
    public (long Min, long Max) GetIntegerFeatureBounds(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_get_integer_bounds(_handle, featurePtr, out long min, out long max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the bounds for a float feature
    /// </summary>
    public (double Min, double Max) GetFloatFeatureBounds(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_get_float_bounds(_handle, featurePtr, out double min, out double max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Feature Increments ===

    /// <summary>
    /// Gets the increment value for an integer feature
    /// </summary>
    public long GetIntegerFeatureIncrement(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var increment = AravisNative.arv_camera_get_integer_increment(_handle, featurePtr, out error);
            CheckError(error);
            return increment;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the increment value for a float feature
    /// </summary>
    public double GetFloatFeatureIncrement(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var increment = AravisNative.arv_camera_get_float_increment(_handle, featurePtr, out error);
            CheckError(error);
            return increment;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the width increment
    /// </summary>
    public int GetWidthIncrement()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var increment = AravisNative.arv_camera_get_width_increment(_handle, out error);
            CheckError(error);
            return increment;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the height increment
    /// </summary>
    public int GetHeightIncrement()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var increment = AravisNative.arv_camera_get_height_increment(_handle, out error);
            CheckError(error);
            return increment;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Feature Availability ===

    /// <summary>
    /// Checks if a feature is available
    /// </summary>
    public bool IsFeatureAvailable(string feature)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var available = AravisNative.arv_camera_is_feature_available(_handle, featurePtr, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero)
                Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if binning is available
    /// </summary>
    public bool IsBinningAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_binning_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if exposure time control is available
    /// </summary>
    public bool IsExposureTimeAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_exposure_time_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if automatic exposure is available
    /// </summary>
    public bool IsExposureAutoAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_exposure_auto_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if gain control is available
    /// </summary>
    public bool IsGainAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_gain_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if automatic gain is available
    /// </summary>
    public bool IsGainAutoAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_gain_auto_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if frame rate control is available
    /// </summary>
    public bool IsFrameRateAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_frame_rate_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Device Type Checks ===

    /// <summary>
    /// Checks if this is a GigE Vision device
    /// </summary>
    public bool IsGigEVisionDevice()
    {
        CheckDisposed();
        return AravisNative.arv_camera_is_gv_device(_handle);
    }

    /// <summary>
    /// Checks if this is a USB3 Vision device
    /// </summary>
    public bool IsUSB3VisionDevice()
    {
        CheckDisposed();
        return AravisNative.arv_camera_is_uv_device(_handle);
    }

    // === GigE Vision Specific ===

    /// <summary>
    /// Automatically determines and sets the optimal packet size for GigE Vision cameras
    /// </summary>
    public void GvAutoPacketSize()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_gv_auto_packet_size(_handle, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the packet size for GigE Vision cameras (in bytes)
    /// </summary>
    public int GvGetPacketSize()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var size = AravisNative.arv_camera_gv_get_packet_size(_handle, out error);
            CheckError(error);
            return size;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the packet size for GigE Vision cameras (in bytes)
    /// </summary>
    public void GvSetPacketSize(int size)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_gv_set_packet_size(_handle, size, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === USB3 Vision Specific ===

    /// <summary>
    /// Gets the bandwidth limit for USB3 Vision cameras (in bytes/second)
    /// </summary>
    public int UvGetBandwidth()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var bandwidth = AravisNative.arv_camera_uv_get_bandwidth(_handle, out error);
            CheckError(error);
            return bandwidth;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the bandwidth limit for USB3 Vision cameras (in bytes/second).
    /// A value &lt;= 0 disables the limit.
    /// </summary>
    public void UvSetBandwidth(int bandwidth)
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_uv_set_bandwidth(_handle, bandwidth, out error);
            CheckError(error);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the bandwidth bounds for USB3 Vision cameras (in bytes/second)
    /// </summary>
    public (int Min, int Max) UvGetBandwidthBounds()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_uv_get_bandwidth_bounds(_handle, out int min, out int max, out error);
            CheckError(error);
            return (min, max);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Checks if bandwidth control is available on this USB3 Vision device
    /// </summary>
    public bool UvIsBandwidthControlAvailable()
    {
        CheckDisposed();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_uv_is_bandwidth_control_available(_handle, out error);
            CheckError(error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Stream Creation ===

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
                GLibNative.g_error_free(error);
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
                GLibNative.g_object_unref(_handle);
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
