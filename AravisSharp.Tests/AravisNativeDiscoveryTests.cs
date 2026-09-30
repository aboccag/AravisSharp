using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Tests for manually declared Aravis discovery and interface bindings.
/// </summary>
public class AravisNativeDiscoveryTests
{
    [NativeFact]
    public void GetDevicePhysicalId_WithValidIndex_ShouldReturnNonNull()
    {
        uint index = AravisNativeTests.FakeDeviceIndex();

        IntPtr physicalIdPtr = AravisNative.arv_get_device_physical_id(index);
        string? physicalId = Marshal.PtrToStringUTF8(physicalIdPtr);

        Assert.NotEqual(IntPtr.Zero, physicalIdPtr);
        Assert.NotNull(physicalId);
        Assert.NotEmpty(physicalId);
    }

    [NativeFact]
    public void GetInterfaceId_WithValidIndex_ShouldReturnNonNull()
    {
        AravisNative.arv_update_device_list();
        uint interfaceCount = AravisNative.arv_get_n_interfaces();

        // Aravis always registers its interfaces (at least GigE Vision and Fake).
        Assert.True(interfaceCount > 0);

        IntPtr interfaceIdPtr = AravisNative.arv_get_interface_id(0);
        string? interfaceId = Marshal.PtrToStringUTF8(interfaceIdPtr);

        Assert.NotEqual(IntPtr.Zero, interfaceIdPtr);
        Assert.NotNull(interfaceId);
        Assert.NotEmpty(interfaceId);
    }

    [NativeFact]
    public void GetDeviceManufacturerInfo_ForFakeCamera_ShouldReturnString()
    {
        uint index = AravisNativeTests.FakeDeviceIndex();

        // The fake interface always reports a manufacturer info string.
        IntPtr manufacturerInfoPtr = AravisNative.arv_get_device_manufacturer_info(index);
        string? manufacturerInfo = Marshal.PtrToStringUTF8(manufacturerInfoPtr);

        Assert.NotEqual(IntPtr.Zero, manufacturerInfoPtr);
        Assert.False(string.IsNullOrEmpty(manufacturerInfo));
    }

    [NativeFact]
    public void GetNativeVersion_ShouldReturnAravis08()
    {
        var version = AravisLibrary.GetNativeVersion();

        Assert.Equal(0, version.Major);
        Assert.Equal(8, version.Minor);
    }
}
