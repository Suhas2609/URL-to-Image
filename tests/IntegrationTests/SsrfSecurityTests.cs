using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class SsrfSecurityTests : IAsyncLifetime
{
    private MockSupplierServer _supplier = null!;
    private ApiFactory _strictFactory = null!;
    private HttpClient _strictClient = null!;

    public async Task InitializeAsync()
    {
        _supplier = await MockSupplierServer.StartAsync();
        // Use the REAL production DestinationValidator (useStrictProductionSsrfValidator: true)
        _strictFactory = new ApiFactory(_supplier.Port, useStrictProductionSsrfValidator: true);
        _strictClient = _strictFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _strictClient.Dispose();
        _strictFactory.Dispose();
        await _supplier.DisposeAsync();
    }

    [Theory]
    [InlineData("http://localhost/fixtures/images/valid-car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://127.0.0.1/fixtures/images/valid-car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://10.0.0.1/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://172.16.0.1/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://192.168.1.1/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://169.254.169.254/latest/meta-data/", ErrorCodes.UnsafeDestination)]
    [InlineData("http://[::1]/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://[fe80::1]/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("http://[fc00::1]/car.jpg", ErrorCodes.UnsafeDestination)]
    [InlineData("file:///etc/passwd", ErrorCodes.InvalidUrl)]
    [InlineData("ftp://supplier.example.com/car.jpg", ErrorCodes.InvalidUrl)]
    public async Task CreateVehicle_DirectSsrfOrDisallowedScheme_IsRejectedBeforeContactingTarget(
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

        HttpResponseMessage response = await _strictClient.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(expectedCode, failure.Code);
        Assert.Equal(0, _supplier.TotalRequests);
    }

    [Fact]
    public async Task CreateVehicle_HostnameResolvingToPrivateAddressAtConnectTime_IsRejected()
    {
        using var rebindFactory = new ApiFactory(
            _supplier.Port,
            useStrictProductionSsrfValidator: true,
            customDnsResolver: (_, _) => new ValueTask<IPAddress[]>([IPAddress.Loopback]));
        using var client = rebindFactory.CreateClient();

        _supplier.ResetCounters();

        var request = new CreateVehicleRequest
        {
            Make = "Security",
            Model = "DnsRebind",
            Year = 2026,
            ImageUrl = "http://attacker-controlled.example.com/fixtures/images/valid-car.jpg"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.UnsafeDestination, failure.Code);
        Assert.Equal(0, _supplier.TotalRequests);
    }

    [Fact]
    public async Task CreateVehicle_DnsRebindingFromPublicToPrivateAcrossRedirect_IsRejectedAtConnectTime()
    {
        int dnsCallCount = 0;
        using var rebindFactory = new ApiFactory(
            _supplier.Port,
            useStrictProductionSsrfValidator: false,
            customDnsResolver: (host, _) =>
            {
                if (string.Equals(host, "rebind.attacker.example.com", StringComparison.OrdinalIgnoreCase))
                {
                    dnsCallCount++;
                    // First resolution returns loopback (representing the initial redirect server in test),
                    // second resolution rebinds to cloud metadata IP 169.254.169.254!
                    return dnsCallCount == 1
                        ? new ValueTask<IPAddress[]>([IPAddress.Loopback])
                        : new ValueTask<IPAddress[]>([IPAddress.Parse("169.254.169.254")]);
                }
                return new ValueTask<IPAddress[]>([IPAddress.Loopback]);
            });

        using var client = rebindFactory.CreateClient();

        // First request redirects to a second URL on the same host, triggering a new resolution or re-validation
        var request = new CreateVehicleRequest
        {
            Make = "Security",
            Model = "RebindHop",
            Year = 2026,
            ImageUrl = $"http://rebind.attacker.example.com:{_supplier.Port}/redirect/chain-start"
        };

        // Call 1 resolves to Loopback (redirect hop 1), then hop 2 or second request resolves to 169.254.169.254
        _ = await client.PostAsJsonAsync("/api/vehicles", request);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/vehicles", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondResponse.StatusCode);
        var failure = await secondResponse.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.UnsafeDestination, failure.Code);
    }
}
