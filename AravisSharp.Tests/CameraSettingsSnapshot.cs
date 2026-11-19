using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using AravisSharp.GenICam;
using Xunit.Abstractions;
using Xunit.Sdk;

// Lets RealCameraSettings.RestoreAtEndOfRun() run once the tests are over (xunit v2 has no
// assembly fixture: the assembly runner's "before finished" step is the hook the runner awaits).
[assembly: Xunit.TestFramework("AravisSharp.Tests.SettingsRestoringTestFramework", "AravisSharp.Tests")]

namespace AravisSharp.Tests;

/// <summary>
/// The values of the camera settings the suite may change (issue #21), read from a camera
/// and written back to it later. A feature the camera does not have is skipped; trigger mode
/// and source are recorded for every value of the trigger selector; an exposure or a gain
/// driven by its auto mode is not recorded, since the camera keeps changing it.
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

    // The values each auto mode drives while it is not "Off".
    private static readonly (string Auto, string[] Driven)[] AutoModes =
    {
        ("ExposureAuto", new[] { "ExposureTime", "ExposureTimeAbs", "ExposureTimeRaw" }),
        ("GainAuto", new[] { "Gain", "GainRaw" }),
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

        foreach (var (auto, driven) in AutoModes)
        {
            var mode = settings.FirstOrDefault(s => s.Name == auto);
            if (mode != null && mode.Value != "Off")
                settings.RemoveAll(s => driven.Contains(s.Name));
        }

        var selector = nodeMap.GetFeatureDetails(TriggerSelector);
        if (Read(nodeMap, TriggerSelector) is not { } original || selector == null)
        {
            foreach (string name in TriggerFeatures)
                AddIfReadable(settings, nodeMap, name, selector: null);
            return new CameraSettingsSnapshot(settings);
        }

        // Reading a trigger setting of another selector means selecting it. The selector is
        // put back before returning, whatever happens; that write never throws.
        try
        {
            foreach (string choice in selector.EnumChoices.DefaultIfEmpty(original.Value))
            {
                if (!TryWrite(nodeMap, original with { Value = choice }, out _))
                    continue; // a selector value this camera does not accept
                foreach (string name in TriggerFeatures)
                    AddIfReadable(settings, nodeMap, name, choice);
            }
        }
        finally
        {
            TryWrite(nodeMap, original, out _);
        }

        // Last, so that Restore ends on the recorded selector.
        settings.Add(original);
        return new CameraSettingsSnapshot(settings);
    }

    /// <summary>A copy with one recorded value replaced (tests).</summary>
    public CameraSettingsSnapshot With(string key, string value) =>
        new(_settings.Select(s => s.Key == key ? s with { Value = value } : s).ToList());

    /// <summary>
    /// Writes back every recorded value the camera no longer has, and nothing else, then
    /// reads the camera again: <see cref="RestoreResult.Restored"/> lists what was written
    /// and is back, <see cref="RestoreResult.Failed"/> what still differs. Never throws for
    /// a value the camera refuses.
    /// </summary>
    public RestoreResult Restore(NodeMap nodeMap)
    {
        var before = Capture(nodeMap).ByKey();
        // A setting that can no longer be read (made unavailable by another one) is pending
        // too: restoring the other one at the first pass may bring it back for the second.
        var pending = _settings.Where(s => !before.TryGetValue(s.Key, out var now) || !s.HasValue(now.Value)).ToList();

        var written = new HashSet<string>();
        var writeErrors = new Dictionary<string, string>();
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
                        written.Add(setting.Key);
                        writeErrors.Remove(setting.Key);
                    }
                    else
                    {
                        refused.Add(setting);
                        writeErrors[setting.Key] = error;
                    }
                }
                pending = refused;
            }
        }
        finally
        {
            // Restoring a trigger setting selects its selector: end on the recorded one.
            if (recordedSelector != null)
                TryWrite(nodeMap, recordedSelector, out _);
        }

        // A write the camera accepted can be undone by a later one (a binning that rescales
        // the width, for example): only what the camera shows now counts as restored.
        var after = Capture(nodeMap).ByKey();
        var restored = new List<string>();
        var failed = new List<string>();
        foreach (var setting in _settings)
        {
            if (after.TryGetValue(setting.Key, out var now) && setting.HasValue(now.Value))
            {
                if (written.Contains(setting.Key))
                    restored.Add(setting.Key);
                continue;
            }

            string state = now == null ? "cannot be read" : $"is \"{now.Value}\"";
            string cause = writeErrors.TryGetValue(setting.Key, out var error) ? $": {error}" : "";
            failed.Add($"{setting.Key} {state} instead of \"{setting.Value}\"{cause}");
        }

        return new RestoreResult(restored, failed);
    }

    private Dictionary<string, Setting> ByKey() => _settings.ToDictionary(s => s.Key);

    private static bool TryRestore(NodeMap nodeMap, Setting setting, [NotNullWhen(false)] out string? error)
    {
        if (setting.Selector != null &&
            !TryWrite(nodeMap, new Setting(TriggerSelector, null, FeatureType.Enumeration, setting.Selector), out error))
            return false;
        return TryWrite(nodeMap, setting, out error);
    }

    private static bool TryWrite(NodeMap nodeMap, Setting setting, [NotNullWhen(false)] out string? error)
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
                    nodeMap.SetBooleanFeature(setting.Name, bool.Parse(setting.Value));
                    break;
                default:
                    nodeMap.SetStringFeature(setting.Name, setting.Value);
                    break;
            }
            error = null;
            return true;
        }
        // NodeMap wraps the Aravis error of a refused write in an InvalidOperationException.
        catch (Exception e) when (e is AravisException or InvalidOperationException or FormatException or OverflowException)
        {
            error = $"the write was refused ({e.Message})";
            return false;
        }
    }

    private static void AddIfReadable(List<Setting> settings, NodeMap nodeMap, string name, string? selector)
    {
        if (Read(nodeMap, name) is { } setting)
            settings.Add(setting with { Selector = selector });
    }

    // Null unless the camera has the feature, with a value, of a type that can be written back.
    private static Setting? Read(NodeMap nodeMap, string name)
    {
        var details = nodeMap.GetFeatureDetails(name);
        if (details is not { IsImplemented: true, IsAvailable: true } || string.IsNullOrEmpty(details.CurrentValue))
            return null;

        switch (details.Type)
        {
            case FeatureType.Float:
                // Not details.CurrentValue: Aravis formats it with "%g", six significant
                // digits in the C locale. The round-trip format keeps the exact value.
                try
                {
                    return new Setting(name, null, FeatureType.Float,
                        nodeMap.GetFloatFeature(name).ToString("R", CultureInfo.InvariantCulture));
                }
                catch (Exception e) when (e is AravisException or InvalidOperationException)
                {
                    return null; // NodeMap wraps the Aravis error of a failed read
                }
            case FeatureType.Integer:
            case FeatureType.Boolean:
            case FeatureType.Enumeration:
            case FeatureType.String:
                return new Setting(name, null, details.Type, details.CurrentValue);
            default:
                return null;
        }
    }

    private sealed record Setting(string Name, string? Selector, FeatureType Type, string Value)
    {
        public string Key => Selector == null ? Name : $"{Name}[{Selector}]";

        /// <summary>Whether the camera's current value is this one; floats within one part in a million.</summary>
        public bool HasValue(string current)
        {
            if (Type != FeatureType.Float)
                return current == Value;

            return double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double recorded) &&
                   double.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out double now) &&
                   Math.Abs(recorded - now) <= 1e-6 * Math.Max(1.0, Math.Max(Math.Abs(recorded), Math.Abs(now)));
        }
    }
}

/// <summary>What <see cref="CameraSettingsSnapshot.Restore"/> wrote back, and what still differs afterwards.</summary>
internal sealed record RestoreResult(IReadOnlyList<string> Restored, IReadOnlyList<string> Failed);

/// <summary>
/// Protects the real camera named by ARAVIS_TEST_DEVICE_ID from the suite: its settings are
/// recorded before the first test opens it and written back when the tests are over. The
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
    /// Writes back the recorded settings that changed. Returns null when nothing was recorded
    /// (the fake camera, or no camera test ran); otherwise a report, and whether every
    /// recorded setting is back.
    /// </summary>
    public static (string Report, bool Complete)? RestoreAtEndOfRun()
    {
        lock (Gate)
        {
            if (_recorded == null || _deviceId == null)
                return null;

            using var camera = new Camera(_deviceId);
            using var device = camera.GetDevice();
            var result = _recorded.Restore(device.NodeMap);
            string report = $"{_deviceId}: restored {(result.Restored.Count == 0 ? "nothing (no setting changed)" : string.Join(", ", result.Restored))}";
            return result.Failed.Count == 0
                ? (report, true)
                : ($"{report}; NOT restored: {string.Join("; ", result.Failed)}", false);
        }
    }
}

/// <summary>
/// The xunit test framework of this assembly: the stock one, whose assembly runner calls
/// <see cref="RealCameraSettings.RestoreAtEndOfRun"/> once the tests are over. The runner
/// awaits that step before it reports the assembly finished, so the test host does not
/// exit in the middle of it. A setting that could not be restored is reported as an
/// assembly cleanup failure.
/// </summary>
public sealed class SettingsRestoringTestFramework : XunitTestFramework
{
    public SettingsRestoringTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new Executor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

    private sealed class Executor : XunitTestFrameworkExecutor
    {
        public Executor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        // The stock implementation, with the assembly runner below. xunit declares it async void.
        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using var runner = new AssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
            await runner.RunAsync();
        }
    }

    private sealed class AssemblyRunner : XunitTestAssemblyRunner
    {
        public AssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override Task BeforeTestAssemblyFinishedAsync()
        {
            // Whatever goes wrong here becomes an assembly cleanup failure in the test
            // report, never an exception out of the runner.
            Aggregator.Run(() =>
            {
                var outcome = RealCameraSettings.RestoreAtEndOfRun();
                if (outcome == null)
                    return;

                var (report, complete) = outcome.Value;
                report = "[camera settings] " + report;
                DiagnosticMessageSink.OnMessage(new DiagnosticMessage(report));
                Console.Error.WriteLine(report);
                if (!complete)
                    throw new InvalidOperationException(report);
            });
            return base.BeforeTestAssemblyFinishedAsync();
        }
    }
}
