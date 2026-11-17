using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Security;

public sealed class UrlValidator
{
    private readonly RetrievalOptions _options;
    private static readonly IdnMapping Idn = new();

    public UrlValidator(IOptions<RetrievalOptions> options)
    {
        _options = options.Value;
    }

    public Uri Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw ImageIngestionException.InvalidUrl("The image URL is required.");
        }

        string trimmed = ValidateUrlText(raw, "URL");

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw ImageIngestionException.InvalidUrl("The URL must be a valid absolute URI.");
        }

        return ValidateUriPolicy(uri);
    }

    public Uri ValidateRedirectTarget(Uri currentUri, string? locationHeader)
    {
        ArgumentNullException.ThrowIfNull(currentUri);

        if (string.IsNullOrWhiteSpace(locationHeader))
        {
            throw ImageIngestionException.InvalidUrl("Redirect response did not include a valid Location header.");
        }

        string trimmed = ValidateUrlText(locationHeader, "Redirect target URL");

        if (!Uri.TryCreate(currentUri, trimmed, out var targetUri))
        {
            throw ImageIngestionException.InvalidUrl("Redirect target URL could not be resolved.");
        }

        return ValidateUriPolicy(targetUri);
    }

    public static string RedactForLogging(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.GetLeftPart(UriPartial.Path);
    }

    private string ValidateUrlText(string raw, string description)
    {
        string trimmed = raw.Trim();
        if (trimmed.Length > _options.MaxUrlLength)
        {
            throw ImageIngestionException.InvalidUrl(
                $"{description} exceeds the maximum length of {_options.MaxUrlLength} characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            throw ImageIngestionException.InvalidUrl(
                $"{description} contains invalid control characters.");
        }

        return trimmed;
    }

    private Uri ValidateUriPolicy(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw ImageIngestionException.InvalidUrl("Only HTTP and HTTPS URLs are permitted.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw ImageIngestionException.InvalidUrl("URLs containing embedded credentials (userinfo) are not permitted.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw ImageIngestionException.InvalidUrl("The URL must specify a valid host.");
        }

        if (!_options.AllowedPorts.Contains(uri.Port))
        {
            throw ImageIngestionException.UnsafeDestination($"Port {uri.Port} is not permitted.");
        }

        string normalizedHost = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalizedHost == "localhost" || normalizedHost.EndsWith(".localhost", StringComparison.Ordinal))
        {
            throw ImageIngestionException.UnsafeDestination("Localhost destinations are not permitted.");
        }

        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            if (!IPAddress.TryParse(uri.DnsSafeHost, out var ipAddress))
            {
                throw ImageIngestionException.InvalidUrl("The IP literal in the URL is malformed.");
            }

            if (IPAddress.IsLoopback(ipAddress))
            {
                throw ImageIngestionException.UnsafeDestination("Loopback destinations are not permitted.");
            }

            return uri;
        }

        // Normalize IDN hostnames and ensure basic hostname validity
        try
        {
            string asciiHost = Idn.GetAscii(uri.DnsSafeHost);
            if (string.IsNullOrWhiteSpace(asciiHost) || !asciiHost.Contains('.'))
            {
                throw ImageIngestionException.InvalidUrl("The URL hostname must be a valid domain name.");
            }
        }
        catch (ArgumentException)
        {
            throw ImageIngestionException.InvalidUrl("The URL hostname could not be normalized.");
        }

        return uri;
    }
}
