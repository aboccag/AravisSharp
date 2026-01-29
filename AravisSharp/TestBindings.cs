using System;
using System.Runtime.InteropServices;
using AravisSharp.Native;
using AravisSharp.Generated;

namespace AravisSharp;

/// <summary>
/// Tests to verify that auto-generated bindings work identically to manual bindings
/// </summary>
public static class BindingTests
{
    public static void RunTests()
    {
        Console.WriteLine("=== Testing Auto-Generated Bindings ===\n");

        TestCameraEnumeration();
        TestDeviceInfo();

        Console.WriteLine("\n✓ All binding tests passed!");
    }

    private static void TestCameraEnumeration()
    {
        Console.WriteLine("Test 1: Camera Enumeration");
        Console.WriteLine("  Comparing: arv_update_device_list() & arv_get_n_devices()");
        
        // Using manual binding
        AravisNative.arv_update_device_list();
        uint manualCount = AravisNative.arv_get_n_devices();
        
        // Using generated binding
        AravisGenerated.arv_update_device_list();
        uint generatedCount = AravisGenerated.arv_get_n_devices();
        
        Console.WriteLine($"    Manual:    {manualCount} device(s)");
        Console.WriteLine($"    Generated: {generatedCount} device(s)");
        
        if (manualCount == generatedCount)
        {
            Console.WriteLine("  ✓ Device count matches");
            
            // Compare device IDs
            for (uint i = 0; i < manualCount; i++)
            {
                IntPtr manualIdPtr = AravisNative.arv_get_device_id(i);
                IntPtr generatedIdPtr = AravisGenerated.arv_get_device_id(i);
                
                string? manualId = Marshal.PtrToStringAnsi(manualIdPtr);
                string? generatedId = Marshal.PtrToStringAnsi(generatedIdPtr);
                
                Console.WriteLine($"    Device {i}: {manualId}");
                
                if (manualId != generatedId)
                {
                    throw new Exception($"Device ID mismatch for device {i}!");
                }
            }
            Console.WriteLine();
        }
        else
        {
            throw new Exception("Device count mismatch between manual and generated bindings!");
        }
    }

    private static void TestDeviceInfo()
    {
        Console.WriteLine("Test 2: Device Information");
        Console.WriteLine("  Comparing: arv_get_device_* functions");
        
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
        
        // Generated bindings
        IntPtr generatedVendor = AravisGenerated.arv_get_device_vendor(deviceIndex);
        IntPtr generatedModel = AravisGenerated.arv_get_device_model(deviceIndex);
        IntPtr generatedSerial = AravisGenerated.arv_get_device_serial_nbr(deviceIndex);
        IntPtr generatedProtocol = AravisGenerated.arv_get_device_protocol(deviceIndex);
        
        string? mv = Marshal.PtrToStringAnsi(manualVendor);
        string? gv = Marshal.PtrToStringAnsi(generatedVendor);
        string? mm = Marshal.PtrToStringAnsi(manualModel);
        string? gm = Marshal.PtrToStringAnsi(generatedModel);
        string? ms = Marshal.PtrToStringAnsi(manualSerial);
        string? gs = Marshal.PtrToStringAnsi(generatedSerial);
        string? mp = Marshal.PtrToStringAnsi(manualProtocol);
        string? gp = Marshal.PtrToStringAnsi(generatedProtocol);
        
        Console.WriteLine($"    Vendor: {mv}");
        Console.WriteLine($"    Model: {mm}");
        Console.WriteLine($"    Serial: {ms}");
        Console.WriteLine($"    Protocol: {mp}");
        
        if (mv == gv && mm == gm && ms == gs && mp == gp)
        {
            Console.WriteLine("  ✓ Device info matches\n");
        }
        else
        {
            throw new Exception("Device info mismatch between manual and generated bindings!");
        }
    }
}
