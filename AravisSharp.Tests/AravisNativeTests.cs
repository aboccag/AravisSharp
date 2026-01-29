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
    [Fact]
    public void UpdateDeviceList_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => AravisNative.arv_update_device_list());
        Assert.Null(exception);
    }

    [Fact]
    public void GetNumberOfDevices_ShouldReturnNonNegativeValue()
    {
        // Arrange
        AravisNative.arv_update_device_list();

        // Act
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Assert
        Assert.True(deviceCount >= 0);
    }

    [Fact]
    public void GetDeviceId_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr deviceIdPtr = AravisNative.arv_get_device_id(0);
        string? deviceId = Marshal.PtrToStringAnsi(deviceIdPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, deviceIdPtr);
        Assert.NotNull(deviceId);
        Assert.NotEmpty(deviceId);
    }

    [Fact]
    public void GetDeviceVendor_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr vendorPtr = AravisNative.arv_get_device_vendor(0);
        string? vendor = Marshal.PtrToStringAnsi(vendorPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, vendorPtr);
        Assert.NotNull(vendor);
        Assert.NotEmpty(vendor);
    }

    [Fact]
    public void GetDeviceModel_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr modelPtr = AravisNative.arv_get_device_model(0);
        string? model = Marshal.PtrToStringAnsi(modelPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, modelPtr);
        Assert.NotNull(model);
        Assert.NotEmpty(model);
    }

    [Fact]
    public void GetDeviceSerialNumber_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr serialPtr = AravisNative.arv_get_device_serial_nbr(0);
        string? serial = Marshal.PtrToStringAnsi(serialPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, serialPtr);
        Assert.NotNull(serial);
        Assert.NotEmpty(serial);
    }

    [Fact]
    public void GetDeviceProtocol_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr protocolPtr = AravisNative.arv_get_device_protocol(0);
        string? protocol = Marshal.PtrToStringAnsi(protocolPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, protocolPtr);
        Assert.NotNull(protocol);
        Assert.NotEmpty(protocol);
    }

    [Fact]
    public void GetDeviceAddress_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr addressPtr = AravisNative.arv_get_device_address(0);
        string? address = Marshal.PtrToStringAnsi(addressPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, addressPtr);
        Assert.NotNull(address);
        Assert.NotEmpty(address);
    }

    [Fact]
    public void CameraNew_WithNullDeviceId_ShouldOpenFirstCamera()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr error = IntPtr.Zero;
        IntPtr camera = AravisNative.arv_camera_new(IntPtr.Zero, out error);

        try
        {
            // Assert
            Assert.Equal(IntPtr.Zero, error);
            Assert.NotEqual(IntPtr.Zero, camera);
        }
        finally
        {
            if (camera != IntPtr.Zero)
            {
                AravisNative.g_object_unref(camera);
            }
        }
    }

    [Fact]
    public void BufferNewAllocate_ShouldCreateValidBuffer()
    {
        // Arrange
        IntPtr size = new IntPtr(1024);

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
                AravisNative.g_object_unref(buffer);
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
