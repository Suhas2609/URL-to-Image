namespace VehicleImage.Api.Models;

public sealed class RetrievedImage : IDisposable, IAsyncDisposable
{
    public required Stream Content { get; init; }
    public string? ContentType { get; init; }
    public required Uri SourceUrl { get; init; }
    public required Uri FinalSourceUrl { get; init; }
    public required long FileSize { get; init; }
    public required string Strategy { get; init; }

    public void Dispose()
    {
        Content.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Content.DisposeAsync();
    }
}
