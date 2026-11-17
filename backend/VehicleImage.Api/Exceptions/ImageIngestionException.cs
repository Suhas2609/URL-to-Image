using Microsoft.AspNetCore.Http;

namespace VehicleImage.Api.Exceptions;

public sealed class ImageIngestionException : Exception
{
    public string Code { get; }
    public bool IsTransient { get; }
    public int? UpstreamStatusCode { get; }
    public TimeSpan? RetryAfter { get; }
    public int HttpStatusCode { get; }

    public ImageIngestionException(
        string code,
        string clientSafeMessage,
        bool isTransient = false,
        int? upstreamStatusCode = null,
        TimeSpan? retryAfter = null,
        int httpStatusCode = StatusCodes.Status422UnprocessableEntity,
        Exception? innerException = null)
        : base(clientSafeMessage, innerException)
    {
        Code = code;
        IsTransient = isTransient;
        UpstreamStatusCode = upstreamStatusCode;
        RetryAfter = retryAfter;
        HttpStatusCode = httpStatusCode;
    }

    public static ImageIngestionException InvalidUrl(string detail = "The supplied URL is malformed or does not meet policy requirements.")
        => new(ErrorCodes.InvalidUrl, detail, isTransient: false);

    public static ImageIngestionException UnsafeDestination(string detail = "The supplied URL points to a restricted or non-public destination.")
        => new(ErrorCodes.UnsafeDestination, detail, isTransient: false);

    public static ImageIngestionException DnsResolutionFailed(string detail = "The supplier hostname could not be resolved.", Exception? inner = null)
        => new(ErrorCodes.DnsResolutionFailed, detail, isTransient: true, innerException: inner);

    public static ImageIngestionException ConnectionTimeout(string detail = "Connecting to the supplier server timed out.", Exception? inner = null)
        => new(ErrorCodes.ConnectionTimeout, detail, isTransient: true, innerException: inner);

    public static ImageIngestionException RequestTimeout(string detail = "Reading the image from the supplier server timed out.", Exception? inner = null)
        => new(ErrorCodes.RequestTimeout, detail, isTransient: true, innerException: inner);

    public static ImageIngestionException TlsFailure(string detail = "TLS certificate or handshake verification with the supplier server failed.", Exception? inner = null)
        => new(ErrorCodes.TlsFailure, detail, isTransient: false, innerException: inner);

    public static ImageIngestionException RedirectLimitExceeded(int maxRedirects)
        => new(ErrorCodes.RedirectLimitExceeded, $"The URL exceeded the maximum of {maxRedirects} redirects.", isTransient: false);

    public static ImageIngestionException HttpNotFound()
        => new(ErrorCodes.HttpNotFound, "The supplier returned HTTP 404 (Not Found) for the image URL.", isTransient: false, upstreamStatusCode: 404);

    public static ImageIngestionException HttpUnauthorized()
        => new(ErrorCodes.HttpUnauthorized, "The supplier requires authentication (HTTP 401) to access this URL.", isTransient: false, upstreamStatusCode: 401);

    public static ImageIngestionException HttpForbidden()
        => new(ErrorCodes.HttpForbidden, "Access to the supplier URL was forbidden (HTTP 403).", isTransient: false, upstreamStatusCode: 403);

    public static ImageIngestionException HttpRateLimited(TimeSpan? retryAfter = null)
        => new(ErrorCodes.HttpRateLimited, "The supplier rate-limited the request (HTTP 429).", isTransient: true, upstreamStatusCode: 429, retryAfter: retryAfter);

    public static ImageIngestionException HttpServerError(int statusCode)
        => new(ErrorCodes.HttpServerError, $"The supplier server returned an upstream error (HTTP {statusCode}).", isTransient: true, upstreamStatusCode: statusCode);

    public static ImageIngestionException HttpError(int statusCode)
        => new(ErrorCodes.HttpError, $"The supplier returned an unexpected status code (HTTP {statusCode}).", isTransient: false, upstreamStatusCode: statusCode);

    public static ImageIngestionException UnsupportedContentType(string detail = "The retrieved resource is not an image.")
        => new(ErrorCodes.UnsupportedContentType, detail, isTransient: false);

    public static ImageIngestionException UnsupportedImageFormat(string formatName)
        => new(ErrorCodes.UnsupportedImageFormat, $"The image format '{formatName}' is not supported. Accepted formats are JPEG, PNG and WebP.", isTransient: false);

    public static ImageIngestionException ImageTooLarge(long maxBytes)
        => new(ErrorCodes.ImageTooLarge, $"The downloaded content exceeded the maximum allowed size of {maxBytes / (1024 * 1024)} MB.", isTransient: false);

    public static ImageIngestionException ImageDimensionsExceeded(int width, int height, int maxWidth, int maxHeight, long maxPixels)
        => new(ErrorCodes.ImageDimensionsExceeded, $"Image dimensions ({width}x{height}) exceed allowed limits (max {maxWidth}x{maxHeight}, {maxPixels:N0} pixels).", isTransient: false);

    public static ImageIngestionException ImageTooSmall(int width, int height, int minWidth, int minHeight)
        => new(ErrorCodes.ImageTooSmall, $"Image dimensions ({width}x{height}) are below the minimum required size of {minWidth}x{minHeight} pixels.", isTransient: false);

    public static ImageIngestionException ImageDecodeFailed(string detail = "The image bytes could not be decoded or are corrupted.", bool isTransient = false, Exception? inner = null)
        => new(ErrorCodes.ImageDecodeFailed, detail, isTransient: isTransient, innerException: inner);

    public static ImageIngestionException NoImageFound(string detail = "No usable vehicle image candidate could be found on the supplied HTML page.")
        => new(ErrorCodes.NoImageFound, detail, isTransient: false);

    public static ImageIngestionException UnsupportedSource(string detail = "The supplied source URL type is not supported.")
        => new(ErrorCodes.UnsupportedSource, detail, isTransient: false);

    public static ImageIngestionException ProviderAuthRequired(string detail = "The supplier resource requires authentication and is not publicly accessible.")
        => new(ErrorCodes.ProviderAuthRequired, detail, isTransient: false);

    public static ImageIngestionException StorageFailed(string detail = "Failed to persist or read the image in internal storage.", Exception? inner = null)
        => new(ErrorCodes.StorageFailed, detail, isTransient: false, httpStatusCode: StatusCodes.Status500InternalServerError, innerException: inner);

    public static ImageIngestionException DatabasePersistenceFailed(string detail = "Failed to persist vehicle and image metadata to the database.", Exception? inner = null)
        => new(ErrorCodes.DatabasePersistenceFailed, detail, isTransient: false, httpStatusCode: StatusCodes.Status500InternalServerError, innerException: inner);
}
