using System.Runtime.CompilerServices;
using AravisSharp.GenICam;
using AravisSharp.Native;
using AravisBuffer = AravisSharp.Buffer;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #7: ownership of the native objects behind <see cref="Camera"/>, <see cref="Device"/>,
/// <see cref="NodeMap"/>, <see cref="Stream"/> and <see cref="AravisBuffer"/>. Each owned
/// reference is released exactly once (AC-1), and a <see cref="Device"/> or
/// <see cref="NodeMap"/> shares its camera's lifetime: it throws
/// <see cref="ObjectDisposedException"/> once the camera is disposed and never keeps the
/// connection open (AC-5).
/// </summary>
/// <remarks>
/// Release counts come from <see cref="GObjectReleaseProbe"/>. Where a test holds its own
/// native reference, a second unref by a wrapper drops the test's reference instead of
/// freeing memory twice: it shows as a release before the test's own unref, never as a
/// native crash.
/// </remarks>
[Collection("CameraAcquisition")]
public class HandleLifetimeTests
{
    private const int Threads = 4;

    #region Device and NodeMap share the camera's lifetime (AC-5)

    [NativeFact]
    public void Device_AndNodeMap_AfterCameraDisposed_ShouldThrowObjectDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        var lateDevice = camera.GetDevice();
        Assert.True(device.GetIntegerFeature("Width") > 0);
        Assert.NotNull(nodeMap.GetFeatureDetails("Width"));

        camera.Dispose();

        Assert.Throws<ObjectDisposedException>(() => device.GetIntegerFeature("Width"));
        Assert.Throws<ObjectDisposedException>(() => device.SetIntegerFeature("Width", 64));
        Assert.Throws<ObjectDisposedException>(() => device.GetStringFeature("DeviceVendorName"));
        Assert.Throws<ObjectDisposedException>(() => device.GetFloatFeature("Gain"));
        Assert.Throws<ObjectDisposedException>(() => device.GetBooleanFeature("ReverseX"));
        Assert.Throws<ObjectDisposedException>(() => device.ExecuteCommand("AcquisitionStop"));
        // A node map asked for only after the camera is gone is not created either.
        Assert.Throws<ObjectDisposedException>(() => lateDevice.NodeMap);

        // Every NodeMap member, including those that return null or an empty list on an
        // Aravis error: a disposed camera is not an Aravis error.
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetFeatureDetails("Width"));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetFeaturesInCategory("Root"));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetFeaturesByCategory());
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetAllFeatures());
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetNode("Width"));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetStringFeature("DeviceVendorName"));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetIntegerFeature("Width"));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.SetIntegerFeature("Width", 64));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.ExecuteCommand("AcquisitionStop"));
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetGenicamXml());

        // Disposing the wrappers afterwards is still allowed.
        device.Dispose();
        nodeMap.Dispose();
    }

    [NativeFact]
    public void Device_NotDisposed_ShouldNotKeepTheNativeDevice_OnceTheCameraIsDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        Assert.True(nodeMap.GetIntegerFeature("Width") > 0);
        long id = GObjectReleaseProbe.Watch(device.Handle);

        camera.Dispose();

        // The ArvDevice (the USB3 interface or GigE control channel of a real camera) goes
        // with the camera, although the Device and NodeMap wrappers are still referenced.
        Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id));
        GC.KeepAlive(device);
        GC.KeepAlive(nodeMap);
    }

    [NativeFact]
    public void Device_Dispose_ShouldOnlyMarkTheWrapper_AndNotReleaseTheCamerasDevice()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        long id = GObjectReleaseProbe.Watch(device.Handle);

        device.Dispose();
        device.Dispose();

        Assert.Equal(0, GObjectReleaseProbe.ReleaseCount(id));
        Assert.Throws<ObjectDisposedException>(() => device.GetIntegerFeature("Width"));
        // The camera, and a new Device from it, are unaffected.
        Assert.True(camera.GetIntegerFeature("Width") > 0);
        Assert.True(camera.GetDevice().GetIntegerFeature("Width") > 0);

        camera.Dispose();
        Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id));
    }

    // AC-8. Red on 8fd1bff only as a compile error: the GenICamNode constructor took no owner.
    [NativeFact]
    public void GenICamNode_AfterCameraDisposed_ShouldThrowObjectDisposed()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var node = OpenNode(device, "Width");
        Assert.Equal("Width", node.Name);
        Assert.True(node.IsAvailable());
        Assert.False(string.IsNullOrEmpty(node.GetValueAsString()));

        camera.Dispose();

        Assert.Throws<ObjectDisposedException>(() => node.IsAvailable());
        Assert.Throws<ObjectDisposedException>(() => node.IsImplemented());
        Assert.Throws<ObjectDisposedException>(() => node.IsLocked());
        Assert.Throws<ObjectDisposedException>(() => node.GetValueAsString());
        Assert.Throws<ObjectDisposedException>(() => node.SetValueFromString("64"));
        node.Dispose();
    }

    [NativeFact]
    public void GenICamNode_AfterItsOwnDispose_ShouldThrowObjectDisposed_AndLeaveTheCameraUsable()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var node = OpenNode(camera.GetDevice(), "Width");

        node.Dispose();
        node.Dispose();

        Assert.Throws<ObjectDisposedException>(() => node.GetValueAsString());
        Assert.True(camera.GetIntegerFeature("Width") > 0);
    }

    private static GenICamNode OpenNode(Device device, string name)
    {
        var genicam = AravisNative.arv_device_get_genicam(device.Handle);
        Assert.NotEqual(IntPtr.Zero, genicam);
        var namePtr = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(name);
        try
        {
            var nodePtr = AravisNative.arv_gc_get_node(genicam, namePtr);
            Assert.NotEqual(IntPtr.Zero, nodePtr);
            return new GenICamNode(device, nodePtr, genicam);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(namePtr);
        }
    }

    #endregion

    #region Each reference released exactly once (AC-1)

    // A race test: on code with a non-atomic Dispose() the double unref only happens when
    // two threads interleave between the check and the unref, so a pass on such code is
    // possible; a failure is always real.
    [NativeFact]
    public void Camera_DisposedByManyThreadsAtOnce_ShouldReleaseItsReferenceOnce()
    {
        for (int round = 0; round < 20; round++)
        {
            var camera = CameraTestHelpers.OpenTestCamera();
            IntPtr own = GLibNative.g_object_ref(camera.Handle);
            long id = GObjectReleaseProbe.Watch(own);

            DisposeFromManyThreads(camera);

            Assert.Equal(0, GObjectReleaseProbe.ReleaseCount(id));
            Assert.Throws<ObjectDisposedException>(() => camera.GetModelName());
            GLibNative.g_object_unref(own);
            Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id));
        }
    }

    // Race test, see Camera_DisposedByManyThreadsAtOnce_ShouldReleaseItsReferenceOnce.
    [NativeFact]
    public void Stream_DisposedByManyThreadsAtOnce_ShouldReleaseItsReferenceOnce()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        for (int round = 0; round < 20; round++)
        {
            var stream = camera.CreateStream();
            IntPtr own = GLibNative.g_object_ref(stream.Handle);
            long id = GObjectReleaseProbe.Watch(own);

            DisposeFromManyThreads(stream);

            Assert.Equal(0, GObjectReleaseProbe.ReleaseCount(id));
            Assert.Throws<ObjectDisposedException>(() => stream.GetStatistics());
            GLibNative.g_object_unref(own);
            Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id));
        }
    }

    // Race test, see Camera_DisposedByManyThreadsAtOnce_ShouldReleaseItsReferenceOnce.
    [NativeFact]
    public void Buffer_DisposedByManyThreadsAtOnce_ShouldReleaseItsReferenceOnce()
    {
        for (int round = 0; round < 50; round++)
        {
            var buffer = new AravisBuffer(1024);
            IntPtr own = GLibNative.g_object_ref(buffer.Handle);
            long id = GObjectReleaseProbe.Watch(own);

            DisposeFromManyThreads(buffer);

            Assert.Equal(0, GObjectReleaseProbe.ReleaseCount(id));
            Assert.Throws<ObjectDisposedException>(() => buffer.Status);
            GLibNative.g_object_unref(own);
            Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id));
        }
    }

    [NativeFact]
    public void AbandonedWrappers_ShouldEachBeReleasedOnceByTheFinalizers()
    {
        // Nothing is disposed: the finalizers drop each reference exactly once, in whatever
        // order the GC picks. The test keeps its own reference on each native object, so a
        // second unref shows as a release before the test's own unref.
        var (objects, ids, wrappers) = AbandonCameraDeviceStreamAndBuffers();
        for (int i = 0; i < 10 && wrappers.Any(w => w.IsAlive); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.All(wrappers, w => Assert.False(w.IsAlive, "an abandoned wrapper was not collected: nothing was checked"));
        Assert.All(ids, id => Assert.Equal(0, GObjectReleaseProbe.ReleaseCount(id)));

        foreach (var obj in objects)
            GLibNative.g_object_unref(obj);
        Assert.All(ids, id => Assert.Equal(1, GObjectReleaseProbe.ReleaseCount(id)));

        // The camera can be opened again afterwards.
        using var camera = CameraTestHelpers.OpenTestCamera();
        Assert.True(camera.GetPayloadSize() > 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (IntPtr[] Objects, long[] Ids, WeakReference[] Wrappers) AbandonCameraDeviceStreamAndBuffers()
    {
        var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var stream = camera.CreateStream();
        int payload = (int)camera.GetPayloadSize();
        var buffer = new AravisBuffer(payload);
        stream.PushBuffer(new AravisBuffer(payload));
        Assert.True(device.NodeMap.GetIntegerFeature("Width") > 0);

        var objects = new[]
        {
            GLibNative.g_object_ref(camera.Handle),
            GLibNative.g_object_ref(stream.Handle),
            GLibNative.g_object_ref(buffer.Handle),
        };
        var ids = objects.Select(GObjectReleaseProbe.Watch).ToArray();
        var wrappers = new WeakReference[]
        {
            new(camera), new(device), new(device.NodeMap), new(stream), new(buffer),
        };
        return (objects, ids, wrappers);
    }

    private static void DisposeFromManyThreads(IDisposable disposable)
    {
        using var barrier = new Barrier(Threads);
        var tasks = Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(30));
            disposable.Dispose();
        })).ToArray();
        Assert.True(Task.WaitAll(tasks, TimeSpan.FromSeconds(30)), "Dispose() did not return");
    }

    #endregion
}
