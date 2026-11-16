using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using VehicleImage.Api.Data;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Entities;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Services;
using VehicleImage.Api.Storage;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.IntegrationTests;

public sealed class VehicleServiceCancellationTests
{
    [Fact]
    public async Task CreateAsync_CancellationDuringPersistence_DeletesNewFileAndPreservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new TrackingStorage();
        var ingestion = new StubIngestionService(NewImage());
        await using var harness = await ServiceHarness.CreateAsync(
            storage,
            ingestion,
            new CancelOnCommandInterceptor(cancellation));

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Service.CreateAsync(NewCreateRequest(), cancellation.Token));

        Assert.Equal(["vehicles/new.jpg"], storage.DeletedKeys);
        Assert.All(storage.DeleteTokenCanceledStates, Assert.False);
        Assert.NotEqual(typeof(ImageIngestionException), exception.GetType());
    }

    [Fact]
    public async Task ReplaceImageAsync_CancellationDuringTransactionCreation_DeletesNewFileAndPreservesOldFile()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new TrackingStorage();
        var ingestion = new StubIngestionService(NewImage(), () => cancellation.Cancel());
        await using var harness = await ServiceHarness.CreateAsync(storage, ingestion, seedExistingImage: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Service.ReplaceImageAsync(harness.VehicleId, NewReplaceRequest(), cancellation.Token));

        Assert.Equal(["vehicles/new.jpg"], storage.DeletedKeys);
        Assert.All(storage.DeleteTokenCanceledStates, Assert.False);
        Assert.DoesNotContain("vehicles/old.jpg", storage.DeletedKeys);
        await harness.AssertExistingImageAsync();
    }

    [Fact]
    public async Task ReplaceImageAsync_CancellationDuringDatabasePersistence_DeletesNewFileAndPreservesOldFile()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new TrackingStorage();
        var ingestion = new StubIngestionService(NewImage());
        await using var harness = await ServiceHarness.CreateAsync(
            storage,
            ingestion,
            new CancelOnCommandInterceptor(cancellation),
            seedExistingImage: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Service.ReplaceImageAsync(harness.VehicleId, NewReplaceRequest(), cancellation.Token));

        Assert.Equal(["vehicles/new.jpg"], storage.DeletedKeys);
        Assert.All(storage.DeleteTokenCanceledStates, Assert.False);
        Assert.DoesNotContain("vehicles/old.jpg", storage.DeletedKeys);
        await harness.AssertExistingImageAsync();
    }

    [Fact]
    public async Task ReplaceImageAsync_CancellationDuringCommit_RetainsBothFilesWhenOutcomeIsUncertain()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new TrackingStorage();
        var ingestion = new StubIngestionService(NewImage());
        var commitInterceptor = new CancelAfterCommitInterceptor(cancellation);
        await using var harness = await ServiceHarness.CreateAsync(
            storage,
            ingestion,
            transactionInterceptor: commitInterceptor,
            seedExistingImage: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Service.ReplaceImageAsync(harness.VehicleId, NewReplaceRequest(), cancellation.Token));

        Assert.Empty(storage.DeletedKeys);
        Assert.Contains("vehicles/new.jpg", storage.KnownKeys);
        Assert.Contains("vehicles/old.jpg", storage.KnownKeys);
        await harness.AssertImageAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "vehicles/new.jpg");
    }

    [Fact]
    public async Task CreateAsync_CancellationCleanupFailure_PreservesOriginalCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var storage = new TrackingStorage { ThrowOnDelete = true };
        var ingestion = new StubIngestionService(NewImage());
        await using var harness = await ServiceHarness.CreateAsync(
            storage,
            ingestion,
            new CancelOnCommandInterceptor(cancellation));

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Service.CreateAsync(NewCreateRequest(), cancellation.Token));

        Assert.NotEqual(typeof(ImageIngestionException), exception.GetType());
        Assert.Single(storage.DeleteAttempts);
        Assert.All(storage.DeleteTokenCanceledStates, Assert.False);
    }

    private static CreateVehicleRequest NewCreateRequest() => new()
    {
        Make = "Test",
        Model = "Vehicle",
        Year = 2026,
        ImageUrl = "https://supplier.test/new.jpg"
    };

    private static ReplaceVehicleImageRequest NewReplaceRequest() => new()
    {
        ImageUrl = "https://supplier.test/new.jpg"
    };

    private static IngestedImage NewImage() => new(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        "vehicles/new.jpg",
        "image/jpeg",
        "JPEG",
        4,
        2,
        2,
        "hash",
        "https://supplier.test/new.jpg",
        "https://supplier.test/new.jpg",
        "Direct");

    private sealed class ServiceHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ILoggerFactory _loggerFactory;

        private ServiceHarness(
            SqliteConnection connection,
            ApplicationDbContext dbContext,
            VehicleService service,
            Guid vehicleId,
            ILoggerFactory loggerFactory)
        {
            _connection = connection;
            DbContext = dbContext;
            Service = service;
            VehicleId = vehicleId;
            _loggerFactory = loggerFactory;
        }

        public ApplicationDbContext DbContext { get; }
        public VehicleService Service { get; }
        public Guid VehicleId { get; }

        public static async Task<ServiceHarness> CreateAsync(
            TrackingStorage storage,
            StubIngestionService ingestion,
            DbCommandInterceptor? commandInterceptor = null,
            DbTransactionInterceptor? transactionInterceptor = null,
            bool seedExistingImage = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            Guid vehicleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            if (seedExistingImage)
            {
                var seedOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;
                await using var seedContext = new ApplicationDbContext(seedOptions);
                await seedContext.Database.EnsureCreatedAsync();
                var vehicle = new Vehicle
                {
                    Id = vehicleId,
                    Make = "Existing",
                    Model = "Vehicle",
                    Year = 2025,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Image = new VehicleImageEntity
                    {
                        Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        StorageKey = "vehicles/old.jpg",
                        SourceUrl = "https://supplier.test/old.jpg",
                        FinalSourceUrl = "https://supplier.test/old.jpg",
                        ContentType = "image/jpeg",
                        FileSize = 4,
                        Width = 2,
                        Height = 2,
                        Format = "JPEG",
                        RetrievalStrategy = "Direct",
                        Status = ImageStatus.Completed,
                        Sha256 = "old-hash",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                seedContext.Vehicles.Add(vehicle);
                await seedContext.SaveChangesAsync();
                storage.KnownKeys.Add("vehicles/old.jpg");
            }

            var interceptors = new List<IInterceptor>();
            if (commandInterceptor is not null)
            {
                interceptors.Add(commandInterceptor);
            }

            if (transactionInterceptor is not null)
            {
                interceptors.Add(transactionInterceptor);
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptors)
                .Options;
            var dbContext = new ApplicationDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            storage.KnownKeys.Add("vehicles/new.jpg");
            if (commandInterceptor is CancelOnCommandInterceptor cancelOnCommand)
            {
                cancelOnCommand.Enabled = true;
            }

            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
            var service = new VehicleService(
                dbContext,
                ingestion,
                storage,
                loggerFactory.CreateLogger<VehicleService>());
            return new ServiceHarness(connection, dbContext, service, vehicleId, loggerFactory);
        }

        public async Task AssertExistingImageAsync()
        {
            await AssertImageAsync(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "vehicles/old.jpg");
        }

        public async Task AssertImageAsync(Guid imageId, string storageKey)
        {
            var image = await DbContext.VehicleImages.AsNoTracking().SingleAsync();
            Assert.Equal(imageId, image.Id);
            Assert.Equal(storageKey, image.StorageKey);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
            _loggerFactory.Dispose();
        }
    }

    private sealed class StubIngestionService : IImageIngestionService
    {
        private readonly IngestedImage _image;
        private readonly Action? _beforeReturn;

        public StubIngestionService(IngestedImage image, Action? beforeReturn = null)
        {
            _image = image;
            _beforeReturn = beforeReturn;
        }

        public Task<IngestedImage> IngestAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            _beforeReturn?.Invoke();
            return Task.FromResult(_image);
        }
    }

    private sealed class TrackingStorage : IImageStorage
    {
        public List<string> DeleteAttempts { get; } = [];
        public List<bool> DeleteTokenCanceledStates { get; } = [];
        public List<string> DeletedKeys { get; } = [];
        public HashSet<string> KnownKeys { get; } = [];
        public bool ThrowOnDelete { get; init; }

        public Task<string> SaveAsync(Stream image, string fileExtension, CancellationToken cancellationToken = default)
            => Task.FromResult("vehicles/new.jpg");

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4]));

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            DeleteAttempts.Add(storageKey);
            DeleteTokenCanceledStates.Add(cancellationToken.IsCancellationRequested);
            if (ThrowOnDelete)
            {
                throw new IOException("cleanup failure");
            }

            DeletedKeys.Add(storageKey);
            KnownKeys.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class CancelOnCommandInterceptor : DbCommandInterceptor
    {
        private readonly CancellationTokenSource? _cancellation;

        public CancelOnCommandInterceptor(CancellationTokenSource? cancellation = null)
        {
            _cancellation = cancellation;
        }

        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => Enabled && ShouldCancel(command)
                ? ValueTask.FromException<InterceptionResult<int>>(
                    new OperationCanceledException(cancellationToken))
                : base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => Enabled && ShouldCancel(command)
                ? ValueTask.FromException<InterceptionResult<DbDataReader>>(
                    new OperationCanceledException(cancellationToken))
                : base.ReaderExecutingAsync(command, eventData, result, cancellationToken);

        private bool ShouldCancel(DbCommand command)
        {
            bool shouldCancel =
                command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                command.CommandText.Contains("DELETE", StringComparison.OrdinalIgnoreCase);
            if (shouldCancel)
            {
                _cancellation?.Cancel();
            }

            return shouldCancel;
        }
    }

    private sealed class CancelAfterCommitInterceptor : DbTransactionInterceptor
    {
        private readonly CancellationTokenSource _cancellation;

        public CancelAfterCommitInterceptor(CancellationTokenSource cancellation)
        {
            _cancellation = cancellation;
        }

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _cancellation.Cancel();
            return Task.FromException(new OperationCanceledException(_cancellation.Token));
        }
    }
}
