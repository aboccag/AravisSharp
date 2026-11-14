using System.Reflection;

namespace AravisSharp.Tests;

/// <summary>
/// Argument validation of the public <see cref="Camera"/> members that take a feature name:
/// a null name is rejected in managed code, before any native call (issue #12).
/// </summary>
public class CameraArgumentTests : IClassFixture<CameraArgumentTests.SharedCamera>
{
    /// <summary>
    /// One camera for the whole class: a null name is rejected before any native call, so the
    /// rows need no fresh camera. Opened lazily, so a class whose rows are all skipped (no native
    /// Aravis) never opens one.
    /// </summary>
    public sealed class SharedCamera : IDisposable
    {
        private readonly Lazy<Camera> _camera = new(CameraTestHelpers.OpenTestCamera);

        public Camera Camera => _camera.Value;

        public void Dispose()
        {
            if (_camera.IsValueCreated)
                _camera.Value.Dispose();
        }
    }

    private readonly SharedCamera _shared;

    public CameraArgumentTests(SharedCamera shared)
    {
        _shared = shared;
    }

    // Every public Camera member that takes a feature name, called with a null name. The
    // other arguments are valid values, so only the feature name can be rejected.
    internal static readonly IReadOnlyDictionary<string, Action<Camera>> NullFeatureCalls =
        new Dictionary<string, Action<Camera>>
        {
            [nameof(Camera.ExecuteCommand)] = c => c.ExecuteCommand(null!),
            [nameof(Camera.GetStringFeature)] = c => c.GetStringFeature(null!),
            [nameof(Camera.SetStringFeature)] = c => c.SetStringFeature(null!, "value"),
            [nameof(Camera.GetIntegerFeature)] = c => c.GetIntegerFeature(null!),
            [nameof(Camera.SetIntegerFeature)] = c => c.SetIntegerFeature(null!, 1),
            [nameof(Camera.GetFloatFeature)] = c => c.GetFloatFeature(null!),
            [nameof(Camera.SetFloatFeature)] = c => c.SetFloatFeature(null!, 1.0),
            [nameof(Camera.GetBooleanFeature)] = c => c.GetBooleanFeature(null!),
            [nameof(Camera.SetBooleanFeature)] = c => c.SetBooleanFeature(null!, true),
            [nameof(Camera.GetIntegerFeatureBounds)] = c => c.GetIntegerFeatureBounds(null!),
            [nameof(Camera.GetFloatFeatureBounds)] = c => c.GetFloatFeatureBounds(null!),
            [nameof(Camera.GetIntegerFeatureIncrement)] = c => c.GetIntegerFeatureIncrement(null!),
            [nameof(Camera.GetFloatFeatureIncrement)] = c => c.GetFloatFeatureIncrement(null!),
            [nameof(Camera.IsFeatureAvailable)] = c => c.IsFeatureAvailable(null!),
            [nameof(Camera.GetAvailableEnumerationValues)] = c => c.GetAvailableEnumerationValues(null!),
            [nameof(Camera.IsFeatureImplemented)] = c => c.IsFeatureImplemented(null!),
        };

    public static TheoryData<string> FeatureNameMembers()
    {
        var data = new TheoryData<string>();
        foreach (var name in NullFeatureCalls.Keys)
            data.Add(name);
        return data;
    }

    [NativeTheory]
    [MemberData(nameof(FeatureNameMembers))]
    public void NullFeatureName_ThrowsArgumentNullException(string member)
    {
        var ex = Assert.Throws<ArgumentNullException>(() => NullFeatureCalls[member](_shared.Camera));
        Assert.Equal("feature", ex.ParamName);
    }
}

/// <summary>
/// Keeps <see cref="CameraArgumentTests"/> exhaustive. Reflection only: no camera, no native call.
/// </summary>
public class CameraArgumentCoverageTests
{
    [Fact]
    public void NullFeatureCalls_CoverEveryPublicCameraMemberTakingAFeatureName()
    {
        // One entry per method, not per name: a second overload taking a feature name repeats
        // its name here and no longer matches the dictionary, whose keys are unique.
        var members = typeof(Camera)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetParameters().Any(p => p.Name == "feature" && p.ParameterType == typeof(string)))
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        Assert.Equal(CameraArgumentTests.NullFeatureCalls.Keys.OrderBy(n => n, StringComparer.Ordinal), members);
    }
}
