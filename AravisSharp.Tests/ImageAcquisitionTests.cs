using System;
using System.IO;
using AravisSharp;
using AravisSharp.Native;
using AravisSharp.Utilities;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Integration tests that acquire real frames from the connected camera.
/// All tests in this class are skipped when no camera is available.
/// </summary>
[Collection("CameraAcquisition")]
public class ImageAcquisitionTests : IDisposable
{
    private readonly Camera? _camera;
    private readonly bool _hasCamera;

    public ImageAcquisitionTests()
    {
        if (!NativeTestEnvironment.IsAravisAvailable)
        {
            _hasCamera = false;
            return;
        }

        try
        {
            CameraDiscovery.UpdateDeviceList();
            if (CameraDiscovery.GetDeviceCount() > 0)
            {
                _camera = new Camera(null);
                ConfigureCameraForAcquisition(_camera);
                _hasCamera = true;
            }
        }
        catch
        {
            _hasCamera = false;
        }
    }

    public void Dispose() => _camera?.Dispose();

    private static void ConfigureCameraForAcquisition(Camera camera)
    {
        // Set short exposure so tests don't time out (camera default may be seconds-long)
        if (camera.IsExposureTimeAvailable())
        {
            CameraTestHelpers.TryDisableExposureAuto(camera);

            var (minExp, _) = camera.GetExposureTimeBounds();
            double targetExp = Math.Max(minExp, 10_000); // 10 ms minimum
            camera.SetExposureTime(targetExp);
        }
        // Ensure free-running (no trigger)
        try { camera.ClearTriggers(); } catch { }
    }

    private static Stream CreateStreamWithBuffers(Camera camera, int numBuffers, out uint payloadSize)
    {
        payloadSize = camera.GetPayloadSize();
        var stream = camera.CreateStream();
        for (int i = 0; i < numBuffers; i++)
            stream.PushBuffer(new Buffer((int)payloadSize));
        return stream;
    }

    [Fact]
    public void AcquireSingleFrame_StatusShouldBeSuccess()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        _camera.StartAcquisition();
        try
        {
            var buffer = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buffer);
            using (buffer)
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }
    }

    [Fact]
    public void AcquireSingleFrame_DimensionsShouldBePositive()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        _camera.StartAcquisition();
        try
        {
            var buffer = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buffer);
            using (buffer)
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                Assert.True(buffer.Width > 0, $"Expected positive width, got {buffer.Width}");
                Assert.True(buffer.Height > 0, $"Expected positive height, got {buffer.Height}");
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }
    }

    [Fact]
    public void AcquireSingleFrame_DataShouldBeNonEmpty()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        _camera.StartAcquisition();
        try
        {
            var buffer = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buffer);
            using (buffer)
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                var (dataPtr, size) = buffer.GetData();
                Assert.NotEqual(IntPtr.Zero, dataPtr);
                Assert.True(size > 0, $"Expected non-empty data, got size={size}");
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }
    }

    [Fact]
    public void AcquireSingleFrame_DataSizeMatchesDimensions()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        _camera.StartAcquisition();
        try
        {
            var buffer = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buffer);
            using (buffer)
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                var (_, size) = buffer.GetData();
                var expectedSize = ImageHelper.CalculateBufferSize(buffer.Width, buffer.Height, buffer.PixelFormat);
                Assert.Equal(expectedSize, size);
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }
    }

    [Fact]
    public void AcquireAndSavePng_ShouldCreateValidFile()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        var path = Path.Combine(Path.GetTempPath(), $"aravis_test_{Guid.NewGuid():N}.png");
        try
        {
            _camera.StartAcquisition();
            try
            {
                var buffer = stream.PopBuffer(timeoutMs: 5000);
                Assert.NotNull(buffer);
                using (buffer)
                {
                    Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                    ImageHelper.SaveToPng(buffer, path);
                }
            }
            finally
            {
                _camera.StopAcquisition();
            }

            Assert.True(File.Exists(path), "PNG file was not created");
            var info = new FileInfo(path);
            Assert.True(info.Length > 0, "PNG file is empty");

            // Verify it is a valid PNG (starts with PNG magic bytes)
            var header = new byte[8];
            using var fs = File.OpenRead(path);
            _ = fs.Read(header, 0, 8);
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, header);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void AcquireMultipleFrames_AllShouldSucceed()
    {
        if (!_hasCamera || _camera == null) return;

        const int framesToAcquire = 5;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, framesToAcquire + 2, out _);

        int successCount = 0;

        _camera.StartAcquisition();
        try
        {
            for (int i = 0; i < framesToAcquire; i++)
            {
                var buffer = stream.PopBuffer(timeoutMs: 5000);
                Assert.NotNull(buffer);
                using (buffer)
                {
                    if (buffer.Status == ArvBufferStatus.Success)
                        successCount++;
                    stream.PushBuffer(new Buffer((int)_camera.GetPayloadSize()));
                }
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }

        Assert.Equal(framesToAcquire, successCount);
    }

    [Fact]
    public void AcquireFrames_StreamStatisticsShouldShowNoFailures()
    {
        if (!_hasCamera || _camera == null) return;

        const int framesToAcquire = 3;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, framesToAcquire + 2, out var payloadSize);

        _camera.StartAcquisition();
        try
        {
            for (int i = 0; i < framesToAcquire; i++)
            {
                var buffer = stream.PopBuffer(timeoutMs: 5000);
                if (buffer != null)
                {
                    using (buffer)
                        stream.PushBuffer(new Buffer((int)payloadSize));
                }
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }

        var (completed, failures, _) = stream.GetStatistics();
        Assert.True(completed > 0, "No frames completed");
        Assert.Equal(0UL, failures);
    }

    [Fact]
    public void AcquireFrame_FrameIdShouldIncrement()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out var payloadSize);

        ulong firstId = 0, secondId = 0;

        _camera.StartAcquisition();
        try
        {
            var buf1 = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buf1);
            using (buf1)
            {
                Assert.Equal(ArvBufferStatus.Success, buf1.Status);
                firstId = buf1.FrameId;
                stream.PushBuffer(new Buffer((int)payloadSize));
            }

            var buf2 = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buf2);
            using (buf2)
            {
                Assert.Equal(ArvBufferStatus.Success, buf2.Status);
                secondId = buf2.FrameId;
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }

        Assert.True(secondId > firstId, $"Frame IDs did not increment: {firstId} -> {secondId}");
    }

    [Fact]
    public void AcquireFrame_TimestampShouldBeNonZero()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        _camera.StartAcquisition();
        try
        {
            var buffer = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buffer);
            using (buffer)
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);
                Assert.True(buffer.Timestamp > 0, $"Timestamp was zero");
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }
    }

    [Fact]
    public void AcquireFrame_CopyDataShouldMatchSpanData()
    {
        if (!_hasCamera || _camera == null) return;

        _camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = CreateStreamWithBuffers(_camera, 5, out _);

        _camera.StartAcquisition();
        try
        {
            var buffer = stream.PopBuffer(timeoutMs: 5000);
            Assert.NotNull(buffer);
            using (buffer)
            {
                Assert.Equal(ArvBufferStatus.Success, buffer.Status);

                var copied = buffer.CopyData();
                var span = buffer.GetDataSpan();

                Assert.Equal(copied.Length, span.Length);
                Assert.True(copied.AsSpan().SequenceEqual(span), "CopyData and GetDataSpan returned different bytes");
            }
        }
        finally
        {
            _camera.StopAcquisition();
        }
    }
}
