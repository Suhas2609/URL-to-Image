using System.Text;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;

namespace VehicleImage.UnitTests;

public class ImageValidatorTests
{
    private readonly ImageValidator _validator;

    public ImageValidatorTests()
    {
        var options = Options.Create(new ImageValidationOptions
        {
            AllowedFormats = ["Jpeg", "Png", "WebP"],
            MinWidth = 100,
            MinHeight = 100,
            MaxWidth = 2000,
            MaxHeight = 2000,
            MaxPixels = 2_000_000
        });
        _validator = new ImageValidator(options);
    }

    [Fact]
    public async Task ValidateAsync_ValidJpeg_ReturnsMetadataAndResetsStreamPosition()
    {
        using var stream = await CreateImageStreamAsync(320, 240, "jpeg");
        ImageMetadata metadata = await _validator.ValidateAsync(stream);

        Assert.Equal(ImageFormatKind.Jpeg, metadata.Format);
        Assert.Equal("image/jpeg", metadata.ContentType);
        Assert.Equal(".jpg", metadata.FileExtension);
        Assert.Equal(320, metadata.Width);
        Assert.Equal(240, metadata.Height);
        Assert.Equal(stream.Length, metadata.FileSize);
        Assert.Equal(64, metadata.Sha256.Length);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task ValidateAsync_ValidPng_ReturnsMetadata()
    {
        using var stream = await CreateImageStreamAsync(200, 150, "png");
        ImageMetadata metadata = await _validator.ValidateAsync(stream);

        Assert.Equal(ImageFormatKind.Png, metadata.Format);
        Assert.Equal("image/png", metadata.ContentType);
        Assert.Equal(".png", metadata.FileExtension);
        Assert.Equal(200, metadata.Width);
        Assert.Equal(150, metadata.Height);
    }

    [Fact]
    public async Task ValidateAsync_ValidWebP_ReturnsMetadata()
    {
        using var stream = await CreateImageStreamAsync(250, 180, "webp");
        ImageMetadata metadata = await _validator.ValidateAsync(stream);

        Assert.Equal(ImageFormatKind.WebP, metadata.Format);
        Assert.Equal("image/webp", metadata.ContentType);
        Assert.Equal(".webp", metadata.FileExtension);
        Assert.Equal(250, metadata.Width);
        Assert.Equal(180, metadata.Height);
    }

    [Fact]
    public async Task ValidateAsync_TruncatedJpeg_ThrowsImageDecodeFailed()
    {
        using var fullStream = await CreateImageStreamAsync(300, 200, "jpeg");
        byte[] fullBytes = fullStream.ToArray();
        using var truncatedStream = new MemoryStream(fullBytes, 0, fullBytes.Length / 2);

        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(truncatedStream));

        Assert.Equal(ErrorCodes.ImageDecodeFailed, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_CorruptedPngPayload_ThrowsImageDecodeFailed()
    {
        using var fullStream = await CreateImageStreamAsync(200, 200, "png");
        byte[] bytes = fullStream.ToArray();
        // Corrupt the IDAT compressed chunk data near the middle while keeping header intact
        for (int i = bytes.Length / 2; i < Math.Min(bytes.Length - 12, (bytes.Length / 2) + 32); i++)
        {
            bytes[i] ^= 0xFF;
        }

        using var corruptedStream = new MemoryStream(bytes);
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(corruptedStream));

        Assert.Equal(ErrorCodes.ImageDecodeFailed, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedGifAndBmpFormats_ThrowsUnsupportedImageFormat()
    {
        using var gifStream = await CreateImageStreamAsync(200, 200, "gif");
        var gifEx = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(gifStream));
        Assert.Equal(ErrorCodes.UnsupportedImageFormat, gifEx.Code);

        using var bmpStream = await CreateImageStreamAsync(200, 200, "bmp");
        var bmpEx = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(bmpStream));
        Assert.Equal(ErrorCodes.UnsupportedImageFormat, bmpEx.Code);
    }

    [Theory]
    [InlineData("<!DOCTYPE html><html><body><h1>404 Not Found</h1></body></html>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\"><script>alert(1)</script></svg>")]
    [InlineData("<?xml version=\"1.0\"?><svg><rect width=\"100\" height=\"100\"/></svg>")]
    [InlineData("{\"error\": \"not an image\"}")]
    [InlineData("Plain text pretending to be car.jpg")]
    public async Task ValidateAsync_NonImageOrSvgContent_ThrowsUnsupportedContentType(string payload)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(stream));

        Assert.Equal(ErrorCodes.UnsupportedContentType, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_ImageBelowMinDimensions_ThrowsImageTooSmall()
    {
        using var tinyStream = await CreateImageStreamAsync(64, 64, "png");
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(tinyStream));

        Assert.Equal(ErrorCodes.ImageTooSmall, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_ImageExceedingMaxDimensionsOrPixels_ThrowsImageDimensionsExceeded()
    {
        // Craft a minimal PNG header (IHDR) declaring 15000 x 15000 dimensions (225M pixels)
        // without allocating a huge image in the test!
        using var basePng = await CreateImageStreamAsync(120, 120, "png");
        byte[] bytes = basePng.ToArray();
        // PNG IHDR width is at bytes 16..19 (big-endian), height at bytes 20..23
        WriteBigEndianInt32(bytes.AsSpan(16, 4), 15000);
        WriteBigEndianInt32(bytes.AsSpan(20, 4), 15000);

        using var bombStream = new MemoryStream(bytes);
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(bombStream));

        Assert.Equal(ErrorCodes.ImageDimensionsExceeded, ex.Code);
    }

    private static async Task<MemoryStream> CreateImageStreamAsync(int width, int height, string format)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)((x * 7) % 256), (byte)((y * 13) % 256), 140, 255);
            }
        }

        var ms = new MemoryStream();
        switch (format)
        {
            case "jpeg":
                await image.SaveAsJpegAsync(ms);
                break;
            case "png":
                await image.SaveAsPngAsync(ms);
                break;
            case "webp":
                await image.SaveAsWebpAsync(ms);
                break;
            case "gif":
                await image.SaveAsGifAsync(ms);
                break;
            case "bmp":
                await image.SaveAsBmpAsync(ms);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }

        ms.Position = 0;
        return ms;
    }

    private static void WriteBigEndianInt32(Span<byte> target, int value)
    {
        target[0] = (byte)((value >> 24) & 0xFF);
        target[1] = (byte)((value >> 16) & 0xFF);
        target[2] = (byte)((value >> 8) & 0xFF);
        target[3] = (byte)(value & 0xFF);
    }
}
