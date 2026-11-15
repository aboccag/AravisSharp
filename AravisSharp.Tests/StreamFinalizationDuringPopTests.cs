using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AravisSharp.Native;
using Xunit.Abstractions;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #13, B7: a <see cref="Stream"/> is not collected while a thread is blocked in its own
/// pop. The pops pass the native result to the instance method <c>WrapBuffer</c>, which calls
/// <see cref="GC.KeepAlive(object)"/>, so <c>this</c> stays reachable until the native call
/// returns. If nothing used <c>this</c> after the native call, then once the JIT optimizes
/// the pop, <c>this</c> would be dead as soon as <c>_handle</c> is loaded. A stream the caller
/// no longer references could then be finalized while a thread still waits in
/// <c>arv_stream_pop_buffer</c> on it, and the finalizer would drop the last native
/// reference under the waiter.
/// </summary>
/// <remarks>
/// Each scenario runs in a child test host (<c>dotnet test</c> on this assembly, filtered to
/// one child test) with <c>DOTNET_TieredCompilation=0</c>, so that the library code is fully
/// optimized at once, as in a hot loop of a real application. A crash or hang of the child
/// cannot take this test host down. The defect only exists when AravisSharp is built with
/// optimizations (Release, as in CI): on a Debug build the parent tests are reported as
/// skipped (<see cref="OptimizedNativeFactAttribute"/>). The child holds its own native
/// reference on the <c>ArvStream</c>, so the defect shows as a failed assertion, never as a
/// use after free. The child does not inherit the parent's test platform or profiler
/// variables. Still, do not run these tests under a coverage collector: instrumentation
/// can change what the JIT keeps alive, and only the uninstrumented run is meaningful.
/// </remarks>
public class StreamFinalizationDuringPopTests
{
    private const string ChildEnvironmentVariable = "ARAVISSHARP_TEST_CHILD";
    private const string ChildMarker = "B7-CHILD";
    private static readonly TimeSpan ChildBound = TimeSpan.FromSeconds(180);

    private readonly ITestOutputHelper _output;

    public StreamFinalizationDuringPopTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Skipped, not passed, without native Aravis or on a Debug build of AravisSharp.
    [OptimizedNativeFact]
    public Task Stream_ShouldNotBeCollectedWhileItsTimedPopIsBlocked() =>
        RunChildAsync(nameof(Child_StreamMustNotBeCollectedWhileItsTimedPopIsBlocked));

    // Same through PopBuffer() without timeout, where the wait has no bound.
    [OptimizedNativeFact]
    public Task Stream_ShouldNotBeCollectedWhileItsUnboundedPopIsBlocked() =>
        RunChildAsync(nameof(Child_StreamMustNotBeCollectedWhileItsUnboundedPopIsBlocked));

    /// <summary>
    /// Runs one child scenario in its own test host and reads its outcome from the TRX file
    /// and the exit code, not from the console wording: the child must report exactly that
    /// test, with the outcome Passed, and exit with 0.
    /// </summary>
    private async Task RunChildAsync(string childTestName)
    {
        string expectedTest = $"{typeof(StreamFinalizationDuringPopTests).FullName}.{childTestName}";
        string testAssembly = typeof(StreamFinalizationDuringPopTests).Assembly.Location;
        string resultsDirectory = Path.Combine(Path.GetTempPath(), "aravissharp-b7-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(resultsDirectory);
        try
        {
            // The dotnet host that runs this test host, so that the child uses the same SDK;
            // "dotnet" from PATH when the runner does not say.
            var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(testAssembly)!,
            };
            start.ArgumentList.Add("test");
            start.ArgumentList.Add(testAssembly);
            start.ArgumentList.Add("--filter");
            start.ArgumentList.Add($"FullyQualifiedName={expectedTest}");
            start.ArgumentList.Add("--results-directory");
            start.ArgumentList.Add(resultsDirectory);
            start.ArgumentList.Add("--logger");
            start.ArgumentList.Add("trx;LogFileName=child.trx");
            RemoveInheritedTestHostVariables(start.Environment);
            start.Environment[ChildEnvironmentVariable] = ChildMarker;
            start.Environment["DOTNET_TieredCompilation"] = "0";

            using var child = Process.Start(start)!;
            var stdout = child.StandardOutput.ReadToEndAsync();
            var stderr = child.StandardError.ReadToEndAsync();
            bool finished;
            using (var timeout = new CancellationTokenSource(ChildBound))
            {
                try
                {
                    await child.WaitForExitAsync(timeout.Token);
                    finished = true;
                }
                catch (OperationCanceledException)
                {
                    child.Kill(entireProcessTree: true);
                    finished = false;
                    await BoundedAsync(child.WaitForExitAsync(), "waiting for the killed child to exit");
                }
            }
            _output.WriteLine(await BoundedAsync(stdout, "reading the child's standard output") ?? "");
            _output.WriteLine(await BoundedAsync(stderr, "reading the child's standard error") ?? "");
            Assert.True(finished, $"the child test host did not finish within {ChildBound.TotalSeconds} s and was killed; its output is above");

            string trx = Path.Combine(resultsDirectory, "child.trx");
            Assert.True(File.Exists(trx), $"the child test host wrote no TRX file (exit code {child.ExitCode})");
            var results = XDocument.Load(trx).Descendants()
                .Where(e => e.Name.LocalName == "UnitTestResult")
                .Select(e => (Name: (string?)e.Attribute("testName"), Outcome: (string?)e.Attribute("outcome"),
                              Message: e.Descendants().FirstOrDefault(m => m.Name.LocalName == "Message")?.Value))
                .ToList();
            string summary = string.Join("; ", results.Select(r => $"{r.Name}: {r.Outcome} {r.Message}"));
            Assert.True(results.Count == 1 && results[0].Name == expectedTest,
                $"the child test host did not run exactly {expectedTest}: [{summary}]");
            Assert.True(results[0].Outcome == "Passed", $"child: {summary}");
            Assert.True(child.ExitCode == 0, $"the child test host exited with {child.ExitCode}: [{summary}]");
        }
        finally
        {
            try
            {
                Directory.Delete(resultsDirectory, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A leftover temporary folder does not change the outcome.
            }
        }
    }

    // Bound of each wait after the child's own bound: the killed child's exit and the reads
    // of its output, which a surviving grandchild holding the pipes could otherwise block.
    private static readonly TimeSpan AfterChildBound = TimeSpan.FromSeconds(10);

    private async Task<T?> BoundedAsync<T>(Task<T> task, string what)
    {
        try
        {
            return await task.WaitAsync(AfterChildBound);
        }
        catch (TimeoutException)
        {
            _output.WriteLine($"Gave up {what} after {AfterChildBound.TotalSeconds} s.");
            return default;
        }
    }

    private async Task BoundedAsync(Task task, string what)
    {
        try
        {
            await task.WaitAsync(AfterChildBound);
        }
        catch (TimeoutException)
        {
            _output.WriteLine($"Gave up {what} after {AfterChildBound.TotalSeconds} s.");
        }
    }

    // Variables of the parent's test platform and of a coverage collector. Inherited, they
    // would make the child connect to the parent's runner or load a profiler it was not
    // meant to run under.
    private static readonly string[] InheritedVariablePrefixes =
    {
        "VSTEST_", "TESTINGPLATFORM_", "COVERLET_", "CORECLR_ENABLE_PROFILING", "CORECLR_PROFILER",
        "COR_ENABLE_PROFILING", "COR_PROFILER", "MicrosoftInstrumentationEngine_",
    };

    private static void RemoveInheritedTestHostVariables(IDictionary<string, string?> environment)
    {
        var inherited = environment.Keys
            .Where(key => InheritedVariablePrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        foreach (var key in inherited)
            environment.Remove(key);
    }

    // Only runs inside the child test host started above.
    [ChildOnlyFact]
    public void Child_StreamMustNotBeCollectedWhileItsTimedPopIsBlocked() => RunChildScenario(timeoutMs: 600_000);

    // Only runs inside the child test host. PopBuffer() never returns on its own.
    [ChildOnlyFact]
    public void Child_StreamMustNotBeCollectedWhileItsUnboundedPopIsBlocked() => RunChildScenario(timeoutMs: null);

    /// <summary>
    /// Pops on a stream that only the pop itself references, collects while the pop blocks,
    /// releases the pop by pushing a buffer through the native handle, then checks two
    /// things: the stream was still reachable during the pop, and it becomes unreachable
    /// once the pop has returned. The second check proves that the scenario does produce a
    /// collectible stream, so the first one cannot pass vacuously.
    /// </summary>
    private static void RunChildScenario(ulong? timeoutMs)
    {
        string call = timeoutMs is ulong ms ? $"PopBuffer({ms})" : "PopBuffer()";
        var camera = CameraTestHelpers.OpenTestCamera();
        var created = new ManualResetEventSlim();
        var nativeStream = new StrongBox<IntPtr>(IntPtr.Zero);
        var managedStream = new StrongBox<WeakReference?>(null);
        Exception? workerError = null;
        bool popReturned = false;

        var worker = new Thread(() =>
        {
            try
            {
                using var buffer = PopOnStreamTheCallerNoLongerUses(camera, timeoutMs, nativeStream, managedStream, created);
                Volatile.Write(ref popReturned, true);
            }
            catch (Exception e)
            {
                workerError = e;
            }
        }) { IsBackground = true, Name = "B7 pop worker" };

        bool released = false;
        try
        {
            CameraTestHelpers.ConfigureForAcquisition(camera);
            camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
            uint payload = camera.GetPayloadSize();
            camera.StartAcquisition(); // no buffer is pushed yet: the pop blocks
            worker.Start();
            Assert.True(created.Wait(TimeSpan.FromSeconds(10)), "the worker did not create its stream within 10 s");
            var weak = managedStream.Value!;

            // Collecting before the worker is in the native pop would check nothing: wait until
            // a thread waits on the output queue (a GLib async queue length is the items queued
            // minus the waiting threads, so it turns negative).
            var waitClock = Stopwatch.StartNew();
            int output = 0;
            while (waitClock.Elapsed < TimeSpan.FromSeconds(10) && worker.IsAlive)
            {
                AravisNative.arv_stream_get_n_buffers(nativeStream.Value, out _, out output);
                if (output < 0)
                    break;
                Thread.Sleep(5);
            }
            Assert.True(output < 0,
                $"no thread waited on the output queue within 10 s (output queue length {output}, worker " +
                $"{(worker.IsAlive ? "alive" : "ended")}): {call} never blocked, so nothing would be checked");

            // The stream exists and only its pop references it: collect while the pop blocks.
            for (int i = 0; i < 10 && weak.IsAlive && worker.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(50);
            }
            bool collectedDuringPop = !weak.IsAlive;
            bool popStillBlocked = worker.IsAlive && !Volatile.Read(ref popReturned);

            // Release the pop: push one buffer through the native handle, which the extra
            // reference keeps valid whatever happened to the managed Stream.
            var nativeBuffer = AravisNative.arv_buffer_new((UIntPtr)payload, IntPtr.Zero);
            AravisNative.arv_stream_push_buffer(nativeStream.Value, nativeBuffer);
            released = worker.Join(TimeSpan.FromSeconds(10));

            Assert.True(released, $"{call} did not return within 10 s after a buffer was pushed");
            Assert.Null(workerError);
            Assert.True(popStillBlocked, $"{call} returned before the collections ran: nothing was checked");
            Assert.False(collectedDuringPop,
                $"the Stream was collected while its {call} was still blocked: its finalizer released the " +
                "native stream under the waiting thread");

            // The pop has returned: the stream is now garbage and must be collected.
            for (int i = 0; i < 10 && weak.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.False(weak.IsAlive,
                "the Stream was not collected after its pop returned: the scenario keeps it reachable, so it checks nothing");
        }
        finally
        {
            if (released || !worker.IsAlive)
            {
                camera.StopAcquisition();
                camera.Dispose();
                if (nativeStream.Value != IntPtr.Zero)
                    GLibNative.g_object_unref(nativeStream.Value);
            }
            else
            {
                // A worker is still blocked in a native pop: leave the native objects alone.
                // This is a child host; it exits after this test.
                GC.SuppressFinalize(camera);
            }
        }
    }

    // The stream is a temporary: nothing but the Stream method itself can keep it alive
    // while the native pop blocks. The extra native reference keeps the ArvStream valid even
    // when the defect lets ~Stream drop the managed wrapper's reference during the pop.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Buffer? PopOnStreamTheCallerNoLongerUses(Camera camera, ulong? timeoutMs,
        StrongBox<IntPtr> nativeStream, StrongBox<WeakReference?> managedStream, ManualResetEventSlim created)
    {
        var stream = camera.CreateStream();
        nativeStream.Value = GLibNative.g_object_ref(stream.Handle);
        managedStream.Value = new WeakReference(stream);
        created.Set();
        return timeoutMs is ulong ms ? stream.PopBuffer(ms) : stream.PopBuffer();
    }

    private sealed class ChildOnlyFactAttribute : NativeFactAttribute
    {
        public ChildOnlyFactAttribute()
        {
            if (Skip == null && Environment.GetEnvironmentVariable(ChildEnvironmentVariable) != ChildMarker)
                Skip = "Child scenario of StreamFinalizationDuringPopTests: runs only in the child test host it starts.";
        }
    }
}
