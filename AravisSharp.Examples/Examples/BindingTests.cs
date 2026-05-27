using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.Examples;

/// <summary>
/// Basic diagnostics for the manually declared native bindings.
/// </summary>
public static class BindingTests
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Native Bindings ===\n");

        TestNativeVersion();
        TestInterfaces();
        TestCameraEnumeration();
        TestDeviceInfo();

        Console.WriteLine("\n✓ All binding tests passed!");
    }

    private static void TestNativeVersion()
    {
        Console.WriteLine("Test 1: Native Version");
        var version = AravisLibrary.GetNativeVersion();
        Console.WriteLine($"    Aravis: {version}");

        if (version.Major != 0 || version.Minor != 8)
        {
            throw new Exception($"Expected Aravis 0.8.x, got {version}.");
        }

        Console.WriteLine("  ✓ Native version is compatible\n");
    }

    private static void TestInterfaces()
    {
        Console.WriteLine("Test 2: Interface Enumeration");

        uint interfaceCount = AravisNative.arv_get_n_interfaces();
        Console.WriteLine($"    Interfaces: {interfaceCount}");

        for (uint i = 0; i < interfaceCount; i++)
        {
            string? id = Marshal.PtrToStringUTF8(AravisNative.arv_get_interface_id(i));
            Console.WriteLine($"    [{i}] {id}");
        }

        Console.WriteLine("  ✓ Interface enumeration completed\n");
    }

    private static void TestCameraEnumeration()
    {
        Console.WriteLine("Test 3: Camera Enumeration");
        Console.WriteLine("  Comparing: arv_update_device_list() & arv_get_n_devices()");
        
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();

        Console.WriteLine($"    Devices: {deviceCount}");
        
        for (uint i = 0; i < deviceCount; i++)
        {
            string? id = Marshal.PtrToStringUTF8(AravisNative.arv_get_device_id(i));
            Console.WriteLine($"    Device {i}: {id}");
        }

        Console.WriteLine("  ✓ Camera enumeration completed\n");
    }

    private static void TestDeviceInfo()
    {
        Console.WriteLine("Test 4: Device Information");
        
        AravisNative.arv_update_device_list();
        uint deviceCount = AravisNative.arv_get_n_devices();
        
        if (deviceCount == 0)
        {
            Console.WriteLine("  ⚠ No devices found, skipping device info test\n");
            return;
        }
        
        // Test first device
        uint deviceIndex = 0;
        
        // Manual bindings
        IntPtr manualVendor = AravisNative.arv_get_device_vendor(deviceIndex);
        IntPtr manualModel = AravisNative.arv_get_device_model(deviceIndex);
        IntPtr manualSerial = AravisNative.arv_get_device_serial_nbr(deviceIndex);
        IntPtr manualProtocol = AravisNative.arv_get_device_protocol(deviceIndex);
        IntPtr manualPhysicalId = AravisNative.arv_get_device_physical_id(deviceIndex);
        IntPtr manualManufacturerInfo = AravisNative.arv_get_device_manufacturer_info(deviceIndex);
        
        string? mv = Marshal.PtrToStringUTF8(manualVendor);
        string? mm = Marshal.PtrToStringUTF8(manualModel);
        string? ms = Marshal.PtrToStringUTF8(manualSerial);
        string? mp = Marshal.PtrToStringUTF8(manualProtocol);
        string? physicalId = Marshal.PtrToStringUTF8(manualPhysicalId);
        string? manufacturerInfo = Marshal.PtrToStringUTF8(manualManufacturerInfo);
        
        Console.WriteLine($"    Vendor: {mv}");
        Console.WriteLine($"    Model: {mm}");
        Console.WriteLine($"    Serial: {ms}");
        Console.WriteLine($"    Protocol: {mp}");
        Console.WriteLine($"    Physical ID: {physicalId}");
        Console.WriteLine($"    Manufacturer Info: {manufacturerInfo}");

        Console.WriteLine("  ✓ Device info read completed\n");
    }
}
