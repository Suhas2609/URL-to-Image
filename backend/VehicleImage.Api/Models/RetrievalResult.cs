namespace VehicleImage.Api.Models;

public sealed record RetrievalResult(
    RetrievedImage Image,
    string Strategy,
    IReadOnlyList<Uri> AttemptedSources,
    TimeSpan Elapsed
);
