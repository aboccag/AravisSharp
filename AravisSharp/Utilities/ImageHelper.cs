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
        if (buffer.Status != ArvBufferStatus.Success)
        {
            throw new InvalidOperationException($"Cannot save buffer with status: {buffer.Status}");
        }

        var width = buffer.Width;
        var height = buffer.Height;
        var pixelFormat = buffer.PixelFormat;

        // Only support mono 8-bit for PGM
        if (pixelFormat != ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8)
        {
            throw new NotSupportedException($"PGM format only supports MONO_8. Current format: 0x{pixelFormat:X8}");
        }

        var data = buffer.CopyData();
        
        using var writer = new StreamWriter(filename);
        writer.WriteLine("P5");
        writer.WriteLine($"{width} {height}");
        writer.WriteLine("255");
        writer.Flush();
        
        using var stream = writer.BaseStream;
        stream.Write(data, 0, data.Length);
    }

    /// <summary>
    /// Saves a buffer to a PNG file (supports Mono8, RGB, RGBA)
    /// </summary>
    public static void SaveToPng(AravisSharp.Buffer buffer, string filename)
    {
        if (buffer.Status != ArvBufferStatus.Success)
        {
            throw new InvalidOperationException($"Cannot save buffer with status: {buffer.Status}");
        }

        var width = buffer.Width;
        var height = buffer.Height;
        var pixelFormat = buffer.PixelFormat;
        var data = buffer.GetDataSpan();

        if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8)
        {
            // 8-bit grayscale
            var image = Image.LoadPixelData<L8>(data, width, height);
            image.SaveAsPng(filename);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED)
        {
            // RGB 24-bit
            var image = Image.LoadPixelData<Rgb24>(data, width, height);
            image.SaveAsPng(filename);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGBA_8_PACKED)
        {
            // RGBA 32-bit
            var image = Image.LoadPixelData<Rgba32>(data, width, height);
            image.SaveAsPng(filename);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BGR_8_PACKED)
        {
            // BGR 24-bit
            var image = Image.LoadPixelData<Bgr24>(data, width, height);
            image.SaveAsPng(filename);
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_BGRA_8_PACKED)
        {
            // BGRA 32-bit
            var image = Image.LoadPixelData<Bgra32>(data, width, height);
            image.SaveAsPng(filename);
        }
        else
        {
            throw new NotSupportedException($"PNG saving not supported for pixel format: {GetPixelFormatName(pixelFormat)} (0x{pixelFormat:X8})");
        }
    }

    /// <summary>
    /// Saves a buffer to a JPEG file
    /// </summary>
    public static void SaveToJpeg(AravisSharp.Buffer buffer, string filename, int quality = 90)
    {
        if (buffer.Status != ArvBufferStatus.Success)
        {
            throw new InvalidOperationException($"Cannot save buffer with status: {buffer.Status}");
        }

        var width = buffer.Width;
        var height = buffer.Height;
        var pixelFormat = buffer.PixelFormat;
        var data = buffer.GetDataSpan();

        if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_MONO_8)
        {
            var image = Image.LoadPixelData<L8>(data, width, height);
            image.SaveAsJpeg(filename, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = quality });
        }
        else if (pixelFormat == ArvPixelFormat.ARV_PIXEL_FORMAT_RGB_8_PACKED)
        {
            var image = Image.LoadPixelData<Rgb24>(data, width, height);
            image.SaveAsJpeg(filename, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = quality });
        }
        else
        {
            throw new NotSupportedException($"JPEG saving not supported for pixel format: {GetPixelFormatName(pixelFormat)}");
        }
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
