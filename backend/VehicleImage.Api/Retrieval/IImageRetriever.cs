using VehicleImage.Api.Models;

namespace VehicleImage.Api.Retrieval;

public interface IImageRetriever
{
    string Name { get; }
    Task<RetrievedImage> RetrieveAsync(Uri source, CancellationToken cancellationToken = default);
}
