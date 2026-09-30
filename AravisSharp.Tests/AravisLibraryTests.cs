using AravisSharp.Native;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// Native library probe order. Pure path logic: no native library needed.
/// </summary>
public class AravisLibraryTests
{
    private static readonly string[] Names = { "libaravis-0.8.so.0", "libaravis-0.8.so" };

    [Fact]
    public void ProbeOrder_TriesApplicationCopiesBeforeTheSystemSearchPath()
    {
        var app = Path.Combine(Path.GetTempPath(), "app");
        var native = Path.Combine(app, "runtimes", "linux-x64", "native");

        var order = AravisLibrary.GetProbeOrder(Names, new[] { app }, "linux-x64").ToList();

        Assert.Equal(new[]
        {
            Path.Combine(app, "libaravis-0.8.so.0"),
            Path.Combine(app, "libaravis-0.8.so"),
            Path.Combine(native, "libaravis-0.8.so.0"),
            Path.Combine(native, "libaravis-0.8.so"),
            "libaravis-0.8.so.0",
            "libaravis-0.8.so",
        }, order);
    }

    [Fact]
    public void ProbeOrder_ProbesEveryRootBeforeFallingBackToBareNames()
    {
        var app = Path.Combine(Path.GetTempPath(), "app");
        var assemblyDir = Path.Combine(Path.GetTempPath(), "lib");

        var order = AravisLibrary.GetProbeOrder(Names, new[] { app, assemblyDir }, "linux-arm64").ToList();

        var firstBareName = order.FindIndex(p => !Path.IsPathRooted(p));
        Assert.Equal(8, firstBareName);
        Assert.All(order.Take(firstBareName), p => Assert.True(
            p.StartsWith(app, StringComparison.Ordinal) || p.StartsWith(assemblyDir, StringComparison.Ordinal)));
        Assert.Contains(Path.Combine(assemblyDir, "runtimes", "linux-arm64", "native", "libaravis-0.8.so.0"), order);
    }
}
