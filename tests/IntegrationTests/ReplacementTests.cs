using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class ReplacementTests : IAsyncLifetime
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
    public async Task ReplaceImage_ValidNewImage_UpdatesReferenceWithNewImageIdAndDeletesOldFile()
    {
        // 1. Create vehicle with JPEG
        var createReq = new CreateVehicleRequest
        {
            Make = "Ferrari",
            Model = "296 GTB",
            Year = 2026,
            ImageUrl = _supplier.Url("/fixtures/images/valid-car.jpg")
        };
        var createResp = await _client.PostAsJsonAsync("/api/vehicles", createReq);
        var created = (await createResp.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;

        string[] filesAfterCreate = Directory.GetFiles(_factory.TempStorageRoot, "*", SearchOption.AllDirectories);
        Assert.Single(filesAfterCreate);
        string firstFilePath = filesAfterCreate[0];

        // 2. Replace with PNG
        var replaceReq = new ReplaceVehicleImageRequest
        {
            ImageUrl = _supplier.Url("/fixtures/images/valid-car.png")
        };
        var replaceResp = await _client.PutAsJsonAsync($"/api/vehicles/{created.VehicleId}/image", replaceReq);
        Assert.Equal(HttpStatusCode.OK, replaceResp.StatusCode);

        var replaced = (await replaceResp.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;
        Assert.Equal(created.VehicleId, replaced.VehicleId);
        Assert.NotEqual(created.ImageId, replaced.ImageId); // Decision 5.5: new ImageId on replacement

        // Old file was deleted after commit; only new PNG file exists
        Assert.False(File.Exists(firstFilePath));
        string[] filesAfterReplace = Directory.GetFiles(_factory.TempStorageRoot, "*", SearchOption.AllDirectories);
        Assert.Single(filesAfterReplace);
        Assert.EndsWith(".png", filesAfterReplace[0]);

        // Old ImageId now returns 404, new ImageId returns 200 image/png
        var oldImgResp = await _client.GetAsync(created.ImageUrl);
        Assert.Equal(HttpStatusCode.NotFound, oldImgResp.StatusCode);

        var newImgResp = await _client.GetAsync(replaced.ImageUrl);
        Assert.Equal(HttpStatusCode.OK, newImgResp.StatusCode);
        Assert.Equal("image/png", newImgResp.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ReplaceImage_FailedRetrievalOrValidation_PreservesExistingImageAndFile()
    {
        // 1. Create vehicle with valid JPEG
        var createReq = new CreateVehicleRequest
        {
            Make = "Lamborghini",
            Model = "Revuelto",
            Year = 2026,
            ImageUrl = _supplier.Url("/fixtures/images/valid-car.jpg")
        };
        var createResp = await _client.PostAsJsonAsync("/api/vehicles", createReq);
        var created = (await createResp.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;

        // 2. Attempt replacement with a corrupted image
        var badReplaceReq = new ReplaceVehicleImageRequest
        {
            ImageUrl = _supplier.Url("/fixtures/images/corrupted.jpg")
        };
        var replaceResp = await _client.PutAsJsonAsync($"/api/vehicles/{created.VehicleId}/image", badReplaceReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, replaceResp.StatusCode);

        var failure = (await replaceResp.Content.ReadFromJsonAsync<ImageFailureResponse>())!;
        Assert.Equal(ErrorCodes.ImageDecodeFailed, failure.Code);

        // 3. Verify original image is still intact in DB and served by API
        var vehicle = (await _client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{created.VehicleId}"))!;
        Assert.Equal(created.ImageId, vehicle.ImageId);
        Assert.Equal(created.ImageUrl, vehicle.ImageUrl);

        var imageResp = await _client.GetAsync(created.ImageUrl);
        Assert.Equal(HttpStatusCode.OK, imageResp.StatusCode);
        byte[] servedBytes = await imageResp.Content.ReadAsByteArrayAsync();
        Assert.Equal(_supplier.ValidJpegBytes, servedBytes);
    }

    [Fact]
    public async Task ReplaceImage_UnknownVehicle_Returns404WithoutFetchingUrl()
    {
        _supplier.ResetCounters();

        var replaceReq = new ReplaceVehicleImageRequest
        {
            ImageUrl = _supplier.Url("/fixtures/images/valid-car.jpg")
        };
        var resp = await _client.PutAsJsonAsync($"/api/vehicles/{Guid.NewGuid()}/image", replaceReq);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        Assert.Equal(0, _supplier.TotalRequests);
    }
}
