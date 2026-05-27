using AravisSharp.Native;
namespace AravisSharp.Tests;

internal static class NativeTestEnvironment
{
    private static readonly Lazy<bool> AravisAvailable = new(AravisLibrary.IsAravisAvailable);
    public const string MissingAravisMessage = "Native Aravis 0.8 library is not installed or not available in the test output.";

    public static bool IsAravisAvailable => AravisAvailable.Value;
}

internal sealed class NativeFactAttribute : FactAttribute
{
    public NativeFactAttribute()
    {
        if (!NativeTestEnvironment.IsAravisAvailable)
        {
            Skip = NativeTestEnvironment.MissingAravisMessage;
        }
    }
}
