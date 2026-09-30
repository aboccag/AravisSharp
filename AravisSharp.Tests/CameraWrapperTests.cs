using System;
using System.Linq;
using AravisSharp;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Comprehensive unit tests for Camera wrapper methods
/// </summary>
public class CameraWrapperTests : IDisposable
{
    private readonly Camera _camera;

    // xunit creates one instance per test: every test gets a freshly opened camera (the
    // fake camera comes back with its defaults). Open failures propagate and fail the test.
    public CameraWrapperTests()
    {
        _camera = CameraTestHelpers.OpenTestCamera();
    }

    public void Dispose()
    {
        _camera.Dispose();
    }

    /// <summary>
    /// Returns whether the camera implements an optional feature. The fake camera is known
    /// to implement the features this is used for, so there a missing feature is a failure;
    /// on a real camera it lets the test return early.
    /// </summary>
    private static bool Supports(bool available, string feature)
    {
        if (!available && CameraTestHelpers.UsesFakeCamera)
            Assert.Fail($"The fake camera should implement {feature}.");
        return available;
    }

    private string? FindBooleanFeature() =>
        new[] { "ReverseX", "TestBoolean" }.FirstOrDefault(_camera.IsFeatureAvailable);

    #region Sensor Size Tests

    [NativeFact]
    public void GetSensorSize_ShouldReturnPositiveDimensions()
    {
        // Act
        var (width, height) = _camera.GetSensorSize();

        // Assert
        Assert.True(width > 0);
        Assert.True(height > 0);
    }

    #endregion

    #region Acquisition Mode Tests

    [NativeFact]
    public void GetAcquisitionMode_ShouldReturnValidMode()
    {
        // Act
        var mode = _camera.GetAcquisitionMode();

        // Assert
        Assert.True(Enum.IsDefined(typeof(ArvAcquisitionMode), mode));
    }

    [NativeFact]
    public void SetAcquisitionMode_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous));
        Assert.Null(exception);
    }

    [NativeFact]
    public void SetAcquisitionMode_SingleFrame_ShouldWork()
    {
        // Act
        _camera.SetAcquisitionMode(ArvAcquisitionMode.SingleFrame);
        var mode = _camera.GetAcquisitionMode();

        // Assert
        Assert.Equal(ArvAcquisitionMode.SingleFrame, mode);
    }

    [NativeFact]
    public void SetAcquisitionMode_Continuous_ShouldWork()
    {
        // Act
        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        var mode = _camera.GetAcquisitionMode();

        // Assert
        Assert.Equal(ArvAcquisitionMode.Continuous, mode);
    }

    #endregion

    #region Frame Count Tests

    [RealCameraFact("AcquisitionFrameCount")]
    public void GetFrameCount_ShouldReturnNonNegative()
    {
        // Not every real camera has it either (xunit v2 cannot skip at run time).
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Act
        var count = _camera.GetFrameCount();

        // Assert
        Assert.True(count >= 0);
    }

    [RealCameraFact("AcquisitionFrameCount")]
    public void SetFrameCount_ShouldNotThrow()
    {
        // Not every real camera has it either (xunit v2 cannot skip at run time).
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Act & Assert
        var exception = Record.Exception(() => _camera.SetFrameCount(10));
        Assert.Null(exception);
    }

    [RealCameraFact("AcquisitionFrameCount")]
    public void GetFrameCountBounds_ShouldReturnValidRange()
    {
        // Not every real camera has it either (xunit v2 cannot skip at run time).
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Act
        var (min, max) = _camera.GetFrameCountBounds();

        // Assert
        Assert.True(min >= 0);
        Assert.True(max >= min);
    }

    [RealCameraFact("AcquisitionFrameCount")]
    public void SetFrameCount_WithinBounds_ShouldWork()
    {
        // Not every real camera has it either (xunit v2 cannot skip at run time).
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Arrange
        var (min, max) = _camera.GetFrameCountBounds();
        long targetCount = min + (max - min) / 2;

        // Act
        _camera.SetFrameCount(targetCount);
        var actualCount = _camera.GetFrameCount();

        // Assert
        Assert.Equal(targetCount, actualCount);
    }

    #endregion

    #region Auto Exposure Tests

    [RealCameraFact("ExposureAuto")]
    public void GetExposureTimeAuto_ShouldReturnValidMode()
    {
        if (!_camera.IsExposureAutoAvailable()) return;

        // Act
        var mode = _camera.GetExposureTimeAuto();

        // Assert
        Assert.True(Enum.IsDefined(typeof(ArvAuto), mode));
    }

    [RealCameraFact("ExposureAuto")]
    public void SetExposureTimeAuto_Off_ShouldWork()
    {
        if (!_camera.IsExposureAutoAvailable()) return;

        // Act
        _camera.SetExposureTimeAuto(ArvAuto.Off);
        var mode = _camera.GetExposureTimeAuto();

        // Assert
        Assert.Equal(ArvAuto.Off, mode);
    }

    [RealCameraFact("ExposureAuto")]
    public void SetExposureTimeAuto_Continuous_ShouldWork()
    {
        if (!_camera.IsExposureAutoAvailable()) return;

        // Act
        ArvAuto mode;
        try
        {
            _camera.SetExposureTimeAuto(ArvAuto.Continuous);
            mode = _camera.GetExposureTimeAuto();
        }
        finally
        {
            CameraTestHelpers.TryDisableExposureAuto(_camera);
        }

        // Assert
        Assert.Equal(ArvAuto.Continuous, mode);
    }

    #endregion

    #region Auto Gain Tests

    [NativeFact]
    public void GetGainAuto_ShouldReturnValidMode()
    {
        if (!Supports(_camera.IsGainAutoAvailable(), "GainAuto")) return;

        // Act
        var mode = _camera.GetGainAuto();

        // Assert
        Assert.True(Enum.IsDefined(typeof(ArvAuto), mode));
    }

    [NativeFact]
    public void SetGainAuto_Off_ShouldWork()
    {
        if (!Supports(_camera.IsGainAutoAvailable(), "GainAuto")) return;

        var original = _camera.GetGainAuto();
        ArvAuto mode;
        try
        {
            // Act
            _camera.SetGainAuto(ArvAuto.Off);
            mode = _camera.GetGainAuto();
        }
        finally
        {
            _camera.SetGainAuto(original);
        }

        // Assert
        Assert.Equal(ArvAuto.Off, mode);
    }

    [NativeFact]
    public void SetGainAuto_Continuous_ShouldWork()
    {
        if (!Supports(_camera.IsGainAutoAvailable(), "GainAuto")) return;

        var original = _camera.GetGainAuto();
        ArvAuto mode;
        try
        {
            // Act
            _camera.SetGainAuto(ArvAuto.Continuous);
            mode = _camera.GetGainAuto();
        }
        finally
        {
            _camera.SetGainAuto(original);
        }

        // Assert
        Assert.Equal(ArvAuto.Continuous, mode);
    }

    #endregion

    #region Frame Rate Tests

    // Note: GetFrameRateEnable/SetFrameRateEnable are not available in Aravis 0.8
    // These would be tested if available in newer versions

    #endregion

    #region Generic Feature Access Tests

    [NativeFact]
    public void GetStringFeature_PixelFormat_ShouldReturnNonEmpty()
    {
        // Act
        var pixelFormat = _camera.GetStringFeature("PixelFormat");

        // Assert
        Assert.NotNull(pixelFormat);
        Assert.NotEmpty(pixelFormat);
    }

    [NativeFact]
    public void SetStringFeature_PixelFormat_ShouldWork()
    {
        // Arrange
        var originalFormat = _camera.GetPixelFormat();

        // Act & Assert - should not throw
        var exception = Record.Exception(() => _camera.SetStringFeature("PixelFormat", originalFormat));
        Assert.Null(exception);
    }

    [NativeFact]
    public void GetIntegerFeature_Width_ShouldReturnPositive()
    {
        // Act
        var width = _camera.GetIntegerFeature("Width");

        // Assert
        Assert.True(width > 0);
    }

    [NativeFact]
    public void SetIntegerFeature_Width_ShouldWork()
    {
        // Arrange
        var (minWidth, maxWidth) = _camera.GetWidthBounds();

        // Act
        _camera.SetIntegerFeature("Width", maxWidth);
        var width = _camera.GetIntegerFeature("Width");

        // Assert
        Assert.Equal(maxWidth, width);
    }

    [NativeFact]
    public void GetFloatFeature_ExposureTime_ShouldReturnPositive()
    {
        var feature = CameraTestHelpers.ResolveExposureTimeFeature(_camera);
        if (!Supports(feature != null, "an exposure time feature")) return;

        // Act
        var exposure = _camera.GetFloatFeature(feature!);

        // Assert
        Assert.True(exposure > 0);
    }

    [NativeFact]
    public void SetFloatFeature_ExposureTime_ShouldWork()
    {
        var feature = CameraTestHelpers.ResolveExposureTimeFeature(_camera);
        if (!Supports(feature != null, "an exposure time feature")) return;
        if (!CameraTestHelpers.TryDisableExposureAuto(_camera)) return;

        // Arrange: a mid-range exposure can be seconds on a real camera, so restore it
        var original = _camera.GetFloatFeature(feature!);
        var (min, max) = _camera.GetExposureTimeBounds();
        var target = min + (max - min) / 2;

        // Act
        double actual;
        try
        {
            _camera.SetFloatFeature(feature!, target);
            actual = _camera.GetFloatFeature(feature!);
        }
        finally
        {
            _camera.SetFloatFeature(feature!, original);
        }

        // Assert
        Assert.True(Math.Abs(actual - target) < 1.0); // Allow small tolerance
    }

    [NativeFact]
    public void GetBooleanFeature_ShouldReturnBool()
    {
        // ReverseX on real cameras, TestBoolean on the fake camera
        var feature = FindBooleanFeature();
        if (!Supports(feature != null, "a boolean feature")) return;

        // Act & Assert - reading must not throw
        var exception = Record.Exception(() => _camera.GetBooleanFeature(feature!));
        Assert.Null(exception);
    }

    [NativeFact]
    public void SetBooleanFeature_ShouldWork()
    {
        var feature = FindBooleanFeature();
        if (!Supports(feature != null, "a boolean feature")) return;

        var original = _camera.GetBooleanFeature(feature!);
        bool toggled;
        try
        {
            // Act
            _camera.SetBooleanFeature(feature!, !original);
            toggled = _camera.GetBooleanFeature(feature!);
        }
        finally
        {
            _camera.SetBooleanFeature(feature!, original);
        }

        // Assert
        Assert.Equal(!original, toggled);
        Assert.Equal(original, _camera.GetBooleanFeature(feature!));
    }

    #endregion

    #region Feature Bounds Tests

    [NativeFact]
    public void GetIntegerFeatureBounds_Width_ShouldReturnValidRange()
    {
        // Act
        var (min, max) = _camera.GetIntegerFeatureBounds("Width");

        // Assert
        Assert.True(min > 0);
        Assert.True(max >= min);
    }

    [NativeFact]
    public void GetFloatFeatureBounds_ExposureTime_ShouldReturnValidRange()
    {
        var feature = CameraTestHelpers.ResolveExposureTimeFeature(_camera);
        if (!Supports(feature != null, "an exposure time feature")) return;

        // Act
        var (min, max) = _camera.GetFloatFeatureBounds(feature!);

        // Assert
        Assert.True(min > 0);
        Assert.True(max >= min);
    }

    #endregion

    #region Feature Increment Tests

    [NativeFact]
    public void GetWidthIncrement_ShouldReturnPositive()
    {
        // Act
        var increment = _camera.GetWidthIncrement();

        // Assert
        Assert.True(increment > 0);
    }

    [NativeFact]
    public void GetHeightIncrement_ShouldReturnPositive()
    {
        // Act
        var increment = _camera.GetHeightIncrement();

        // Assert
        Assert.True(increment > 0);
    }

    [NativeFact]
    public void GetIntegerFeatureIncrement_Width_ShouldReturnPositive()
    {
        // Act
        var increment = _camera.GetIntegerFeatureIncrement("Width");

        // Assert
        Assert.True(increment > 0);
    }

    [NativeFact]
    public void GetFloatFeatureIncrement_ExposureTime_ShouldReturnPositive()
    {
        var feature = CameraTestHelpers.ResolveExposureTimeFeature(_camera);
        if (!Supports(feature != null, "an exposure time feature")) return;

        // Act
        double increment;
        try
        {
            increment = _camera.GetFloatFeatureIncrement(feature!);
        }
        catch (AravisException) when (!CameraTestHelpers.UsesFakeCamera)
        {
            // Some descriptions give no increment at all (Opto ITA24: "<Inc> node not found").
            return;
        }

        // Assert
        Assert.True(increment >= 0); // Can be 0 if continuous
    }

    #endregion

    #region Feature Availability Tests

    [NativeFact]
    public void IsFeatureAvailable_Width_ShouldReturnTrue()
    {
        // Act
        var available = _camera.IsFeatureAvailable("Width");

        // Assert
        Assert.True(available);
    }

    [NativeFact]
    public void IsFeatureAvailable_NonExistent_ShouldReturnFalse()
    {
        // Act
        var available = _camera.IsFeatureAvailable("ThisFeatureDoesNotExist12345");

        // Assert
        Assert.False(available);
    }

    [NativeFact]
    public void IsBinningAvailable_ShouldReturnBool()
    {
        // Act
        var available = _camera.IsBinningAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [NativeFact]
    public void IsExposureTimeAvailable_ShouldReturnBool()
    {
        // Act
        var available = _camera.IsExposureTimeAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [NativeFact]
    public void IsExposureAutoAvailable_ShouldReturnBool()
    {
        // Act
        var available = _camera.IsExposureAutoAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [NativeFact]
    public void IsGainAvailable_ShouldReturnBool()
    {
        // Act
        var available = _camera.IsGainAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [NativeFact]
    public void IsGainAutoAvailable_ShouldReturnBool()
    {
        // Act
        var available = _camera.IsGainAutoAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [NativeFact]
    public void IsFrameRateAvailable_ShouldReturnBool()
    {
        // Act
        var available = _camera.IsFrameRateAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    #endregion

    #region Device Type Tests

    [NativeFact]
    public void IsGigEVisionDevice_ShouldReturnBool()
    {
        // Act
        var isGigE = _camera.IsGigEVisionDevice();

        // Assert - just check it doesn't throw
        Assert.True(isGigE == true || isGigE == false);
    }

    [NativeFact]
    public void IsUSB3VisionDevice_ShouldReturnBool()
    {
        // Act
        var isUSB = _camera.IsUSB3VisionDevice();

        // Assert - just check it doesn't throw
        Assert.True(isUSB == true || isUSB == false);
    }

    [NativeFact]
    public void DeviceType_ShouldBeEitherGigEOrUSB()
    {
        // Act
        var isGigE = _camera.IsGigEVisionDevice();
        var isUSB = _camera.IsUSB3VisionDevice();

        // Assert - should be one or the other (or potentially neither for other protocols)
        // Just verify both don't return true
        Assert.False(isGigE && isUSB);
    }

    #endregion

    #region GigE Vision Specific Tests

    [GigECameraFact]
    public void GvAutoPacketSize_OnGigECamera_ShouldNotThrow()
    {
        Assert.True(_camera.IsGigEVisionDevice());

        // Act & Assert
        var exception = Record.Exception(() => _camera.GvAutoPacketSize());
        Assert.Null(exception);
    }

    [GigECameraFact]
    public void GvGetPacketSize_OnGigECamera_ShouldReturnPositive()
    {
        Assert.True(_camera.IsGigEVisionDevice());

        // Act
        var packetSize = _camera.GvGetPacketSize();

        // Assert
        Assert.True(packetSize > 0);
    }

    [GigECameraFact]
    public void GvSetPacketSize_OnGigECamera_ShouldWork()
    {
        Assert.True(_camera.IsGigEVisionDevice());

        // Arrange
        var originalSize = _camera.GvGetPacketSize();

        int newSize;
        try
        {
            // Act
            _camera.GvSetPacketSize(1500);
            newSize = _camera.GvGetPacketSize();
        }
        finally
        {
            _camera.GvSetPacketSize(originalSize);
        }

        // Assert: cameras round down to their packet size increment (the Opto ITA24 gives 1496)
        Assert.InRange(newSize, 1500 - 64, 1500);
    }

    #endregion

    #region USB3 Vision Specific Tests

    [Usb3CameraFact]
    public void UvIsBandwidthControlAvailable_OnUSBCamera_ShouldReturnBool()
    {
        Assert.True(_camera.IsUSB3VisionDevice());

        // Act
        var available = _camera.UvIsBandwidthControlAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Usb3CameraFact]
    public void UvGetBandwidth_OnUSBCameraWithControl_ShouldBeZeroOrWithinBounds()
    {
        Assert.True(_camera.IsUSB3VisionDevice());
        // Bandwidth control is optional in USB3 Vision (xunit v2 cannot skip at run time).
        if (!_camera.UvIsBandwidthControlAvailable()) return;

        // Act
        var bandwidth = _camera.UvGetBandwidth();
        var (min, max) = _camera.UvGetBandwidthBounds();

        // Assert - 0 means bandwidth limiting is disabled
        Assert.True(bandwidth == 0 || (bandwidth >= min && bandwidth <= max),
            $"Bandwidth {bandwidth} outside [{min}, {max}]");
    }

    [Usb3CameraFact]
    public void UvGetBandwidthBounds_OnUSBCameraWithControl_ShouldReturnValidRange()
    {
        Assert.True(_camera.IsUSB3VisionDevice());
        // Bandwidth control is optional in USB3 Vision (xunit v2 cannot skip at run time).
        if (!_camera.UvIsBandwidthControlAvailable()) return;

        // Act
        var (min, max) = _camera.UvGetBandwidthBounds();

        // Assert
        Assert.True(max >= min);
    }

    [Usb3CameraFact]
    public void UvSetBandwidth_OnUSBCameraWithControl_ShouldWork()
    {
        Assert.True(_camera.IsUSB3VisionDevice());
        // Bandwidth control is optional in USB3 Vision (xunit v2 cannot skip at run time).
        if (!_camera.UvIsBandwidthControlAvailable()) return;

        // Arrange
        var originalBandwidth = _camera.UvGetBandwidth();
        var (min, max) = _camera.UvGetBandwidthBounds();
        var targetBandwidth = min + (max - min) / 2;

        uint newBandwidth;
        try
        {
            // Act
            _camera.UvSetBandwidth(targetBandwidth);
            newBandwidth = _camera.UvGetBandwidth();
        }
        finally
        {
            _camera.UvSetBandwidth(originalBandwidth);
        }

        // Assert
        Assert.Equal(targetBandwidth, newBandwidth);
    }

    #endregion

    #region Command Execution Tests

    [NativeFact]
    public void ExecuteCommand_WithValidCommand_ShouldNotThrow()
    {
        if (!Supports(_camera.IsFeatureAvailable("AcquisitionStart"), "AcquisitionStart")) return;

        Exception? exception;
        try
        {
            // Act
            exception = Record.Exception(() => _camera.ExecuteCommand("AcquisitionStart"));
        }
        finally
        {
            _camera.StopAcquisition();
        }

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Integration Tests

    [NativeFact]
    public void FullWorkflow_SetupAndQuery_ShouldWork()
    {
        // Get sensor size
        var (sensorWidth, sensorHeight) = _camera.GetSensorSize();
        Assert.True(sensorWidth > 0);
        Assert.True(sensorHeight > 0);

        // Check device type
        var isGigE = _camera.IsGigEVisionDevice();
        var isUSB = _camera.IsUSB3VisionDevice();
        Assert.True(isGigE || isUSB || (!isGigE && !isUSB)); // Should be one or the other or neither

        // Set acquisition mode
        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        Assert.Equal(ArvAcquisitionMode.Continuous, _camera.GetAcquisitionMode());

        // Configure exposure if available
        if (_camera.IsExposureTimeAvailable())
        {
            if (!CameraTestHelpers.TryDisableExposureAuto(_camera)) return;

            var originalExp = _camera.GetExposureTime();
            var (minExp, maxExp) = _camera.GetExposureTimeBounds();
            var targetExp = minExp + (maxExp - minExp) / 2;
            double actualExp;
            try
            {
                _camera.SetExposureTime(targetExp);
                actualExp = _camera.GetExposureTime();
            }
            finally
            {
                // A mid-range exposure can be seconds on a real camera
                _camera.SetExposureTime(originalExp);
            }
            Assert.True(Math.Abs(actualExp - targetExp) < (maxExp - minExp) * 0.1); // Within 10%
        }

        // Configure gain if available
        if (_camera.IsGainAvailable())
        {
            var (minGain, maxGain) = _camera.GetGainBounds();
            var targetGain = minGain;
            _camera.SetGain(targetGain);
            var actualGain = _camera.GetGain();
            Assert.True(Math.Abs(actualGain - targetGain) < (maxGain - minGain) * 0.1); // Within 10%
        }

        // Check feature availability
        Assert.True(_camera.IsFeatureAvailable("Width"));
        Assert.True(_camera.IsFeatureAvailable("Height"));
        Assert.True(_camera.IsFeatureAvailable("PixelFormat"));
    }

    [NativeFact]
    public void AutoModes_EnableAndDisable_ShouldWork()
    {
        // Test auto exposure if available
        if (_camera.IsExposureAutoAvailable())
        {
            try
            {
                _camera.SetExposureTimeAuto(ArvAuto.Off);
                Assert.Equal(ArvAuto.Off, _camera.GetExposureTimeAuto());

                _camera.SetExposureTimeAuto(ArvAuto.Continuous);
                Assert.Equal(ArvAuto.Continuous, _camera.GetExposureTimeAuto());
            }
            finally
            {
                CameraTestHelpers.TryDisableExposureAuto(_camera);
            }
        }

        // Test auto gain if available
        if (Supports(_camera.IsGainAutoAvailable(), "GainAuto"))
        {
            var original = _camera.GetGainAuto();
            try
            {
                _camera.SetGainAuto(ArvAuto.Off);
                Assert.Equal(ArvAuto.Off, _camera.GetGainAuto());

                _camera.SetGainAuto(ArvAuto.Continuous);
                Assert.Equal(ArvAuto.Continuous, _camera.GetGainAuto());
            }
            finally
            {
                _camera.SetGainAuto(original);
            }
        }
    }

    #endregion
}
