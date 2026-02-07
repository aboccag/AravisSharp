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

- `void SetTrigger(string source)` - Set trigger source (e.g., "Software", "Line1")
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

- `int UvGetBandwidth()` - Get bandwidth limit in bytes/second
- `void UvSetBandwidth(int bandwidth)` - Set bandwidth limit in bytes/second (≤0 disables)
- `(int Min, int Max) UvGetBandwidthBounds()` - Get bandwidth range
- `bool UvIsBandwidthControlAvailable()` - Check if bandwidth control is available

## Device Access

- `Device GetDevice()` - Get underlying device object for low-level access

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

### Basic Acquisition with Auto Exposure
```csharp
using var camera = Camera.Create(null);

// Enable auto exposure
camera.SetExposureTimeAuto(ArvAuto.Continuous);

// Set region of interest
camera.SetRegion(0, 0, 640, 480);

// Start acquisition
camera.StartAcquisition();
using var stream = camera.CreateStream();

// Acquire frames...
```

### Software Triggered Acquisition
```csharp
using var camera = Camera.Create(null);

if (camera.IsSoftwareTriggerSupported())
{
    camera.SetTrigger("Software");
    camera.StartAcquisition();
    
    using var stream = camera.CreateStream();
    
    for (int i = 0; i < 10; i++)
    {
        camera.SoftwareTrigger();
        // Pop buffer from stream...
    }
    
    camera.StopAcquisition();
}
```

### Multi-Frame Acquisition
```csharp
using var camera = Camera.Create(null);

// Set to multi-frame mode
camera.SetAcquisitionMode(ArvAcquisitionMode.MultiFrame);
camera.SetFrameCount(100);

camera.StartAcquisition();
using var stream = camera.CreateStream();

// Camera will acquire 100 frames then stop automatically
```

### GigE Vision Optimization
```csharp
using var camera = Camera.Create(null);

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
using var camera = Camera.Create(null);

if (camera.IsUSB3VisionDevice() && camera.UvIsBandwidthControlAvailable())
{
    var (min, max) = camera.UvGetBandwidthBounds();
    
    // Set to 80% of maximum bandwidth
    camera.UvSetBandwidth((int)(max * 0.8));
}
```

### Generic Feature Access
```csharp
using var camera = Camera.Create(null);

// Check feature availability
if (camera.IsFeatureAvailable("GainRaw"))
{
    // Get bounds
    var (min, max) = camera.GetIntegerFeatureBounds("GainRaw");
    
    // Set value
    camera.SetIntegerFeature("GainRaw", min + (max - min) / 2);
}

// Execute command
if (camera.IsFeatureAvailable("UserSetSave"))
{
    camera.ExecuteCommand("UserSetSave");
}
```

## Notes

- All methods throw `AravisException` on errors
- Camera must not be disposed before calling methods
- GigE Vision and USB3 Vision specific methods will fail if called on the wrong device type
- Feature availability should be checked before accessing features
- Auto modes (exposure, gain) may not be available on all cameras
- Frame rate limiting requires camera support
