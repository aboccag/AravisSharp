using System.Collections.Concurrent;
using System.Diagnostics;
using AravisSharp.GenICam;
using AravisSharp.Native;
using AravisBuffer = AravisSharp.Buffer;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #7: wrappers used from several threads. Disposal is atomic for callers: a call
/// either completes or throws <see cref="ObjectDisposedException"/> (AC-2, AC-3); one buffer
/// pushed by several threads is queued once (AC-4); a thread waiting for a buffer is woken
/// by <see cref="Stream.Dispose"/> (AC-6).
/// </summary>
/// <remarks>
/// Most of these are race tests. On code without the leases a use-after-free only shows
/// when the native release lands inside another thread's native call, so such code can
/// pass them, or crash the test host; what they prove on fixed code is the absence of a
/// crash and of any outcome other than <see cref="ObjectDisposedException"/>. A failure is
/// always real.
/// </remarks>
[Collection("CameraAcquisition")]
public class ConcurrencyTests
{
    private const int Workers = 4;

    /// <summary>
    /// Runs <paramref name="action"/> in a loop on several threads until it throws, and returns
    /// what each thread ended on (null: it never threw within the time allowed).
    /// <paramref name="whileRunning"/> runs once every thread has completed at least one call.
    /// </summary>
    private static Exception?[] HammerUntilFailure(Action<int> action, Action whileRunning)
    {
        using var started = new CountdownEvent(Workers);
        using var giveUp = new CancellationTokenSource();

        var tasks = Enumerable.Range(0, Workers).Select(worker => Task.Run(() =>
        {
            bool signalled = false;
            try
            {
                while (!giveUp.IsCancellationRequested)
                {
                    action(worker);
                    if (!signalled)
                    {
                        signalled = true;
                        started.Signal();
                    }
                }
                return (Exception?)null;
            }
            catch (Exception ex)
            {
                if (!signalled)
                    started.Signal();
                return ex;
            }
        })).ToArray();

        try
        {
            Assert.True(started.Wait(TimeSpan.FromSeconds(30)), "worker threads did not start");
            whileRunning();
        }
        finally
        {
            // Workers still calling 10 s after whileRunning are not refused: stop them.
            giveUp.CancelAfter(TimeSpan.FromSeconds(10));
            Assert.True(Task.WaitAll(tasks, TimeSpan.FromSeconds(60)), "worker threads did not stop");
        }
        return tasks.Select(t => t.Result).ToArray();
    }

    private static void AssertAllObjectDisposed(Exception?[] outcomes) =>
        Assert.All(outcomes, ex =>
        {
            Assert.True(ex is not null, "a worker was still allowed to call after Dispose() had returned");
            Assert.IsType<ObjectDisposedException>(ex);
        });

    [NativeFact]
    public void Camera_DisposedWhileCameraDeviceAndNodeMapAreInUse_ShouldOnlyEverThrowObjectDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;

        var outcomes = HammerUntilFailure(
            worker =>
            {
                switch (worker % 4)
                {
                    case 0: Assert.True(camera.GetPayloadSize() > 0); break;
                    case 1: Assert.True(device.GetIntegerFeature("Width") > 0); break;
                    case 2: Assert.True(nodeMap.GetIntegerFeature("Height") > 0); break;
                    default: Assert.NotNull(nodeMap.GetFeatureDetails("Width")); break;
                }
            },
            whileRunning: () =>
            {
                Thread.Sleep(50);
                camera.Dispose();

                // Dispose() has returned: no call may start any more, on any thread, even
                // while another thread is still inside one.
                Assert.Throws<ObjectDisposedException>(() => camera.GetPayloadSize());
                Assert.Throws<ObjectDisposedException>(() => device.GetIntegerFeature("Width"));
                Assert.Throws<ObjectDisposedException>(() => nodeMap.GetIntegerFeature("Width"));
            });

        AssertAllObjectDisposed(outcomes);
    }

    [NativeFact]
    public void Camera_CallsFromCameraDeviceAndNodeMapOnSeveralThreads_ShouldAllSucceed()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        long width = camera.GetIntegerFeature("Width");
        long height = camera.GetIntegerFeature("Height");
        var failures = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, Workers).Select(worker => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 200; i++)
                {
                    switch (worker % 4)
                    {
                        case 0: Assert.Equal(width, camera.GetIntegerFeature("Width")); break;
                        case 1: Assert.Equal(height, device.GetIntegerFeature("Height")); break;
                        case 2: Assert.Equal(width, nodeMap.GetIntegerFeature("Width")); break;
                        default: Assert.Equal(FeatureType.Integer, nodeMap.GetFeatureDetails("Height")!.Type); break;
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        })).ToArray();

        Assert.True(Task.WaitAll(tasks, TimeSpan.FromSeconds(60)), "worker threads did not stop");
        Assert.Empty(failures);
    }

    [NativeFact]
    public void Device_NodeMapRequestedFromSeveralThreads_ShouldBeOneInstance()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();

        for (int round = 0; round < 20; round++)
        {
            var device = camera.GetDevice();
            using var barrier = new Barrier(Workers);

            var tasks = Enumerable.Range(0, Workers).Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(30));
                return device.NodeMap;
            })).ToArray();

            Assert.True(Task.WaitAll(tasks, TimeSpan.FromSeconds(30)));
            Assert.All(tasks, t => Assert.Same(tasks[0].Result, t.Result));
            Assert.Same(tasks[0].Result, device.NodeMap);
        }
    }

    // AC-4. On code that reads the buffer's handle before releasing it, several pushes of one
    // buffer can all succeed: the native buffer is then queued, and later released, more than
    // once, which may crash the test host instead of failing the assertion.
    [FakeCameraFact]
    public void Buffer_PushedFromSeveralThreads_ShouldBeQueuedOnce()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();
        int payload = (int)camera.GetPayloadSize();
        const int rounds = 25;

        for (int round = 0; round < rounds; round++)
        {
            var buffer = new AravisBuffer(payload);
            using var barrier = new Barrier(Workers);
            var failures = new ConcurrentBag<Exception>();
            int pushed = 0;

            var tasks = Enumerable.Range(0, Workers).Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(30));
                try
                {
                    stream.PushBuffer(buffer);
                    Interlocked.Increment(ref pushed);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            })).ToArray();

            Assert.True(Task.WaitAll(tasks, TimeSpan.FromSeconds(30)));
            Assert.Equal(1, pushed);
            Assert.Equal(Workers - 1, failures.Count);
            Assert.All(failures, ex => Assert.IsType<ObjectDisposedException>(ex));
        }

        // The fake stream thread fills whatever is queued, even without acquisition: exactly
        // one buffer per round comes back, no more (queued twice) and no fewer (lost).
        for (int i = 0; i < rounds; i++)
        {
            using var filled = stream.PopBuffer(2000);
            Assert.NotNull(filled);
            Assert.Equal(payload, filled!.CopyData().Length);
        }
        Assert.Null(stream.PopBuffer(200));
    }

    [NativeFact]
    public void Buffer_ReadWhilePushedAndTheStreamDisposed_ShouldOnlyEverThrowObjectDisposed()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var stream = camera.CreateStream();
        var buffer = new AravisBuffer((int)camera.GetPayloadSize());

        var outcomes = HammerUntilFailure(
            worker =>
            {
                // What matters is that each call reaches a live native buffer or is refused.
                // The content is not asserted: a reader that got in just before the push may
                // see the buffer empty or already filled by the stream thread.
                if (worker % 2 == 0)
                    Assert.NotNull(buffer.CopyData());
                else
                    Assert.NotEqual(IntPtr.Zero, buffer.GetData().Data);
            },
            whileRunning: () =>
            {
                Thread.Sleep(20);
                stream.PushBuffer(buffer);
                // The stream now holds the only long-lived reference and drops it here,
                // possibly while a reader is still inside the native buffer.
                stream.Dispose();
            });

        AssertAllObjectDisposed(outcomes);
    }

    [NativeFact]
    public void Stream_DisposedWhilePolledFromSeveralThreads_ShouldOnlyEverThrowObjectDisposed()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        CameraTestHelpers.ConfigureForAcquisition(camera);
        var stream = camera.CreateStream();
        int payload = (int)camera.GetPayloadSize();
        for (int i = 0; i < 8; i++)
            stream.PushBuffer(new AravisBuffer(payload));

        camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
        camera.StartAcquisition();
        try
        {
            var outcomes = HammerUntilFailure(
                worker =>
                {
                    switch (worker % 3)
                    {
                        case 0:
                            var frame = stream.PopBuffer(20);
                            if (frame != null)
                                stream.PushBuffer(frame);
                            break;
                        case 1: stream.GetStatistics(); break;
                        default: stream.GetBufferCounts(); break;
                    }
                },
                whileRunning: () =>
                {
                    Thread.Sleep(100);
                    stream.Dispose();
                    Assert.Throws<ObjectDisposedException>(() => stream.GetStatistics());
                });

            AssertAllObjectDisposed(outcomes);
        }
        finally
        {
            camera.StopAcquisition();
        }
    }

    // AC-6. No buffer is queued, so no frame can ever arrive: without help from Dispose()
    // the waits below last for ever or a minute.
    [NativeFact]
    public void Stream_DisposedWhileAThreadWaitsForABuffer_ShouldWakeItWithObjectDisposed()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();

        foreach (var (name, wait) in new (string, Func<Stream, AravisBuffer?>)[]
                 {
                     ("PopBuffer()", s => s.PopBuffer()),
                     ("PopBuffer(60000)", s => s.PopBuffer(60_000)),
                     ("PopBuffer(ulong.MaxValue)", s => s.PopBuffer(ulong.MaxValue)),
                 })
        {
            var stream = camera.CreateStream();
            using var waiting = new ManualResetEventSlim();

            var waiter = Task.Run(() =>
            {
                waiting.Set();
                return Record.Exception(() => wait(stream));
            });

            Assert.True(waiting.Wait(TimeSpan.FromSeconds(30)));
            Thread.Sleep(300);
            Assert.False(waiter.IsCompleted, $"{name} returned although no buffer was queued");

            var clock = Stopwatch.StartNew();
            stream.Dispose();

            Assert.True(waiter.Wait(TimeSpan.FromSeconds(5)), $"Dispose() did not wake the thread waiting in {name}");
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2), $"{name} took {clock.Elapsed} to give up");
            Assert.IsType<ObjectDisposedException>(waiter.Result);
        }
    }

    [NativeFact]
    public void Stream_BoundedWait_ShouldStillTimeOutWithNull_AfterTheTimeAskedFor()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();

        var clock = Stopwatch.StartNew();
        Assert.Null(stream.PopBuffer(350));
        clock.Stop();

        // The wait may be cut in slices internally: the total is still the one asked for.
        Assert.InRange(clock.ElapsedMilliseconds, 340, 3000);
        Assert.Null(stream.PopBuffer(0));
        Assert.Null(stream.TryPopBuffer());
    }

    [FakeCameraFact]
    public void Stream_UnboundedPop_ShouldReturnTheBufferAsSoonAsItIsReady()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var stream = camera.CreateStream();
        int payload = (int)camera.GetPayloadSize();

        var waiter = Task.Run(() => stream.PopBuffer());
        Thread.Sleep(250);
        Assert.False(waiter.IsCompleted);

        // The fake stream thread fills a queued buffer within one frame period (25 ms by default).
        stream.PushBuffer(new AravisBuffer(payload));

        Assert.True(waiter.Wait(TimeSpan.FromSeconds(5)), "PopBuffer() did not return the buffer");
        using var frame = waiter.Result;
        Assert.NotNull(frame);
        Assert.Equal(ArvBufferStatus.Success, frame!.Status);
    }
}
