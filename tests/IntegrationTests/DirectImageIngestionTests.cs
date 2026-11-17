using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleImage.Api.Data;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class DirectImageIngestionTests : IAsyncLifetime
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
    [InlineData("/fixtures/images/valid-car.jpg", "JPEG", "image/jpeg")]
    [InlineData("/fixtures/images/valid-car.png", "PNG", "image/png")]
    [InlineData("/fixtures/images/valid-car.webp", "WebP", "image/webp")]
    public async Task CreateVehicle_ValidDirectImage_StoresFileAndPersistsMetadataWithoutHeadRequest(
        string path,
        string expectedFormat,
        string expectedContentType)
    {
        _supplier.ResetCounters();

        var request = new CreateVehicleRequest
        {
            Make = "Porsche",
            Model = "911",
            Variant = "Carrera GTS",
            Year = 2026,
            ImageUrl = _supplier.Url(path)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var submission = await response.Content.ReadFromJsonAsync<ImageSubmissionResponse>();
        Assert.NotNull(submission);
        Assert.Equal("Completed", submission.Status);
        Assert.Equal($"/api/images/{submission.ImageId}", submission.ImageUrl);

        // Verify no HEAD request was sent (Decision 4)
        Assert.DoesNotContain("HEAD", _supplier.RequestMethods);
        Assert.Equal(1, _supplier.GetRequestCount(path));

        // Verify database persistence and VehicleResponse shape
        var detail = await _client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{submission.VehicleId}");
        Assert.NotNull(detail);
        Assert.Equal("Porsche", detail.Make);
        Assert.Equal("911", detail.Model);
        Assert.Equal("Carrera GTS", detail.Variant);
        Assert.Equal(expectedFormat, detail.Format);
        Assert.Equal(expectedContentType, detail.ContentType);
        Assert.Equal("Direct", detail.RetrievalStrategy);
        Assert.Equal(640, detail.Width);
        Assert.Equal(400, detail.Height);
    }

    [Theory]
    [InlineData("/fixtures/images/corrupted.jpg", ErrorCodes.ImageDecodeFailed)]
    [InlineData("/fixtures/images/unsupported.gif", ErrorCodes.UnsupportedImageFormat)]
    [InlineData("/fixtures/images/unsupported.bmp", ErrorCodes.UnsupportedImageFormat)]
    [InlineData("/fixtures/images/vector.svg", ErrorCodes.UnsupportedContentType)]
    [InlineData("/fixtures/images/html-masquerade.jpg", ErrorCodes.UnsupportedContentType)]
    [InlineData("/fixtures/images/too-small.png", ErrorCodes.ImageTooSmall)]
    [InlineData("/fixtures/images/decompression-bomb.png", ErrorCodes.ImageDimensionsExceeded)]
    [InlineData("/status/404", ErrorCodes.HttpNotFound)]
    [InlineData("/status/401", ErrorCodes.HttpUnauthorized)]
    [InlineData("/status/403", ErrorCodes.HttpForbidden)]
    [InlineData("/status/429-long", ErrorCodes.HttpRateLimited)]
    [InlineData("/status/500", ErrorCodes.HttpServerError)]
    public async Task CreateVehicle_InvalidOrFailingUpstreamResource_Returns422WithExpectedCodeAndPersistsNothing(
        string path,
        string expectedErrorCode)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Toyota",
            Model = "Supra",
            Year = 2025,
            ImageUrl = _supplier.Url(path)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(expectedErrorCode, failure.Code);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await db.Vehicles.CountAsync());
    }

    [Fact]
    public async Task CreateVehicle_PermanentImageValidationFailure_IsNotRetried()
    {
        _supplier.ResetCounters();

        var request = new CreateVehicleRequest
        {
            Make = "Toyota",
            Model = "Supra",
            Year = 2025,
            ImageUrl = _supplier.Url("/fixtures/images/corrupted.jpg")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, _supplier.GetRequestCount("/fixtures/images/corrupted.jpg"));
    }

    [Fact]
    public async Task CreateVehicle_Transient503Then200_RetriesAndSucceeds()
    {
        _supplier.ResetCounters();

        var request = new CreateVehicleRequest
        {
            Make = "Honda",
            Model = "Civic Type R",
            Year = 2026,
            ImageUrl = _supplier.Url("/flaky/transient-503")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, _supplier.GetRequestCount("/flaky/transient-503"));
    }

    [Fact]
    public async Task CreateVehicle_DatabaseSaveFailure_DeletesStoredFileAsCompensation()
    {
        // Drop the Vehicles table in an isolated factory so SaveChangesAsync throws a DB error after file write
        using var brokenDbFactory = new ApiFactory(_supplier.Port);
        using (var scope = brokenDbFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE VehicleImages; DROP TABLE Vehicles;");
        }

        using var client = brokenDbFactory.CreateClient();
        var request = new CreateVehicleRequest
        {
            Make = "Ford",
            Model = "Mustang",
            Year = 2026,
            ImageUrl = _supplier.Url("/fixtures/images/valid-car.jpg")
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.DatabasePersistenceFailed, failure.Code);

        // Verify the compensating file delete removed the stored image file so no orphan remains
        string[] filesOnDisk = Directory.GetFiles(brokenDbFactory.TempStorageRoot, "*", SearchOption.AllDirectories);
        Assert.Empty(filesOnDisk);
    }
}
