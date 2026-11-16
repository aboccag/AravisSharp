using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AravisSharp.Tests;

/// <summary>
/// The ABI family of a C or C# type, as the P/Invoke header check compares them (issue #11).
/// Signed and unsigned integers of each width are distinct families. Every pointer (C <c>*</c>,
/// array, function pointer, callback typedef; C# <c>out</c>/<c>ref</c>, string, delegate, array,
/// <see cref="IntPtr"/>) is <see cref="Pointer"/>. <c>gsize</c>, <c>gssize</c>, <c>GType</c>,
/// <c>gintptr</c> and <c>guintptr</c> are <see cref="PointerSized"/>. <c>gboolean</c> is
/// <see cref="Bool32"/>. A C enum is <see cref="Enum32"/>. <c>glong</c>/<c>gulong</c> are
/// <see cref="CLong"/>/<see cref="CULong"/>, whose width differs between Windows and the other
/// targets.
/// </summary>
internal enum TypeFamily
{
    Void,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float32,
    Float64,
    Bool32,
    Bool8,
    Enum32,
    Pointer,
    PointerSized,
    CLong,
    CULong,
    Struct,
    Unknown,
}

/// <summary>
/// A C function prototype read from a header. <paramref name="Exported"/> is true when
/// <c>ARV_API</c> precedes it (or the <c>G_DECLARE_*</c> line that declares it), or when it is the
/// <c>get_type</c> function the build generates for a public Aravis enum.
/// </summary>
internal sealed record CPrototype(string Name, string ReturnType, IReadOnlyList<string> Parameters, bool IsVariadic, string Source, bool Exported = false)
{
    /// <summary>The prototype with its spacing normalised, to compare two declarations of one name.</summary>
    public string Signature =>
        $"{Normalize(ReturnType)} {Name} ({string.Join(", ", Parameters.Select(Normalize))}{(IsVariadic ? ", ..." : "")})";

    private static string Normalize(string type) =>
        string.Join(' ', type.Replace("*", " * ").Split(' ', StringSplitOptions.RemoveEmptyEntries)).Replace(" *", "*");
}

/// <summary>
/// The function prototypes and the type names (typedefs, enums, structs, callbacks) of a set of
/// C headers, read with a small regex reader made for the Aravis and GLib headers. It is not a C
/// parser: a prototype it cannot read shows up as a declaration with no prototype, which fails the
/// check, so a gap of the reader is a red test, never a silent pass.
/// </summary>
/// <remarks>
/// <para>Reading a header: line continuations are joined, comments and preprocessor lines are
/// dropped. Both branches of an <c>#if</c> are kept: the first prototype of a name is the one
/// <see cref="Find"/> returns, a second identical one only adds its <c>ARV_API</c> mark, and a
/// second different one is recorded and returned by <see cref="Conflicts"/>, which the check
/// reports.
/// <c>G_DECLARE_FINAL_TYPE</c>, <c>G_DECLARE_DERIVABLE_TYPE</c> and <c>G_DECLARE_INTERFACE</c>
/// declare <c>GType &lt;prefix&gt;_get_type (void)</c>. Every other all-capitals identifier, with
/// its parenthesised arguments if any, is a macro and is removed: <c>ARV_API</c>,
/// <c>GLIB_AVAILABLE_IN_*</c>, <c>GOBJECT_AVAILABLE_IN_*</c>, <c>GIO_AVAILABLE_IN_*</c>,
/// <c>G_GNUC_*</c>, <c>G_BEGIN_DECLS</c>; <c>ARV_API</c> is first recorded on the prototype it
/// precedes. A function name in parentheses (<c>void (g_free) (gpointer mem)</c>) is read as the
/// name. Inline function bodies are skipped.</para>
/// <para>The reader fails closed: a prototype whose argument parentheses do not balance, or with a
/// parenthesised argument that is not a function pointer (<c>void (*name) (...)</c>), such as a
/// lowercase <c>__attribute__((...))</c>, is left out, so its declaration reports no
/// prototype.</para>
/// <para>For a public Aravis header (<c>synthesizeEnumGetTypes</c>), each <c>typedef enum</c> also
/// declares <c>GType &lt;snake_name&gt;_get_type (void)</c>, as the <c>arvenumtypes.h</c> that
/// <c>gnome.mkenums_simple</c> generates at build time (<c>aravis/src/meson.build</c>) does.</para>
/// <para>Types: GLib's fixed-width names (<c>gint</c>, <c>guint32</c>, <c>gsize</c>,
/// <c>GType</c>, <c>gboolean</c>, ...) and the C keywords map to a family directly, so the
/// platform-specific <c>glibconfig.h</c> is never consulted for them. Other names are resolved
/// through the typedefs read from the headers.</para>
/// </remarks>
internal sealed partial class HeaderModel
{
    private static readonly Dictionary<string, TypeFamily> BuiltinTypes = new(StringComparer.Ordinal)
    {
        ["void"] = TypeFamily.Void,
        ["char"] = TypeFamily.Int8,
        ["signed char"] = TypeFamily.Int8,
        ["unsigned char"] = TypeFamily.UInt8,
        ["short"] = TypeFamily.Int16,
        ["short int"] = TypeFamily.Int16,
        ["signed short"] = TypeFamily.Int16,
        ["unsigned short"] = TypeFamily.UInt16,
        ["unsigned short int"] = TypeFamily.UInt16,
        ["int"] = TypeFamily.Int32,
        ["signed"] = TypeFamily.Int32,
        ["signed int"] = TypeFamily.Int32,
        ["unsigned"] = TypeFamily.UInt32,
        ["unsigned int"] = TypeFamily.UInt32,
        ["long"] = TypeFamily.CLong,
        ["long int"] = TypeFamily.CLong,
        ["signed long"] = TypeFamily.CLong,
        ["unsigned long"] = TypeFamily.CULong,
        ["unsigned long int"] = TypeFamily.CULong,
        ["long long"] = TypeFamily.Int64,
        ["long long int"] = TypeFamily.Int64,
        ["signed long long"] = TypeFamily.Int64,
        ["unsigned long long"] = TypeFamily.UInt64,
        ["unsigned long long int"] = TypeFamily.UInt64,
        ["float"] = TypeFamily.Float32,
        ["double"] = TypeFamily.Float64,
        ["_Bool"] = TypeFamily.Bool8,
        ["bool"] = TypeFamily.Bool8,
        ["size_t"] = TypeFamily.PointerSized,
        ["ssize_t"] = TypeFamily.PointerSized,
        ["intptr_t"] = TypeFamily.PointerSized,
        ["uintptr_t"] = TypeFamily.PointerSized,
        ["int8_t"] = TypeFamily.Int8,
        ["uint8_t"] = TypeFamily.UInt8,
        ["int16_t"] = TypeFamily.Int16,
        ["uint16_t"] = TypeFamily.UInt16,
        ["int32_t"] = TypeFamily.Int32,
        ["uint32_t"] = TypeFamily.UInt32,
        ["int64_t"] = TypeFamily.Int64,
        ["uint64_t"] = TypeFamily.UInt64,
        ["gboolean"] = TypeFamily.Bool32,
        ["gchar"] = TypeFamily.Int8,
        ["guchar"] = TypeFamily.UInt8,
        ["gint8"] = TypeFamily.Int8,
        ["guint8"] = TypeFamily.UInt8,
        ["gshort"] = TypeFamily.Int16,
        ["gushort"] = TypeFamily.UInt16,
        ["gint16"] = TypeFamily.Int16,
        ["guint16"] = TypeFamily.UInt16,
        ["gint"] = TypeFamily.Int32,
        ["guint"] = TypeFamily.UInt32,
        ["gint32"] = TypeFamily.Int32,
        ["guint32"] = TypeFamily.UInt32,
        ["gint64"] = TypeFamily.Int64,
        ["guint64"] = TypeFamily.UInt64,
        ["goffset"] = TypeFamily.Int64,
        ["glong"] = TypeFamily.CLong,
        ["gulong"] = TypeFamily.CULong,
        ["gfloat"] = TypeFamily.Float32,
        ["gdouble"] = TypeFamily.Float64,
        ["gsize"] = TypeFamily.PointerSized,
        ["gssize"] = TypeFamily.PointerSized,
        ["gintptr"] = TypeFamily.PointerSized,
        ["guintptr"] = TypeFamily.PointerSized,
        ["GType"] = TypeFamily.PointerSized,
        ["gpointer"] = TypeFamily.Pointer,
        ["gconstpointer"] = TypeFamily.Pointer,
    };

    private static readonly HashSet<string> Qualifiers = new(StringComparer.Ordinal)
    {
        "const", "volatile", "restrict", "__restrict", "register", "extern", "static", "inline", "__inline",
    };

    private static readonly HashSet<string> TypeKeywords = new(StringComparer.Ordinal)
    {
        "void", "char", "short", "int", "long", "signed", "unsigned", "float", "double", "_Bool",
    };

    // Stands for ARV_API, which the macro pass would otherwise remove, until the statement it
    // precedes is read.
    private const string ExportMarker = "__header_check_arv_api__";

    private readonly Dictionary<string, CPrototype> _functions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<CPrototype>> _conflicts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
    private readonly HashSet<string> _enums = new(StringComparer.Ordinal);
    private readonly HashSet<string> _structs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pointers = new(StringComparer.Ordinal);

    public int FunctionCount => _functions.Count;

    public CPrototype? Find(string name) => _functions.GetValueOrDefault(name);

    /// <summary>
    /// Every different prototype read for a name, the first one included; empty when the name
    /// has one prototype (or none).
    /// </summary>
    public IReadOnlyList<CPrototype> Conflicts(string name) =>
        _conflicts.TryGetValue(name, out var others) ? [_functions[name], .. others] : [];

    /// <summary>Reads every <c>*.h</c> file of a directory (recursively or not).</summary>
    public void AddDirectory(string directory, bool recursive, Func<string, bool> synthesizeEnumGetTypes)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(directory, "*.h", option).OrderBy(f => f, StringComparer.Ordinal))
            Parse(File.ReadAllText(file), file, synthesizeEnumGetTypes(file));
    }

    /// <summary>Reads the prototypes and type names of one header's text.</summary>
    public void Parse(string text, string source, bool synthesizeEnumGetTypes = false)
    {
        text = ContinuationRegex().Replace(text, " ");
        text = BlockCommentRegex().Replace(text, " ");
        text = LineCommentRegex().Replace(text, "");
        text = PreprocessorLineRegex().Replace(text, "");

        foreach (Match m in GDeclareRegex().Matches(text))
            AddFunction(new CPrototype(m.Groups["prefix"].Value + "_get_type", "GType", [], false, source, m.Groups["api"].Success));
        text = GDeclareRegex().Replace(text, " ");
        text = ArvApiRegex().Replace(text, " " + ExportMarker + " ");
        text = MacroRegex().Replace(text, " ");
        text = ParenthesisedNameRegex().Replace(text, "${name} (");

        foreach (var raw in SplitStatements(text))
        {
            var statement = WhitespaceRegex().Replace(raw, " ").Trim();
            var exported = statement.Contains(ExportMarker, StringComparison.Ordinal);
            if (exported)
                statement = WhitespaceRegex().Replace(statement.Replace(ExportMarker, " ", StringComparison.Ordinal), " ").Trim();
            if (statement.Length == 0)
                continue;
            if (statement.StartsWith("typedef ", StringComparison.Ordinal))
                ReadTypedef(statement, source, synthesizeEnumGetTypes);
            else if (EnumDefinitionRegex().Match(statement) is { Success: true } e)
                _enums.Add(e.Groups["name"].Value);
            else
                ReadFunction(statement, source, exported);
        }
    }

    /// <summary>The family of a C type as written in a prototype (qualifiers and all).</summary>
    public TypeFamily Resolve(string cType) => Resolve(cType, 0);

    /// <summary>
    /// The type a C pointer type points to: one <c>*</c> stripped (an array parameter is already
    /// written <c>T *</c>), or the target of a pointer typedef such as <c>gpointer</c>
    /// (<c>void</c>); null when the type is not a pointer the reader can see through.
    /// </summary>
    public string? Pointee(string cType) => Pointee(cType, 0);

    private string? Pointee(string cType, int depth)
    {
        var type = cType.Trim();
        if (type.EndsWith('*'))
            return type[..^1].Trim();
        if (depth > 32 || type.Contains('('))
            return null;
        var name = string.Join(' ', type.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !Qualifiers.Contains(w)));
        if (name is "gpointer" or "gconstpointer")
            return "void";
        return _aliases.TryGetValue(name, out var target) ? Pointee(target, depth + 1) : null;
    }

    private TypeFamily Resolve(string cType, int depth)
    {
        if (depth > 32)
            return TypeFamily.Unknown;
        if (cType.Contains('*') || cType.Contains('[') || cType.Contains('('))
            return TypeFamily.Pointer;

        var words = cType.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !Qualifiers.Contains(w)).ToList();
        if (words.Count == 0)
            return TypeFamily.Unknown;
        if (words[0] == "enum")
            return TypeFamily.Enum32;
        if (words[0] is "struct" or "union")
            return TypeFamily.Struct;

        var name = string.Join(' ', words);
        if (BuiltinTypes.TryGetValue(name, out var family))
            return family;
        if (_enums.Contains(name))
            return TypeFamily.Enum32;
        if (_pointers.Contains(name))
            return TypeFamily.Pointer;
        if (_structs.Contains(name))
            return TypeFamily.Struct;
        if (_aliases.TryGetValue(name, out var target))
            return Resolve(target, depth + 1);
        return TypeFamily.Unknown;
    }

    private void ReadTypedef(string statement, string source, bool synthesizeEnumGetTypes)
    {
        var body = statement["typedef ".Length..].Trim();
        var name = TrailingIdentifierRegex().Match(body).Groups["name"].Value;

        if (body.StartsWith("enum", StringComparison.Ordinal) && (body.Length == 4 || !IsIdentifierChar(body[4])))
        {
            if (name.Length == 0)
                return;
            _enums.Add(name);
            // arvenumtypes.h, generated at build time, declares it ARV_API.
            if (synthesizeEnumGetTypes && body.Contains('{'))
                AddFunction(new CPrototype(MkEnumsPrefix(name) + "_get_type", "GType", [], false, source, Exported: true));
            return;
        }
        if (StructHeadRegex().IsMatch(body))
        {
            if (name.Length > 0)
                _structs.Add(name);
            return;
        }
        if (FunctionPointerTypedefRegex().Match(body) is { Success: true } fp)
        {
            _pointers.Add(fp.Groups["name"].Value);
            return;
        }
        if (body.Contains('('))
        {
            // A function type (typedef void GFunc (gpointer data);): only usable through a pointer.
            var fn = FunctionTypeTypedefRegex().Match(body);
            if (fn.Success)
                _pointers.Add(fn.Groups["name"].Value);
            return;
        }
        if (name.Length == 0 || body.Length == name.Length)
            return;
        var target = body[..^name.Length].Trim();
        if (target.Length > 0)
            _aliases.TryAdd(name, target);
    }

    private void ReadFunction(string statement, string source, bool exported)
    {
        if (statement.Contains('{') || statement.Contains('='))
            return;
        var m = FunctionRegex().Match(statement);
        if (!m.Success)
            return;
        var returnType = NormalizeType(m.Groups["ret"].Value);
        if (returnType.Length == 0)
            return;

        var arguments = SplitArguments(m.Groups["args"].Value);
        if (arguments is null || arguments.Any(a => a.Contains('(') && !FunctionPointerParameterRegex().IsMatch(a)))
            return; // unbalanced, or an argument the reader cannot read: no prototype, which fails closed
        var parameters = new List<string>();
        var variadic = false;
        foreach (var argument in arguments)
        {
            var arg = argument.Trim();
            if (arg == "...")
            {
                variadic = true;
                continue;
            }
            parameters.Add(ParameterType(arg));
        }
        if (parameters is ["void"])
            parameters.Clear();
        AddFunction(new CPrototype(m.Groups["name"].Value, returnType, parameters, variadic, source, exported));
    }

    private void AddFunction(CPrototype prototype)
    {
        if (!_functions.TryGetValue(prototype.Name, out var first))
        {
            _functions.Add(prototype.Name, prototype);
            return;
        }
        if (first.Signature == prototype.Signature)
        {
            if (prototype.Exported && !first.Exported)
                _functions[prototype.Name] = first with { Exported = true };
            return;
        }
        if (!_conflicts.TryGetValue(prototype.Name, out var others))
            _conflicts.Add(prototype.Name, others = []);
        if (!others.Any(o => o.Signature == prototype.Signature))
            others.Add(prototype);
    }

    /// <summary>The type of a parameter declaration, without its name.</summary>
    internal static string ParameterType(string parameter)
    {
        if (parameter.Contains('('))
            return "(function pointer) *";
        var spaced = NormalizeType(parameter.Replace("*", " * "));
        var arrayed = spaced.Contains('[');
        if (arrayed)
            spaced = spaced[..spaced.IndexOf('[')].Trim();
        var tokens = spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var words = tokens.Where(t => t != "*" && !Qualifiers.Contains(t) && t is not ("struct" or "enum" or "union")).ToList();
        if (words.Count >= 2 && !TypeKeywords.Contains(words[^1]) && tokens[^1] == words[^1])
            tokens.RemoveAt(tokens.Count - 1);
        var type = WhitespaceRegex().Replace(string.Join(' ', tokens).Replace(" *", "*").Replace("* ", "*"), " ").Trim();
        return arrayed ? type + " *" : type;
    }

    private static string NormalizeType(string type) =>
        WhitespaceRegex().Replace(type, " ").Trim();

    /// <summary>The <c>glib-mkenums</c> lower-case name of a CamelCase type name.</summary>
    internal static string MkEnumsPrefix(string typeName)
    {
        var s = LowerUpperRegex().Replace(typeName, "$1_$2");
        s = UpperUpperLowerRegex().Replace(s, "$1_$2");
        return s.ToLowerInvariant();
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static IEnumerable<string> SplitStatements(string text)
    {
        var sb = new StringBuilder();
        int braces = 0, parens = 0, bodyStart = 0;
        foreach (var c in text)
        {
            switch (c)
            {
                case '{':
                    if (braces == 0)
                        bodyStart = sb.Length;
                    braces++;
                    sb.Append(c);
                    continue;
                case '}':
                    braces = Math.Max(0, braces - 1);
                    sb.Append(c);
                    // An inline function body ends its definition, which has no ';'.
                    if (braces == 0 && sb.ToString(0, bodyStart).TrimEnd().EndsWith(')'))
                        sb.Clear();
                    continue;
                case '(':
                    parens++;
                    break;
                case ')':
                    parens = Math.Max(0, parens - 1);
                    break;
                case ';' when braces == 0 && parens == 0:
                    yield return sb.ToString();
                    sb.Clear();
                    continue;
            }
            sb.Append(c);
        }
        if (sb.Length > 0)
            yield return sb.ToString();
    }

    /// <summary>The top-level arguments of an argument list; null when its parentheses do not balance.</summary>
    private static List<string>? SplitArguments(string args)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    if (--depth < 0)
                        return null;
                    break;
                case ',' when depth == 0:
                    result.Add(args[start..i]);
                    start = i + 1;
                    break;
            }
        }
        if (depth != 0)
            return null;
        if (args[start..].Trim().Length > 0 || start > 0)
            result.Add(args[start..]);
        return result;
    }

    [GeneratedRegex(@"\\\r?\n")]
    private static partial Regex ContinuationRegex();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(@"^[ \t]*#[^\n]*", RegexOptions.Multiline)]
    private static partial Regex PreprocessorLineRegex();

    [GeneratedRegex(@"(?<api>\bARV_API\s+)?\bG_DECLARE_(?:FINAL_TYPE|DERIVABLE_TYPE|INTERFACE)\s*\(\s*\w+\s*,\s*(?<prefix>\w+)\s*,[^()]*\)")]
    private static partial Regex GDeclareRegex();

    [GeneratedRegex(@"\bARV_API\b")]
    private static partial Regex ArvApiRegex();

    [GeneratedRegex(@"\(\s*\*\s*\w*\s*\)\s*\(")]
    private static partial Regex FunctionPointerParameterRegex();

    // An all-capitals identifier (a macro), with its balanced parenthesised arguments if any.
    [GeneratedRegex(@"\b_*[A-Z][A-Z0-9_]*[A-Z0-9]\b(?:\s*\((?>[^()]+|\((?<d>)|\)(?<-d>))*(?(d)(?!))\))?")]
    private static partial Regex MacroRegex();

    [GeneratedRegex(@"\(\s*(?<name>[a-z_][A-Za-z0-9_]*)\s*\)\s*\(")]
    private static partial Regex ParenthesisedNameRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^enum\s+(?<name>\w+)\s*\{")]
    private static partial Regex EnumDefinitionRegex();

    [GeneratedRegex(@"(?<name>[A-Za-z_]\w*)\s*$")]
    private static partial Regex TrailingIdentifierRegex();

    [GeneratedRegex(@"^(?:struct|union)\b")]
    private static partial Regex StructHeadRegex();

    [GeneratedRegex(@"\(\s*\*\s*(?<name>\w+)\s*\)\s*\(")]
    private static partial Regex FunctionPointerTypedefRegex();

    [GeneratedRegex(@"(?<name>[A-Za-z_]\w*)\s*\(")]
    private static partial Regex FunctionTypeTypedefRegex();

    [GeneratedRegex(@"^(?<ret>[^(){}]*?[\w*])\s*\b(?<name>[A-Za-z_]\w*)\s*\((?<args>.*)\)$", RegexOptions.Singleline)]
    private static partial Regex FunctionRegex();

    [GeneratedRegex(@"([^A-Z])([A-Z])")]
    private static partial Regex LowerUpperRegex();

    [GeneratedRegex(@"([A-Z][A-Z])([A-Z][0-9a-z])")]
    private static partial Regex UpperUpperLowerRegex();
}

/// <summary>
/// Where the header check finds its headers: the Aravis sources of the submodule
/// (<c>&lt;repository&gt;/aravis/src</c>, or <c>ARAVIS_SOURCE_DIR</c>: the Aravis source tree or
/// its <c>src</c> directory) and the GLib headers (the <c>-I</c> directories of
/// <c>pkg-config --cflags-only-I glib-2.0 gobject-2.0 gio-2.0</c> under a <c>glib-2.0</c> path,
/// else <c>/usr/include/glib-2.0</c>). <paramref name="GLibNote"/> says why <c>pkg-config</c> gave
/// nothing (not installed, an error, a timeout), when it did.
/// </summary>
internal sealed record HeaderSources(string? AravisDirectory, IReadOnlyList<string> GLibDirectories, string? Problem, string? GLibNote = null)
{
    private static readonly Lazy<HeaderSources> Located = new(Locate);

    public static HeaderSources Current => Located.Value;

    /// <summary>
    /// True in CI (<c>GITHUB_ACTIONS=true</c> or <c>CI=true</c>, any case), where missing headers
    /// fail the check instead of skipping it.
    /// </summary>
    public static bool IsCi => IsCiEnvironment(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), Environment.GetEnvironmentVariable("CI"));

    internal static bool IsCiEnvironment(string? githubActions, string? ci) =>
        string.Equals(githubActions, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The skip reason of the header check: null (the check runs) when the headers were found, or
    /// in CI, where the check then runs and fails; the reason otherwise.
    /// </summary>
    internal static string? SkipReason(string? problem, bool isCi) =>
        problem is null || isCi ? null : "C headers not found, the P/Invoke header check cannot run: " + problem;

    private static HeaderSources Locate()
    {
        var problems = new List<string>();
        var aravis = LocateAravis(problems);
        var glib = LocateGLib(problems, out var note);
        return new HeaderSources(aravis, glib, problems.Count == 0 ? null : string.Join(" ", problems), note);
    }

    private static string? LocateAravis(List<string> problems)
    {
        var overridden = Environment.GetEnvironmentVariable("ARAVIS_SOURCE_DIR");
        string[] candidates;
        if (!string.IsNullOrWhiteSpace(overridden))
        {
            candidates = [Path.Combine(overridden, "src"), overridden];
        }
        else
        {
            var root = FindRepositoryRoot();
            candidates = root is null ? [] : [Path.Combine(root, "aravis", "src")];
        }
        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "arvcamera.h")))
                return candidate;
        }
        problems.Add(!string.IsNullOrWhiteSpace(overridden)
            ? $"No Aravis headers (arvcamera.h) under ARAVIS_SOURCE_DIR={overridden} or its src directory."
            : "No Aravis headers (aravis/src/arvcamera.h): run 'git submodule update --init aravis' or set ARAVIS_SOURCE_DIR.");
        return null;
    }

    private static string? FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AravisSharp.slnx")))
                return dir.FullName;
        }
        return null;
    }

    private static IReadOnlyList<string> LocateGLib(List<string> problems, out string? note)
    {
        var dirs = PkgConfigIncludeDirectories(out note)
            .Where(d => d.Contains("glib-2.0", StringComparison.Ordinal) && Directory.Exists(d))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (dirs.Count == 0 && Directory.Exists("/usr/include/glib-2.0"))
            dirs.Add("/usr/include/glib-2.0");
        if (!dirs.Any(d => File.Exists(Path.Combine(d, "glib.h"))))
        {
            problems.Add("No GLib headers (glib.h) from 'pkg-config --cflags-only-I glib-2.0 gobject-2.0 gio-2.0' or /usr/include/glib-2.0: install the GLib development package (libglib2.0-dev)."
                + (note is null ? "" : " " + note));
            return [];
        }
        return dirs;
    }

    /// <summary>
    /// The <c>-I</c> directories <c>pkg-config</c> gives for GLib; empty, with <paramref name="note"/>
    /// saying why, when it is not installed, fails or does not answer within 10 seconds. Both
    /// output streams are read concurrently, so a full stderr pipe cannot block it.
    /// </summary>
    private static IReadOnlyList<string> PkgConfigIncludeDirectories(out string? note)
    {
        const string Command = "pkg-config --cflags-only-I glib-2.0 gobject-2.0 gio-2.0";
        note = null;
        try
        {
            var start = new ProcessStartInfo("pkg-config", "--cflags-only-I glib-2.0 gobject-2.0 gio-2.0")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(start);
            if (process is null)
            {
                note = $"'{Command}' did not start.";
                return [];
            }
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(10_000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                note = $"'{Command}' did not exit within 10 s and was killed.";
                return [];
            }
            process.WaitForExit(); // the end of both redirected streams
            var output = stdout.GetAwaiter().GetResult();
            var error = stderr.GetAwaiter().GetResult().Trim();
            if (process.ExitCode != 0)
            {
                note = $"'{Command}' exited with code {process.ExitCode}" + (error.Length == 0 ? "." : $": {error}");
                return [];
            }
            return output.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.StartsWith("-I", StringComparison.Ordinal))
                .Select(t => t[2..])
                .ToList();
        }
        catch (Win32Exception ex)
        {
            note = $"'{Command}' could not run ({ex.Message}); is pkg-config installed?";
            return [];
        }
    }

    /// <summary>
    /// Reads the GLib headers (recursively) and the Aravis headers (<c>*.h</c> of the source
    /// directory; the enum <c>get_type</c> functions of the public, non-<c>*private.h</c> ones).
    /// </summary>
    public HeaderModel Load()
    {
        if (Problem is not null || AravisDirectory is null)
            throw new InvalidOperationException(Problem);
        var model = new HeaderModel();
        foreach (var dir in GLibDirectories)
            model.AddDirectory(dir, recursive: true, _ => false);
        model.AddDirectory(AravisDirectory, recursive: false,
            file => !Path.GetFileName(file).EndsWith("private.h", StringComparison.Ordinal));
        return model;
    }
}

/// <summary>
/// A fact that compares the P/Invoke declarations with the C headers. Skipped, with the reason,
/// when the headers cannot be found, except in CI (<see cref="HeaderSources.IsCi"/>), where the
/// test runs and fails instead: CI must never skip the check silently.
/// </summary>
internal sealed class HeaderFactAttribute : FactAttribute
{
    public HeaderFactAttribute()
    {
        var reason = HeaderSources.SkipReason(HeaderSources.Current.Problem, HeaderSources.IsCi);
        if (reason is not null)
            Skip = reason;
    }
}

/// <summary>
/// Compares P/Invoke declarations with C prototypes, family by family (see
/// <see cref="TypeFamily"/>). The C# side of each type is the set of families it may stand for:
/// <list type="bullet">
/// <item><c>int</c> is <c>Int32</c> or a C enum, <c>uint</c> is <c>UInt32</c> or a C enum, and a
/// C# enum is its underlying type, or a C enum when that type is <c>int</c> or <c>uint</c>: a C
/// enum is a 32-bit integer on every target the package ships.</item>
/// <item><c>bool</c> is <c>gboolean</c> only when marshalled as 4 bytes (no <c>MarshalAs</c>, or
/// <c>UnmanagedType.Bool</c>); <c>U1</c>/<c>I1</c> is a 1-byte C <c>bool</c>.</item>
/// <item><c>IntPtr</c>/<c>nint</c> is a pointer or a pointer-sized integer; <c>UIntPtr</c>/<c>nuint</c>
/// a pointer-sized integer; <c>out</c>/<c>ref</c>, string, <c>StringBuilder</c>, arrays, delegates
/// and classes are pointers; other structs are structs by value.</item>
/// <item><c>System.Runtime.InteropServices.CLong</c>/<c>CULong</c> are <c>glong</c>/<c>gulong</c>.</item>
/// <item>The pointee of a by-reference, pointer or array parameter whose element is a primitive,
/// an enum, <c>bool</c>, <c>IntPtr</c>/<c>UIntPtr</c> or <c>CLong</c>/<c>CULong</c> is compared too:
/// one <c>*</c> is stripped from the C type and its family must be one the element stands for, so
/// <c>out uint</c> for a <c>guint64 *</c> fails. An <c>out IntPtr</c> for a <c>T **</c> passes; a
/// struct element (<c>ref GValue</c>) is only an address.</item>
/// <item>C# <c>char</c> stands for nothing: its width depends on the <c>CharSet</c>; use
/// <c>byte</c>, <c>sbyte</c> or <c>ushort</c>.</item>
/// <item>A returned <c>string</c> or <c>StringBuilder</c>, and an <c>out</c>/<c>ref</c> string, are
/// mismatches: the marshaller frees the native memory it reads, which is wrong for memory C still
/// owns. Such a declaration needs an allow-list entry that says who owns the memory.</item>
/// <item>An <c>arv_*</c> function must have an <c>ARV_API</c> prototype: Aravis builds with
/// <c>-fvisibility=hidden</c>, so no other function is exported from libaravis.</item>
/// <item>A name read with two different prototypes (both branches of an <c>#if</c>) is
/// reported.</item>
/// </list>
/// </summary>
internal static class PInvokeSignatureCheck
{
    private const string LibraryImportAttributeName = "System.Runtime.InteropServices.LibraryImportAttribute";

    /// <summary>The DllImport and LibraryImport methods a type declares, public or not.</summary>
    public static IReadOnlyList<MethodInfo> Imports(Type type) =>
        type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(m => !m.Name.Contains('<')) // the local extern a LibraryImport stub generates
            .Where(m => (m.Attributes & MethodAttributes.PinvokeImpl) != 0
                     || m.CustomAttributes.Any(a => a.AttributeType.FullName == LibraryImportAttributeName))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The DllImport and LibraryImport methods of every type of an assembly, nested and private
    /// types included, ordered by declaring type and name.
    /// </summary>
    public static IReadOnlyList<MethodInfo> Imports(Assembly assembly) =>
        assembly.GetTypes()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .SelectMany(Imports)
            .ToList();

    /// <summary>The native name a declaration binds.</summary>
    public static string EntryPoint(MethodInfo method)
    {
        var dll = method.GetCustomAttribute<DllImportAttribute>();
        if (dll is not null)
            return string.IsNullOrEmpty(dll.EntryPoint) ? method.Name : dll.EntryPoint;
        var library = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == LibraryImportAttributeName);
        var entry = library?.NamedArguments.FirstOrDefault(a => a.MemberName == "EntryPoint").TypedValue.Value as string;
        return string.IsNullOrEmpty(entry) ? method.Name : entry;
    }

    /// <summary>
    /// Every mismatch between the declarations and the prototypes, one line each, naming the
    /// function; empty when all match. An allow-listed entry point is not compared, and an
    /// allow-list entry that names no declaration, or a declaration that matches, is itself
    /// reported, so the list cannot go stale.
    /// </summary>
    public static IReadOnlyList<string> Check(IEnumerable<MethodInfo> declarations, HeaderModel model, IReadOnlyDictionary<string, string> allowList)
    {
        var failures = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in declarations)
        {
            var entry = EntryPoint(method);
            seen.Add(entry);
            var problems = Compare(method, entry, model);
            if (allowList.ContainsKey(entry))
            {
                if (problems.Count == 0)
                    failures.Add($"{entry}: allow-listed, but its declaration matches the headers; remove it from the allow-list.");
                continue;
            }
            failures.AddRange(problems);
        }
        failures.AddRange(allowList.Keys.Where(k => !seen.Contains(k)).Select(k => $"{k}: allow-listed, but no declaration binds it; remove it from the allow-list."));
        return failures;
    }

    private static List<string> Compare(MethodInfo method, string entry, HeaderModel model)
    {
        var where = $"{entry} ({method.DeclaringType?.Name}.{method.Name})";
        var prototype = model.Find(entry);
        if (prototype is null)
            return [$"{where}: no C prototype found in the headers."];
        var conflicts = model.Conflicts(entry);
        if (conflicts.Count > 0)
            return [$"{where}: {conflicts.Count} different C prototypes, the check cannot tell which one applies: {string.Join("; ", conflicts.Select(c => $"{c.Signature} ({Path.GetFileName(c.Source)})"))}."];

        var problems = new List<string>();
        if (entry.StartsWith("arv_", StringComparison.Ordinal) && !prototype.Exported)
            problems.Add($"{where}: the C prototype ({Path.GetFileName(prototype.Source)}) is not marked ARV_API, so libaravis does not export it (Aravis builds with -fvisibility=hidden).");
        if (prototype.IsVariadic)
            problems.Add($"{where}: the C function is variadic ({Path.GetFileName(prototype.Source)}); a fixed-argument import of it is not portable.");
        var parameters = method.GetParameters();
        if (parameters.Length != prototype.Parameters.Count)
        {
            problems.Add($"{where}: {parameters.Length} argument(s) in C#, {prototype.Parameters.Count} in C ({string.Join(", ", prototype.Parameters)}).");
            return problems;
        }
        CompareOne(problems, where, "return", prototype.ReturnType, method.ReturnParameter, model);
        for (var i = 0; i < parameters.Length; i++)
            CompareOne(problems, where, $"argument {i + 1} ({parameters[i].Name})", prototype.Parameters[i], parameters[i], model);
        return problems;
    }

    private static void CompareOne(List<string> problems, string where, string what, string cType, ParameterInfo managed, HeaderModel model)
    {
        var type = managed.ParameterType;
        var element = type.IsByRef || type.IsPointer || type.IsArray ? type.GetElementType() : null;
        if ((IsString(type) && managed.Position < 0) || (element is not null && type.IsByRef && IsString(element)))
        {
            problems.Add($"{where}: {what} is a managed string ({Describe(managed)}) for '{cType}' in C: the marshaller frees the native memory it reads; declare IntPtr and copy, or allow-list the declaration saying who owns the memory.");
            return;
        }

        var cFamily = model.Resolve(cType);
        var accepted = ManagedFamilies(managed);
        if (cFamily == TypeFamily.Unknown)
        {
            problems.Add($"{where}: {what}: C type '{cType}' cannot be resolved from the headers.");
            return;
        }
        if (!accepted.Contains(cFamily))
        {
            problems.Add($"{where}: {what} is '{cType}' ({cFamily}) in C, {Describe(managed)} ({string.Join("/", accepted)}) in C#.");
            return;
        }
        if (element is null || !IsScalar(element))
            return;

        var pointee = model.Pointee(cType);
        var pointeeFamily = pointee is null ? TypeFamily.Unknown : model.Resolve(pointee);
        var elementFamilies = ManagedFamilies(element, managed.GetCustomAttribute<MarshalAsAttribute>());
        if (pointeeFamily == TypeFamily.Unknown)
            problems.Add($"{where}: {what}: the type that C type '{cType}' points to cannot be resolved from the headers, so {Describe(managed)} cannot be checked.");
        else if (!elementFamilies.Contains(pointeeFamily))
            problems.Add($"{where}: {what} points to '{pointee}' ({pointeeFamily}) in C, {Describe(managed)} ({string.Join("/", elementFamilies)}) in C#.");
    }

    private static bool IsString(Type type) => type == typeof(string) || type == typeof(StringBuilder);

    /// <summary>An element type whose pointee the check compares (see the class remarks).</summary>
    private static bool IsScalar(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(CLong) || type == typeof(CULong);

    private static string Describe(ParameterInfo p)
    {
        var type = p.ParameterType;
        var text = type.IsByRef ? (p.IsOut ? "out " : "ref ") + type.GetElementType()!.Name : type.Name;
        var marshal = p.GetCustomAttribute<MarshalAsAttribute>();
        return marshal is null ? text : $"[MarshalAs({marshal.Value})] {text}";
    }

    /// <summary>The C type families a managed parameter or return type may stand for.</summary>
    internal static IReadOnlySet<TypeFamily> ManagedFamilies(ParameterInfo p) =>
        ManagedFamilies(p.ParameterType, p.GetCustomAttribute<MarshalAsAttribute>());

    /// <summary>The C type families a managed type, with its <c>MarshalAs</c> if any, may stand for.</summary>
    internal static IReadOnlySet<TypeFamily> ManagedFamilies(Type type, MarshalAsAttribute? marshal)
    {
        if (type.IsByRef || type.IsPointer || type.IsArray || type == typeof(string) || type == typeof(StringBuilder)
            || typeof(Delegate).IsAssignableFrom(type) || type.IsFunctionPointer)
            return Set(TypeFamily.Pointer);
        if (type == typeof(void))
            return Set(TypeFamily.Void);
        if (type == typeof(IntPtr))
            return Set(TypeFamily.Pointer, TypeFamily.PointerSized);
        if (type == typeof(UIntPtr))
            return Set(TypeFamily.PointerSized);
        if (type == typeof(CLong))
            return Set(TypeFamily.CLong);
        if (type == typeof(CULong))
            return Set(TypeFamily.CULong);
        if (type == typeof(bool))
        {
            return marshal is null || marshal.Value == UnmanagedType.Bool
                ? Set(TypeFamily.Bool32)
                : marshal.Value is UnmanagedType.U1 or UnmanagedType.I1 ? Set(TypeFamily.Bool8) : Set();
        }
        if (type.IsEnum)
        {
            var underlying = Enum.GetUnderlyingType(type);
            var families = new HashSet<TypeFamily>(Primitive(underlying));
            if (underlying == typeof(int) || underlying == typeof(uint))
                families.Add(TypeFamily.Enum32);
            return families;
        }
        if (type == typeof(int))
            return Set(TypeFamily.Int32, TypeFamily.Enum32);
        if (type == typeof(uint))
            return Set(TypeFamily.UInt32, TypeFamily.Enum32);
        var primitive = Primitive(type);
        if (primitive.Count > 0)
            return primitive;
        if (type.IsPrimitive)
            return Set(); // char: its width depends on the CharSet
        if (type.IsValueType)
            return Set(TypeFamily.Struct);
        return Set(TypeFamily.Pointer); // a class marshalled by reference
    }

    private static IReadOnlySet<TypeFamily> Primitive(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.SByte => Set(TypeFamily.Int8),
        TypeCode.Byte => Set(TypeFamily.UInt8),
        TypeCode.Int16 => Set(TypeFamily.Int16),
        TypeCode.UInt16 => Set(TypeFamily.UInt16),
        TypeCode.Int32 => Set(TypeFamily.Int32),
        TypeCode.UInt32 => Set(TypeFamily.UInt32),
        TypeCode.Int64 => Set(TypeFamily.Int64),
        TypeCode.UInt64 => Set(TypeFamily.UInt64),
        TypeCode.Single => Set(TypeFamily.Float32),
        TypeCode.Double => Set(TypeFamily.Float64),
        _ => Set(),
    };

    private static HashSet<TypeFamily> Set(params TypeFamily[] families) => [.. families];
}
