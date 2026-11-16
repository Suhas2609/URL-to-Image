namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record ImageSubmissionResponse(
    Guid VehicleId,
    Guid ImageId,
    string Status,
    string ImageUrl
);
