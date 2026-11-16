using System;
using System.Runtime.InteropServices;
using AravisSharp.GenICam;
using AravisSharp.Native;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// #10 AC-3: <see cref="AravisException"/> carries the domain and code of the native GError,
/// read from real GErrors that Aravis sets on the test camera (the fake camera by default).
/// #10 AC-4: the catch blocks of <see cref="NodeMap"/> only absorb Aravis errors.
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

    // A GError built here, so the domain, the code 0 and the message are known exactly.
    // g_error_new_literal is not variadic (g_error_new is: see InteropPinTests B8).
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint QuarkFromStringFn([MarshalAs(UnmanagedType.LPUTF8Str)] string text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr ErrorNewLiteralFn(uint domain, int code, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

    [NativeFact]
    public void TakeGError_ShouldCopyDomainCodeAndMessageAndZeroThePointer()
    {
        var glib = AravisLibrary.ResolveDllImport(GLibNative.GLibLibraryName, typeof(GLibNative).Assembly, null);
        Assert.NotEqual(IntPtr.Zero, glib);
        var quarkFromString = Marshal.GetDelegateForFunctionPointer<QuarkFromStringFn>(
            NativeLibrary.GetExport(glib, "g_quark_from_string"));
        var errorNewLiteral = Marshal.GetDelegateForFunctionPointer<ErrorNewLiteralFn>(
            NativeLibrary.GetExport(glib, "g_error_new_literal"));

        var error = errorNewLiteral(quarkFromString("aravissharp-test"), 0, "m");
        Assert.NotEqual(IntPtr.Zero, error);

        // TakeGError frees the GError: no g_error_free here.
        var ex = AravisException.TakeGError(ref error);

        Assert.Equal(IntPtr.Zero, error);
        Assert.Equal("aravissharp-test", ex.Domain);
        Assert.NotNull(ex.Code);
        Assert.Equal(0, ex.Code);
        Assert.Equal("m", ex.Message);
    }

    #endregion

    #region AC-4

    // Every catch in GenICam/ either names a type other than Exception, or filters with
    // `when`: a bare `catch` or `catch (Exception)` would swallow ObjectDisposedException,
    // OutOfMemoryException and marshalling errors again.
    private static readonly System.Text.RegularExpressions.Regex UnfilteredCatch = new(
        @"\bcatch\s*(?:\{|$|\(\s*(?:System\.)?Exception(?:\s+\w+)?\s*\)(?!\s*when\b))",
        System.Text.RegularExpressions.RegexOptions.Multiline);

    [Fact]
    public void GenICamSources_ShouldHaveNoUnfilteredCatch()
    {
        var genicam = Path.Combine(FindRepositoryRoot(), "AravisSharp", "GenICam");
        var files = Directory.GetFiles(genicam, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        var offenders = files
            .SelectMany(file => File.ReadAllLines(file).Select((line, index) => (file, line, index)))
            .Where(l => !l.line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .Where(l => UnfilteredCatch.IsMatch(StripLineComment(l.line)))
            .Select(l => $"{Path.GetFileName(l.file)}:{l.index + 1}: {l.line.Trim()}")
            .ToList();

        Assert.True(offenders.Count == 0, "Unfiltered catch blocks:\n" + string.Join("\n", offenders));
    }

    [Theory]
    [InlineData("catch", true)]
    [InlineData("        catch {", true)]
    [InlineData("catch (Exception)", true)]
    [InlineData("catch (System.Exception ex)", true)]
    [InlineData("catch (Exception ex) when (IsOptionalMetadataFailure(ex))", false)]
    [InlineData("catch (AravisException)", false)]
    [InlineData("catch (EntryPointNotFoundException ex)", false)]
    public void UnfilteredCatchPattern_ShouldMatchOnlyUnfilteredCatches(string line, bool expected)
    {
        Assert.Equal(expected, UnfilteredCatch.IsMatch(line));
    }

    private static string StripLineComment(string line)
    {
        var index = line.IndexOf("//", StringComparison.Ordinal);
        return index < 0 ? line : line[..index];
    }

    // The test assembly runs from AravisSharp.Tests/bin/<config>/<tfm>/: walk up to the
    // folder that holds both AravisSharp.slnx and AravisSharp/GenICam.
    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AravisSharp.slnx")) &&
                Directory.Exists(Path.Combine(dir.FullName, "AravisSharp", "GenICam")))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException(
            $"No AravisSharp.slnx with an AravisSharp/GenICam folder above {AppContext.BaseDirectory}.");
    }

    [Fact]
    public void OptionalMetadataFilter_ShouldAbsorbOnlyAravisErrorsAndMissingEntryPoints()
    {
        Assert.True(FeatureDetails.IsOptionalMetadataFailure(new AravisException("boom")));
        Assert.True(FeatureDetails.IsOptionalMetadataFailure(new EntryPointNotFoundException()));

        Assert.False(FeatureDetails.IsOptionalMetadataFailure(new ObjectDisposedException("Device")));
        Assert.False(FeatureDetails.IsOptionalMetadataFailure(new OutOfMemoryException()));
        Assert.False(FeatureDetails.IsOptionalMetadataFailure(new MarshalDirectiveException()));
        Assert.False(FeatureDetails.IsOptionalMetadataFailure(new DllNotFoundException()));
    }

    [NativeFact]
    public void NodeMap_UnknownFeature_ShouldStillReturnNullOrEmpty()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        using var device = camera.GetDevice();
        var nodeMap = device.NodeMap;

        Assert.Null(nodeMap.GetStringFeature(MissingFeature));
        Assert.Empty(nodeMap.GetFeaturesInCategory(MissingFeature));
        var details = nodeMap.GetFeatureDetails(MissingFeature);
        Assert.NotNull(details);
        Assert.False(details!.IsImplemented);
    }

    [NativeFact]
    public void NodeMap_GetNode_OnDisposedDevice_ShouldThrowObjectDisposedException()
    {
        using var camera = CameraTestHelpers.OpenTestCamera();
        var device = camera.GetDevice();
        var nodeMap = device.NodeMap;
        Assert.NotNull(nodeMap.GetNode("Width"));

        device.Dispose();

        // Swallowed by a bare catch before #10: GetNode returned null as if the feature
        // did not exist.
        Assert.Throws<ObjectDisposedException>(() => nodeMap.GetNode("Width"));
    }

    #endregion
}
