namespace VehicleImage.Api.Exceptions;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidUrl = "INVALID_URL";
    public const string UnsafeDestination = "UNSAFE_DESTINATION";
    public const string DnsResolutionFailed = "DNS_RESOLUTION_FAILED";
    public const string ConnectionTimeout = "CONNECTION_TIMEOUT";
    public const string RequestTimeout = "REQUEST_TIMEOUT";
    public const string TlsFailure = "TLS_FAILURE";
    public const string RedirectLimitExceeded = "REDIRECT_LIMIT_EXCEEDED";
    public const string HttpNotFound = "HTTP_NOT_FOUND";
    public const string HttpUnauthorized = "HTTP_UNAUTHORIZED";
    public const string HttpForbidden = "HTTP_FORBIDDEN";
    public const string HttpRateLimited = "HTTP_RATE_LIMITED";
    public const string HttpServerError = "HTTP_SERVER_ERROR";
    public const string HttpError = "HTTP_ERROR";
    public const string UnsupportedContentType = "UNSUPPORTED_CONTENT_TYPE";
    public const string UnsupportedImageFormat = "UNSUPPORTED_IMAGE_FORMAT";
    public const string ImageTooLarge = "IMAGE_TOO_LARGE";
    public const string ImageDimensionsExceeded = "IMAGE_DIMENSIONS_EXCEEDED";
    public const string ImageTooSmall = "IMAGE_TOO_SMALL";
    public const string ImageDecodeFailed = "IMAGE_DECODE_FAILED";
    public const string NoImageFound = "NO_IMAGE_FOUND";
    public const string StorageFailed = "STORAGE_FAILED";
    public const string DatabasePersistenceFailed = "DATABASE_PERSISTENCE_FAILED";
    public const string VehicleNotFound = "VEHICLE_NOT_FOUND";
    public const string ImageNotFound = "IMAGE_NOT_FOUND";
    public const string ConcurrentUpdate = "CONCURRENT_UPDATE";
    public const string InternalError = "INTERNAL_ERROR";
}
