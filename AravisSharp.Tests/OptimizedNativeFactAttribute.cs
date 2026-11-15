using System.Diagnostics;

namespace AravisSharp.Tests;

/// <summary>
/// A fact that is reported as skipped, not passed, when it cannot check anything: when the
/// native Aravis library is missing, or when AravisSharp is built with the JIT optimizer
/// disabled (a Debug build). Object lifetime defects only exist in optimized code: in a
/// Debug build the JIT keeps every reference alive to the end of its method.
/// </summary>
internal sealed class OptimizedNativeFactAttribute : NativeFactAttribute
{
    public OptimizedNativeFactAttribute()
    {
        if (Skip == null && IsLibraryJitOptimizerDisabled())
        {
            Skip = "AravisSharp is built with the JIT optimizer disabled (Debug); this lifetime test " +
                   "is only meaningful on an optimized build. Run with -c Release, as CI does.";
        }
    }

    internal static bool IsLibraryJitOptimizerDisabled()
    {
        var attributes = typeof(Stream).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
        return attributes.Length > 0 && ((DebuggableAttribute)attributes[0]).IsJITOptimizerDisabled;
    }
}
