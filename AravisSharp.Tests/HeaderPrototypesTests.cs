using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AravisSharp.Tests;

/// <summary>
/// The header reader (<see cref="HeaderModel"/>) and the comparison rules
/// (<see cref="PInvokeSignatureCheck"/>) on synthetic header snippets and declarations (issue
/// #11). Pure: no header files, no native library.
/// </summary>
public partial class HeaderPrototypesTests
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
        var quarkToString = model.Find("g_quark_to_string")!;
        Assert.Equal(new[] { "GQuark" }, quarkToString.Parameters);
        Assert.Equal(TypeFamily.Pointer, model.Resolve(quarkToString.ReturnType));
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

        // The inline definition is not a prototype, and nothing of its body is read as one.
        Assert.Equal(1, model.FunctionCount);
        Assert.Null(model.Find("g_set_object"));
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

    [Theory]
    [InlineData("void x_attr (gint a) __attribute__((x));")]
    [InlineData("void x_attr (gint a __attribute__((unused)));")]
    [InlineData("void x_attr (gint a, (gint b));")]
    [InlineData("void x_attr (gint a));")]
    public void Reader_LeavesOutAPrototypeItCannotRead(string header) =>
        Assert.Null(Read(header).Find("x_attr"));

    [Fact]
    public void Reader_ReadsAFunctionPointerParameter()
    {
        var model = Read("void x_callback (gint a, void (*callback) (gpointer data, gint b), gpointer user_data);");

        Assert.Equal(new[] { "gint", "(function pointer) *", "gpointer" }, model.Find("x_callback")!.Parameters);
    }

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
        void x_u64 (gpointer o, guint64 *value);
        void x_i64 (gpointer o, gint64 *value);
        void x_size_out (gpointer o, size_t *value);
        void x_buffer_out (gpointer o, ArvBuffer **buffer);
        void x_bool_out (gpointer o, gboolean *value);
        void x_c_bool_out (gpointer o, bool *value);
        void x_value (GValue *value);
        void x_enum_out (ArvAuto *value);
        const char *x_string (gpointer o);
        void x_string_out (gpointer o, char **value);
        void x_u16 (guint16 value);
        void x_gchar (gchar value);
        void x_glong (glong value);
        gulong x_gulong (void);
        void x_c_bool (bool value);
        """;

    private static MethodInfo Fixture(string name) =>
        typeof(Fixtures).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static IReadOnlyList<string> CheckFixture(string name, IReadOnlyDictionary<string, string>? allowList = null) =>
        PInvokeSignatureCheck.Check([Fixture(name)], Read(Header), allowList ?? new Dictionary<string, string>());

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
    [InlineData(nameof(Fixtures.U64_AsOutULong))]
    [InlineData(nameof(Fixtures.U64_AsRefULong))]
    [InlineData(nameof(Fixtures.U64_AsULongArray))]
    [InlineData(nameof(Fixtures.SizeOut_AsOutUIntPtr))]
    [InlineData(nameof(Fixtures.BufferOut_AsOutIntPtr))]
    [InlineData(nameof(Fixtures.BoolOut_AsOutBool))]
    [InlineData(nameof(Fixtures.CBoolOut_AsOutBoolU1))]
    [InlineData(nameof(Fixtures.Value_AsRefStruct))]
    [InlineData(nameof(Fixtures.EnumOut_AsOutManagedEnum))]
    [InlineData(nameof(Fixtures.String_AsIntPtr))]
    [InlineData(nameof(Fixtures.StringOut_AsOutIntPtr))]
    [InlineData(nameof(Fixtures.U16_AsUShort))]
    [InlineData(nameof(Fixtures.GChar_AsSByte))]
    [InlineData(nameof(Fixtures.GLong_AsCLong))]
    [InlineData(nameof(Fixtures.GULong_AsCULong))]
    [InlineData(nameof(Fixtures.CBool_AsU1))]
    [InlineData(nameof(Fixtures.LibSetBool_MarshalledAsBool))]
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
    [InlineData(nameof(Fixtures.U64_AsOutUInt), "argument 2 (value) points to 'guint64' (UInt64) in C, out UInt32")]
    [InlineData(nameof(Fixtures.U64_AsUIntArray), "argument 2 (value) points to 'guint64' (UInt64) in C, UInt32[]")]
    [InlineData(nameof(Fixtures.I64_AsOutInt), "argument 2 (value) points to 'gint64' (Int64) in C, out Int32")]
    [InlineData(nameof(Fixtures.SizeOut_AsOutUInt), "argument 2 (value) points to 'size_t' (PointerSized) in C, out UInt32")]
    [InlineData(nameof(Fixtures.BoolOut_AsOutBoolU1), "argument 2 (value) points to 'gboolean' (Bool32) in C, [MarshalAs(U1)] out Boolean")]
    [InlineData(nameof(Fixtures.String_AsString), "x_string (Fixtures.String_AsString): return is a managed string")]
    [InlineData(nameof(Fixtures.String_AsStringBuilder), "x_string (Fixtures.String_AsStringBuilder): return is a managed string")]
    [InlineData(nameof(Fixtures.StringOut_AsOutString), "x_string_out (Fixtures.StringOut_AsOutString): argument 2 (value) is a managed string")]
    [InlineData(nameof(Fixtures.U16_AsChar), "argument 1 (value) is 'guint16' (UInt16) in C, Char")]
    [InlineData(nameof(Fixtures.GChar_AsChar), "argument 1 (value) is 'gchar' (Int8) in C, Char")]
    [InlineData(nameof(Fixtures.GLong_AsLong), "argument 1 (value) is 'glong' (CLong) in C, Int64")]
    [InlineData(nameof(Fixtures.GULong_AsULong), "return is 'gulong' (CULong) in C, UInt64")]
    [InlineData(nameof(Fixtures.CBool_Default), "argument 1 (value) is 'bool' (Bool8) in C, Boolean")]
    [InlineData(nameof(Fixtures.SetBool_MarshalledAsVariantBool), "argument 2 (value) is 'gboolean' (Bool32) in C, [MarshalAs(VariantBool)] Boolean")]
    [InlineData(nameof(Fixtures.Bool_ObjectAsUIntPtr), "argument 1 (obj) is 'gpointer' (Pointer) in C, UIntPtr")]
    [InlineData(nameof(Fixtures.Size_AsLong), "return is 'gsize' (PointerSized) in C, Int64")]
    [InlineData(nameof(Fixtures.Size_AsULong), "return is 'gsize' (PointerSized) in C, UInt64")]
    [InlineData(nameof(Fixtures.LibSetBool_MarshalledAsU1), "x_set_bool (Fixtures.LibSetBool_MarshalledAsU1): argument 2 (value) is 'gboolean' (Bool32) in C, [MarshalAs(U1)] Boolean")]
    public void Check_ReportsAMismatch_NamingTheFunction(string fixture, string expected)
    {
        var failure = Assert.Single(CheckFixture(fixture));
        Assert.Contains(expected, failure);
    }

    [Fact]
    public void Check_ReportsEveryMismatch_InOneList()
    {
        var methods = new[] { nameof(Fixtures.Uint_AsInt), nameof(Fixtures.Bool_MarshalledAsU1), nameof(Fixtures.x_uint) }
            .Select(Fixture);

        var failures = PInvokeSignatureCheck.Check(methods, Read(Header), new Dictionary<string, string>());

        Assert.Collection(failures,
            f => Assert.StartsWith("x_uint (Fixtures.Uint_AsInt): return", f),
            f => Assert.StartsWith("x_bool (Fixtures.Bool_MarshalledAsU1): return", f));
    }

    [Fact]
    public void Check_AcceptsTheEntryPointOfALibraryImport()
    {
        Assert.Equal("x_set_bool", PInvokeSignatureCheck.EntryPoint(Fixture(nameof(Fixtures.LibSetBool_MarshalledAsBool))));
        Assert.Equal("x_set_bool", PInvokeSignatureCheck.EntryPoint(Fixture(nameof(Fixtures.LibSetBool_MarshalledAsU1))));
    }

    private const string ExportHeader = """
        ARV_API void arv_x_public (gpointer a);
        void arv_x_hidden (gpointer a);
        ARV_API G_DECLARE_FINAL_TYPE (ArvXShown, arv_x_shown, ARV, X_SHOWN, GObject)
        void arv_x_after_macro (gpointer a);
        G_DECLARE_FINAL_TYPE (ArvXHiddenObject, arv_x_hidden_object, ARV, X_HIDDEN_OBJECT, GObject)
        typedef enum { ARV_XYZ_MODE_A } ArvXyzMode;
        void g_x_not_aravis (gpointer a);
        """;

    [Theory]
    [InlineData(nameof(Fixtures.arv_x_public))]
    [InlineData(nameof(Fixtures.arv_x_shown_get_type))]
    [InlineData(nameof(Fixtures.arv_xyz_mode_get_type))]
    [InlineData(nameof(Fixtures.g_x_not_aravis))]
    public void Check_AcceptsAnExportedAravisFunction(string fixture) =>
        Assert.Empty(PInvokeSignatureCheck.Check([Fixture(fixture)], Read(ExportHeader, publicAravisHeader: true), new Dictionary<string, string>()));

    [Theory]
    [InlineData(nameof(Fixtures.arv_x_hidden))]
    [InlineData(nameof(Fixtures.arv_x_hidden_object_get_type))]
    [InlineData(nameof(Fixtures.arv_x_after_macro))]
    public void Check_ReportsAnAravisFunctionWithoutArvApi(string fixture)
    {
        var failure = Assert.Single(PInvokeSignatureCheck.Check([Fixture(fixture)], Read(ExportHeader, publicAravisHeader: true), new Dictionary<string, string>()));

        Assert.Contains($"{fixture} (Fixtures.{fixture}): the C prototype (test.h) is not marked ARV_API", failure);
    }

    [Fact]
    public void Check_ReportsTwoDifferentPrototypesOfOneName()
    {
        var model = Read("""
            #ifdef X_NEW
            guint x_twice (gpointer a);
            #else
            gint x_twice (gpointer a);
            #endif
            guint x_same (gpointer a);
            guint  x_same (gpointer   a);
            """);

        var failure = Assert.Single(PInvokeSignatureCheck.Check([Fixture(nameof(Fixtures.Twice_AsUInt))], model, new Dictionary<string, string>()));

        Assert.Contains("x_twice (Fixtures.Twice_AsUInt): 2 different C prototypes", failure);
        Assert.Empty(PInvokeSignatureCheck.Check([Fixture(nameof(Fixtures.Same_AsUInt))], model, new Dictionary<string, string>()));
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

    // A GValue-shaped struct, passed by reference: only its address is compared.
    private readonly record struct FixtureValue(IntPtr GType, long Data0, long Data1);

    // Declarations for the comparison only: never called, the library does not exist.
#pragma warning disable IDE1006, SYSLIB1054, CA5392 // C names, DllImport on purpose
    private static partial class Fixtures
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
        [DllImport(Lib, EntryPoint = "x_u64")] internal static extern void U64_AsOutUInt(IntPtr o, out uint value);
        [DllImport(Lib, EntryPoint = "x_u64")] internal static extern void U64_AsOutULong(IntPtr o, out ulong value);
        [DllImport(Lib, EntryPoint = "x_u64")] internal static extern void U64_AsRefULong(IntPtr o, ref ulong value);
        [DllImport(Lib, EntryPoint = "x_u64")] internal static extern void U64_AsULongArray(IntPtr o, ulong[] value);
        [DllImport(Lib, EntryPoint = "x_u64")] internal static extern void U64_AsUIntArray(IntPtr o, uint[] value);
        [DllImport(Lib, EntryPoint = "x_i64")] internal static extern void I64_AsOutInt(IntPtr o, out int value);
        [DllImport(Lib, EntryPoint = "x_size_out")] internal static extern void SizeOut_AsOutUIntPtr(IntPtr o, out UIntPtr value);
        [DllImport(Lib, EntryPoint = "x_size_out")] internal static extern void SizeOut_AsOutUInt(IntPtr o, out uint value);
        [DllImport(Lib, EntryPoint = "x_buffer_out")] internal static extern void BufferOut_AsOutIntPtr(IntPtr o, out IntPtr buffer);
        [DllImport(Lib, EntryPoint = "x_bool_out")] internal static extern void BoolOut_AsOutBool(IntPtr o, out bool value);
        [DllImport(Lib, EntryPoint = "x_bool_out")] internal static extern void BoolOut_AsOutBoolU1(IntPtr o, [MarshalAs(UnmanagedType.U1)] out bool value);
        [DllImport(Lib, EntryPoint = "x_c_bool_out")] internal static extern void CBoolOut_AsOutBoolU1(IntPtr o, [MarshalAs(UnmanagedType.U1)] out bool value);
        [DllImport(Lib, EntryPoint = "x_value")] internal static extern void Value_AsRefStruct(ref FixtureValue value);
        [DllImport(Lib, EntryPoint = "x_enum_out")] internal static extern void EnumOut_AsOutManagedEnum(out ManagedAuto value);
        [DllImport(Lib, EntryPoint = "x_string")] internal static extern string String_AsString(IntPtr o);
        [DllImport(Lib, EntryPoint = "x_string")] internal static extern StringBuilder String_AsStringBuilder(IntPtr o);
        [DllImport(Lib, EntryPoint = "x_string")] internal static extern IntPtr String_AsIntPtr(IntPtr o);
        [DllImport(Lib, EntryPoint = "x_string_out")] internal static extern void StringOut_AsOutString(IntPtr o, out string value);
        [DllImport(Lib, EntryPoint = "x_string_out")] internal static extern void StringOut_AsOutIntPtr(IntPtr o, out IntPtr value);
        [DllImport(Lib, EntryPoint = "x_u16")] internal static extern void U16_AsChar(char value);
        [DllImport(Lib, EntryPoint = "x_u16")] internal static extern void U16_AsUShort(ushort value);
        [DllImport(Lib, EntryPoint = "x_gchar")] internal static extern void GChar_AsChar(char value);
        [DllImport(Lib, EntryPoint = "x_gchar")] internal static extern void GChar_AsSByte(sbyte value);
        [DllImport(Lib, EntryPoint = "x_glong")] internal static extern void GLong_AsCLong(CLong value);
        [DllImport(Lib, EntryPoint = "x_glong")] internal static extern void GLong_AsLong(long value);
        [DllImport(Lib, EntryPoint = "x_gulong")] internal static extern CULong GULong_AsCULong();
        [DllImport(Lib, EntryPoint = "x_gulong")] internal static extern ulong GULong_AsULong();
        [DllImport(Lib, EntryPoint = "x_c_bool")] internal static extern void CBool_AsU1([MarshalAs(UnmanagedType.U1)] bool value);
        [DllImport(Lib, EntryPoint = "x_c_bool")] internal static extern void CBool_Default(bool value);
        [DllImport(Lib, EntryPoint = "x_set_bool")] internal static extern void SetBool_MarshalledAsVariantBool(IntPtr obj, [MarshalAs(UnmanagedType.VariantBool)] bool value);
        [DllImport(Lib, EntryPoint = "x_bool")] internal static extern bool Bool_ObjectAsUIntPtr(UIntPtr obj);
        [DllImport(Lib, EntryPoint = "x_size")] internal static extern long Size_AsLong(string name);
        [DllImport(Lib, EntryPoint = "x_size")] internal static extern ulong Size_AsULong(string name);
        [DllImport(Lib, EntryPoint = "x_twice")] internal static extern uint Twice_AsUInt(IntPtr a);
        [DllImport(Lib, EntryPoint = "x_same")] internal static extern uint Same_AsUInt(IntPtr a);
        [DllImport(Lib)] internal static extern void arv_x_public(IntPtr a);
        [DllImport(Lib)] internal static extern void arv_x_hidden(IntPtr a);
        [DllImport(Lib)] internal static extern void arv_x_after_macro(IntPtr a);
        [DllImport(Lib)] internal static extern IntPtr arv_x_shown_get_type();
        [DllImport(Lib)] internal static extern IntPtr arv_x_hidden_object_get_type();
        [DllImport(Lib)] internal static extern IntPtr arv_xyz_mode_get_type();
        [DllImport(Lib)] internal static extern void g_x_not_aravis(IntPtr a);

        [LibraryImport(Lib, EntryPoint = "x_set_bool")]
        internal static partial void LibSetBool_MarshalledAsBool(IntPtr obj, [MarshalAs(UnmanagedType.Bool)] bool value);

        [LibraryImport(Lib, EntryPoint = "x_set_bool")]
        internal static partial void LibSetBool_MarshalledAsU1(IntPtr obj, [MarshalAs(UnmanagedType.U1)] bool value);
    }
#pragma warning restore IDE1006, SYSLIB1054, CA5392
}
