namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record VehicleImportResponse(
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    IReadOnlyList<VehicleImportRowResult> Results
);

public sealed record VehicleImportRowResult(
    int RowNumber,
    string? Make,
    string? Model,
    int? Year,
    bool Success,
    Guid? VehicleId,
    Guid? ImageId,
    string? ImageUrl,
    string? ErrorCode,
    string? ErrorMessage
);
