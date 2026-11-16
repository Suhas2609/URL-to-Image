namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record VehicleResponse(
    Guid Id,
    string Make,
    string Model,
    string? Variant,
    int Year,
    Guid? ImageId,
    string? ImageUrl,
    string ImageStatus,
    string? ContentType,
    long? FileSize,
    int? Width,
    int? Height,
    string? Format,
    string? RetrievalStrategy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
