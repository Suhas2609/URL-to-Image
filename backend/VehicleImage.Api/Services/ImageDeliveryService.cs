using Microsoft.EntityFrameworkCore;
using VehicleImage.Api.Data;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Storage;

namespace VehicleImage.Api.Services;

public sealed class ImageDeliveryService : IImageDeliveryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<ImageDeliveryService> _logger;

    public ImageDeliveryService(
        ApplicationDbContext dbContext,
        IImageStorage imageStorage,
        ILogger<ImageDeliveryService> logger)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    public async Task<ImageDeliveryResult> OpenImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var imageRecord = await _dbContext.VehicleImages
            .AsNoTracking()
            .Include(i => i.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);

        if (imageRecord is null)
        {
            throw ResourceNotFoundException.Image(imageId);
        }

        Stream fileStream;
        try
        {
            fileStream = await _imageStorage.OpenReadAsync(imageRecord.StorageKey, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(
                ex,
                "Data integrity error: VehicleImage row {ImageId} (VehicleId={VehicleId}) references missing file {StorageKey}.",
                imageRecord.Id,
                imageRecord.VehicleId,
                imageRecord.StorageKey);

            throw ResourceNotFoundException.Image(imageId);
        }

        string extension = Path.GetExtension(imageRecord.StorageKey);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = imageRecord.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        string downloadFileName = BuildDownloadFileName(imageRecord.Vehicle?.Make, imageRecord.Vehicle?.Model, imageRecord.Vehicle?.Year, imageId, extension);

        return new ImageDeliveryResult(
            Stream: fileStream,
            ContentType: imageRecord.ContentType,
            FileSize: imageRecord.FileSize,
            DownloadFileName: downloadFileName);
    }

    private static string BuildDownloadFileName(string? make, string? model, int? year, Guid imageId, string extension)
    {
        if (!string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(model) && year.HasValue)
        {
            string safeMake = SanitizeSegment(make);
            string safeModel = SanitizeSegment(model);
            return $"{safeMake}-{safeModel}-{year.Value}-{imageId.ToString("N")[..8]}{extension}";
        }

        return $"vehicle-image-{imageId:N}{extension}";
    }

    private static string SanitizeSegment(string value)
    {
        Span<char> buffer = stackalloc char[Math.Min(value.Length, 32)];
        int idx = 0;
        foreach (char c in value)
        {
            if (idx >= buffer.Length)
            {
                break;
            }
            if (char.IsLetterOrDigit(c))
            {
                buffer[idx++] = char.ToLowerInvariant(c);
            }
            else if (c is ' ' or '-' or '_' && idx > 0 && buffer[idx - 1] != '-')
            {
                buffer[idx++] = '-';
            }
        }
        string result = new string(buffer[..idx]).Trim('-');
        return string.IsNullOrEmpty(result) ? "vehicle" : result;
    }
}
