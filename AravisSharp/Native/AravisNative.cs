using System.Runtime.InteropServices;

namespace AravisSharp.Native;

/// <summary>
/// P/Invoke declarations for the Aravis library
/// Cross-platform: Windows (aravis-0.8-0.dll), Linux (libaravis-0.8.so.0), macOS (libaravis-0.8.dylib)
/// </summary>
public static class AravisNative
{
    // Logical library name — resolved at runtime by AravisLibrary.RegisterResolver()
    // The resolver maps this to the correct platform-specific file:
    //   Windows: libaravis-0.8-0.dll  |  Linux: libaravis-0.8.so.0  |  macOS: libaravis-0.8.dylib
    internal const string LibraryName = "aravis-0.8";

    // Camera discovery and enumeration
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_update_device_list();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_n_devices();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_model(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_serial_nbr(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_vendor(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_protocol(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_address(uint index);

    // Camera opening and closing
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_new(IntPtr deviceId, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_new_with_device(IntPtr device, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_open_device(IntPtr deviceId, out IntPtr error);

    // Camera info
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_vendor_name(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_model_name(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_device_serial_number(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_device_id(IntPtr camera, out IntPtr error);

    // Region of interest
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_region(IntPtr camera, out int x, out int y, out int width, out int height, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_region(IntPtr camera, int x, int y, int width, int height, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_width_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_height_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    // Binning
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_binning(IntPtr camera, out int dx, out int dy, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_binning(IntPtr camera, int dx, int dy, out IntPtr error);

    // Pixel format
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_camera_get_pixel_format(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_pixel_format(IntPtr camera, uint pixelFormat, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_pixel_format_as_string(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_pixel_format_from_string(IntPtr camera, IntPtr format, out IntPtr error);

    // Acquisition
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_start_acquisition(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_stop_acquisition(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_abort_acquisition(IntPtr camera, out IntPtr error);

    // Exposure time
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_exposure_time(IntPtr camera, double exposureTimeUs, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_exposure_time(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_exposure_time_bounds(IntPtr camera, out double min, out double max, out IntPtr error);

    // Gain
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_gain(IntPtr camera, double gain, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_gain(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_gain_bounds(IntPtr camera, out double min, out double max, out IntPtr error);

    // Frame rate
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_frame_rate(IntPtr camera, double frameRate, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_frame_rate(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_frame_rate_bounds(IntPtr camera, out double min, out double max, out IntPtr error);

    // Trigger
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_trigger(IntPtr camera, IntPtr source, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_trigger_source(IntPtr camera, IntPtr source, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_trigger_source(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_software_trigger(IntPtr camera, out IntPtr error);

    // Stream creation
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_create_stream(IntPtr camera, IntPtr callback, IntPtr userData, out IntPtr error);

    // Stream operations
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_push_buffer(IntPtr stream, IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_pop_buffer(IntPtr stream);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_timeout_pop_buffer(IntPtr stream, ulong timeout);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_get_statistics(IntPtr stream, out ulong nCompletedBuffers, out ulong nFailures, out ulong nUnderruns);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_set_emit_signals(IntPtr stream, bool emitSignals);

    // Buffer operations
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_new(IntPtr size, IntPtr priv);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_new_allocate(IntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ArvBufferStatus arv_buffer_get_status(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_data(IntPtr buffer, out IntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_get_image_region(IntPtr buffer, out int x, out int y, out int width, out int height);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_width(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_height(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_buffer_get_image_pixel_format(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_buffer_get_timestamp(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_buffer_get_frame_id(IntPtr buffer);

    // Device (low-level)
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_device(IntPtr camera);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_string_feature_value(IntPtr device, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_string_feature_value(IntPtr device, IntPtr feature, IntPtr value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_device_get_integer_feature_value(IntPtr device, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_integer_feature_value(IntPtr device, IntPtr feature, long value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_device_get_float_feature_value(IntPtr device, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_float_feature_value(IntPtr device, IntPtr feature, double value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_get_boolean_feature_value(IntPtr device, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_boolean_feature_value(IntPtr device, IntPtr feature, bool value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_execute_command(IntPtr device, IntPtr feature, out IntPtr error);

    // GenICam feature introspection
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_genicam(IntPtr device);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_get_integer_feature_bounds(IntPtr device, IntPtr feature, out long min, out long max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_device_get_integer_feature_increment(IntPtr device, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_get_float_feature_bounds(IntPtr device, IntPtr feature, out double min, out double max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_device_get_float_feature_increment(IntPtr device, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_dup_available_enumeration_feature_values_as_strings(IntPtr device, IntPtr feature, out uint n_values, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_dup_available_enumeration_feature_values_as_display_names(IntPtr device, IntPtr feature, out uint n_values, out IntPtr error);

    // GenICam node map functions
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_node(IntPtr genicam, IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_category_get_features(IntPtr category);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_name(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_display_name(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_description(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_tooltip(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_value_as_string(IntPtr node, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_actual_access_mode(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_visibility(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_available(IntPtr node, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_implemented(IntPtr node, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_locked(IntPtr node, out IntPtr error);

    // NOTE: g_object_ref/unref and g_error_free/g_clear_error live in GLib/GObject,
    // NOT in the Aravis library. Use GLibNative for those functions.
}

/// <summary>
/// Buffer status enumeration
/// </summary>
public enum ArvBufferStatus
{
    Success = 0,
    Cleared = 1,
    Timeout = 2,
    Missing_packets = 3,
    Wrong_packet_id = 4,
    Size_mismatch = 5,
    Filling = 6,
    Aborted = 7
}

/// <summary>
/// Pixel format constants (subset of common formats)
/// </summary>
public static class ArvPixelFormat
{
    public const uint ARV_PIXEL_FORMAT_MONO_8 = 0x01080001;
    public const uint ARV_PIXEL_FORMAT_MONO_10 = 0x01100003;
    public const uint ARV_PIXEL_FORMAT_MONO_12 = 0x01100005;
    public const uint ARV_PIXEL_FORMAT_MONO_14 = 0x01100025;
    public const uint ARV_PIXEL_FORMAT_MONO_16 = 0x01100007;
    
    public const uint ARV_PIXEL_FORMAT_BAYER_GR_8 = 0x01080008;
    public const uint ARV_PIXEL_FORMAT_BAYER_RG_8 = 0x01080009;
    public const uint ARV_PIXEL_FORMAT_BAYER_GB_8 = 0x0108000A;
    public const uint ARV_PIXEL_FORMAT_BAYER_BG_8 = 0x0108000B;
    
    public const uint ARV_PIXEL_FORMAT_RGB_8_PACKED = 0x02180014;
    public const uint ARV_PIXEL_FORMAT_BGR_8_PACKED = 0x02180015;
    public const uint ARV_PIXEL_FORMAT_RGBA_8_PACKED = 0x02200016;
    public const uint ARV_PIXEL_FORMAT_BGRA_8_PACKED = 0x02200017;
    
    public const uint ARV_PIXEL_FORMAT_YUV_422_PACKED = 0x0210001F;
    public const uint ARV_PIXEL_FORMAT_YUV_422_YUYV_PACKED = 0x02100032;
}
