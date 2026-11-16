using VehicleImage.Api.Models;

namespace VehicleImage.Api.Services;

public interface IImageIngestionService
{
    Task<IngestedImage> IngestAsync(string imageUrl, CancellationToken cancellationToken = default);
}
