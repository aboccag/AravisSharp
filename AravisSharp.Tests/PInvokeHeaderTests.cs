using System.Reflection;
using AravisSharp.Native;
using Xunit.Abstractions;

namespace AravisSharp.Tests;

/// <summary>
/// Every P/Invoke declaration of the AravisSharp assembly (in practice <see cref="AravisNative"/>
/// and <see cref="GLibNative"/>) against its C prototype in the Aravis and GLib headers (issue #11). The comparison rules are those of
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

    /// <summary>Every DllImport and LibraryImport of the library, nested and private types included.</summary>
    private static IReadOnlyList<MethodInfo> LibraryImports => PInvokeSignatureCheck.Imports(typeof(AravisNative).Assembly);

    [HeaderFact]
    public void EveryDeclaration_MatchesItsCPrototype()
    {
        var sources = HeaderSources.Current;
        if (sources.Problem is not null)
            Assert.Fail("C headers not found, and missing headers fail the check on GitHub Actions: " + sources.Problem);

        var model = sources.Load();
        var declarations = LibraryImports;
        Assert.NotEmpty(declarations);
        output.WriteLine($"Aravis headers: {sources.AravisDirectory}; GLib headers: {string.Join(", ", sources.GLibDirectories)}");
        if (sources.GLibNote is not null)
            output.WriteLine("pkg-config: " + sources.GLibNote);
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
        Assert.True(aravis.Count >= 200, $"{aravis.Count} Aravis declarations, fewer than the 200 of v0.8.36.");
        Assert.True(glib.Count >= 30, $"{glib.Count} GLib declarations, fewer than the 30 of v0.8.36.");
        Assert.Equal(aravis.Count, aravis.Distinct().Count());
        Assert.Equal(glib.Count, glib.Distinct().Count());
    }

    [Fact]
    public void LibraryImports_AreExactlyTheDeclarationsOfAravisNativeAndGLibNative()
    {
        // The check covers the whole assembly; a declaration added to another class is checked
        // too, and this test names it so that the documentation of the check can follow.
        var expected = new[] { typeof(AravisNative), typeof(GLibNative) }.SelectMany(PInvokeSignatureCheck.Imports).ToHashSet();

        Assert.NotEmpty(LibraryImports);
        Assert.Equal(
            expected.Select(Name).Order(StringComparer.Ordinal),
            LibraryImports.Select(Name).Order(StringComparer.Ordinal));

        static string Name(MethodInfo m) => $"{m.DeclaringType?.FullName}.{m.Name}";
    }

    [Fact]
    public void ImportsOfAnAssembly_IncludeNestedPrivateTypesAndLibraryImports()
    {
        var names = PInvokeSignatureCheck.Imports(typeof(PInvokeHeaderTests).Assembly).Select(m => m.Name).ToList();

        Assert.Contains("x_uint", names);                        // HeaderPrototypesTests.Fixtures, nested and private
        Assert.Contains("LibSetBool_MarshalledAsBool", names);   // a LibraryImport
        Assert.DoesNotContain(names, n => n.Contains('<'));      // not the extern its generated stub calls
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, false)]
    [InlineData("no headers", false, true)]
    [InlineData("no headers", true, false)]
    public void SkipReason_SkipsOnlyOutsideCi_WhenHeadersAreMissing(string? problem, bool isCi, bool skipped)
    {
        var reason = HeaderSources.SkipReason(problem, isCi);

        Assert.Equal(skipped, reason is not null);
        if (skipped)
            Assert.Contains(problem!, reason);
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("true", null, true)]
    [InlineData("TRUE", null, true)]
    [InlineData(null, "true", true)]
    [InlineData(null, "True", true)]
    [InlineData("false", "false", false)]
    [InlineData("1", "", false)]
    public void IsCiEnvironment_IsTrueForGitHubActionsOrCi(string? githubActions, string? ci, bool expected) =>
        Assert.Equal(expected, HeaderSources.IsCiEnvironment(githubActions, ci));
}
