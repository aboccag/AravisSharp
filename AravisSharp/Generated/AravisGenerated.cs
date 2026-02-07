using System;
using System.Runtime.InteropServices;

namespace AravisSharp.Generated;

/// <summary>
/// Auto-generated Aravis bindings from GObject Introspection data
/// Generated from: Aravis-0.8
/// Total functions: 475
/// </summary>
public static class AravisGenerated
{
    // Use the same logical name resolved by AravisLibrary.RegisterResolver()
    private const string LibraryName = AravisSharp.Native.AravisNative.LibraryName;

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_acquisition_mode_from_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_acquisition_mode_to_string(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_auto_from_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_auto_to_string(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_chunk_parser_error_quark();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_error_quark();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_exposure_mode_from_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_exposure_mode_to_string(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_fake_interface_get_instance();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_invalidator_has_changed(IntPtr self);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_access_mode_from_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_access_mode_to_string(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_error_quark();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gentl_interface_get_instance();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gv_interface_get_instance();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_network_error_quark();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_uv_interface_get_instance();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_xml_schema_error_quark();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_debug_enable(IntPtr category_selection);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_disable_interface(IntPtr interface_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_implementation_add_document_type(IntPtr qualified_name, IntPtr document_type);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_implementation_cleanup();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_implementation_create_document(IntPtr namespace_uri, IntPtr qualified_name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_enable_interface(IntPtr interface_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_address(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_manufacturer_info(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_model(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_physical_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_protocol(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_serial_nbr(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_device_vendor(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_interface(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_interface_by_id(IntPtr interface_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_interface_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_get_interface_protocol(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_n_devices();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_get_n_interfaces();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_make_thread_high_priority(int nice_level);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_make_thread_realtime(int priority);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_open_device(IntPtr device_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_set_fake_camera_genicam_filename(IntPtr filename);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_set_gv_port_range(ushort port_min, ushort port_max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_set_gv_port_range_from_string(IntPtr range);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_set_interface_flags(IntPtr interface_id, int flags);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_shutdown();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_update_device_list();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_find_component(uint component_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte arv_buffer_get_chunk_data(ulong chunk_id, out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte arv_buffer_get_data(out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_buffer_get_frame_id();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte arv_buffer_get_gendc_data(out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte arv_buffer_get_gendc_descriptor(out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte arv_buffer_get_image_data(out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_height();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_get_image_padding(out int x_padding, out int y_padding);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_image_pixel_format();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_get_image_region(out int x, out int y, out int width, out int height);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_width();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_x();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_image_y();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_buffer_get_n_parts();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_buffer_get_part_component_id(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte arv_buffer_get_part_data(uint part_id, out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_part_data_type(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_part_height(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_get_part_padding(uint part_id, out int x_padding, out int y_padding);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_part_pixel_format(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_get_part_region(uint part_id, out int x, out int y, out int width, out int height);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_part_width(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_part_x(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_buffer_get_part_y(uint part_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_payload_type();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_status();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_buffer_get_system_timestamp();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_buffer_get_timestamp();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_buffer_get_user_data();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_buffer_has_chunks();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_buffer_has_gendc();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_set_frame_id(ulong frame_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_set_system_timestamp(ulong timestamp_ns);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_buffer_set_timestamp(ulong timestamp_ns);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_abort_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_acquisition(ulong timeout);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_are_chunks_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_clear_triggers();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_create_chunk_parser();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_create_stream(IntPtr callback, IntPtr user_data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_create_stream_full(IntPtr callback, IntPtr user_data, IntPtr destroy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_black_levels(out uint n_selectors);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_components(out uint n_components);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_dup_available_enumerations(IntPtr feature, out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_enumerations_as_display_names(IntPtr feature, out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_enumerations_as_strings(IntPtr feature, out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_gains(out uint n_selectors);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_dup_available_pixel_formats(out uint n_pixel_formats);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_pixel_formats_as_display_names(out uint n_pixel_formats);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_pixel_formats_as_strings(out uint n_pixel_formats);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_trigger_sources(out uint n_sources);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_available_triggers(out uint n_triggers);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_dup_register(IntPtr feature, out ulong length);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_execute_command(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_acquisition_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_binning(out int dx, out int dy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_black_level();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_black_level_auto();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_black_level_bounds(out double min, out double max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_get_boolean(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_boolean_gi(IntPtr feature, out bool value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_get_chunk_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_get_chunk_state(IntPtr chunk);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_device();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_device_id();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_device_serial_number();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_exposure_time();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_exposure_time_auto();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_exposure_time_bounds(out double min, out double max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_exposure_time_representation();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_feature_representation(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_float(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_float_bounds(IntPtr feature, out double min, out double max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_float_increment(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_get_frame_count();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_frame_count_bounds(out long min, out long max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_frame_rate();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_frame_rate_bounds(out double min, out double max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_get_frame_rate_enable();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_camera_get_gain();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_gain_auto();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_gain_bounds(out double min, out double max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_gain_representation();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_height_bounds(out int min, out int max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_height_increment();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_get_integer(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_integer_bounds(IntPtr feature, out long min, out long max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_get_integer_increment(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_model_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_camera_get_payload();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_pixel_format();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_pixel_format_as_string();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_region(out int x, out int y, out int width, out int height);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_sensor_size(out int width, out int height);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_string(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_trigger_source();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_get_vendor_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_width_bounds(out int min, out int max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_width_increment();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_x_binning_bounds(out int min, out int max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_x_binning_increment();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_x_offset_bounds(out int min, out int max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_x_offset_increment();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_y_binning_bounds(out int min, out int max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_y_binning_increment();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_get_y_offset_bounds(out int min, out int max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_get_y_offset_increment();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_camera_gv_auto_packet_size();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_current_stream_channel();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_camera_gv_get_ip_configuration_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_gv_get_multipart();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_n_network_interfaces();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_camera_gv_get_n_stream_channels();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_camera_gv_get_packet_delay();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_camera_gv_get_packet_size();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_get_persistent_ip(out IntPtr ip, out IntPtr mask, out IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_gv_is_multipart_supported();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_select_stream_channel(int channel_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_ip_configuration_mode(IntPtr mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_multipart(bool enable);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_packet_delay(long delay_ns);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_packet_size(int packet_size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_packet_size_adjustment(IntPtr adjustment);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_persistent_ip(IntPtr ip, IntPtr mask, IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_persistent_ip_from_string(IntPtr ip, IntPtr mask, IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_gv_set_stream_options(IntPtr options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_binning_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_black_level_auto_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_black_level_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_component_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_enumeration_entry_available(IntPtr feature, IntPtr entry);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_exposure_auto_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_exposure_time_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_feature_available(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_feature_implemented(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_frame_rate_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gain_auto_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gain_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gentl_device();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_gv_device();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_region_offset_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_software_trigger_supported();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_is_uv_device();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_select_and_enable_component(IntPtr component, bool disable_others);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_select_black_level(IntPtr selector);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_select_component(IntPtr component, IntPtr flags, out uint component_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_select_gain(IntPtr selector);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_access_check_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_acquisition_mode(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_binning(int dx, int dy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_black_level(double blacklevel);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_black_level_auto(IntPtr auto_mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_boolean(IntPtr feature, bool value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_chunk_mode(bool is_active);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_chunk_state(IntPtr chunk, bool is_enabled);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_chunks(IntPtr chunk_list);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_exposure_mode(IntPtr mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_exposure_time(double exposure_time_us);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_exposure_time_auto(IntPtr auto_mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_float(IntPtr feature, double value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_frame_count(long frame_count);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_frame_rate(double frame_rate);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_frame_rate_enable(bool enable);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_gain(double gain);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_gain_auto(IntPtr auto_mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_integer(IntPtr feature, long value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_pixel_format(IntPtr format);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_pixel_format_from_string(IntPtr format);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_range_check_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_region(int x, int y, int width, int height);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_register(IntPtr feature, ulong length, IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_register_cache_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_string(IntPtr feature, IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_trigger(IntPtr source);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_set_trigger_source(IntPtr source);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_software_trigger();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_start_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_stop_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_camera_uv_get_bandwidth();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_uv_get_bandwidth_bounds(out uint min, out uint max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_camera_uv_is_bandwidth_control_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_uv_set_bandwidth(uint bandwidth);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_uv_set_maximum_transfer_size(ulong size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_camera_uv_set_usb_mode(IntPtr usb_mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_chunk_parser_get_boolean_value(IntPtr buffer, IntPtr chunk);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_chunk_parser_get_float_value(IntPtr buffer, IntPtr chunk);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_chunk_parser_get_integer_value(IntPtr buffer, IntPtr chunk);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_chunk_parser_get_string_value(IntPtr buffer, IntPtr chunk);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_create_chunk_parser();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_create_stream(IntPtr callback, IntPtr user_data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_create_stream_full(IntPtr callback, IntPtr user_data, IntPtr destroy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_device_dup_available_enumeration_feature_values(IntPtr feature, out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_dup_available_enumeration_feature_values_as_display_names(IntPtr feature, out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_dup_available_enumeration_feature_values_as_strings(IntPtr feature, out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_dup_register_feature_value(IntPtr feature, out ulong length);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_execute_command(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_get_boolean_feature_value(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_get_boolean_feature_value_gi(IntPtr feature, out bool value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_feature(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_feature_access_mode(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_feature_representation(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_get_feature_value(IntPtr feature, out IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_get_float_feature_bounds(IntPtr feature, out double min, out double max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_device_get_float_feature_increment(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_device_get_float_feature_value(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_genicam();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_genicam_xml(out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_get_integer_feature_bounds(IntPtr feature, out long min, out long max);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_device_get_integer_feature_increment(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_device_get_integer_feature_value(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_device_get_string_feature_value(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_is_enumeration_entry_available(IntPtr feature, IntPtr entry);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_is_feature_available(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_is_feature_implemented(IntPtr feature);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_read_memory(ulong address, uint size, IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_read_register(ulong address, out uint value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_access_check_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_boolean_feature_value(IntPtr feature, bool value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_feature_value(IntPtr feature, IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_set_features_from_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_float_feature_value(IntPtr feature, double value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_integer_feature_value(IntPtr feature, long value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_range_check_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_register_cache_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_register_feature_value(IntPtr feature, ulong length, IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_device_set_string_feature_value(IntPtr feature, IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_start_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_stop_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_write_memory(ulong address, uint size, IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_device_write_register(ulong address, uint value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_character_data_get_data();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_character_data_set_data(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_document_append_from_memory(IntPtr node, IntPtr buffer, int size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_document_create_element(IntPtr tag_name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_document_create_text_node(IntPtr data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_document_get_document_element();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_document_get_href_data(IntPtr href, UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_document_get_url();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_document_set_path(IntPtr path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_document_set_url(IntPtr url);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_element_get_attribute(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_element_get_tag_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_element_set_attribute(IntPtr name, IntPtr attribute_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_named_node_map_get_item(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_dom_named_node_map_get_length();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_named_node_map_get_named_item(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_named_node_map_remove_named_item(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_named_node_map_set_named_item(IntPtr item);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_append_child(IntPtr new_child);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_node_changed();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_child_nodes();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_first_child();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_last_child();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_next_sibling();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_node_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_node_type();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_node_value();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_owner_document();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_parent_node();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_get_previous_sibling();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_dom_node_has_child_nodes();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_insert_before(IntPtr new_child, IntPtr ref_child);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_remove_child(IntPtr old_child);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_replace_child(IntPtr new_child, IntPtr old_child);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_dom_node_set_node_value(IntPtr new_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_dom_node_list_get_item(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_dom_node_list_get_length();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_evaluator_evaluate_as_double();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_evaluator_evaluate_as_int64();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_evaluator_get_constant(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_evaluator_get_expression();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_evaluator_get_sub_expression(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_evaluator_set_constant(IntPtr name, IntPtr constant);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_evaluator_set_double_variable(IntPtr name, double v_double);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_evaluator_set_expression(IntPtr expression);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_evaluator_set_int64_variable(IntPtr name, long v_int64);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_evaluator_set_sub_expression(IntPtr name, IntPtr expression);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_check_and_acknowledge_software_trigger();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_fake_camera_fill_buffer(IntPtr buffer, out uint packet_size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_fake_camera_get_acquisition_status();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_fake_camera_get_control_channel_privilege();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_fake_camera_get_genicam_xml(out UIntPtr size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_fake_camera_get_genicam_xml_url();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_fake_camera_get_heartbeat_timeout();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UIntPtr arv_fake_camera_get_payload();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_fake_camera_get_sleep_time_for_next_frame(out ulong next_timestamp_us);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_fake_camera_get_stream_address();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_is_in_free_running_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_is_in_software_trigger_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_read_memory(uint address, uint size, IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_read_register(uint address, out uint value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_fake_camera_set_control_channel_privilege(uint privilege);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_fake_camera_set_fill_pattern(IntPtr fill_pattern_callback, IntPtr fill_pattern_data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_fake_camera_set_inet_address(IntPtr address);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_fake_camera_set_trigger_frequency(double frequency);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_fake_camera_wait_for_next_frame();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_write_memory(uint address, uint size, IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_fake_camera_write_register(uint address, uint value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_fake_device_get_fake_camera();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_access_check_policy();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_buffer();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_device();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_node(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_range_check_policy();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_get_register_cache_policy();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_register_feature_node(IntPtr node);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_set_access_check_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_set_buffer(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_set_range_check_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_set_register_cache_policy(IntPtr policy);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_boolean_get_value();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_boolean_get_value_gi(out bool value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_boolean_set_value(bool v_boolean);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_category_get_features();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_command_execute();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_enum_entry_get_value();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_enumeration_dup_available_display_names(out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_enumeration_dup_available_int_values(out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_enumeration_dup_available_string_values(out uint n_values);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_enumeration_get_entries();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_enumeration_get_int_value();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_enumeration_get_string_value();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_enumeration_set_int_value(long value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_enumeration_set_string_value(IntPtr value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_actual_access_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_description();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_display_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_imposed_access_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_name_space();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_tooltip();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_value_as_string();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_feature_node_get_visibility();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_available();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_implemented();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_feature_node_is_locked();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_feature_node_set_value_from_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_index_node_get_index(long default_offset);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_node_get_genicam();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_port_read(IntPtr buffer, ulong address, ulong length);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_port_write(IntPtr buffer, ulong address, ulong length);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_access_mode(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_cachable(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_display_notation(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_property_node_get_display_precision(long default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_gc_property_node_get_double();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_property_node_get_endianness(uint default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_property_node_get_int64();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_linked_node();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_property_node_get_lsb(uint default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_property_node_get_msb(uint default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_node_type();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_representation(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_sign(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_streamable(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_string();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_property_node_get_visibility(IntPtr default_value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_property_node_set_double(double v_double);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_property_node_set_int64(long v_int64);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gc_property_node_set_string(IntPtr @string);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gc_register_description_node_check_schema_version(uint required_major, uint required_minor, uint required_subminor);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int arv_gc_register_description_node_compare_schema_version(uint major, uint minor, uint subminor);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_register_description_node_get_major_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_register_description_node_get_minor_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_register_description_node_get_model_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_register_description_node_get_schema_major_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_register_description_node_get_schema_minor_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_register_description_node_get_schema_subminor_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gc_register_description_node_get_subminor_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gc_register_description_node_get_vendor_name();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern long arv_gc_value_indexed_node_get_index();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gv_device_auto_packet_size();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_get_current_ip(out IntPtr ip, out IntPtr mask, out IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gv_device_get_device_address();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gv_device_get_interface_address();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gv_device_get_ip_configuration_mode();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_gv_device_get_packet_size();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_get_persistent_ip(out IntPtr ip, out IntPtr mask, out IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gv_device_get_stream_options();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_gv_device_get_timestamp_tick_frequency();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_is_controller();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_leave_control();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_set_ip_configuration_mode(IntPtr mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gv_device_set_packet_size(int packet_size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gv_device_set_packet_size_adjustment(IntPtr adjustment);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_set_persistent_ip(IntPtr ip, IntPtr mask, IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_set_persistent_ip_from_string(IntPtr ip, IntPtr mask, IntPtr gateway);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gv_device_set_stream_options(IntPtr options);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_device_take_control();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_gv_fake_camera_get_fake_camera();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_gv_fake_camera_is_running();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort arv_gv_stream_get_port();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_gv_stream_get_statistics(out ulong n_resent_packets, out ulong n_missing_packets);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_address(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_manufacturer_info(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_model(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_physical_id(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_protocol(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_serial_nbr(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_get_device_vendor(uint index);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_interface_get_n_devices();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_interface_open_device(IntPtr device_id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_interface_update_device_list();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_stream_create_buffers(uint n_buffers, IntPtr user_data, IntPtr user_data_destroy_func);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_stream_delete_buffers();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_stream_get_emit_signals();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_stream_get_info_double(uint id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double arv_stream_get_info_double_by_name(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_get_info_name(uint id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_get_info_type(uint id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_stream_get_info_uint64(uint id);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong arv_stream_get_info_uint64_by_name(IntPtr name);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint arv_stream_get_n_infos();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_get_n_owned_buffers(out int n_input_buffers, out int n_output_buffers, out int n_buffer_filling);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_get_statistics(out ulong n_completed_buffers, out ulong n_failures, out ulong n_underruns);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_pop_buffer();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_push_buffer(IntPtr buffer);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_stream_set_emit_signals(bool emit_signals);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_stream_start_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_stream_stop_acquisition();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_timeout_pop_buffer(ulong timeout);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr arv_stream_try_pop_buffer();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_uv_device_set_maximum_transfer_size(ulong size);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void arv_uv_device_set_usb_mode(IntPtr usb_mode);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool arv_xml_schema_validate(IntPtr xml, UIntPtr size, int line, int column);

}
