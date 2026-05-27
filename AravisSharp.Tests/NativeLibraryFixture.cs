using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AravisSharp.Generated;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Registers the native library resolver once before any test code executes.
/// </summary>
internal static class NativeLibraryInitializer
{
    private const string FakeInterfaceName = "Fake";

    [ModuleInitializer]
    internal static void Init()
    {
        AravisLibrary.RegisterResolver();

        var idPtr = Marshal.StringToCoTaskMemUTF8(FakeInterfaceName);
        try
        {
            AravisGenerated.arv_enable_interface(idPtr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(idPtr);
        }
    }
}
