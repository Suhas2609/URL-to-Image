using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class BulkImportOptions
{
    public const string SectionName = "BulkImport";

    /// <summary>
    /// Hard upper bound enforced by the controller's request size limit.
    /// MaxFileBytes cannot be configured above this value.
    /// </summary>
    public const long AbsoluteMaxFileBytes = 5 * 1024 * 1024;

    [Range(1, 500)]
    public int MaxRecords { get; set; } = 50;

    [Range(1024, AbsoluteMaxFileBytes)]
    public long MaxFileBytes { get; set; } = 1024 * 1024; // 1 MB
}
