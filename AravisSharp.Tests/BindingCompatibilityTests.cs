using System;
using System.Runtime.InteropServices;
using AravisSharp.Native;
using AravisSharp.Generated;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Comparison tests between manual and generated bindings to ensure compatibility
/// These tests verify that both binding sets can successfully enumerate and access devices
/// </summary>
public class BindingCompatibilityTests
{
    [Fact]
    public void DeviceEnumeration_BothBindings_ShouldReturnSameCount()
    {
        // Arrange & Act
        AravisNative.arv_update_device_list();
        uint nativeCount = AravisNative.arv_get_n_devices();

        AravisGenerated.arv_update_device_list();
        uint generatedCount = AravisGenerated.arv_get_n_devices();

        // Assert
        Assert.Equal(nativeCount, generatedCount);
    }

    [Fact]
    public void DeviceId_BothBindings_ShouldReturnValidPointers()
    {
        // Arrange
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act - Both bindings should return valid non-zero pointers
        IntPtr nativeIdPtr = AravisNative.arv_get_device_id(0);
        IntPtr generatedIdPtr = AravisGenerated.arv_get_device_id(0);

        // Assert
        Assert.NotEqual(IntPtr.Zero, nativeIdPtr);
        Assert.NotEqual(IntPtr.Zero, generatedIdPtr);
        
        // Both should marshal to valid strings
        string? nativeId = Marshal.PtrToStringAnsi(nativeIdPtr);
        string? generatedId = Marshal.PtrToStringAnsi(generatedIdPtr);
        
        Assert.NotNull(nativeId);
        Assert.NotNull(generatedId);
    }

    [Fact]
    public void DeviceVendor_BothBindings_ShouldReturnValidValues()
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
        IntPtr nativeVendorPtr = AravisNative.arv_get_device_vendor(0);
        IntPtr generatedVendorPtr = AravisGenerated.arv_get_device_vendor(0);

        string? nativeVendor = Marshal.PtrToStringAnsi(nativeVendorPtr);
        string? generatedVendor = Marshal.PtrToStringAnsi(generatedVendorPtr);

        // Assert
        Assert.NotNull(nativeVendor);
        Assert.NotNull(generatedVendor);
        Assert.NotEmpty(nativeVendor);
        Assert.NotEmpty(generatedVendor);
    }

    [Fact]
    public void DeviceModel_BothBindings_ShouldReturnValidValues()
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
        IntPtr nativeModelPtr = AravisNative.arv_get_device_model(0);
        IntPtr generatedModelPtr = AravisGenerated.arv_get_device_model(0);

        string? nativeModel = Marshal.PtrToStringAnsi(nativeModelPtr);
        string? generatedModel = Marshal.PtrToStringAnsi(generatedModelPtr);

        // Assert
        Assert.NotNull(nativeModel);
        Assert.NotNull(generatedModel);
        Assert.NotEmpty(nativeModel);
        Assert.NotEmpty(generatedModel);
    }

    [Fact]
    public void DeviceSerial_BothBindings_ShouldReturnValidValues()
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
        IntPtr nativeSerialPtr = AravisNative.arv_get_device_serial_nbr(0);
        IntPtr generatedSerialPtr = AravisGenerated.arv_get_device_serial_nbr(0);

        string? nativeSerial = Marshal.PtrToStringAnsi(nativeSerialPtr);
        string? generatedSerial = Marshal.PtrToStringAnsi(generatedSerialPtr);

        // Assert
        Assert.NotNull(nativeSerial);
        Assert.NotNull(generatedSerial);
        Assert.NotEmpty(nativeSerial);
        Assert.NotEmpty(generatedSerial);
    }

    [Fact]
    public void DeviceProtocol_BothBindings_ShouldReturnValidValues()
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
        IntPtr nativeProtocolPtr = AravisNative.arv_get_device_protocol(0);
        IntPtr generatedProtocolPtr = AravisGenerated.arv_get_device_protocol(0);

        string? nativeProtocol = Marshal.PtrToStringAnsi(nativeProtocolPtr);
        string? generatedProtocol = Marshal.PtrToStringAnsi(generatedProtocolPtr);

        // Assert
        Assert.NotNull(nativeProtocol);
        Assert.NotNull(generatedProtocol);
        Assert.NotEmpty(nativeProtocol);
        Assert.NotEmpty(generatedProtocol);
    }

    [Fact]
    public void DeviceAddress_BothBindings_ShouldReturnValidValues()
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
        IntPtr nativeAddressPtr = AravisNative.arv_get_device_address(0);
        IntPtr generatedAddressPtr = AravisGenerated.arv_get_device_address(0);

        string? nativeAddress = Marshal.PtrToStringAnsi(nativeAddressPtr);
        string? generatedAddress = Marshal.PtrToStringAnsi(generatedAddressPtr);

        // Assert
        Assert.NotNull(nativeAddress);
        Assert.NotNull(generatedAddress);
        Assert.NotEmpty(nativeAddress);
        Assert.NotEmpty(generatedAddress);
    }
}
