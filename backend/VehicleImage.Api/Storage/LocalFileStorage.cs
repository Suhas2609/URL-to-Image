using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Storage;

public sealed class LocalFileStorage : IImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".png",
        ".webp"
    };

    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<StorageOptions> options, ILogger<LocalFileStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(options.Value.RootPath))
        {
            throw new InvalidOperationException("Storage:RootPath must be configured.");
        }

        _rootPath = Path.GetFullPath(options.Value.RootPath);
        _rootPathWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
    }

    public string RootPath => _rootPath;

    public async Task<string> SaveAsync(
        Stream image,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (string.IsNullOrWhiteSpace(fileExtension) || !AllowedExtensions.Contains(fileExtension))
        {
            throw ImageIngestionException.StorageFailed($"Unsupported storage file extension '{fileExtension}'.");
        }

        string normalizedExt = fileExtension.ToLowerInvariant();
        DateTime now = DateTime.UtcNow;
        string storageKey = $"vehicles/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{normalizedExt}";

        string fullPath = ResolveSafeFullPath(storageKey);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw ImageIngestionException.StorageFailed("Unable to determine target storage directory.");
        }

        string tempPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);

            if (image.CanSeek)
            {
                image.Position = 0;
            }

            await using (var fileStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous))
            {
                await image.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, fullPath, overwrite: false);
            return storageKey;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
        catch (ImageIngestionException)
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
        catch (Exception ex)
        {
            TryDeleteTempFile(tempPath);
            _logger.LogError(ex, "Failed to save image to storage key {StorageKey}", storageKey);
            throw ImageIngestionException.StorageFailed("Failed to write validated image to storage.", ex);
        }
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveSafeFullPath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Stored image file was not found.", storageKey);
        }

        try
        {
            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 65536,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult(stream);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open stored image for key {StorageKey}", storageKey);
            throw ImageIngestionException.StorageFailed("Failed to read stored image from storage.", ex);
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveSafeFullPath(storageKey);

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete stored image for key {StorageKey}", storageKey);
            throw ImageIngestionException.StorageFailed("Failed to delete image from storage.", ex);
        }
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullPath = ResolveSafeFullPath(storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    public string ResolveSafeFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw ImageIngestionException.StorageFailed("Storage key cannot be empty.");
        }

        if (storageKey.Contains("..", StringComparison.Ordinal) ||
            storageKey.Contains('\\', StringComparison.Ordinal) ||
            storageKey.StartsWith('/') ||
            storageKey.Contains('\0') ||
            Path.IsPathRooted(storageKey))
        {
            throw ImageIngestionException.StorageFailed("Storage key contains invalid or unsafe path segments.");
        }

        string relativeOsPath = storageKey.Replace('/', Path.DirectorySeparatorChar);
        string combined = Path.GetFullPath(Path.Combine(_rootPath, relativeOsPath));

        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!combined.StartsWith(_rootPathWithSeparator, comparison))
        {
            throw ImageIngestionException.StorageFailed("Storage key resolved outside the configured storage root.");
        }

        return combined;
    }

    private void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary storage file {TempPath}", tempPath);
        }
    }
}
