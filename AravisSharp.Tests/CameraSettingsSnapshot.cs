using System.Globalization;
using AravisSharp.GenICam;
using Xunit.Abstractions;
using Xunit.Sdk;

// Runs RealCameraSettings.RestoreAtEndOfRun() once the test run is over (xunit v2 has no
// assembly fixture: the test framework's own disposal is the hook).
[assembly: Xunit.TestFramework("AravisSharp.Tests.SettingsRestoringTestFramework", "AravisSharp.Tests")]

namespace AravisSharp.Tests;

/// <summary>
/// The values of the camera settings the suite may change (issue #21), read from a camera
/// and written back to it later. A feature the camera does not have is skipped; trigger mode
/// and source are recorded for every value of the trigger selector.
/// </summary>
internal sealed class CameraSettingsSnapshot
{
    private const string TriggerSelector = "TriggerSelector";

    // Order matters little: Restore writes in two passes, so a value refused because another
    // one is not restored yet (an offset before its width, a value locked by an auto mode)
    // is written at the second pass.
    private static readonly string[] Features =
    {
        "Width", "Height", "OffsetX", "OffsetY", "BinningHorizontal", "BinningVertical", "PixelFormat",
        "AcquisitionMode", "AcquisitionFrameCount",
        "AcquisitionFrameRate", "AcquisitionFrameRateAbs", "AcquisitionFrameRateEnable", "AcquisitionFramePeriod",
        "ExposureTime", "ExposureTimeAbs", "ExposureTimeRaw", "ExposureAuto",
        "Gain", "GainRaw", "GainAuto",
        "GevSCPSPacketSize", "GevSCPD",
        "DeviceLinkThroughputLimit", "DeviceLinkThroughputLimitMode",
    };

    // Read and written under each value of TriggerSelector.
    private static readonly string[] TriggerFeatures = { "TriggerMode", "TriggerSource" };

    private readonly IReadOnlyList<Setting> _settings;

    private CameraSettingsSnapshot(IReadOnlyList<Setting> settings) => _settings = settings;

    /// <summary>"Width", or "TriggerMode[FrameStart]" for a setting under a trigger selector.</summary>
    public IEnumerable<string> Keys => _settings.Select(s => s.Key);

    public static CameraSettingsSnapshot Capture(NodeMap nodeMap)
    {
        var settings = new List<Setting>();
        foreach (string name in Features)
            AddIfReadable(settings, nodeMap, name, selector: null);

        var selectorDetails = Read(nodeMap, TriggerSelector);
        if (selectorDetails == null)
        {
            foreach (string name in TriggerFeatures)
                AddIfReadable(settings, nodeMap, name, selector: null);
            return new CameraSettingsSnapshot(settings);
        }

        // Reading a trigger setting of another selector means selecting it: the selector is
        // put back before returning, whatever happens.
        string original = selectorDetails.CurrentValue!;
        try
        {
            foreach (string choice in selectorDetails.EnumChoices.DefaultIfEmpty(original))
            {
                if (!TryWrite(nodeMap, new Setting(TriggerSelector, null, FeatureType.Enumeration, choice), out _))
                    continue; // a selector value this camera does not accept
                foreach (string name in TriggerFeatures)
                    AddIfReadable(settings, nodeMap, name, choice);
            }
        }
        finally
        {
            nodeMap.SetStringFeature(TriggerSelector, original);
        }

        // Last, so that Restore ends on the recorded selector.
        settings.Add(new Setting(TriggerSelector, null, FeatureType.Enumeration, original));
        return new CameraSettingsSnapshot(settings);
    }

    /// <summary>A copy with one recorded value replaced (tests).</summary>
    public CameraSettingsSnapshot With(string key, string value) =>
        new(_settings.Select(s => s.Key == key ? s with { Value = value } : s).ToList());

    /// <summary>
    /// Writes back every recorded value the camera no longer has, and nothing else.
    /// A refused value is reported in <see cref="RestoreResult.Failed"/>, never thrown.
    /// </summary>
    public RestoreResult Restore(NodeMap nodeMap)
    {
        var current = Capture(nodeMap)._settings.ToDictionary(s => s.Key, s => s.Value);
        var pending = _settings
            .Where(s => current.TryGetValue(s.Key, out var now) && now != s.Value)
            .ToList();

        var restored = new List<string>();
        var failures = new Dictionary<string, string>();
        var recordedSelector = _settings.FirstOrDefault(s => s.Name == TriggerSelector);
        try
        {
            for (int pass = 0; pass < 2 && pending.Count > 0; pass++)
            {
                var refused = new List<Setting>();
                foreach (var setting in pending)
                {
                    if (TryRestore(nodeMap, setting, out string? error))
                    {
                        restored.Add(setting.Key);
                        failures.Remove(setting.Key);
                    }
                    else
                    {
                        refused.Add(setting);
                        failures[setting.Key] = error!;
                    }
                }
                pending = refused;
            }
        }
        finally
        {
            // Restoring a trigger setting selects its selector: end on the recorded one.
            if (recordedSelector != null && !TryWrite(nodeMap, recordedSelector, out string? error))
                failures[TriggerSelector] = error!;
        }

        return new RestoreResult(restored, failures.Select(f => $"{f.Key}: {f.Value}").ToList());
    }

    private static bool TryRestore(NodeMap nodeMap, Setting setting, out string? error)
    {
        if (setting.Selector != null &&
            !TryWrite(nodeMap, new Setting(TriggerSelector, null, FeatureType.Enumeration, setting.Selector), out error))
            return false;
        return TryWrite(nodeMap, setting, out error);
    }

    private static bool TryWrite(NodeMap nodeMap, Setting setting, out string? error)
    {
        try
        {
            switch (setting.Type)
            {
                case FeatureType.Integer:
                    nodeMap.SetIntegerFeature(setting.Name, long.Parse(setting.Value, CultureInfo.InvariantCulture));
                    break;
                case FeatureType.Float:
                    nodeMap.SetFloatFeature(setting.Name, double.Parse(setting.Value, CultureInfo.InvariantCulture));
                    break;
                case FeatureType.Boolean:
                    nodeMap.SetBooleanFeature(setting.Name, setting.Value is "1" || bool.Parse(setting.Value));
                    break;
                default:
                    nodeMap.SetStringFeature(setting.Name, setting.Value);
                    break;
            }
            error = null;
            return true;
        }
        catch (Exception e) when (e is AravisException or FormatException or OverflowException)
        {
            error = $"could not write \"{setting.Value}\" ({e.Message})";
            return false;
        }
    }

    private static void AddIfReadable(List<Setting> settings, NodeMap nodeMap, string name, string? selector)
    {
        var details = Read(nodeMap, name);
        if (details != null)
            settings.Add(new Setting(name, selector, details.Type, details.CurrentValue!));
    }

    // Null unless the camera has the feature, with a value, of a type that can be written back.
    private static FeatureDetails? Read(NodeMap nodeMap, string name)
    {
        var details = nodeMap.GetFeatureDetails(name);
        bool usable = details is { IsImplemented: true, IsAvailable: true } &&
                      !string.IsNullOrEmpty(details.CurrentValue) &&
                      details.Type is FeatureType.Integer or FeatureType.Float or FeatureType.Boolean
                          or FeatureType.Enumeration or FeatureType.String;
        return usable ? details : null;
    }

    private sealed record Setting(string Name, string? Selector, FeatureType Type, string Value)
    {
        public string Key => Selector == null ? Name : $"{Name}[{Selector}]";
    }
}

/// <summary>What <see cref="CameraSettingsSnapshot.Restore"/> wrote back, and what the camera refused.</summary>
internal sealed record RestoreResult(IReadOnlyList<string> Restored, IReadOnlyList<string> Failed);

/// <summary>
/// Protects the real camera named by ARAVIS_TEST_DEVICE_ID from the suite: its settings are
/// recorded before the first test opens it and written back when the test run ends. The
/// fake camera is never recorded. Not restored when the test host is killed (a hang, a crash).
/// </summary>
internal static class RealCameraSettings
{
    private static readonly object Gate = new();
    private static string? _deviceId;
    private static CameraSettingsSnapshot? _recorded;

    /// <summary>Records the settings of the device, the first time it is called.</summary>
    public static void RecordOnce(string deviceId)
    {
        lock (Gate)
        {
            if (_recorded != null)
                return;

            using var camera = new Camera(deviceId);
            using var device = camera.GetDevice();
            _recorded = CameraSettingsSnapshot.Capture(device.NodeMap);
            _deviceId = deviceId;
        }
    }

    /// <summary>
    /// Writes back the recorded settings that changed. Returns a one-line report, or null
    /// when nothing was recorded (the fake camera, or no camera test ran).
    /// </summary>
    public static string? RestoreAtEndOfRun()
    {
        lock (Gate)
        {
            if (_recorded == null || _deviceId == null)
                return null;

            using var camera = new Camera(_deviceId);
            using var device = camera.GetDevice();
            var result = _recorded.Restore(device.NodeMap);
            string report = $"{_deviceId}: restored {(result.Restored.Count == 0 ? "nothing (no setting changed)" : string.Join(", ", result.Restored))}";
            return result.Failed.Count == 0 ? report : $"{report}; NOT restored: {string.Join("; ", result.Failed)}";
        }
    }
}

/// <summary>
/// The xunit test framework of this assembly: the stock one, plus
/// <see cref="RealCameraSettings.RestoreAtEndOfRun"/> when the run ends.
/// </summary>
public sealed class SettingsRestoringTestFramework : XunitTestFramework
{
    public SettingsRestoringTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
        DisposalTracker.Add(new RestoreWhenDisposed(messageSink));
    }

    private sealed class RestoreWhenDisposed : IDisposable
    {
        private readonly IMessageSink _messageSink;

        public RestoreWhenDisposed(IMessageSink messageSink) => _messageSink = messageSink;

        public void Dispose()
        {
            string? report;
            try
            {
                report = RealCameraSettings.RestoreAtEndOfRun();
            }
            // The end of a test run has nowhere to throw to: report every failure instead.
            catch (Exception e)
            {
                report = $"camera settings NOT restored: {e.GetType().Name}: {e.Message}";
            }

            if (report == null)
                return;
            _messageSink.OnMessage(new DiagnosticMessage("[camera settings] " + report));
            Console.Error.WriteLine("[camera settings] " + report);
        }
    }
}
