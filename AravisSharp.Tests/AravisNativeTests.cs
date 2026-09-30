using System;
using System.Runtime.InteropServices;
using AravisSharp.Native;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Unit tests for manually created AravisNative bindings
/// </summary>
public class AravisNativeTests
{
    /// <summary>
    /// Index of the fake camera in the device list. The fake interface is always enabled
    /// (NativeLibraryInitializer), so it must be listed; using its index keeps the tests
    /// independent of whatever real devices happen to enumerate first. Reads discovery
    /// data only, never opens a device.
    /// </summary>
    internal static uint FakeDeviceIndex()
    {
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();
        Assert.True(deviceCount > 0, "No devices listed although the fake interface is enabled.");

        for (uint i = 0; i < deviceCount; i++)
        {
            if (Marshal.PtrToStringUTF8(AravisNative.arv_get_device_protocol(i)) == "Fake")
                return i;
        }

        Assert.Fail("The fake camera is not in the device list.");
        return 0;
    }

    [NativeFact]
    public void UpdateDeviceList_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => AravisNative.arv_update_device_list());
        Assert.Null(exception);
    }

    [NativeFact]
    public void GetNumberOfDevices_WithFakeInterfaceEnabled_ShouldBePositive()
    {
        // Arrange
        AravisNative.arv_update_device_list();

        // Act
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Assert
        Assert.True(deviceCount > 0, "The fake interface is enabled by NativeLibraryInitializer, so the fake camera must be listed.");
    }

    [NativeFact]
    public void GetDeviceId_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        uint index = FakeDeviceIndex();

        // Act
        IntPtr deviceIdPtr = AravisNative.arv_get_device_id(index);
        string? deviceId = Marshal.PtrToStringUTF8(deviceIdPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, deviceIdPtr);
        Assert.NotNull(deviceId);
        Assert.NotEmpty(deviceId);
    }

    [NativeFact]
    public void GetDeviceVendor_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        uint index = FakeDeviceIndex();

        // Act
        IntPtr vendorPtr = AravisNative.arv_get_device_vendor(index);
        string? vendor = Marshal.PtrToStringUTF8(vendorPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, vendorPtr);
        Assert.NotNull(vendor);
        Assert.NotEmpty(vendor);
    }

    [NativeFact]
    public void GetDeviceModel_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        uint index = FakeDeviceIndex();

        // Act
        IntPtr modelPtr = AravisNative.arv_get_device_model(index);
        string? model = Marshal.PtrToStringUTF8(modelPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, modelPtr);
        Assert.NotNull(model);
        Assert.NotEmpty(model);
    }

    [NativeFact]
    public void GetDeviceSerialNumber_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        uint index = FakeDeviceIndex();

        // Act
        IntPtr serialPtr = AravisNative.arv_get_device_serial_nbr(index);
        string? serial = Marshal.PtrToStringUTF8(serialPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, serialPtr);
        Assert.NotNull(serial);
        Assert.NotEmpty(serial);
    }

    [NativeFact]
    public void GetDeviceProtocol_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        uint index = FakeDeviceIndex();

        // Act
        IntPtr protocolPtr = AravisNative.arv_get_device_protocol(index);
        string? protocol = Marshal.PtrToStringUTF8(protocolPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, protocolPtr);
        Assert.NotNull(protocol);
        Assert.NotEmpty(protocol);
    }

    [NativeFact]
    public void GetDeviceAddress_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        uint index = FakeDeviceIndex();

        // Act
        IntPtr addressPtr = AravisNative.arv_get_device_address(index);
        string? address = Marshal.PtrToStringUTF8(addressPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, addressPtr);
        Assert.NotNull(address);
        Assert.NotEmpty(address);
    }

    [NativeFact]
    public void BufferNewAllocate_ShouldCreateValidBuffer()
    {
        // Arrange
        UIntPtr size = new UIntPtr(1024);

        // Act
        IntPtr buffer = AravisNative.arv_buffer_new_allocate(size);

        try
        {
            // Assert
            Assert.NotEqual(IntPtr.Zero, buffer);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                GLibNative.g_object_unref(buffer);
            }
        }
    }

    [Fact]
    public void PixelFormatConstants_ShouldHaveValidValues()
    {
        // Assert
        Assert.Equal(0x01080001u, ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8);
        Assert.Equal(0x01100003u, ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_10);
        Assert.Equal(0x01100005u, ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12);
        Assert.Equal(0x01100025u, ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_14);
        Assert.Equal(0x01100007u, ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16);
    }
}
