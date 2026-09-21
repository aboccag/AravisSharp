using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        // Aravis may not be installed at all. Probe before calling into it: an exception
        // escaping a module initializer surfaces as a TypeInitializationException while
        // xunit constructs the [NativeFact] attributes, which turns every test in the
        // assembly into a discovery error instead of a clean skip.
        if (!NativeTestEnvironment.IsAravisAvailable)
            return;

        var idPtr = Marshal.StringToCoTaskMemUTF8(FakeInterfaceName);
        try
        {
            AravisNative.arv_enable_interface(idPtr);
        }
        catch (Exception ex) when (ex is DllNotFoundException
                                      or BadImageFormatException
                                      or EntryPointNotFoundException)
        {
            // A partially usable native library (wrong architecture, ABI mismatch) must
            // not take the whole assembly down either — [NativeFact] handles the skip.
        }
        finally
        {
            Marshal.FreeCoTaskMem(idPtr);
        }
    }
}
