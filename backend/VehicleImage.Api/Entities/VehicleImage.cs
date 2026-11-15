namespace VehicleImage.Api.Entities;

public class VehicleImage
{
    public Guid Id {get; set;}
    public Guid VehicleId {get; set;}
    public required string SourceUrl {get; set;}
    public string? FinalSourceUrl {get; set;}
    public required string StorageKey {get; set;}
    public required string ContentType {get; set;}
    public long FileSize {get; set;}
    public int Width {get; set;}
    public int Height {get; set;}
    public required string Format {get; set;}
    public required string RetrievalStrategy {get; set;}
    public required string Status {get; set;}
    public string? FailureReason {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
    public required Vehicle Vehicle {get; set;}
}