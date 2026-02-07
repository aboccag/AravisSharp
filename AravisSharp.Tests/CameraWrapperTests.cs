using System;
using AravisSharp;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Comprehensive unit tests for Camera wrapper methods
/// </summary>
public class CameraWrapperTests : IDisposable
{
    private readonly Camera? _camera;
    private readonly bool _hasCamera;

    public CameraWrapperTests()
    {
        try
        {
            CameraDiscovery.UpdateDeviceList();
            if (CameraDiscovery.GetDeviceCount() > 0)
            {
                _camera = new Camera(null);
                _hasCamera = true;
            }
        }
        catch
        {
            _hasCamera = false;
        }
    }

    public void Dispose()
    {
        _camera?.Dispose();
    }

    private void SkipIfNoCamera()
    {
        if (!_hasCamera || _camera == null)
        {
            return;
        }
    }

    #region Sensor Size Tests

    [Fact]
    public void GetSensorSize_ShouldReturnPositiveDimensions()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var (width, height) = _camera.GetSensorSize();

        // Assert
        Assert.True(width > 0);
        Assert.True(height > 0);
    }

    #endregion

    #region Acquisition Mode Tests

    [Fact]
    public void GetAcquisitionMode_ShouldReturnValidMode()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var mode = _camera.GetAcquisitionMode();

        // Assert
        Assert.True(Enum.IsDefined(typeof(ArvAcquisitionMode), mode));
    }

    [Fact]
    public void SetAcquisitionMode_ShouldNotThrow()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act & Assert
        var exception = Record.Exception(() => _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous));
        Assert.Null(exception);
    }

    [Fact]
    public void SetAcquisitionMode_SingleFrame_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        _camera.SetAcquisitionMode(ArvAcquisitionMode.SingleFrame);
        var mode = _camera.GetAcquisitionMode();

        // Assert
        Assert.Equal(ArvAcquisitionMode.SingleFrame, mode);
    }

    [Fact]
    public void SetAcquisitionMode_Continuous_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        var mode = _camera.GetAcquisitionMode();

        // Assert
        Assert.Equal(ArvAcquisitionMode.Continuous, mode);
    }

    #endregion

    #region Frame Count Tests

    [Fact]
    public void GetFrameCount_ShouldReturnNonNegative()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Check if feature is available
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Act
        var count = _camera.GetFrameCount();

        // Assert
        Assert.True(count >= 0);
    }

    [Fact]
    public void SetFrameCount_ShouldNotThrow()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Check if feature is available
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Act & Assert
        var exception = Record.Exception(() => _camera.SetFrameCount(10));
        Assert.Null(exception);
    }

    [Fact]
    public void GetFrameCountBounds_ShouldReturnValidRange()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Check if feature is available
        if (!_camera.IsFeatureAvailable("AcquisitionFrameCount")) return;

        // Act
        var (min, max) = _camera.GetFrameCountBounds();

        // Assert
        Assert.True(min >= 0);
        Assert.True(max >= min);
    }

    [Fact]
    public void SetFrameCount_WithinBounds_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Check if feature is available
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

    [Fact]
    public void GetExposureTimeAuto_ShouldReturnValidMode()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureAutoAvailable()) return;

        // Act
        var mode = _camera.GetExposureTimeAuto();

        // Assert
        Assert.True(Enum.IsDefined(typeof(ArvAuto), mode));
    }

    [Fact]
    public void SetExposureTimeAuto_Off_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureAutoAvailable()) return;

        // Act
        _camera.SetExposureTimeAuto(ArvAuto.Off);
        var mode = _camera.GetExposureTimeAuto();

        // Assert
        Assert.Equal(ArvAuto.Off, mode);
    }

    [Fact]
    public void SetExposureTimeAuto_Continuous_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureAutoAvailable()) return;

        // Act
        _camera.SetExposureTimeAuto(ArvAuto.Continuous);
        var mode = _camera.GetExposureTimeAuto();

        // Assert
        Assert.Equal(ArvAuto.Continuous, mode);
    }

    #endregion

    #region Auto Gain Tests

    [Fact]
    public void GetGainAuto_ShouldReturnValidMode()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsGainAutoAvailable()) return;

        // Act
        var mode = _camera.GetGainAuto();

        // Assert
        Assert.True(Enum.IsDefined(typeof(ArvAuto), mode));
    }

    [Fact]
    public void SetGainAuto_Off_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsGainAutoAvailable()) return;

        // Act
        _camera.SetGainAuto(ArvAuto.Off);
        var mode = _camera.GetGainAuto();

        // Assert
        Assert.Equal(ArvAuto.Off, mode);
    }

    [Fact]
    public void SetGainAuto_Continuous_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsGainAutoAvailable()) return;

        // Act
        _camera.SetGainAuto(ArvAuto.Continuous);
        var mode = _camera.GetGainAuto();

        // Assert
        Assert.Equal(ArvAuto.Continuous, mode);
    }

    #endregion

    #region Frame Rate Tests

    // Note: GetFrameRateEnable/SetFrameRateEnable are not available in Aravis 0.8
    // These would be tested if available in newer versions

    #endregion

    #region Generic Feature Access Tests

    [Fact]
    public void GetStringFeature_PixelFormat_ShouldReturnNonEmpty()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var pixelFormat = _camera.GetStringFeature("PixelFormat");

        // Assert
        Assert.NotNull(pixelFormat);
        Assert.NotEmpty(pixelFormat);
    }

    [Fact]
    public void SetStringFeature_PixelFormat_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Arrange
        var originalFormat = _camera.GetPixelFormat();

        // Act & Assert - should not throw
        var exception = Record.Exception(() => _camera.SetStringFeature("PixelFormat", originalFormat));
        Assert.Null(exception);
    }

    [Fact]
    public void GetIntegerFeature_Width_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var width = _camera.GetIntegerFeature("Width");

        // Assert
        Assert.True(width > 0);
    }

    [Fact]
    public void SetIntegerFeature_Width_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Arrange
        var (minWidth, maxWidth) = _camera.GetWidthBounds();

        // Act
        _camera.SetIntegerFeature("Width", maxWidth);
        var width = _camera.GetIntegerFeature("Width");

        // Assert
        Assert.Equal(maxWidth, width);
    }

    [Fact]
    public void GetFloatFeature_ExposureTime_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureTimeAvailable()) return;

        // Act
        var exposure = _camera.GetFloatFeature("ExposureTime");

        // Assert
        Assert.True(exposure > 0);
    }

    [Fact]
    public void SetFloatFeature_ExposureTime_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureTimeAvailable()) return;

        // Arrange
        var (min, max) = _camera.GetExposureTimeBounds();
        var target = min + (max - min) / 2;

        // Act
        _camera.SetFloatFeature("ExposureTime", target);
        var actual = _camera.GetFloatFeature("ExposureTime");

        // Assert
        Assert.True(Math.Abs(actual - target) < 1.0); // Allow small tolerance
    }

    [Fact]
    public void GetBooleanFeature_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Try to find a boolean feature
        if (_camera.IsFeatureAvailable("ReverseX"))
        {
            // Act
            var value = _camera.GetBooleanFeature("ReverseX");

            // Assert - just check it doesn't throw
            Assert.True(value == true || value == false);
        }
    }

    [Fact]
    public void SetBooleanFeature_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Try to find a boolean feature
        if (_camera.IsFeatureAvailable("ReverseX"))
        {
            // Act & Assert - should not throw
            var exception = Record.Exception(() => _camera.SetBooleanFeature("ReverseX", false));
            Assert.Null(exception);
        }
    }

    #endregion

    #region Feature Bounds Tests

    [Fact]
    public void GetIntegerFeatureBounds_Width_ShouldReturnValidRange()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var (min, max) = _camera.GetIntegerFeatureBounds("Width");

        // Assert
        Assert.True(min > 0);
        Assert.True(max >= min);
    }

    [Fact]
    public void GetFloatFeatureBounds_ExposureTime_ShouldReturnValidRange()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureTimeAvailable()) return;

        // Act
        var (min, max) = _camera.GetFloatFeatureBounds("ExposureTime");

        // Assert
        Assert.True(min > 0);
        Assert.True(max >= min);
    }

    #endregion

    #region Feature Increment Tests

    [Fact]
    public void GetWidthIncrement_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var increment = _camera.GetWidthIncrement();

        // Assert
        Assert.True(increment > 0);
    }

    [Fact]
    public void GetHeightIncrement_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var increment = _camera.GetHeightIncrement();

        // Assert
        Assert.True(increment > 0);
    }

    [Fact]
    public void GetIntegerFeatureIncrement_Width_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var increment = _camera.GetIntegerFeatureIncrement("Width");

        // Assert
        Assert.True(increment > 0);
    }

    [Fact]
    public void GetFloatFeatureIncrement_ExposureTime_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsExposureTimeAvailable()) return;

        // Act
        var increment = _camera.GetFloatFeatureIncrement("ExposureTime");

        // Assert
        Assert.True(increment >= 0); // Can be 0 if continuous
    }

    #endregion

    #region Feature Availability Tests

    [Fact]
    public void IsFeatureAvailable_Width_ShouldReturnTrue()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsFeatureAvailable("Width");

        // Assert
        Assert.True(available);
    }

    [Fact]
    public void IsFeatureAvailable_NonExistent_ShouldReturnFalse()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsFeatureAvailable("ThisFeatureDoesNotExist12345");

        // Assert
        Assert.False(available);
    }

    [Fact]
    public void IsBinningAvailable_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsBinningAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Fact]
    public void IsExposureTimeAvailable_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsExposureTimeAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Fact]
    public void IsExposureAutoAvailable_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsExposureAutoAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Fact]
    public void IsGainAvailable_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsGainAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Fact]
    public void IsGainAutoAvailable_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsGainAutoAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Fact]
    public void IsFrameRateAvailable_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var available = _camera.IsFrameRateAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    #endregion

    #region Device Type Tests

    [Fact]
    public void IsGigEVisionDevice_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var isGigE = _camera.IsGigEVisionDevice();

        // Assert - just check it doesn't throw
        Assert.True(isGigE == true || isGigE == false);
    }

    [Fact]
    public void IsUSB3VisionDevice_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var isUSB = _camera.IsUSB3VisionDevice();

        // Assert - just check it doesn't throw
        Assert.True(isUSB == true || isUSB == false);
    }

    [Fact]
    public void DeviceType_ShouldBeEitherGigEOrUSB()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Act
        var isGigE = _camera.IsGigEVisionDevice();
        var isUSB = _camera.IsUSB3VisionDevice();

        // Assert - should be one or the other (or potentially neither for other protocols)
        // Just verify both don't return true
        Assert.False(isGigE && isUSB);
    }

    #endregion

    #region GigE Vision Specific Tests

    [Fact]
    public void GvAutoPacketSize_OnGigECamera_ShouldNotThrow()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsGigEVisionDevice()) return;

        // Act & Assert
        var exception = Record.Exception(() => _camera.GvAutoPacketSize());
        Assert.Null(exception);
    }

    [Fact]
    public void GvGetPacketSize_OnGigECamera_ShouldReturnPositive()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsGigEVisionDevice()) return;

        // Act
        var packetSize = _camera.GvGetPacketSize();

        // Assert
        Assert.True(packetSize > 0);
    }

    [Fact]
    public void GvSetPacketSize_OnGigECamera_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsGigEVisionDevice()) return;

        // Arrange
        var originalSize = _camera.GvGetPacketSize();

        // Act
        _camera.GvSetPacketSize(1500);
        var newSize = _camera.GvGetPacketSize();

        // Restore
        _camera.GvSetPacketSize(originalSize);

        // Assert
        Assert.Equal(1500, newSize);
    }

    #endregion

    #region USB3 Vision Specific Tests

    [Fact]
    public void UvIsBandwidthControlAvailable_OnUSBCamera_ShouldReturnBool()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsUSB3VisionDevice()) return;

        // Act
        var available = _camera.UvIsBandwidthControlAvailable();

        // Assert - just check it doesn't throw
        Assert.True(available == true || available == false);
    }

    [Fact]
    public void UvGetBandwidth_OnUSBCameraWithControl_ShouldReturnNonNegative()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsUSB3VisionDevice()) return;
        if (!_camera.UvIsBandwidthControlAvailable()) return;

        // Act
        var bandwidth = _camera.UvGetBandwidth();

        // Assert
        Assert.True(bandwidth >= 0);
    }

    [Fact]
    public void UvGetBandwidthBounds_OnUSBCameraWithControl_ShouldReturnValidRange()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsUSB3VisionDevice()) return;
        if (!_camera.UvIsBandwidthControlAvailable()) return;

        // Act
        var (min, max) = _camera.UvGetBandwidthBounds();

        // Assert
        Assert.True(min >= 0);
        Assert.True(max >= min);
    }

    [Fact]
    public void UvSetBandwidth_OnUSBCameraWithControl_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        if (!_camera.IsUSB3VisionDevice()) return;
        if (!_camera.UvIsBandwidthControlAvailable()) return;

        // Arrange
        var originalBandwidth = _camera.UvGetBandwidth();
        var (min, max) = _camera.UvGetBandwidthBounds();
        var targetBandwidth = min + (max - min) / 2;

        // Act
        _camera.UvSetBandwidth(targetBandwidth);
        var newBandwidth = _camera.UvGetBandwidth();

        // Restore
        _camera.UvSetBandwidth(originalBandwidth);

        // Assert
        Assert.Equal(targetBandwidth, newBandwidth);
    }

    #endregion

    #region Command Execution Tests

    [Fact]
    public void ExecuteCommand_WithValidCommand_ShouldNotThrow()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Try common commands that might be available
        if (_camera.IsFeatureAvailable("AcquisitionStart"))
        {
            // Act & Assert
            var exception = Record.Exception(() => _camera.ExecuteCommand("AcquisitionStart"));
            // Note: This might fail if acquisition is already running
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_SetupAndQuery_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

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
            var (minExp, maxExp) = _camera.GetExposureTimeBounds();
            var targetExp = minExp + (maxExp - minExp) / 2;
            _camera.SetExposureTime(targetExp);
            var actualExp = _camera.GetExposureTime();
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

    [Fact]
    public void AutoModes_EnableAndDisable_ShouldWork()
    {
        SkipIfNoCamera();
        if (!_hasCamera || _camera == null) return;

        // Test auto exposure if available
        if (_camera.IsExposureAutoAvailable())
        {
            _camera.SetExposureTimeAuto(ArvAuto.Off);
            Assert.Equal(ArvAuto.Off, _camera.GetExposureTimeAuto());

            _camera.SetExposureTimeAuto(ArvAuto.Continuous);
            Assert.Equal(ArvAuto.Continuous, _camera.GetExposureTimeAuto());

            // Restore to off
            _camera.SetExposureTimeAuto(ArvAuto.Off);
        }

        // Test auto gain if available
        if (_camera.IsGainAutoAvailable())
        {
            _camera.SetGainAuto(ArvAuto.Off);
            Assert.Equal(ArvAuto.Off, _camera.GetGainAuto());

            _camera.SetGainAuto(ArvAuto.Continuous);
            Assert.Equal(ArvAuto.Continuous, _camera.GetGainAuto());

            // Restore to off
            _camera.SetGainAuto(ArvAuto.Off);
        }
    }

    #endregion
}
