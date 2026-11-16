namespace VehicleImage.Api.Models;

public sealed record IngestedImage(
    Guid ImageId,
    string StorageKey,
    string ContentType,
    string Format,
    long FileSize,
    int Width,
    int Height,
    string Sha256,
    string SourceUrl,
    string FinalSourceUrl,
    string RetrievalStrategy
);
