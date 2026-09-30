using AravisSharp;
using AravisSharp.Native;

namespace AravisSharp.Examples;

/// <summary>
/// Diagnostic tool for GigE Vision cameras (e.g. Basler).
/// Checks network configuration, packet size, socket buffers, and attempts a test acquisition.
/// Run this first when GigE cameras are not delivering images.
/// </summary>
public static class GigEDiagnosticExample
{
    public static void Run()
    {
        Console.WriteLine("============================================");
        Console.WriteLine("  GigE Vision Camera Diagnostic Tool");
        Console.WriteLine("============================================\n");

        // Step 1: Discover cameras
        Console.WriteLine("[1/7] Discovering cameras...");
        CameraDiscovery.UpdateDeviceList();
        var cameras = CameraDiscovery.DiscoverCameras();

        // A camera named by --device / ARAVIS_EXAMPLE_DEVICE_ID may be reachable (e.g. by IP)
        // without answering discovery, so only stop here when nothing names one.
        if (cameras.Count == 0 && !CameraPicker.HasExplicitDeviceId)
        {
            Console.WriteLine("  ERROR: No cameras found!\n");
            PrintDiscoveryTroubleshooting();
            return;
        }

        var hasGigECamera = cameras.Any(cam =>
            cam.Protocol.Contains("GigEVision", StringComparison.OrdinalIgnoreCase) ||
            cam.Protocol.Contains("GigE", StringComparison.OrdinalIgnoreCase) ||
            cam.Protocol.Contains("GV", StringComparison.OrdinalIgnoreCase));

        if (cameras.Count > 0 && !hasGigECamera)
        {
            Console.WriteLine("\n  WARNING: No GigE Vision camera detected among discovered devices.");
            Console.WriteLine("  Protocols found: " + string.Join(", ", cameras.Select(c => c.Protocol).Distinct()));
        }

        // Ask which camera to diagnose rather than taking the first GigE one: on a shared
        // network it may belong to another application, and the diagnostic streams from it.
        var deviceId = CameraPicker.Choose(cameras);
        if (deviceId == null)
            return;

        // Step 2: Connect to camera
        Console.WriteLine("\n[2/7] Connecting to camera...");
        Camera camera;
        try
        {
            camera = new Camera(deviceId);
            Console.WriteLine($"  Connected: {camera.GetVendorName()} {camera.GetModelName()}");
            Console.WriteLine($"  Serial: {camera.GetSerialNumber()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: Failed to connect: {ex.Message}");
            return;
        }

        using (camera)
        {
            // Step 3: Check device type
            Console.WriteLine("\n[3/7] Checking device type...");
            bool isGigE = camera.IsGigEVisionDevice();
            bool isUSB = camera.IsUSB3VisionDevice();
            Console.WriteLine($"  GigE Vision: {(isGigE ? "YES" : "no")}");
            Console.WriteLine($"  USB3 Vision: {(isUSB ? "YES" : "no")}");

            if (!isGigE)
            {
                Console.WriteLine("\n  This camera is NOT a GigE device. This diagnostic is for GigE cameras.");
                Console.WriteLine("  If you expected a GigE camera, check the network connection.");
                return;
            }

            // Step 4: Negotiate packet size
            Console.WriteLine("\n[4/7] Negotiating GigE packet size...");
            int packetSize = 0;
            try
            {
                // First read current packet size
                var currentSize = camera.GvGetPacketSize();
                Console.WriteLine($"  Current packet size: {currentSize} bytes");

                // Auto-negotiate optimal size
                camera.GvAutoPacketSize();
                packetSize = camera.GvGetPacketSize();
                Console.WriteLine($"  Negotiated packet size: {packetSize} bytes");

                if (packetSize <= 576)
                {
                    Console.WriteLine("  WARNING: Packet size is very small (≤576 bytes)!");
                    Console.WriteLine("  This indicates a network path issue:");
                    Console.WriteLine("    - The NIC may not support jumbo frames");
                    Console.WriteLine("    - A switch between PC and camera may not support jumbo frames");
                    Console.WriteLine("    - Direct connection recommended for best performance");
                    Console.WriteLine("  Images may still work but at lower throughput.");
                }
                else if (packetSize >= 8000)
                {
                    Console.WriteLine($"  GOOD: Jumbo frames supported ({packetSize} bytes)");
                }
                else
                {
                    Console.WriteLine($"  OK: Standard Ethernet frames ({packetSize} bytes)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: Packet size negotiation failed: {ex.Message}");
                Console.WriteLine("  Falling back to 1500 bytes...");
                try
                {
                    camera.GvSetPacketSize(1500);
                    packetSize = 1500;
                }
                catch
                {
                    Console.WriteLine("  ERROR: Could not set packet size at all!");
                    return;
                }
            }

            // Step 5: Read camera settings
            Console.WriteLine("\n[5/7] Reading camera settings...");
            var (x, y, width, height) = camera.GetRegion();
            Console.WriteLine($"  ROI: {width}x{height} at ({x},{y})");
            Console.WriteLine($"  Pixel format: {camera.GetPixelFormat()}");
            Console.WriteLine($"  Exposure: {camera.GetExposureTime():F0} µs");
            
            try { Console.WriteLine($"  Frame rate: {camera.GetFrameRate():F1} fps"); } catch { }
            
            uint payloadSize;
            try
            {
                payloadSize = camera.GetPayloadSize();
                Console.WriteLine($"  Payload size: {payloadSize} bytes");
            }
            catch
            {
                payloadSize = (uint)(width * height * 2);
                Console.WriteLine($"  Payload size (calculated): {payloadSize} bytes");
            }

            // Step 6: Create stream and configure
            Console.WriteLine("\n[6/7] Creating and configuring GigE stream...");
            Stream stream;
            try
            {
                stream = camera.CreateStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: Failed to create stream: {ex.Message}");
                Console.WriteLine("  Check Windows Firewall settings!");
                PrintFirewallTroubleshooting();
                return;
            }

            using (stream)
            {
                // Configure GigE stream
                try
                {
                    stream.ConfigureGigEDefaults(socketBufferSizeMB: 4);
                    var socketBufSize = stream.GetSocketBufferSize();
                    Console.WriteLine($"  Socket buffer: {socketBufSize / 1024} KB");
                    Console.WriteLine($"  Packet resend: enabled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  WARNING: Could not configure stream: {ex.Message}");
                }

                // Allocate and push buffers
                var buffers = new List<AravisSharp.Buffer>();
                for (int i = 0; i < 10; i++)
                {
                    var buffer = new AravisSharp.Buffer(new IntPtr(payloadSize));
                    buffers.Add(buffer);
                    stream.PushBuffer(buffer);
                }
                Console.WriteLine($"  Pushed {buffers.Count} buffers ({payloadSize} bytes each)");

                // Step 7: Test acquisition
                Console.WriteLine("\n[7/7] Test acquisition (5 frames, 3s timeout each)...");
                try
                {
                    camera.StartAcquisition();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: Failed to start acquisition: {ex.Message}");
                    PrintFirewallTroubleshooting();
                    foreach (var buf in buffers) buf.Dispose();
                    return;
                }

                int successCount = 0;
                int failCount = 0;
                int timeoutCount = 0;

                for (int i = 0; i < 5; i++)
                {
                    Console.Write($"  Frame {i + 1}/5: ");
                    var buffer = stream.PopBuffer(3000); // 3 second timeout

                    if (buffer != null)
                    {
                        if (buffer.Status == ArvBufferStatus.Success)
                        {
                            successCount++;
                            Console.WriteLine($"OK - {buffer.Width}x{buffer.Height}, " +
                                $"FrameID={buffer.FrameId}, {buffer.GetData().Size} bytes");
                        }
                        else
                        {
                            failCount++;
                            Console.WriteLine($"FAILED - Status: {buffer.Status}");
                        }
                        stream.PushBuffer(buffer);
                    }
                    else
                    {
                        timeoutCount++;
                        Console.WriteLine("TIMEOUT - no frame received within 3 seconds");
                    }
                }

                camera.StopAcquisition();

                // Print results
                Console.WriteLine("\n============================================");
                Console.WriteLine("  DIAGNOSTIC RESULTS");
                Console.WriteLine("============================================");
                Console.WriteLine($"  Frames received: {successCount}/5");
                Console.WriteLine($"  Frames failed:   {failCount}/5");
                Console.WriteLine($"  Timeouts:        {timeoutCount}/5");

                var (completed, failures, underruns) = stream.GetStatistics();
                Console.WriteLine($"\n  Stream stats: {completed} completed, {failures} failures, {underruns} underruns");

                try
                {
                    var (port, resent, missing) = stream.GetGigEStatistics();
                    Console.WriteLine($"  GigE stats:   port={port}, resent={resent}, missing={missing}");
                    
                    if (missing > 0)
                    {
                        Console.WriteLine($"\n  WARNING: {missing} missing packets detected!");
                    }
                }
                catch { }

                Console.WriteLine();
                if (successCount == 5)
                {
                    Console.WriteLine("  RESULT: All frames received successfully!");
                    Console.WriteLine("  Your GigE camera is working correctly with AravisSharp.");
                }
                else if (successCount > 0)
                {
                    Console.WriteLine("  RESULT: Partial success - some frames received.");
                    Console.WriteLine("  Some packet loss is occurring. Recommendations:");
                    PrintPerformanceTips();
                }
                else if (timeoutCount == 5)
                {
                    Console.WriteLine("  RESULT: No frames received (all timeouts).");
                    Console.WriteLine("  The camera may be blocked by Windows Firewall.");
                    PrintFirewallTroubleshooting();
                }
                else
                {
                    Console.WriteLine("  RESULT: Frames received but all failed.");
                    Console.WriteLine("  Likely packet loss or buffer configuration issue.");
                    PrintPerformanceTips();
                }

                foreach (var buf in buffers) buf.Dispose();
            }
        }
    }

    private static void PrintDiscoveryTroubleshooting()
    {
        Console.WriteLine("Troubleshooting camera discovery:");
        Console.WriteLine("  1. Check physical connection (Ethernet cable)");
        Console.WriteLine("  2. Ensure camera has power (PoE or external)");
        Console.WriteLine("  3. Camera and PC must be on the same subnet");
        Console.WriteLine("     - Camera IP: often 169.254.x.x (link-local) initially");
        Console.WriteLine("     - Set static IP on the NIC in the same subnet, or use DHCP");
        Console.WriteLine("  4. Disable VPN software (can interfere with multicast discovery)");
        Console.WriteLine("  5. Allow Aravis/your application through Windows Firewall");
        Console.WriteLine("  6. Try Basler Pylon or Aravis viewer to verify camera is reachable");
    }

    private static void PrintFirewallTroubleshooting()
    {
        Console.WriteLine("\nWindows Firewall troubleshooting:");
        Console.WriteLine("  GigE Vision uses UDP for image streaming. Windows Firewall");
        Console.WriteLine("  may block the incoming UDP packets from the camera.\n");
        Console.WriteLine("  Quick fix (run as Administrator in PowerShell):");
        Console.WriteLine("    New-NetFirewallRule -DisplayName 'AravisSharp GigE' `");
        Console.WriteLine("      -Direction Inbound -Protocol UDP -Action Allow `");
        Console.WriteLine("      -LocalPort 1024-65535\n");
        Console.WriteLine("  Or: Windows Security > Firewall > Allow an app through firewall");
        Console.WriteLine("       and add your application.\n");
        Console.WriteLine("  You can also temporarily disable the firewall to test.");
    }

    private static void PrintPerformanceTips()
    {
        Console.WriteLine("\n  Performance tips for GigE Vision:");
        Console.WriteLine("    1. Enable Jumbo Frames on your NIC (9014 MTU)");
        Console.WriteLine("       Network Adapter > Properties > Advanced > Jumbo Packet");
        Console.WriteLine("    2. Connect camera directly to NIC (avoid unmanaged switches)");
        Console.WriteLine("    3. Disable NIC power management:");
        Console.WriteLine("       Device Manager > NIC > Power Management > uncheck 'Allow turn off'");
        Console.WriteLine("    4. Increase receive buffer in NIC driver:");
        Console.WriteLine("       NIC Properties > Advanced > Receive Buffers > set to maximum");
        Console.WriteLine("    5. Disable Windows Firewall for the camera NIC");
        Console.WriteLine("    6. Lower frame rate or resolution if packet loss persists");
    }
}
