using Microsoft.EntityFrameworkCore;
using VehicleImage.Api.Data;
using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Entities;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Storage;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Services;

public sealed class VehicleService : IVehicleService
{
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _dbContext;
    private readonly IImageIngestionService _ingestionService;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        ApplicationDbContext dbContext,
        IImageIngestionService ingestionService,
        IImageStorage imageStorage,
        ILogger<VehicleService> logger)
    {
        _dbContext = dbContext;
        _ingestionService = ingestionService;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    public async Task<ImageSubmissionResponse> CreateAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IngestedImage ingested = await _ingestionService.IngestAsync(request.ImageUrl, cancellationToken);

        DateTime utcNow = DateTime.UtcNow;
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Variant = string.IsNullOrWhiteSpace(request.Variant) ? null : request.Variant.Trim(),
            Year = request.Year,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        var vehicleImage = CreateVehicleImage(vehicle, ingested, utcNow);

        vehicle.Image = vehicleImage;

        try
        {
            _dbContext.Vehicles.Add(vehicle);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await CompensateFileDeleteAsync(ingested.StorageKey);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Database persistence failed while creating vehicle. Executing compensating delete for storage key {StorageKey}.",
                ingested.StorageKey);

            await CompensateFileDeleteAsync(ingested.StorageKey);

            throw ImageIngestionException.DatabasePersistenceFailed(
                "Failed to save the vehicle and image record to the database.",
                ex);
        }

        return new ImageSubmissionResponse(
            VehicleId: vehicle.Id,
            ImageId: vehicleImage.Id,
            Status: vehicleImage.Status.ToString(),
            ImageUrl: BuildInternalImageUrl(vehicleImage.Id));
    }

    public async Task<ImageSubmissionResponse> ReplaceImageAsync(
        Guid vehicleId,
        ReplaceVehicleImageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Look up the vehicle and its current image; 404 if absent before any network activity
        var initialSnapshot = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.Image)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);

        if (initialSnapshot is null)
        {
            throw ResourceNotFoundException.Vehicle(vehicleId);
        }

        Guid? previousImageId = initialSnapshot.Image?.Id;
        string? previousStorageKey = initialSnapshot.Image?.StorageKey;

        // 2, 3, 4. Retrieve, validate, and save the new image under a new unique storage key.
        // If any step here fails, the existing database row and file remain untouched.
        IngestedImage ingested = await _ingestionService.IngestAsync(request.ImageUrl, cancellationToken);

        // 5, 6, 8. Update the database reference in a transaction, conditional on the prior ImageId.
        bool commitAttempted = false;
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var vehicle = await _dbContext.Vehicles
                .Include(v => v.Image)
                .FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);

            if (vehicle is null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                await CompensateFileDeleteAsync(ingested.StorageKey);
                throw ResourceNotFoundException.Vehicle(vehicleId);
            }

            if (vehicle.Image?.Id != previousImageId)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                await CompensateFileDeleteAsync(ingested.StorageKey);
                throw new ConcurrentUpdateException();
            }

            if (vehicle.Image is not null)
            {
                _dbContext.VehicleImages.Remove(vehicle.Image);
                // Flush removal so the unique index on VehicleId allows inserting the new VehicleImage row
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            DateTime utcNow = DateTime.UtcNow;
            var newImage = CreateVehicleImage(vehicle, ingested, utcNow);

            vehicle.UpdatedAt = utcNow;
            _dbContext.VehicleImages.Add(newImage);

            await _dbContext.SaveChangesAsync(cancellationToken);
            commitAttempted = true;
            await transaction.CommitAsync(cancellationToken);
        }
        catch (ResourceNotFoundException)
        {
            throw;
        }
        catch (ConcurrentUpdateException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            if (!commitAttempted)
            {
                await CompensateFileDeleteAsync(ingested.StorageKey);
            }

            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrent update detected while replacing image for VehicleId={VehicleId}", vehicleId);
            if (!commitAttempted)
            {
                await CompensateFileDeleteAsync(ingested.StorageKey);
            }

            throw new ConcurrentUpdateException();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Database persistence failed during image replacement for VehicleId={VehicleId}. New storage key {NewStorageKey} is retained because commit outcome may be uncertain.",
                vehicleId,
                ingested.StorageKey);

            if (!commitAttempted)
            {
                await CompensateFileDeleteAsync(ingested.StorageKey);
            }

            throw ImageIngestionException.DatabasePersistenceFailed(
                "Failed to update the vehicle image reference in the database.",
                ex);
        }

        // 7. Only after a successful commit, delete the old file.
        if (!string.IsNullOrWhiteSpace(previousStorageKey) &&
            !string.Equals(previousStorageKey, ingested.StorageKey, StringComparison.Ordinal))
        {
            try
            {
                await _imageStorage.DeleteAsync(previousStorageKey, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Orphaned file warning: failed to delete superseded storage key {OldStorageKey} after replacing image for VehicleId={VehicleId}.",
                    previousStorageKey,
                    vehicleId);
            }
        }

        return new ImageSubmissionResponse(
            VehicleId: vehicleId,
            ImageId: ingested.ImageId,
            Status: ImageStatus.Completed.ToString(),
            ImageUrl: BuildInternalImageUrl(ingested.ImageId));
    }

    public async Task<PagedResponse<VehicleResponse>> ListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        int normalizedPage = Math.Max(1, page);
        int normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.Image);

        int totalCount = await query.CountAsync(cancellationToken);

        var vehicles = await query
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = vehicles.Select(MapToResponse).ToList();

        return new PagedResponse<VehicleResponse>(
            Items: items,
            Page: normalizedPage,
            PageSize: normalizedPageSize,
            TotalCount: totalCount);
    }

    public async Task<VehicleResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.Image)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vehicle is null)
        {
            throw ResourceNotFoundException.Vehicle(id);
        }

        return MapToResponse(vehicle);
    }

    public static string BuildInternalImageUrl(Guid imageId) => $"/api/images/{imageId}";

    private static VehicleImageEntity CreateVehicleImage(
        Vehicle vehicle,
        IngestedImage ingested,
        DateTime utcNow)
    {
        return new VehicleImageEntity
        {
            Id = ingested.ImageId,
            VehicleId = vehicle.Id,
            SourceUrl = ingested.SourceUrl,
            FinalSourceUrl = ingested.FinalSourceUrl,
            StorageKey = ingested.StorageKey,
            ContentType = ingested.ContentType,
            FileSize = ingested.FileSize,
            Width = ingested.Width,
            Height = ingested.Height,
            Format = ingested.Format,
            RetrievalStrategy = ingested.RetrievalStrategy,
            Status = ImageStatus.Completed,
            FailureReason = null,
            Sha256 = ingested.Sha256,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Vehicle = vehicle
        };
    }

    private static VehicleResponse MapToResponse(Vehicle vehicle)
    {
        var image = vehicle.Image;
        return new VehicleResponse(
            Id: vehicle.Id,
            Make: vehicle.Make,
            Model: vehicle.Model,
            Variant: vehicle.Variant,
            Year: vehicle.Year,
            ImageId: image?.Id,
            ImageUrl: image is not null ? BuildInternalImageUrl(image.Id) : null,
            ImageStatus: image?.Status.ToString() ?? ImageStatus.Failed.ToString(),
            ContentType: image?.ContentType,
            FileSize: image?.FileSize,
            Width: image?.Width,
            Height: image?.Height,
            Format: image?.Format,
            RetrievalStrategy: image?.RetrievalStrategy,
            CreatedAt: vehicle.CreatedAt,
            UpdatedAt: vehicle.UpdatedAt);
    }

    private async Task CompensateFileDeleteAsync(string storageKey)
    {
        try
        {
            await _imageStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception deleteEx)
        {
            _logger.LogError(
                deleteEx,
                "Orphaned file alert: compensating delete failed for storage key {OrphanStorageKey}. Manual or scheduled reconciliation required.",
                storageKey);
        }
    }
}
