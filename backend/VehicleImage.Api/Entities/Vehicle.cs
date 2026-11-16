namespace VehicleImage.Api.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public string? Variant { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public VehicleImage? Image { get; set; }
}