using System.Runtime.CompilerServices;
using System.Text;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Models;

namespace VehicleImage.Api.Retrieval;

public sealed class RetrievalOrchestrator
{
    private readonly DirectImageRetriever _directRetriever;
    private readonly HtmlImageRetriever _htmlRetriever;
    private readonly GoogleDriveImageRetriever _googleDriveRetriever;

    public RetrievalOrchestrator(
        DirectImageRetriever directRetriever,
        HtmlImageRetriever htmlRetriever,
        GoogleDriveImageRetriever googleDriveRetriever)
    {
        _directRetriever = directRetriever;
        _htmlRetriever = htmlRetriever;
        _googleDriveRetriever = googleDriveRetriever;
    }

    public async IAsyncEnumerable<RetrievedImage> RetrieveCandidatesAsync(
        Uri source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_googleDriveRetriever.CanHandle(source))
        {
            yield return await _googleDriveRetriever.RetrieveAsync(source, cancellationToken);
            yield break;
        }

        // Single initial GET — no mandatory HEAD request (Decision 4 & Section 9.2)
        var (initialFetch, charset) = await _directRetriever.FetchWithRetriesAsync(
            source,
            "Direct",
            cancellationToken,
            allowHtml: true);

        bool transferredOwnership = false;
        try
        {
            if (_googleDriveRetriever.CanHandle(initialFetch.FinalSourceUrl))
            {
                Uri redirectedDriveUrl = initialFetch.FinalSourceUrl;
                initialFetch.Dispose();
                transferredOwnership = true;

                RetrievedImage driveImage = await _googleDriveRetriever.RetrieveAsync(
                    redirectedDriveUrl,
                    cancellationToken);
                yield return new RetrievedImage
                {
                    Content = driveImage.Content,
                    ContentType = driveImage.ContentType,
                    SourceUrl = source,
                    FinalSourceUrl = driveImage.FinalSourceUrl,
                    FileSize = driveImage.FileSize,
                    Strategy = driveImage.Strategy
                };
                yield break;
            }

            if (IsHtmlResponse(initialFetch))
            {
                string html = HtmlImageRetriever.DecodeHtml(initialFetch.Content, charset);
                Uri finalPageUrl = initialFetch.FinalSourceUrl;
                initialFetch.Dispose();
                transferredOwnership = true;

                await using var enumerator = _htmlRetriever
                    .RetrieveCandidatesFromHtmlAsync(html, source, finalPageUrl, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                while (true)
                {
                    bool hasNext;
                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                    }
                    catch (ImageIngestionException ex) when (
                        ex.Code == ErrorCodes.NoImageFound &&
                        (HasKnownImageExtension(finalPageUrl) || HasKnownImageExtension(source)))
                    {
                        // A URL with an image extension returning an HTML error/interstitial page with no usable images
                        // should report UNSUPPORTED_CONTENT_TYPE rather than NO_IMAGE_FOUND.
                        throw ImageIngestionException.UnsupportedContentType(
                            "The retrieved resource is HTML, XML, SVG or JSON rather than a binary image.");
                    }

                    if (!hasNext)
                    {
                        break;
                    }

                    yield return enumerator.Current;
                }

                yield break;
            }

            transferredOwnership = true;
            yield return initialFetch;
        }
        finally
        {
            if (!transferredOwnership)
            {
                initialFetch.Dispose();
            }
        }
    }

    internal static bool IsHtmlResponse(RetrievedImage retrieved)
    {
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        Span<byte> prefix = stackalloc byte[(int)Math.Min(1024, retrieved.Content.Length)];
        int read = retrieved.Content.Read(prefix);
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        return IsHtmlPayload(retrieved.ContentType, prefix[..read]);
    }

    internal static bool IsHtmlPayload(string? contentType, ReadOnlySpan<byte> prefix)
    {
        if (prefix.IsEmpty)
        {
            return IsHtmlMediaType(contentType);
        }

        // Binary image magic bytes always take precedence over Content-Type or URL extension
        if (ImageValidator.TryMatchBinaryImageSignature(prefix, out _, out _, out _, out _))
        {
            return false;
        }

        // If magic bytes are SVG, let ImageValidator reject it as UNSUPPORTED_CONTENT_TYPE
        string sample = Encoding.UTF8.GetString(prefix).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (IsSvgSample(sample))
        {
            return false;
        }

        return IsHtmlMediaType(contentType) ||
               sample.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<head", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<body", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<meta", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("<img", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("og:image", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("twitter:image", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHtmlMediaType(string? contentType) =>
        string.Equals(contentType, "text/html", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contentType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsSvgSample(string trimmedSample) =>
        trimmedSample.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
        (trimmedSample.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
         trimmedSample.Contains("<svg", StringComparison.OrdinalIgnoreCase));

    private static bool HasKnownImageExtension(Uri uri)
    {
        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant() is
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" or
            ".tiff" or ".tif" or ".ico" or ".svg" or ".avif" or ".heic";
    }
}
