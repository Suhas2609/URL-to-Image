using System.Text.Json.Serialization;

namespace VehicleImage.Api.DTOs.Images;

public sealed record ImageFailureResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("traceId")] string TraceId,
    [property: JsonPropertyName("retryable")] bool Retryable,
    [property: JsonPropertyName("retryAfterSeconds")] int? RetryAfterSeconds = null,
    [property: JsonPropertyName("type")] string StatusType = "https://tools.ietf.org/html/rfc4918#section-11.2"
);
