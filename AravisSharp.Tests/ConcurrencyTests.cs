using System.Collections.Concurrent;
using System.Diagnostics;
using AravisSharp.GenICam;
using AravisSharp.Native;
using Xunit.Sdk;
using AravisBuffer = AravisSharp.Buffer;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #7: wrappers used from several threads. Disposal is atomic for callers: a call
/// either completes or throws <see cref="ObjectDisposedException"/> (AC-2, AC-3); one buffer
/// pushed by several threads is queued once (AC-4); a thread waiting for a buffer is woken
/// by <see cref="Stream.Dispose"/> (AC-6); calls on one camera are serialised.
/// </summary>
/// <remarks>
/// The tests that hammer a wrapper from several threads are race tests. On code without the
/// leases a use-after-free only shows when the native release lands inside another thread's
/// native call, so such code can pass them, or crash the test host; what they prove on fixed
/// code is the absence of a crash and of any outcome other than
/// <see cref="ObjectDisposedException"/>. A failure is always real. The tests that hold a
/// lease themselves are deterministic.
/// </remarks>
[Collection("CameraAcquisition")]
public class ConcurrencyTests
{
    private const int Workers = 4;

    /// <summary>When the disposal of a hammered wrapper was called, and when it returned.</summary>
    private sealed class DisposalClock
    {
        private volatile bool _called;
        private volatile bool _returned;

        public bool Called => _called;
        public bool Returned => _returned;

        public void Run(Action dispose)
        {
            _called = true;
            dispose();
            _returned = true;
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> in a loop on several threads until it throws, and returns
    /// what each thread ended on (null: it never threw within the time allowed). Once every
    /// thread has completed a call, <paramref name="dispose"/> runs. A worker fails with an
    /// <see cref="XunitException"/> when a call that started after <paramref name="dispose"/>
    /// returned succeeds (AC-3), or when it gets <see cref="ObjectDisposedException"/> before
    /// <paramref name="dispose"/> was called. <paramref name="afterDispose"/> runs once
    /// <paramref name="dispose"/> has returned.
    /// </summary>
    private static Exception?[] HammerUntilFailure(Action<int> action, Action dispose, Action? afterDispose = null)
    {
        using var started = new CountdownEvent(Workers);
        using var giveUp = new CancellationTokenSource();
        var clock = new DisposalClock();

        var tasks = Enumerable.Range(0, Workers).Select(worker => Task.Run(() =>
        {
            bool signalled = false;
            try
            {
                while (!giveUp.IsCancellationRequested)
                {
                    bool startedAfterDispose = clock.Returned;
                    try
                    {
                        action(worker);
                    }
                    catch (ObjectDisposedException ex) when (!clock.Called)
                    {
                        return new XunitException("ObjectDisposedException before Dispose() was called: " + ex.Message);
                    }
                    if (startedAfterDispose)
                        return new XunitException("a call that started after Dispose() had returned succeeded");
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
                return ex;
            }
            finally
            {
                if (!signalled)
                    started.Signal();
            }
        })).ToArray();

        try
        {
            Assert.True(started.Wait(TimeSpan.FromSeconds(30)), "worker threads did not start");
            clock.Run(dispose);
            afterDispose?.Invoke();
        }
        finally
        {
            // Workers still calling 10 s after the disposal are not refused: stop them.
            giveUp.CancelAfter(TimeSpan.FromSeconds(10));
            Assert.True(Task.WaitAll(tasks, TimeSpan.FromSeconds(60)), "worker threads did not stop");
        }
        return tasks.Select(t => t.Result).ToArray();
    }

    private static void AssertAllObjectDisposed(Exception?[] outcomes) =>
        Assert.All(outcomes, ex =>
        {
            Assert.True(ex is not null, "a worker was still allowed to call 10 s after Dispose() had returned");
            Assert.IsType<ObjectDisposedException>(ex);
        });

    private static void SignalAndWait(Barrier barrier) =>
        Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(30)), "the other threads did not reach the barrier");

    [NativeFact]
    public void Camera_DisposedWhileCameraDeviceAndNodeMapAreInUse_ShouldOnlyEverThrowObjectDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        try
        {
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
                dispose: () =>
                {
                    Thread.Sleep(50);
                    camera.Dispose();
                },
                afterDispose: () =>
                {
                    // Dispose() has returned: no call may start any more, on any thread, even
                    // while another thread is still inside one.
                    Assert.Throws<ObjectDisposedException>(() => camera.GetPayloadSize());
                    Assert.Throws<ObjectDisposedException>(() => device.GetIntegerFeature("Width"));
                    Assert.Throws<ObjectDisposedException>(() => nodeMap.GetIntegerFeature("Width"));
                });

            AssertAllObjectDisposed(outcomes);
        }
        finally
        {
            camera.Dispose();
        }
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

    // Deterministic: the test holds the camera's lock itself, through a Device lease.
    [NativeFact]
    public void CameraLock_HeldByOneThread_ShouldMakeAnotherThreadsCameraCallWait()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            var device = camera.GetDevice();
            Task<uint> other;
            using (var lease = device.AcquireHandle())
            {
                other = Task.Run(() => camera.GetPayloadSize());
                Assert.False(other.Wait(TimeSpan.FromMilliseconds(300)), "a camera call ran while another thread held the camera's lock");
            }

            Assert.True(other.Wait(TimeSpan.FromSeconds(5)), "the camera call did not run once the lock was released");
            Assert.True(other.Result > 0);
        }
        finally
        {
            camera.Dispose();
        }
    }

    // Deterministic: a call queued on the camera's lock gives up soon after Dispose(), even
    // though the lock is still held (the lock is waited for in 50 ms slices).
    [NativeFact]
    public void CameraLock_CallQueuedWhenTheCameraIsDisposed_ShouldThrowObjectDisposedPromptly()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            var device = camera.GetDevice();
            using (var lease = device.AcquireHandle())
            {
                var other = Task.Run(() => Record.Exception(() => camera.GetPayloadSize()));
                Assert.False(other.Wait(TimeSpan.FromMilliseconds(300)));

                var clock = Stopwatch.StartNew();
                camera.Dispose();

                Assert.True(other.Wait(TimeSpan.FromSeconds(2)), "the queued call did not give up while the lock was held");
                Assert.True(clock.ElapsedMilliseconds < 500, $"the queued call took {clock.ElapsedMilliseconds} ms to give up");
                Assert.IsType<ObjectDisposedException>(other.Result);
            }
        }
        finally
        {
            camera.Dispose();
        }
    }

    // AC-3 for Device: a call that passed the Device's own disposed check and then waited for
    // the camera's lock must not start once Device.Dispose() has returned.
    [NativeFact]
    public void Device_DisposedWhileACallWaitsForTheCamerasLock_ShouldThrowObjectDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            var device = camera.GetDevice();
            var lockHolder = camera.GetDevice();
            Task<Exception?> other;
            using (var lease = lockHolder.AcquireHandle())
            {
                other = Task.Run(() => Record.Exception(() => device.GetIntegerFeature("Width")));
                Assert.False(other.Wait(TimeSpan.FromMilliseconds(300)));
                device.Dispose();
            }

            Assert.True(other.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsType<ObjectDisposedException>(other.Result);
            Assert.True(camera.GetIntegerFeature("Width") > 0);
        }
        finally
        {
            camera.Dispose();
        }
    }

    // Same for GenICamNode.
    [NativeFact]
    public void GenICamNode_DisposedWhileACallWaitsForTheCamerasLock_ShouldThrowObjectDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        try
        {
            var device = camera.GetDevice();
            var node = HandleLifetimeTests.OpenNode(device, "Width");
            Task<Exception?> other;
            using (var lease = device.AcquireHandle())
            {
                other = Task.Run(() => Record.Exception(() => node.GetValueAsString()));
                Assert.False(other.Wait(TimeSpan.FromMilliseconds(300)));
                node.Dispose();
            }

            Assert.True(other.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsType<ObjectDisposedException>(other.Result);
        }
        finally
        {
            camera.Dispose();
        }
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
                SignalAndWait(barrier);
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
                SignalAndWait(barrier);
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
        try
        {
            var buffer = new AravisBuffer((int)camera.GetPayloadSize());

            var outcomes = HammerUntilFailure(
                worker =>
                {
                    // What matters is that each call reaches a live native buffer or is refused.
                    // The content is not asserted: a reader that got in just before the push
                    // may see the buffer empty, already filled, or half filled.
                    if (worker % 2 == 0)
                        Assert.NotNull(buffer.CopyData());
                    else
                        Assert.NotEqual(IntPtr.Zero, buffer.GetData().Data);
                },
                dispose: () =>
                {
                    Thread.Sleep(20);
                    // Pushing closes the Buffer wrapper.
                    stream.PushBuffer(buffer);
                },
                // The stream now holds the only long-lived reference and drops it here,
                // possibly while a reader is still inside the native buffer.
                afterDispose: stream.Dispose);

            AssertAllObjectDisposed(outcomes);
        }
        finally
        {
            stream.Dispose();
        }
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
                dispose: () =>
                {
                    Thread.Sleep(100);
                    stream.Dispose();
                },
                afterDispose: () => Assert.Throws<ObjectDisposedException>(() => stream.GetStatistics()));

            AssertAllObjectDisposed(outcomes);
        }
        finally
        {
            camera.StopAcquisition();
            stream.Dispose();
        }
    }

    // AC-6. No buffer is queued, so no frame can ever arrive: without help from Dispose()
    // the waits below last for ever or a minute. The GLib log of every thread is captured: the
    // native release runs on the waiting thread when it returns its lease, and disposing a
    // stream under a waiting thread makes GLib log a critical (g_async_queue_unref).
    [NativeFact]
    public void Stream_DisposedWhileAThreadWaitsForABuffer_ShouldWakeItWithObjectDisposed()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var glibLog = new GObjectLogCapture("GLib", allThreads: true);

        foreach (var (name, wait) in new (string, Func<Stream, AravisBuffer?>)[]
                 {
                     ("PopBuffer()", s => s.PopBuffer()),
                     ("PopBuffer(60000)", s => s.PopBuffer(60_000)),
                     ("PopBuffer(ulong.MaxValue)", s => s.PopBuffer(ulong.MaxValue)),
                 })
        {
            var stream = camera.CreateStream();
            try
            {
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
            finally
            {
                stream.Dispose();
            }
        }

        Assert.Empty(glibLog.Criticals);
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
