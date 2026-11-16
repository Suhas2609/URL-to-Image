using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.DTOs.Vehicles;

public sealed class ReplaceVehicleImageRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(2048, MinimumLength = 1)]
    public string ImageUrl { get; set; } = string.Empty;
}
