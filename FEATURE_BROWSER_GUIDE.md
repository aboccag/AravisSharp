# AravisSharp — GenICam Feature Browser Guide

## Overview

The GenICam feature browser lets you introspect, browse, and modify every feature exposed by your camera through the GenICam standard node map.

### Capabilities

- **Feature metadata** — name, display name, description, tooltip
- **Access modes** — ReadOnly, ReadWrite, WriteOnly, NotAvailable
- **Feature types** — Integer, Float, String, Boolean, Enumeration, Command, Category
- **Value constraints** — min / max / increment for numeric features
- **Enumeration choices** — available options with display names
- **Category tree** — hierarchical organisation matching the GenICam standard
- **Visibility levels** — Beginner, Expert, Guru, Invisible

---

## Quick Start

```bash
cd AravisSharp
dotnet run
# Choose option 5: Feature browser (comprehensive)
# Or option 8: Quick feature demo
```

---

## Using the NodeMap API

### Read Feature Details

```csharp
using var camera = new Camera();
var device = camera.GetDevice();
var nodeMap = device.NodeMap;

var details = nodeMap.GetFeatureDetails("ExposureTime");

Console.WriteLine($"Name:        {details.Name}");
Console.WriteLine($"Display:     {details.DisplayName}");
Console.WriteLine($"Type:        {details.Type}");           // Float
Console.WriteLine($"Access:      {details.AccessMode}");      // ReadWrite
Console.WriteLine($"Value:       {details.CurrentValue}");
Console.WriteLine($"Range:       {details.FloatMin} – {details.FloatMax}");
Console.WriteLine($"Increment:   {details.FloatIncrement}");
```

### Browse by Category

```csharp
var categories = nodeMap.GetFeaturesByCategory();

foreach (var (category, features) in categories)
{
    Console.WriteLine($"\n📁 {category}");
    foreach (var f in features)
    {
        var rw = f.AccessMode == FeatureAccessMode.ReadWrite ? "RW" : "RO";
        Console.WriteLine($"  [{rw}] {f.DisplayName,-30} = {f.CurrentValue}");

        if (f.Type == FeatureType.Enumeration)
            Console.WriteLine($"       Choices: {string.Join(", ", f.EnumChoices)}");
    }
}
```

### Flat List

```csharp
var all = nodeMap.GetAllFeatures();
Console.WriteLine($"Total features: {all.Count}");

foreach (var f in all.OrderBy(f => f.Name))
    Console.WriteLine($"{f.Name}: {f.CurrentValue}  ({f.Type}, {f.AccessMode})");
```

### Search

```csharp
var matches = nodeMap.GetAllFeatures()
    .Where(f => f.Name.Contains("gain", StringComparison.OrdinalIgnoreCase))
    .ToList();

foreach (var f in matches)
    Console.WriteLine($"{f.DisplayName}: {f.CurrentValue}");
```

### Modify a Feature

```csharp
var details = nodeMap.GetFeatureDetails("ExposureTime");

if (details.AccessMode != FeatureAccessMode.ReadWrite)
{
    Console.WriteLine("Feature is not writable");
    return;
}

// Float
nodeMap.SetFloatFeature("ExposureTime", 10000.0);

// Integer
nodeMap.SetIntegerFeature("Width", 640);

// Enumeration (set as string)
nodeMap.SetStringFeature("PixelFormat", "Mono8");

// Boolean
nodeMap.SetBooleanFeature("AcquisitionFrameRateEnable", true);

// Command (no value)
nodeMap.ExecuteCommand("TriggerSoftware");
```

---

## Feature Types

| Type | Icon | Examples |
|------|------|----------|
| Integer | 🔢 | Width, Height, OffsetX, OffsetY |
| Float | 📊 | ExposureTime, Gain, AcquisitionFrameRate |
| String | 📝 | DeviceVendorName, DeviceModelName |
| Boolean | ☑️ | AcquisitionFrameRateEnable, ReverseX |
| Command | ▶️ | AcquisitionStart, AcquisitionStop, TriggerSoftware |
| Enumeration | 📋 | PixelFormat, TriggerMode, ExposureAuto |
| Category | 📁 | DeviceControl, ImageFormatControl |

## Access Modes

| Mode | Meaning |
|------|---------|
| `ReadWrite` | Can read and write |
| `ReadOnly` | Can only read (e.g. DeviceVendorName, temperature) |
| `WriteOnly` | Can only write (rare) |
| `NotAvailable` | Depends on other settings (e.g. Width while streaming) |
| `NotImplemented` | Camera does not support this feature |

## Visibility Levels

| Level | Audience |
|-------|----------|
| `Beginner` | Basic features for all users |
| `Expert` | Advanced features |
| `Guru` | Low-level / expert-only features |
| `Invisible` | Internal, hidden from UI |

---

## Typical GenICam Category Tree

```
📁 DeviceControl
   DeviceVendorName (String, RO)
   DeviceModelName (String, RO)
   DeviceSerialNumber (String, RO)
   DeviceFirmwareVersion (String, RO)
   DeviceTemperature (Float, RO)

📁 ImageFormatControl
   Width (Integer, RW)        [64 – 720, step 4]
   Height (Integer, RW)       [64 – 542, step 2]
   OffsetX (Integer, RW)
   OffsetY (Integer, RW)
   PixelFormat (Enum, RW)     [Mono8, Mono10, Mono12, …]

📁 AcquisitionControl
   AcquisitionMode (Enum, RW) [Continuous, SingleFrame]
   AcquisitionStart (Command)
   AcquisitionStop (Command)
   AcquisitionFrameRate (Float, RW)

📁 AnalogControl
   ExposureTime (Float, RW)   [28.0 – 1000000.0 µs]
   ExposureAuto (Enum, RW)    [Off, Once, Continuous]
   Gain (Float, RW)
   GainAuto (Enum, RW)

📁 DigitalIOControl
   TriggerMode (Enum, RW)     [Off, On]
   TriggerSource (Enum, RW)   [Software, Line1, …]
   TriggerActivation (Enum)   [RisingEdge, FallingEdge]

📁 TransportLayerControl
   PayloadSize (Integer, RO)
```

---

## AravisNative vs AravisGenerated

| Aspect | AravisNative (hand-crafted) | AravisGenerated (auto) |
|--------|-----------------------------|------------------------|
| Functions | ~80 | 475 |
| Error handling | ✅ Correct `GError**` parameters | ⚠️ Some lost in generation |
| Type safety | ✅ Proper C# types | ⚠️ Some generic `IntPtr` |
| Documentation | ✅ XML docs | ❌ None |
| Use for | Production code | API discovery, advanced/rare calls |

**Recommendation**: Use `AravisNative` for day-to-day work. Consult `AravisGenerated` when you need a function not covered by the hand-crafted bindings, then promote it to `AravisNative` if you use it regularly.

---

## Interactive Browser (Menu Option 5)

The built-in interactive browser offers:

1. **Browse by category** — tree view with icons
2. **List all features** — flat alphabetical list with stats
3. **Search** — case-insensitive partial match on name / display name
4. **Feature details** — full metadata, constraints, and choices
5. **Modify value** — interactive value entry with validation

---

## FeatureDetails API Reference

```csharp
public class FeatureDetails
{
    // Identity
    string Name;
    string DisplayName;
    string Description;
    string Tooltip;

    // Classification
    FeatureType Type;
    FeatureAccessMode AccessMode;
    FeatureVisibility Visibility;

    // State
    bool IsAvailable;
    bool IsImplemented;
    bool IsLocked;
    string? CurrentValue;

    // Integer constraints
    long? IntMin, IntMax, IntIncrement;

    // Float constraints
    double? FloatMin, FloatMax, FloatIncrement;

    // Enumeration
    List<string> EnumChoices;
    List<string> EnumDisplayNames;
}
```
