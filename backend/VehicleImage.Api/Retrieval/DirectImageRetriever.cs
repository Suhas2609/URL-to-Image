using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

public sealed class DirectImageRetriever : IImageRetriever
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UrlValidator _urlValidator;
    private readonly DestinationValidator _destinationValidator;
    private readonly RetrievalOptions _retrievalOptions;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<DirectImageRetriever> _logger;

    public DirectImageRetriever(
        IHttpClientFactory httpClientFactory,
        UrlValidator urlValidator,
        DestinationValidator destinationValidator,
        IOptions<RetrievalOptions> retrievalOptions,
        IOptions<RetryOptions> retryOptions,
        ILogger<DirectImageRetriever> logger)
    {
        _httpClientFactory = httpClientFactory;
        _urlValidator = urlValidator;
        _destinationValidator = destinationValidator;
        _retrievalOptions = retrievalOptions.Value;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    public string Name => "Direct";

    public async Task<RetrievedImage> RetrieveAsync(Uri source, CancellationToken cancellationToken = default)
    {
        var (image, _) = await FetchWithRetriesAsync(source, Name, cancellationToken);
        return image;
    }

    internal async Task<(RetrievedImage Image, string? Charset)> FetchWithRetriesAsync(
        Uri source,
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        int maxAttempts = Math.Max(1, _retryOptions.MaxAttempts);
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                return await ExecuteSingleFetchAsync(source, strategyName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ImageIngestionException ex) when (ShouldRetry(ex, attempt, maxAttempts, out TimeSpan delay))
            {
                _logger.LogWarning(
                    "Transient failure [{Code}] on attempt {Attempt}/{MaxAttempts} for {Host}. Retrying in {DelayMs}ms.",
                    ex.Code, attempt, maxAttempts, source.Host, (int)delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private bool ShouldRetry(ImageIngestionException ex, int attempt, int maxAttempts, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (!ex.IsTransient || attempt >= maxAttempts)
        {
            return false;
        }

        // Permanent errors are never retried; only transient codes are eligible
        if (ex.Code is ErrorCodes.InvalidUrl
            or ErrorCodes.UnsafeDestination
            or ErrorCodes.RedirectLimitExceeded
            or ErrorCodes.HttpNotFound
            or ErrorCodes.HttpUnauthorized
            or ErrorCodes.HttpForbidden
            or ErrorCodes.UnsupportedContentType
            or ErrorCodes.UnsupportedImageFormat
            or ErrorCodes.ImageTooLarge
            or ErrorCodes.ImageDimensionsExceeded
            or ErrorCodes.ImageTooSmall
            or ErrorCodes.TlsFailure)
        {
            return false;
        }

        if (ex.Code == ErrorCodes.HttpRateLimited)
        {
            if (ex.RetryAfter.HasValue)
            {
                if (ex.RetryAfter.Value.TotalSeconds > _retryOptions.MaxRetryAfterSeconds)
                {
                    return false;
                }

                delay = ex.RetryAfter.Value;
                return true;
            }
        }

        double exponentialMs = _retryOptions.BaseDelayMs * Math.Pow(2, attempt - 1);
        double cappedMs = Math.Min(exponentialMs, _retryOptions.MaxDelayMs);

        if (_retryOptions.EnableJitter)
        {
            double jitterFactor = 0.75 + (Random.Shared.NextDouble() * 0.5);
            cappedMs *= jitterFactor;
        }

        delay = TimeSpan.FromMilliseconds(Math.Max(10, cappedMs));
        return true;
    }

    private async Task<(RetrievedImage Image, string? Charset)> ExecuteSingleFetchAsync(
        Uri source,
        string strategyName,
        CancellationToken cancellationToken)
    {
        Uri currentUri = _urlValidator.Validate(source.OriginalString);
        HttpClient client = _httpClientFactory.CreateClient(SsrfGuardHttpHandler.ClientName);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_retrievalOptions.TimeoutSeconds));
        CancellationToken effectiveToken = timeoutCts.Token;

        for (int hop = 0; hop <= _retrievalOptions.MaxRedirects; hop++)
        {
            await _destinationValidator.ResolveAndValidateAsync(currentUri.DnsSafeHost, effectiveToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _retrievalOptions.UserAgent);
            request.Headers.TryAddWithoutValidation(
                "Accept",
                "image/webp,image/png,image/jpeg,image/*;q=0.9,text/html,application/xhtml+xml;q=0.8,*/*;q=0.5");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    effectiveToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw ImageIngestionException.RequestTimeout(inner: ex);
            }
            catch (ImageIngestionException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                throw MapHttpRequestException(ex);
            }
            catch (AuthenticationException ex)
            {
                throw ImageIngestionException.TlsFailure(inner: ex);
            }

            using (response)
            {
                if (IsRedirectStatusCode(response.StatusCode))
                {
                    if (hop >= _retrievalOptions.MaxRedirects)
                    {
                        throw ImageIngestionException.RedirectLimitExceeded(_retrievalOptions.MaxRedirects);
                    }

                    string? location = response.Headers.Location?.OriginalString;
                    currentUri = _urlValidator.ValidateRedirectTarget(currentUri, location);
                    continue;
                }

                EnsureSuccessOrThrowMappedStatus(response);

                string? mediaType = response.Content.Headers.ContentType?.MediaType;
                string? charSet = response.Content.Headers.ContentType?.CharSet;

                MemoryStream buffer = await ReadBoundedStreamAsync(
                    response.Content,
                    _retrievalOptions.MaxDownloadBytes,
                    effectiveToken,
                    cancellationToken);

                var retrieved = new RetrievedImage
                {
                    Content = buffer,
                    ContentType = mediaType,
                    SourceUrl = source,
                    FinalSourceUrl = currentUri,
                    FileSize = buffer.Length,
                    Strategy = strategyName
                };

                return (retrieved, charSet);
            }
        }

        throw ImageIngestionException.RedirectLimitExceeded(_retrievalOptions.MaxRedirects);
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.MovedPermanently // 301
            or HttpStatusCode.Found                          // 302
            or HttpStatusCode.SeeOther                       // 303
            or HttpStatusCode.TemporaryRedirect              // 307
            or HttpStatusCode.PermanentRedirect;             // 308
    }

    private static void EnsureSuccessOrThrowMappedStatus(HttpResponseMessage response)
    {
        int code = (int)response.StatusCode;
        if (code >= 200 && code <= 299)
        {
            return;
        }

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw ImageIngestionException.HttpNotFound();

            case HttpStatusCode.Unauthorized:
                throw ImageIngestionException.HttpUnauthorized();

            case HttpStatusCode.Forbidden:
                throw ImageIngestionException.HttpForbidden();

            case HttpStatusCode.TooManyRequests:
                TimeSpan? retryAfter = null;
                if (response.Headers.RetryAfter?.Delta is { } delta)
                {
                    retryAfter = delta > TimeSpan.Zero ? delta : TimeSpan.FromSeconds(1);
                }
                else if (response.Headers.RetryAfter?.Date is { } date)
                {
                    var diff = date - DateTimeOffset.UtcNow;
                    if (diff > TimeSpan.Zero)
                    {
                        retryAfter = diff;
                    }
                }
                throw ImageIngestionException.HttpRateLimited(retryAfter);

            default:
                if (code >= 500 && code <= 599)
                {
                    throw ImageIngestionException.HttpServerError(code);
                }
                throw ImageIngestionException.HttpError(code);
        }
    }

    private static async Task<MemoryStream> ReadBoundedStreamAsync(
        HttpContent content,
        long maxBytes,
        CancellationToken effectiveToken,
        CancellationToken callerToken)
    {
        var memoryStream = new MemoryStream();
        try
        {
            await using Stream networkStream = await content.ReadAsStreamAsync(effectiveToken);
            byte[] chunk = new byte[16 * 1024];
            long totalRead = 0;

            while (true)
            {
                int bytesRead = await networkStream.ReadAsync(chunk.AsMemory(0, chunk.Length), effectiveToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
                if (totalRead > maxBytes)
                {
                    throw ImageIngestionException.ImageTooLarge(maxBytes);
                }

                memoryStream.Write(chunk, 0, bytesRead);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            memoryStream.Dispose();
            throw;
        }
        catch (OperationCanceledException ex)
        {
            memoryStream.Dispose();
            throw ImageIngestionException.RequestTimeout(inner: ex);
        }
        catch (ImageIngestionException)
        {
            memoryStream.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            memoryStream.Dispose();
            throw ImageIngestionException.ImageDecodeFailed(
                "The image transfer was interrupted before completion.",
                isTransient: true,
                inner: ex);
        }
    }

    private static ImageIngestionException MapHttpRequestException(HttpRequestException ex)
    {
        if (ex.InnerException is ImageIngestionException ingestionEx)
        {
            return ingestionEx;
        }

        if (ex.InnerException is AuthenticationException ||
            ex.HttpRequestError == HttpRequestError.SecureConnectionError)
        {
            return ImageIngestionException.TlsFailure(inner: ex);
        }

        if (ex.HttpRequestError == HttpRequestError.NameResolutionError ||
            ex.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain })
        {
            return ImageIngestionException.DnsResolutionFailed(inner: ex);
        }

        if (ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut } ||
            ex.InnerException is TimeoutException)
        {
            return ImageIngestionException.ConnectionTimeout(inner: ex);
        }

        return ImageIngestionException.ConnectionTimeout(
            "Unable to connect to the supplier server.",
            inner: ex);
    }
}
