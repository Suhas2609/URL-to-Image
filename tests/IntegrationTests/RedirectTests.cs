using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class RedirectTests : IAsyncLifetime
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

    [Fact]
    public async Task CreateVehicle_ValidRedirectChain_FollowsRedirectsAndStoresFinalImage()
    {
        var request = new CreateVehicleRequest
        {
            Make = "Mazda",
            Model = "MX-5",
            Year = 2026,
            ImageUrl = _supplier.Url("/redirect/chain-start")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var submission = await response.Content.ReadFromJsonAsync<ImageSubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal("Completed", submission.Status);
    }

    [Fact]
    public async Task CreateVehicle_RedirectLoopExceedingLimit_Returns422RedirectLimitExceeded()
    {
        var request = new CreateVehicleRequest
        {
            Make = "Mazda",
            Model = "MX-5",
            Year = 2026,
            ImageUrl = _supplier.Url("/redirect/loop/1")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.RedirectLimitExceeded, failure.Code);
    }

    [Theory]
    [InlineData("/redirect/to-localhost")]
    public async Task CreateVehicle_RedirectToUnsafeDestination_Returns422UnsafeDestination(string path)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Mazda",
            Model = "MX-5",
            Year = 2026,
            ImageUrl = _supplier.Url(path)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.UnsafeDestination, failure.Code);
    }
}
