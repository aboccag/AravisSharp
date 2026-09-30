# AravisSharp — Quick Reference

## Installation

### Linux (Ubuntu / Debian)

```bash
# Option A: System package
sudo apt update && sudo apt install -y libaravis-0.8-0

# Option B: NuGet package (bundles libaravis; GLib, libusb and zlib come from the distribution)
dotnet add package AravisSharp

# Check the runtime requirements (.NET, GLib, zlib, libusb, udev rules)
./check-setup.sh

# USB3 Vision cameras: set up udev permissions
./setup-usb-permissions.sh   # then unplug / replug the camera
```

### Windows

```powershell
# NuGet package includes all native DLLs — nothing else to install
dotnet add package AravisSharp

# USB3 Vision cameras: install WinUSB driver with Zadig
# See WINDOWS_SETUP.md for step-by-step guide
# GigE cameras work without driver changes
```

### macOS

```bash
# Apple Silicon: the NuGet package bundles Aravis and its dylibs
dotnet add package AravisSharp

# Intel (osx-x64 is not built): install Aravis with Homebrew
brew install aravis
```

### Build & Run the Examples

```bash
# From the repository root: AravisSharp is a library, the demo menu is AravisSharp.Examples
dotnet build AravisSharp.slnx
dotnet run --project AravisSharp.Examples -f net10.0
```

The examples never pick a camera on their own: they list the cameras and ask which one to open, since on a shared network the first camera Aravis finds may belong to another application. To skip the prompt, name the device ID with `--device`, or set `ARAVIS_EXAMPLE_DEVICE_ID`:

```bash
dotnet run --project AravisSharp.Examples -f net10.0 -- --device "<device id>"
```

---

## Common Patterns

### 1. Discover Cameras

```csharp
using AravisSharp;
using AravisSharp.Native;

AravisLibrary.RegisterResolver();  // optional: the resolver registers itself when AravisSharp loads

CameraDiscovery.UpdateDeviceList();
var cameras = CameraDiscovery.DiscoverCameras();
foreach (var cam in cameras)
    Console.WriteLine(cam);
// "Basler acA720-520um (S/N: 40013997) [USB3Vision]"
```

### 2. Connect to a Camera

```csharp
// First available camera (on a shared network, name the camera instead:
// the first one Aravis finds may belong to another application)
using var camera = new Camera();

// Specific camera by ID
using var camera = new Camera("Basler-40013997");
```

### 3. Read Camera Info

```csharp
Console.WriteLine(camera.GetVendorName());      // "Basler"
Console.WriteLine(camera.GetModelName());        // "acA720-520um"
Console.WriteLine(camera.GetSerialNumber());     // "40013997"
```

### 4. Configure Camera

```csharp
camera.SetExposureTime(10000);     // 10 ms
camera.SetGain(5.0);
camera.SetFrameRate(30.0);
camera.SetRegion(0, 0, 720, 540);  // x, y, width, height
camera.SetPixelFormat("Mono8");
```

### 5. Acquire Images

```csharp
using var stream = camera.CreateStream();

// Allocate buffer pool, sized for the current pixel format and ROI
var payloadSize = (int)camera.GetPayloadSize();
for (int i = 0; i < 10; i++)
{
    var buf = new AravisSharp.Buffer(payloadSize);
    stream.PushBuffer(buf);
}

camera.StartAcquisition();

var frame = stream.PopBuffer(2000);  // 2 s timeout
if (frame != null)
{
    if (frame.Status == ArvBufferStatus.Success)
    {
        Console.WriteLine($"Frame {frame.FrameId}: {frame.Width}x{frame.Height}");
        var data = frame.GetDataSpan();   // zero-copy
    }
    stream.PushBuffer(frame);         // always return to pool, whatever the status
}

camera.StopAcquisition();
```

### 6. GenICam Features (NodeMap)

```csharp
using var device = camera.GetDevice();
var nodeMap = device.NodeMap;

// String / enum features
nodeMap.SetStringFeature("PixelFormat", "Mono8");
var fmt = nodeMap.GetStringFeature("PixelFormat");

// Integer features
nodeMap.SetIntegerFeature("Width", 640);
long w = nodeMap.GetIntegerFeature("Width");

// Float features
nodeMap.SetFloatFeature("ExposureTime", 5000.0);
double exp = nodeMap.GetFloatFeature("ExposureTime");

// Boolean features
nodeMap.SetBooleanFeature("AcquisitionFrameRateEnable", true);

// Commands
nodeMap.ExecuteCommand("TriggerSoftware");
```

### 7. Feature Introspection

```csharp
var details = nodeMap.GetFeatureDetails("ExposureTime");
Console.WriteLine($"Type:   {details.Type}");        // Float
Console.WriteLine($"Access: {details.AccessMode}");   // ReadWrite
Console.WriteLine($"Range:  {details.FloatMin} – {details.FloatMax}");

// Enumeration choices
var pf = nodeMap.GetFeatureDetails("PixelFormat");
Console.WriteLine($"Choices: {string.Join(", ", pf.EnumChoices)}");
```

---

## Continuous Acquisition Loop

```csharp
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

camera.StartAcquisition();
while (!cts.Token.IsCancellationRequested)
{
    var buffer = stream.PopBuffer(1000);
    if (buffer == null)
        continue;                        // timeout

    if (buffer.Status == ArvBufferStatus.Success)
        ProcessImage(buffer.GetDataSpan());

    stream.PushBuffer(buffer);           // always hand it back, or the pool drains
}
camera.StopAcquisition();
```

## Software Trigger

```csharp
camera.SetTrigger("Software");   // TriggerMode=On, TriggerSource=Software on FrameStart

camera.StartAcquisition();
for (int i = 0; i < 10; i++)
{
    camera.SoftwareTrigger();
    var buffer = stream.PopBuffer(5000);
    if (buffer == null)
        continue;                    // timeout

    if (buffer.Status == ArvBufferStatus.Success)
        ProcessImage(buffer);

    stream.PushBuffer(buffer);
}
camera.StopAcquisition();
camera.ClearTriggers();          // back to free-running
```

## Performance Monitoring

```csharp
using AravisSharp.Utilities;

var stats = new AcquisitionStats();
stats.Start();

// … acquisition loop …
stats.RecordSuccess(buffer.GetData().Size);
stats.PrintStatus();

stats.Stop();
Console.WriteLine(stats.ToString());
```

## Save Image

```csharp
using AravisSharp.Utilities;

ImageHelper.SaveToRawFile(buffer, "image.raw");
ImageHelper.SaveToPgm(buffer, "image.pgm");   // Mono8 only
```

---

## Error Handling

```csharp
try
{
    using var camera = new Camera();
    camera.StartAcquisition();
}
catch (AravisException ex)
{
    Console.WriteLine($"Aravis error: {ex.Message}");
}
```

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `DllNotFoundException` | Add the `AravisSharp` NuGet package (bundles the native runtimes) or install Aravis system-wide |
| No cameras found | Linux: `./check-setup.sh`; Windows: install WinUSB via Zadig |
| Permission denied (USB) | `./setup-usb-permissions.sh`, then unplug / replug the camera |
| Missing packets (USB) | `./increase-usb-buffer.sh` or check USB3 cable |
| GigE poor performance | `sudo sysctl -w net.core.rmem_max=33554432` |
| Timeouts | Increase timeout; check camera connection |
| Buffer underruns | Allocate more buffers (10–20) |

## API Quick Reference

### CameraDiscovery
`UpdateDeviceList()` · `GetDeviceCount()` · `DiscoverCameras()`

### Camera
`new Camera(id?)` · `GetVendorName()` · `GetModelName()` · `GetSerialNumber()`
`SetExposureTime(µs)` · `SetGain(v)` · `SetFrameRate(fps)` · `SetRegion(x,y,w,h)` · `SetPixelFormat(fmt)`
`CreateStream()` · `GetPayloadSize()` · `StartAcquisition()` · `StopAcquisition()` · `AcquireSingleFrame(timeoutUs)`
`SetTrigger(source)` · `SoftwareTrigger()` · `ClearTriggers()` · `GetDevice()` (dispose the returned `Device`)

### Stream
`PushBuffer(buf)` · `PopBuffer(timeoutMs?)` · `GetStatistics()`

### Buffer
`Width` · `Height` · `PixelFormat` · `Status` · `FrameId` · `Timestamp`
`GetDataSpan()` · `CopyData()` · `GetImageRegion()`

### Device / NodeMap
`Get/SetStringFeature` · `Get/SetIntegerFeature` · `Get/SetFloatFeature` · `Get/SetBooleanFeature` · `ExecuteCommand`
`GetFeatureDetails(name)` · `GetAllFeatures()` · `GetFeaturesByCategory()`
