using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class StreamingLimitTests : IAsyncLifetime
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
    [InlineData("/stream/oversize-no-length")]
    [InlineData("/stream/oversize-misleading-length")]
    public async Task CreateVehicle_OversizedStreamedResponse_AbortsAndReturns422ImageTooLarge(string path)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Rivian",
            Model = "R1T",
            Year = 2026,
            ImageUrl = _supplier.Url(path)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.ImageTooLarge, failure.Code);
    }

    [Fact]
    public async Task CreateVehicle_SlowUpstreamExceedingTimeout_Returns422RequestTimeout()
    {
        var request = new CreateVehicleRequest
        {
            Make = "Lucid",
            Model = "Air",
            Year = 2026,
            ImageUrl = _supplier.Url("/slow/timeout")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.RequestTimeout, failure.Code);
        Assert.True(failure.Retryable);
    }

    [Theory]
    [InlineData("/stream/oversize-non-image", ErrorCodes.UnsupportedContentType)]
    [InlineData("/stream/oversize-gif", ErrorCodes.UnsupportedImageFormat)]
    public async Task CreateVehicle_OversizedNonImageOrUnsupportedFormat_AbortsAfterPrefixWithoutBuffering(
        string path,
        string expectedErrorCode)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Polestar",
            Model = "4",
            Year = 2026,
            ImageUrl = _supplier.Url(path)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(expectedErrorCode, failure.Code);
    }

    [Fact]
    public async Task CreateVehicle_HtmlCandidateReturnsOversizedHtml_AbortsCandidateAfterPrefixAndFallsBackToNextCandidate()
    {
        var request = new CreateVehicleRequest
        {
            Make = "Volvo",
            Model = "EX30",
            Year = 2026,
            ImageUrl = _supplier.Url("/fixtures/html/candidate-returns-oversize-html.html")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var submission = (await response.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;
        var vehicle = (await _client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{submission.VehicleId}"))!;
        Assert.Equal("Html", vehicle.RetrievalStrategy);
        Assert.Equal("JPEG", vehicle.Format);
    }
}
