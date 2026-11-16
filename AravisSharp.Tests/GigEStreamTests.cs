using AravisSharp.Native;
using AravisBuffer = AravisSharp.Buffer;

namespace AravisSharp.Tests;

/// <summary>
/// Starts Aravis' GigE Vision camera simulator (ArvGvFakeCamera) on 127.0.0.1 for the
/// duration of <see cref="GigEStreamTests"/>. It is a fake camera, not hardware: its streams
/// are real ArvGvStream instances, which the "Fake" interface does not provide.
/// </summary>
public sealed class GvFakeCameraFixture : IDisposable
{
    // The GVCP discovery reply holds at most 16 characters of serial number.
    private const string SerialNumber = "AravisSharpGV";

    private IntPtr _simulator;

    public GvFakeCameraFixture()
    {
        if (!NativeTestEnvironment.IsAravisAvailable)
            return;

        _simulator = AravisNative.arv_gv_fake_camera_new("127.0.0.1", SerialNumber);
        IsRunning = _simulator != IntPtr.Zero && AravisNative.arv_gv_fake_camera_is_running(_simulator);
    }

    public bool IsRunning { get; }

    /// <summary>Opens the simulator, found by discovery on 127.0.0.1 and opened by its id.</summary>
    public Camera OpenCamera()
    {
        Assert.True(IsRunning, "The GigE Vision camera simulator did not start on 127.0.0.1 (is UDP port 3956 in use?).");

        var cameras = CameraDiscovery.DiscoverCameras();
        var simulator = cameras.FirstOrDefault(c =>
            c.Protocol == CameraTestHelpers.GigEVisionProtocol &&
            c.Address == "127.0.0.1" &&
            c.DeviceId.EndsWith(SerialNumber, StringComparison.Ordinal));
        Assert.True(simulator != null, "The GigE Vision camera simulator was not discovered on 127.0.0.1. Visible devices: " +
            string.Join(", ", cameras.Select(c => $"{c.DeviceId} ({c.Protocol}, {c.Address})")));

        return new Camera(simulator!.DeviceId);
    }

    public void Dispose()
    {
        if (_simulator != IntPtr.Zero)
        {
            GLibNative.g_object_unref(_simulator);
            _simulator = IntPtr.Zero;
        }
    }
}

public class GigEStreamTests : IClassFixture<GvFakeCameraFixture>
{
    private readonly GvFakeCameraFixture _fixture;

    public GigEStreamTests(GvFakeCameraFixture fixture)
    {
        _fixture = fixture;
    }

    [NativeFact]
    public void ConfigureGigEDefaults_ShouldRequestResendsBeforeTheFrameTimesOut()
    {
        using var camera = _fixture.OpenCamera();
        Assert.True(camera.IsGigEVisionDevice());
        using var stream = camera.CreateStream();

        stream.SetInitialPacketTimeout(1_000_000);
        stream.ConfigureGigEDefaults(socketBufferSizeMB: 8);

        // A missing packet is only requested after the initial packet timeout, and an
        // incomplete frame is dropped once it received nothing for the frame retention:
        // the first request, plus at least one retry, must fit in the retention.
        Assert.Equal(1_000u, stream.GetInitialPacketTimeout());
        Assert.Equal(20_000u, stream.GetPacketTimeout());
        Assert.Equal(100_000u, stream.GetFrameRetention());
        Assert.True(stream.GetInitialPacketTimeout() + 2 * stream.GetPacketTimeout() < stream.GetFrameRetention());
        Assert.Equal(8 * 1024 * 1024, stream.GetSocketBufferSize());
    }

    [NativeFact]
    public void ConfigureGigEDefaults_ShouldKeepThePacketResendPolicy()
    {
        using var camera = _fixture.OpenCamera();
        using var stream = camera.CreateStream();

        stream.SetPacketResend(ArvGvStreamPacketResend.Never);
        stream.ConfigureGigEDefaults();

        Assert.Equal(ArvGvStreamPacketResend.Never, stream.GetPacketResend());
    }

    [NativeFact]
    public void ConfigureGigEDefaults_ShouldRejectNegativeSocketBufferSize()
    {
        using var camera = _fixture.OpenCamera();
        using var stream = camera.CreateStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.ConfigureGigEDefaults(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.ConfigureGigEDefaults(4096));
    }

    [NativeFact]
    public void GigEStreamSetters_ValueRefusedByAravis_ShouldThrowAndKeepThePreviousValue()
    {
        // #14 AC-3 on ArvGvStream: "socket-buffer-size" is a gint in [-1, G_MAXINT] and the
        // policies are enums (aravis/src/arvgvstream.c). These writes used to be ignored silently.
        using var camera = _fixture.OpenCamera();
        using var stream = camera.CreateStream();

        stream.SetSocketBufferSize(-1); // the declared minimum, "not set"
        Assert.Equal(-1, stream.GetSocketBufferSize());
        stream.SetSocketBufferSize(1 << 20);
        Assert.Equal(1 << 20, stream.GetSocketBufferSize());
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetSocketBufferSize(-2));
        Assert.Equal("sizeBytes", ex.ParamName);
        Assert.Equal(1 << 20, stream.GetSocketBufferSize());

        stream.SetPacketResend(ArvGvStreamPacketResend.Never);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetPacketResend((ArvGvStreamPacketResend)99));
        Assert.Equal("policy", ex.ParamName);
        Assert.Equal(ArvGvStreamPacketResend.Never, stream.GetPacketResend());

        // No public getter for the socket buffer policy: read it as Stream would.
        var socketBufferType = AravisNative.arv_gv_stream_socket_buffer_get_type();
        stream.SetSocketBufferPolicy(ArvGvStreamSocketBuffer.Auto);
        ex = Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetSocketBufferPolicy((ArvGvStreamSocketBuffer)99));
        Assert.Equal("policy", ex.ParamName);
        Assert.Equal((int)ArvGvStreamSocketBuffer.Auto, GLibNative.GetEnumProperty(stream.Handle, "socket-buffer", socketBufferType));
    }

    [NativeFact]
    public void GigEStreamTimings_ShouldRoundTrip()
    {
        using var camera = _fixture.OpenCamera();
        using var stream = camera.CreateStream();

        stream.SetInitialPacketTimeout(2_000);
        stream.SetPacketTimeout(15_000);
        stream.SetFrameRetention(250_000);
        stream.SetPacketResend(ArvGvStreamPacketResend.Always);

        Assert.Equal(2_000u, stream.GetInitialPacketTimeout());
        Assert.Equal(15_000u, stream.GetPacketTimeout());
        Assert.Equal(250_000u, stream.GetFrameRetention());
        Assert.Equal(ArvGvStreamPacketResend.Always, stream.GetPacketResend());
    }

    [NativeFact]
    public void GigEGuard_OnGigEStreamAndCamera_ShouldLetTheGigEMembersThrough()
    {
        // #10 AC-1 and AC-2, the GigE side of the guard: IsGigEVision is true on an
        // ArvGvStream, and the guarded members of Camera reach Aravis.
        using var camera = _fixture.OpenCamera();
        Assert.True(camera.IsGigEVisionDevice());
        using var stream = camera.CreateStream();

        Assert.True(stream.IsGigEVision);
        Assert.True(camera.GvGetStreamChannelCount() >= 1);
        var packetSize = camera.GvGetPacketSize();
        Assert.InRange(packetSize, 1, 65535); // GevSCPSPacketSize is a 16-bit field
        var packetDelay = camera.GvGetPacketDelay();

        // The guard must let each call through; what the simulator does with it is not
        // under test, so an AravisException from Aravis is accepted, a
        // NotSupportedException from the guard is not.
        ReachesAravis(() => camera.GvGetIpConfigurationMode());
        ReachesAravis(() => camera.GvGetPersistentIp());
        ReachesAravis(() => camera.GvGetNetworkInterfaceCount());
        ReachesAravis(() => camera.GvAutoPacketSize());
        // Written back with the values read above, so the simulator keeps its settings.
        ReachesAravis(() => camera.GvSetPacketSize(packetSize));
        ReachesAravis(() => camera.GvSetPacketDelay(packetDelay));

        // The stream binds its UDP socket when it is created.
        Assert.NotEqual(0, stream.GetGigEStatistics().Port);
    }

    private static void ReachesAravis(Action call)
    {
        try
        {
            call();
        }
        catch (AravisException)
        {
            // Aravis answered: the guard let the call through.
        }
    }

    [NativeFact]
    public void GigEStream_WithDefaults_ShouldAcquireFrames()
    {
        using var camera = _fixture.OpenCamera();
        camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        using var stream = camera.CreateStream();
        stream.ConfigureGigEDefaults();

        var payload = (int)camera.GetPayloadSize();
        for (int i = 0; i < 4; i++)
            stream.PushBuffer(new AravisBuffer(payload));

        // Loopback UDP can still drop a packet on a loaded runner, and the simulator does not
        // answer resend requests: like Aravis' own tests/fakegv.c, do not require every frame
        // to be complete, only that frames flow with these settings.
        int received = 0, complete = 0;
        camera.StartAcquisition();
        try
        {
            for (int i = 0; i < 10 && complete < 3; i++)
            {
                var buffer = stream.PopBuffer(5000);
                if (buffer == null)
                    continue;
                received++;
                if (buffer.Status == ArvBufferStatus.Success)
                    complete++;
                stream.PushBuffer(buffer);
            }
        }
        finally
        {
            camera.StopAcquisition();
        }

        Assert.True(received > 0, "No buffer came back from the GigE Vision simulator.");
        Assert.True(complete > 0, $"None of the {received} buffers from the GigE Vision simulator was complete.");
        Assert.NotEqual(0, stream.GetGigEStatistics().Port);
    }
}

/// <summary>
/// The conversion of the <c>guint</c> packet size Aravis returns to the <see cref="int"/> of
/// <see cref="Camera.GvGetPacketSize"/> (issue #11). Pure: no camera.
/// </summary>
public class PacketSizeConversionTests
{
    [Theory]
    [InlineData(1500u, 1500)]
    [InlineData(65535u, 65535)]
    [InlineData((uint)int.MaxValue, int.MaxValue)]
    public void ToPacketSize_KeepsASizeThatFitsAnInt(uint native, int expected) =>
        Assert.Equal(expected, Camera.ToPacketSize(native));

    [Fact]
    public void ToPacketSize_ThrowsOnASizeAboveIntMaxValue() =>
        Assert.Throws<OverflowException>(() => Camera.ToPacketSize(uint.MaxValue));
}
