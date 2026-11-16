using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    [Range(1024, 104_857_600)]
    public long MaxDownloadBytes { get; set; } = 20 * 1024 * 1024; // 20 MB

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    [Range(1, 60)]
    public int ConnectTimeoutSeconds { get; set; } = 5;

    [Range(0, 20)]
    public int MaxRedirects { get; set; } = 5;

    [Required]
    public int[] AllowedPorts { get; set; } = [80, 443];

    [Range(64, 8192)]
    public int MaxUrlLength { get; set; } = 2048;

    [Required(AllowEmptyStrings = false)]
    public string UserAgent { get; set; } = "VehicleImageSystem/1.0";

    [Range(5, 600)]
    public int PooledConnectionLifetimeSeconds { get; set; } = 60;
}
