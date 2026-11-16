namespace VehicleImage.Api.ImageProcessing;

public enum ImageFormatKind
{
    Jpeg,
    Png,
    WebP
}

public sealed record ImageMetadata(
    ImageFormatKind Format,
    string ContentType,
    string FileExtension,
    int Width,
    int Height,
    long FileSize,
    string Sha256
);
