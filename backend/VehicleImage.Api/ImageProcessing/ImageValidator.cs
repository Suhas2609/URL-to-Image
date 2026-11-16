using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.ImageProcessing;

public sealed class ImageValidator
{
    private readonly ImageValidationOptions _options;
    private readonly SixLabors.ImageSharp.Configuration _decoderConfiguration;

    public ImageValidator(IOptions<ImageValidationOptions> options)
    {
        _options = options.Value;
        _decoderConfiguration = SixLabors.ImageSharp.Configuration.Default.Clone();
        _decoderConfiguration.MaxDegreeOfParallelism = 1;
        _decoderConfiguration.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
        {
            MaximumPoolSizeMegabytes = 64,
            AllocationLimitMegabytes = 256
        });
    }

    public async Task<ImageMetadata> ValidateAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanSeek)
        {
            throw new ArgumentException("The image content stream must be seekable.", nameof(content));
        }

        if (content.Length == 0)
        {
            throw ImageIngestionException.UnsupportedContentType("The downloaded response body is empty.");
        }

        content.Position = 0;

        byte[] header = new byte[(int)Math.Min(512, content.Length)];
        int headerRead = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        ReadOnlySpan<byte> headerSpan = header.AsSpan(0, headerRead);

        // 1. Signature inspection (never trust Content-Type header or file extension)
        var (formatKind, contentType, extension) = InspectSignature(headerSpan);

        if (!_options.AllowedFormats.Any(f => string.Equals(f, formatKind.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            throw ImageIngestionException.UnsupportedImageFormat(formatKind.ToString().ToUpperInvariant());
        }

        // For JPEG, verify the stream is not truncated (ends with EOI marker FF D9)
        if (formatKind == ImageFormatKind.Jpeg && !HasValidJpegEndOfImage(content))
        {
            throw ImageIngestionException.ImageDecodeFailed(
                "The JPEG stream is truncated (missing End-Of-Image marker).",
                isTransient: true);
        }

        // For PNG, inspect IHDR width & height (bytes 16..23) immediately to catch dimension bombs before chunk decoding
        if (formatKind == ImageFormatKind.Png && headerSpan.Length >= 24)
        {
            int ihdrWidth = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(headerSpan.Slice(16, 4));
            int ihdrHeight = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(headerSpan.Slice(20, 4));
            if (ihdrWidth > 0 && ihdrHeight > 0)
            {
                long ihdrPixels = (long)ihdrWidth * ihdrHeight;
                if (ihdrWidth > _options.MaxWidth || ihdrHeight > _options.MaxHeight || ihdrPixels > _options.MaxPixels)
                {
                    throw ImageIngestionException.ImageDimensionsExceeded(
                        ihdrWidth, ihdrHeight, _options.MaxWidth, _options.MaxHeight, _options.MaxPixels);
                }
            }
        }

        // 2. Identify dimensions and frame count without decoding pixels
        content.Position = 0;
        ImageInfo imageInfo;
        try
        {
            var decoderOptions = new DecoderOptions
            {
                Configuration = _decoderConfiguration
            };
            imageInfo = await Image.IdentifyAsync(decoderOptions, content, cancellationToken)
                ?? throw ImageIngestionException.ImageDecodeFailed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageIngestionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw ImageIngestionException.ImageDecodeFailed(inner: ex);
        }

        if (imageInfo.FrameMetadataCollection.Count > 1)
        {
            throw ImageIngestionException.UnsupportedImageFormat("Animated");
        }

        int width = imageInfo.Width;
        int height = imageInfo.Height;
        long totalPixels = (long)width * height;

        // 3. Enforce dimension ceilings and decompression-bomb pixel ceiling before full decode
        if (width > _options.MaxWidth || height > _options.MaxHeight || totalPixels > _options.MaxPixels)
        {
            throw ImageIngestionException.ImageDimensionsExceeded(
                width, height, _options.MaxWidth, _options.MaxHeight, _options.MaxPixels);
        }

        if (width < _options.MinWidth || height < _options.MinHeight)
        {
            throw ImageIngestionException.ImageTooSmall(
                width, height, _options.MinWidth, _options.MinHeight);
        }

        // 4. Full pixel decode to detect structural corruption
        content.Position = 0;
        try
        {
            var decoderOptions = new DecoderOptions
            {
                Configuration = _decoderConfiguration,
                MaxFrames = 1
            };
            using var decoded = await Image.LoadAsync<Rgba32>(decoderOptions, content, cancellationToken);
            if (decoded.Width != width || decoded.Height != height)
            {
                throw ImageIngestionException.ImageDecodeFailed("Decoded dimensions did not match image header.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageIngestionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw ImageIngestionException.ImageDecodeFailed(
                "The image failed full pixel decoding due to corrupted data.",
                isTransient: true,
                inner: ex);
        }

        // 5. Compute SHA-256 hash and reset stream to position 0
        content.Position = 0;
        byte[] hashBytes = await SHA256.HashDataAsync(content, cancellationToken);
        string sha256 = Convert.ToHexStringLower(hashBytes);

        content.Position = 0;

        return new ImageMetadata(
            Format: formatKind,
            ContentType: contentType,
            FileExtension: extension,
            Width: width,
            Height: height,
            FileSize: content.Length,
            Sha256: sha256);
    }

    private static (ImageFormatKind Kind, string ContentType, string Extension) InspectSignature(ReadOnlySpan<byte> header)
    {
        // JPEG: FF D8 FF
        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return (ImageFormatKind.Jpeg, "image/jpeg", ".jpg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header.Length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return (ImageFormatKind.Png, "image/png", ".png");
        }

        // WebP: "RIFF" + 4 size bytes + "WEBP"
        if (header.Length >= 12 &&
            header[0] == (byte)'R' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'F' &&
            header[8] == (byte)'W' &&
            header[9] == (byte)'E' &&
            header[10] == (byte)'B' &&
            header[11] == (byte)'P')
        {
            return (ImageFormatKind.WebP, "image/webp", ".webp");
        }

        // Recognised but unsupported image formats -> UNSUPPORTED_IMAGE_FORMAT
        // GIF87a or GIF89a
        if (header.Length >= 6 &&
            header[0] == (byte)'G' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'8' &&
            (header[4] == (byte)'7' || header[4] == (byte)'9') &&
            header[5] == (byte)'a')
        {
            throw ImageIngestionException.UnsupportedImageFormat("GIF");
        }

        // BMP: "BM"
        if (header.Length >= 2 &&
            header[0] == (byte)'B' &&
            header[1] == (byte)'M')
        {
            throw ImageIngestionException.UnsupportedImageFormat("BMP");
        }

        // TIFF: "II*\0" or "MM\0*"
        if (header.Length >= 4 &&
            ((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00) ||
             (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A)))
        {
            throw ImageIngestionException.UnsupportedImageFormat("TIFF");
        }

        // ICO: 00 00 01 00
        if (header.Length >= 4 &&
            header[0] == 0x00 &&
            header[1] == 0x00 &&
            header[2] == 0x01 &&
            header[3] == 0x00)
        {
            throw ImageIngestionException.UnsupportedImageFormat("ICO");
        }

        // ISO BMFF (AVIF / HEIC): bytes 4..7 == "ftyp"
        if (header.Length >= 12 &&
            header[4] == (byte)'f' &&
            header[5] == (byte)'t' &&
            header[6] == (byte)'y' &&
            header[7] == (byte)'p')
        {
            throw ImageIngestionException.UnsupportedImageFormat("AVIF/HEIF");
        }

        // Check for HTML, XML, SVG, or JSON text content -> UNSUPPORTED_CONTENT_TYPE
        string textSample = Encoding.UTF8.GetString(header).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (textSample.StartsWith('<') ||
            textSample.Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
            textSample.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            textSample.StartsWith('{') ||
            textSample.StartsWith('['))
        {
            throw ImageIngestionException.UnsupportedContentType(
                "The retrieved resource is HTML, XML, SVG or JSON rather than a binary image.");
        }

        throw ImageIngestionException.UnsupportedContentType();
    }

    private static bool HasValidJpegEndOfImage(Stream content)
    {
        if (content.Length < 4)
        {
            return false;
        }

        int tailLength = (int)Math.Min(16, content.Length);
        Span<byte> tail = stackalloc byte[tailLength];
        content.Position = content.Length - tailLength;
        int read = content.Read(tail);
        tail = tail[..read];

        for (int i = tail.Length - 2; i >= 0; i--)
        {
            if (tail[i] == 0xFF && tail[i + 1] == 0xD9)
            {
                return true;
            }
        }

        return false;
    }
}
