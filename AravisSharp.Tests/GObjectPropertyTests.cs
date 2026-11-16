using System.Reflection;
using System.Runtime.InteropServices;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Issue #14: the GObject property helpers of <see cref="GLibNative"/> reject unknown and
/// mistyped properties and read back their writes (docs/gobject-property-checks-spec.md).
/// They run on GIO objects, without any camera. Property types (gio/gsocketclient.c,
/// gio/gsocket.c, gio/ginetsocketaddress.c): GSocketClient "timeout" guint, "family"
/// GSocketFamily, "tls" gboolean; GSocket "listen-backlog" gint in [0, SOMAXCONN]
/// (default 10); GInetSocketAddress "port" construct-only guint.
/// </summary>
public class GObjectPropertyTests
{
    private const string UnknownProperty = "no-such-property";

    private static void WithSocketClient(Action<IntPtr> body)
    {
        var client = GioTestNative.g_socket_client_new();
        Assert.NotEqual(IntPtr.Zero, client);
        try
        {
            body(client);
        }
        finally
        {
            GLibNative.g_object_unref(client);
        }
    }

    private static void WithSocket(Action<IntPtr> body)
    {
        var socket = GioTestNative.g_socket_new(GioTestNative.SocketFamilyIpv4, GioTestNative.SocketTypeStream,
            GioTestNative.SocketProtocolDefault, out var error);
        try
        {
            Assert.Equal(IntPtr.Zero, error);
            Assert.NotEqual(IntPtr.Zero, socket);
            body(socket);
        }
        finally
        {
            if (error != IntPtr.Zero) GLibNative.g_error_free(error);
            if (socket != IntPtr.Zero) GLibNative.g_object_unref(socket);
        }
    }

    private static void AssertNoGObjectWarning(GObjectLogCapture log) =>
        Assert.True(log.Messages.Count == 0, "GLib-GObject logged: " + string.Join(" | ", log.Messages));

    // === AC-1: unknown property ===

    public static TheoryData<string> AllHelpers() => new()
    {
        "SetIntProperty", "SetUIntProperty", "SetEnumProperty", "SetBoolProperty",
        "GetIntProperty", "GetUIntProperty", "GetEnumProperty", "GetBoolProperty",
    };

    private static void CallHelper(string helper, IntPtr obj, string propertyName, IntPtr enumType)
    {
        // Bound by name so that each helper is tested on its own (one theory case each).
        object?[] arguments = helper switch
        {
            "SetIntProperty" => new object?[] { obj, propertyName, 1 },
            "SetUIntProperty" => new object?[] { obj, propertyName, 1u },
            "SetEnumProperty" => new object?[] { obj, propertyName, enumType, 1 },
            "SetBoolProperty" => new object?[] { obj, propertyName, true },
            "GetEnumProperty" => new object?[] { obj, propertyName, enumType },
            _ => new object?[] { obj, propertyName },
        };
        var method = typeof(GLibNative).GetMethod(helper, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null, arguments.Select(a => a!.GetType()).ToArray(), null);
        Assert.True(method != null, $"GLibNative.{helper} not found");
        try
        {
            method!.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    [NativeTheory]
    [MemberData(nameof(AllHelpers))]
    public void Helper_UnknownProperty_ShouldThrowArgumentExceptionNamingTypeAndProperty(string helper)
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentException>(() =>
                CallHelper(helper, client, UnknownProperty, GioTestNative.g_socket_family_get_type()));
            Assert.Contains(UnknownProperty, ex.Message);
            Assert.Contains("GSocketClient", ex.Message);
            Assert.Equal("propertyName", ex.ParamName);
            AssertNoGObjectWarning(log);
        });
    }

    // === AC-2: property of another type ===

    [NativeFact]
    public void UIntHelpers_OnIntProperty_ShouldThrowArgumentException()
    {
        WithSocket(socket =>
        {
            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentException>(() => GLibNative.SetUIntProperty(socket, "listen-backlog", 5u));
            Assert.Contains("listen-backlog", ex.Message);
            ex = Assert.Throws<ArgumentException>(() => GLibNative.GetUIntProperty(socket, "listen-backlog"));
            Assert.Contains("listen-backlog", ex.Message);
            AssertNoGObjectWarning(log);

            // The refused write did not convert the value: the default is still there.
            Assert.Equal(10, GLibNative.GetIntProperty(socket, "listen-backlog"));
        });
    }

    [NativeFact]
    public void IntHelpers_OnUIntProperty_ShouldThrowArgumentException()
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentException>(() => GLibNative.SetIntProperty(client, "timeout", 5));
            Assert.Contains("timeout", ex.Message);
            ex = Assert.Throws<ArgumentException>(() => GLibNative.GetIntProperty(client, "timeout"));
            Assert.Contains("timeout", ex.Message);
            AssertNoGObjectWarning(log);

            Assert.Equal(0u, GLibNative.GetUIntProperty(client, "timeout"));
        });
    }

    [NativeFact]
    public void EnumHelpers_WithAnotherEnumType_ShouldThrowArgumentException()
    {
        WithSocketClient(client =>
        {
            var socketType = GioTestNative.g_socket_type_get_type();
            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentException>(() =>
                GLibNative.SetEnumProperty(client, "family", socketType, GioTestNative.SocketFamilyIpv4));
            Assert.Contains("family", ex.Message);
            ex = Assert.Throws<ArgumentException>(() => GLibNative.GetEnumProperty(client, "family", socketType));
            Assert.Contains("family", ex.Message);
            AssertNoGObjectWarning(log);

            Assert.Equal(GioTestNative.SocketFamilyInvalid,
                GLibNative.GetEnumProperty(client, "family", GioTestNative.g_socket_family_get_type()));
        });
    }

    [NativeFact]
    public void IntAndUIntHelpers_OnEnumProperty_ShouldThrowArgumentException()
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            Assert.Throws<ArgumentException>(() => GLibNative.SetIntProperty(client, "family", GioTestNative.SocketFamilyIpv4));
            Assert.Throws<ArgumentException>(() => GLibNative.GetIntProperty(client, "family"));
            Assert.Throws<ArgumentException>(() => GLibNative.GetUIntProperty(client, "family"));
            AssertNoGObjectWarning(log);
        });
    }

    [NativeFact]
    public void EnumHelpers_OnIntProperty_ShouldThrowArgumentException()
    {
        WithSocket(socket =>
        {
            var family = GioTestNative.g_socket_family_get_type();
            using var log = new GObjectLogCapture();
            Assert.Throws<ArgumentException>(() => GLibNative.SetEnumProperty(socket, "listen-backlog", family, 5));
            Assert.Throws<ArgumentException>(() => GLibNative.GetEnumProperty(socket, "listen-backlog", family));
            AssertNoGObjectWarning(log);
        });
    }

    // === AC-3: a write GLib refuses is reported ===

    [NativeFact]
    public void SetIntProperty_OutOfDeclaredRange_ShouldThrowAndKeepThePreviousValue()
    {
        WithSocket(socket =>
        {
            GLibNative.SetIntProperty(socket, "listen-backlog", 7);

            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => GLibNative.SetIntProperty(socket, "listen-backlog", -1));
            Assert.Equal("value", ex.ParamName);
            Assert.Contains("listen-backlog", ex.Message);
            Assert.Contains("GSocket", ex.Message);
            Assert.Equal(7, GLibNative.GetIntProperty(socket, "listen-backlog"));

            // GLib's own warning, which the read-back turns into an exception; it also shows
            // that the log capture of the other tests sees GObject warnings at all.
            Assert.NotEmpty(log.Messages);
        });
    }

    [NativeFact]
    public void SetEnumProperty_NotAMember_ShouldThrowAndKeepThePreviousValue()
    {
        WithSocketClient(client =>
        {
            var family = GioTestNative.g_socket_family_get_type();
            GLibNative.SetEnumProperty(client, "family", family, GioTestNative.SocketFamilyIpv4);

            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => GLibNative.SetEnumProperty(client, "family", family, 999));
            Assert.Equal("value", ex.ParamName);
            Assert.Equal(GioTestNative.SocketFamilyIpv4, GLibNative.GetEnumProperty(client, "family", family));
        });
    }

    [NativeFact]
    public void SetUIntProperty_RefusedWrite_ShouldThrowAndKeepThePreviousValue()
    {
        // A construct-only guint: GLib logs a critical and keeps the value.
        var address = GioTestNative.g_inet_socket_address_new_from_string("127.0.0.1", 1);
        Assert.NotEqual(IntPtr.Zero, address);
        try
        {
            using var log = new GObjectLogCapture();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => GLibNative.SetUIntProperty(address, "port", 2u));
            Assert.Equal("value", ex.ParamName);
            Assert.Equal(1u, GLibNative.GetUIntProperty(address, "port"));
        }
        finally
        {
            GLibNative.g_object_unref(address);
        }
    }

    // === AC-4: round trips ===

    [NativeFact]
    public void IntProperty_ShouldRoundTrip()
    {
        WithSocket(socket =>
        {
            using var log = new GObjectLogCapture();
            GLibNative.SetIntProperty(socket, "listen-backlog", 42);
            Assert.Equal(42, GLibNative.GetIntProperty(socket, "listen-backlog"));
            GLibNative.SetIntProperty(socket, "listen-backlog", 0); // the declared minimum
            Assert.Equal(0, GLibNative.GetIntProperty(socket, "listen-backlog"));
            AssertNoGObjectWarning(log);
        });
    }

    [NativeFact]
    public void UIntProperty_ShouldRoundTrip()
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            GLibNative.SetUIntProperty(client, "timeout", 4_000_000_000u); // above int.MaxValue
            Assert.Equal(4_000_000_000u, GLibNative.GetUIntProperty(client, "timeout"));
            GLibNative.SetUIntProperty(client, "timeout", 7u);
            Assert.Equal(7u, GLibNative.GetUIntProperty(client, "timeout"));
            AssertNoGObjectWarning(log);
        });
    }

    [NativeFact]
    public void EnumProperty_ShouldRoundTrip()
    {
        // Stream.SetSocketBufferPolicy and SetPacketResend take this path on ArvGvStream.
        WithSocketClient(client =>
        {
            var family = GioTestNative.g_socket_family_get_type();
            using var log = new GObjectLogCapture();
            GLibNative.SetEnumProperty(client, "family", family, GioTestNative.SocketFamilyIpv4);
            Assert.Equal(GioTestNative.SocketFamilyIpv4, GLibNative.GetEnumProperty(client, "family", family));
            GLibNative.SetEnumProperty(client, "family", family, GioTestNative.SocketFamilyInvalid);
            Assert.Equal(GioTestNative.SocketFamilyInvalid, GLibNative.GetEnumProperty(client, "family", family));
            AssertNoGObjectWarning(log);
        });
    }

    [NativeFact]
    public void BoolProperty_ShouldRoundTrip()
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            GLibNative.SetBoolProperty(client, "tls", true);
            Assert.True(GLibNative.GetBoolProperty(client, "tls"));
            GLibNative.SetBoolProperty(client, "tls", false);
            Assert.False(GLibNative.GetBoolProperty(client, "tls"));
            AssertNoGObjectWarning(log);
        });
    }

    [NativeFact]
    public void BoolHelpers_OnPropertyOfAnotherType_ShouldThrowArgumentException()
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            Assert.Throws<ArgumentException>(() => GLibNative.SetBoolProperty(client, "timeout", true));
            Assert.Throws<ArgumentException>(() => GLibNative.GetBoolProperty(client, "timeout"));
            Assert.Throws<ArgumentException>(() => GLibNative.SetUIntProperty(client, "tls", 1u));
            Assert.Throws<ArgumentException>(() => GLibNative.GetIntProperty(client, "tls"));
            AssertNoGObjectWarning(log);

            Assert.Equal(0u, GLibNative.GetUIntProperty(client, "timeout"));
            Assert.False(GLibNative.GetBoolProperty(client, "tls"));
        });
    }

    [NativeFact]
    public void EnumHelpers_WithANonEnumType_ShouldThrowArgumentException()
    {
        // G_TYPE_INT passes the type check on a gint property, but is no enum type.
        WithSocket(socket =>
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                GLibNative.SetEnumProperty(socket, "listen-backlog", GLibNative.G_TYPE_INT, 5));
            Assert.Equal("enumType", ex.ParamName);
            ex = Assert.Throws<ArgumentException>(() =>
                GLibNative.GetEnumProperty(socket, "listen-backlog", GLibNative.G_TYPE_INT));
            Assert.Equal("enumType", ex.ParamName);
            Assert.Equal(10, GLibNative.GetIntProperty(socket, "listen-backlog"));
        });
    }

    // === v0.8.36 shims forward to the checked helpers ===

#pragma warning disable CS0618 // the obsolete shims are the subject of these tests
    [NativeFact]
    public void LegacyShims_ShouldForwardToTheCheckedHelpers()
    {
        WithSocketClient(client =>
        {
            using var log = new GObjectLogCapture();
            GLibNative.g_object_set_uint(client, "timeout", 7u, IntPtr.Zero);
            GLibNative.g_object_get_uint(client, "timeout", out uint timeout, IntPtr.Zero);
            Assert.Equal(7u, timeout);

            GLibNative.g_object_set_bool(client, "tls", true, IntPtr.Zero);
            GLibNative.g_object_get_bool(client, "tls", out bool tls, IntPtr.Zero);
            Assert.True(tls);

            // v0.8.36's Stream set its enum properties through g_object_set_int.
            GLibNative.g_object_set_int(client, "family", GioTestNative.SocketFamilyIpv4, IntPtr.Zero);
            GLibNative.g_object_get_int(client, "family", out int family, IntPtr.Zero);
            Assert.Equal(GioTestNative.SocketFamilyIpv4, family);
            AssertNoGObjectWarning(log);

            Assert.Throws<ArgumentException>(() => GLibNative.g_object_set_int(client, UnknownProperty, 1, IntPtr.Zero));
            Assert.Throws<ArgumentException>(() => GLibNative.g_object_get_int(client, "timeout", out _, IntPtr.Zero));
            Assert.Throws<ArgumentException>(() => GLibNative.g_object_set_uint(client, "tls", 1u, IntPtr.Zero));
            Assert.Throws<ArgumentException>(() => GLibNative.g_object_get_bool(client, "timeout", out _, IntPtr.Zero));
        });

        WithSocket(socket =>
        {
            GLibNative.g_object_set_int(socket, "listen-backlog", 5, IntPtr.Zero);
            GLibNative.g_object_get_int(socket, "listen-backlog", out int backlog, IntPtr.Zero);
            Assert.Equal(5, backlog);
            Assert.Throws<ArgumentOutOfRangeException>(() => GLibNative.g_object_set_int(socket, "listen-backlog", -1, IntPtr.Zero));
            Assert.Equal(5, GLibNative.GetIntProperty(socket, "listen-backlog"));
        });
    }
#pragma warning restore CS0618

    // === Null arguments ===

    [NativeTheory]
    [MemberData(nameof(AllHelpers))]
    public void Helper_NullArguments_ShouldThrowArgumentNullException(string helper)
    {
        WithSocketClient(client =>
        {
            var family = GioTestNative.g_socket_family_get_type();
            var ex = Assert.Throws<ArgumentNullException>(() => CallHelper(helper, IntPtr.Zero, "tls", family));
            Assert.Equal("obj", ex.ParamName);
            ex = Assert.Throws<ArgumentNullException>(() => CallHelperWithNullName(helper, client, family));
            Assert.Equal("propertyName", ex.ParamName);
        });
    }

    private static void CallHelperWithNullName(string helper, IntPtr obj, IntPtr enumType)
    {
        var method = typeof(GLibNative).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == helper);
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        arguments[0] = obj;
        arguments[1] = null;
        for (int i = 2; i < parameters.Length; i++)
            arguments[i] = parameters[i].ParameterType == typeof(IntPtr) ? enumType : Activator.CreateInstance(parameters[i].ParameterType);
        try
        {
            method.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    // === Public surface (spec, question 1 and 3) ===

    [Theory]
    [MemberData(nameof(AllHelpers))]
    public void PropertyHelpers_ShouldBeInternal(string helper)
    {
        var methods = typeof(GLibNative).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == helper)
            .ToList();
        Assert.Single(methods);
        Assert.True(methods[0].IsAssembly, $"GLibNative.{helper} is not internal");
    }

    [Theory]
    [InlineData("g_object_set_int", typeof(int), false)]
    [InlineData("g_object_set_uint", typeof(uint), false)]
    [InlineData("g_object_set_bool", typeof(bool), false)]
    [InlineData("g_object_get_int", typeof(int), true)]
    [InlineData("g_object_get_uint", typeof(uint), true)]
    [InlineData("g_object_get_bool", typeof(bool), true)]
    public void LegacyGObjectAccessors_ShouldBeObsoleteManagedShimsWithTheV0836Signature(string name, Type valueType, bool isOut)
    {
        // v0.8.36: void name(IntPtr obj, string propertyName, [out] T value, IntPtr terminator).
        var valueParameterType = isOut ? valueType.MakeByRefType() : valueType;
        var method = typeof(GLibNative).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(IntPtr), typeof(string), valueParameterType, typeof(IntPtr) }, null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Equal(new[] { "obj", "propertyName", "value", "terminator" },
            method.GetParameters().Select(p => p.Name).ToArray());
        Assert.Equal(isOut, method.GetParameters()[2].IsOut);

        var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(obsolete);
        Assert.False(obsolete!.IsError);

        Assert.Equal((MethodAttributes)0, method.Attributes & MethodAttributes.PinvokeImpl);
        Assert.Null(method.GetCustomAttribute<DllImportAttribute>());
    }
}
