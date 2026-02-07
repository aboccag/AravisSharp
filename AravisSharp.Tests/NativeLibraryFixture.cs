using System.Runtime.CompilerServices;
using AravisSharp.Native;

namespace AravisSharp.Tests;

/// <summary>
/// Registers the native library resolver once before any test code executes.
/// </summary>
internal static class NativeLibraryInitializer
{
    [ModuleInitializer]
    internal static void Init()
    {
        AravisLibrary.RegisterResolver();
    }
}
