using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class ImageValidationOptions
{
    public const string SectionName = "ImageValidation";

    [Required]
    public string[] AllowedFormats { get; set; } = ["Jpeg", "Png", "WebP"];

    [Range(1, 10000)]
    public int MinWidth { get; set; } = 100;

    [Range(1, 10000)]
    public int MinHeight { get; set; } = 100;

    [Range(100, 50000)]
    public int MaxWidth { get; set; } = 10000;

    [Range(100, 50000)]
    public int MaxHeight { get; set; } = 10000;

    [Range(10000, 200_000_000)]
    public long MaxPixels { get; set; } = 50_000_000;
}
