using System.Reflection;
using AravisSharp.Native;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #7, AC-7 and AC-8: reads the library sources (AravisSharp/**/*.cs, syntax only, no
/// native library) and fails when a member reaches native code outside a lease, reads a
/// native pointer other than a lease's, reads buffer memory outside <see cref="Buffer"/>, or
/// swallows exceptions with an unfiltered <c>catch</c>.
/// </summary>
/// <remarks>
/// <para>
/// A lease is a <c>using</c> declaration or statement whose variable is declared as
/// <c>GObjectLease</c>, or whose initializer calls one of the lease factories
/// (<see cref="LeaseCheck.LeaseFactories"/>): it pins the native object, so neither
/// <c>Dispose()</c> on another thread nor a finalizer can release it during the call. A
/// native call is covered when it lies in the scope of such a lease in the same function:
/// lambdas, anonymous methods and local functions are boundaries, since they can run after
/// the lease is returned.
/// </para>
/// <para>
/// Native calls are the invocations of <c>AravisNative</c> and <c>GLibNative</c> members,
/// however the class is named (qualified, <c>global::</c>). Functions that take no object
/// (<see cref="LeaseCheck.ObjectFreeFunctions"/>, <c>*_get_type</c>) are not checked. A
/// method of those classes referenced without being called (a method group) is always
/// reported, and so is a <c>using static</c> or alias directive naming them. The two classes'
/// own files are the P/Invoke layer and are not scanned.
/// </para>
/// </remarks>
public class LeaseCoverageTests
{
    /// <summary>
    /// Members that reach native code without a lease: the number of such calls, and why.
    /// The test fails when a member's count differs from its entry (a new call, or a stale
    /// entry), so the list cannot grow or go stale silently.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (int Count, string Reason)> NativeCallAllowList =
        new Dictionary<string, (int, string)>(StringComparer.Ordinal)
        {
            ["CameraDiscovery.UpdateDeviceList()"] = (1, "Module-level device list: no object to pin; serialised by the class lock."),
            ["CameraDiscovery.GetDeviceCount()"] = (1, "Module-level device list: no object to pin; serialised by the class lock."),
            ["CameraDiscovery.GetCameraInfo(uint)"] = (6, "Module-level device list: no object to pin; serialised by the class lock."),
            ["AravisLibrary.GetNativeVersion()"] = (3, "Library version numbers: no object."),
            ["AravisLibrary.IsAravisAvailable()"] = (1, "Loads the library through a version call: no object."),
            ["Camera..ctor(string?)"] = (2, "arv_camera_new creates the object the handle then owns: nothing to pin yet; on an error it drops the reference it was given."),
            ["Camera.GInetAddressToString(IntPtr)"] = (1, "Static helper on a GInetAddress this call owns (transfer full out parameter), called under the camera's lease."),
            ["Camera.GInetAddressMaskToDotted(IntPtr)"] = (1, "Static helper on a GInetAddressMask this call owns, called under the camera's lease."),
            ["Buffer..ctor(IntPtr)"] = (1, "arv_buffer_new_allocate creates the object the handle then owns: nothing to pin yet."),
            ["GObjectSafeHandle.ReleaseHandle()"] = (1, "The release itself: SafeHandle runs it once the last lease is returned."),
            ["FeatureDetails.FromNode(IntPtr, string)"] = (16, "Public static helper taking a raw device pointer: the library calls it only from NodeMap.GetFeatureDetails, under the camera's lease."),
            ["FeatureDetails.DetermineTypeAndConstraints(IntPtr, string, IntPtr, FeatureDetails)"] = (1, "Local function Is() of a private helper of FromNode, called synchronously under the same lease."),
            ["FeatureDetails.ReadIntegerConstraints(IntPtr, IntPtr, FeatureDetails)"] = (2, "Private helper of FromNode, same lease."),
            ["FeatureDetails.ReadFloatConstraints(IntPtr, IntPtr, FeatureDetails)"] = (2, "Private helper of FromNode, same lease."),
            ["FeatureDetails.ReadEnumerationChoices(IntPtr, IntPtr, FeatureDetails)"] = (2, "Private helper of FromNode, same lease."),
        };

    /// <summary>Unfiltered catch blocks that do not rethrow, each with the reason.</summary>
    private static readonly IReadOnlyDictionary<string, (int Count, string Reason)> CatchAllowList =
        new Dictionary<string, (int, string)>(StringComparer.Ordinal)
        {
            ["AravisLibrary.IsAravisAvailable()"] = (1, "Availability probe: the loading failures are caught above it; any other failure means the library was found."),
        };

    [Fact]
    public void EveryNativeCall_InTheLibrary_IsMadeUnderALease()
    {
        var findings = LibrarySyntaxTrees().SelectMany(LeaseCheck.UncoveredNativeCalls).ToList();
        AssertMatchesAllowList(findings, NativeCallAllowList, "native call(s) outside a lease");
    }

    [Fact]
    public void TheLibrary_NeverNamesTheNativeClassesInAWayTheScanCannotSee()
    {
        var findings = LibrarySyntaxTrees().SelectMany(LeaseCheck.HiddenNativeReferences).ToList();
        Assert.True(findings.Count == 0,
            "using static / alias directives or method groups of the native classes:" + Environment.NewLine +
            string.Join(Environment.NewLine, findings.Select(f => $"{f.Member} ({f.Where})")));
    }

    [Fact]
    public void TheLibrary_ReadsNativePointersOnlyFromALease()
    {
        var findings = LibrarySyntaxTrees().SelectMany(LeaseCheck.HandleReadsOutsideALease).ToList();
        Assert.True(findings.Count == 0,
            ".Handle read on something other than a lease:" + Environment.NewLine +
            string.Join(Environment.NewLine, findings.Select(f => $"{f.Member} ({f.Where})")));
    }

    [Fact]
    public void TheLibrary_ReadsBufferMemoryOnlyInsideBuffer()
    {
        // GetData() and GetDataSpan() hand out pointers into the native buffer that no lease
        // covers once they return: library code outside Buffer goes through a Buffer member
        // that holds the lease while it reads.
        var findings = LibrarySyntaxTrees()
            .Where(tree => Path.GetFileName(tree.FilePath) != "Buffer.cs")
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Where(call => call.Expression is MemberAccessExpressionSyntax access &&
                           access.Name.Identifier.ValueText is "GetData" or "GetDataSpan")
            .Select(call => $"{LeaseCheck.MemberKey(call)} ({LeaseCheck.Location(call)})")
            .ToList();
        Assert.True(findings.Count == 0, "Buffer memory read outside Buffer:" + Environment.NewLine + string.Join(Environment.NewLine, findings));
    }

    [Fact]
    public void TheLibrary_HasNoUnfilteredCatchThatSwallows()
    {
        var findings = LibrarySyntaxTrees().SelectMany(LeaseCheck.SwallowingCatches).ToList();
        AssertMatchesAllowList(findings, CatchAllowList, "unfiltered catch block(s)");
    }

    // The checker itself, on small samples.
    [Theory]
    [InlineData("void M() { AravisNative.arv_camera_get_width(_handle); }", 1)]
    [InlineData("void M() { using var lease = AcquireHandle(); AravisNative.arv_camera_get_width(lease.Handle); }", 0)]
    [InlineData("void M() { using (var lease = _h.Acquire(\"x\")) { AravisNative.arv_camera_get_width(lease.Handle); } }", 0)]
    [InlineData("void M() { using var lease = _h.AcquireBorrowed(p, \"x\"); AravisNative.arv_device_get_genicam(lease.Handle); }", 0)]
    [InlineData("void M() { using var lease = AcquireOwner(); AravisNative.arv_gc_feature_node_get_name(lease.Handle); }", 0)]
    [InlineData("void M() { using GObjectLease lease = Make(); AravisNative.arv_camera_get_width(lease.Handle); }", 0)]
    [InlineData("void M() { using (var lease = _h.Acquire(\"x\")) { } AravisNative.arv_camera_get_width(p); }", 1)]
    [InlineData("void M() { AravisNative.arv_camera_get_width(p); using var lease = AcquireHandle(); }", 1)]
    [InlineData("void M() { using var other = Open(); AravisNative.arv_camera_get_width(p); }", 1)]
    [InlineData("void M() { using var other = AcquireSomething(); AravisNative.arv_camera_get_width(p); }", 1)]
    [InlineData("void M() { using var lease = AcquireHandle(); Action a = () => AravisNative.arv_camera_get_width(lease.Handle); }", 1)]
    [InlineData("void M() { using var lease = AcquireHandle(); Action a = delegate { AravisNative.arv_camera_get_width(p); }; }", 1)]
    [InlineData("void M() { using var lease = AcquireHandle(); void F() => AravisNative.arv_camera_get_width(p); F(); }", 1)]
    [InlineData("void M() { Action a = () => { using var lease = AcquireHandle(); AravisNative.arv_camera_get_width(lease.Handle); }; }", 0)]
    [InlineData("void M() { global::AravisSharp.Native.AravisNative.arv_camera_get_width(p); }", 1)]
    [InlineData("void M() { AravisSharp.Native.AravisNative.arv_camera_get_width(p); }", 1)]
    [InlineData("void M() { GLibNative.g_object_ref(p); }", 1)]
    [InlineData("void M() { GLibNative.g_type_check_instance_is_a(p, t); }", 1)]
    [InlineData("void M() { GLibNative.g_free(p); GLibNative.g_error_free(e); GLibNative.ClearError(ref e); }", 0)]
    [InlineData("bool M() => Is(AravisNative.arv_gc_integer_get_type());", 0)]
    public void LeaseCheck_FindsNativeCallsOutsideALease(string member, int expected)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ {member} }}");
        Assert.Equal(expected, LeaseCheck.UncoveredNativeCalls(tree).Count());
    }

    [Theory]
    [InlineData("using static AravisSharp.Native.AravisNative; class C { void M() { arv_camera_get_width(p); } }", 1)]
    [InlineData("using N = AravisSharp.Native.GLibNative; class C { void M() { N.g_object_ref(p); } }", 1)]
    [InlineData("using global::AravisSharp.Native; class C { void M() { } }", 0)]
    [InlineData("class C { void M() { Func<IntPtr, uint> f = AravisNative.arv_camera_get_payload; } }", 1)]
    [InlineData("class C { void M() { using var lease = AcquireHandle(); Action<IntPtr> f = GLibNative.g_object_unref; } }", 1)]
    [InlineData("class C { string M() => AravisNative.LibraryName; }", 0)]
    public void LeaseCheck_FindsNativeReferencesTheCallScanCannotSee(string source, int expected)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        Assert.Equal(expected, LeaseCheck.HiddenNativeReferences(tree).Count());
    }

    [Theory]
    [InlineData("void M() { using var lease = AcquireHandle(); F(lease.Handle); }", 0)]
    [InlineData("void M() { using (var pin = _h.Acquire(\"x\")) { F(pin.Handle); } }", 0)]
    [InlineData("void M() { F(buffer.Handle); }", 1)]
    [InlineData("void M() { using var lease = AcquireHandle(); F(buffer.Handle); }", 1)]
    [InlineData("void M() { using var lease = AcquireHandle(); Action a = () => F(lease.Handle); }", 1)]
    [InlineData("IntPtr Handle { get { using var lease = AcquireHandle(); return lease.Handle; } }", 0)]
    public void LeaseCheck_FindsHandleReadsOutsideALease(string member, int expected)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ {member} }}");
        Assert.Equal(expected, LeaseCheck.HandleReadsOutsideALease(tree).Count());
    }

    [Theory]
    [InlineData("void M() { try { } catch { } }", 1)]
    [InlineData("void M() { try { } catch (Exception) { } }", 1)]
    [InlineData("void M() { try { } catch { Undo(); throw; } }", 0)]
    [InlineData("void M() { try { } catch (Exception ex) when (Ok(ex)) { } }", 0)]
    [InlineData("void M() { try { } catch (AravisException) { } }", 0)]
    public void LeaseCheck_FindsSwallowingCatches(string member, int expected)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ {member} }}");
        Assert.Equal(expected, LeaseCheck.SwallowingCatches(tree).Count());
    }

    [Fact]
    public void MemberKey_IsTheTypeTheMemberAndItsParameterTypes()
    {
        var tree = CSharpSyntaxTree.ParseText(
            "class C { C(string? id) { A(); } void M(IntPtr p, ref int n) { A(); } int P { get { A(); return 0; } } }");
        var keys = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Select(LeaseCheck.MemberKey);
        Assert.Equal(new[] { "C..ctor(string?)", "C.M(IntPtr, ref int)", "C.P" }, keys);
    }

    private static void AssertMatchesAllowList(
        IReadOnlyList<LeaseCheck.Finding> findings, IReadOnlyDictionary<string, (int Count, string Reason)> allowList, string what)
    {
        var byMember = findings.GroupBy(f => f.Member).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var problems = new List<string>();
        foreach (var (member, list) in byMember.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!allowList.TryGetValue(member, out var entry))
                problems.AddRange(list.Select(f => $"not allowed: {member} ({f.Where})"));
            else if (entry.Count != list.Count)
                problems.Add($"{member}: {list.Count} found, {entry.Count} allowed ({string.Join(", ", list.Select(f => f.Where))})");
        }
        problems.AddRange(allowList.Keys.Where(k => !byMember.ContainsKey(k)).Select(k => $"allow-list entry no longer needed: {k}"));

        Assert.True(problems.Count == 0, $"{what}:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    private static IEnumerable<SyntaxTree> LibrarySyntaxTrees()
    {
        var library = Path.Combine(FindRepositoryRoot(), "AravisSharp");
        var files = Directory.GetFiles(library, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .ToList();
        Assert.True(files.Count >= 10, $"Only {files.Count} source files found under {library}.");
        return files
            .Where(f => Path.GetFileName(f) is not ("AravisNative.cs" or "GLibNative.cs"))
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));
    }

    // The test assembly runs from AravisSharp.Tests/bin/<config>/<tfm>/: walk up to the
    // folder that holds both AravisSharp.slnx and AravisSharp/AravisSharp.csproj.
    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AravisSharp.slnx")) &&
                File.Exists(Path.Combine(dir.FullName, "AravisSharp", "AravisSharp.csproj")))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException(
            $"No AravisSharp.slnx with an AravisSharp/AravisSharp.csproj above {AppContext.BaseDirectory}.");
    }
}

/// <summary>The syntax rules of <see cref="LeaseCoverageTests"/>.</summary>
internal static class LeaseCheck
{
    public sealed record Finding(string Member, string Where);

    private static readonly string[] NativeClasses = { nameof(AravisNative), nameof(GLibNative) };

    /// <summary>The methods that return a lease.</summary>
    public static readonly IReadOnlySet<string> LeaseFactories =
        new HashSet<string>(StringComparer.Ordinal) { "Acquire", "AcquireBorrowed", "AcquireHandle", "AcquireOwner" };

    /// <summary>Native functions that take no GObject, so need no lease.</summary>
    public static readonly IReadOnlySet<string> ObjectFreeFunctions = new HashSet<string>(StringComparer.Ordinal)
    {
        "g_free",           // frees plain memory a call returned
        "g_error_free",     // frees a GError a call returned
        "ClearError",       // g_error_free when set
        "g_quark_to_string" // a GQuark is an integer
    };

    // Every method name of the two native classes: a member access to one of them that is not
    // invoked is a method group.
    private static readonly IReadOnlySet<string> NativeMethodNames = typeof(AravisNative)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Concat(typeof(GLibNative).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        .Select(m => m.Name)
        .ToHashSet(StringComparer.Ordinal);

    public static IEnumerable<Finding> UncoveredNativeCalls(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(IsCheckedNativeCall)
            .Where(call => !IsInLeaseScope(call, leaseName: null))
            .Select(call => new Finding(MemberKey(call), Location(call)));

    public static IEnumerable<Finding> HiddenNativeReferences(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var directives = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(u => (u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) || u.Alias is not null) &&
                        NativeClasses.Contains(RightmostName(u.NamespaceOrType)))
            .Select(u => new Finding("using " + u.NamespaceOrType, Location(u)));

        var methodGroups = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Where(access => IsNativeClass(access.Expression) &&
                             NativeMethodNames.Contains(access.Name.Identifier.ValueText) &&
                             !(access.Parent is InvocationExpressionSyntax invocation && invocation.Expression == access))
            .Select(access => new Finding(MemberKey(access), Location(access)));

        return directives.Concat(methodGroups);
    }

    public static IEnumerable<Finding> HandleReadsOutsideALease(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Where(access => access.Name.Identifier.ValueText == "Handle")
            .Where(access => access.Expression is not IdentifierNameSyntax name ||
                             !IsInLeaseScope(access, name.Identifier.ValueText))
            .Select(access => new Finding(MemberKey(access), Location(access)));

    public static IEnumerable<Finding> SwallowingCatches(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>()
            .Where(c => c.Filter is null &&
                        (c.Declaration is null || c.Declaration.Type.ToString() is "Exception" or "System.Exception"))
            .Where(c => !c.Block.DescendantNodes().OfType<ThrowStatementSyntax>().Any(t => t.Expression is null))
            .Select(c => new Finding(MemberKey(c), Location(c)));

    private static bool IsNativeClass(ExpressionSyntax expression) => NativeClasses.Contains(RightmostName(expression));

    private static string RightmostName(SyntaxNode node) => node switch
    {
        IdentifierNameSyntax id => id.Identifier.ValueText,
        QualifiedNameSyntax q => q.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
        MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
        _ => node.ToString(),
    };

    private static bool IsCheckedNativeCall(InvocationExpressionSyntax call)
    {
        if (call.Expression is not MemberAccessExpressionSyntax access || !IsNativeClass(access.Expression))
            return false;
        var name = access.Name.Identifier.ValueText;
        return !ObjectFreeFunctions.Contains(name) && !name.EndsWith("_get_type", StringComparison.Ordinal);
    }

    /// <summary>
    /// True when <paramref name="node"/> lies in the scope of a lease of the same function
    /// (named <paramref name="leaseName"/> when given).
    /// </summary>
    private static bool IsInLeaseScope(SyntaxNode node, string? leaseName)
    {
        foreach (var ancestor in node.Ancestors())
        {
            // using (var lease = x.Acquire…()) { … }
            if (ancestor is UsingStatementSyntax usingStatement &&
                usingStatement.Statement.Span.Contains(node.Span) &&
                usingStatement.Declaration is { } usingDeclaration &&
                DeclaresLease(usingDeclaration, leaseName))
                return true;

            // using var lease = x.Acquire…(); …  (the rest of the enclosing block)
            if (ancestor is BlockSyntax block)
            {
                foreach (var statement in block.Statements)
                {
                    if (statement.SpanStart >= node.SpanStart)
                        break;
                    if (statement is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: (int)SyntaxKind.UsingKeyword } declaration &&
                        DeclaresLease(declaration.Declaration, leaseName))
                        return true;
                }
            }

            // A lambda, anonymous method or local function may run after the lease is returned.
            if (ancestor is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax)
                return false;
        }
        return false;
    }

    private static bool DeclaresLease(VariableDeclarationSyntax declaration, string? leaseName)
    {
        bool typedAsLease = RightmostName(declaration.Type) == "GObjectLease";
        return declaration.Variables.Any(v =>
            (leaseName is null || v.Identifier.ValueText == leaseName) &&
            (typedAsLease || (v.Initializer?.Value is InvocationExpressionSyntax invocation && IsLeaseFactory(invocation))));
    }

    private static bool IsLeaseFactory(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        IdentifierNameSyntax name => LeaseFactories.Contains(name.Identifier.ValueText),
        MemberAccessExpressionSyntax access => LeaseFactories.Contains(access.Name.Identifier.ValueText),
        _ => false,
    };

    /// <summary>"Type.Member(parameter types)" of the member holding <paramref name="node"/>; properties have no list.</summary>
    public static string MemberKey(SyntaxNode node)
    {
        var member = node.Ancestors().OfType<MemberDeclarationSyntax>()
            .First(m => m is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax or FieldDeclarationSyntax);
        var type = member.Ancestors().OfType<BaseTypeDeclarationSyntax>().First().Identifier.ValueText;
        string name = member switch
        {
            ConstructorDeclarationSyntax c => ".ctor" + Parameters(c.ParameterList),
            DestructorDeclarationSyntax => ".dtor()",
            MethodDeclarationSyntax method => method.Identifier.ValueText + Parameters(method.ParameterList),
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax indexer => "this" + Parameters(indexer.ParameterList),
            EventDeclarationSyntax e => e.Identifier.ValueText,
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.ValueText + Parameters(op.ParameterList),
            ConversionOperatorDeclarationSyntax conversion => "operator " + conversion.Type + Parameters(conversion.ParameterList),
            FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.ValueText,
            _ => member.Kind().ToString(),
        };
        return $"{type}.{name}";
    }

    private static string Parameters(BaseParameterListSyntax list) =>
        "(" + string.Join(", ", list.Parameters.Select(p =>
            string.Concat(p.Modifiers.Select(m => m.ValueText + " ")) + p.Type)) + ")";

    public static string Location(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return $"{Path.GetFileName(span.Path)}:{span.StartLinePosition.Line + 1}";
    }
}
