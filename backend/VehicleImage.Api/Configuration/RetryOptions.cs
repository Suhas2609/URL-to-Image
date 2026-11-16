using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class RetryOptions
{
    public const string SectionName = "Retry";

    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 3;

    [Range(10, 10000)]
    public int BaseDelayMs { get; set; } = 150;

    [Range(50, 30000)]
    public int MaxDelayMs { get; set; } = 1500;

    public bool EnableJitter { get; set; } = true;

    [Range(1, 30)]
    public int MaxRetryAfterSeconds { get; set; } = 3;
}
