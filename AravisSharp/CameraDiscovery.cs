using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp;

/// <summary>
/// Represents information about a discovered camera device
/// </summary>
public class CameraInfo
{
    public string DeviceId { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string Vendor { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"{Vendor} {Model} (S/N: {SerialNumber}) [{Protocol}] @ {Address}";
    }
}

/// <summary>
/// Provides methods for discovering and enumerating Aravis-compatible cameras
/// </summary>
public static class CameraDiscovery
{
    /// <summary>
    /// Updates the internal device list by scanning the network and USB buses
    /// </summary>
    public static void UpdateDeviceList()
    {
        AravisNative.arv_update_device_list();
    }

    /// <summary>
    /// Gets the number of currently available devices
    /// </summary>
    public static uint GetDeviceCount()
    {
        return AravisNative.arv_get_n_devices();
    }

    /// <summary>
    /// Discovers all available cameras
    /// </summary>
    /// <returns>List of discovered cameras</returns>
    public static List<CameraInfo> DiscoverCameras()
    {
        UpdateDeviceList();
        var count = GetDeviceCount();
        var cameras = new List<CameraInfo>();

        for (uint i = 0; i < count; i++)
        {
            var info = GetCameraInfo(i);
            if (info != null)
            {
                cameras.Add(info);
            }
        }

        return cameras;
    }

    /// <summary>
    /// Gets information about a specific device by index
    /// </summary>
    public static CameraInfo? GetCameraInfo(uint index)
    {
        var deviceIdPtr = AravisNative.arv_get_device_id(index);
        if (deviceIdPtr == IntPtr.Zero)
            return null;

        return new CameraInfo
        {
            DeviceId = MarshalString(deviceIdPtr),
            Model = MarshalString(AravisNative.arv_get_device_model(index)),
            SerialNumber = MarshalString(AravisNative.arv_get_device_serial_nbr(index)),
            Vendor = MarshalString(AravisNative.arv_get_device_vendor(index)),
            Protocol = MarshalString(AravisNative.arv_get_device_protocol(index)),
            Address = MarshalString(AravisNative.arv_get_device_address(index))
        };
    }

    private static string MarshalString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}
