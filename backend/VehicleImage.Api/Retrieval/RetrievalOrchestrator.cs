using System.Runtime.CompilerServices;
using System.Text;
using VehicleImage.Api.Models;

namespace VehicleImage.Api.Retrieval;

public sealed class RetrievalOrchestrator
{
    private readonly DirectImageRetriever _directRetriever;
    private readonly HtmlImageRetriever _htmlRetriever;

    public RetrievalOrchestrator(
        DirectImageRetriever directRetriever,
        HtmlImageRetriever htmlRetriever)
    {
        _directRetriever = directRetriever;
        _htmlRetriever = htmlRetriever;
    }

    public async IAsyncEnumerable<RetrievedImage> RetrieveCandidatesAsync(
        Uri source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Single initial GET — no mandatory HEAD request (Decision 4 & Section 9.2)
        var (initialFetch, charset) = await _directRetriever.FetchWithRetriesAsync(
            source,
            "Direct",
            cancellationToken);

        bool transferredOwnership = false;
        try
        {
            if (IsHtmlResponse(initialFetch))
            {
                string html = HtmlImageRetriever.DecodeHtml(initialFetch.Content, charset);
                Uri finalPageUrl = initialFetch.FinalSourceUrl;
                initialFetch.Dispose();
                transferredOwnership = true;

                await foreach (var htmlCandidate in _htmlRetriever.RetrieveCandidatesFromHtmlAsync(
                    html,
                    source,
                    finalPageUrl,
                    cancellationToken))
                {
                    yield return htmlCandidate;
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

    private static bool IsHtmlResponse(RetrievedImage retrieved)
    {
        if (HasKnownImageExtension(retrieved.FinalSourceUrl))
        {
            return false;
        }

        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        Span<byte> prefix = stackalloc byte[(int)Math.Min(512, retrieved.Content.Length)];
        int read = retrieved.Content.Read(prefix);
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        prefix = prefix[..read];

        // If magic bytes match binary image formats, it is NOT HTML regardless of Content-Type header
        if (StartsWithBinaryImageSignature(prefix))
        {
            return false;
        }

        // If magic bytes are SVG, let ImageValidator reject it as UNSUPPORTED_CONTENT_TYPE
        string sample = Encoding.UTF8.GetString(prefix).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (sample.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
            (sample.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
             sample.Contains("<svg", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return string.Equals(retrieved.ContentType, "text/html", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(retrieved.ContentType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase) ||
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

    private static bool HasKnownImageExtension(Uri uri)
    {
        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant() is
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" or
            ".tiff" or ".tif" or ".ico" or ".svg" or ".avif" or ".heic";
    }

    private static bool StartsWithBinaryImageSignature(ReadOnlySpan<byte> header)
    {
        // JPEG
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return true;

        // PNG
        if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return true;

        // WebP
        if (header.Length >= 12 &&
            header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
            return true;

        // GIF
        if (header.Length >= 6 && header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F')
            return true;

        // BMP
        if (header.Length >= 2 && header[0] == (byte)'B' && header[1] == (byte)'M')
            return true;

        return false;
    }

}
