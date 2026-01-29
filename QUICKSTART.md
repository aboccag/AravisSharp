# AravisSharp Quick Reference

## Installation

```bash
# Install Aravis library
sudo apt-get install libaravis-0.8-0 libaravis-0.8-dev aravis-tools-0.8

# Check installation
./check-setup.sh

# Build and run
cd AravisSharp
dotnet run
```

## Basic Usage

### 1. Discover Cameras

```csharp
using AravisSharp;

var cameras = CameraDiscovery.DiscoverCameras();
foreach (var cam in cameras)
    Console.WriteLine(cam); // Basler acA1920-50gm (S/N: 12345) [GigEVision] @ 192.168.1.100
```

### 2. Connect to Camera

```csharp
// First available camera
using var camera = new Camera();

// Specific camera by ID
using var camera = new Camera("Basler-12345678");
```

### 3. Get Camera Info

```csharp
Console.WriteLine(camera.GetVendorName());      // "Basler"
Console.WriteLine(camera.GetModelName());       // "acA1920-50gm"
Console.WriteLine(camera.GetSerialNumber());    // "12345678"
```

### 4. Configure Camera

```csharp
// Exposure (microseconds)
camera.SetExposureTime(10000);  // 10ms
var exp = camera.GetExposureTime();

// Gain
camera.SetGain(5.0);
var gain = camera.GetGain();

// Frame rate
camera.SetFrameRate(30.0);
var fps = camera.GetFrameRate();

// ROI
camera.SetRegion(0, 0, 1920, 1080);
var (x, y, w, h) = camera.GetRegion();

// Pixel format
camera.SetPixelFormat("Mono8");
var format = camera.GetPixelFormat();
```

### 5. Acquire Images

```csharp
// Create stream
using var stream = camera.CreateStream();

// Allocate buffers
var buffers = new List<AravisSharp.Buffer>();
for (int i = 0; i < 5; i++)
{
    var buf = new AravisSharp.Buffer(new IntPtr(1920 * 1080 * 2));
    buffers.Add(buf);
    stream.PushBuffer(buf);
}

// Start acquisition
camera.StartAcquisition();

// Get frame
var frame = stream.PopBuffer(2000); // 2 sec timeout
if (frame?.Status == ArvBufferStatus.Success)
{
    Console.WriteLine($"Frame {frame.FrameId}: {frame.Width}x{frame.Height}");
    
    // Access image data (zero-copy)
    var data = frame.GetDataSpan();
    
    // Or copy to array
    var bytes = frame.CopyData();
    
    // Return to pool
    stream.PushBuffer(frame);
}

// Stop
camera.StopAcquisition();
```

### 6. GenICam Features (Advanced)

```csharp
var device = camera.GetDevice();

// String features
device.SetStringFeature("TriggerMode", "On");
var mode = device.GetStringFeature("TriggerMode");

// Integer features  
device.SetIntegerFeature("Width", 1920);
var width = device.GetIntegerFeature("Width");

// Float features
device.SetFloatFeature("ExposureTime", 5000.0);
var exp = device.GetFloatFeature("ExposureTime");

// Boolean features
device.SetBooleanFeature("AcquisitionFrameRateEnable", true);
var enabled = device.GetBooleanFeature("AcquisitionFrameRateEnable");

// Commands
device.ExecuteCommand("AcquisitionStart");
```

## Common Patterns

### Continuous Acquisition Loop

```csharp
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

camera.StartAcquisition();

while (!cts.Token.IsCancellationRequested)
{
    var buffer = stream.PopBuffer(1000);
    if (buffer?.Status == ArvBufferStatus.Success)
    {
        ProcessImage(buffer.GetDataSpan());
        stream.PushBuffer(buffer);
    }
}

camera.StopAcquisition();
```

### Software Triggered Acquisition

```csharp
var device = camera.GetDevice();
device.SetStringFeature("TriggerMode", "On");
device.SetStringFeature("TriggerSource", "Software");

camera.StartAcquisition();

for (int i = 0; i < 10; i++)
{
    camera.SoftwareTrigger();
    var buffer = stream.PopBuffer(5000);
    if (buffer?.Status == ArvBufferStatus.Success)
    {
        ProcessImage(buffer);
        stream.PushBuffer(buffer);
    }
}

camera.StopAcquisition();
```

### Save Image to File

```csharp
using AravisSharp.Utilities;

// Save as raw binary
ImageHelper.SaveToRawFile(buffer, "image.raw");

// Save as PGM (Mono8 only)
ImageHelper.SaveToPgm(buffer, "image.pgm");
```

### Performance Monitoring

```csharp
using AravisSharp.Utilities;

var stats = new AcquisitionStats();
stats.Start();

camera.StartAcquisition();

while (acquiring)
{
    var buffer = stream.PopBuffer(1000);
    if (buffer?.Status == ArvBufferStatus.Success)
    {
        stats.RecordSuccess(buffer.GetData().Size);
        stats.PrintStatus(); // Real-time display
    }
}

stats.Stop();
Console.WriteLine(stats.ToString()); // Final summary
```

## Pixel Formats

```csharp
using AravisSharp.Native;

// Common formats
ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8      // 8-bit mono
ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12     // 12-bit mono
ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8  // 8-bit Bayer
ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED // RGB 24-bit

// Helpers
ImageHelper.GetPixelFormatName(format);
ImageHelper.GetBytesPerPixel(format);
ImageHelper.IsMonoFormat(format);
ImageHelper.IsBayerFormat(format);
```

## Error Handling

```csharp
try
{
    using var camera = new Camera();
    camera.StartAcquisition();
    // ...
}
catch (AravisException ex)
{
    Console.WriteLine($"Aravis error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| No cameras found | Run `arv-tool-0.8 -l` to verify camera is detected |
| Permission denied (USB) | Setup udev rules (see README.md) |
| Poor GigE performance | Increase network buffers: `sudo sysctl -w net.core.rmem_max=33554432` |
| Library not found | Run `sudo ldconfig` or set `LD_LIBRARY_PATH` |
| Timeouts | Increase timeout value or check camera connection |
| Buffer underruns | Allocate more buffers or reduce frame rate |

## Performance Tips

1. **Use zero-copy access**: `GetDataSpan()` instead of `CopyData()`
2. **Allocate enough buffers**: 10-20 for high-speed capture
3. **GigE optimization**: Enable jumbo frames (MTU 9000)
4. **USB3 optimization**: Ensure USB3 port and cable
5. **Process in separate thread**: Don't block acquisition loop
6. **Reuse buffers**: Push back to stream immediately

## API Reference

### CameraDiscovery
- `DiscoverCameras()` → List<CameraInfo>
- `GetDeviceCount()` → uint
- `UpdateDeviceList()` → void

### Camera
- Constructor: `Camera(string? deviceId = null)`
- Info: `GetVendorName()`, `GetModelName()`, `GetSerialNumber()`
- Region: `GetRegion()`, `SetRegion(x, y, w, h)`, `GetWidthBounds()`, `GetHeightBounds()`
- Exposure: `GetExposureTime()`, `SetExposureTime(us)`, `GetExposureTimeBounds()`
- Gain: `GetGain()`, `SetGain(value)`, `GetGainBounds()`
- Frame Rate: `GetFrameRate()`, `SetFrameRate(fps)`
- Pixel Format: `GetPixelFormat()`, `SetPixelFormat(format)`
- Acquisition: `StartAcquisition()`, `StopAcquisition()`, `AbortAcquisition()`
- Trigger: `SoftwareTrigger()`
- Advanced: `CreateStream()`, `GetDevice()`

### Stream
- `PushBuffer(buffer)` → void
- `PopBuffer()` → Buffer?
- `PopBuffer(timeoutMs)` → Buffer?
- `GetStatistics()` → (completed, failures, underruns)

### Buffer
- Properties: `Width`, `Height`, `PixelFormat`, `Status`, `FrameId`, `Timestamp`
- `GetData()` → (IntPtr, int)
- `GetDataSpan()` → ReadOnlySpan<byte>
- `CopyData()` → byte[]
- `CopyDataTo(Span<byte>)` → void
- `GetImageRegion()` → (x, y, width, height)

### Device (GenICam)
- `GetStringFeature(name)` / `SetStringFeature(name, value)`
- `GetIntegerFeature(name)` / `SetIntegerFeature(name, value)`
- `GetFloatFeature(name)` / `SetFloatFeature(name, value)`
- `GetBooleanFeature(name)` / `SetBooleanFeature(name, value)`
- `ExecuteCommand(name)`

## See Also

- [Full README](README.md) - Complete documentation
- [Examples.cs](Examples.cs) - Code examples
- [Aravis Documentation](https://aravisproject.github.io/aravis/aravis-stable/)
