using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Storage;

namespace VehicleImage.UnitTests;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"vehicle-storage-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);

        var options = Options.Create(new StorageOptions { RootPath = _tempRoot });
        _storage = new LocalFileStorage(options, NullLogger<LocalFileStorage>.Instance);
    }

    [Fact]
    public async Task SaveOpenDelete_ValidLifecycle_WorksCorrectly()
    {
        byte[] payload = [1, 2, 3, 4, 5, 6, 7, 8];
        using var input = new MemoryStream(payload);

        string key = await _storage.SaveAsync(input, ".jpg");

        Assert.StartsWith("vehicles/", key);
        Assert.EndsWith(".jpg", key);
        Assert.DoesNotContain("\\", key);
        await using (Stream readStream = await _storage.OpenReadAsync(key))
        {
            using var copy = new MemoryStream();
            await readStream.CopyToAsync(copy);
            Assert.Equal(payload, copy.ToArray());
        }

        await _storage.DeleteAsync(key);
        await Assert.ThrowsAsync<FileNotFoundException>(() => _storage.OpenReadAsync(key));

        // Second delete is idempotent
        await _storage.DeleteAsync(key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../outside.jpg")]
    [InlineData("vehicles/../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("vehicles\\2026\\11\\test.jpg")]
    public void ResolveSafeFullPath_PathTraversalOrInvalidKey_ThrowsStorageFailed(string unsafeKey)
    {
        var ex = Assert.Throws<ImageIngestionException>(() => _storage.ResolveSafeFullPath(unsafeKey));
        Assert.Equal(ErrorCodes.StorageFailed, ex.Code);
    }

    [Theory]
    [InlineData(".svg")]
    [InlineData(".exe")]
    [InlineData(".html")]
    [InlineData("")]
    public async Task SaveAsync_DisallowedExtension_ThrowsStorageFailed(string ext)
    {
        using var input = new MemoryStream([1, 2, 3]);
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(() => _storage.SaveAsync(input, ext));
        Assert.Equal(ErrorCodes.StorageFailed, ex.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
