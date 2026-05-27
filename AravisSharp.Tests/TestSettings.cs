// Aravis is a native library with non-thread-safe global state (device list, interfaces).
// All tests must run sequentially to avoid concurrent arv_update_device_list() calls
// corrupting the enumeration and garbling device ID strings.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
