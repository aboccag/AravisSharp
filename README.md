# AravisSharp - C# Bindings for Aravis Industrial Camera Library

Complete C# bindings for the Aravis library, providing full support for industrial cameras (GigE Vision, USB3 Vision) on Linux.

## Features

- **Camera Discovery**: Automatic detection of GigE and USB3 Vision cameras
- **Full Camera Control**: Exposure, gain, frame rate, ROI, pixel format, and more
- **Image Acquisition**: High-performance streaming with buffer management
- **GenICam Support**: Direct access to GenICam features via Device API
- **Zero-Copy Access**: Efficient image data access using Span<T>
- **Linux Native**: Optimized for Linux with direct P/Invoke to libaravis

## Prerequisites

### Install Aravis Library

On Ubuntu/Debian:
```bash
sudo apt-get update
sudo apt-get install libaravis-0.8-0 libaravis-0.8-dev
```

On Fedora/RHEL:
```bash
sudo dnf install aravis aravis-devel
```

### USB3 Camera Permissions

For USB3 cameras, you may need to set up udev rules:

```bash
# Create udev rule file
sudo nano /etc/udev/rules.d/99-aravis.rules
```

Add the following content (adjust for your camera vendor ID):
```
# Basler USB3 cameras
SUBSYSTEM=="usb", ATTRS{idVendor}=="2676", MODE="0666"

# Generic USB3 Vision cameras
SUBSYSTEM=="usb", ENV{ID_VENDOR_ID}=="*", MODE="0666"
```

Reload udev rules:
```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

### GigE Camera Network Setup

For optimal GigE camera performance:

1. **Increase receive buffer size:**
```bash
sudo sysctl -w net.core.rmem_max=33554432
sudo sysctl -w net.core.rmem_default=33554432
```

2. **Make permanent** by adding to `/etc/sysctl.conf`:
```
net.core.rmem_max=33554432
net.core.rmem_default=33554432
```

3. **Configure network interface** (if using dedicated NIC):
```bash
# Set MTU to 9000 (Jumbo Frames)
sudo ip link set dev eth0 mtu 9000

# Disable firewall for camera subnet
sudo ufw allow from 192.168.1.0/24
```

## Building and Running

```bash
cd AravisSharp
dotnet build
dotnet run
```

## Usage Examples

### Basic Camera Discovery

```csharp
using AravisSharp;

// Discover all cameras
var cameras = CameraDiscovery.DiscoverCameras();

foreach (var info in cameras)
{
    Console.WriteLine($"Found: {info}");
}
```

### Connect and Acquire Images

```csharp
using AravisSharp;

// Connect to first available camera
using var camera = new Camera();

Console.WriteLine($"Connected: {camera.GetModelName()}");

// Configure camera
camera.SetExposureTime(10000); // 10ms
camera.SetGain(10.0);

// Create stream
using var stream = camera.CreateStream();

// Allocate buffers
var buffers = new List<Buffer>();
for (int i = 0; i < 5; i++)
{
    var buffer = new Buffer(new IntPtr(1920 * 1200 * 2));
    buffers.Add(buffer);
    stream.PushBuffer(buffer);
}

// Start acquisition
camera.StartAcquisition();

// Acquire frame
var frame = stream.PopBuffer(2000); // 2 second timeout

if (frame != null && frame.Status == ArvBufferStatus.Success)
{
    Console.WriteLine($"Frame {frame.FrameId}: {frame.Width}x{frame.Height}");
    
    // Zero-copy access
    var imageData = frame.GetDataSpan();
    
    // Or copy to array
    var bytes = frame.CopyData();
    
    // Push buffer back
    stream.PushBuffer(frame);
}

camera.StopAcquisition();

// Cleanup
foreach (var buf in buffers)
    buf.Dispose();
```

### Advanced GenICam Feature Access

```csharp
using var camera = new Camera();
var device = camera.GetDevice();

// Set custom features
device.SetStringFeature("TriggerMode", "On");
device.SetStringFeature("TriggerSource", "Line1");
device.SetFloatFeature("ExposureTime", 5000.0);
device.SetIntegerFeature("Width", 1920);
device.SetBooleanFeature("AcquisitionFrameRateEnable", true);

// Execute commands
device.ExecuteCommand("AcquisitionStart");

// Read features
var triggerMode = device.GetStringFeature("TriggerMode");
var width = device.GetIntegerFeature("Width");
Console.WriteLine($"Trigger: {triggerMode}, Width: {width}");
```

### Continuous Acquisition Loop

```csharp
using var camera = new Camera();
using var stream = camera.CreateStream();

// Setup buffers
var buffers = new List<Buffer>();
for (int i = 0; i < 10; i++)
{
    var buffer = new Buffer(new IntPtr(1920 * 1200 * 2));
    buffers.Add(buffer);
    stream.PushBuffer(buffer);
}

camera.StartAcquisition();

// Continuous acquisition
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => 
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.Token.IsCancellationRequested)
{
    var buffer = stream.PopBuffer(1000);
    
    if (buffer != null && buffer.Status == ArvBufferStatus.Success)
    {
        // Process image
        ProcessImage(buffer);
        
        // Return buffer to pool
        stream.PushBuffer(buffer);
    }
}

camera.StopAcquisition();

void ProcessImage(Buffer buffer)
{
    Console.WriteLine($"Processing frame {buffer.FrameId}");
    // Your image processing code here
}
```

## API Overview

### CameraDiscovery
- `DiscoverCameras()` - Find all available cameras
- `GetDeviceCount()` - Get number of devices
- `GetCameraInfo(index)` - Get info for specific device

### Camera
- Camera control: `SetExposureTime()`, `SetGain()`, `SetFrameRate()`
- ROI: `GetRegion()`, `SetRegion()`
- Pixel format: `GetPixelFormat()`, `SetPixelFormat()`
- Acquisition: `StartAcquisition()`, `StopAcquisition()`
- Stream: `CreateStream()`
- Device: `GetDevice()` - For GenICam feature access

### Stream
- `PushBuffer()` - Add buffer to input queue
- `PopBuffer(timeout)` - Get completed buffer
- `GetStatistics()` - Get stream statistics

### Buffer
- Properties: `Width`, `Height`, `PixelFormat`, `Status`, `FrameId`, `Timestamp`
- `GetDataSpan()` - Zero-copy access to image data
- `CopyData()` - Copy image data to byte array
- `CopyDataTo(span)` - Copy to provided span

### Device
- `GetStringFeature()`, `SetStringFeature()`
- `GetIntegerFeature()`, `SetIntegerFeature()`
- `GetFloatFeature()`, `SetFloatFeature()`
- `GetBooleanFeature()`, `SetBooleanFeature()`
- `ExecuteCommand()`

## Supported Cameras

Tested with:
- ✅ Basler ace (USB3 and GigE)
- ✅ FLIR/Point Grey (USB3 and GigE)
- ✅ Allied Vision (GigE)
- ✅ JAI (GigE)
- ✅ Teledyne DALSA (GigE)

Any camera supporting GenICam (GigE Vision or USB3 Vision) should work.

## Troubleshooting

### "No cameras found"
- Ensure Aravis is installed: `ldconfig -p | grep aravis`
- Check camera connection: `arv-tool-0.8 -l`
- Verify USB permissions or network connectivity

### "Failed to open camera"
- Check if another application is using the camera
- For USB3: verify udev rules are loaded
- For GigE: check firewall settings

### Performance Issues
- Increase network buffer size (see GigE setup above)
- Use zero-copy access (`GetDataSpan()`) instead of copying
- Increase number of buffers in stream
- For GigE: enable Jumbo Frames (MTU 9000)

### Library Not Found
If you get "DllNotFoundException":
```bash
# Find aravis library
ldconfig -p | grep aravis

# If not found, rebuild library cache
sudo ldconfig

# Or set LD_LIBRARY_PATH
export LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu:$LD_LIBRARY_PATH
```

## License

This binding follows the LGPL-2.1-or-later license of the Aravis library.

## Resources

- [Aravis Documentation](https://aravisproject.github.io/aravis/aravis-stable/)
- [Aravis GitHub](https://github.com/AravisProject/aravis)
- [GenICam Standard](http://www.genicam.org)
