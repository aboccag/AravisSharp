# AravisSharp Comprehensive API Documentation

This document lists all the high-level wrapper methods now available in the `Camera` class, organized by category.

## Camera Information

- `string GetVendorName()` - Get camera vendor name
- `string GetModelName()` - Get camera model name
- `string GetSerialNumber()` - Get camera serial number
- `string GetDeviceId()` - Get camera device ID
- `(int Width, int Height) GetSensorSize()` - Get full sensor dimensions

## Region of Interest (ROI)

- `void SetRegion(int x, int y, int width, int height)` - Set ROI
- `(int X, int Y, int Width, int Height) GetRegion()` - Get current ROI
- `(int Min, int Max) GetWidthBounds()` - Get allowed width range
- `(int Min, int Max) GetHeightBounds()` - Get allowed height range
- `int GetWidthIncrement()` - Get width step size
- `int GetHeightIncrement()` - Get height step size

## Pixel Format

- `string GetPixelFormat()` - Get current pixel format
- `void SetPixelFormat(string format)` - Set pixel format

## Exposure Control

- `double GetExposureTime()` - Get exposure time in microseconds
- `void SetExposureTime(double exposureTime)` - Set exposure time in microseconds
- `(double Min, double Max) GetExposureTimeBounds()` - Get exposure time range
- `ArvAuto GetExposureTimeAuto()` - Get auto exposure mode
- `void SetExposureTimeAuto(ArvAuto mode)` - Set auto exposure mode (Off, Once, Continuous)
- `bool IsExposureTimeAvailable()` - Check if exposure control is available
- `bool IsExposureAutoAvailable()` - Check if auto exposure is available

## Gain Control

- `double GetGain()` - Get gain value
- `void SetGain(double gain)` - Set gain value
- `(double Min, double Max) GetGainBounds()` - Get gain range
- `ArvAuto GetGainAuto()` - Get auto gain mode
- `void SetGainAuto(ArvAuto mode)` - Set auto gain mode (Off, Once, Continuous)
- `bool IsGainAvailable()` - Check if gain control is available
- `bool IsGainAutoAvailable()` - Check if auto gain is available

## Frame Rate Control

- `double GetFrameRate()` - Get frame rate in Hz
- `void SetFrameRate(double frameRate)` - Set frame rate in Hz
- `(double Min, double Max) GetFrameRateBounds()` - Get frame rate range
- `bool GetFrameRateEnable()` - Check if frame rate limiting is enabled
- `void SetFrameRateEnable(bool enable)` - Enable/disable frame rate limiting
- `bool IsFrameRateAvailable()` - Check if frame rate control is available

## Binning

- `(int Horizontal, int Vertical) GetBinning()` - Get current binning
- `void SetBinning(int horizontal, int vertical)` - Set binning
- `bool IsBinningAvailable()` - Check if binning is available

## Acquisition Control

- `void StartAcquisition()` - Start continuous acquisition
- `void StopAcquisition()` - Stop acquisition
- `void AbortAcquisition()` - Abort acquisition immediately
- `ArvAcquisitionMode GetAcquisitionMode()` - Get acquisition mode
- `void SetAcquisitionMode(ArvAcquisitionMode mode)` - Set acquisition mode (Continuous, SingleFrame, MultiFrame)
- `long GetFrameCount()` - Get frame count for MultiFrame mode
- `void SetFrameCount(long count)` - Set frame count for MultiFrame mode
- `(long Min, long Max) GetFrameCountBounds()` - Get frame count range

## Trigger Control

- `void SetTrigger(string source)` - Enable triggered acquisition (FrameStart, rising edge) from a source (e.g., "Software", "Line1"); other triggers are disabled
- `void SetTriggerSource(string source)` - Set trigger source explicitly
- `string GetTriggerSource()` - Get current trigger source
- `void ClearTriggers()` - Clear all trigger settings
- `void SoftwareTrigger()` - Execute software trigger
- `bool IsSoftwareTriggerSupported()` - Check if software trigger is supported

## Stream and Buffer Management

- `Stream CreateStream()` - Create stream for image acquisition
- `uint GetPayloadSize()` - Get image buffer size in bytes

## Generic Feature Access

These methods allow access to any GenICam feature by name:

### String Features
- `string GetStringFeature(string feature)` - Get string feature value
- `void SetStringFeature(string feature, string value)` - Set string feature value

### Integer Features
- `long GetIntegerFeature(string feature)` - Get integer feature value
- `void SetIntegerFeature(string feature, long value)` - Set integer feature value
- `(long Min, long Max) GetIntegerFeatureBounds(string feature)` - Get integer range
- `long GetIntegerFeatureIncrement(string feature)` - Get integer step size

### Float Features
- `double GetFloatFeature(string feature)` - Get float feature value
- `void SetFloatFeature(string feature, double value)` - Set float feature value
- `(double Min, double Max) GetFloatFeatureBounds(string feature)` - Get float range
- `double GetFloatFeatureIncrement(string feature)` - Get float step size

### Boolean Features
- `bool GetBooleanFeature(string feature)` - Get boolean feature value
- `void SetBooleanFeature(string feature, bool value)` - Set boolean feature value

### Command Execution
- `void ExecuteCommand(string feature)` - Execute GenICam command

### Feature Availability
- `bool IsFeatureAvailable(string feature)` - Check if a feature is available

## Device Type Detection

- `bool IsGigEVisionDevice()` - Check if GigE Vision camera
- `bool IsUSB3VisionDevice()` - Check if USB3 Vision camera

## GigE Vision Specific Methods

These methods only work with GigE Vision cameras:

- `void GvAutoPacketSize()` - Automatically determine optimal packet size
- `int GvGetPacketSize()` - Get packet size in bytes
- `void GvSetPacketSize(int size)` - Set packet size in bytes

## USB3 Vision Specific Methods

These methods only work with USB3 Vision cameras:

- `uint UvGetBandwidth()` - Get bandwidth limit in bytes/second
- `void UvSetBandwidth(uint bandwidth)` - Set bandwidth limit in bytes/second (0 disables the limit)
- `(uint Min, uint Max) UvGetBandwidthBounds()` - Get bandwidth range
- `bool UvIsBandwidthControlAvailable()` - Check if bandwidth control is available

## Device Access

- `Device GetDevice()` - Get underlying device object for low-level access. `Device` is `IDisposable`: `using var device = camera.GetDevice();`

## Enumerations

### ArvAcquisitionMode
- `Continuous` - Continuous acquisition until stopped
- `SingleFrame` - Acquire one frame then stop
- `MultiFrame` - Acquire specified number of frames then stop

### ArvAuto
- `Off` - Manual control (auto disabled)
- `Once` - Single automatic adjustment then return to manual
- `Continuous` - Continuous automatic adjustment

## Usage Examples

The examples open the camera named by `deviceId`: a `CameraInfo.DeviceId` from `CameraDiscovery.DiscoverCameras()`, or `null` for the first camera Aravis finds.

### Basic Acquisition with Auto Exposure
```csharp
using AravisSharp;
using AravisSharp.Native;   // ArvBufferStatus

string? deviceId = CameraDiscovery.DiscoverCameras().FirstOrDefault()?.DeviceId;
using var camera = new Camera(deviceId);

// Enable auto exposure
camera.SetExposureTimeAuto(ArvAuto.Continuous);

// Set region of interest
camera.SetRegion(0, 0, 640, 480);

// Create the stream and queue buffers sized for the current format and ROI
using var stream = camera.CreateStream();
var payloadSize = (int)camera.GetPayloadSize();
for (int i = 0; i < 10; i++)
    stream.PushBuffer(new AravisSharp.Buffer(payloadSize));

// Start acquisition once buffers are queued
camera.StartAcquisition();

for (int i = 0; i < 100; i++)
{
    var buffer = stream.PopBuffer(2000);   // timeout in ms
    if (buffer == null)
        continue;                          // timeout

    if (buffer.Status == ArvBufferStatus.Success)
        Console.WriteLine($"Frame {buffer.FrameId}: {buffer.Width}x{buffer.Height}");

    stream.PushBuffer(buffer);             // always hand the buffer back
}

camera.StopAcquisition();
```

### Software Triggered Acquisition
```csharp
using var camera = new Camera(deviceId);

if (camera.IsSoftwareTriggerSupported())
{
    camera.SetTrigger("Software");

    using var stream = camera.CreateStream();
    var payloadSize = (int)camera.GetPayloadSize();
    for (int i = 0; i < 4; i++)
        stream.PushBuffer(new AravisSharp.Buffer(payloadSize));

    camera.StartAcquisition();

    for (int i = 0; i < 10; i++)
    {
        camera.SoftwareTrigger();
        var buffer = stream.PopBuffer(5000);
        if (buffer == null)
            continue;

        if (buffer.Status == ArvBufferStatus.Success)
        {
            // Process the frame...
        }
        stream.PushBuffer(buffer);
    }

    camera.StopAcquisition();
    camera.ClearTriggers();   // back to free-running
}
```

### Multi-Frame Acquisition
```csharp
using var camera = new Camera(deviceId);

// Set to multi-frame mode
camera.SetAcquisitionMode(ArvAcquisitionMode.MultiFrame);
camera.SetFrameCount(100);

using var stream = camera.CreateStream();
var payloadSize = (int)camera.GetPayloadSize();
for (int i = 0; i < 10; i++)
    stream.PushBuffer(new AravisSharp.Buffer(payloadSize));

camera.StartAcquisition();

// Camera will acquire 100 frames then stop automatically
int received = 0;
while (received < 100)
{
    var buffer = stream.PopBuffer(2000);
    if (buffer == null)
        break;                // timeout: the camera has stopped sending

    if (buffer.Status == ArvBufferStatus.Success)
        received++;
    stream.PushBuffer(buffer);
}

camera.StopAcquisition();
```

### GigE Vision Optimization
```csharp
using var camera = new Camera(deviceId);

if (camera.IsGigEVisionDevice())
{
    // Optimize packet size for network
    camera.GvAutoPacketSize();
    
    int packetSize = camera.GvGetPacketSize();
    Console.WriteLine($"Packet size: {packetSize} bytes");
}
```

### USB3 Vision Bandwidth Control
```csharp
using var camera = new Camera(deviceId);

if (camera.IsUSB3VisionDevice() && camera.UvIsBandwidthControlAvailable())
{
    var (min, max) = camera.UvGetBandwidthBounds();
    
    // Set to 80% of maximum bandwidth
    camera.UvSetBandwidth((uint)(max * 0.8));
}
```

### Generic Feature Access
```csharp
using var camera = new Camera(deviceId);

// Check feature availability
if (camera.IsFeatureAvailable("GainRaw"))
{
    // Get bounds
    var (min, max) = camera.GetIntegerFeatureBounds("GainRaw");
    
    // Set value
    camera.SetIntegerFeature("GainRaw", min + (max - min) / 2);
}

// Execute command: save the current settings to user set 1.
// WARNING: this overwrites that user set in the camera's non-volatile memory.
if (camera.IsFeatureAvailable("UserSetSelector") && camera.IsFeatureAvailable("UserSetSave"))
{
    camera.SetStringFeature("UserSetSelector", "UserSet1");
    camera.ExecuteCommand("UserSetSave");
}
```

> **Warning:** `UserSetSave` writes to the camera's non-volatile memory and replaces what the selected user set held, including a start-up configuration another application relies on. Run it only on purpose, on a camera you own — never from routine or test code.

## Notes

- All methods throw `AravisException` on errors
- Camera must not be disposed before calling methods
- GigE Vision and USB3 Vision specific methods will fail if called on the wrong device type
- Feature availability should be checked before accessing features
- Auto modes (exposure, gain) may not be available on all cameras
- Frame rate limiting requires camera support
