using System.Runtime.InteropServices;
using AravisSharp;

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

    // Version
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_major_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_minor_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_micro_version();

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

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_physical_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_manufacturer_info(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_n_interfaces();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_interface_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_enable_interface(IntPtr interfaceId);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_disable_interface(IntPtr interfaceId);

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
    public static extern void arv_camera_clear_triggers(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_software_trigger_supported(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_software_trigger(IntPtr camera, out IntPtr error);

    // Payload
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_camera_get_payload(IntPtr camera, out IntPtr error);

    // Sensor size
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_sensor_size(IntPtr camera, out int width, out int height, out IntPtr error);

    // Execute command
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_execute_command(IntPtr camera, IntPtr feature, out IntPtr error);

    // Acquisition mode
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_acquisition_mode(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_acquisition_mode(IntPtr camera, int mode, out IntPtr error);

    // Frame count  
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_get_frame_count(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_frame_count(IntPtr camera, long frameCount, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_frame_count_bounds(IntPtr camera, out long min, out long max, out IntPtr error);

    // Exposure time auto
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_exposure_time_auto(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_exposure_time_auto(IntPtr camera, int autoMode, out IntPtr error);

    // Gain auto
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_gain_auto(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_gain_auto(IntPtr camera, int autoMode, out IntPtr error);

    // Generic feature access (for advanced use)
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_string(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_string(IntPtr camera, IntPtr feature, IntPtr value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_get_integer(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_integer(IntPtr camera, IntPtr feature, long value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_float(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_float(IntPtr camera, IntPtr feature, double value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_get_boolean(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_boolean(IntPtr camera, IntPtr feature, bool value, out IntPtr error);

    // Feature bounds
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_integer_bounds(IntPtr camera, IntPtr feature, out long min, out long max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_float_bounds(IntPtr camera, IntPtr feature, out double min, out double max, out IntPtr error);

    // Feature increment  
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_get_integer_increment(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_float_increment(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_width_increment(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_height_increment(IntPtr camera, out IntPtr error);

    // Feature availability checks
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_feature_available(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_binning_available(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_exposure_time_available(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_exposure_auto_available(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gain_available(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gain_auto_available(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_frame_rate_available(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gv_device(IntPtr camera);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_uv_device(IntPtr camera);

    // GigE Vision specifics
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_auto_packet_size(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_packet_size(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_packet_size(IntPtr camera, int size, out IntPtr error);

    // GigE Vision IP configuration
    // ip/mask/gateway out-params are GInetAddress* / GInetAddressMask* — use GLibNative helpers to convert
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_ip_configuration_mode(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_ip_configuration_mode(IntPtr camera, int mode, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_get_persistent_ip(IntPtr camera,
        out IntPtr ipObj, out IntPtr maskObj, out IntPtr gatewayObj, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_persistent_ip_from_string(IntPtr camera,
        IntPtr ip, IntPtr mask, IntPtr gateway, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_n_network_interfaces(IntPtr camera);

    // USB Vision specifics
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_uv_get_bandwidth(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_uv_set_bandwidth(IntPtr camera, int bandwidth, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_uv_get_bandwidth_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_uv_is_bandwidth_control_available(IntPtr camera, out IntPtr error);

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

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort arv_gv_stream_get_port(IntPtr gvStream);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gv_stream_get_statistics(
        IntPtr gvStream,
        out ulong nResentPackets,
        out ulong nMissingPackets);

    // Buffer operations
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_new(UIntPtr size, IntPtr priv);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_new_allocate(UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ArvBufferStatus arv_buffer_get_status(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_data(IntPtr buffer, out UIntPtr size);

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

    /// <summary>Returns the GType for ArvGcFeatureNode. Used for safe type checking before calling feature-node-specific functions.</summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_type();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_value_as_string(IntPtr node, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_gc_feature_node_get_actual_access_mode(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_gc_feature_node_get_visibility(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_feature_node_set_value_from_string(IntPtr node, IntPtr value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_available(IntPtr node, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_implemented(IntPtr node, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_locked(IntPtr node, out IntPtr error);

    // === Buffer — additional accessors ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_x(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_y(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_get_image_padding(IntPtr buffer, out int xPadding, out int yPadding);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ArvBufferPayloadType arv_buffer_get_payload_type(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_buffer_get_system_timestamp(IntPtr buffer);

    // === Camera — convenience single-frame acquisition ===

    /// <summary>Acquire a single buffer with optional timeout (µs). Caller owns the returned ArvBuffer.</summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_acquisition(IntPtr camera, ulong timeoutUs, out IntPtr error);

    // === Camera — enumerate available values ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_pixel_formats_as_strings(IntPtr camera, out uint nFormats, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_pixel_formats_as_display_names(IntPtr camera, out uint nFormats, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_enumerations_as_strings(IntPtr camera, IntPtr feature, out uint nValues, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_enumerations_as_display_names(IntPtr camera, IntPtr feature, out uint nValues, out IntPtr error);

    // === Camera — feature meta (increment / representation) ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_exposure_time_increment(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_gain_increment(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_get_frame_rate_enable(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_frame_rate_enable(IntPtr camera, bool enable, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_exposure_mode(IntPtr camera, ArvExposureMode mode, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_feature_implemented(IntPtr camera, IntPtr feature, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_region_offset_available(IntPtr camera, out IntPtr error);

    // === Camera — X/Y offset bounds (ROI positioning) ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_x_offset_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_x_offset_increment(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_y_offset_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_y_offset_increment(IntPtr camera, out IntPtr error);

    // === Camera — per-axis binning bounds ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_x_binning_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_x_binning_increment(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_y_binning_bounds(IntPtr camera, out int min, out int max, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_y_binning_increment(IntPtr camera, out IntPtr error);

    // === Camera — GigE Vision extended ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_gv_get_packet_delay(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_packet_delay(IntPtr camera, long delayNs, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_n_stream_channels(IntPtr camera, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_select_stream_channel(IntPtr camera, int channelIndex, out IntPtr error);

    // === Stream — additional ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_try_pop_buffer(IntPtr stream);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_get_n_buffers(IntPtr stream, out int nInputBuffers, out int nOutputBuffers);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_stream_get_emit_signals(IntPtr stream);

    // === Device — additional ===

    /// <summary>Returns the raw GenICam XML descriptor. The returned string is owned by the device; do not free it.</summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_genicam_xml(IntPtr device, out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_is_feature_available(IntPtr device, IntPtr feature, out IntPtr error);

    /// <summary>Apply multiple features from a key=value string, e.g. "Width=640,Height=480".</summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_set_features_from_string(IntPtr device, IntPtr settings, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_read_register(IntPtr device, ulong address, out uint value, out IntPtr error);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_write_register(IntPtr device, ulong address, uint value, out IntPtr error);

    // === System ===

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_shutdown();

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

/// <summary>
/// GigE Vision IP configuration mode flags (can be combined with bitwise OR)
/// </summary>
[Flags]
public enum ArvGvIpConfigurationMode : int
{
    None = 0,
    /// <summary>Persistent (static) IP address</summary>
    PersistentIp = 1,
    /// <summary>DHCP address assignment</summary>
    Dhcp = 2,
    /// <summary>Link-Local Address (169.254.x.x auto-configuration)</summary>
    Lla = 4
}
