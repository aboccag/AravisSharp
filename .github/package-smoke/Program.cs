using System.Diagnostics;
using System.Runtime.InteropServices;
using AravisSharp;
using AravisSharp.Native;

const int FramesToAcquire = 5;

AravisLibrary.RegisterResolver();
if (!AravisLibrary.IsAravisAvailable())
    return Fail("the native Aravis library could not be loaded from the package");

var interfaceId = Marshal.StringToCoTaskMemUTF8("Fake");
try { AravisNative.arv_enable_interface(interfaceId); }
finally { Marshal.FreeCoTaskMem(interfaceId); }

// The point of the package is that it needs nothing installed: make sure the Aravis that
// got loaded is the bundled copy and not one the machine happens to have.
// Match the native library by its file name: "aravis" alone also matches AravisSharp.dll.
var aravisModule = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
    .FirstOrDefault(m => m.ModuleName.StartsWith("libaravis-0.8", StringComparison.OrdinalIgnoreCase));
if (aravisModule is not null)
{
    Console.WriteLine($"Loaded {aravisModule.FileName}");
    var bundled = Path.GetFullPath(AppContext.BaseDirectory);
    if (!Path.GetFullPath(aravisModule.FileName).StartsWith(bundled, StringComparison.OrdinalIgnoreCase))
        return Fail($"Aravis was loaded from outside the application ({bundled})");
}
else if (!OperatingSystem.IsMacOS())
{
    // Process.Modules does not list dylibs on macOS; everywhere else it must find Aravis.
    return Fail("Aravis is not among the loaded modules");
}

var fake = CameraDiscovery.DiscoverCameras()
    .FirstOrDefault(c => string.Equals(c.Protocol, "Fake", StringComparison.OrdinalIgnoreCase));
if (fake is null)
    return Fail("the fake camera was not discovered");

using var camera = new Camera(fake.DeviceId);
Console.WriteLine($"Opened {camera.GetVendorName()} {camera.GetModelName()} ({fake.DeviceId})");

using var stream = camera.CreateStream();
var payload = (int)camera.GetPayloadSize();
for (var i = 0; i < 4; i++)
    stream.PushBuffer(new AravisSharp.Buffer(payload));

camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
camera.StartAcquisition();
try
{
    for (var frame = 0; frame < FramesToAcquire; frame++)
    {
        var buffer = stream.PopBuffer(5000);
        if (buffer is null)
            return Fail($"frame {frame}: no buffer within 5 s");
        if (buffer.Status != ArvBufferStatus.Success)
            return Fail($"frame {frame}: status {buffer.Status}");
        if (buffer.GetDataSpan().Length == 0)
            return Fail($"frame {frame}: empty buffer");

        Console.WriteLine($"Frame {buffer.FrameId}: {buffer.Width}x{buffer.Height}");
        stream.PushBuffer(buffer);
    }
}
finally
{
    camera.StopAcquisition();
}

Console.WriteLine($"OK: {FramesToAcquire} frames acquired through the packaged native runtime on {RuntimeInformation.RuntimeIdentifier}");
return 0;

static int Fail(string reason)
{
    Console.Error.WriteLine($"Package smoke test FAILED: {reason}");
    return 1;
}
