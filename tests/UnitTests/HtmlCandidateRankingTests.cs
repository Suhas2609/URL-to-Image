using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Retrieval;
using VehicleImage.Api.Security;

namespace VehicleImage.UnitTests;

public class HtmlCandidateRankingTests
{
    private readonly HtmlImageRetriever _htmlRetriever;

    public HtmlCandidateRankingTests()
    {
        var retrievalOptions = Options.Create(new RetrievalOptions());
        var retryOptions = Options.Create(new RetryOptions());
        var htmlOptions = Options.Create(new HtmlExtractionOptions());
        var destinationValidator = new DestinationValidator();
        var urlValidator = new UrlValidator(retrievalOptions, destinationValidator);

        var directRetriever = new DirectImageRetriever(
            new DummyHttpClientFactory(),
            urlValidator,
            destinationValidator,
            retrievalOptions,
            retryOptions,
            NullLogger<DirectImageRetriever>.Instance);

        _htmlRetriever = new HtmlImageRetriever(
            directRetriever,
            urlValidator,
            htmlOptions,
            NullLogger<HtmlImageRetriever>.Instance);
    }

    [Fact]
    public void ExtractAndRankCandidates_PrioritizesOgImageOverOtherCandidates_AndFiltersBoilerplate()
    {
        string html = """
            <!DOCTYPE html>
            <html>
            <head>
                <meta property="og:image" content="/media/vehicles/2026-porsche-911-hero.jpg" />
                <meta name="twitter:image" content="https://cdn.dealer.example.com/vehicles/911-twitter.jpg" />
            </head>
            <body>
                <header>
                    <img src="/assets/dealer-logo.png" width="200" height="60" alt="Dealer Logo" />
                </header>
                <main>
                    <img src="/images/pixel-tracker.gif" width="1" height="1" />
                    <img src="/vehicles/gallery-side.jpg" width="800" height="600" alt="Vehicle exterior side view" />
                </main>
                <footer>
                    <img src="/assets/footer-badge.png" width="120" height="120" />
                </footer>
            </body>
            </html>
            """;

        var pageUrl = new Uri("https://dealer.example.com/inventory/2026-porsche-911");
        IReadOnlyList<ImageCandidate> ranked = _htmlRetriever.ExtractAndRankCandidates(html, pageUrl);

        Assert.Equal(3, ranked.Count);
        Assert.Equal("https://dealer.example.com/media/vehicles/2026-porsche-911-hero.jpg", ranked[0].Url.AbsoluteUri);
        Assert.Equal(CandidateSourceKind.OgImage, ranked[0].Source);
        Assert.Equal("https://cdn.dealer.example.com/vehicles/911-twitter.jpg", ranked[1].Url.AbsoluteUri);
        Assert.Equal("https://dealer.example.com/vehicles/gallery-side.jpg", ranked[2].Url.AbsoluteUri);
    }

    [Fact]
    public void ExtractAndRankCandidates_RespectsBaseHrefAndPicksLargestSrcsetAndDataSrc()
    {
        string html = """
            <html>
            <head>
                <base href="https://static.supplier.example.com/catalog/2026/" />
            </head>
            <body>
                <div class="vehicle-gallery">
                    <img srcset="small-car.jpg 320w, medium-car.jpg 640w, hd-vehicle-main.jpg 1600w"
                         src="small-car.jpg"
                         alt="Primary vehicle photo" />
                    <img data-src="interior/cockpit.webp" width="1024" height="768" alt="Vehicle interior" />
                </div>
            </body>
            </html>
            """;

        var pageUrl = new Uri("https://supplier.example.com/listing/123");
        IReadOnlyList<ImageCandidate> ranked = _htmlRetriever.ExtractAndRankCandidates(html, pageUrl);

        Assert.NotEmpty(ranked);
        Assert.Equal("https://static.supplier.example.com/catalog/2026/hd-vehicle-main.jpg", ranked[0].Url.AbsoluteUri);
        Assert.Contains(ranked, c => c.Url.AbsoluteUri == "https://static.supplier.example.com/catalog/2026/interior/cockpit.webp");
    }

    [Fact]
    public void ExtractAndRankCandidates_HtmlErrorPageReturning200_ReturnsEmpty()
    {
        string html = """
            <!DOCTYPE html>
            <html>
            <head><title>404 Not Found - Supplier Portal</title></head>
            <body>
                <h1>Page Not Found</h1>
                <img src="https://supplier.example.com/images/oops-car.jpg" width="600" height="400" />
            </body>
            </html>
            """;

        var pageUrl = new Uri("https://supplier.example.com/missing-vehicle");
        IReadOnlyList<ImageCandidate> ranked = _htmlRetriever.ExtractAndRankCandidates(html, pageUrl);

        Assert.Empty(ranked);
    }

    [Fact]
    public void DecodeHtml_Iso88591MetaCharset_DecodesCorrectly()
    {
        string rawHtml = "<html><head><meta charset=\"iso-8859-1\"></head><body><h1>Citroën Coupé</h1><img src=\"/citroen.jpg\" width=\"600\" height=\"400\"/></body></html>";
        byte[] isoBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(rawHtml);
        using var ms = new MemoryStream(isoBytes);

        string decoded = HtmlImageRetriever.DecodeHtml(ms, null);
        Assert.Contains("Citroën Coupé", decoded);
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
