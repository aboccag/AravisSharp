namespace AravisSharp.Tests;

/// <summary>
/// The millisecond to microsecond conversion of <see cref="Stream.PopBuffer(ulong)"/>
/// (issue #12). Pure: no native call, so a wrong cap is caught here rather than by GLib
/// aborting the process inside the native pop.
/// </summary>
public class StreamTimeoutTests
{
    // long.MaxValue / 2000, written out so the test does not repeat the implementation.
    private const ulong CapMs = 4_611_686_018_427_387;
    private const ulong CapUs = CapMs * 1000;

    // 100 years (36 525 days), in microseconds.
    private const ulong HundredYearsUs = 36_525UL * 86_400 * 1_000_000;

    [Fact]
    public void Zero_GivesZero()
    {
        Assert.Equal(0UL, Stream.ToTimeoutMicroseconds(0));
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(1_000UL)]
    [InlineData(1_500UL)]
    [InlineData(86_400_000UL)]
    [InlineData(CapMs - 1)]
    public void BelowCap_IsMultipliedByThousand(ulong timeoutMs)
    {
        Assert.Equal(timeoutMs * 1000, Stream.ToTimeoutMicroseconds(timeoutMs));
    }

    [Theory]
    [InlineData(CapMs)]
    [InlineData(CapMs + 1)]
    [InlineData((ulong)long.MaxValue)]
    [InlineData(ulong.MaxValue - 1)]
    [InlineData(ulong.MaxValue)]
    public void AtOrAboveCap_GivesCap(ulong timeoutMs)
    {
        Assert.Equal(CapUs, Stream.ToTimeoutMicroseconds(timeoutMs));
    }

    [Fact]
    public void Cap_StaysWellBelowSignedOverflow()
    {
        // GLib adds the timeout to a signed 64-bit monotonic time.
        Assert.True(CapUs <= (ulong)long.MaxValue / 2);
        Assert.True(Stream.ToTimeoutMicroseconds(ulong.MaxValue - 1) <= (ulong)long.MaxValue / 2);
    }

    [Theory]
    [InlineData(CapMs)]
    [InlineData(ulong.MaxValue - 1)]
    public void LargestInputs_WaitAtLeastHundredYears(ulong timeoutMs)
    {
        Assert.True(Stream.ToTimeoutMicroseconds(timeoutMs) >= HundredYearsUs);
    }

    [Fact]
    public void Conversion_IsMonotonic()
    {
        var samples = new List<ulong> { 0, 1, 2, 999, 1000, 1001, CapMs - 1, CapMs, CapMs + 1, ulong.MaxValue - 1, ulong.MaxValue };
        for (var shift = 0; shift < 64; shift++)
        {
            var power = 1UL << shift;
            samples.Add(power - 1);
            samples.Add(power);
            samples.Add(power + 1);
        }
        samples.Sort();

        var previous = Stream.ToTimeoutMicroseconds(samples[0]);
        foreach (var timeoutMs in samples.Skip(1))
        {
            var current = Stream.ToTimeoutMicroseconds(timeoutMs);
            Assert.True(current >= previous, $"{timeoutMs} ms gives {current} us, below the previous {previous} us");
            previous = current;
        }
    }
}
