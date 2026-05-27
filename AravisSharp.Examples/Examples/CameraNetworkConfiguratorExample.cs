using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using AravisSharp;
using AravisSharp.Native;

namespace AravisSharp.Examples;

/// <summary>
/// Network configurator for GigE Vision cameras connected via USB-to-Ethernet adapters
/// or any scenario where camera and host NIC may be on different subnets.
///
/// Typical situation: Dalsa/Teledyne camera ships with a static IP (e.g. 192.168.1.x)
/// while the USB-to-Ethernet adapter auto-configures to 169.254.x.x (LLA),
/// or vice-versa. This tool discovers cameras, checks for subnet mismatches,
/// and lets you reconfigure the camera's IP to match your adapter — or guides
/// you through setting the adapter's IP to match the camera.
/// </summary>
public static class CameraNetworkConfiguratorExample
{
    public static void Run()
    {
        Console.WriteLine("============================================");
        Console.WriteLine("  GigE Camera Network Configurator");
        Console.WriteLine("============================================\n");

        // Step 1: Show active Ethernet adapters
        Console.WriteLine("[1/4] Scanning network adapters...");
        var adapters = GetEthernetAdapters();
        if (adapters.Count == 0)
        {
            Console.WriteLine("  WARNING: No active Ethernet adapters found.");
            Console.WriteLine("  Make sure your USB-to-Ethernet adapter is plugged in and has a driver.");
        }
        else
        {
            Console.WriteLine($"  Found {adapters.Count} active Ethernet adapter(s):");
            foreach (var (name, ip, mask) in adapters)
            {
                Console.WriteLine($"    {name}");
                Console.WriteLine($"      IP   : {ip}");
                Console.WriteLine($"      Mask : {mask}");
                Console.WriteLine($"      Subnet: {GetNetworkAddress(ip, mask)}/{GetPrefixLength(mask)}");
            }
        }

        // Step 2: Discover cameras
        Console.WriteLine("\n[2/4] Scanning for cameras (broadcast discovery)...");
        Console.WriteLine("  Note: Aravis uses UDP broadcast — cameras on a different subnet");
        Console.WriteLine("        may not respond. If none are found, see guidance below.\n");

        CameraDiscovery.UpdateDeviceList();
        var cameras = CameraDiscovery.DiscoverCameras();

        if (cameras.Count == 0)
        {
            Console.WriteLine("  No cameras found.\n");
            PrintSubnetGuidance(adapters);
            return;
        }

        Console.WriteLine($"  Found {cameras.Count} camera(s):");
        for (int i = 0; i < cameras.Count; i++)
            Console.WriteLine($"    [{i}] {cameras[i]}");

        // Step 3: Filter GigE cameras and check subnet alignment
        Console.WriteLine("\n[3/4] Checking subnet alignment for GigE cameras...");
        var gigeCamera = cameras.FirstOrDefault(c =>
            c.Protocol.Contains("GigE", StringComparison.OrdinalIgnoreCase) ||
            c.Protocol.Contains("GV", StringComparison.OrdinalIgnoreCase));

        if (gigeCamera == null)
        {
            Console.WriteLine("  No GigE Vision camera found. Network configuration only applies to GigE cameras.");
            Console.WriteLine($"  Protocols detected: {string.Join(", ", cameras.Select(c => c.Protocol).Distinct())}");
            return;
        }

        Console.WriteLine($"  GigE camera: {gigeCamera.Vendor} {gigeCamera.Model}");
        Console.WriteLine($"  Camera address: {gigeCamera.Address}");

        // Parse the camera IP from the address field (format: "IP:port" or just "IP")
        var cameraIp = gigeCamera.Address.Split(':')[0].Trim();
        Console.WriteLine($"  Camera IP: {cameraIp}");

        // Find which adapter is (or isn't) on the same subnet
        string? matchingAdapter = null;
        string? mismatchedAdapter = null;

        foreach (var (name, adapterIp, mask) in adapters)
        {
            if (IsOnSameSubnet(cameraIp, adapterIp, mask))
            {
                matchingAdapter = name;
                Console.WriteLine($"  ✓ Camera is on the same subnet as: {name} ({adapterIp}/{GetPrefixLength(mask)})");
            }
            else
            {
                mismatchedAdapter = name;
            }
        }

        if (matchingAdapter != null)
        {
            Console.WriteLine("\n  Subnet alignment is CORRECT. Camera should be reachable.");
        }
        else if (adapters.Count > 0)
        {
            Console.WriteLine("\n  SUBNET MISMATCH detected!");
            Console.WriteLine($"  Camera IP ({cameraIp}) is not on the same subnet as any adapter.");
            Console.WriteLine();
            PrintSubnetGuidance(adapters, cameraIp);
        }

        // Step 4: Connect and offer IP reconfiguration
        Console.WriteLine("\n[4/4] Connecting to camera for IP configuration...");
        Camera camera;
        try
        {
            camera = new Camera(gigeCamera.DeviceId);
            Console.WriteLine($"  Connected: {camera.GetVendorName()} {camera.GetModelName()} (S/N: {camera.GetSerialNumber()})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: Could not connect — {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("  If the camera is on a different subnet, you must first align");
            Console.WriteLine("  the adapter IP (Option A) before the library can connect.");
            return;
        }

        using (camera)
        {
            if (!camera.IsGigEVisionDevice())
            {
                Console.WriteLine("  This camera is not a GigE Vision device.");
                return;
            }

            // Read current IP config from camera
            try
            {
                var mode = camera.GvGetIpConfigurationMode();
                Console.WriteLine($"\n  Current IP mode: {FormatMode(mode)}");

                var (persistIp, persistMask, persistGw) = camera.GvGetPersistentIp();
                if (!string.IsNullOrEmpty(persistIp))
                {
                    Console.WriteLine($"  Persistent IP:   {persistIp}");
                    Console.WriteLine($"  Persistent Mask: {persistMask}");
                    Console.WriteLine($"  Persistent GW:   {(string.IsNullOrEmpty(persistGw) ? "(none)" : persistGw)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  WARNING: Could not read IP configuration — {ex.Message}");
            }

            // Offer reconfiguration options
            Console.WriteLine("\n============================================");
            Console.WriteLine("  Reconfiguration Options");
            Console.WriteLine("============================================");
            Console.WriteLine("  1. Switch camera to LLA (169.254.x.x auto-configuration)");
            Console.WriteLine("     Best for direct connection — no DHCP server needed.");
            Console.WriteLine("     Set your adapter to 169.254.0.1 / 255.255.0.0 to match.");
            Console.WriteLine();
            Console.WriteLine("  2. Switch camera to DHCP");
            Console.WriteLine("     Camera gets an IP automatically if a DHCP server is present.");
            Console.WriteLine("     USB-to-Ethernet adapters typically don't have a DHCP server.");
            Console.WriteLine();
            Console.WriteLine("  3. Set camera to a specific static IP");
            Console.WriteLine("     Full control — you choose the subnet.");
            Console.WriteLine();
            Console.WriteLine("  0. Skip — do not change camera settings");
            Console.Write("\nChoice: ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    ApplyLlaMode(camera);
                    break;
                case "2":
                    ApplyDhcpMode(camera);
                    break;
                case "3":
                    ApplyStaticIp(camera, adapters);
                    break;
                case "0":
                default:
                    Console.WriteLine("  No changes made.");
                    break;
            }
        }
    }

    // ─── Reconfiguration actions ──────────────────────────────────────────────

    private static void ApplyLlaMode(Camera camera)
    {
        try
        {
            camera.GvSetIpConfigurationMode(ArvGvIpConfigurationMode.Lla);
            Console.WriteLine("  Camera switched to LLA mode.");
            Console.WriteLine();
            Console.WriteLine("  Next steps:");
            Console.WriteLine("  1. Power-cycle the camera.");
            Console.WriteLine("  2. The camera will self-assign a 169.254.x.x address.");
            Console.WriteLine("  3. Set your adapter IP to 169.254.0.1 / 255.255.0.0");
            Console.WriteLine("     (Control Panel > Network > Adapter > IPv4 Properties)");
            Console.WriteLine("  4. Run this tool again to verify connectivity.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }
    }

    private static void ApplyDhcpMode(Camera camera)
    {
        try
        {
            camera.GvSetIpConfigurationMode(ArvGvIpConfigurationMode.Dhcp);
            Console.WriteLine("  Camera switched to DHCP mode.");
            Console.WriteLine();
            Console.WriteLine("  Next steps:");
            Console.WriteLine("  1. Power-cycle the camera.");
            Console.WriteLine("  2. Make sure a DHCP server is reachable on the adapter's network.");
            Console.WriteLine("  3. If using a direct cable without a router/DHCP server,");
            Console.WriteLine("     use LLA mode instead (Option 1).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }
    }

    private static void ApplyStaticIp(Camera camera, List<(string Name, string Ip, string Mask)> adapters)
    {
        Console.WriteLine("  Enter new static IP configuration for the camera.");
        Console.WriteLine("  Leave gateway empty if not needed (direct connection).\n");

        // Suggest an IP based on the first adapter
        string suggestedIp = "192.168.1.100";
        string suggestedMask = "255.255.255.0";
        if (adapters.Count > 0)
        {
            var (_, adapterIp, adapterMask) = adapters[0];
            suggestedIp = SuggestCameraIp(adapterIp, adapterMask);
            suggestedMask = adapterMask;
            Console.WriteLine($"  Suggested (based on '{adapters[0].Name}'): {suggestedIp} / {suggestedMask}");
            Console.WriteLine();
        }

        Console.Write($"  Camera IP   [{suggestedIp}]: ");
        var ip = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(ip)) ip = suggestedIp;

        Console.Write($"  Subnet mask [{suggestedMask}]: ");
        var mask = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(mask)) mask = suggestedMask;

        Console.Write("  Gateway     [leave empty for none]: ");
        var gw = Console.ReadLine()?.Trim() ?? "";

        Console.WriteLine();
        try
        {
            // Set mode + IP in one go
            camera.GvSetIpConfigurationMode(ArvGvIpConfigurationMode.PersistentIp);
            camera.GvSetPersistentIp(ip, mask, gw);

            Console.WriteLine($"  Camera configured:");
            Console.WriteLine($"    Mode: Static / Persistent IP");
            Console.WriteLine($"    IP  : {ip}");
            Console.WriteLine($"    Mask: {mask}");
            Console.WriteLine($"    GW  : {(string.IsNullOrEmpty(gw) ? "(none)" : gw)}");
            Console.WriteLine();
            Console.WriteLine("  Power-cycle the camera to apply the new IP address.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }
    }

    // ─── Network helpers ──────────────────────────────────────────────────────

    private static List<(string Name, string Ip, string Mask)> GetEthernetAdapters()
    {
        var result = new List<(string, string, string)>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                nic.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet &&
                nic.NetworkInterfaceType != NetworkInterfaceType.FastEthernetT &&
                nic.NetworkInterfaceType != NetworkInterfaceType.FastEthernetFx) continue;

            foreach (var addr in nic.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                result.Add((nic.Name, addr.Address.ToString(), addr.IPv4Mask.ToString()));
            }
        }
        return result;
    }

    private static bool IsOnSameSubnet(string ip1, string ip2, string mask)
    {
        try
        {
            var a = IPAddress.Parse(ip1).GetAddressBytes();
            var b = IPAddress.Parse(ip2).GetAddressBytes();
            var m = IPAddress.Parse(mask).GetAddressBytes();
            for (int i = 0; i < 4; i++)
                if ((a[i] & m[i]) != (b[i] & m[i])) return false;
            return true;
        }
        catch { return false; }
    }

    private static string GetNetworkAddress(string ip, string mask)
    {
        try
        {
            var a = IPAddress.Parse(ip).GetAddressBytes();
            var m = IPAddress.Parse(mask).GetAddressBytes();
            return string.Join(".", a.Zip(m, (x, y) => (byte)(x & y)));
        }
        catch { return ip; }
    }

    private static int GetPrefixLength(string mask)
    {
        try
        {
            return IPAddress.Parse(mask).GetAddressBytes()
                .SelectMany(b => Enumerable.Range(0, 8).Select(i => (b >> (7 - i)) & 1))
                .TakeWhile(bit => bit == 1)
                .Count();
        }
        catch { return 0; }
    }

    private static string SuggestCameraIp(string adapterIp, string mask)
    {
        try
        {
            var a = IPAddress.Parse(adapterIp).GetAddressBytes();
            var m = IPAddress.Parse(mask).GetAddressBytes();
            // Use last octet = 100 as a safe suggestion
            var suggested = a.Zip(m, (x, y) => (byte)(x & y)).ToArray();
            suggested[3] = 100;
            return string.Join(".", suggested);
        }
        catch { return "192.168.1.100"; }
    }

    private static string FormatMode(ArvGvIpConfigurationMode mode)
    {
        var parts = new List<string>();
        if (mode.HasFlag(ArvGvIpConfigurationMode.PersistentIp)) parts.Add("Static/PersistentIP");
        if (mode.HasFlag(ArvGvIpConfigurationMode.Dhcp)) parts.Add("DHCP");
        if (mode.HasFlag(ArvGvIpConfigurationMode.Lla)) parts.Add("LLA");
        return parts.Count > 0 ? string.Join(" + ", parts) : $"None/Unknown ({(int)mode})";
    }

    private static void PrintSubnetGuidance(
        List<(string Name, string Ip, string Mask)> adapters,
        string? cameraIp = null)
    {
        Console.WriteLine("── Subnet Troubleshooting ───────────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("  GigE Vision discovery uses UDP broadcast. The camera and your");
        Console.WriteLine("  USB-to-Ethernet adapter MUST be on the same IP subnet.");
        Console.WriteLine();

        if (cameraIp != null)
        {
            Console.WriteLine($"  Camera IP: {cameraIp}");
            Console.WriteLine();
            Console.WriteLine("  Option A — Change the adapter IP to match the camera (recommended):");
            Console.WriteLine("    1. Open: Control Panel > Network and Sharing Center");
            Console.WriteLine("             > Change adapter settings");
            Console.WriteLine($"    2. Right-click your USB-to-Ethernet adapter > Properties");
            Console.WriteLine("    3. Select 'Internet Protocol Version 4 (TCP/IPv4)' > Properties");
            Console.WriteLine("    4. Choose 'Use the following IP address':");

            // Suggest an adapter IP one step away from the camera IP
            var parts = cameraIp.Split('.');
            if (parts.Length == 4 && int.TryParse(parts[3], out int last))
            {
                int adapterLast = last == 1 ? 2 : 1;
                Console.WriteLine($"         IP address  : {parts[0]}.{parts[1]}.{parts[2]}.{adapterLast}");
            }
            else
            {
                Console.WriteLine($"         IP address  : (same subnet as camera, different host)");
            }
            Console.WriteLine($"         Subnet mask : 255.255.255.0");
            Console.WriteLine($"         Gateway     : (leave empty for direct connection)");
        }
        else
        {
            Console.WriteLine("  Your USB-to-Ethernet adapter may not yet have an IP assigned,");
            Console.WriteLine("  or the camera is on a completely different physical network.");
            Console.WriteLine();
            Console.WriteLine("  Option A — Use LLA (simplest for direct camera connections):");
            Console.WriteLine("    Set both the camera and adapter to use 169.254.x.x auto-IP.");
            Console.WriteLine("    Most GigE cameras fall back to LLA if no DHCP is found.");
            Console.WriteLine("    Wait ~30 seconds after connecting for LLA negotiation.");
        }

        Console.WriteLine();
        Console.WriteLine("  Option B — Run this tool after correcting the adapter IP:");
        Console.WriteLine("    The camera will appear in discovery once subnets match.");
        Console.WriteLine();

        if (adapters.Count > 0)
        {
            Console.WriteLine("  Your adapters' current configuration:");
            foreach (var (name, ip, mask) in adapters)
                Console.WriteLine($"    {name}: {ip} / {mask}");
        }

        Console.WriteLine();
        Console.WriteLine("  Tip: On Windows, you can also enable the 'GigE Vision' firewall rule:");
        Console.WriteLine("    PowerShell (as Admin):");
        Console.WriteLine("      New-NetFirewallRule -DisplayName 'GigE Vision' \\");
        Console.WriteLine("        -Direction Inbound -Protocol UDP -Action Allow \\");
        Console.WriteLine("        -LocalPort 3956,3957,1024-65535");
        Console.WriteLine("─────────────────────────────────────────────────────────────────────");
    }
}
