using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class HtmlExtractionTests : IAsyncLifetime
{
    private MockSupplierServer _supplier = null!;
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _supplier = await MockSupplierServer.StartAsync();
        _factory = new ApiFactory(_supplier.Port);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await _supplier.DisposeAsync();
    }

    [Theory]
    [InlineData("/fixtures/html/og-image.html", "JPEG")]
    [InlineData("/fixtures/html/twitter-image.html", "WebP")]
    [InlineData("/fixtures/html/multi-candidate.html", "PNG")]
    [InlineData("/fixtures/html/relative-urls.html", "JPEG")]
    [InlineData("/fixtures/html/lazy-data-src.html", "WebP")]
    [InlineData("/fixtures/html/srcset-gallery.html", "JPEG")]
    [InlineData("/fixtures/html/malformed.html", "PNG")]
    [InlineData("/fixtures/html/charset-iso.html", "JPEG")]
    public async Task CreateVehicle_HtmlPageWithVehicleImage_ExtractsValidatesAndStoresImage(
        string htmlPath,
        string expectedFormat)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Audi",
            Model = "RS6 Avant",
            Year = 2026,
            ImageUrl = _supplier.Url(htmlPath)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var submission = (await response.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;
        var vehicle = (await _client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{submission.VehicleId}"))!;

        Assert.Equal("Html", vehicle.RetrievalStrategy);
        Assert.Equal(expectedFormat, vehicle.Format);
        Assert.Equal(640, vehicle.Width);
        Assert.Equal(400, vehicle.Height);
    }

    [Theory]
    [InlineData("/fixtures/html/no-image.html")]
    [InlineData("/fixtures/html/error-200.html")]
    public async Task CreateVehicle_HtmlPageWithNoImageOrSoft404ErrorPage_Returns422NoImageFound(string htmlPath)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Audi",
            Model = "A4",
            Year = 2026,
            ImageUrl = _supplier.Url(htmlPath)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = (await response.Content.ReadFromJsonAsync<ImageFailureResponse>())!;
        Assert.Equal(ErrorCodes.NoImageFound, failure.Code);
    }
}
