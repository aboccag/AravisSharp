namespace AravisSharp.Tests;

/// <summary>
/// The record-and-restore of camera settings that protects a real camera from the suite
/// (issue #21), exercised on the fake camera through <see cref="CameraSettingsSnapshot"/>.
/// The fake camera itself is never restored at the end of a run: these tests call the
/// snapshot directly.
/// </summary>
[Collection("CameraAcquisition")]
public class CameraSettingsSnapshotTests
{
    [FakeCameraFact]
    public void Restore_ShouldWriteBackWhatChanged_AndOnlyThat()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        var snapshot = CameraSettingsSnapshot.Capture(nodeMap);
        long width = nodeMap.GetIntegerFeature("Width");
        long height = nodeMap.GetIntegerFeature("Height");
        try
        {
            nodeMap.SetIntegerFeature("Width", width / 2);

            var result = snapshot.Restore(nodeMap);

            Assert.Equal(new[] { "Width" }, result.Restored);
            Assert.Empty(result.Failed);
            Assert.Equal(width, nodeMap.GetIntegerFeature("Width"));
            Assert.Equal(height, nodeMap.GetIntegerFeature("Height"));
        }
        finally
        {
            nodeMap.SetIntegerFeature("Width", width);
        }
    }

    [FakeCameraFact]
    public void Restore_NothingChanged_ShouldWriteNothing()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var snapshot = CameraSettingsSnapshot.Capture(device.NodeMap);

        var result = snapshot.Restore(device.NodeMap);

        Assert.Empty(result.Restored);
        Assert.Empty(result.Failed);
    }

    [FakeCameraFact]
    public void Capture_ShouldRecordWhatTheCameraHas_AndSkipWhatItLacks()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();

        var snapshot = CameraSettingsSnapshot.Capture(device.NodeMap);

        Assert.Contains("Width", snapshot.Keys);
        Assert.Contains("Height", snapshot.Keys);
        Assert.Contains("PixelFormat", snapshot.Keys);
        Assert.Contains("AcquisitionMode", snapshot.Keys);
        // The USB3 bandwidth limit does not exist on the fake camera.
        Assert.DoesNotContain("DeviceLinkThroughputLimit", snapshot.Keys);
        Assert.DoesNotContain("DeviceLinkThroughputLimitMode", snapshot.Keys);
    }

    [FakeCameraFact]
    public void Capture_ShouldLeaveTheTriggerSelectorWhereItWas()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        string? selector = nodeMap.GetStringFeature("TriggerSelector");

        CameraSettingsSnapshot.Capture(nodeMap);

        Assert.Equal(selector, nodeMap.GetStringFeature("TriggerSelector"));
    }

    [FakeCameraFact]
    public void Restore_ShouldBringBackATriggerClearedByTheSuite_ForItsOwnSelector()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        try
        {
            camera.SetTrigger("Software"); // FrameStart: TriggerMode On, TriggerSource Software
            string? selector = nodeMap.GetStringFeature("TriggerSelector");
            var snapshot = CameraSettingsSnapshot.Capture(nodeMap);
            Assert.Contains($"TriggerMode[{selector}]", snapshot.Keys);

            camera.ClearTriggers(); // what CameraTestHelpers.ConfigureForAcquisition does
            Assert.Equal("Off", nodeMap.GetStringFeature("TriggerMode"));

            var result = snapshot.Restore(nodeMap);

            Assert.Empty(result.Failed);
            Assert.Contains($"TriggerMode[{selector}]", result.Restored);
            Assert.Equal(selector, nodeMap.GetStringFeature("TriggerSelector"));
            Assert.Equal("On", nodeMap.GetStringFeature("TriggerMode"));
            Assert.Equal("Software", nodeMap.GetStringFeature("TriggerSource"));
        }
        finally
        {
            camera.ClearTriggers();
        }
    }

    [FakeCameraFact]
    public void Restore_AValueTheCameraRefuses_ShouldReportItAndRestoreTheRest()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        long width = nodeMap.GetIntegerFeature("Width");
        long height = nodeMap.GetIntegerFeature("Height");
        // A snapshot holding a Height that cannot be written (the fake camera accepts any
        // number, so the recorded value is not one), next to a Width that can be restored.
        var snapshot = CameraSettingsSnapshot.Capture(nodeMap).With("Height", "not a number");
        try
        {
            nodeMap.SetIntegerFeature("Width", width / 2);

            var result = snapshot.Restore(nodeMap);

            Assert.Contains("Width", result.Restored);
            Assert.Equal(width, nodeMap.GetIntegerFeature("Width"));
            var failure = Assert.Single(result.Failed);
            Assert.StartsWith("Height", failure);
        }
        finally
        {
            nodeMap.SetIntegerFeature("Width", width);
            nodeMap.SetIntegerFeature("Height", height);
        }
    }
}

/// <summary>
/// <see cref="CameraTestHelpers.FindDevice"/> (issue #21): a discovery that misses the
/// requested device once must not fail a test. Pure: the discovery is a delegate.
/// </summary>
public class DeviceLookupTests
{
    private static readonly CameraInfo Wanted = new() { DeviceId = "Vendor-Model-123", Protocol = "GigEVision" };
    private static readonly CameraInfo Other = new() { DeviceId = "Vendor-Model-999", Protocol = "GigEVision" };

    [Fact]
    public void FindDevice_SeenAtFirstDiscovery_ShouldDiscoverOnce()
    {
        int calls = 0;

        var found = CameraTestHelpers.FindDevice(Wanted.DeviceId, () => { calls++; return new() { Other, Wanted }; }, out _);

        Assert.Same(Wanted, found);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FindDevice_MissedOnce_ShouldDiscoverAgainAndFindIt()
    {
        int calls = 0;

        var found = CameraTestHelpers.FindDevice(
            Wanted.DeviceId,
            () => ++calls == 1 ? new List<CameraInfo> { Other } : new List<CameraInfo> { Other, Wanted },
            out _);

        Assert.Same(Wanted, found);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void FindDevice_Absent_ShouldGiveUpAfterTheAttempts_AndReturnWhatWasVisible()
    {
        int calls = 0;

        var found = CameraTestHelpers.FindDevice(Wanted.DeviceId, () => { calls++; return new() { Other }; }, out var visible);

        Assert.Null(found);
        Assert.Equal(CameraTestHelpers.DiscoveryAttempts, calls);
        Assert.Equal(new[] { Other.DeviceId }, visible.Select(c => c.DeviceId));
    }
}
