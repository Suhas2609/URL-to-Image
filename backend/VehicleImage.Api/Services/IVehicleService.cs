using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;

namespace VehicleImage.Api.Services;

public interface IVehicleService
{
    Task<ImageSubmissionResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<ImageSubmissionResponse> ReplaceImageAsync(Guid vehicleId, ReplaceVehicleImageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<VehicleResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<VehicleResponse> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
