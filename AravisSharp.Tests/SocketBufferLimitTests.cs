namespace AravisSharp.Tests;

/// <summary>
/// <see cref="Stream.GetSocketBufferSizeLimit"/> (issue #17): the cap the operating system
/// puts on a socket receive buffer, read from <c>net.core.rmem_max</c> on Linux. Pure: no
/// camera and no native library. The check against the kernel's own figure is
/// <c>GigEStreamTests.SocketBufferSizeLimit_ShouldMatchWhatTheKernelGrants</c>.
/// </summary>
public class SocketBufferLimitTests
{
    [Fact]
    public void GetSocketBufferSizeLimit_ShouldBeRmemMaxOnLinuxAndNullElsewhere()
    {
        int? limit = Stream.GetSocketBufferSizeLimit();

        // A container may not expose net.core.rmem_max: the documented result is null too.
        if (!OperatingSystem.IsLinux() || !File.Exists(Stream.SocketBufferSizeLimitPath))
        {
            Assert.Null(limit);
            return;
        }

        string text = File.ReadAllText(Stream.SocketBufferSizeLimitPath).Trim();
        Assert.Equal(int.Parse(text, System.Globalization.CultureInfo.InvariantCulture), limit);
        Assert.True(limit > 0, $"net.core.rmem_max read as {limit}");
    }

    [Theory]
    [InlineData("212992\n", 212992)]
    [InlineData("  4194304  \n", 4194304)]
    [InlineData("2147483647", int.MaxValue)]
    public void ReadSocketBufferSizeLimit_ValidFile_ShouldReturnItsValue(string content, int expected)
    {
        WithFile(content, path => Assert.Equal(expected, Stream.ReadSocketBufferSizeLimit(path)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483648")]      // above int.MaxValue
    [InlineData("4096 87380 6291456")] // a three-value sysctl such as tcp_rmem
    [InlineData("4194304.0")]
    public void ReadSocketBufferSizeLimit_MalformedFile_ShouldReturnNull(string content)
    {
        WithFile(content, path => Assert.Null(Stream.ReadSocketBufferSizeLimit(path)));
    }

    [Fact]
    public void ReadSocketBufferSizeLimit_MissingFile_ShouldReturnNull()
    {
        string path = Path.Combine(Path.GetTempPath(), "aravissharp-" + Guid.NewGuid().ToString("N"), "rmem_max");

        Assert.Null(Stream.ReadSocketBufferSizeLimit(path));
    }

    [Fact]
    public void ReadSocketBufferSizeLimit_PathIsADirectory_ShouldReturnNull()
    {
        string directory = Path.Combine(Path.GetTempPath(), "aravissharp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Assert.Null(Stream.ReadSocketBufferSizeLimit(directory));
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    private static void WithFile(string content, Action<string> test)
    {
        string path = Path.Combine(Path.GetTempPath(), "aravissharp-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, content);
        try
        {
            test(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
