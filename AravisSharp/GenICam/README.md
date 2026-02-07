# AravisSharp.GenICam — Node Map Support

Provides access to all camera features through the GenICam standard node map.

## Usage

```csharp
using AravisSharp;
using AravisSharp.GenICam;

using var camera = new Camera();
var device = camera.GetDevice();
var nodeMap = device.NodeMap;
```

### Read Features

```csharp
// String
string vendor = nodeMap.GetStringFeature("DeviceVendorName");

// Integer
long width = nodeMap.GetIntegerFeature("Width");

// Float
double exposure = nodeMap.GetFloatFeature("ExposureTime");

// Boolean
bool fpsEnabled = nodeMap.GetBooleanFeature("AcquisitionFrameRateEnable");
```

### Write Features

```csharp
nodeMap.SetIntegerFeature("Width", 640);
nodeMap.SetFloatFeature("ExposureTime", 10000.0);
nodeMap.SetStringFeature("PixelFormat", "Mono8");
nodeMap.SetBooleanFeature("AcquisitionFrameRateEnable", true);
nodeMap.ExecuteCommand("TriggerSoftware");
```

### Introspect Features

```csharp
var details = nodeMap.GetFeatureDetails("ExposureTime");
// details.Type           → Float
// details.AccessMode     → ReadWrite
// details.FloatMin       → 28.0
// details.FloatMax       → 1000000.0
// details.CurrentValue   → "10000"

var all = nodeMap.GetAllFeatures();
var byCategory = nodeMap.GetFeaturesByCategory();
```

## Files

| File | Purpose |
|------|---------|
| `NodeMap.cs` | Feature read / write / browse / introspect |
| `GenICamNode.cs` | Individual node wrapper |
| `FeatureDetails.cs` | Full feature metadata (type, range, choices) |
| `FeatureAccessMode.cs` | `ReadOnly`, `ReadWrite`, `WriteOnly`, `NotAvailable`, `NotImplemented` |

## Common Features

| Feature | Type | Access |
|---------|------|--------|
| `DeviceVendorName` | String | RO |
| `DeviceModelName` | String | RO |
| `Width` / `Height` | Integer | RW |
| `PixelFormat` | Enumeration | RW |
| `ExposureTime` | Float | RW |
| `Gain` | Float | RW |
| `AcquisitionFrameRate` | Float | RW |
| `TriggerMode` | Enumeration | RW |

## See Also

- [FEATURE_BROWSER_GUIDE.md](../../FEATURE_BROWSER_GUIDE.md) — full feature browser documentation
- [Aravis API docs](https://aravisproject.github.io/docs/aravis-0.8/)
- [GenICam standard](https://www.emva.org/standards-technology/genicam/)
