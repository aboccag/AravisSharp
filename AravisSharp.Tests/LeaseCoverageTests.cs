using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #7, AC-7 and AC-8: reads the library sources (AravisSharp/**/*.cs, syntax only, no
/// native library) and fails when a member calls into <c>AravisNative</c> outside a lease,
/// or swallows exceptions with an unfiltered <c>catch</c>.
/// </summary>
/// <remarks>
/// A lease is a <c>using</c> declaration or statement whose initializer calls a method named
/// <c>Acquire…</c> (<c>GObjectSafeHandle.Acquire</c> and the wrappers' <c>AcquireHandle</c>):
/// it pins the native object, so neither <c>Dispose()</c> on another thread nor a finalizer
/// can release it during the call. A native call is covered when it lies in the scope of
/// such a lease. Calls to <c>arv_*_get_type</c> are not checked: they return a static type
/// id and take no object.
/// </remarks>
public class LeaseCoverageTests
{
    /// <summary>
    /// Members that reach <c>AravisNative</c> without a lease, each with the reason. An entry
    /// that is not needed any more fails the test, so the list cannot go stale.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> NativeCallAllowList = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CameraDiscovery.UpdateDeviceList"] = "Module-level device list: no object to pin; serialised by the class lock.",
        ["CameraDiscovery.GetDeviceCount"] = "Module-level device list: no object to pin; serialised by the class lock.",
        ["CameraDiscovery.GetCameraInfo"] = "Module-level device list: no object to pin; serialised by the class lock.",
        ["AravisLibrary.GetNativeVersion"] = "Library version numbers: no object.",
        ["AravisLibrary.IsAravisAvailable"] = "Loads the library through a version call: no object.",
        ["Camera..ctor"] = "arv_camera_new creates the object the handle then owns: nothing to pin yet.",
        ["Buffer..ctor"] = "arv_buffer_new_allocate creates the object the handle then owns: nothing to pin yet.",
        ["FeatureDetails.FromNode"] = "Public static helper taking a raw device pointer: the library calls it only from NodeMap.GetFeatureDetails, under the camera's lease.",
        ["FeatureDetails.ReadIntegerConstraints"] = "Private helper of FromNode, same lease.",
        ["FeatureDetails.ReadFloatConstraints"] = "Private helper of FromNode, same lease.",
        ["FeatureDetails.ReadEnumerationChoices"] = "Private helper of FromNode, same lease.",
    };

    /// <summary>Unfiltered catch blocks that do not rethrow, each with the reason.</summary>
    private static readonly IReadOnlyDictionary<string, string> CatchAllowList = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AravisLibrary.IsAravisAvailable"] = "Availability probe: the loading failures are caught above it; any other failure means the library was found.",
    };

    [Fact]
    public void EveryNativeCall_InTheLibrary_IsMadeUnderALease()
    {
        var uncovered = LibrarySyntaxTrees()
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Where(IsCheckedNativeCall)
            .Where(call => !IsInLeaseScope(call))
            .Select(call => (Member: MemberKey(call), Where: Location(call)))
            .ToList();

        var unexpected = uncovered
            .Where(u => !NativeCallAllowList.ContainsKey(u.Member))
            .Select(u => $"{u.Member} ({u.Where})")
            .ToList();
        var stale = NativeCallAllowList.Keys.Except(uncovered.Select(u => u.Member)).ToList();

        Assert.True(unexpected.Count == 0,
            $"{unexpected.Count} native call(s) outside a lease:{Environment.NewLine}{string.Join(Environment.NewLine, unexpected)}");
        Assert.True(stale.Count == 0, "Allow-list entries no longer needed: " + string.Join(", ", stale));
    }

    [Fact]
    public void TheLibrary_HasNoUnfilteredCatchThatSwallows()
    {
        var swallowing = LibrarySyntaxTrees()
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>())
            .Where(IsUnfiltered)
            .Where(c => !c.Block.DescendantNodes().OfType<ThrowStatementSyntax>().Any(t => t.Expression is null))
            .Select(c => (Member: MemberKey(c), Where: Location(c)))
            .ToList();

        var unexpected = swallowing.Where(s => !CatchAllowList.ContainsKey(s.Member)).Select(s => $"{s.Member} ({s.Where})").ToList();
        var stale = CatchAllowList.Keys.Except(swallowing.Select(s => s.Member)).ToList();

        Assert.True(unexpected.Count == 0, "Unfiltered catch blocks:" + Environment.NewLine + string.Join(Environment.NewLine, unexpected));
        Assert.True(stale.Count == 0, "Allow-list entries no longer needed: " + string.Join(", ", stale));
    }

    // The checker itself, on small samples: it must see a call outside a lease and accept
    // one inside, in a using declaration as in a using statement.
    [Theory]
    [InlineData("void M() { AravisNative.arv_camera_get_width(_handle); }", false)]
    [InlineData("void M() { using var lease = AcquireHandle(); AravisNative.arv_camera_get_width(lease.Handle); }", true)]
    [InlineData("void M() { using (var lease = _h.Acquire(\"x\")) { AravisNative.arv_camera_get_width(lease.Handle); } }", true)]
    [InlineData("void M() { using (var lease = _h.Acquire(\"x\")) { } AravisNative.arv_camera_get_width(_handle); }", false)]
    [InlineData("void M() { AravisNative.arv_camera_get_width(_handle); using var lease = AcquireHandle(); }", false)]
    [InlineData("void M() { using var other = Open(); AravisNative.arv_camera_get_width(_handle); }", false)]
    [InlineData("bool M() => Is(AravisNative.arv_gc_integer_get_type());", true)]
    public void LeaseCheck_RecognisesLeases(string member, bool covered)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ {member} }}");
        var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single(IsNativeCall);

        Assert.Equal(covered, !IsCheckedNativeCall(call) || IsInLeaseScope(call));
    }

    private static bool IsNativeCall(InvocationExpressionSyntax call) =>
        call.Expression is MemberAccessExpressionSyntax access &&
        access.Expression.ToString() is "AravisNative" or "Native.AravisNative" or "AravisSharp.Native.AravisNative";

    private static bool IsCheckedNativeCall(InvocationExpressionSyntax call) =>
        IsNativeCall(call) &&
        !((MemberAccessExpressionSyntax)call.Expression).Name.Identifier.ValueText.EndsWith("_get_type", StringComparison.Ordinal);

    private static bool IsInLeaseScope(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            // using (var lease = x.Acquire…()) { … }
            if (ancestor is UsingStatementSyntax usingStatement &&
                usingStatement.Statement.Span.Contains(node.Span) &&
                IsLeaseInitializer((SyntaxNode?)usingStatement.Declaration ?? usingStatement.Expression))
                return true;

            // using var lease = x.Acquire…(); …  (the rest of the enclosing block)
            if (ancestor is BlockSyntax block)
            {
                foreach (var statement in block.Statements)
                {
                    if (statement.SpanStart >= node.SpanStart)
                        break;
                    if (statement is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: (int)SyntaxKind.UsingKeyword } declaration &&
                        IsLeaseInitializer(declaration.Declaration))
                        return true;
                }
            }

            if (ancestor is MemberDeclarationSyntax)
                return false;
        }
        return false;
    }

    private static bool IsLeaseInitializer(SyntaxNode? node) =>
        node is not null &&
        node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(i => i.Expression switch
        {
            IdentifierNameSyntax name => name.Identifier.ValueText.StartsWith("Acquire", StringComparison.Ordinal),
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText.StartsWith("Acquire", StringComparison.Ordinal),
            _ => false,
        });

    private static bool IsUnfiltered(CatchClauseSyntax clause) =>
        clause.Filter is null &&
        (clause.Declaration is null || clause.Declaration.Type.ToString() is "Exception" or "System.Exception");

    private static string MemberKey(SyntaxNode node)
    {
        var member = node.Ancestors().OfType<MemberDeclarationSyntax>()
            .First(m => m is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax or FieldDeclarationSyntax);
        var type = member.Ancestors().OfType<BaseTypeDeclarationSyntax>().First().Identifier.ValueText;
        string name = member switch
        {
            ConstructorDeclarationSyntax => ".ctor",
            DestructorDeclarationSyntax => ".dtor",
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            EventDeclarationSyntax e => e.Identifier.ValueText,
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.ValueText,
            ConversionOperatorDeclarationSyntax conversion => "operator " + conversion.Type,
            FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.ValueText,
            _ => member.Kind().ToString(),
        };
        return $"{type}.{name}";
    }

    private static string Location(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return $"{Path.GetFileName(span.Path)}:{span.StartLinePosition.Line + 1}";
    }

    private static IEnumerable<SyntaxTree> LibrarySyntaxTrees()
    {
        var library = Path.Combine(FindRepositoryRoot(), "AravisSharp");
        var files = Directory.GetFiles(library, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .ToList();
        Assert.True(files.Count >= 10, $"Only {files.Count} source files found under {library}.");
        return files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));
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
