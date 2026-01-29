# GenICam Node Map Support

AravisSharp now provides complete access to camera features through the GenICam standard node map interface.

## Overview

GenICam (Generic Interface for Cameras) is an industry standard for configuring industrial cameras. The **NodeMap** provides hierarchical access to all camera features including:

- Device information (vendor, model, firmware, serial number)
- Image configuration (width, height, pixel format, ROI)
- Acquisition settings (exposure time, gain, frame rate)
- Trigger configuration (mode, source, activation)
- Advanced camera-specific features

## Usage

### Basic Feature Access

```csharp
using AravisSharp;
using AravisSharp.GenICam;

// Connect to camera
using var camera = new Camera();

// Get the device and node map
var device = camera.GetDevice();
var nodeMap = device.NodeMap;

// Read string features
var vendor = nodeMap.GetStringFeature("DeviceVendorName");
var model = nodeMap.GetStringFeature("DeviceModelName");
var pixelFormat = nodeMap.GetStringFeature("PixelFormat");

// Read numeric features
long width = nodeMap.GetIntegerFeature("Width");
long height = nodeMap.GetIntegerFeature("Height");
double exposureTime = nodeMap.GetFloatFeature("ExposureTime");
double gain = nodeMap.GetFloatFeature("Gain");

// Read boolean features
bool frameRateEnabled = nodeMap.GetBooleanFeature("AcquisitionFrameRateEnable");
```

### Modifying Features

```csharp
// Set integer features
nodeMap.SetIntegerFeature("Width", 640);
nodeMap.SetIntegerFeature("Height", 480);

// Set float features
nodeMap.SetFloatFeature("ExposureTime", 20000.0);  // 20ms
nodeMap.SetFloatFeature("Gain", 12.0);

// Set string features (enumerations)
nodeMap.SetStringFeature("PixelFormat", "Mono8");
nodeMap.SetStringFeature("TriggerMode", "On");

// Set boolean features
nodeMap.SetBooleanFeature("AcquisitionFrameRateEnable", true);

// Execute commands
nodeMap.ExecuteCommand("AcquisitionStart");
```

### Common Camera Features

#### Device Information
- `DeviceVendorName` - Camera manufacturer
- `DeviceModelName` - Camera model
- `DeviceFirmwareVersion` - Firmware version
- `DeviceSerialNumber` - Serial number
- `DeviceTemperature` - Sensor temperature (if supported)

#### Image Format
- `Width` - Image width in pixels
- `Height` - Image height in pixels
- `OffsetX` - Horizontal ROI offset
- `OffsetY` - Vertical ROI offset
- `PixelFormat` - Pixel format (Mono8, Mono12, RGB8, etc.)
- `BinningHorizontal` - Horizontal binning
- `BinningVertical` - Vertical binning

#### Acquisition Control
- `ExposureTime` - Exposure time in microseconds
- `Gain` - Sensor gain
- `AcquisitionMode` - Continuous, SingleFrame, MultiFrame
- `AcquisitionFrameRate` - Frame rate in Hz
- `AcquisitionFrameRateEnable` - Enable frame rate control

#### Trigger Control
- `TriggerMode` - On/Off
- `TriggerSource` - Software, Line0, etc.
- `TriggerActivation` - RisingEdge, FallingEdge
- `TriggerDelay` - Trigger delay in microseconds

## Feature Discovery

To explore available features for your specific camera:

```csharp
// Get common features
var features = nodeMap.GetAllFeatures();
foreach (var feature in features)
{
    Console.WriteLine($"{feature.Name}: {feature.Value}");
}

// Query specific feature
var node = nodeMap.GetNode("Gain");
if (node != null)
{
    Console.WriteLine($"Feature: {node.Name}");
    Console.WriteLine($"Value: {node.Value}");
    Console.WriteLine($"Available: {node.IsAvailable}");
}
```

## Examples

### Example 1: Read All Device Information

```csharp
var deviceInfo = new Dictionary<string, string>
{
    ["Vendor"] = nodeMap.GetStringFeature("DeviceVendorName"),
    ["Model"] = nodeMap.GetStringFeature("DeviceModelName"),
    ["Firmware"] = nodeMap.GetStringFeature("DeviceFirmwareVersion"),
    ["Serial"] = nodeMap.GetStringFeature("DeviceSerialNumber")
};

foreach (var (key, value) in deviceInfo)
{
    Console.WriteLine($"{key}: {value}");
}
```

### Example 2: Configure Camera for High-Speed Capture

```csharp
// Set small ROI
nodeMap.SetIntegerFeature("Width", 640);
nodeMap.SetIntegerFeature("Height", 480);

// Fast exposure
nodeMap.SetFloatFeature("ExposureTime", 1000.0);  // 1ms

// Increase gain to compensate
nodeMap.SetFloatFeature("Gain", 18.0);

// Enable high frame rate
nodeMap.SetBooleanFeature("AcquisitionFrameRateEnable", true);
nodeMap.SetFloatFeature("AcquisitionFrameRate", 100.0);  // 100 fps
```

### Example 3: Setup Hardware Triggering

```csharp
// Enable trigger mode
nodeMap.SetStringFeature("TriggerMode", "On");

// Use external line 0
nodeMap.SetStringFeature("TriggerSource", "Line0");

// Trigger on rising edge
nodeMap.SetStringFeature("TriggerActivation", "RisingEdge");

// Optional: add delay
nodeMap.SetFloatFeature("TriggerDelay", 100.0);  // 100µs delay
```

## Implementation Details

The NodeMap implementation uses the Aravis device API for feature access:
- `arv_device_get_string_feature_value()` / `arv_device_set_string_feature_value()`
- `arv_device_get_integer_feature_value()` / `arv_device_set_integer_feature_value()`
- `arv_device_get_float_feature_value()` / `arv_device_set_float_feature_value()`
- `arv_device_get_boolean_feature_value()` / `arv_device_set_boolean_feature_value()`
- `arv_device_execute_command()`

## Error Handling

Feature access throws `InvalidOperationException` if:
- Feature doesn't exist
- Feature is not readable/writable
- Value is out of range
- Feature is locked

Always use try-catch blocks when accessing camera-specific features:

```csharp
try
{
    var temperature = nodeMap.GetFloatFeature("DeviceTemperature");
    Console.WriteLine($"Temperature: {temperature}°C");
}
catch (InvalidOperationException)
{
    Console.WriteLine("Temperature sensor not available");
}
```

## Demo Programs

Run the GenICam demos:

```bash
cd AravisSharp
dotnet run

# Choose option 3: GenICam node map demo (simple)
# Or option 4: GenICam explorer (interactive)
```

## See Also

- [GenICam Standard](https://www.emva.org/standards-technology/genicam/)
- [Aravis Documentation](https://aravisproject.github.io/docs/aravis-0.8/)
- [Camera.cs](../Camera.cs) - High-level camera wrapper
- [Device.cs](../Device.cs) - Low-level device access
