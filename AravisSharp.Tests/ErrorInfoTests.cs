using System;
using System.Runtime.InteropServices;
using AravisSharp.GenICam;
using AravisSharp.Native;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// #10 AC-3: <see cref="AravisException"/> carries the domain and code of the native GError,
/// read from real GErrors that Aravis sets on the test camera (the fake camera by default).
/// </summary>
public class ErrorInfoTests
{
    private const string MissingFeature = "NoSuchFeature_AravisSharpTests";

    // aravis/src/arvdevice.{h,c}: ARV_DEVICE_ERROR and its enum.
    private const string DeviceErrorDomain = "arv-device-error-quark";
    private const int DeviceErrorFeatureNotFound = 1; // ARV_DEVICE_ERROR_FEATURE_NOT_FOUND
    private const int DeviceErrorNotFound = 6;        // ARV_DEVICE_ERROR_NOT_FOUND

    // aravis/src/arvgc.{h,c}: ARV_GC_ERROR and its enum.
    private const string GcErrorDomain = "arv-gc-error-quark";
    private const int GcErrorInvalidSyntax = 14;      // ARV_GC_ERROR_INVALID_SYNTAX

    // See CameraLifecycleTests.MissingDeviceId.
    private const string MissingDeviceId = "AravisSharp test: no such device";

    #region AC-3

    [Fact]
    public void AravisException_WithoutGError_ShouldLeaveDomainAndCodeEmpty()
    {
        var plain = new AravisException("boom");
        var wrapped = new AravisException("boom", new InvalidOperationException("inner"));

        Assert.Null(plain.Domain);
        Assert.Null(plain.Code);
        Assert.Null(wrapped.Domain);
        Assert.Null(wrapped.Code);
    }

    [NativeFact]
    public void Camera_UnknownFeature_ShouldCarryTheGErrorDomainAndCode()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();

        var ex = Assert.Throws<AravisException>(() => camera.GetIntegerFeature(MissingFeature));

        Assert.Contains(MissingFeature, ex.Message);
        Assert.Equal(DeviceErrorDomain, ex.Domain);
        Assert.Equal(DeviceErrorFeatureNotFound, ex.Code);
    }

    [NativeFact]
    public void CameraConstructor_UnknownDevice_ShouldCarryTheGErrorDomainAndCode()
    {
        var ex = Assert.Throws<AravisException>(() => new Camera(MissingDeviceId));

        Assert.Equal(DeviceErrorDomain, ex.Domain);
        Assert.Equal(DeviceErrorNotFound, ex.Code);
    }

    [NativeFact]
    public void Device_UnknownFeature_ShouldCarryTheGErrorDomainAndCode()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();

        var errors = new[]
        {
            Assert.Throws<AravisException>(() => device.GetIntegerFeature(MissingFeature)),
            Assert.Throws<AravisException>(() => device.GetStringFeature(MissingFeature)),
            Assert.Throws<AravisException>(() => device.SetIntegerFeature(MissingFeature, 1)),
        };

        Assert.All(errors, ex =>
        {
            Assert.Contains(MissingFeature, ex.Message);
            Assert.Equal(DeviceErrorDomain, ex.Domain);
            Assert.Equal(DeviceErrorFeatureNotFound, ex.Code);
        });
    }

    [NativeFact]
    public void GenICamNode_InvalidValueString_ShouldCarryTheGErrorDomainAndCode()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var genicam = AravisNative.arv_device_get_genicam(device.Handle);
        Assert.NotEqual(IntPtr.Zero, genicam);
        var namePtr = Marshal.StringToCoTaskMemUTF8("Width");
        IntPtr nodePtr;
        try
        {
            nodePtr = AravisNative.arv_gc_get_node(genicam, namePtr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(namePtr);
        }
        Assert.NotEqual(IntPtr.Zero, nodePtr);
        using var node = new GenICamNode(nodePtr, genicam);

        // Width is an integer node: Aravis refuses the string before touching the device.
        var ex = Assert.Throws<AravisException>(() => node.SetValueFromString("not a number"));

        Assert.Equal(GcErrorDomain, ex.Domain);
        Assert.Equal(GcErrorInvalidSyntax, ex.Code);
        GC.KeepAlive(device);
    }

    [NativeFact]
    public void NodeMap_UnknownFeature_ShouldKeepItsExceptionTypeAndCarryTheGError()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var nodeMap = device.NodeMap;

        var ex = Assert.Throws<InvalidOperationException>(() => nodeMap.GetIntegerFeature(MissingFeature));

        var native = Assert.IsType<AravisException>(ex.InnerException);
        Assert.Equal(DeviceErrorDomain, native.Domain);
        Assert.Equal(DeviceErrorFeatureNotFound, native.Code);
        Assert.Contains(native.Message, ex.Message);
    }

    #endregion
}
