using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Pins interop behaviours of the bindings that no other test covered (issue #13): blocking
/// and non-blocking pops, the release of the arrays of <c>arv_camera_dup_available_*</c>,
/// the absence of variadic imports, and the layout of <see cref="GLibNative.GValue"/>.
/// Camera tests open the test camera through <see cref="CameraTestHelpers"/>: the fake
/// camera unless ARAVIS_TEST_DEVICE_ID names a device.
/// </summary>
[Collection("CameraAcquisition")]
public class InteropPinTests
{
    // Objects deliberately leaked when a worker stays blocked in a native pop: disposing or
    // finalizing a stream under a waiter frees the queue it waits on (see the remarks of
    // Stream.PopBuffer).
    private static readonly List<object> LeakedUnderBlockedPop = new();

    // How long a pop must stay blocked to count as blocking, and the upper bounds of every
    // wait. Wide on purpose: a loaded CI runner can stall a thread for a long time, and a
    // bound only matters when the behaviour is broken.
    private static readonly TimeSpan BlockedWindow = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StartBound = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ReturnBound = TimeSpan.FromSeconds(10);

    // === B1, B2: a pop with no buffer queued blocks until one arrives ===

    [NativeTheory]
    [InlineData(null)]                 // PopBuffer()
    [InlineData(ulong.MaxValue)]       // PopBuffer(ulong.MaxValue): "infinite"
    [InlineData(600_000UL)]            // PopBuffer(ulong) with a finite timeout of 10 min
    [InlineData(ulong.MaxValue - 1)]   // largest finite timeout, capped by ToTimeoutMicroseconds
    public void PopBuffer_NoBufferQueued_ShouldBlockUntilOneIsPushed(ulong? timeoutMs)
    {
        string call = timeoutMs is null ? "PopBuffer()" : $"PopBuffer({timeoutMs})";
        var camera = CameraTestHelpers.OpenTestCamera();
        Stream? stream = null;
        Thread? worker = null;
        bool released = false;
        try
        {
            CameraTestHelpers.ConfigureForAcquisition(camera);
            camera.SetAcquisitionMode(ArvAcquisitionMode.Continuous);
            uint payload = camera.GetPayloadSize();
            stream = camera.CreateStream(); // no buffer pushed: nothing can be popped yet

            var pop = StartPopWorker(stream, timeoutMs, out worker, out var aboutToPop);
            camera.StartAcquisition();
            try
            {
                Assert.True(aboutToPop.Wait(StartBound), "the pop worker did not start");
                // "Not yet started" is not "blocked": the window opens once the worker is
                // about to call the pop.
                bool returnedEarly = worker.Join(BlockedWindow);
                Assert.False(returnedEarly,
                    $"{call} returned before any buffer was pushed (buffer: {(pop.Buffer is null ? "null" : "non-null")}, " +
                    $"error: {pop.Error?.Message ?? "none"})");

                stream.PushBuffer(new Buffer((int)payload));
                released = worker.Join(ReturnBound);
                Assert.True(released, $"{call} did not return within {ReturnBound.TotalSeconds} s after a buffer was pushed");

                Assert.Null(pop.Error);
                Assert.NotNull(pop.Buffer);
                using (pop.Buffer)
                    Assert.Equal(ArvBufferStatus.Success, pop.Buffer!.Status);
            }
            finally
            {
                // Release a worker still blocked (an assertion failed before the push).
                released = released || ReleaseWorker(stream, payload, worker);
                if (released)
                    camera.StopAcquisition();
            }
        }
        finally
        {
            DisposeUnlessBlocked(camera, stream, worker);
        }
    }

    // === B3: PopBuffer(0) on an empty queue returns null at once ===

    [NativeFact]
    public void PopBuffer_ZeroTimeout_EmptyQueue_ShouldReturnNullAtOnce()
    {
        // Upper bound for "at once": a non-blocking pop takes microseconds, a loaded runner
        // can stall a thread for tens of milliseconds. Any real wait (one frame, one second,
        // forever) is far above it.
        const int AtOnceMs = 250;
        var camera = CameraTestHelpers.OpenTestCamera();
        Stream? stream = null;
        Thread? worker = null;
        try
        {
            CameraTestHelpers.ConfigureForAcquisition(camera);
            uint payload = camera.GetPayloadSize();
            stream = camera.CreateStream();

            // The first call is a warm-up (JIT, native symbol binding), the second is measured.
            // Each runs in a worker, so that a pop that blocks cannot hang the test host.
            for (int call = 1; call <= 2; call++)
            {
                var pop = StartPopWorker(stream, 0, out worker, out var aboutToPop);
                Assert.True(aboutToPop.Wait(StartBound), "the pop worker did not start");
                bool returned = worker.Join(ReturnBound);
                if (!returned)
                    ReleaseWorker(stream, payload, worker);

                Assert.True(returned, $"PopBuffer(0) on an empty queue did not return within {ReturnBound.TotalSeconds} s (call {call})");
                Assert.Null(pop.Error);
                Assert.Null(pop.Buffer);
                if (call == 2)
                {
                    Assert.True(pop.Elapsed < TimeSpan.FromMilliseconds(AtOnceMs),
                        $"PopBuffer(0) on an empty queue took {pop.Elapsed.TotalMilliseconds:F1} ms, bound {AtOnceMs} ms");
                }
            }
        }
        finally
        {
            DisposeUnlessBlocked(camera, stream, worker);
        }
    }

    private sealed class PopResult
    {
        public Buffer? Buffer;
        public Exception? Error;
        public TimeSpan Elapsed;
    }

    /// <summary>
    /// Starts a background thread that pops once from <paramref name="stream"/>:
    /// <see cref="Stream.PopBuffer()"/> when <paramref name="timeoutMs"/> is null,
    /// <see cref="Stream.PopBuffer(ulong)"/> otherwise. The result is valid once the thread
    /// has been joined.
    /// </summary>
    private static PopResult StartPopWorker(Stream stream, ulong? timeoutMs, out Thread worker, out ManualResetEventSlim aboutToPop)
    {
        var result = new PopResult();
        var signal = new ManualResetEventSlim(false);
        worker = new Thread(() =>
        {
            try
            {
                signal.Set();
                var clock = Stopwatch.StartNew();
                result.Buffer = timeoutMs is null ? stream.PopBuffer() : stream.PopBuffer(timeoutMs.Value);
                result.Elapsed = clock.Elapsed;
            }
            catch (Exception e)
            {
                result.Error = e;
            }
        }) { IsBackground = true, Name = "PopBuffer worker" };
        aboutToPop = signal;
        worker.Start();
        return result;
    }

    /// <summary>
    /// Wakes a worker blocked in a pop by pushing one buffer. Returns whether it returned
    /// within the bound. The pushed buffer is popped by the worker or freed with the stream.
    /// </summary>
    private static bool ReleaseWorker(Stream stream, uint payload, Thread worker)
    {
        if (!worker.IsAlive)
            return true;
        try
        {
            stream.PushBuffer(new Buffer((int)payload));
        }
        catch (Exception)
        {
            // Nothing else can wake the worker: it counts as released only if it returned.
        }
        return worker.Join(ReturnBound);
    }

    /// <summary>
    /// Disposes the stream and the camera, unless a worker is still blocked in a native pop
    /// on the stream: then both are leaked for the life of the test host, never disposed or
    /// finalized under the waiter. The worker is a background thread and cannot keep the
    /// host alive.
    /// </summary>
    private static void DisposeUnlessBlocked(Camera camera, Stream? stream, Thread? worker)
    {
        if (worker is { IsAlive: true })
        {
            if (stream != null)
                GC.SuppressFinalize(stream);
            GC.SuppressFinalize(camera);
            lock (LeakedUnderBlockedPop)
            {
                if (stream != null)
                    LeakedUnderBlockedPop.Add(stream);
                LeakedUnderBlockedPop.Add(camera);
            }
            return;
        }

        try
        {
            stream?.Dispose();
        }
        finally
        {
            camera.Dispose();
        }
    }

    // === B4: the arrays of arv_camera_dup_available_* are freed once, by g_free ===

    [NativeFact]
    public void DupAvailableWrappers_10000Calls_ShouldSurviveAndReturnStableResults()
    {
        // Pins the free of each (transfer container) array: a double free, or a g_free of an
        // element (the strings belong to the GenICam nodes), corrupts the heap and glibc
        // aborts the process long before 10 000 calls. A missing g_free only leaks, which
        // this test does not see (spec #13, decision on question 2).
        const int Calls = 10_000;
        using var camera = CameraTestHelpers.OpenTestCamera();

        var formats = camera.GetAvailablePixelFormats();
        var displayNames = camera.GetAvailablePixelFormatDisplayNames();
        var values = camera.GetAvailableEnumerationValues("PixelFormat");
        Assert.NotEmpty(formats);
        Assert.NotEmpty(displayNames);
        Assert.NotEmpty(values);
        Assert.Equal(formats.Length, displayNames.Length);

        for (int i = 1; i < Calls; i++)
        {
            Assert.True(formats.SequenceEqual(camera.GetAvailablePixelFormats()), $"pixel formats changed at call {i}");
            Assert.True(displayNames.SequenceEqual(camera.GetAvailablePixelFormatDisplayNames()), $"display names changed at call {i}");
            Assert.True(values.SequenceEqual(camera.GetAvailableEnumerationValues("PixelFormat")), $"enumeration values changed at call {i}");
        }
    }

    // === B8: no DllImport binds a variadic C function ===

    // Variadic GLib/GObject functions a binding is tempted to import. A fixed-argument
    // DllImport of one of them breaks on Apple arm64, where variadic arguments go on the
    // stack (GLibNative.cs, "GObject property access").
    private static readonly string[] VariadicEntryPoints =
    {
        "g_object_set", "g_object_get", "g_object_new", "g_object_connect",
        "g_strdup_printf", "g_error_new", "g_log",
    };

    private static IEnumerable<(MethodInfo Method, string EntryPoint)> AllDllImports()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        // GetTypes() includes nested types.
        foreach (var type in typeof(Camera).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(All))
            {
                var import = method.GetCustomAttribute<DllImportAttribute>();
                if (import is not null)
                    yield return (method, import.EntryPoint ?? method.Name);
            }
        }
    }

    [Fact]
    public void NoDllImport_ShouldBindAVariadicFunction()
    {
        var imports = AllDllImports().ToList();

        // The scan must see the imports at all, or the check below would pass vacuously.
        Assert.Contains(imports, i => i.EntryPoint == "g_object_unref");
        Assert.Contains(imports, i => i.EntryPoint == "g_object_set_property");
        Assert.Contains(imports, i => i.EntryPoint == "arv_camera_new");

        var variadic = imports
            .Where(i => VariadicEntryPoints.Contains(i.EntryPoint))
            .Select(i => $"{i.Method.DeclaringType}.{i.Method.Name} -> {i.EntryPoint}")
            .ToList();
        Assert.Empty(variadic);
    }

    // === B9: GValue matches struct _GValue on 64-bit ===

    [SixtyFourBitFact] // skipped, not passed, in a 32-bit process
    public void GValue_ShouldMatchCLayoutOn64Bit()
    {
        // /usr/include/glib-2.0/gobject/gvalue.h: struct _GValue { GType g_type; union {...} data[2]; }
        // with GType = gsize and each union 8 bytes (gint64 / guint64 / gdouble / gpointer).
        Assert.Equal(24, Marshal.SizeOf<GLibNative.GValue>());
        Assert.Equal(0, (int)Marshal.OffsetOf<GLibNative.GValue>(nameof(GLibNative.GValue.GType)));
        Assert.Equal(8, (int)Marshal.OffsetOf<GLibNative.GValue>(nameof(GLibNative.GValue.Data0)));
        Assert.Equal(16, (int)Marshal.OffsetOf<GLibNative.GValue>(nameof(GLibNative.GValue.Data1)));

        // Size and offsets alone miss a narrowed slot: with Data0 or Data1 as an int, the
        // alignment padding keeps the size at 24 and the offsets at 8 and 16, but GLib
        // writes 8 bytes into a 4-byte field. Pin the field types too.
        var fields = typeof(GLibNative.GValue).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(
            new[] { (nameof(GLibNative.GValue.GType), typeof(IntPtr)), (nameof(GLibNative.GValue.Data0), typeof(long)), (nameof(GLibNative.GValue.Data1), typeof(long)) },
            fields.OrderBy(f => (int)Marshal.OffsetOf<GLibNative.GValue>(f.Name)).Select(f => (f.Name, f.FieldType)).ToArray());
    }
}
