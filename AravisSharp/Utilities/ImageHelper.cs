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
    /// Saves a buffer to a PNG file (supports Mono8, RGB, RGBA)
    /// </summary>
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
    /// Saves a buffer to a JPEG file
    /// </summary>
    public static void SaveToJpeg(AravisSharp.Buffer buffer, string filename, int quality = 90)
    {
        EnsureSuccess(buffer);
        var pixelFormat = buffer.PixelFormat;
        if (pixelFormat != ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8 &&
            pixelFormat != ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED)
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
        return IsMonoFormat(pixelFormat) || IsColorFormat(pixelFormat);
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
