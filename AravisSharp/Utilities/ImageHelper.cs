using AravisSharp.Native;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AravisSharp.Utilities;

/// <summary>
/// Helper utilities for working with Aravis buffers and images
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Saves a buffer to a raw binary file
    /// </summary>
    public static void SaveToRawFile(AravisSharp.Buffer buffer, string filename)
    {
        if (buffer.Status != ArvBufferStatus.Success)
        {
            throw new InvalidOperationException($"Cannot save buffer with status: {buffer.Status}");
        }

        var data = buffer.CopyData();
        File.WriteAllBytes(filename, data);
    }

    /// <summary>
    /// Saves a buffer to a PGM file (for mono images)
    /// </summary>
    public static void SaveToPgm(AravisSharp.Buffer buffer, string filename)
    {
        EnsureSuccess(buffer);

        var width = buffer.Width;
        var height = buffer.Height;
        var pixelFormat = buffer.PixelFormat;

        // Only support mono 8-bit for PGM
        if (pixelFormat != ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8)
        {
            throw new NotSupportedException($"PGM format only supports MONO_8. Current format: 0x{pixelFormat:X8}");
        }

        // PGM allows exactly one whitespace byte after maxval, so write "\n", never "\r\n".
        using var file = File.Create(filename);
        file.Write(System.Text.Encoding.ASCII.GetBytes($"P5\n{width} {height}\n255\n"));
        file.Write(GetPackedPixels(buffer, 1));
        GC.KeepAlive(buffer);
    }

    /// <summary>
    /// Saves a buffer to a PNG file. Accepted pixel formats: Mono8, Mono10, Mono12, Mono14 and
    /// Mono16 (scaled to 8 bits), RGB8, BGR8, RGBA8, BGRA8, and the 8-bit Bayer formats
    /// BayerRG8, BayerGR8, BayerGB8 and BayerBG8, which are demosaiced bilinearly to RGB
    /// (see <see cref="DemosaicBayer8"/>; a diagnostics path with no performance budget).
    /// </summary>
    /// <exception cref="InvalidOperationException">The buffer status is not success, or it holds less than a whole image.</exception>
    /// <exception cref="NotSupportedException">Any other pixel format.</exception>
    public static void SaveToPng(AravisSharp.Buffer buffer, string filename)
    {
        EnsureSuccess(buffer);
        var pixelFormat = buffer.PixelFormat;
        if (!IsImageSharpFormat(pixelFormat))
            throw new NotSupportedException($"PNG saving not supported for pixel format: {GetPixelFormatName(pixelFormat)} (0x{pixelFormat:X8})");

        using var image = LoadImage(buffer);
        image.SaveAsPng(filename);
    }

    /// <summary>
    /// Saves a buffer to a JPEG file. Accepted pixel formats: Mono8, RGB8, and the 8-bit Bayer
    /// formats BayerRG8, BayerGR8, BayerGB8 and BayerBG8, which are demosaiced bilinearly to
    /// RGB (see <see cref="DemosaicBayer8"/>; a diagnostics path with no performance budget).
    /// </summary>
    /// <exception cref="InvalidOperationException">The buffer status is not success, or it holds less than a whole image.</exception>
    /// <exception cref="NotSupportedException">Any other pixel format.</exception>
    public static void SaveToJpeg(AravisSharp.Buffer buffer, string filename, int quality = 90)
    {
        EnsureSuccess(buffer);
        var pixelFormat = buffer.PixelFormat;
        if (pixelFormat != ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8 &&
            pixelFormat != ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED &&
            !IsBayerFormat(pixelFormat))
            throw new NotSupportedException($"JPEG saving not supported for pixel format: {GetPixelFormatName(pixelFormat)}");

        using var image = LoadImage(buffer);
        image.SaveAsJpeg(filename, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = quality });
    }

    private static void EnsureSuccess(AravisSharp.Buffer buffer)
    {
        if (buffer.Status != ArvBufferStatus.Success)
        {
            throw new InvalidOperationException($"Cannot save buffer with status: {buffer.Status}");
        }
    }

    private static bool IsImageSharpFormat(uint pixelFormat)
    {
        return IsMonoFormat(pixelFormat) || IsColorFormat(pixelFormat) || IsBayerFormat(pixelFormat);
    }

    /// <summary>
    /// Copies the buffer into an ImageSharp image. The pixels are copied, so the image
    /// does not depend on the buffer once this returns.
    /// </summary>
    private static Image LoadImage(AravisSharp.Buffer buffer)
    {
        var width = buffer.Width;
        var height = buffer.Height;
        var pixelFormat = buffer.PixelFormat;
        var data = GetPackedPixels(buffer, GetBytesPerPixel(pixelFormat));

        Image image;
        if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8)
        {
            image = Image.LoadPixelData<L8>(data, width, height);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_10 ||
                 pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12 ||
                 pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_14 ||
                 pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16)
        {
            // 10/12/14/16-bit: stored as little-endian uint16, normalize to 8-bit
            int bitsPerPixel = pixelFormat switch
            {
                ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_10 => 10,
                ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12 => 12,
                ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_14 => 14,
                _ => 16,
            };
            int maxVal = (1 << bitsPerPixel) - 1;
            var pixels = new L8[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                ushort raw = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i * 2));
                pixels[i] = new L8((byte)(Math.Min((int)raw, maxVal) * 255 / maxVal));
            }
            image = Image.LoadPixelData<L8>(pixels, width, height);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED)
        {
            image = Image.LoadPixelData<Rgb24>(data, width, height);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGBA_8_PACKED)
        {
            image = Image.LoadPixelData<Rgba32>(data, width, height);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BGR_8_PACKED)
        {
            image = Image.LoadPixelData<Bgr24>(data, width, height);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BGRA_8_PACKED)
        {
            image = Image.LoadPixelData<Bgra32>(data, width, height);
        }
        else if (IsBayerFormat(pixelFormat))
        {
            image = Image.LoadPixelData<Rgb24>(DemosaicBayer8(data, width, height, pixelFormat), width, height);
        }
        else
        {
            throw new NotSupportedException($"Unsupported pixel format: {GetPixelFormatName(pixelFormat)} (0x{pixelFormat:X8})");
        }

        // The span above points into the native buffer: keep it alive until the copy is done.
        GC.KeepAlive(buffer);
        return image;
    }

    /// <summary>
    /// Returns the image pixels without row padding, after checking the buffer holds a whole image.
    /// The span may point into the buffer's native memory: keep the buffer alive while using it.
    /// </summary>
    private static ReadOnlySpan<byte> GetPackedPixels(AravisSharp.Buffer buffer, int bytesPerPixel)
    {
        var width = buffer.Width;
        var height = buffer.Height;
        var (xPadding, _) = buffer.GetImagePadding();
        var data = buffer.GetDataSpan();

        long rowBytes = (long)width * bytesPerPixel;
        long stride = rowBytes + xPadding;
        if (width <= 0 || height <= 0 || data.Length < stride * (height - 1) + rowBytes)
        {
            throw new InvalidOperationException(
                $"Buffer holds {data.Length} bytes, too few for a {width}x{height} image with {xPadding} bytes of row padding.");
        }

        if (xPadding == 0)
            return data.Slice(0, (int)(rowBytes * height));

        var packed = new byte[rowBytes * height];
        for (int y = 0; y < height; y++)
        {
            data.Slice((int)(y * stride), (int)rowBytes).CopyTo(packed.AsSpan((int)(y * rowBytes)));
        }
        return packed;
    }

    /// <summary>
    /// Converts an 8-bit Bayer mosaic to packed RGB (3 bytes per pixel) with bilinear
    /// interpolation: each missing colour is the mean, rounded half up, of the pixels that
    /// carry it in the 3x3 window around the pixel, the window being clipped at the image border.
    /// </summary>
    /// <param name="data">Mosaic data, one byte per pixel, rows of <paramref name="width"/> bytes with no padding.
    /// Bytes past <c>width * height</c> are ignored.</param>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <param name="pixelFormat">One of the 8-bit Bayer formats: BayerRG8, BayerGR8, BayerGB8 or BayerBG8
    /// (GenICam PFNC naming, the first two letters are the top-left 2x2 cell's first row).</param>
    /// <returns>A new array of <c>width * height * 3</c> bytes, packed R, G, B.</returns>
    /// <remarks>
    /// A diagnostics helper with no performance budget: it runs in managed code, reads up to
    /// nine pixels per output pixel and allocates the whole RGB image. Each pixel keeps its own
    /// measured sample. A colour absent from the window, which happens only on images one
    /// pixel wide or one pixel high, is 0.
    /// </remarks>
    /// <exception cref="NotSupportedException"><paramref name="pixelFormat"/> is not an 8-bit Bayer format.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="data"/> holds fewer than <c>width * height</c> bytes.</exception>
    public static byte[] DemosaicBayer8(ReadOnlySpan<byte> data, int width, int height, uint pixelFormat)
    {
        // Position of the red pixel inside the 2x2 Bayer cell
        (int redX, int redY) = pixelFormat switch
        {
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8 => (0, 0),
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8 => (1, 0),
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GB_8 => (0, 1),
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8 => (1, 1),
            _ => throw new NotSupportedException($"Not an 8-bit Bayer format: {GetPixelFormatName(pixelFormat)}")
        };

        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be positive.");

        // In long: width * height in int can wrap and pass the length check.
        long pixelCount = (long)width * height;
        if (data.Length < pixelCount)
            throw new ArgumentException($"Bayer data is too small for a {width}x{height} image. Required: {pixelCount}, available: {data.Length}.", nameof(data));

        // pixelCount <= data.Length <= int.MaxValue, so every pixel index below fits in int,
        // and so does every RGB index once the array is allocated.
        var rgb = new byte[checked(pixelCount * 3)];
        Span<int> sum = stackalloc int[3];
        Span<int> count = stackalloc int[3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sum.Clear();
                count.Clear();

                for (int ny = Math.Max(y - 1, 0); ny <= Math.Min(y + 1, height - 1); ny++)
                {
                    for (int nx = Math.Max(x - 1, 0); nx <= Math.Min(x + 1, width - 1); nx++)
                    {
                        int channel = BayerChannel(nx, ny, redX, redY);
                        sum[channel] += data[ny * width + nx];
                        count[channel]++;
                    }
                }

                int ownChannel = BayerChannel(x, y, redX, redY);
                int pixel = y * width + x;
                int o = pixel * 3;
                for (int c = 0; c < 3; c++)
                {
                    rgb[o + c] = c == ownChannel
                        ? data[pixel]
                        : count[c] == 0 ? (byte)0 : (byte)((sum[c] + count[c] / 2) / count[c]);
                }
            }
        }

        return rgb;
    }

    /// <summary>0 = red, 1 = green, 2 = blue: the colour the Bayer pixel at (x, y) measures.</summary>
    private static int BayerChannel(int x, int y, int redX, int redY)
    {
        bool redRow = (y & 1) == redY;
        bool redColumn = (x & 1) == redX;
        return redRow == redColumn ? (redRow ? 0 : 2) : 1;
    }

    /// <summary>
    /// Gets a string representation of a pixel format
    /// </summary>
    public static string GetPixelFormatName(uint pixelFormat)
    {
        return pixelFormat switch
        {
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8 => "Mono8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_10 => "Mono10",
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12 => "Mono12",
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_14 => "Mono14",
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16 => "Mono16",
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8 => "BayerGR8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8 => "BayerRG8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GB_8 => "BayerGB8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8 => "BayerBG8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED => "RGB8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_BGR_8_PACKED => "BGR8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_RGBA_8_PACKED => "RGBA8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_BGRA_8_PACKED => "BGRA8",
            ArvPixelFormat.ARV_PIXEL_FORMAT_YUV_422_PACKED => "YUV422",
            ArvPixelFormat.ARV_PIXEL_FORMAT_YUV_422_YUYV_PACKED => "YUYV",
            _ => $"Unknown (0x{pixelFormat:X8})"
        };
    }

    /// <summary>
    /// Calculates the bytes per pixel for a given pixel format
    /// </summary>
    public static int GetBytesPerPixel(uint pixelFormat)
    {
        return pixelFormat switch
        {
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8 => 1,
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_10 => 2,
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12 => 2,
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_14 => 2,
            ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16 => 2,
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8 => 1,
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8 => 1,
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GB_8 => 1,
            ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8 => 1,
            ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED => 3,
            ArvPixelFormat.ARV_PIXEL_FORMAT_BGR_8_PACKED => 3,
            ArvPixelFormat.ARV_PIXEL_FORMAT_RGBA_8_PACKED => 4,
            ArvPixelFormat.ARV_PIXEL_FORMAT_BGRA_8_PACKED => 4,
            ArvPixelFormat.ARV_PIXEL_FORMAT_YUV_422_PACKED => 2,
            ArvPixelFormat.ARV_PIXEL_FORMAT_YUV_422_YUYV_PACKED => 2,
            _ => throw new NotSupportedException($"Unknown pixel format: 0x{pixelFormat:X8}")
        };
    }

    /// <summary>
    /// Calculates expected buffer size for given dimensions and pixel format
    /// </summary>
    public static int CalculateBufferSize(int width, int height, uint pixelFormat)
    {
        var bytesPerPixel = GetBytesPerPixel(pixelFormat);
        return width * height * bytesPerPixel;
    }

    /// <summary>
    /// Checks if a pixel format is a Bayer pattern
    /// </summary>
    public static bool IsBayerFormat(uint pixelFormat)
    {
        return pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GR_8 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_RG_8 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_GB_8 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BAYER_BG_8;
    }

    /// <summary>
    /// Checks if a pixel format is monochrome
    /// </summary>
    public static bool IsMonoFormat(uint pixelFormat)
    {
        return pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_10 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_12 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_14 ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_16;
    }

    /// <summary>
    /// Checks if a pixel format is color
    /// </summary>
    public static bool IsColorFormat(uint pixelFormat)
    {
        return pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BGR_8_PACKED ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGBA_8_PACKED ||
               pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BGRA_8_PACKED;
    }
}
