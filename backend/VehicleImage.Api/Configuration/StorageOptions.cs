using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(AllowEmptyStrings = false)]
    public string RootPath { get; set; } = string.Empty;
}
