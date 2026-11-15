namespace AravisSharp.Tests;

/// <summary>
/// A fact that is reported as skipped, not passed, in a 32-bit process. The package ships
/// 64-bit runtimes only, and the layouts it pins are those of the 64-bit targets.
/// </summary>
internal sealed class SixtyFourBitFactAttribute : FactAttribute
{
    public SixtyFourBitFactAttribute()
    {
        if (!Environment.Is64BitProcess)
            Skip = "Pins a 64-bit layout; this process is 32-bit.";
    }
}
