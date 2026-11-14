using System;
using AravisSharp.Native;
using AravisSharp.Utilities;
using Xunit;

namespace AravisSharp.Tests;

/// <summary>
/// <see cref="ImageHelper.DemosaicBayer8"/> on synthetic mosaics (issue #6, AC-1 to AC-4).
/// Pure tests: no native library, no camera.
/// </summary>
public class BayerDemosaicTests
{
    // PFNC naming: red at (redX, redY) of the 2 x 2 cell, blue on the opposite corner,
    // green on the other two.
    public static TheoryData<uint, int, int> BayerPatterns => new()
    {
        { ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8, 0, 0 },
        { ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8, 1, 0 },
        { ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GB_8, 0, 1 },
        { ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8, 1, 1 },
    };

    /// <summary>0 = red, 1 = green, 2 = blue: the channel a pixel of the mosaic measures.</summary>
    private static int OwnChannel(int x, int y, int redX, int redY)
    {
        bool redRow = (y & 1) == redY, redColumn = (x & 1) == redX;
        return redRow && redColumn ? 0 : !redRow && !redColumn ? 2 : 1;
    }

    // AC-1
    [Theory]
    [MemberData(nameof(BayerPatterns))]
    public void UniformScene_ShouldBeRecoveredExactly(uint pixelFormat, int redX, int redY)
    {
        const int width = 6, height = 4;
        byte[] scene = { 200, 120, 40 }; // R, G, B: three distinct values so a swap shows

        var mosaic = new byte[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                mosaic[y * width + x] = scene[OwnChannel(x, y, redX, redY)];

        var rgb = ImageHelper.DemosaicBayer8(mosaic, width, height, pixelFormat);

        Assert.Equal(width * height * 3, rgb.Length);
        for (int i = 0; i < width * height; i++)
        {
            Assert.True(rgb[i * 3] == scene[0] && rgb[i * 3 + 1] == scene[1] && rgb[i * 3 + 2] == scene[2],
                $"pixel ({i % width}, {i / width}): got ({rgb[i * 3]}, {rgb[i * 3 + 1]}, {rgb[i * 3 + 2]}), " +
                $"expected ({scene[0]}, {scene[1]}, {scene[2]})");
        }
    }

    // AC-2
    [Theory]
    [MemberData(nameof(BayerPatterns))]
    public void RandomMosaic_ShouldKeepEveryMeasuredSample(uint pixelFormat, int redX, int redY)
    {
        // Odd sizes so that the last row and column end on an incomplete 2 x 2 cell.
        const int width = 7, height = 5;
        var mosaic = new byte[width * height];
        new Random(42).NextBytes(mosaic);

        var rgb = ImageHelper.DemosaicBayer8(mosaic, width, height, pixelFormat);

        Assert.Equal(width * height * 3, rgb.Length);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int channel = OwnChannel(x, y, redX, redY);
                byte expected = mosaic[y * width + x];
                byte actual = rgb[(y * width + x) * 3 + channel];
                Assert.True(expected == actual,
                    $"pixel ({x}, {y}) channel {channel}: expected {expected}, got {actual}");
            }
        }
    }

    [Fact]
    public void MissingChannel_ShouldBeTheRoundedMeanOfItsNeighbours()
    {
        // 3 x 3 BayerRG8: the centre (1, 1) is a blue sample. Its four red diagonal
        // neighbours sum to 42 (mean 10.5) and its four green neighbours to 82 (mean 20.5):
        // round half up gives 11 and 21, truncation would give 10 and 20.
        byte[] mosaic =
        {
            10, 20, 11,
            21, 99, 21,
            11, 20, 10,
        };

        var rgb = ImageHelper.DemosaicBayer8(mosaic, 3, 3, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8);

        Assert.Equal(new byte[] { 11, 21, 99 }, rgb[12..15]);
    }

    [Fact]
    public void BorderPixel_ShouldAverageOnlyTheNeighboursInsideTheImage()
    {
        // 3 x 3 BayerRG8, corner (0, 0) is red. Inside the clipped 2 x 2 window: greens at
        // (1, 0) = 20 and (0, 1) = 21 (mean 20.5 -> 21), blue at (1, 1) = 99.
        byte[] mosaic =
        {
            10, 20, 11,
            21, 99, 21,
            11, 20, 10,
        };

        var rgb = ImageHelper.DemosaicBayer8(mosaic, 3, 3, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8);

        Assert.Equal(new byte[] { 10, 21, 99 }, rgb[0..3]);
    }

    [Fact]
    public void LongerData_ShouldUseOnlyTheFirstWidthTimesHeightBytes()
    {
        byte[] mosaic = { 10, 20, 21, 99, 255, 255 };

        var rgb = ImageHelper.DemosaicBayer8(mosaic, 2, 2, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8);

        Assert.Equal(new byte[] { 10, 21, 99, 10, 20, 99, 10, 21, 99, 10, 21, 99 }, rgb);
    }

    // AC-3
    [Theory]
    [InlineData(ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8)]
    [InlineData(ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16)]
    [InlineData(ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED)]
    [InlineData(0u)]
    public void NonBayer8Format_ShouldThrowNotSupported(uint pixelFormat)
    {
        Assert.Throws<NotSupportedException>(() =>
            ImageHelper.DemosaicBayer8(new byte[16], 4, 4, pixelFormat));
    }

    [Theory]
    [InlineData(0, 4, "width")]
    [InlineData(-1, 4, "width")]
    [InlineData(int.MinValue, 4, "width")]
    [InlineData(4, 0, "height")]
    [InlineData(4, -1, "height")]
    [InlineData(4, int.MinValue, "height")]
    public void NonPositiveDimension_ShouldThrowNamingTheParameter(int width, int height, string expected)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageHelper.DemosaicBayer8(new byte[16], width, height, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8));
        Assert.Equal(expected, ex.ParamName);
    }

    [Theory]
    [InlineData(15, 4, 4)]
    [InlineData(0, 1, 1)]
    public void ShortData_ShouldThrowArgumentException(int length, int width, int height)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ImageHelper.DemosaicBayer8(new byte[length], width, height, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8));
        Assert.Equal("data", ex.ParamName);
    }

    [Theory]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(65536, 65536)] // width * height overflows int to 0
    [InlineData(46341, 46341)] // width * height overflows int to a negative number
    public void HugeDimensions_ShouldThrowArgumentExceptionWithoutOverflow(int width, int height)
    {
        // width * height (* 3) computed in int would wrap and could pass the length check;
        // the check must be done without overflow and fail before any allocation.
        var ex = Assert.Throws<ArgumentException>(() =>
            ImageHelper.DemosaicBayer8(new byte[16], width, height, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8));
        Assert.Equal("data", ex.ParamName);
    }

    // AC-4
    [Theory]
    // BayerBG8 puts red at (1, 1): an image one pixel wide or one pixel high never holds a
    // red sample, so R is 0 everywhere. Mosaic {10, 20, 30, 40} reads blue, green, blue,
    // green along the single column or row.
    [InlineData(1, 4)]
    [InlineData(4, 1)]
    public void OnePixelWideOrHigh_ShouldNotThrowAndZeroAbsentChannels(int width, int height)
    {
        byte[] mosaic = { 10, 20, 30, 40 };

        var rgb = ImageHelper.DemosaicBayer8(mosaic, width, height, ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8);

        byte[] expected =
        {
            0, 20, 10, // blue sample, green from its single neighbour
            0, 20, 20, // green sample, blue = round((10 + 30) / 2)
            0, 30, 30, // blue sample, green = round((20 + 40) / 2)
            0, 40, 30, // green sample, blue from its single neighbour
        };
        Assert.Equal(expected, rgb);
    }

    [Theory]
    [MemberData(nameof(BayerPatterns))]
    public void SinglePixel_ShouldKeepItsSampleAndZeroTheOtherChannels(uint pixelFormat, int redX, int redY)
    {
        var rgb = ImageHelper.DemosaicBayer8(new byte[] { 77 }, 1, 1, pixelFormat);

        var expected = new byte[3];
        expected[OwnChannel(0, 0, redX, redY)] = 77;
        Assert.Equal(expected, rgb);
    }
}
