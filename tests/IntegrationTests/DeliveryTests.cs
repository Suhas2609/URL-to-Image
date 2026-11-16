using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;

namespace VehicleImage.IntegrationTests;

public class DeliveryTests : IAsyncLifetime
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
    public async Task GetImageAndDownload_AfterSourceDisappears_StillServesStoredBytesWithSecurityHeaders()
    {
        const string sourcePath = "/fixtures/images/valid-car.jpg";
        var createReq = new CreateVehicleRequest
        {
            Make = "Aston Martin",
            Model = "Vantage",
            Year = 2026,
            ImageUrl = _supplier.Url(sourcePath)
        };

        var createResp = await _client.PostAsJsonAsync("/api/vehicles", createReq);
        var created = (await createResp.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;

        // Simulate the supplier deleting the original image URL!
        _supplier.DisableRoute(sourcePath);
        _supplier.ResetCounters();

        // 1. Stream via GET /api/images/{id}
        var getResp = await _client.GetAsync(created.ImageUrl);
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        Assert.Equal("image/jpeg", getResp.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", getResp.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
        Assert.Contains("immutable", getResp.Headers.CacheControl?.ToString() ?? string.Empty);

        byte[] servedBytes = await getResp.Content.ReadAsByteArrayAsync();
        Assert.Equal(_supplier.ValidJpegBytes, servedBytes);

        // Supplier was NEVER contacted during image delivery
        Assert.Equal(0, _supplier.TotalRequests);

        // 2. Download via GET /api/images/{id}/download
        var downloadResp = await _client.GetAsync($"{created.ImageUrl}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);
        Assert.Equal("attachment", downloadResp.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("aston-martin-vantage-2026", downloadResp.Content.Headers.ContentDisposition?.FileName ?? string.Empty);

        // 3. Paged listing via GET /api/vehicles
        var paged = await _client.GetFromJsonAsync<PagedResponse<VehicleResponse>>("/api/vehicles?page=1&pageSize=10");
        Assert.NotNull(paged);
        Assert.Equal(1, paged.TotalCount);
        Assert.Single(paged.Items);
        Assert.Equal(created.ImageUrl, paged.Items[0].ImageUrl);
    }

    [Fact]
    public async Task GetImage_WhenFileDeletedFromDiskBehindDatabaseBack_LogsAndReturns404()
    {
        var createReq = new CreateVehicleRequest
        {
            Make = "McLaren",
            Model = "750S",
            Year = 2026,
            ImageUrl = _supplier.Url("/fixtures/images/valid-car.jpg")
        };

        var createResp = await _client.PostAsJsonAsync("/api/vehicles", createReq);
        var created = (await createResp.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;

        foreach (string file in Directory.GetFiles(_factory.TempStorageRoot, "*", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }

        var missingFileResp = await _client.GetAsync(created.ImageUrl);
        Assert.Equal(HttpStatusCode.NotFound, missingFileResp.StatusCode);
    }
}
