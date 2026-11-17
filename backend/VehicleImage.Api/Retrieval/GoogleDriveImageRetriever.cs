using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

/// <summary>
/// Retrieves publicly shared ("Anyone with the link") Google Drive image files by translating
/// sharing URLs into direct-download requests executed through <see cref="DirectImageRetriever"/>
/// (preserving URL validation, timeout, bounded-streaming, and retry guarantees).
/// OAuth and private Google Drive files are intentionally out of scope.
/// </summary>
public sealed partial class GoogleDriveImageRetriever
{
    private const string StrategyName = "GoogleDrive";
    private const string DriveHost = "drive.google.com";
    private const string DriveUserContentHost = "drive.usercontent.google.com";
    private const string DocsHost = "docs.google.com";

    private readonly DirectImageRetriever _directRetriever;
    private readonly UrlValidator _urlValidator;
    private readonly ILogger<GoogleDriveImageRetriever> _logger;

    public GoogleDriveImageRetriever(
        DirectImageRetriever directRetriever,
        UrlValidator urlValidator,
        ILogger<GoogleDriveImageRetriever> logger)
    {
        _directRetriever = directRetriever;
        _urlValidator = urlValidator;
        _logger = logger;
    }

    public bool CanHandle(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            return false;
        }

        string host = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        return host is DriveHost or DriveUserContentHost or DocsHost;
    }

    public async Task<RetrievedImage> RetrieveAsync(
        Uri source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        (string fileId, string? resourceKey) = ParseAndValidateDriveTarget(source);
        Uri downloadUri = BuildDownloadUri(fileId, resourceKey);

        _logger.LogInformation(
            "Delegating Google Drive file {FileId} (hasResourceKey={HasResourceKey}) to public download endpoint.",
            fileId,
            !string.IsNullOrEmpty(resourceKey));

        var (initialResponse, initialCharset) = await FetchDriveResponseAsync(downloadUri, cancellationToken);

        if (!RetrievalOrchestrator.IsHtmlResponse(initialResponse))
        {
            return RewrapSourceUrl(initialResponse, source);
        }

        string initialHtml;
        Uri initialFinalUrl = initialResponse.FinalSourceUrl;
        await using (initialResponse)
        {
            initialHtml = HtmlImageRetriever.DecodeHtml(initialResponse.Content, initialCharset);
        }

        if (TryExtractConfirmationUri(initialHtml, initialFinalUrl, fileId, resourceKey, out Uri? confirmUri) &&
            confirmUri is not null)
        {
            _logger.LogInformation(
                "Google Drive returned a download confirmation page for file {FileId}; following confirmation link.",
                fileId);

            var (confirmedResponse, confirmedCharset) = await FetchDriveResponseAsync(confirmUri, cancellationToken);

            if (!RetrievalOrchestrator.IsHtmlResponse(confirmedResponse))
            {
                return RewrapSourceUrl(confirmedResponse, source);
            }

            string confirmedHtml;
            Uri confirmedFinalUrl = confirmedResponse.FinalSourceUrl;
            await using (confirmedResponse)
            {
                confirmedHtml = HtmlImageRetriever.DecodeHtml(confirmedResponse.Content, confirmedCharset);
            }

            throw ClassifyHtmlFailure(
                confirmedHtml,
                confirmedFinalUrl,
                fallbackMessage: "Google Drive confirmation page could not be resolved into downloadable image bytes.");
        }

        throw ClassifyHtmlFailure(
            initialHtml,
            initialFinalUrl,
            fallbackMessage: "Google Drive returned an HTML page instead of downloadable image bytes. Ensure the file is a supported image shared with 'Anyone with the link'.");
    }

    private async Task<(RetrievedImage Image, string? Charset)> FetchDriveResponseAsync(
        Uri targetUri,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _directRetriever.FetchWithRetriesAsync(targetUri, StrategyName, cancellationToken);
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpNotFound)
        {
            throw ImageIngestionException.HttpNotFound();
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpUnauthorized)
        {
            throw ImageIngestionException.ProviderAuthRequired(
                "This Google Drive file requires authentication and is not publicly accessible. Set sharing to 'Anyone with the link'.");
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpForbidden)
        {
            throw new ImageIngestionException(
                ErrorCodes.HttpForbidden,
                "Access to this Google Drive file was denied or restricted by the owner (HTTP 403).",
                isTransient: false,
                upstreamStatusCode: 403,
                innerException: ex);
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpError && ex.UpstreamStatusCode == 400)
        {
            throw ImageIngestionException.UnsupportedSource(
                "Google Drive rejected the download request (HTTP 400). Ensure the URL points to a binary image file rather than a Google Docs, Sheets, or Slides file.");
        }
    }

    private static (string FileId, string? ResourceKey) ParseAndValidateDriveTarget(Uri uri)
    {
        string host = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(uri.Query);

        string? resourceKey = GetQueryValue(query, "resourcekey");
        if (!string.IsNullOrEmpty(resourceKey) && !ResourceKeyRegex().IsMatch(resourceKey))
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive resource key is malformed.");
        }

        if (host == DocsHost)
        {
            throw ImageIngestionException.UnsupportedSource(
                "Google Docs, Sheets, Slides, and Forms URLs are not supported. Share a downloadable image file stored in Google Drive.");
        }

        if (host == DriveUserContentHost)
        {
            if (segments.Length == 1 &&
                string.Equals(segments[0], "download", StringComparison.OrdinalIgnoreCase))
            {
                string? id = GetQueryValue(query, "id");
                return (ValidateFileId(id), resourceKey);
            }

            throw ImageIngestionException.InvalidUrl("Unsupported Google Drive download URL format.");
        }

        if (host != DriveHost)
        {
            throw ImageIngestionException.InvalidUrl("The URL does not belong to Google Drive.");
        }

        if (segments.Length == 0)
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive URL is missing a file path and file ID.");
        }

        // Reject folders and workspace views explicitly
        if (ContainsSegment(segments, "folders") ||
            (string.Equals(segments[0], "drive", StringComparison.OrdinalIgnoreCase) &&
             !ContainsSegment(segments, "file")))
        {
            throw ImageIngestionException.UnsupportedSource(
                "Google Drive folders and directory views are not supported. Provide a sharing URL to an individual image file.");
        }

        // 1. /file/d/{FILE_ID}[/view|edit|preview|...] or /file/u/{n}/d/{FILE_ID}[/...]
        if (string.Equals(segments[0], "file", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length >= 3 &&
                string.Equals(segments[1], "d", StringComparison.OrdinalIgnoreCase))
            {
                return (ValidateFileId(segments[2]), resourceKey);
            }

            if (segments.Length >= 5 &&
                string.Equals(segments[1], "u", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(segments[2], out _) &&
                string.Equals(segments[3], "d", StringComparison.OrdinalIgnoreCase))
            {
                return (ValidateFileId(segments[4]), resourceKey);
            }

            throw ImageIngestionException.InvalidUrl("The Google Drive file URL is missing a valid file ID.");
        }

        // 2. /open?id={FILE_ID} or /uc?id={FILE_ID}[&export=download]
        if (segments.Length == 1 &&
            (string.Equals(segments[0], "open", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(segments[0], "uc", StringComparison.OrdinalIgnoreCase)))
        {
            string? id = GetQueryValue(query, "id");
            return (ValidateFileId(id), resourceKey);
        }

        throw ImageIngestionException.InvalidUrl(
            "Unsupported Google Drive URL format. Expected a sharing URL such as https://drive.google.com/file/d/{FILE_ID}/view.");
    }

    private static string ValidateFileId(string? rawId)
    {
        string trimmed = rawId?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive URL is missing a file ID.");
        }

        if (!FileIdRegex().IsMatch(trimmed))
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive file ID is invalid or malformed.");
        }

        return trimmed;
    }

    private static Uri BuildDownloadUri(string fileId, string? resourceKey)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["export"] = "download",
            ["id"] = fileId
        };

        if (!string.IsNullOrEmpty(resourceKey))
        {
            queryParams["resourcekey"] = resourceKey;
        }

        string url = QueryHelpers.AddQueryString($"https://{DriveHost}/uc", queryParams);
        return new Uri(url, UriKind.Absolute);
    }

    private bool TryExtractConfirmationUri(
        string html,
        Uri currentUri,
        string fileId,
        string? resourceKey,
        out Uri? confirmUri)
    {
        confirmUri = null;
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Modern Google Drive confirmation page uses <form id="download-form" action="https://drive.usercontent.google.com/download" method="get">
        HtmlNode? form = doc.DocumentNode.SelectSingleNode("//form[@id='download-form']")
            ?? doc.DocumentNode.SelectNodes("//form")
                ?.FirstOrDefault(f =>
                {
                    string act = f.GetAttributeValue("action", string.Empty);
                    return act.Contains("usercontent.google.com/download", StringComparison.OrdinalIgnoreCase) ||
                           act.Contains("drive.google.com/uc", StringComparison.OrdinalIgnoreCase);
                });

        if (form is not null)
        {
            string action = WebUtility.HtmlDecode(form.GetAttributeValue("action", string.Empty)).Trim();
            Uri baseActionUri;
            try
            {
                baseActionUri = string.IsNullOrWhiteSpace(action)
                    ? currentUri
                    : _urlValidator.ValidateRedirectTarget(currentUri, action);
            }
            catch (ImageIngestionException)
            {
                return false;
            }

            if (!IsAllowedConfirmationHost(baseActionUri))
            {
                return false;
            }

            var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in QueryHelpers.ParseQuery(baseActionUri.Query))
            {
                string? val = existing.Value.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    parameters[existing.Key] = val;
                }
            }

            HtmlNodeCollection? inputs = form.SelectNodes(".//input[@name]");
            if (inputs is not null)
            {
                foreach (HtmlNode input in inputs)
                {
                    string type = input.GetAttributeValue("type", string.Empty).Trim().ToLowerInvariant();
                    if (type is "submit" or "button" or "reset" or "image")
                    {
                        continue;
                    }

                    string name = WebUtility.HtmlDecode(input.GetAttributeValue("name", string.Empty)).Trim();
                    string value = WebUtility.HtmlDecode(input.GetAttributeValue("value", string.Empty));
                    if (!string.IsNullOrEmpty(name))
                    {
                        parameters[name] = value;
                    }
                }
            }

            if (parameters.Count > 0)
            {
                if (!parameters.ContainsKey("id"))
                {
                    parameters["id"] = fileId;
                }
                if (!parameters.ContainsKey("export"))
                {
                    parameters["export"] = "download";
                }
                if (!string.IsNullOrEmpty(resourceKey) && !parameters.ContainsKey("resourcekey"))
                {
                    parameters["resourcekey"] = resourceKey;
                }

                string cleanBase = baseActionUri.GetLeftPart(UriPartial.Path);
                string builtUrl = QueryHelpers.AddQueryString(cleanBase, parameters);

                try
                {
                    Uri candidate = _urlValidator.Validate(builtUrl);
                    if (IsAllowedConfirmationHost(candidate) &&
                        !string.Equals(candidate.AbsoluteUri, currentUri.AbsoluteUri, StringComparison.Ordinal))
                    {
                        confirmUri = candidate;
                        return true;
                    }
                }
                catch (ImageIngestionException)
                {
                    return false;
                }
            }
        }

        // Legacy anchor confirmation link (<a id="uc-download-link" href="...">)
        HtmlNode? link = doc.DocumentNode.SelectSingleNode("//a[@id='uc-download-link']")
            ?? doc.DocumentNode.SelectNodes("//a[@href]")
                ?.FirstOrDefault(a => a.GetAttributeValue("href", string.Empty).Contains("confirm=", StringComparison.OrdinalIgnoreCase));

        if (link is not null)
        {
            string href = WebUtility.HtmlDecode(link.GetAttributeValue("href", string.Empty)).Trim();
            if (!string.IsNullOrWhiteSpace(href))
            {
                try
                {
                    Uri candidate = _urlValidator.ValidateRedirectTarget(currentUri, href);
                    if (IsAllowedConfirmationHost(candidate) &&
                        !string.Equals(candidate.AbsoluteUri, currentUri.AbsoluteUri, StringComparison.Ordinal))
                    {
                        confirmUri = candidate;
                        return true;
                    }
                }
                catch (ImageIngestionException)
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static ImageIngestionException ClassifyHtmlFailure(
        string html,
        Uri finalUrl,
        string fallbackMessage)
    {
        string finalHost = finalUrl.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        if (finalHost == "accounts.google.com" ||
            finalHost.EndsWith(".accounts.google.com", StringComparison.Ordinal) ||
            finalUrl.AbsolutePath.Contains("ServiceLogin", StringComparison.OrdinalIgnoreCase))
        {
            return ImageIngestionException.ProviderAuthRequired(
                "This Google Drive file is private or requires Google sign-in. Share it as 'Anyone with the link'.");
        }

        string visibleText = ExtractVisibleText(html);

        if (ContainsAny(
            visibleText,
            "file you have requested does not exist",
            "make sure that you have the correct url",
            "page not found",
            "error 404"))
        {
            return ImageIngestionException.HttpNotFound();
        }

        if (ContainsAny(
            visibleText,
            "you need access",
            "ask the owner for access",
            "sign in to continue",
            "sign in"))
        {
            return ImageIngestionException.ProviderAuthRequired(
                "This Google Drive file is not publicly accessible ('Anyone with the link' permission is required).");
        }

        if (ContainsAny(
            visibleText,
            "too many users have viewed or downloaded this file recently",
            "sorry, you can't view or download this file",
            "you don't have permission",
            "download is disabled",
            "owner has",
            "violates our terms of service"))
        {
            return new ImageIngestionException(
                ErrorCodes.HttpForbidden,
                "Google Drive download is restricted by the file owner or temporary download quota limits.",
                isTransient: false,
                upstreamStatusCode: 403);
        }

        if (ContainsAny(
            visibleText,
            "only files with binary content can be downloaded",
            "cannot be exported"))
        {
            return ImageIngestionException.UnsupportedSource(
                "This Google Drive item is not a downloadable binary image file.");
        }

        if (ContainsAny(
            visibleText,
            "can't scan this file for viruses",
            "download anyway"))
        {
            return ImageIngestionException.UnsupportedContentType(
                "Google Drive returned a virus-scan confirmation page that could not be resolved into image bytes.");
        }

        return ImageIngestionException.UnsupportedContentType(fallbackMessage);
    }

    private static string ExtractVisibleText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection? hiddenNodes = doc.DocumentNode.SelectNodes("//script|//style|//noscript");
        if (hiddenNodes is not null)
        {
            foreach (HtmlNode node in hiddenNodes)
            {
                node.Remove();
            }
        }

        string title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? string.Empty;
        string body = doc.DocumentNode.SelectSingleNode("//body")?.InnerText ?? doc.DocumentNode.InnerText ?? string.Empty;
        return WebUtility.HtmlDecode($"{title} {body}").ToLowerInvariant();
    }

    private static bool ContainsAny(string text, params string[] markers)
    {
        foreach (string marker in markers)
        {
            if (text.Contains(marker, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsAllowedConfirmationHost(Uri uri)
    {
        string host = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        return host is DriveHost or DriveUserContentHost;
    }

    private static bool ContainsSegment(string[] segments, string target) =>
        segments.Any(s => string.Equals(s, target, StringComparison.OrdinalIgnoreCase));

    private static string? GetQueryValue(Dictionary<string, StringValues> query, string key)
    {
        foreach (var pair in query)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                string? val = pair.Value.FirstOrDefault();
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
        }
        return null;
    }

    private static RetrievedImage RewrapSourceUrl(RetrievedImage fetched, Uri originalSourceUrl)
    {
        if (fetched.SourceUrl == originalSourceUrl)
        {
            return fetched;
        }

        return new RetrievedImage
        {
            Content = fetched.Content,
            ContentType = fetched.ContentType,
            SourceUrl = originalSourceUrl,
            FinalSourceUrl = fetched.FinalSourceUrl,
            FileSize = fetched.FileSize,
            Strategy = fetched.Strategy
        };
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{10,128}$")]
    private static partial Regex FileIdRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{4,128}$")]
    private static partial Regex ResourceKeyRegex();
}
