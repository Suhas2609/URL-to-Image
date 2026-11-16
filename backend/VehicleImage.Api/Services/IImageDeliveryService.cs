namespace VehicleImage.Api.Services;

public sealed record ImageDeliveryResult(
    Stream Stream,
    string ContentType,
    long FileSize,
    string DownloadFileName
);

public interface IImageDeliveryService
{
    Task<ImageDeliveryResult> OpenImageAsync(Guid imageId, CancellationToken cancellationToken = default);
}
