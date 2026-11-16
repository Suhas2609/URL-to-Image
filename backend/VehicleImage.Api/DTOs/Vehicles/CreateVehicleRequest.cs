using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.DTOs.Vehicles;

public sealed class CreateVehicleRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Make { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Variant { get; set; }

    [Required]
    public int Year { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(2048, MinimumLength = 1)]
    public string ImageUrl { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        int maxYear = DateTime.UtcNow.Year + 1;
        if (Year < 1886 || Year > maxYear)
        {
            yield return new ValidationResult(
                $"Year must be between 1886 and {maxYear}.",
                [nameof(Year)]);
        }
    }
}
