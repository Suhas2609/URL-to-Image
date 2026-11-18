using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

public enum CandidateSourceKind
{
    OgImage,
    TwitterImage,
    Srcset,
    DataSrc,
    Img
}

public sealed record ImageCandidate(
    Uri Url,
    CandidateSourceKind Source,
    int? DeclaredWidth,
    int? DeclaredHeight,
    int Score
);

public sealed partial class HtmlImageRetriever
{
    private static readonly string[] HtmlErrorPhrases =
    [
        "404 not found",
        "page not found",
        "error 404",
        "403 forbidden",
        "access denied",
        "500 internal server error",
        "service unavailable",
        "bad gateway"
    ];

    private readonly DirectImageRetriever _directRetriever;
    private readonly UrlValidator _urlValidator;
    private readonly HtmlExtractionOptions _options;
    private readonly ILogger<HtmlImageRetriever> _logger;

    static HtmlImageRetriever()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public HtmlImageRetriever(
        DirectImageRetriever directRetriever,
        UrlValidator urlValidator,
        IOptions<HtmlExtractionOptions> options,
        ILogger<HtmlImageRetriever> logger)
    {
        _directRetriever = directRetriever;
        _urlValidator = urlValidator;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<RetrievedImage> RetrieveCandidatesFromHtmlAsync(
        string html,
        Uri originalSourceUrl,
        Uri finalPageUrl,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ImageCandidate> rankedCandidates = ExtractAndRankCandidates(html, finalPageUrl);
        if (rankedCandidates.Count == 0)
        {
            throw ImageIngestionException.NoImageFound();
        }

        int maxToTry = Math.Min(3, Math.Max(1, _options.MaxCandidates));
        int attempted = 0;
        ImageIngestionException? lastCandidateFailure = null;

        foreach (var candidate in rankedCandidates)
        {
            if (attempted >= maxToTry)
            {
                break;
            }

            attempted++;
            RetrievedImage? retrieved = null;
            try
            {
                var (fetched, _) = await _directRetriever.FetchWithRetriesAsync(
                    candidate.Url,
                    "Html",
                    cancellationToken,
                    allowHtml: false);

                retrieved = new RetrievedImage
                {
                    Content = fetched.Content,
                    ContentType = fetched.ContentType,
                    SourceUrl = originalSourceUrl,
                    FinalSourceUrl = fetched.FinalSourceUrl,
                    FileSize = fetched.FileSize,
                    Strategy = "Html"
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ImageIngestionException ex)
            {
                lastCandidateFailure = ex;
                _logger.LogInformation(
                    "HTML candidate {Index} ({CandidateUrl}) failed retrieval with [{Code}]; trying next candidate if available.",
                    attempted, UrlValidator.RedactForLogging(candidate.Url), ex.Code);
            }

            if (retrieved is not null)
            {
                yield return retrieved;
            }
        }

        if (lastCandidateFailure is not null)
        {
            throw lastCandidateFailure;
        }

        throw ImageIngestionException.NoImageFound();
    }

    public IReadOnlyList<ImageCandidate> ExtractAndRankCandidates(string html, Uri pageUrl)
    {
        var rawCandidates = ExtractCandidates(html, pageUrl);
        return Rank(rawCandidates);
    }

    public List<ImageCandidate> ExtractCandidates(string html, Uri pageUrl)
    {
        ArgumentNullException.ThrowIfNull(pageUrl);

        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var doc = new HtmlDocument();
        string normalizedHtml = EnsureClosedTitleTag(html);
        doc.LoadHtml(normalizedHtml);

        if (IsHtmlErrorPage(doc))
        {
            return [];
        }

        Uri baseUri = ResolveBaseUri(doc, pageUrl);
        var candidates = new List<ImageCandidate>();

        // 1. Meta tags: og:image and twitter:image
        var metaNodes = doc.DocumentNode.SelectNodes("//meta");
        if (metaNodes is not null)
        {
            foreach (var meta in metaNodes)
            {
                string prop = (meta.GetAttributeValue("property", string.Empty) + " " +
                               meta.GetAttributeValue("name", string.Empty)).Trim().ToLowerInvariant();
                string? content = WebUtility.HtmlDecode(meta.GetAttributeValue("content", null!));

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                if (prop.Contains("og:image", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:width", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:height", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:type", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:alt", StringComparison.Ordinal))
                {
                    TryAddCandidate(candidates, baseUri, content, CandidateSourceKind.OgImage, null, null, meta);
                }
                else if (prop.Contains("twitter:image", StringComparison.Ordinal) &&
                         !prop.Contains("twitter:image:alt", StringComparison.Ordinal))
                {
                    TryAddCandidate(candidates, baseUri, content, CandidateSourceKind.TwitterImage, null, null, meta);
                }
            }
        }

        // 2. <img> elements: srcset, data-src / data-original, and src
        var imgNodes = doc.DocumentNode.SelectNodes("//img");
        if (imgNodes is not null)
        {
            foreach (var img in imgNodes)
            {
                int? declaredW = ParseDimension(img.GetAttributeValue("width", null!));
                int? declaredH = ParseDimension(img.GetAttributeValue("height", null!));

                if ((declaredW.HasValue && declaredW.Value > 0 && declaredW.Value < _options.MinDeclaredDimension) ||
                    (declaredH.HasValue && declaredH.Value > 0 && declaredH.Value < _options.MinDeclaredDimension))
                {
                    continue;
                }

                // Check srcset or data-srcset for the largest descriptor
                string? srcset = WebUtility.HtmlDecode(
                    img.GetAttributeValue("srcset", null!) ?? img.GetAttributeValue("data-srcset", null!));
                if (!string.IsNullOrWhiteSpace(srcset))
                {
                    var (bestSrcsetUrl, srcsetWidth) = PickLargestSrcsetEntry(srcset);
                    if (!string.IsNullOrWhiteSpace(bestSrcsetUrl))
                    {
                        TryAddCandidate(
                            candidates,
                            baseUri,
                            bestSrcsetUrl,
                            CandidateSourceKind.Srcset,
                            srcsetWidth ?? declaredW,
                            declaredH,
                            img);
                    }
                }

                // Check lazy-loading attributes: data-src, data-original, data-lazy-src
                string? dataSrc = WebUtility.HtmlDecode(
                    img.GetAttributeValue("data-src", null!) ??
                    img.GetAttributeValue("data-original", null!) ??
                    img.GetAttributeValue("data-lazy-src", null!));
                if (!string.IsNullOrWhiteSpace(dataSrc))
                {
                    TryAddCandidate(candidates, baseUri, dataSrc, CandidateSourceKind.DataSrc, declaredW, declaredH, img);
                }

                // Standard src attribute
                string? src = WebUtility.HtmlDecode(img.GetAttributeValue("src", null!));
                if (!string.IsNullOrWhiteSpace(src))
                {
                    TryAddCandidate(candidates, baseUri, src, CandidateSourceKind.Img, declaredW, declaredH, img);
                }
            }
        }

        return candidates;
    }

    public IReadOnlyList<ImageCandidate> Rank(IEnumerable<ImageCandidate> candidates)
    {
        return candidates
            .GroupBy(c => c.Url.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(c => c.Score).First())
            .OrderByDescending(c => c.Score)
            .ToList();
    }

    public static string DecodeHtml(Stream content, string? headerCharset)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        byte[] bytes;
        if (content is MemoryStream ms)
        {
            bytes = ms.ToArray();
        }
        else
        {
            using var copy = new MemoryStream();
            content.CopyTo(copy);
            bytes = copy.ToArray();
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        string? detectedCharset = CleanCharset(headerCharset);
        if (string.IsNullOrWhiteSpace(detectedCharset))
        {
            // Inspect first 2048 bytes as ASCII to check <meta charset="..."> or content="text/html; charset=..."
            int sampleLen = Math.Min(bytes.Length, 2048);
            string asciiPrefix = Encoding.ASCII.GetString(bytes, 0, sampleLen);
            Match match = MetaCharsetRegex().Match(asciiPrefix);
            if (match.Success)
            {
                detectedCharset = CleanCharset(match.Groups[1].Value);
            }
        }

        Encoding encoding = Encoding.UTF8;
        if (!string.IsNullOrWhiteSpace(detectedCharset))
        {
            try
            {
                encoding = Encoding.GetEncoding(detectedCharset);
            }
            catch (ArgumentException)
            {
                encoding = Encoding.UTF8;
            }
        }

        return encoding.GetString(bytes);
    }

    private void TryAddCandidate(
        List<ImageCandidate> candidates,
        Uri baseUri,
        string rawUrl,
        CandidateSourceKind source,
        int? declaredWidth,
        int? declaredHeight,
        HtmlNode node)
    {
        string trimmed = rawUrl.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith('#'))
        {
            return;
        }

        Uri resolvedUri;
        try
        {
            resolvedUri = _urlValidator.ValidateRedirectTarget(baseUri, trimmed);
        }
        catch (ImageIngestionException)
        {
            return;
        }

        string pathAndQuery = resolvedUri.PathAndQuery.ToLowerInvariant();
        if (pathAndQuery.EndsWith(".svg", StringComparison.Ordinal) ||
            pathAndQuery.Contains(".svg?", StringComparison.Ordinal))
        {
            return;
        }

        if (MatchesIgnoredPattern(resolvedUri, node))
        {
            return;
        }

        candidates.Add(new ImageCandidate(
            Url: resolvedUri,
            Source: source,
            DeclaredWidth: declaredWidth,
            DeclaredHeight: declaredHeight,
            Score: GetCandidatePriority(source, declaredWidth, declaredHeight)));
    }

    private bool MatchesIgnoredPattern(Uri uri, HtmlNode node)
    {
        string pathLower = uri.AbsolutePath.ToLowerInvariant();
        string nodeContext = string.Concat(
            node.GetAttributeValue("class", string.Empty), " ",
            node.GetAttributeValue("id", string.Empty), " ",
            node.GetAttributeValue("alt", string.Empty)).ToLowerInvariant();

        foreach (string pattern in _options.IgnoreUrlPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            string pat = pattern.ToLowerInvariant();
            if (pathLower.Contains(pat, StringComparison.Ordinal) ||
                nodeContext.Contains(pat, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetCandidatePriority(
        CandidateSourceKind source,
        int? declaredWidth,
        int? declaredHeight)
    {
        int priority = source switch
        {
            CandidateSourceKind.OgImage => 400,
            CandidateSourceKind.TwitterImage => 300,
            CandidateSourceKind.Srcset => 200,
            CandidateSourceKind.DataSrc or CandidateSourceKind.Img => 100,
            _ => 0
        };

        if (declaredWidth >= 400 || declaredHeight >= 300)
        {
            priority += 50;
        }

        return priority;
    }

    private static bool IsHtmlErrorPage(HtmlDocument doc)
    {
        string title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim().ToLowerInvariant() ?? string.Empty;
        string h1 = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim().ToLowerInvariant() ?? string.Empty;
        string combined = $"{title} {h1}";

        foreach (string phrase in HtmlErrorPhrases)
        {
            if (combined.Contains(phrase, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private Uri ResolveBaseUri(HtmlDocument doc, Uri pageUrl)
    {
        var baseNode = doc.DocumentNode.SelectSingleNode("//base[@href]");
        string? baseHref = baseNode?.GetAttributeValue("href", null!);
        if (!string.IsNullOrWhiteSpace(baseHref))
        {
            try
            {
                return _urlValidator.ValidateRedirectTarget(pageUrl, baseHref);
            }
            catch (ImageIngestionException)
            {
                return pageUrl;
            }
        }

        return pageUrl;
    }

    private static (string? Url, int? Width) PickLargestSrcsetEntry(string srcset)
    {
        string? bestUrl = null;
        double bestMetric = -1;
        int? bestWidth = null;

        string[] parts = srcset.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            string[] tokens = part.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                continue;
            }

            string url = tokens[0];
            double metric = 1.0;
            int? width = null;

            if (tokens.Length >= 2)
            {
                string descriptor = tokens[^1].ToLowerInvariant();
                if (descriptor.EndsWith('w') && int.TryParse(descriptor[..^1], out int w) && w > 0)
                {
                    metric = w;
                    width = w;
                }
                else if (descriptor.EndsWith('x') && double.TryParse(descriptor[..^1], System.Globalization.CultureInfo.InvariantCulture, out double x) && x > 0)
                {
                    metric = x * 1000.0;
                }
            }

            if (metric > bestMetric)
            {
                bestMetric = metric;
                bestUrl = url;
                bestWidth = width;
            }
        }

        return (bestUrl, bestWidth);
    }

    private static int? ParseDimension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string digits = value.Trim();
        if (digits.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            digits = digits[..^2].Trim();
        }

        return int.TryParse(digits, out int result) && result > 0 ? result : null;
    }

    private static string? CleanCharset(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().Trim('"', '\'');
    }

    private static string EnsureClosedTitleTag(string html)
    {
        int titleStart = html.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
        if (titleStart >= 0 &&
            html.IndexOf("</title>", titleStart, StringComparison.OrdinalIgnoreCase) < 0)
        {
            int nextTag = html.IndexOf('<', titleStart + 6);
            if (nextTag > titleStart)
            {
                return html.Insert(nextTag, "</title>");
            }
        }
        return html;
    }

    [GeneratedRegex(@"charset\s*=\s*[""']?([a-zA-Z0-9_\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MetaCharsetRegex();
}
