using System.Reflection;
using AravisSharp.Native;
using Xunit.Abstractions;

namespace AravisSharp.Tests;

/// <summary>
/// Every P/Invoke declaration of <see cref="AravisNative"/> and <see cref="GLibNative"/> against
/// its C prototype in the Aravis and GLib headers (issue #11). The comparison rules are those of
/// <see cref="PInvokeSignatureCheck"/>; the header reader is <see cref="HeaderModel"/>; where the
/// headers come from is <see cref="HeaderSources"/>. No native library is loaded.
/// </summary>
public class PInvokeHeaderTests(ITestOutputHelper output)
{
    /// <summary>
    /// Entry points deliberately left unchecked, each with the reason. Empty: every declaration
    /// is compared. An entry that is not needed any more fails the test.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> AllowList = new Dictionary<string, string>(StringComparer.Ordinal);

    private static readonly Type[] DeclaringTypes = [typeof(AravisNative), typeof(GLibNative)];

    [HeaderFact]
    public void EveryDeclaration_MatchesItsCPrototype()
    {
        var sources = HeaderSources.Current;
        if (sources.Problem is not null)
            Assert.Fail("C headers not found, and missing headers fail the check on GitHub Actions: " + sources.Problem);

        var model = sources.Load();
        var declarations = DeclaringTypes.SelectMany(PInvokeSignatureCheck.Imports).ToList();
        output.WriteLine($"Aravis headers: {sources.AravisDirectory}; GLib headers: {string.Join(", ", sources.GLibDirectories)}");
        output.WriteLine($"{model.FunctionCount} C prototypes read; {declarations.Count} declarations checked.");

        var failures = PInvokeSignatureCheck.Check(declarations, model, AllowList);

        Assert.True(failures.Count == 0,
            $"{failures.Count} P/Invoke declaration problem(s):{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [Fact]
    public void Imports_FindsTheDeclarationsOfBothClasses_PublicAndPrivate()
    {
        var aravis = PInvokeSignatureCheck.Imports(typeof(AravisNative)).Select(PInvokeSignatureCheck.EntryPoint).ToList();
        var glib = PInvokeSignatureCheck.Imports(typeof(GLibNative)).Select(PInvokeSignatureCheck.EntryPoint).ToList();

        Assert.Contains("arv_camera_new", aravis);
        Assert.Contains("arv_camera_gv_get_packet_size", aravis);
        Assert.Contains("g_object_unref", glib);
        Assert.Contains("g_value_get_boolean", glib); // private
        Assert.Contains("g_quark_to_string", glib);   // internal
        Assert.Equal(aravis.Count, aravis.Distinct().Count());
    }
}
