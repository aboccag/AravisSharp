using AravisSharp;
using AravisSharp.Native;

namespace AravisSharp.Examples;

/// <summary>
/// Validates software-triggered acquisition across multiple cameras.
/// </summary>
public static class MultiCameraSoftwareTriggerCheckExample
{
    private sealed class CameraSession : IDisposable
    {
        public required CameraInfo Info { get; init; }
        public required Camera Camera { get; init; }
        public required Stream Stream { get; init; }
        public required List<Buffer> Buffers { get; init; }

        public bool AcquisitionStarted { get; set; }
        public bool FreeRunOk { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }

        public void Dispose()
        {
            try
            {
                if (AcquisitionStarted)
                {
                    Camera.StopAcquisition();
                }
            }
            catch
            {
                // Best-effort cleanup.
            }

            try
            {
                Camera.ClearTriggers();
            }
            catch
            {
                // Best-effort cleanup.
            }

            try
            {
                Stream.Dispose();
            }
            catch
            {
                // Best-effort cleanup.
            }

            // Buffers are owned by the stream once queued; disposing them here can
            // double-unref the same native objects during stream teardown.
            Buffers.Clear();

            try
            {
                Camera.Dispose();
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    public static void Run(int expectedCameraCount = 10, int triggerCount = 5, ulong timeoutMs = 2000)
    {
        Console.WriteLine("=== Multi-Camera Software Trigger Check ===\n");
        Console.WriteLine($"Target cameras: {expectedCameraCount}");
        Console.WriteLine($"Triggers per camera: {triggerCount}");
        Console.WriteLine($"Frame timeout: {timeoutMs} ms\n");

        var discovered = CameraDiscovery.DiscoverCameras();
        if (discovered.Count == 0)
        {
            Console.WriteLine("No cameras discovered.");
            return;
        }

        Console.WriteLine($"Discovered {discovered.Count} camera(s):");
        for (int i = 0; i < discovered.Count; i++)
        {
            var info = discovered[i];
            Console.WriteLine($"  [{i}] {info.Vendor} {info.Model} - {info.DeviceId} ({info.Protocol})");
        }
        Console.WriteLine();

        if (discovered.Count < expectedCameraCount)
        {
            Console.WriteLine($"WARNING: Expected {expectedCameraCount} cameras, found {discovered.Count}.");
            Console.WriteLine("Checking all discovered cameras.\n");
        }

        var cameraInfos = discovered.Take(expectedCameraCount).ToList();

        // Every listed camera is opened, reconfigured (packet size, acquisition mode,
        // triggers) and streamed from. Cameras on a shared network may belong to another
        // application, so ask first; anything but an explicit yes leaves them alone.
        Console.WriteLine("The following camera(s) will be opened and reconfigured (packet size, triggers, acquisition):");
        foreach (var info in cameraInfos)
            Console.WriteLine($"  {info.Vendor} {info.Model} - {info.DeviceId} ({info.Protocol})");
        Console.Write("Proceed? [y/N]: ");
        var answer = Console.ReadLine()?.Trim();
        if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Cancelled. No camera was opened.");
            return;
        }
        Console.WriteLine();

        var sessions = new List<CameraSession>();

        try
        {
            foreach (var info in cameraInfos)
            {
                Camera? camera = null;
                Stream? stream = null;
                try
                {
                    camera = new Camera(info.DeviceId);

                    if (camera.IsGigEVisionDevice())
                    {
                        try
                        {
                            camera.GvAutoPacketSize();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"WARN {info.DeviceId}: GvAutoPacketSize failed ({ex.Message}).");
                        }
                    }

                    stream = camera.CreateStream();
                    var payloadSize = (int)camera.GetPayloadSize();
                    if (payloadSize <= 0)
                    {
                        var (_, _, width, height) = camera.GetRegion();
                        payloadSize = Math.Max(1, width * height);
                    }

                    var buffers = new List<Buffer>();
                    const int bufferCount = 8;
                    for (int i = 0; i < bufferCount; i++)
                    {
                        var buffer = new Buffer(new IntPtr(payloadSize));
                        buffers.Add(buffer);
                        stream.PushBuffer(buffer);
                    }

                    sessions.Add(new CameraSession
                    {
                        Info = info,
                        Camera = camera,
                        Stream = stream,
                        Buffers = buffers
                    });
                    // The session owns them now.
                    camera = null;
                    stream = null;

                    Console.WriteLine($"READY {info.DeviceId}: payload={payloadSize} bytes, buffers={bufferCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAIL {info.DeviceId}: setup failed - {ex.Message}");

                    // Not handed to a session yet: release here, stream before camera.
                    // Buffers already pushed belong to the stream and go with it.
                    stream?.Dispose();
                    camera?.Dispose();
                }
            }

            if (sessions.Count == 0)
            {
                Console.WriteLine("\nNo camera could be configured for software-trigger acquisition.");
                return;
            }

            Console.WriteLine("\nPhase 1: Free-run check (TriggerMode=Off)...");
            foreach (var session in sessions)
            {
                try
                {
                    session.Camera.ClearTriggers();
                    session.Camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
                    if (session.Camera.IsGigEVisionDevice())
                    {
                        try
                        {
                            session.Camera.GvSetPacketSize(1500);
                        }
                        catch
                        {
                            // Keep going if packet size cannot be changed.
                        }
                    }
                    session.Camera.StartAcquisition();
                    session.AcquisitionStarted = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAIL {session.Info.DeviceId}: free-run start failed - {ex.Message}");
                }
            }

            Thread.Sleep(250);

            foreach (var session in sessions)
            {
                if (!session.AcquisitionStarted)
                {
                    continue;
                }

                try
                {
                    var buffer = session.Stream.PopBuffer(timeoutMs);
                    if (buffer != null && buffer.Status == ArvBufferStatus.Success)
                    {
                        session.FreeRunOk = true;
                        session.Stream.PushBuffer(buffer);
                    }
                    else
                    {
                        if (buffer != null)
                        {
                            session.Stream.PushBuffer(buffer);
                        }
                    }
                }
                catch
                {
                    // Keep FreeRunOk = false.
                }
            }

            Console.WriteLine("\nFree-run diagnostics:");
            foreach (var session in sessions)
            {
                try
                {
                    var (completed, failures, underruns) = session.Stream.GetStatistics();
                    var (port, resent, missing) = session.Stream.GetGigEStatistics();
                    var scda = SafeGetIntegerFeature(session.Camera, "GevSCDA");
                    var hostPort = SafeGetIntegerFeature(session.Camera, "GevSCPHostPort");
                    var packetSize = SafeGetIntegerFeature(session.Camera, "GevSCPSPacketSize");
                    var interPacketDelay = SafeGetIntegerFeature(session.Camera, "GevSCPD");

                    Console.WriteLine(
                        $"{session.Info.DeviceId}: stream[c={completed},f={failures},u={underruns}], " +
                        $"gv[port={port},resent={resent},missing={missing}], " +
                        $"feat[SCDA={scda},HostPort={hostPort},PktSize={packetSize},SCPD={interPacketDelay}]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{session.Info.DeviceId}: diagnostics unavailable ({ex.Message})");
                }
            }

            foreach (var session in sessions)
            {
                if (!session.AcquisitionStarted)
                {
                    continue;
                }

                try
                {
                    session.Camera.StopAcquisition();
                }
                catch
                {
                    // Best-effort cleanup.
                }
                session.AcquisitionStarted = false;
            }

            var softwareCandidates = sessions.Where(s => s.FreeRunOk).ToList();
            Console.WriteLine("\nPhase 2: Software-trigger check (TriggerMode=On, TriggerSource=Software)...");
            Console.WriteLine($"Software-trigger candidates: {softwareCandidates.Count}/{sessions.Count}");

            foreach (var session in softwareCandidates)
            {
                try
                {
                    if (!session.Camera.IsSoftwareTriggerSupported())
                    {
                        Console.WriteLine($"SKIP {session.Info.DeviceId}: software trigger not supported.");
                        continue;
                    }

                    session.Camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
                    session.Camera.SetTrigger("Software");
                    session.Camera.StartAcquisition();
                    session.AcquisitionStarted = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAIL {session.Info.DeviceId}: software-trigger start failed - {ex.Message}");
                }
            }

            Thread.Sleep(250);

            Console.WriteLine("\nTriggering cameras...");
            for (int triggerIndex = 0; triggerIndex < triggerCount; triggerIndex++)
            {
                foreach (var session in softwareCandidates)
                {
                    if (!session.AcquisitionStarted)
                    {
                        continue;
                    }

                    try
                    {
                        try
                        {
                            session.Camera.ExecuteCommand("TriggerSoftware");
                        }
                        catch
                        {
                            session.Camera.SoftwareTrigger();
                        }
                        var buffer = session.Stream.PopBuffer(timeoutMs);

                        if (buffer != null && buffer.Status == ArvBufferStatus.Success)
                        {
                            session.SuccessCount++;
                            session.Stream.PushBuffer(buffer);
                        }
                        else
                        {
                            session.FailureCount++;
                            if (buffer != null)
                            {
                                session.Stream.PushBuffer(buffer);
                            }
                        }
                    }
                    catch
                    {
                        session.FailureCount++;
                    }
                }
            }

            Console.WriteLine("\n=== Results ===");
            foreach (var session in sessions)
            {
                var total = session.SuccessCount + session.FailureCount;
                Console.WriteLine(
                    $"{session.Info.DeviceId}: free-run={(session.FreeRunOk ? "OK" : "FAIL")}, " +
                    $"software-trigger={session.SuccessCount}/{total} OK, {session.FailureCount} timeout/failure");
            }

            var passed = sessions.All(s => s.FreeRunOk) &&
                         softwareCandidates.Count > 0 &&
                         softwareCandidates.All(s => s.SuccessCount == triggerCount);
            Console.WriteLine();
            Console.WriteLine(passed
                ? "ACQUISITION CHECK PASSED"
                : "ACQUISITION CHECK FAILED (see per-camera results above)");
        }
        finally
        {
            foreach (var session in sessions)
            {
                session.Dispose();
            }
        }
    }

    private static string SafeGetIntegerFeature(Camera camera, string name)
    {
        try
        {
            return camera.GetIntegerFeature(name).ToString();
        }
        catch
        {
            return "n/a";
        }
    }
}
