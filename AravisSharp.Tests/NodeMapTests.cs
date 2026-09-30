using System;
using System.Linq;
using AravisSharp;
using AravisSharp.GenICam;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// GenICam node map access through <see cref="Device.NodeMap"/>, against the test camera.
/// Values checked for the fake camera come from aravis/src/arv-fake-camera.xml.
/// </summary>
public class NodeMapTests : IDisposable
{
    private readonly Camera _camera;
    private readonly Device _device;

    public NodeMapTests()
    {
        _camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            _device = _camera.GetDevice();
        }
        catch
        {
            _camera.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _device.Dispose();
        _camera.Dispose();
    }

    private NodeMap NodeMap => _device.NodeMap;

    [NativeFact]
    public void GetGenicamXml_ShouldReturnXmlDocument()
    {
        var xml = NodeMap.GetGenicamXml();

        Assert.False(string.IsNullOrWhiteSpace(xml));
        var trimmed = xml!.TrimStart('﻿', ' ', '\t', '\r', '\n');
        Assert.StartsWith("<", trimmed);
        Assert.Contains("RegisterDescription", trimmed);
    }

    [NativeFact]
    public void GetFeaturesByCategory_ShouldListCategoriesWithFeatures()
    {
        var categories = NodeMap.GetFeaturesByCategory();

        Assert.NotEmpty(categories);
        Assert.All(categories.Values, features => Assert.NotEmpty(features));
        Assert.DoesNotContain(categories.Values.SelectMany(f => f), f => f.Type == FeatureType.Category);
        Assert.Contains(categories.Values.SelectMany(f => f), f => f.Name == "Width");

        if (CameraTestHelpers.UsesFakeCamera)
        {
            Assert.Contains(categories["ImageFormatControl"], f => f.Name == "PixelFormat");
            // AnalogControl is not referenced from Root in arv-fake-camera.xml, so it is not walked.
            Assert.Contains(categories["AcquisitionControl"], f => f.Name == "AcquisitionStart");
            Assert.False(categories.ContainsKey("AnalogControl"));
        }
    }

    [NativeFact]
    public void GetAllFeatures_ShouldIncludeCoreFeatures()
    {
        var names = NodeMap.GetAllFeatures().Select(f => f.Name).ToList();

        Assert.Contains("Width", names);
        Assert.Contains("Height", names);
        Assert.Contains("PixelFormat", names);
    }

    [NativeFact]
    public void GetFeatureDetails_Width_ShouldBeIntegerWithBounds()
    {
        var details = NodeMap.GetFeatureDetails("Width");

        Assert.NotNull(details);
        Assert.Equal(FeatureType.Integer, details!.Type);
        Assert.NotNull(details.IntMin);
        Assert.NotNull(details.IntMax);
        Assert.True(details.IntMin <= details.IntMax);
        Assert.Equal(_camera.GetIntegerFeature("Width").ToString(), details.CurrentValue);

        if (CameraTestHelpers.UsesFakeCamera)
        {
            // <Min>1</Min>, <pMax>SensorWidth</pMax> = 2048
            Assert.Equal(1, details.IntMin);
            Assert.Equal(2048, details.IntMax);
        }
    }

    [NativeFact]
    public void GetFeatureDetails_PixelFormat_ShouldBeEnumerationWithChoices()
    {
        var details = NodeMap.GetFeatureDetails("PixelFormat");

        Assert.NotNull(details);
        Assert.Equal(FeatureType.Enumeration, details!.Type);
        Assert.NotEmpty(details.EnumChoices);
        Assert.Contains(_camera.GetPixelFormat(), details.EnumChoices);

        if (CameraTestHelpers.UsesFakeCamera)
        {
            Assert.Equal(
                new[] { "BayerBG8", "BayerGB8", "BayerGR8", "BayerRG8", "Mono8", "RGB8", "Mono16" }.OrderBy(c => c),
                details.EnumChoices.OrderBy(c => c));
            Assert.Equal("Mono8", details.CurrentValue);
        }
    }

    [NativeFact]
    public void GetFeatureDetails_ExposureTime_ShouldBeFloatWithBounds()
    {
        var feature = CameraTestHelpers.ResolveExposureTimeFeature(_camera);
        Assert.True(feature != null || !CameraTestHelpers.UsesFakeCamera, "The fake camera has ExposureTimeAbs.");
        if (feature == null) return;

        var details = NodeMap.GetFeatureDetails(feature);

        Assert.NotNull(details);
        Assert.Equal(FeatureType.Float, details!.Type);
        Assert.NotNull(details.FloatMin);
        Assert.NotNull(details.FloatMax);
        Assert.True(details.FloatMin <= details.FloatMax);

        if (CameraTestHelpers.UsesFakeCamera)
        {
            // ExposureTimeAbs: <Min>10.0</Min>, <Max>10000000.0</Max>
            Assert.Equal("ExposureTimeAbs", feature);
            Assert.Equal(10.0, details.FloatMin);
            Assert.Equal(10_000_000.0, details.FloatMax);
        }
    }

    [NativeFact]
    public void GetFeatureDetails_Command_ShouldBeCommand()
    {
        var details = NodeMap.GetFeatureDetails("AcquisitionStart");

        Assert.NotNull(details);
        Assert.Equal(FeatureType.Command, details!.Type);
    }

    [NativeFact]
    public void NodeMap_ShouldReadAndWriteThroughTheDevice()
    {
        var (min, max) = _camera.GetWidthBounds();
        var increment = _camera.GetWidthIncrement();
        var target = min + increment <= max ? min + increment : min;

        NodeMap.SetIntegerFeature("Width", target);

        Assert.Equal(target, NodeMap.GetIntegerFeature("Width"));
        Assert.Equal(target, _camera.GetIntegerFeature("Width"));
    }
}
