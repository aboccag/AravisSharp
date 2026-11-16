using System.Reflection;
using System.Runtime.InteropServices;

namespace AravisSharp.Tests;

/// <summary>
/// The header reader (<see cref="HeaderModel"/>) and the comparison rules
/// (<see cref="PInvokeSignatureCheck"/>) on synthetic header snippets and declarations (issue
/// #11). Pure: no header files, no native library.
/// </summary>
public class HeaderPrototypesTests
{
    private static HeaderModel Read(string header, bool publicAravisHeader = false)
    {
        var model = new HeaderModel();
        model.Parse(header, "test.h", publicAravisHeader);
        return model;
    }

    [Fact]
    public void Reader_ReadsAMultiLineArvApiPrototype_WithCommentsAndAttributes()
    {
        var model = Read("""
            #include <arvtypes.h>
            G_BEGIN_DECLS
            /**
             * arv_camera_get_region: (skip)
             */
            ARV_API void		arv_camera_get_region	(ArvCamera *camera, /* the camera */
            							 gint *x, gint *y,
            							 // the error
            							 GError **error) G_GNUC_WARN_UNUSED_RESULT;
            ARV_API guint arv_camera_gv_get_packet_size (ArvCamera *camera, GError **error);
            G_END_DECLS
            """);

        var region = Assert.IsType<CPrototype>(model.Find("arv_camera_get_region"));
        Assert.Equal("void", region.ReturnType);
        Assert.Equal(4, region.Parameters.Count);
        Assert.All(region.Parameters, p => Assert.Equal(TypeFamily.Pointer, model.Resolve(p)));
        var packetSize = Assert.IsType<CPrototype>(model.Find("arv_camera_gv_get_packet_size"));
        Assert.Equal(TypeFamily.UInt32, model.Resolve(packetSize.ReturnType));
    }

    [Fact]
    public void Reader_ReadsGLibPrototypes_WithAvailabilityPrefixesAndParenthesisedNames()
    {
        var model = Read("""
            GLIB_AVAILABLE_IN_ALL
            void	 (g_free)	          (gpointer	 mem);
            GLIB_AVAILABLE_IN_2_68
            gpointer g_memdup2 (gconstpointer mem,
                                gsize         byte_size) G_GNUC_ALLOC_SIZE(2);
            GLIB_DEPRECATED_IN_2_32_FOR(g_other)
            const gchar * g_quark_to_string (GQuark quark) G_GNUC_CONST;
            GOBJECT_AVAILABLE_IN_ALL
            gboolean g_type_check_instance_is_a (GTypeInstance *instance, GType iface_type) G_GNUC_PURE;
            """);

        Assert.Equal(new[] { "gpointer" }, model.Find("g_free")!.Parameters);
        Assert.Equal(TypeFamily.PointerSized, model.Resolve(model.Find("g_memdup2")!.Parameters[1]));
        Assert.Equal(TypeFamily.Pointer, model.Resolve(model.Find("g_quark_to_string")!.ReturnType));
        var isA = model.Find("g_type_check_instance_is_a")!;
        Assert.Equal(TypeFamily.Bool32, model.Resolve(isA.ReturnType));
        Assert.Equal(TypeFamily.PointerSized, model.Resolve(isA.Parameters[1]));
    }

    [Fact]
    public void Reader_SkipsInlineFunctionBodies_AndReadsTheNextPrototype()
    {
        var model = Read("""
            static inline gboolean
            g_set_object (GObject **object_ptr, GObject *new_object)
            {
              if (*object_ptr == new_object) { return FALSE; }
              g_object_ref (new_object);
              return TRUE;
            }
            GOBJECT_AVAILABLE_IN_ALL
            void g_object_unref (gpointer object);
            """);

        Assert.Null(model.Find("g_object_ref"));
        Assert.Equal(new[] { "gpointer" }, model.Find("g_object_unref")!.Parameters);
    }

    [Fact]
    public void Reader_DeclaresGetTypeForGDeclareMacros()
    {
        var model = Read("""
            ARV_API G_DECLARE_DERIVABLE_TYPE (ArvCamera, arv_camera, ARV, CAMERA, GObject)
            ARV_API G_DECLARE_FINAL_TYPE (ArvGvStream, arv_gv_stream, ARV, GV_STREAM, ArvStream)
            ARV_API G_DECLARE_INTERFACE (ArvGcInteger, arv_gc_integer, ARV, GC_INTEGER, GObject)
            ARV_API ArvCamera *arv_camera_new (const char *name, GError **error);
            """);

        foreach (var name in new[] { "arv_camera_get_type", "arv_gv_stream_get_type", "arv_gc_integer_get_type" })
        {
            var getType = Assert.IsType<CPrototype>(model.Find(name));
            Assert.Empty(getType.Parameters);
            Assert.Equal(TypeFamily.PointerSized, model.Resolve(getType.ReturnType));
        }
        Assert.Equal(2, model.Find("arv_camera_new")!.Parameters.Count);
    }

    [Fact]
    public void Reader_DeclaresTheMkEnumsGetType_OfAPublicAravisEnum_Only()
    {
        const string header = """
            typedef enum {
            	ARV_GV_STREAM_SOCKET_BUFFER_FIXED,
            	ARV_GV_STREAM_SOCKET_BUFFER_AUTO = (1 << 2)
            } ArvGvStreamSocketBuffer;
            """;

        Assert.NotNull(Read(header, publicAravisHeader: true).Find("arv_gv_stream_socket_buffer_get_type"));
        Assert.Null(Read(header, publicAravisHeader: false).Find("arv_gv_stream_socket_buffer_get_type"));
        Assert.Equal("arv_gc_name_space", HeaderModel.MkEnumsPrefix("ArvGcNameSpace"));
        Assert.Equal("arv_uv_usb_mode", HeaderModel.MkEnumsPrefix("ArvUvUsbMode"));
    }

    [Fact]
    public void Reader_ResolvesTypedefsEnumsStructsAndCallbacks()
    {
        var model = Read("""
            typedef guint32 ArvPixelFormat;
            typedef ArvPixelFormat ArvPixelFormatAlias;
            typedef enum {
            	ARV_AUTO_OFF,
            	ARV_AUTO_ONCE
            } ArvAuto;
            typedef enum _ArvForward ArvForward;
            typedef struct _ArvCamera ArvCamera;
            typedef void (*ArvStreamCallback) (void *user_data, int type, ArvBuffer *buffer);
            typedef gboolean GSourceFunc2 (gpointer user_data);
            typedef ArvCamera *ArvCameraPointer;
            """);

        Assert.Equal(TypeFamily.UInt32, model.Resolve("ArvPixelFormat"));
        Assert.Equal(TypeFamily.UInt32, model.Resolve("ArvPixelFormatAlias"));
        Assert.Equal(TypeFamily.Enum32, model.Resolve("ArvAuto"));
        Assert.Equal(TypeFamily.Enum32, model.Resolve("ArvForward"));
        Assert.Equal(TypeFamily.Enum32, model.Resolve("enum _Anything"));
        Assert.Equal(TypeFamily.Struct, model.Resolve("ArvCamera"));
        Assert.Equal(TypeFamily.Pointer, model.Resolve("ArvCamera *"));
        Assert.Equal(TypeFamily.Pointer, model.Resolve("ArvStreamCallback"));
        Assert.Equal(TypeFamily.Pointer, model.Resolve("GSourceFunc2"));
        Assert.Equal(TypeFamily.Pointer, model.Resolve("ArvCameraPointer"));
        Assert.Equal(TypeFamily.Unknown, model.Resolve("ArvNeverDeclared"));
    }

    [Theory]
    [InlineData("unsigned int", "UInt32")]
    [InlineData("unsigned", "UInt32")]
    [InlineData("int", "Int32")]
    [InlineData("const guint", "UInt32")]
    [InlineData("gint64", "Int64")]
    [InlineData("guint64", "UInt64")]
    [InlineData("long long", "Int64")]
    [InlineData("gulong", "CULong")]
    [InlineData("gdouble", "Float64")]
    [InlineData("gfloat", "Float32")]
    [InlineData("guint16", "UInt16")]
    [InlineData("gboolean", "Bool32")]
    [InlineData("gsize", "PointerSized")]
    [InlineData("GType", "PointerSized")]
    [InlineData("const char *", "Pointer")]
    [InlineData("void", "Void")]
    public void Reader_MapsBuiltinTypes(string cType, string expected) =>
        Assert.Equal(Enum.Parse<TypeFamily>(expected), new HeaderModel().Resolve(cType));

    [Theory]
    [InlineData("guint size", "guint")]
    [InlineData("unsigned int", "unsigned int")]
    [InlineData("unsigned int size", "unsigned int")]
    [InlineData("const gchar *name", "const gchar*")]
    [InlineData("const gchar", "const gchar")]
    [InlineData("GError **error", "GError**")]
    [InlineData("guint8 data[16]", "guint8 *")]
    [InlineData("void (*callback) (gpointer data)", "(function pointer) *")]
    public void Reader_StripsTheParameterName(string parameter, string expected) =>
        Assert.Equal(expected, HeaderModel.ParameterType(parameter));

    [Fact]
    public void Reader_ReadsVariadicPrototypes()
    {
        var model = Read("GOBJECT_AVAILABLE_IN_ALL void g_object_get (gpointer object, const gchar *first_property_name, ...) G_GNUC_NULL_TERMINATED;");

        var getter = model.Find("g_object_get")!;
        Assert.True(getter.IsVariadic);
        Assert.Equal(2, getter.Parameters.Count);
    }

    // === Comparison ===

    private const string Header = """
        typedef enum { ARV_AUTO_OFF } ArvAuto;
        typedef guint32 ArvPixelFormat;
        guint x_uint (ArvCamera *camera, gint *value, GError **error);
        gboolean x_bool (gpointer object);
        void x_set_bool (gpointer object, gboolean value);
        ArvAuto x_enum (gpointer object);
        gsize x_size (const char *name);
        void x_pixel_format (ArvPixelFormat format);
        void x_two (gpointer a, gpointer b);
        void x_variadic (gpointer a, ...);
        """;

    private static IReadOnlyList<string> CheckFixture(string name, IReadOnlyDictionary<string, string>? allowList = null)
    {
        var method = typeof(Fixtures).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
        return PInvokeSignatureCheck.Check([method], Read(Header), allowList ?? new Dictionary<string, string>());
    }

    [Theory]
    [InlineData(nameof(Fixtures.x_uint))]
    [InlineData(nameof(Fixtures.Bool_Default))]
    [InlineData(nameof(Fixtures.Bool_MarshalledAsBool))]
    [InlineData(nameof(Fixtures.SetBool_Default))]
    [InlineData(nameof(Fixtures.Enum_AsInt))]
    [InlineData(nameof(Fixtures.Enum_AsUInt))]
    [InlineData(nameof(Fixtures.Enum_AsManagedEnum))]
    [InlineData(nameof(Fixtures.Size_AsUIntPtr))]
    [InlineData(nameof(Fixtures.Size_AsIntPtr))]
    [InlineData(nameof(Fixtures.PixelFormat_AsUInt))]
    [InlineData(nameof(Fixtures.Two_AsStringAndDelegate))]
    public void Check_AcceptsAMatchingDeclaration(string fixture) =>
        Assert.Empty(CheckFixture(fixture));

    [Theory]
    [InlineData(nameof(Fixtures.Uint_AsInt), "x_uint (Fixtures.Uint_AsInt): return is 'guint' (UInt32) in C, Int32")]
    [InlineData(nameof(Fixtures.Uint_AsVoid), "x_uint (Fixtures.Uint_AsVoid): return is 'guint' (UInt32) in C, Void")]
    [InlineData(nameof(Fixtures.Uint_ValueByPointer), "argument 2 (value) is 'gint*' (Pointer) in C, Int32")]
    [InlineData(nameof(Fixtures.Bool_MarshalledAsU1), "return is 'gboolean' (Bool32) in C, [MarshalAs(U1)] Boolean")]
    [InlineData(nameof(Fixtures.SetBool_MarshalledAsI1), "argument 2 (value) is 'gboolean' (Bool32) in C, [MarshalAs(I1)] Boolean")]
    [InlineData(nameof(Fixtures.SetBool_AsInt), "argument 2 (value) is 'gboolean' (Bool32) in C, Int32")]
    [InlineData(nameof(Fixtures.Enum_AsLong), "return is 'ArvAuto' (Enum32) in C, Int64")]
    [InlineData(nameof(Fixtures.Size_AsUInt), "return is 'gsize' (PointerSized) in C, UInt32")]
    [InlineData(nameof(Fixtures.PixelFormat_AsInt), "argument 1 (format) is 'ArvPixelFormat' (UInt32) in C, Int32")]
    [InlineData(nameof(Fixtures.Two_AsOne), "x_two (Fixtures.Two_AsOne): 1 argument(s) in C#, 2 in C (gpointer, gpointer).")]
    [InlineData(nameof(Fixtures.Variadic), "x_variadic (Fixtures.Variadic): the C function is variadic")]
    [InlineData(nameof(Fixtures.x_missing), "x_missing (Fixtures.x_missing): no C prototype found in the headers.")]
    public void Check_ReportsAMismatch_NamingTheFunction(string fixture, string expected)
    {
        var failure = Assert.Single(CheckFixture(fixture));
        Assert.Contains(expected, failure);
    }

    [Fact]
    public void Check_ReportsEveryMismatch_InOneList()
    {
        var methods = new[] { nameof(Fixtures.Uint_AsInt), nameof(Fixtures.Bool_MarshalledAsU1), nameof(Fixtures.x_uint) }
            .Select(n => typeof(Fixtures).GetMethod(n, BindingFlags.Static | BindingFlags.NonPublic)!);

        var failures = PInvokeSignatureCheck.Check(methods, Read(Header), new Dictionary<string, string>());

        Assert.Equal(2, failures.Count);
    }

    [Fact]
    public void Check_SkipsAnAllowListedMismatch_AndReportsAStaleEntry()
    {
        Assert.Empty(CheckFixture(nameof(Fixtures.Uint_AsInt), new Dictionary<string, string> { ["x_uint"] = "test" }));

        var notNeeded = Assert.Single(CheckFixture(nameof(Fixtures.x_uint), new Dictionary<string, string> { ["x_uint"] = "test" }));
        Assert.Contains("x_uint: allow-listed, but its declaration matches", notNeeded);

        var unknown = CheckFixture(nameof(Fixtures.x_uint), new Dictionary<string, string> { ["x_gone"] = "test" });
        Assert.Contains("x_gone: allow-listed, but no declaration binds it", Assert.Single(unknown));
    }

    [Fact]
    public void Check_ReportsAnUnresolvableCType()
    {
        var model = Read("ArvUnknown x_unknown (gpointer a);");
        var method = typeof(Fixtures).GetMethod(nameof(Fixtures.Unknown), BindingFlags.Static | BindingFlags.NonPublic)!;

        var failure = Assert.Single(PInvokeSignatureCheck.Check([method], model, new Dictionary<string, string>()));

        Assert.Contains("return: C type 'ArvUnknown' cannot be resolved", failure);
    }

    private enum ManagedAuto
    {
        Off,
    }

    // Declarations for the comparison only: never called, the library does not exist.
#pragma warning disable IDE1006, SYSLIB1054, CA5392 // C names, DllImport on purpose
    private static class Fixtures
    {
        private const string Lib = "header-check-fixture-not-a-library";

        public delegate void Callback(IntPtr data);

        [DllImport(Lib)] internal static extern uint x_uint(IntPtr camera, out int value, out IntPtr error);
        [DllImport(Lib)] internal static extern IntPtr x_missing();
        [DllImport(Lib, EntryPoint = "x_uint")] internal static extern int Uint_AsInt(IntPtr camera, out int value, out IntPtr error);
        [DllImport(Lib, EntryPoint = "x_uint")] internal static extern void Uint_AsVoid(IntPtr camera, out int value, out IntPtr error);
        [DllImport(Lib, EntryPoint = "x_uint")] internal static extern uint Uint_ValueByPointer(IntPtr camera, int value, out IntPtr error);
        [DllImport(Lib, EntryPoint = "x_bool")] internal static extern bool Bool_Default(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_bool")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Bool_MarshalledAsBool(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_bool")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool Bool_MarshalledAsU1(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_set_bool")] internal static extern void SetBool_Default(IntPtr obj, bool value);
        [DllImport(Lib, EntryPoint = "x_set_bool")] internal static extern void SetBool_MarshalledAsI1(IntPtr obj, [MarshalAs(UnmanagedType.I1)] bool value);
        [DllImport(Lib, EntryPoint = "x_set_bool")] internal static extern void SetBool_AsInt(IntPtr obj, int value);
        [DllImport(Lib, EntryPoint = "x_enum")] internal static extern int Enum_AsInt(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_enum")] internal static extern uint Enum_AsUInt(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_enum")] internal static extern ManagedAuto Enum_AsManagedEnum(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_enum")] internal static extern long Enum_AsLong(IntPtr obj);
        [DllImport(Lib, EntryPoint = "x_size")] internal static extern UIntPtr Size_AsUIntPtr(string name);
        [DllImport(Lib, EntryPoint = "x_size")] internal static extern IntPtr Size_AsIntPtr(string name);
        [DllImport(Lib, EntryPoint = "x_size")] internal static extern uint Size_AsUInt(string name);
        [DllImport(Lib, EntryPoint = "x_pixel_format")] internal static extern void PixelFormat_AsUInt(uint format);
        [DllImport(Lib, EntryPoint = "x_pixel_format")] internal static extern void PixelFormat_AsInt(int format);
        [DllImport(Lib, EntryPoint = "x_two")] internal static extern void Two_AsStringAndDelegate(string a, Callback b);
        [DllImport(Lib, EntryPoint = "x_two")] internal static extern void Two_AsOne(IntPtr a);
        [DllImport(Lib, EntryPoint = "x_variadic")] internal static extern void Variadic(IntPtr a);
        [DllImport(Lib, EntryPoint = "x_unknown")] internal static extern IntPtr Unknown(IntPtr a);
    }
#pragma warning restore IDE1006, SYSLIB1054, CA5392
}
