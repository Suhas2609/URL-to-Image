using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class UrlValidationTests : IAsyncLifetime
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
    [InlineData("http://localhost/fixtures/images/valid-car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://127.0.0.1/fixtures/images/valid-car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://[::1]/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("file:///etc/passwd", ErrorCodes.InvalidUrl)]
    [InlineData("ftp://supplier.example.com/car.jpg", ErrorCodes.InvalidUrl)]
    public async Task CreateVehicle_LoopbackOrDisallowedScheme_IsRejectedBeforeContactingTarget(
        string maliciousUrl,
        string expectedCode)
    {
        _supplier.ResetCounters();

        var request = new CreateVehicleRequest
        {
            Make = "Security",
            Model = "Test",
            Year = 2026,
            ImageUrl = maliciousUrl
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(expectedCode, failure.Code);
        Assert.Equal(0, _supplier.TotalRequests);
    }
}
