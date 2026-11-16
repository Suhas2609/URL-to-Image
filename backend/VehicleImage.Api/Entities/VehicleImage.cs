namespace VehicleImage.Api.Entities;

public enum ImageStatus
{
    Processing,
    Completed,
    Failed
}

public class VehicleImage
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public required string SourceUrl { get; set; }
    public required string FinalSourceUrl { get; set; }
    public required string StorageKey { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public required string Format { get; set; }
    public required string RetrievalStrategy { get; set; }
    public ImageStatus Status { get; set; } = ImageStatus.Completed;
    public string? FailureReason { get; set; }
    public required string Sha256 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Vehicle? Vehicle { get; set; }
}