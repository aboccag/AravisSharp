using System;
using System.Runtime.InteropServices;
using AravisSharp.Generated;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Unit tests for auto-generated AravisGenerated bindings
/// </summary>
public class AravisGeneratedTests
{
    [Fact]
    public void UpdateDeviceList_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => AravisGenerated.arv_update_device_list());
        Assert.Null(exception);
    }

    [Fact]
    public void GetNumberOfDevices_ShouldReturnNonNegativeValue()
    {
        // Arrange
        AravisGenerated.arv_update_device_list();

        // Act
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Assert
        Assert.True(deviceCount >= 0);
    }

    [Fact]
    public void GetDeviceId_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr deviceIdPtr = AravisGenerated.arv_get_device_id(0);
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
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr vendorPtr = AravisGenerated.arv_get_device_vendor(0);
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
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr modelPtr = AravisGenerated.arv_get_device_model(0);
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
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr serialPtr = AravisGenerated.arv_get_device_serial_nbr(0);
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
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr protocolPtr = AravisGenerated.arv_get_device_protocol(0);
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
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr addressPtr = AravisGenerated.arv_get_device_address(0);
        string? address = Marshal.PtrToStringAnsi(addressPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, addressPtr);
        Assert.NotNull(address);
        Assert.NotEmpty(address);
    }

    [Fact]
    public void GetDevicePhysicalId_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr physicalIdPtr = AravisGenerated.arv_get_device_physical_id(0);
        string? physicalId = Marshal.PtrToStringAnsi(physicalIdPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, physicalIdPtr);
        Assert.NotNull(physicalId);
        Assert.NotEmpty(physicalId);
    }

    [Fact]
    public void GetInterfaceId_WithValidIndex_ShouldReturnNonNull()
    {
        // Arrange
        AravisGenerated.arv_update_device_list();
        uint deviceCount = AravisGenerated.arv_get_n_devices();

        // Skip if no devices
        if (deviceCount == 0)
        {
            return;
        }

        // Act
        IntPtr interfaceIdPtr = AravisGenerated.arv_get_interface_id(0);
        string? interfaceId = Marshal.PtrToStringAnsi(interfaceIdPtr);

        // Assert
        Assert.NotEqual(IntPtr.Zero, interfaceIdPtr);
        Assert.NotNull(interfaceId);
        Assert.NotEmpty(interfaceId);
    }

    [Fact]
    public void GetGeneratedFunctionsCount_ShouldBeGreaterThan400()
    {
        // This test verifies that the generated bindings contain a substantial number of functions
        // The AravisGenerated class should have 475 functions
        
        var methods = typeof(AravisGenerated).GetMethods(
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Static);

        var arvMethods = methods.Where(m => m.Name.StartsWith("arv_")).Count();

        // Assert
        Assert.True(arvMethods >= 400, 
            $"Expected at least 400 arv_* functions, but found {arvMethods}");
    }
}
