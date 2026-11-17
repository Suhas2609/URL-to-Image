using VehicleImage.Api.DTOs.Vehicles;

namespace VehicleImage.Api.Services;

public interface IVehicleImportService
{
    Task<VehicleImportResponse> ImportCsvAsync(IFormFile file, CancellationToken cancellationToken = default);
}
