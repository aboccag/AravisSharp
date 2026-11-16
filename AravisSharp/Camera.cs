using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Represents a GenICam-compatible camera
/// </summary>
/// <remarks>
/// <para>
/// Members may be called from several threads. Calls on one camera, and on the
/// <see cref="Device"/>, <see cref="GenICam.NodeMap"/> and <see cref="GenICam.GenICamNode"/>
/// objects obtained from it, are serialised by a lock of the camera: one call at a time. The
/// lock covers single calls, not sequences: another thread may change a feature between two
/// calls of yours (between a bounds query and a set, for example). <see cref="Stream"/> and
/// <see cref="Buffer"/> calls do not take it.
/// </para>
/// <para>
/// Once <see cref="Dispose"/> has been called, every member of the camera and of its
/// <see cref="Device"/> and node map throws <see cref="ObjectDisposedException"/>; a call
/// already in progress on another thread completes normally, and the native camera is released
/// when it returns. A <see cref="Stream"/> keeps its own native reference: to open the same
/// camera again, dispose the camera and every stream created from it; a camera that is never
/// disposed keeps its connection until the garbage collector releases it.
/// </para>
/// </remarks>
public class Camera : IDisposable
{
    private readonly GObjectSafeHandle _safeHandle;

    /// <summary>The native camera pointer, for tests. Library code takes a lease instead.</summary>
    internal IntPtr Handle
    {
        get
        {
            using var lease = AcquireHandle();
            return lease.Handle;
        }
    }

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

            var handle = AravisNative.arv_camera_new(deviceIdPtr, out error);

            if (error != IntPtr.Zero)
            {
                if (handle != IntPtr.Zero)
                    GLibNative.g_object_unref(handle);
                throw AravisException.TakeGError(ref error);
            }

            if (handle == IntPtr.Zero)
            {
                throw new AravisException("Failed to open camera");
            }

            _safeHandle = new GObjectSafeHandle(handle);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_vendor_name(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_model_name(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_device_serial_number(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_device_id(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_region(lease.Handle, out int x, out int y, out int width, out int height, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_region(lease.Handle, x, y, width, height, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_width_bounds(lease.Handle, out int min, out int max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_height_bounds(lease.Handle, out int min, out int max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var value = AravisNative.arv_camera_get_exposure_time(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_exposure_time(lease.Handle, exposureTimeUs, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_exposure_time_bounds(lease.Handle, out double min, out double max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var value = AravisNative.arv_camera_get_gain(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_gain(lease.Handle, gain, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_gain_bounds(lease.Handle, out double min, out double max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var value = AravisNative.arv_camera_get_frame_rate(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_frame_rate(lease.Handle, frameRate, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_frame_rate_bounds(lease.Handle, out double min, out double max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_binning(lease.Handle, out int dx, out int dy, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_binning(lease.Handle, horizontal, vertical, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_pixel_format_as_string(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr formatPtr = IntPtr.Zero;
        try
        {
            formatPtr = Marshal.StringToCoTaskMemUTF8(format);
            AravisNative.arv_camera_set_pixel_format_from_string(lease.Handle, formatPtr, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_start_acquisition(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_stop_acquisition(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_abort_acquisition(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr sourcePtr = IntPtr.Zero;
        try
        {
            sourcePtr = Marshal.StringToCoTaskMemUTF8(source);
            AravisNative.arv_camera_set_trigger(lease.Handle, sourcePtr, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr sourcePtr = IntPtr.Zero;
        try
        {
            sourcePtr = Marshal.StringToCoTaskMemUTF8(source);
            AravisNative.arv_camera_set_trigger_source(lease.Handle, sourcePtr, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_get_trigger_source(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_clear_triggers(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var result = AravisNative.arv_camera_is_software_trigger_supported(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var size = AravisNative.arv_camera_get_payload(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_software_trigger(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_sensor_size(lease.Handle, out int width, out int height, out error);
            CheckError(ref error);
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
    /// <param name="feature">Command feature name</param>
    public void ExecuteCommand(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_execute_command(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_get_acquisition_mode(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_acquisition_mode(lease.Handle, (int)mode, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var count = AravisNative.arv_camera_get_frame_count(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_frame_count(lease.Handle, count, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_frame_count_bounds(lease.Handle, out long min, out long max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_get_exposure_time_auto(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_exposure_time_auto(lease.Handle, (int)mode, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_get_gain_auto(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_gain_auto(lease.Handle, (int)mode, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var ptr = AravisNative.arv_camera_get_string(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        IntPtr valuePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            valuePtr = Marshal.StringToCoTaskMemUTF8(value);
            AravisNative.arv_camera_set_string(lease.Handle, featurePtr, valuePtr, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var value = AravisNative.arv_camera_get_integer(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_set_integer(lease.Handle, featurePtr, value, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var value = AravisNative.arv_camera_get_float(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_set_float(lease.Handle, featurePtr, value, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var value = AravisNative.arv_camera_get_boolean(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_set_boolean(lease.Handle, featurePtr, value, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_get_integer_bounds(lease.Handle, featurePtr, out long min, out long max, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            AravisNative.arv_camera_get_float_bounds(lease.Handle, featurePtr, out double min, out double max, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var increment = AravisNative.arv_camera_get_integer_increment(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var increment = AravisNative.arv_camera_get_float_increment(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var increment = AravisNative.arv_camera_get_width_increment(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var increment = AravisNative.arv_camera_get_height_increment(lease.Handle, out error);
            CheckError(ref error);
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
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var available = AravisNative.arv_camera_is_feature_available(lease.Handle, featurePtr, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_binning_available(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_exposure_time_available(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_exposure_auto_available(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_gain_available(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_gain_auto_available(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_is_frame_rate_available(lease.Handle, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        var result = AravisNative.arv_camera_is_gv_device(lease.Handle);
        return result;
    }

    /// <summary>
    /// Checks if this is a USB3 Vision device
    /// </summary>
    public bool IsUSB3VisionDevice()
    {
        using var lease = AcquireHandle();
        var result = AravisNative.arv_camera_is_uv_device(lease.Handle);
        return result;
    }

    // === GigE Vision Specific ===

    /// <summary>
    /// Automatically determines and sets the optimal packet size for GigE Vision cameras.
    /// The negotiated size is not returned; read it with <see cref="GvGetPacketSize"/>.
    /// </summary>
    /// <remarks>
    /// Aravis may negotiate nothing, without an error, when the device has no packet-size test
    /// (<c>GevSCPSFireTestPacket</c>): compare <see cref="GvGetPacketSize"/> before and after the
    /// call to know whether the size changed.
    /// </remarks>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public void GvAutoPacketSize()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            // Returns the negotiated size (guint); the public API keeps the v0.8.36 void signature.
            _ = AravisNative.arv_camera_gv_auto_packet_size(lease.Handle, out error);
            CheckError(ref error);
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
    /// <remarks>
    /// Aravis returns a <c>guint</c>; a GigE Vision packet size is a 16-bit field
    /// (<c>GevSCPSPacketSize</c>), so it always fits an <see cref="int"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    /// <exception cref="OverflowException">Aravis returned a size above <see cref="int.MaxValue"/>, which no GigE Vision camera can report.</exception>
    public int GvGetPacketSize()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            var size = AravisNative.arv_camera_gv_get_packet_size(lease.Handle, out error);
            CheckError(ref error);
            return ToPacketSize(size);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>The <c>guint</c> packet size of Aravis as an <see cref="int"/>, checked.</summary>
    /// <exception cref="OverflowException"><paramref name="size"/> is above <see cref="int.MaxValue"/>.</exception>
    internal static int ToPacketSize(uint size) => checked((int)size);

    /// <summary>
    /// Sets the packet size for GigE Vision cameras (in bytes)
    /// </summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public void GvSetPacketSize(int size)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_gv_set_packet_size(lease.Handle, size, out error);
            CheckError(ref error);
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
    public uint UvGetBandwidth()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var bandwidth = AravisNative.arv_camera_uv_get_bandwidth(lease.Handle, out error);
            CheckError(ref error);
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
    /// 0 disables the limit.
    /// </summary>
    public void UvSetBandwidth(uint bandwidth)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_uv_set_bandwidth(lease.Handle, bandwidth, out error);
            CheckError(ref error);
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
    public (uint Min, uint Max) UvGetBandwidthBounds()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_uv_get_bandwidth_bounds(lease.Handle, out uint min, out uint max, out error);
            CheckError(ref error);
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
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var available = AravisNative.arv_camera_uv_is_bandwidth_control_available(lease.Handle, out error);
            CheckError(ref error);
            return available;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === GigE Vision IP Configuration ===

    /// <summary>
    /// Gets the current IP configuration mode of a GigE Vision camera
    /// </summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public ArvGvIpConfigurationMode GvGetIpConfigurationMode()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            var mode = AravisNative.arv_camera_gv_get_ip_configuration_mode(lease.Handle, out error);
            CheckError(ref error);
            return (ArvGvIpConfigurationMode)mode;
        }
        finally
        {
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Sets the IP configuration mode of a GigE Vision camera.
    /// The change takes effect after a camera reboot.
    /// </summary>
    /// <param name="mode">Desired configuration mode (PersistentIp, Dhcp, Lla)</param>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public void GvSetIpConfigurationMode(ArvGvIpConfigurationMode mode)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_gv_set_ip_configuration_mode(lease.Handle, (int)mode, out error);
            CheckError(ref error);
        }
        finally
        {
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Gets the persistent (static) IP configuration stored in the camera.
    /// Returns dotted-decimal strings. Only meaningful when the camera is in PersistentIp mode.
    /// </summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public (string Ip, string Mask, string Gateway) GvGetPersistentIp()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        IntPtr ipObj = IntPtr.Zero, maskObj = IntPtr.Zero, gwObj = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_gv_get_persistent_ip(lease.Handle, out ipObj, out maskObj, out gwObj, out error);
            CheckError(ref error);

            string ip = GInetAddressToString(ipObj);
            string mask = GInetAddressMaskToDotted(maskObj);
            string gw = GInetAddressToString(gwObj);
            return (ip, mask, gw);
        }
        finally
        {
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
            if (ipObj != IntPtr.Zero) GLibNative.g_object_unref(ipObj);
            if (maskObj != IntPtr.Zero) GLibNative.g_object_unref(maskObj);
            if (gwObj != IntPtr.Zero) GLibNative.g_object_unref(gwObj);
        }
    }

    /// <summary>
    /// Sets the persistent (static) IP configuration on the camera.
    /// Call <see cref="GvSetIpConfigurationMode"/> with <see cref="ArvGvIpConfigurationMode.PersistentIp"/>
    /// beforehand, and reboot the camera for the change to take effect.
    /// </summary>
    /// <param name="ip">IP address string, e.g. "192.168.1.100"</param>
    /// <param name="mask">Subnet mask string, e.g. "255.255.255.0"</param>
    /// <param name="gateway">Default gateway string, e.g. "192.168.1.1" (null/empty for none)</param>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public void GvSetPersistentIp(string ip, string mask, string? gateway)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        IntPtr ipPtr = IntPtr.Zero, maskPtr = IntPtr.Zero, gwPtr = IntPtr.Zero;
        try
        {
            ipPtr = Marshal.StringToCoTaskMemUTF8(ip);
            maskPtr = Marshal.StringToCoTaskMemUTF8(mask);
            // Aravis rejects empty string as gateway — pass NULL to skip gateway configuration
            if (!string.IsNullOrEmpty(gateway))
                gwPtr = Marshal.StringToCoTaskMemUTF8(gateway);

            AravisNative.arv_camera_gv_set_persistent_ip_from_string(lease.Handle, ipPtr, maskPtr, gwPtr, out error);
            CheckError(ref error);
        }
        finally
        {
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
            if (ipPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(ipPtr);
            if (maskPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(maskPtr);
            if (gwPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(gwPtr);
        }
    }

    // Converts a GInetAddress* to dotted-decimal string, returns "" if null
    private static string GInetAddressToString(IntPtr addrObj)
    {
        if (addrObj == IntPtr.Zero) return "";
        var strPtr = GLibNative.g_inet_address_to_string(addrObj);
        if (strPtr == IntPtr.Zero) return "";
        try
        {
            return Marshal.PtrToStringUTF8(strPtr) ?? "";
        }
        finally
        {
            GLibNative.g_free(strPtr);
        }
    }

    // Converts a GInetAddressMask* to a dotted-decimal subnet mask string (e.g. "255.255.255.0").
    // Aravis always creates GInetAddressMask with prefix=32 and stores the actual mask bytes
    // inside the embedded GInetAddress — so we extract that address directly.
    private static string GInetAddressMaskToDotted(IntPtr maskObj)
    {
        if (maskObj == IntPtr.Zero) return "";
        // g_inet_address_mask_get_address returns a borrowed ref — do NOT unref
        var addrObj = GLibNative.g_inet_address_mask_get_address(maskObj);
        return GInetAddressToString(addrObj);
    }

    /// <summary>
    /// Gets the number of network interfaces available on a GigE Vision camera
    /// </summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public int GvGetNetworkInterfaceCount()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            var count = AravisNative.arv_camera_gv_get_n_network_interfaces(lease.Handle, out error);
            CheckError(ref error);
            return count;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }


    // === Convenience acquisition ===

    /// <summary>
    /// Convenience method: creates a stream, allocates one buffer, starts acquisition,
    /// waits for a single frame, stops acquisition, and returns the buffer.
    /// The caller owns the returned <see cref="Buffer"/> and must dispose it.
    /// </summary>
    /// <param name="timeoutUs">Timeout in microseconds (0 = infinite)</param>
    public Buffer AcquireSingleFrame(ulong timeoutUs = 2_000_000)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var bufferHandle = AravisNative.arv_camera_acquisition(lease.Handle, timeoutUs, out error);
            // A failed stop acquisition still returns the popped buffer alongside the error.
            if (error != IntPtr.Zero && bufferHandle != IntPtr.Zero)
                GLibNative.g_object_unref(bufferHandle);
            CheckError(ref error);
            if (bufferHandle == IntPtr.Zero)
                throw new AravisException("arv_camera_acquisition returned null buffer");
            return Buffer.FromOwnedHandle(bufferHandle);
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    // === Enumerate available values ===

    /// <summary>
    /// Returns the names of all pixel formats supported by the camera.
    /// </summary>
    public string[] GetAvailablePixelFormats()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_dup_available_pixel_formats_as_strings(lease.Handle, out uint count, out error);
            var values = MarshalStringArray(ptr, count);
            CheckError(ref error);
            return values;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Returns the display names of all pixel formats supported by the camera.
    /// </summary>
    public string[] GetAvailablePixelFormatDisplayNames()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var ptr = AravisNative.arv_camera_dup_available_pixel_formats_as_display_names(lease.Handle, out uint count, out error);
            var values = MarshalStringArray(ptr, count);
            CheckError(ref error);
            return values;
        }
        finally
        {
            if (error != IntPtr.Zero)
                GLibNative.g_error_free(error);
        }
    }

    /// <summary>
    /// Returns the valid string values for any enumeration feature (e.g. "TriggerSource").
    /// </summary>
    public string[] GetAvailableEnumerationValues(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var ptr = AravisNative.arv_camera_dup_available_enumerations_as_strings(lease.Handle, featurePtr, out uint count, out error);
            var values = MarshalStringArray(ptr, count);
            CheckError(ref error);
            return values;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
        }
    }

    // === Feature meta ===

    /// <summary>Gets the exposure time increment in microseconds (0 if continuous).</summary>
    public double GetExposureTimeIncrement()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_exposure_time_increment(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the gain increment (0 if continuous).</summary>
    public double GetGainIncrement()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_gain_increment(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets whether the frame rate is enabled.</summary>
    public bool GetFrameRateEnable()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_frame_rate_enable(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Enables or disables the frame rate control.</summary>
    public void SetFrameRateEnable(bool enable)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_frame_rate_enable(lease.Handle, enable, out error);
            CheckError(ref error);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Sets the exposure mode (Timed, TriggerWidth, TriggerControlled).</summary>
    public void SetExposureMode(ArvExposureMode mode)
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_set_exposure_mode(lease.Handle, mode, out error);
            CheckError(ref error);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Checks if a feature is implemented by the camera (vs. just available).</summary>
    public bool IsFeatureImplemented(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        IntPtr featurePtr = IntPtr.Zero;
        try
        {
            featurePtr = Marshal.StringToCoTaskMemUTF8(feature);
            var v = AravisNative.arv_camera_is_feature_implemented(lease.Handle, featurePtr, out error);
            CheckError(ref error);
            return v;
        }
        finally
        {
            if (featurePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(featurePtr);
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
        }
    }

    /// <summary>Checks if the ROI offset (OffsetX/OffsetY) is adjustable.</summary>
    public bool IsRegionOffsetAvailable()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_is_region_offset_available(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    // === ROI offset bounds ===

    /// <summary>Gets the allowed X offset (OffsetX) range.</summary>
    public (int Min, int Max) GetXOffsetBounds()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_x_offset_bounds(lease.Handle, out int min, out int max, out error);
            CheckError(ref error);
            return (min, max);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the X offset increment.</summary>
    public int GetXOffsetIncrement()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_x_offset_increment(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the allowed Y offset (OffsetY) range.</summary>
    public (int Min, int Max) GetYOffsetBounds()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_y_offset_bounds(lease.Handle, out int min, out int max, out error);
            CheckError(ref error);
            return (min, max);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the Y offset increment.</summary>
    public int GetYOffsetIncrement()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_y_offset_increment(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    // === Per-axis binning bounds ===

    /// <summary>Gets the allowed horizontal binning range.</summary>
    public (int Min, int Max) GetXBinningBounds()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_x_binning_bounds(lease.Handle, out int min, out int max, out error);
            CheckError(ref error);
            return (min, max);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the horizontal binning increment.</summary>
    public int GetXBinningIncrement()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_x_binning_increment(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the allowed vertical binning range.</summary>
    public (int Min, int Max) GetYBinningBounds()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_get_y_binning_bounds(lease.Handle, out int min, out int max, out error);
            CheckError(ref error);
            return (min, max);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the vertical binning increment.</summary>
    public int GetYBinningIncrement()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_get_y_binning_increment(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    // === GigE Vision — extended ===

    /// <summary>Gets the inter-packet delay in nanoseconds for GigE Vision cameras.</summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public long GvGetPacketDelay()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_gv_get_packet_delay(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Sets the inter-packet delay in nanoseconds for GigE Vision cameras.</summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public void GvSetPacketDelay(long delayNs)
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            AravisNative.arv_camera_gv_set_packet_delay(lease.Handle, delayNs, out error);
            CheckError(ref error);
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    /// <summary>Gets the number of stream channels for GigE Vision cameras.</summary>
    /// <exception cref="NotSupportedException">The camera is not a GigE Vision camera (see <see cref="IsGigEVisionDevice"/>).</exception>
    public int GvGetStreamChannelCount()
    {
        using var lease = AcquireHandle();
        CheckGigE();
        IntPtr error = IntPtr.Zero;
        try
        {
            var v = AravisNative.arv_camera_gv_get_n_stream_channels(lease.Handle, out error);
            CheckError(ref error);
            return v;
        }
        finally { if (error != IntPtr.Zero) GLibNative.g_error_free(error); }
    }

    // === Stream Creation ===

    /// <summary>
    /// Creates a stream for acquiring images from the camera
    /// </summary>
    public Stream CreateStream()
    {
        using var lease = AcquireHandle();
        IntPtr error = IntPtr.Zero;
        try
        {
            var streamHandle = AravisNative.arv_camera_create_stream(lease.Handle, IntPtr.Zero, IntPtr.Zero, out error);
            CheckError(ref error);
            
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
    /// Gets the underlying device for low-level feature access.
    /// </summary>
    /// <remarks>
    /// The <see cref="Device"/>, and its <see cref="Device.NodeMap"/>, belong to this camera:
    /// they hold no native reference of their own, share this camera's lock, and throw
    /// <see cref="ObjectDisposedException"/> once the camera is disposed. So a Device that is
    /// never disposed does not keep the camera's connection open after
    /// <see cref="Dispose"/>. Disposing the Device only closes that wrapper.
    /// </remarks>
    public Device GetDevice()
    {
        using var lease = AcquireHandle();
        // (transfer none): the ArvDevice lives exactly as long as the ArvCamera, so the
        // Device wrapper pins the camera's handle around each of its own native calls.
        var deviceHandle = AravisNative.arv_camera_get_device(lease.Handle);
        if (deviceHandle == IntPtr.Zero)
        {
            throw new AravisException("Failed to get device");
        }
        return new Device(_safeHandle, deviceHandle);
    }

    // Serialised: the native camera, its device and its GenICam object are not thread-safe.
    private GObjectLease AcquireHandle() => _safeHandle.Acquire(nameof(Camera), serialized: true);

    // The one GigE guard of Camera: called under the member's lease and before any native call
    // of a Gv* member. Without it Aravis fails its g_return_val_if_fail on a non-GigE camera:
    // GLib logs a CRITICAL and the member returns 0 or does nothing. Same message pattern as
    // Stream.CheckGigE.
    private void CheckGigE([CallerMemberName] string member = "")
    {
        if (!IsGigEVisionDevice())
            throw new NotSupportedException(
                $"{member} requires a GigE Vision camera; check {nameof(Camera)}.{nameof(IsGigEVisionDevice)}() first.");
    }

    private static void CheckError(ref IntPtr error)
    {
        if (error != IntPtr.Zero)
        {
            // Frees the GError and zeroes error: the callers' finally blocks then skip it.
            throw AravisException.TakeGError(ref error);
        }
    }


    /// <summary>
    /// Copies a (transfer container) array of constant strings and frees the array with g_free.
    /// </summary>
    private static string[] MarshalStringArray(IntPtr arrayPtr, uint count)
    {
        if (arrayPtr == IntPtr.Zero)
            return Array.Empty<string>();
        try
        {
            var result = new string[count];
            for (uint i = 0; i < count; i++)
            {
                var strPtr = Marshal.ReadIntPtr(arrayPtr, (int)(i * IntPtr.Size));
                result[i] = Marshal.PtrToStringUTF8(strPtr) ?? string.Empty;
            }
            return result;
        }
        finally
        {
            GLibNative.g_free(arrayPtr);
        }
    }

    /// <summary>
    /// Copies a (transfer none) string owned by the native camera. Call it under the
    /// member's lease: the string is freed with the camera.
    /// </summary>
    private static string MarshalString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    /// <summary>
    /// Closes the camera. Safe to call more than once and from any thread: from the moment
    /// it is called every member of the camera, of its <see cref="Device"/> and of its node
    /// map throws <see cref="ObjectDisposedException"/>, calls waiting for the camera's lock
    /// give up with the same exception, and the native camera is released once the calls in
    /// progress have returned (on the thread that ends the last one).
    /// </summary>
    /// <remarks>
    /// A <see cref="Stream"/> created from this camera holds its own native reference on the
    /// device and keeps the connection open: dispose it too before opening the same camera
    /// again.
    /// </remarks>
    public void Dispose()
    {
        _safeHandle.BeginClose();
        _safeHandle.Dispose();
        GC.SuppressFinalize(this);
    }
}
