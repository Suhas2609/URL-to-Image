# Complete VehicleImageSystem Codebase

## 1. Repository Overview

Repository tree (generated and irrelevant directories excluded):

```text
├── backend/
    └── VehicleImage.Api/
        ├── Configuration/
            ├── HtmlExtractionOptions.cs
            ├── ImageValidationOptions.cs
            ├── RetrievalOptions.cs
            ├── RetryOptions.cs
            └── StorageOptions.cs
        ├── Controllers/
            ├── ImagesController.cs
            └── VehiclesController.cs
        ├── Data/
            ├── Configurations/
                ├── VehicleConfiguration.cs
                └── VehicleImageConfiguration.cs
            └── ApplicationDbContext.cs
        ├── DTOs/
            ├── Images/
                └── ImageFailureResponse.cs
            ├── Vehicles/
                ├── CreateVehicleRequest.cs
                ├── ImageSubmissionResponse.cs
                ├── ReplaceVehicleImageRequest.cs
                └── VehicleResponse.cs
            └── PagedResponse.cs
        ├── Entities/
            ├── Vehicle.cs
            └── VehicleImage.cs
        ├── Exceptions/
            ├── ConcurrentUpdateException.cs
            ├── ErrorCodes.cs
            ├── GlobalExceptionHandler.cs
            ├── ImageIngestionException.cs
            └── ResourceNotFoundException.cs
        ├── ImageProcessing/
            ├── ImageMetadata.cs
            └── ImageValidator.cs
        ├── Migrations/
            ├── 20261116094554_InitialCreate.cs
            ├── 20261116094554_InitialCreate.Designer.cs
            └── ApplicationDbContextModelSnapshot.cs
        ├── Models/
            ├── IngestedImage.cs
            └── RetrievedImage.cs
        ├── Properties/
            └── launchSettings.json
        ├── Retrieval/
            ├── DirectImageRetriever.cs
            ├── HtmlImageRetriever.cs
            └── RetrievalOrchestrator.cs
        ├── Security/
            ├── DestinationValidator.cs
            ├── SsrfGuardHttpHandler.cs
            └── UrlValidator.cs
        ├── Services/
            ├── IImageDeliveryService.cs
            ├── IImageIngestionService.cs
            ├── ImageDeliveryService.cs
            ├── ImageIngestionService.cs
            ├── IVehicleService.cs
            └── VehicleService.cs
        ├── Storage/
            ├── IImageStorage.cs
            └── LocalFileStorage.cs
        ├── appsettings.Development.json
        ├── appsettings.json
        ├── Program.cs
        ├── VehicleImage.Api.csproj
        └── VehicleImage.Api.http
├── docs/
    └── Architecture_and_Decisions.md
├── frontend/
    └── vehicle-image-dashboard/
        ├── src/
            ├── components/
                ├── ImagePlaceholder.jsx
                ├── ImageReplaceForm.jsx
                ├── StatusBadge.jsx
                ├── VehicleCard.jsx
                └── VehicleForm.jsx
            ├── pages/
                └── DashboardPage.jsx
            ├── services/
                └── vehicleApi.js
            ├── App.jsx
            ├── index.css
            └── main.jsx
        ├── index.html
        ├── package-lock.json
        ├── package.json
        └── vite.config.js
├── storage/
    └── vehicles/
        └── .gitkeep
├── tests/
    ├── IntegrationTests/
        ├── Fixtures/
            └── Html/
                ├── error-200.html
                ├── lazy-data-src.html
                ├── malformed.html
                ├── multi-candidate.html
                ├── no-image.html
                ├── og-image.html
                ├── relative-urls.html
                ├── srcset-gallery.html
                └── twitter-image.html
        ├── ApiFactory.cs
        ├── DeliveryTests.cs
        ├── DirectImageIngestionTests.cs
        ├── HtmlExtractionTests.cs
        ├── MockSupplierServer.cs
        ├── RedirectTests.cs
        ├── ReplacementTests.cs
        ├── SsrfSecurityTests.cs
        ├── StreamingLimitTests.cs
        ├── VehicleImage.IntegrationTests.csproj
        └── VehicleServiceCancellationTests.cs
    └── UnitTests/
        ├── DestinationValidatorTests.cs
        ├── GlobalExceptionHandlerTests.cs
        ├── HtmlCandidateRankingTests.cs
        ├── ImageValidatorTests.cs
        ├── LocalFileStorageTests.cs
        ├── UrlValidatorTests.cs
        └── VehicleImage.UnitTests.csproj
├── .editorconfig
├── .gitignore
├── README.md
└── VehicleImageSystem.slnx
```

This export contains complete text contents for every eligible source, test, frontend, configuration, and documentation file selected from the current repository. Secrets are redacted only where a configuration value matches a credential-like key or connection-string credential.

## 2. Backend

### File: `backend/VehicleImage.Api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VehicleImageDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Storage": {
    "RootPath": "../../storage"
  }
}
```

### File: `backend/VehicleImage.Api/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VehicleImageDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Storage": {
    "RootPath": "../../storage"
  },
  "Retrieval": {
    "MaxDownloadBytes": 20971520,
    "TimeoutSeconds": 15,
    "ConnectTimeoutSeconds": 5,
    "MaxRedirects": 5,
    "AllowedPorts": [ 80, 443 ],
    "MaxUrlLength": 2048,
    "UserAgent": "VehicleImageSystem/1.0",
    "PooledConnectionLifetimeSeconds": 60
  },
  "ImageValidation": {
    "AllowedFormats": [ "Jpeg", "Png", "WebP" ],
    "MinWidth": 100,
    "MinHeight": 100,
    "MaxWidth": 10000,
    "MaxHeight": 10000,
    "MaxPixels": 50000000
  },
  "HtmlExtraction": {
    "MaxCandidates": 3,
    "MinDeclaredDimension": 80,
    "IgnoreUrlPatterns": [
      "logo",
      "icon",
      "favicon",
      "sprite",
      "pixel",
      "tracking",
      "tracker",
      "banner",
      "badge",
      "avatar",
      "spinner",
      "loading",
      "placeholder",
      "1x1",
      "/ads/",
      "ad-",
      "ad_"
    ],
    "OgImageWeight": 100,
    "TwitterImageWeight": 90,
    "SrcsetWeight": 70,
    "DataSrcWeight": 65,
    "ImgSrcWeight": 60,
    "VehicleKeywordBonus": 25,
    "LargeDimensionBonus": 20,
    "BoilerplateContainerPenalty": 50
  },
  "Retry": {
    "MaxAttempts": 3,
    "BaseDelayMs": 150,
    "MaxDelayMs": 1500,
    "EnableJitter": true,
    "MaxRetryAfterSeconds": 3
  }
}
```

### File: `backend/VehicleImage.Api/Configuration/HtmlExtractionOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class HtmlExtractionOptions
{
    public const string SectionName = "HtmlExtraction";

    [Range(1, 10)]
    public int MaxCandidates { get; set; } = 3;

    [Range(1, 2000)]
    public int MinDeclaredDimension { get; set; } = 80;

    public string[] IgnoreUrlPatterns { get; set; } =
    [
        "logo",
        "icon",
        "favicon",
        "sprite",
        "pixel",
        "tracking",
        "tracker",
        "banner",
        "badge",
        "avatar",
        "spinner",
        "loading",
        "placeholder",
        "1x1",
        "/ads/",
        "ad-",
        "ad_"
    ];

    public int OgImageWeight { get; set; } = 100;
    public int TwitterImageWeight { get; set; } = 90;
    public int SrcsetWeight { get; set; } = 70;
    public int DataSrcWeight { get; set; } = 65;
    public int ImgSrcWeight { get; set; } = 60;
    public int VehicleKeywordBonus { get; set; } = 25;
    public int LargeDimensionBonus { get; set; } = 20;
    public int BoilerplateContainerPenalty { get; set; } = 50;
}
```

### File: `backend/VehicleImage.Api/Configuration/ImageValidationOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class ImageValidationOptions
{
    public const string SectionName = "ImageValidation";

    [Required]
    public string[] AllowedFormats { get; set; } = ["Jpeg", "Png", "WebP"];

    [Range(1, 10000)]
    public int MinWidth { get; set; } = 100;

    [Range(1, 10000)]
    public int MinHeight { get; set; } = 100;

    [Range(100, 50000)]
    public int MaxWidth { get; set; } = 10000;

    [Range(100, 50000)]
    public int MaxHeight { get; set; } = 10000;

    [Range(10000, 200_000_000)]
    public long MaxPixels { get; set; } = 50_000_000;
}
```

### File: `backend/VehicleImage.Api/Configuration/RetrievalOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    [Range(1024, 104_857_600)]
    public long MaxDownloadBytes { get; set; } = 20 * 1024 * 1024; // 20 MB

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    [Range(1, 60)]
    public int ConnectTimeoutSeconds { get; set; } = 5;

    [Range(0, 20)]
    public int MaxRedirects { get; set; } = 5;

    [Required]
    public int[] AllowedPorts { get; set; } = [80, 443];

    [Range(64, 8192)]
    public int MaxUrlLength { get; set; } = 2048;

    [Required(AllowEmptyStrings = false)]
    public string UserAgent { get; set; } = "VehicleImageSystem/1.0";

    [Range(5, 600)]
    public int PooledConnectionLifetimeSeconds { get; set; } = 60;
}
```

### File: `backend/VehicleImage.Api/Configuration/RetryOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class RetryOptions
{
    public const string SectionName = "Retry";

    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 3;

    [Range(10, 10000)]
    public int BaseDelayMs { get; set; } = 150;

    [Range(50, 30000)]
    public int MaxDelayMs { get; set; } = 1500;

    public bool EnableJitter { get; set; } = true;

    [Range(1, 30)]
    public int MaxRetryAfterSeconds { get; set; } = 3;
}
```

### File: `backend/VehicleImage.Api/Configuration/StorageOptions.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(AllowEmptyStrings = false)]
    public string RootPath { get; set; } = string.Empty;
}
```

### File: `backend/VehicleImage.Api/Controllers/ImagesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using VehicleImage.Api.Services;

namespace VehicleImage.Api.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController : ControllerBase
{
    private readonly IImageDeliveryService _deliveryService;

    public ImagesController(IImageDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        ImageDeliveryResult result = await _deliveryService.OpenImageAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.ContentLength = result.FileSize;

        return File(result.Stream, result.ContentType);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        ImageDeliveryResult result = await _deliveryService.OpenImageAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.ContentLength = result.FileSize;

        return File(result.Stream, result.ContentType, result.DownloadFileName);
    }
}
```

### File: `backend/VehicleImage.Api/Controllers/VehiclesController.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Services;

namespace VehicleImage.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ImageSubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ImageSubmissionResponse>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        ImageSubmissionResponse result = await _vehicleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.VehicleId }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<VehicleResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Query parameters 'page' must be >= 1 and 'pageSize' must be between 1 and 100.");
        }

        PagedResponse<VehicleResponse> result = await _vehicleService.ListAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        VehicleResponse vehicle = await _vehicleService.GetAsync(id, cancellationToken);
        return Ok(vehicle);
    }

    [HttpPut("{id:guid}/image")]
    [ProducesResponseType(typeof(ImageSubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ImageSubmissionResponse>> ReplaceImage(
        Guid id,
        [FromBody] ReplaceVehicleImageRequest request,
        CancellationToken cancellationToken)
    {
        ImageSubmissionResponse result = await _vehicleService.ReplaceImageAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
```

### File: `backend/VehicleImage.Api/Data/ApplicationDbContext.cs`

```csharp

using Microsoft.EntityFrameworkCore;
using VehicleImage.Api.Entities;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehicleImageEntity> VehicleImages =>
        Set<VehicleImageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly
        );
    }
}
```

### File: `backend/VehicleImage.Api/Data/Configurations/VehicleConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleImage.Api.Entities;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles", t =>
        {
            t.HasCheckConstraint("CK_Vehicles_Year", "[Year] >= 1886 AND [Year] <= 2100");
        });

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Make)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Variant)
            .IsRequired(false)
            .HasMaxLength(150);

        builder.Property(v => v.Year)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(v => v.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(v => v.Image)
            .WithOne(image => image.Vehicle)
            .HasForeignKey<VehicleImageEntity>(image => image.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### File: `backend/VehicleImage.Api/Data/Configurations/VehicleImageConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleImageEntity = VehicleImage.Api.Entities.VehicleImage;

namespace VehicleImage.Api.Data.Configurations;

public class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImageEntity>
{
    public void Configure(EntityTypeBuilder<VehicleImageEntity> builder)
    {
        builder.ToTable("VehicleImages", t =>
        {
            t.HasCheckConstraint("CK_VehicleImages_FileSize", "[FileSize] > 0");
            t.HasCheckConstraint("CK_VehicleImages_Width", "[Width] > 0");
            t.HasCheckConstraint("CK_VehicleImages_Height", "[Height] > 0");
        });

        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id)
            .ValueGeneratedNever();

        builder.HasIndex(image => image.VehicleId)
            .IsUnique();

        builder.HasIndex(image => image.StorageKey)
            .IsUnique();

        builder.Property(image => image.SourceUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(image => image.FinalSourceUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(image => image.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(image => image.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(image => image.Format)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(image => image.RetrievalStrategy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(image => image.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(image => image.FailureReason)
            .IsRequired(false);

        builder.Property(image => image.Sha256)
            .IsRequired()
            .HasMaxLength(64)
            .IsFixedLength();

        builder.Property(image => image.FileSize)
            .IsRequired();

        builder.Property(image => image.Width)
            .IsRequired();

        builder.Property(image => image.Height)
            .IsRequired();

        builder.Property(image => image.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(image => image.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
```

### File: `backend/VehicleImage.Api/DTOs/Images/ImageFailureResponse.cs`

```csharp
using System.Text.Json.Serialization;

namespace VehicleImage.Api.DTOs.Images;

public sealed record ImageFailureResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("traceId")] string TraceId,
    [property: JsonPropertyName("retryable")] bool Retryable,
    [property: JsonPropertyName("retryAfterSeconds")] int? RetryAfterSeconds = null,
    [property: JsonPropertyName("type")] string StatusType = "https://tools.ietf.org/html/rfc4918#section-11.2"
);
```

### File: `backend/VehicleImage.Api/DTOs/PagedResponse.cs`

```csharp
namespace VehicleImage.Api.DTOs;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
);
```

### File: `backend/VehicleImage.Api/DTOs/Vehicles/CreateVehicleRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.DTOs.Vehicles;

public sealed class CreateVehicleRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Make { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Variant { get; set; }

    [Required]
    public int Year { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(2048, MinimumLength = 1)]
    public string ImageUrl { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        int maxYear = DateTime.UtcNow.Year + 1;
        if (Year < 1886 || Year > maxYear)
        {
            yield return new ValidationResult(
                $"Year must be between 1886 and {maxYear}.",
                [nameof(Year)]);
        }
    }
}
```

### File: `backend/VehicleImage.Api/DTOs/Vehicles/ImageSubmissionResponse.cs`

```csharp
namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record ImageSubmissionResponse(
    Guid VehicleId,
    Guid ImageId,
    string Status,
    string ImageUrl
);
```

### File: `backend/VehicleImage.Api/DTOs/Vehicles/ReplaceVehicleImageRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.DTOs.Vehicles;

public sealed class ReplaceVehicleImageRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(2048, MinimumLength = 1)]
    public string ImageUrl { get; set; } = string.Empty;
}
```

### File: `backend/VehicleImage.Api/DTOs/Vehicles/VehicleResponse.cs`

```csharp
namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record VehicleResponse(
    Guid Id,
    string Make,
    string Model,
    string? Variant,
    int Year,
    Guid? ImageId,
    string? ImageUrl,
    string ImageStatus,
    string? ContentType,
    long? FileSize,
    int? Width,
    int? Height,
    string? Format,
    string? RetrievalStrategy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
```

### File: `backend/VehicleImage.Api/Entities/Vehicle.cs`

```csharp
namespace VehicleImage.Api.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public string? Variant { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public VehicleImage? Image { get; set; }
}
```

### File: `backend/VehicleImage.Api/Entities/VehicleImage.cs`

```csharp
namespace VehicleImage.Api.Entities;

public enum ImageStatus
{
    Completed,
    Failed
}

public class VehicleImage
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public required string SourceUrl { get; set; }
    public required string FinalSourceUrl { get; set; }
    public required string StorageKey { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public required string Format { get; set; }
    public required string RetrievalStrategy { get; set; }
    public ImageStatus Status { get; set; } = ImageStatus.Completed;
    public string? FailureReason { get; set; }
    public required string Sha256 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Vehicle? Vehicle { get; set; }
}
```

### File: `backend/VehicleImage.Api/Exceptions/ConcurrentUpdateException.cs`

```csharp
namespace VehicleImage.Api.Exceptions;

public sealed class ConcurrentUpdateException : Exception
{
    public string Code => ErrorCodes.ConcurrentUpdate;

    public ConcurrentUpdateException(string message = "The vehicle image was modified by another request. Please retry.")
        : base(message)
    {
    }
}
```

### File: `backend/VehicleImage.Api/Exceptions/ErrorCodes.cs`

```csharp
namespace VehicleImage.Api.Exceptions;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidUrl = "INVALID_URL";
    public const string UnsafeDestination = "UNSAFE_DESTINATION";
    public const string DnsResolutionFailed = "DNS_RESOLUTION_FAILED";
    public const string ConnectionTimeout = "CONNECTION_TIMEOUT";
    public const string RequestTimeout = "REQUEST_TIMEOUT";
    public const string TlsFailure = "TLS_FAILURE";
    public const string RedirectLimitExceeded = "REDIRECT_LIMIT_EXCEEDED";
    public const string HttpNotFound = "HTTP_NOT_FOUND";
    public const string HttpUnauthorized = "HTTP_UNAUTHORIZED";
    public const string HttpForbidden = "HTTP_FORBIDDEN";
    public const string HttpRateLimited = "HTTP_RATE_LIMITED";
    public const string HttpServerError = "HTTP_SERVER_ERROR";
    public const string HttpError = "HTTP_ERROR";
    public const string UnsupportedContentType = "UNSUPPORTED_CONTENT_TYPE";
    public const string UnsupportedImageFormat = "UNSUPPORTED_IMAGE_FORMAT";
    public const string ImageTooLarge = "IMAGE_TOO_LARGE";
    public const string ImageDimensionsExceeded = "IMAGE_DIMENSIONS_EXCEEDED";
    public const string ImageTooSmall = "IMAGE_TOO_SMALL";
    public const string ImageDecodeFailed = "IMAGE_DECODE_FAILED";
    public const string NoImageFound = "NO_IMAGE_FOUND";
    public const string StorageFailed = "STORAGE_FAILED";
    public const string DatabasePersistenceFailed = "DATABASE_PERSISTENCE_FAILED";
    public const string VehicleNotFound = "VEHICLE_NOT_FOUND";
    public const string ImageNotFound = "IMAGE_NOT_FOUND";
    public const string ConcurrentUpdate = "CONCURRENT_UPDATE";
    public const string InternalError = "INTERNAL_ERROR";
}
```

### File: `backend/VehicleImage.Api/Exceptions/GlobalExceptionHandler.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VehicleImage.Api.DTOs.Images;

namespace VehicleImage.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = 499;
            }
            return true;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        ImageFailureResponse response = exception switch
        {
            ImageIngestionException ingestionEx => MapIngestionException(ingestionEx, traceId),
            ResourceNotFoundException notFoundEx => new ImageFailureResponse(
                Code: notFoundEx.Code,
                Title: "Resource Not Found",
                Detail: notFoundEx.Message,
                Status: StatusCodes.Status404NotFound,
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null),
            ConcurrentUpdateException concurrentEx => new ImageFailureResponse(
                Code: concurrentEx.Code,
                Title: "Concurrent Modification",
                Detail: concurrentEx.Message,
                Status: StatusCodes.Status409Conflict,
                TraceId: traceId,
                Retryable: true,
                RetryAfterSeconds: null),
            ValidationException validationEx => new ImageFailureResponse(
                Code: ErrorCodes.ValidationFailed,
                Title: "Validation Failed",
                Detail: validationEx.Message,
                Status: StatusCodes.Status400BadRequest,
                StatusType: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null),
            _ => new ImageFailureResponse(
                Code: ErrorCodes.InternalError,
                Title: "Internal Server Error",
                Detail: "An unexpected internal error occurred while processing the request.",
                Status: StatusCodes.Status500InternalServerError,
                StatusType: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null)
        };

        if (response.Status >= 500)
        {
            _logger.LogError(exception, "Unhandled or infrastructure failure [{Code}] TraceId={TraceId}", response.Code, traceId);
        }
        else
        {
            _logger.LogWarning("Request failed with [{Code}] ({Status}) TraceId={TraceId}: {Detail}",
                response.Code, response.Status, traceId, response.Detail);
        }

        httpContext.Response.StatusCode = response.Status;
        httpContext.Response.ContentType = "application/problem+json";

        if (response.RetryAfterSeconds is > 0)
        {
            httpContext.Response.Headers.RetryAfter = response.RetryAfterSeconds.Value.ToString();
        }

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static ImageFailureResponse MapIngestionException(ImageIngestionException ex, string traceId)
    {
        int? retryAfterSeconds = ex.RetryAfter.HasValue
            ? Math.Max(1, (int)Math.Ceiling(ex.RetryAfter.Value.TotalSeconds))
            : null;

        string title = ex.HttpStatusCode >= 500
            ? "Internal Processing Failure"
            : "Image Ingestion Failed";

        return new ImageFailureResponse(
            Code: ex.Code,
            Title: title,
            Detail: ex.Message,
            Status: ex.HttpStatusCode,
            TraceId: traceId,
            Retryable: ex.IsTransient,
            RetryAfterSeconds: retryAfterSeconds);
    }
}
```

### File: `backend/VehicleImage.Api/Exceptions/ImageIngestionException.cs`

```csharp
using Microsoft.AspNetCore.Http;

namespace VehicleImage.Api.Exceptions;

public sealed class ImageIngestionException : Exception
{
    public string Code { get; }
    public bool IsTransient { get; }
    public int? UpstreamStatusCode { get; }
    public TimeSpan? RetryAfter { get; }
    public int HttpStatusCode { get; }

    public ImageIngestionException(
        string code,
        string clientSafeMessage,
        bool isTransient = false,
        int? upstreamStatusCode = null,
        TimeSpan? retryAfter = null,
        int httpStatusCode = StatusCodes.Status422UnprocessableEntity,
        Exception? innerException = null)
        : base(clientSafeMessage, innerException)
    {
        Code = code;
        IsTransient = isTransient;
        UpstreamStatusCode = upstreamStatusCode;
        RetryAfter = retryAfter;
        HttpStatusCode = httpStatusCode;
    }

    public static ImageIngestionException InvalidUrl(string detail = "The supplied URL is malformed or does not meet policy requirements.")
        => new(ErrorCodes.InvalidUrl, detail, isTransient: false);

    public static ImageIngestionException UnsafeDestination(string detail = "The supplied URL points to a restricted or non-public destination.")
        => new(ErrorCodes.UnsafeDestination, detail, isTransient: false);

    public static ImageIngestionException DnsResolutionFailed(string detail = "The supplier hostname could not be resolved.", Exception? inner = null)
        => new(ErrorCodes.DnsResolutionFailed, detail, isTransient: true, innerException: inner);

    public static ImageIngestionException ConnectionTimeout(string detail = "Connecting to the supplier server timed out.", Exception? inner = null)
        => new(ErrorCodes.ConnectionTimeout, detail, isTransient: true, innerException: inner);

    public static ImageIngestionException RequestTimeout(string detail = "Reading the image from the supplier server timed out.", Exception? inner = null)
        => new(ErrorCodes.RequestTimeout, detail, isTransient: true, innerException: inner);

    public static ImageIngestionException TlsFailure(string detail = "TLS certificate or handshake verification with the supplier server failed.", Exception? inner = null)
        => new(ErrorCodes.TlsFailure, detail, isTransient: false, innerException: inner);

    public static ImageIngestionException RedirectLimitExceeded(int maxRedirects)
        => new(ErrorCodes.RedirectLimitExceeded, $"The URL exceeded the maximum of {maxRedirects} redirects.", isTransient: false);

    public static ImageIngestionException HttpNotFound()
        => new(ErrorCodes.HttpNotFound, "The supplier returned HTTP 404 (Not Found) for the image URL.", isTransient: false, upstreamStatusCode: 404);

    public static ImageIngestionException HttpUnauthorized()
        => new(ErrorCodes.HttpUnauthorized, "The supplier requires authentication (HTTP 401) to access this URL.", isTransient: false, upstreamStatusCode: 401);

    public static ImageIngestionException HttpForbidden()
        => new(ErrorCodes.HttpForbidden, "Access to the supplier URL was forbidden (HTTP 403).", isTransient: false, upstreamStatusCode: 403);

    public static ImageIngestionException HttpRateLimited(TimeSpan? retryAfter = null)
        => new(ErrorCodes.HttpRateLimited, "The supplier rate-limited the request (HTTP 429).", isTransient: true, upstreamStatusCode: 429, retryAfter: retryAfter);

    public static ImageIngestionException HttpServerError(int statusCode)
        => new(ErrorCodes.HttpServerError, $"The supplier server returned an upstream error (HTTP {statusCode}).", isTransient: true, upstreamStatusCode: statusCode);

    public static ImageIngestionException HttpError(int statusCode)
        => new(ErrorCodes.HttpError, $"The supplier returned an unexpected status code (HTTP {statusCode}).", isTransient: false, upstreamStatusCode: statusCode);

    public static ImageIngestionException UnsupportedContentType(string detail = "The retrieved resource is not an image.")
        => new(ErrorCodes.UnsupportedContentType, detail, isTransient: false);

    public static ImageIngestionException UnsupportedImageFormat(string formatName)
        => new(ErrorCodes.UnsupportedImageFormat, $"The image format '{formatName}' is not supported. Accepted formats are JPEG, PNG and WebP.", isTransient: false);

    public static ImageIngestionException ImageTooLarge(long maxBytes)
        => new(ErrorCodes.ImageTooLarge, $"The downloaded content exceeded the maximum allowed size of {maxBytes / (1024 * 1024)} MB.", isTransient: false);

    public static ImageIngestionException ImageDimensionsExceeded(int width, int height, int maxWidth, int maxHeight, long maxPixels)
        => new(ErrorCodes.ImageDimensionsExceeded, $"Image dimensions ({width}x{height}) exceed allowed limits (max {maxWidth}x{maxHeight}, {maxPixels:N0} pixels).", isTransient: false);

    public static ImageIngestionException ImageTooSmall(int width, int height, int minWidth, int minHeight)
        => new(ErrorCodes.ImageTooSmall, $"Image dimensions ({width}x{height}) are below the minimum required size of {minWidth}x{minHeight} pixels.", isTransient: false);

    public static ImageIngestionException ImageDecodeFailed(string detail = "The image bytes could not be decoded or are corrupted.", bool isTransient = false, Exception? inner = null)
        => new(ErrorCodes.ImageDecodeFailed, detail, isTransient: isTransient, innerException: inner);

    public static ImageIngestionException NoImageFound(string detail = "No usable vehicle image candidate could be found on the supplied HTML page.")
        => new(ErrorCodes.NoImageFound, detail, isTransient: false);

    public static ImageIngestionException StorageFailed(string detail = "Failed to persist or read the image in internal storage.", Exception? inner = null)
        => new(ErrorCodes.StorageFailed, detail, isTransient: false, httpStatusCode: StatusCodes.Status500InternalServerError, innerException: inner);

    public static ImageIngestionException DatabasePersistenceFailed(string detail = "Failed to persist vehicle and image metadata to the database.", Exception? inner = null)
        => new(ErrorCodes.DatabasePersistenceFailed, detail, isTransient: false, httpStatusCode: StatusCodes.Status500InternalServerError, innerException: inner);
}
```

### File: `backend/VehicleImage.Api/Exceptions/ResourceNotFoundException.cs`

```csharp
namespace VehicleImage.Api.Exceptions;

public sealed class ResourceNotFoundException : Exception
{
    public string Code { get; }

    public ResourceNotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public static ResourceNotFoundException Vehicle(Guid vehicleId)
        => new(ErrorCodes.VehicleNotFound, $"Vehicle '{vehicleId}' was not found.");

    public static ResourceNotFoundException Image(Guid imageId)
        => new(ErrorCodes.ImageNotFound, $"Image '{imageId}' was not found.");
}
```

### File: `backend/VehicleImage.Api/ImageProcessing/ImageMetadata.cs`

```csharp
namespace VehicleImage.Api.ImageProcessing;

public enum ImageFormatKind
{
    Jpeg,
    Png,
    WebP
}

public sealed record ImageMetadata(
    ImageFormatKind Format,
    string ContentType,
    string FileExtension,
    int Width,
    int Height,
    long FileSize,
    string Sha256
);
```

### File: `backend/VehicleImage.Api/ImageProcessing/ImageValidator.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.ImageProcessing;

public sealed class ImageValidator
{
    private readonly ImageValidationOptions _options;
    private readonly SixLabors.ImageSharp.Configuration _decoderConfiguration;

    public ImageValidator(IOptions<ImageValidationOptions> options)
    {
        _options = options.Value;
        _decoderConfiguration = SixLabors.ImageSharp.Configuration.Default.Clone();
        _decoderConfiguration.MaxDegreeOfParallelism = 1;
        _decoderConfiguration.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
        {
            MaximumPoolSizeMegabytes = 64,
            AllocationLimitMegabytes = 256
        });
    }

    public async Task<ImageMetadata> ValidateAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanSeek)
        {
            throw new ArgumentException("The image content stream must be seekable.", nameof(content));
        }

        if (content.Length == 0)
        {
            throw ImageIngestionException.UnsupportedContentType("The downloaded response body is empty.");
        }

        content.Position = 0;

        byte[] header = new byte[(int)Math.Min(512, content.Length)];
        int headerRead = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        ReadOnlySpan<byte> headerSpan = header.AsSpan(0, headerRead);

        // 1. Signature inspection (never trust Content-Type header or file extension)
        var (formatKind, contentType, extension) = InspectSignature(headerSpan);

        if (!_options.AllowedFormats.Any(f => string.Equals(f, formatKind.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            throw ImageIngestionException.UnsupportedImageFormat(formatKind.ToString().ToUpperInvariant());
        }

        // For JPEG, verify the stream is not truncated (ends with EOI marker FF D9)
        if (formatKind == ImageFormatKind.Jpeg && !HasValidJpegEndOfImage(content))
        {
            throw ImageIngestionException.ImageDecodeFailed(
                "The JPEG stream is truncated (missing End-Of-Image marker).",
                isTransient: true);
        }

        // For PNG, inspect IHDR width & height (bytes 16..23) immediately to catch dimension bombs before chunk decoding
        if (formatKind == ImageFormatKind.Png && headerSpan.Length >= 24)
        {
            int ihdrWidth = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(headerSpan.Slice(16, 4));
            int ihdrHeight = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(headerSpan.Slice(20, 4));
            if (ihdrWidth > 0 && ihdrHeight > 0)
            {
                long ihdrPixels = (long)ihdrWidth * ihdrHeight;
                if (ihdrWidth > _options.MaxWidth || ihdrHeight > _options.MaxHeight || ihdrPixels > _options.MaxPixels)
                {
                    throw ImageIngestionException.ImageDimensionsExceeded(
                        ihdrWidth, ihdrHeight, _options.MaxWidth, _options.MaxHeight, _options.MaxPixels);
                }
            }
        }

        // 2. Identify dimensions and frame count without decoding pixels
        content.Position = 0;
        ImageInfo imageInfo;
        try
        {
            var decoderOptions = new DecoderOptions
            {
                Configuration = _decoderConfiguration
            };
            imageInfo = await Image.IdentifyAsync(decoderOptions, content, cancellationToken)
                ?? throw ImageIngestionException.ImageDecodeFailed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageIngestionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw ImageIngestionException.ImageDecodeFailed(inner: ex);
        }

        if (imageInfo.FrameMetadataCollection.Count > 1)
        {
            throw ImageIngestionException.UnsupportedImageFormat("Animated");
        }

        int width = imageInfo.Width;
        int height = imageInfo.Height;
        long totalPixels = (long)width * height;

        // 3. Enforce dimension ceilings and decompression-bomb pixel ceiling before full decode
        if (width > _options.MaxWidth || height > _options.MaxHeight || totalPixels > _options.MaxPixels)
        {
            throw ImageIngestionException.ImageDimensionsExceeded(
                width, height, _options.MaxWidth, _options.MaxHeight, _options.MaxPixels);
        }

        if (width < _options.MinWidth || height < _options.MinHeight)
        {
            throw ImageIngestionException.ImageTooSmall(
                width, height, _options.MinWidth, _options.MinHeight);
        }

        // 4. Full pixel decode to detect structural corruption
        content.Position = 0;
        try
        {
            var decoderOptions = new DecoderOptions
            {
                Configuration = _decoderConfiguration,
                MaxFrames = 1
            };
            using var decoded = await Image.LoadAsync<Rgba32>(decoderOptions, content, cancellationToken);
            if (decoded.Width != width || decoded.Height != height)
            {
                throw ImageIngestionException.ImageDecodeFailed("Decoded dimensions did not match image header.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageIngestionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw ImageIngestionException.ImageDecodeFailed(
                "The image failed full pixel decoding due to corrupted data.",
                isTransient: true,
                inner: ex);
        }

        // 5. Compute SHA-256 hash and reset stream to position 0
        content.Position = 0;
        byte[] hashBytes = await SHA256.HashDataAsync(content, cancellationToken);
        string sha256 = Convert.ToHexStringLower(hashBytes);

        content.Position = 0;

        return new ImageMetadata(
            Format: formatKind,
            ContentType: contentType,
            FileExtension: extension,
            Width: width,
            Height: height,
            FileSize: content.Length,
            Sha256: sha256);
    }

    private static (ImageFormatKind Kind, string ContentType, string Extension) InspectSignature(ReadOnlySpan<byte> header)
    {
        // JPEG: FF D8 FF
        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return (ImageFormatKind.Jpeg, "image/jpeg", ".jpg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header.Length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return (ImageFormatKind.Png, "image/png", ".png");
        }

        // WebP: "RIFF" + 4 size bytes + "WEBP"
        if (header.Length >= 12 &&
            header[0] == (byte)'R' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'F' &&
            header[8] == (byte)'W' &&
            header[9] == (byte)'E' &&
            header[10] == (byte)'B' &&
            header[11] == (byte)'P')
        {
            return (ImageFormatKind.WebP, "image/webp", ".webp");
        }

        // Recognised but unsupported image formats -> UNSUPPORTED_IMAGE_FORMAT
        // GIF87a or GIF89a
        if (header.Length >= 6 &&
            header[0] == (byte)'G' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'8' &&
            (header[4] == (byte)'7' || header[4] == (byte)'9') &&
            header[5] == (byte)'a')
        {
            throw ImageIngestionException.UnsupportedImageFormat("GIF");
        }

        // BMP: "BM"
        if (header.Length >= 2 &&
            header[0] == (byte)'B' &&
            header[1] == (byte)'M')
        {
            throw ImageIngestionException.UnsupportedImageFormat("BMP");
        }

        // TIFF: "II*\0" or "MM\0*"
        if (header.Length >= 4 &&
            ((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00) ||
             (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A)))
        {
            throw ImageIngestionException.UnsupportedImageFormat("TIFF");
        }

        // ICO: 00 00 01 00
        if (header.Length >= 4 &&
            header[0] == 0x00 &&
            header[1] == 0x00 &&
            header[2] == 0x01 &&
            header[3] == 0x00)
        {
            throw ImageIngestionException.UnsupportedImageFormat("ICO");
        }

        // ISO BMFF (AVIF / HEIC): bytes 4..7 == "ftyp"
        if (header.Length >= 12 &&
            header[4] == (byte)'f' &&
            header[5] == (byte)'t' &&
            header[6] == (byte)'y' &&
            header[7] == (byte)'p')
        {
            throw ImageIngestionException.UnsupportedImageFormat("AVIF/HEIF");
        }

        // Check for HTML, XML, SVG, or JSON text content -> UNSUPPORTED_CONTENT_TYPE
        string textSample = Encoding.UTF8.GetString(header).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (textSample.StartsWith('<') ||
            textSample.Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
            textSample.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            textSample.StartsWith('{') ||
            textSample.StartsWith('['))
        {
            throw ImageIngestionException.UnsupportedContentType(
                "The retrieved resource is HTML, XML, SVG or JSON rather than a binary image.");
        }

        throw ImageIngestionException.UnsupportedContentType();
    }

    private static bool HasValidJpegEndOfImage(Stream content)
    {
        if (content.Length < 4)
        {
            return false;
        }

        int tailLength = (int)Math.Min(16, content.Length);
        Span<byte> tail = stackalloc byte[tailLength];
        content.Position = content.Length - tailLength;
        int read = content.Read(tail);
        tail = tail[..read];

        for (int i = tail.Length - 2; i >= 0; i--)
        {
            if (tail[i] == 0xFF && tail[i + 1] == 0xD9)
            {
                return true;
            }
        }

        return false;
    }
}
```

### File: `backend/VehicleImage.Api/Migrations/20261116094554_InitialCreate.cs`

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleImage.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Make = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.CheckConstraint("CK_Vehicles_Year", "[Year] >= 1886 AND [Year] <= 2100");
                });

            migrationBuilder.CreateTable(
                name: "VehicleImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    FinalSourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RetrievalStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleImages", x => x.Id);
                    table.CheckConstraint("CK_VehicleImages_FileSize", "[FileSize] > 0");
                    table.CheckConstraint("CK_VehicleImages_Height", "[Height] > 0");
                    table.CheckConstraint("CK_VehicleImages_Width", "[Width] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleImages_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleImages_StorageKey",
                table: "VehicleImages",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleImages_VehicleId",
                table: "VehicleImages",
                column: "VehicleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleImages");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
```

### File: `backend/VehicleImage.Api/Migrations/20261116094554_InitialCreate.Designer.cs`

```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VehicleImage.Api.Data;

#nullable disable

namespace VehicleImage.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261116094554_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("VehicleImage.Api.Entities.Vehicle", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("Make")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Model")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("Variant")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Vehicles", null, t =>
                        {
                            t.HasCheckConstraint("CK_Vehicles_Year", "[Year] >= 1886 AND [Year] <= 2100");
                        });
                });

            modelBuilder.Entity("VehicleImage.Api.Entities.VehicleImage", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("FailureReason")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("FileSize")
                        .HasColumnType("bigint");

                    b.Property<string>("FinalSourceUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<string>("Format")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("Height")
                        .HasColumnType("int");

                    b.Property<string>("RetrievalStrategy")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Sha256")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nchar(64)")
                        .IsFixedLength();

                    b.Property<string>("SourceUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("StorageKey")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<Guid>("VehicleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Width")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("StorageKey")
                        .IsUnique();

                    b.HasIndex("VehicleId")
                        .IsUnique();

                    b.ToTable("VehicleImages", null, t =>
                        {
                            t.HasCheckConstraint("CK_VehicleImages_FileSize", "[FileSize] > 0");

                            t.HasCheckConstraint("CK_VehicleImages_Height", "[Height] > 0");

                            t.HasCheckConstraint("CK_VehicleImages_Width", "[Width] > 0");
                        });
                });

            modelBuilder.Entity("VehicleImage.Api.Entities.VehicleImage", b =>
                {
                    b.HasOne("VehicleImage.Api.Entities.Vehicle", "Vehicle")
                        .WithOne("Image")
                        .HasForeignKey("VehicleImage.Api.Entities.VehicleImage", "VehicleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Vehicle");
                });

            modelBuilder.Entity("VehicleImage.Api.Entities.Vehicle", b =>
                {
                    b.Navigation("Image");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### File: `backend/VehicleImage.Api/Migrations/ApplicationDbContextModelSnapshot.cs`

```csharp
// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VehicleImage.Api.Data;

#nullable disable

namespace VehicleImage.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("VehicleImage.Api.Entities.Vehicle", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("Make")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Model")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("Variant")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Vehicles", null, t =>
                        {
                            t.HasCheckConstraint("CK_Vehicles_Year", "[Year] >= 1886 AND [Year] <= 2100");
                        });
                });

            modelBuilder.Entity("VehicleImage.Api.Entities.VehicleImage", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<string>("FailureReason")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("FileSize")
                        .HasColumnType("bigint");

                    b.Property<string>("FinalSourceUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<string>("Format")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("Height")
                        .HasColumnType("int");

                    b.Property<string>("RetrievalStrategy")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Sha256")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nchar(64)")
                        .IsFixedLength();

                    b.Property<string>("SourceUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("nvarchar(2048)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("StorageKey")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("SYSUTCDATETIME()");

                    b.Property<Guid>("VehicleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Width")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("StorageKey")
                        .IsUnique();

                    b.HasIndex("VehicleId")
                        .IsUnique();

                    b.ToTable("VehicleImages", null, t =>
                        {
                            t.HasCheckConstraint("CK_VehicleImages_FileSize", "[FileSize] > 0");

                            t.HasCheckConstraint("CK_VehicleImages_Height", "[Height] > 0");

                            t.HasCheckConstraint("CK_VehicleImages_Width", "[Width] > 0");
                        });
                });

            modelBuilder.Entity("VehicleImage.Api.Entities.VehicleImage", b =>
                {
                    b.HasOne("VehicleImage.Api.Entities.Vehicle", "Vehicle")
                        .WithOne("Image")
                        .HasForeignKey("VehicleImage.Api.Entities.VehicleImage", "VehicleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Vehicle");
                });

            modelBuilder.Entity("VehicleImage.Api.Entities.Vehicle", b =>
                {
                    b.Navigation("Image");
                });
#pragma warning restore 612, 618
        }
    }
}
```

### File: `backend/VehicleImage.Api/Models/IngestedImage.cs`

```csharp
namespace VehicleImage.Api.Models;

public sealed record IngestedImage(
    Guid ImageId,
    string StorageKey,
    string ContentType,
    string Format,
    long FileSize,
    int Width,
    int Height,
    string Sha256,
    string SourceUrl,
    string FinalSourceUrl,
    string RetrievalStrategy
);
```

### File: `backend/VehicleImage.Api/Models/RetrievedImage.cs`

```csharp
namespace VehicleImage.Api.Models;

public sealed class RetrievedImage : IDisposable, IAsyncDisposable
{
    public required Stream Content { get; init; }
    public string? ContentType { get; init; }
    public required Uri SourceUrl { get; init; }
    public required Uri FinalSourceUrl { get; init; }
    public required long FileSize { get; init; }
    public required string Strategy { get; init; }

    public void Dispose()
    {
        Content.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Content.DisposeAsync();
    }
}
```

### File: `backend/VehicleImage.Api/Program.cs`

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Data;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Retrieval;
using VehicleImage.Api.Security;
using VehicleImage.Api.Services;
using VehicleImage.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Bind and validate configuration options on startup
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .PostConfigure(options =>
    {
        if (!string.IsNullOrWhiteSpace(options.RootPath) && !Path.IsPathRooted(options.RootPath))
        {
            options.RootPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, options.RootPath));
        }
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RetrievalOptions>()
    .Bind(builder.Configuration.GetSection(RetrievalOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ImageValidationOptions>()
    .Bind(builder.Configuration.GetSection(ImageValidationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<HtmlExtractionOptions>()
    .Bind(builder.Configuration.GetSection(HtmlExtractionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RetryOptions>()
    .Bind(builder.Configuration.GetSection(RetryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 2. Database registration (SQL Server primary; supports Sqlite provider when configured for local non-Windows/test runs)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// 3. Controllers, Validation error shaping (400 VALIDATION_FAILED), ProblemDetails, and GlobalExceptionHandler
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            string detail = string.Join("; ", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m)));

            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "One or more request validation errors occurred.";
            }

            string traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var failure = new ImageFailureResponse(
                Code: ErrorCodes.ValidationFailed,
                Title: "Validation Failed",
                Detail: detail,
                Status: StatusCodes.Status400BadRequest,
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null,
                StatusType: "https://tools.ietf.org/html/rfc9110#section-15.5.1");

            return new BadRequestObjectResult(failure)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 4. Security singletons and SSRF-safe HttpClient
builder.Services.AddSingleton<DestinationValidator>();
builder.Services.AddSingleton<UrlValidator>();
builder.Services.AddSingleton<ImageValidator>();
builder.Services.AddSingleton<IImageStorage, LocalFileStorage>();

builder.Services.AddHttpClient(SsrfGuardHttpHandler.ClientName, client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan; // Enforced per-request by DirectImageRetriever
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var destinationValidator = sp.GetRequiredService<DestinationValidator>();
    var retrievalOptions = sp.GetRequiredService<IOptions<RetrievalOptions>>().Value;
    return SsrfGuardHttpHandler.Create(destinationValidator, retrievalOptions);
});

// 5. Retrieval and application services
builder.Services.AddScoped<DirectImageRetriever>();
builder.Services.AddScoped<HtmlImageRetriever>();
builder.Services.AddScoped<RetrievalOrchestrator>();
builder.Services.AddScoped<ImageIngestionService>();
builder.Services.AddScoped<IImageIngestionService>(sp => sp.GetRequiredService<ImageIngestionService>());
builder.Services.AddScoped<ImageDeliveryService>();
builder.Services.AddScoped<IImageDeliveryService>(sp => sp.GetRequiredService<ImageDeliveryService>());
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<IVehicleService>(sp => sp.GetRequiredService<VehicleService>());

// 6. CORS for React Vite dashboard and health checks
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. Startup storage root verification: outside web root, created, and writable
InitializeAndVerifyStorageRoot(app.Services, app.Environment);

if (string.Equals(builder.Configuration.GetValue<string>("DatabaseProvider"), "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("DashboardCors");
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static void InitializeAndVerifyStorageRoot(IServiceProvider services, IWebHostEnvironment env)
{
    var storageOptions = services.GetRequiredService<IOptions<StorageOptions>>().Value;
    string fullStorageRoot = Path.GetFullPath(storageOptions.RootPath);

    string webRoot = !string.IsNullOrWhiteSpace(env.WebRootPath)
        ? Path.GetFullPath(env.WebRootPath)
        : Path.GetFullPath(Path.Combine(env.ContentRootPath, "wwwroot"));

    StringComparison pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    string webRootWithSep = webRoot.EndsWith(Path.DirectorySeparatorChar)
        ? webRoot
        : webRoot + Path.DirectorySeparatorChar;

    if (string.Equals(fullStorageRoot, webRoot, pathComparison) ||
        fullStorageRoot.StartsWith(webRootWithSep, pathComparison))
    {
        throw new InvalidOperationException(
            $"Storage:RootPath ('{fullStorageRoot}') must be outside the web root ('{webRoot}').");
    }

    Directory.CreateDirectory(fullStorageRoot);
}

public partial class Program;
```

### File: `backend/VehicleImage.Api/Properties/launchSettings.json`

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7206;http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### File: `backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs`

```csharp
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

public sealed class DirectImageRetriever
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UrlValidator _urlValidator;
    private readonly DestinationValidator _destinationValidator;
    private readonly RetrievalOptions _retrievalOptions;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<DirectImageRetriever> _logger;

    public DirectImageRetriever(
        IHttpClientFactory httpClientFactory,
        UrlValidator urlValidator,
        DestinationValidator destinationValidator,
        IOptions<RetrievalOptions> retrievalOptions,
        IOptions<RetryOptions> retryOptions,
        ILogger<DirectImageRetriever> logger)
    {
        _httpClientFactory = httpClientFactory;
        _urlValidator = urlValidator;
        _destinationValidator = destinationValidator;
        _retrievalOptions = retrievalOptions.Value;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    internal async Task<(RetrievedImage Image, string? Charset)> FetchWithRetriesAsync(
        Uri source,
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        int maxAttempts = Math.Max(1, _retryOptions.MaxAttempts);
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                return await ExecuteSingleFetchAsync(source, strategyName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ImageIngestionException ex) when (ShouldRetry(ex, attempt, maxAttempts, out TimeSpan delay))
            {
                _logger.LogWarning(
                    "Transient failure [{Code}] on attempt {Attempt}/{MaxAttempts} for {Host}. Retrying in {DelayMs}ms.",
                    ex.Code, attempt, maxAttempts, source.Host, (int)delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private bool ShouldRetry(ImageIngestionException ex, int attempt, int maxAttempts, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (!ex.IsTransient || attempt >= maxAttempts)
        {
            return false;
        }

        // Permanent errors are never retried; only transient codes are eligible
        if (ex.Code is ErrorCodes.InvalidUrl
            or ErrorCodes.UnsafeDestination
            or ErrorCodes.RedirectLimitExceeded
            or ErrorCodes.HttpNotFound
            or ErrorCodes.HttpUnauthorized
            or ErrorCodes.HttpForbidden
            or ErrorCodes.UnsupportedContentType
            or ErrorCodes.UnsupportedImageFormat
            or ErrorCodes.ImageTooLarge
            or ErrorCodes.ImageDimensionsExceeded
            or ErrorCodes.ImageTooSmall
            or ErrorCodes.TlsFailure)
        {
            return false;
        }

        if (ex.Code == ErrorCodes.HttpRateLimited)
        {
            if (ex.RetryAfter.HasValue)
            {
                if (ex.RetryAfter.Value.TotalSeconds > _retryOptions.MaxRetryAfterSeconds)
                {
                    return false;
                }

                delay = ex.RetryAfter.Value;
                return true;
            }
        }

        double exponentialMs = _retryOptions.BaseDelayMs * Math.Pow(2, attempt - 1);
        double cappedMs = Math.Min(exponentialMs, _retryOptions.MaxDelayMs);

        if (_retryOptions.EnableJitter)
        {
            double jitterFactor = 0.75 + (Random.Shared.NextDouble() * 0.5);
            cappedMs *= jitterFactor;
        }

        delay = TimeSpan.FromMilliseconds(Math.Max(10, cappedMs));
        return true;
    }

    private async Task<(RetrievedImage Image, string? Charset)> ExecuteSingleFetchAsync(
        Uri source,
        string strategyName,
        CancellationToken cancellationToken)
    {
        Uri currentUri = _urlValidator.Validate(source.OriginalString);
        HttpClient client = _httpClientFactory.CreateClient(SsrfGuardHttpHandler.ClientName);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_retrievalOptions.TimeoutSeconds));
        CancellationToken effectiveToken = timeoutCts.Token;

        for (int hop = 0; hop <= _retrievalOptions.MaxRedirects; hop++)
        {
            await _destinationValidator.ResolveAndValidateAsync(currentUri.DnsSafeHost, effectiveToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _retrievalOptions.UserAgent);
            request.Headers.TryAddWithoutValidation(
                "Accept",
                "image/webp,image/png,image/jpeg,image/*;q=0.9,text/html,application/xhtml+xml;q=0.8,*/*;q=0.5");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    effectiveToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw ImageIngestionException.RequestTimeout(inner: ex);
            }
            catch (ImageIngestionException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                throw MapHttpRequestException(ex);
            }
            catch (AuthenticationException ex)
            {
                throw ImageIngestionException.TlsFailure(inner: ex);
            }

            using (response)
            {
                if (IsRedirectStatusCode(response.StatusCode))
                {
                    if (hop >= _retrievalOptions.MaxRedirects)
                    {
                        throw ImageIngestionException.RedirectLimitExceeded(_retrievalOptions.MaxRedirects);
                    }

                    string? location = response.Headers.Location?.OriginalString;
                    currentUri = _urlValidator.ValidateRedirectTarget(currentUri, location);
                    continue;
                }

                EnsureSuccessOrThrowMappedStatus(response);

                string? mediaType = response.Content.Headers.ContentType?.MediaType;
                string? charSet = response.Content.Headers.ContentType?.CharSet;

                MemoryStream buffer = await ReadBoundedStreamAsync(
                    response.Content,
                    _retrievalOptions.MaxDownloadBytes,
                    effectiveToken,
                    cancellationToken);

                var retrieved = new RetrievedImage
                {
                    Content = buffer,
                    ContentType = mediaType,
                    SourceUrl = source,
                    FinalSourceUrl = currentUri,
                    FileSize = buffer.Length,
                    Strategy = strategyName
                };

                return (retrieved, charSet);
            }
        }

        throw ImageIngestionException.RedirectLimitExceeded(_retrievalOptions.MaxRedirects);
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.MovedPermanently // 301
            or HttpStatusCode.Found                          // 302
            or HttpStatusCode.SeeOther                       // 303
            or HttpStatusCode.TemporaryRedirect              // 307
            or HttpStatusCode.PermanentRedirect;             // 308
    }

    private static void EnsureSuccessOrThrowMappedStatus(HttpResponseMessage response)
    {
        int code = (int)response.StatusCode;
        if (code >= 200 && code <= 299)
        {
            return;
        }

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw ImageIngestionException.HttpNotFound();

            case HttpStatusCode.Unauthorized:
                throw ImageIngestionException.HttpUnauthorized();

            case HttpStatusCode.Forbidden:
                throw ImageIngestionException.HttpForbidden();

            case HttpStatusCode.TooManyRequests:
                TimeSpan? retryAfter = null;
                if (response.Headers.RetryAfter?.Delta is { } delta)
                {
                    retryAfter = delta > TimeSpan.Zero ? delta : TimeSpan.FromSeconds(1);
                }
                else if (response.Headers.RetryAfter?.Date is { } date)
                {
                    var diff = date - DateTimeOffset.UtcNow;
                    if (diff > TimeSpan.Zero)
                    {
                        retryAfter = diff;
                    }
                }
                throw ImageIngestionException.HttpRateLimited(retryAfter);

            default:
                if (code >= 500 && code <= 599)
                {
                    throw ImageIngestionException.HttpServerError(code);
                }
                throw ImageIngestionException.HttpError(code);
        }
    }

    private static async Task<MemoryStream> ReadBoundedStreamAsync(
        HttpContent content,
        long maxBytes,
        CancellationToken effectiveToken,
        CancellationToken callerToken)
    {
        var memoryStream = new MemoryStream();
        try
        {
            await using Stream networkStream = await content.ReadAsStreamAsync(effectiveToken);
            byte[] chunk = new byte[16 * 1024];
            long totalRead = 0;

            while (true)
            {
                int bytesRead = await networkStream.ReadAsync(chunk.AsMemory(0, chunk.Length), effectiveToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
                if (totalRead > maxBytes)
                {
                    throw ImageIngestionException.ImageTooLarge(maxBytes);
                }

                memoryStream.Write(chunk, 0, bytesRead);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            memoryStream.Dispose();
            throw;
        }
        catch (OperationCanceledException ex)
        {
            memoryStream.Dispose();
            throw ImageIngestionException.RequestTimeout(inner: ex);
        }
        catch (ImageIngestionException)
        {
            memoryStream.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            memoryStream.Dispose();
            throw ImageIngestionException.ImageDecodeFailed(
                "The image transfer was interrupted before completion.",
                isTransient: true,
                inner: ex);
        }
    }

    private static ImageIngestionException MapHttpRequestException(HttpRequestException ex)
    {
        if (ex.InnerException is ImageIngestionException ingestionEx)
        {
            return ingestionEx;
        }

        if (ex.InnerException is AuthenticationException ||
            ex.HttpRequestError == HttpRequestError.SecureConnectionError)
        {
            return ImageIngestionException.TlsFailure(inner: ex);
        }

        if (ex.HttpRequestError == HttpRequestError.NameResolutionError ||
            ex.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain })
        {
            return ImageIngestionException.DnsResolutionFailed(inner: ex);
        }

        if (ex.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut } ||
            ex.InnerException is TimeoutException)
        {
            return ImageIngestionException.ConnectionTimeout(inner: ex);
        }

        return ImageIngestionException.ConnectionTimeout(
            "Unable to connect to the supplier server.",
            inner: ex);
    }
}
```

### File: `backend/VehicleImage.Api/Retrieval/HtmlImageRetriever.cs`

```csharp
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

public enum CandidateSourceKind
{
    OgImage,
    TwitterImage,
    Srcset,
    DataSrc,
    Img
}

public sealed record ImageCandidate(
    Uri Url,
    CandidateSourceKind Source,
    int? DeclaredWidth,
    int? DeclaredHeight,
    int Score
);

public sealed partial class HtmlImageRetriever
{
    private static readonly string[] VehicleKeywords =
    [
        "vehicle", "car", "auto", "exterior", "hero", "gallery",
        "main", "primary", "listing", "photo", "sedan", "suv", "truck", "coupe"
    ];

    private static readonly string[] BoilerplateContainerMarkers =
    [
        "header", "footer", "nav", "navbar", "sidebar", "menu",
        "cookie", "modal", "banner", "ad", "ads", "sponsor", "related"
    ];

    private static readonly string[] HtmlErrorPhrases =
    [
        "404 not found",
        "page not found",
        "error 404",
        "403 forbidden",
        "access denied",
        "500 internal server error",
        "service unavailable",
        "bad gateway"
    ];

    private readonly DirectImageRetriever _directRetriever;
    private readonly UrlValidator _urlValidator;
    private readonly HtmlExtractionOptions _options;
    private readonly ILogger<HtmlImageRetriever> _logger;

    static HtmlImageRetriever()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public HtmlImageRetriever(
        DirectImageRetriever directRetriever,
        UrlValidator urlValidator,
        IOptions<HtmlExtractionOptions> options,
        ILogger<HtmlImageRetriever> logger)
    {
        _directRetriever = directRetriever;
        _urlValidator = urlValidator;
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<RetrievedImage> RetrieveCandidatesFromHtmlAsync(
        string html,
        Uri originalSourceUrl,
        Uri finalPageUrl,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ImageCandidate> rankedCandidates = ExtractAndRankCandidates(html, finalPageUrl);
        if (rankedCandidates.Count == 0)
        {
            throw ImageIngestionException.NoImageFound();
        }

        int maxToTry = Math.Max(1, _options.MaxCandidates);
        int attempted = 0;
        ImageIngestionException? lastCandidateFailure = null;

        foreach (var candidate in rankedCandidates)
        {
            if (attempted >= maxToTry)
            {
                break;
            }

            attempted++;
            RetrievedImage? retrieved = null;
            try
            {
                var (fetched, _) = await _directRetriever.FetchWithRetriesAsync(
                    candidate.Url,
                    "Html",
                    cancellationToken);

                retrieved = new RetrievedImage
                {
                    Content = fetched.Content,
                    ContentType = fetched.ContentType,
                    SourceUrl = originalSourceUrl,
                    FinalSourceUrl = fetched.FinalSourceUrl,
                    FileSize = fetched.FileSize,
                    Strategy = "Html"
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ImageIngestionException ex)
            {
                lastCandidateFailure = ex;
                _logger.LogInformation(
                    "HTML candidate {Index} ({CandidateUrl}) failed retrieval with [{Code}]; trying next candidate if available.",
                    attempted, candidate.Url, ex.Code);
            }

            if (retrieved is not null)
            {
                yield return retrieved;
            }
        }

        if (lastCandidateFailure is not null)
        {
            throw lastCandidateFailure;
        }

        throw ImageIngestionException.NoImageFound();
    }

    public IReadOnlyList<ImageCandidate> ExtractAndRankCandidates(string html, Uri pageUrl)
    {
        var rawCandidates = ExtractCandidates(html, pageUrl);
        return Rank(rawCandidates);
    }

    public List<ImageCandidate> ExtractCandidates(string html, Uri pageUrl)
    {
        ArgumentNullException.ThrowIfNull(pageUrl);

        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var doc = new HtmlDocument();
        string normalizedHtml = EnsureClosedTitleTag(html);
        doc.LoadHtml(normalizedHtml);

        if (IsHtmlErrorPage(doc))
        {
            return [];
        }

        Uri baseUri = ResolveBaseUri(doc, pageUrl);
        var candidates = new List<ImageCandidate>();

        // 1. Meta tags: og:image and twitter:image
        var metaNodes = doc.DocumentNode.SelectNodes("//meta");
        if (metaNodes is not null)
        {
            foreach (var meta in metaNodes)
            {
                string prop = (meta.GetAttributeValue("property", string.Empty) + " " +
                               meta.GetAttributeValue("name", string.Empty)).Trim().ToLowerInvariant();
                string? content = WebUtility.HtmlDecode(meta.GetAttributeValue("content", null!));

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                if (prop.Contains("og:image", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:width", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:height", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:type", StringComparison.Ordinal) &&
                    !prop.Contains("og:image:alt", StringComparison.Ordinal))
                {
                    TryAddCandidate(candidates, baseUri, content, CandidateSourceKind.OgImage, null, null, meta);
                }
                else if (prop.Contains("twitter:image", StringComparison.Ordinal) &&
                         !prop.Contains("twitter:image:alt", StringComparison.Ordinal))
                {
                    TryAddCandidate(candidates, baseUri, content, CandidateSourceKind.TwitterImage, null, null, meta);
                }
            }
        }

        // 2. <img> elements: srcset, data-src / data-original, and src
        var imgNodes = doc.DocumentNode.SelectNodes("//img");
        if (imgNodes is not null)
        {
            foreach (var img in imgNodes)
            {
                int? declaredW = ParseDimension(img.GetAttributeValue("width", null!));
                int? declaredH = ParseDimension(img.GetAttributeValue("height", null!));

                if ((declaredW.HasValue && declaredW.Value > 0 && declaredW.Value < _options.MinDeclaredDimension) ||
                    (declaredH.HasValue && declaredH.Value > 0 && declaredH.Value < _options.MinDeclaredDimension))
                {
                    continue;
                }

                // Check srcset or data-srcset for the largest descriptor
                string? srcset = WebUtility.HtmlDecode(
                    img.GetAttributeValue("srcset", null!) ?? img.GetAttributeValue("data-srcset", null!));
                if (!string.IsNullOrWhiteSpace(srcset))
                {
                    var (bestSrcsetUrl, srcsetWidth) = PickLargestSrcsetEntry(srcset);
                    if (!string.IsNullOrWhiteSpace(bestSrcsetUrl))
                    {
                        TryAddCandidate(
                            candidates,
                            baseUri,
                            bestSrcsetUrl,
                            CandidateSourceKind.Srcset,
                            srcsetWidth ?? declaredW,
                            declaredH,
                            img);
                    }
                }

                // Check lazy-loading attributes: data-src, data-original, data-lazy-src
                string? dataSrc = WebUtility.HtmlDecode(
                    img.GetAttributeValue("data-src", null!) ??
                    img.GetAttributeValue("data-original", null!) ??
                    img.GetAttributeValue("data-lazy-src", null!));
                if (!string.IsNullOrWhiteSpace(dataSrc))
                {
                    TryAddCandidate(candidates, baseUri, dataSrc, CandidateSourceKind.DataSrc, declaredW, declaredH, img);
                }

                // Standard src attribute
                string? src = WebUtility.HtmlDecode(img.GetAttributeValue("src", null!));
                if (!string.IsNullOrWhiteSpace(src))
                {
                    TryAddCandidate(candidates, baseUri, src, CandidateSourceKind.Img, declaredW, declaredH, img);
                }
            }
        }

        return candidates;
    }

    public IReadOnlyList<ImageCandidate> Rank(IEnumerable<ImageCandidate> candidates)
    {
        return candidates
            .GroupBy(c => c.Url.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(c => c.Score).First())
            .OrderByDescending(c => c.Score)
            .ToList();
    }

    public static string DecodeHtml(Stream content, string? headerCharset)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        byte[] bytes;
        if (content is MemoryStream ms)
        {
            bytes = ms.ToArray();
        }
        else
        {
            using var copy = new MemoryStream();
            content.CopyTo(copy);
            bytes = copy.ToArray();
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        string? detectedCharset = CleanCharset(headerCharset);
        if (string.IsNullOrWhiteSpace(detectedCharset))
        {
            // Inspect first 2048 bytes as ASCII to check <meta charset="..."> or content="text/html; charset=..."
            int sampleLen = Math.Min(bytes.Length, 2048);
            string asciiPrefix = Encoding.ASCII.GetString(bytes, 0, sampleLen);
            Match match = MetaCharsetRegex().Match(asciiPrefix);
            if (match.Success)
            {
                detectedCharset = CleanCharset(match.Groups[1].Value);
            }
        }

        Encoding encoding = Encoding.UTF8;
        if (!string.IsNullOrWhiteSpace(detectedCharset))
        {
            try
            {
                encoding = Encoding.GetEncoding(detectedCharset);
            }
            catch (ArgumentException)
            {
                encoding = Encoding.UTF8;
            }
        }

        return encoding.GetString(bytes);
    }

    private void TryAddCandidate(
        List<ImageCandidate> candidates,
        Uri baseUri,
        string rawUrl,
        CandidateSourceKind source,
        int? declaredWidth,
        int? declaredHeight,
        HtmlNode node)
    {
        string trimmed = rawUrl.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith('#'))
        {
            return;
        }

        Uri resolvedUri;
        try
        {
            resolvedUri = _urlValidator.ValidateRedirectTarget(baseUri, trimmed);
        }
        catch (ImageIngestionException)
        {
            return;
        }

        string pathAndQuery = resolvedUri.PathAndQuery.ToLowerInvariant();
        if (pathAndQuery.EndsWith(".svg", StringComparison.Ordinal) ||
            pathAndQuery.Contains(".svg?", StringComparison.Ordinal))
        {
            return;
        }

        if (MatchesIgnoredPattern(resolvedUri, node))
        {
            return;
        }

        int score = CalculateScore(resolvedUri, source, declaredWidth, declaredHeight, node);
        if (score <= 0)
        {
            return;
        }

        candidates.Add(new ImageCandidate(
            Url: resolvedUri,
            Source: source,
            DeclaredWidth: declaredWidth,
            DeclaredHeight: declaredHeight,
            Score: score));
    }

    private bool MatchesIgnoredPattern(Uri uri, HtmlNode node)
    {
        string pathLower = uri.AbsolutePath.ToLowerInvariant();
        string nodeContext = string.Concat(
            node.GetAttributeValue("class", string.Empty), " ",
            node.GetAttributeValue("id", string.Empty), " ",
            node.GetAttributeValue("alt", string.Empty)).ToLowerInvariant();

        foreach (string pattern in _options.IgnoreUrlPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            string pat = pattern.ToLowerInvariant();
            if (pathLower.Contains(pat, StringComparison.Ordinal) ||
                nodeContext.Contains(pat, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private int CalculateScore(
        Uri uri,
        CandidateSourceKind source,
        int? declaredWidth,
        int? declaredHeight,
        HtmlNode node)
    {
        int score = source switch
        {
            CandidateSourceKind.OgImage => _options.OgImageWeight,
            CandidateSourceKind.TwitterImage => _options.TwitterImageWeight,
            CandidateSourceKind.Srcset => _options.SrcsetWeight,
            CandidateSourceKind.DataSrc => _options.DataSrcWeight,
            CandidateSourceKind.Img => _options.ImgSrcWeight,
            _ => 50
        };

        if ((declaredWidth.HasValue && declaredWidth.Value >= 400) ||
            (declaredHeight.HasValue && declaredHeight.Value >= 300))
        {
            score += _options.LargeDimensionBonus;
        }

        string combinedSignals = string.Concat(
            uri.AbsolutePath, " ",
            node.GetAttributeValue("alt", string.Empty), " ",
            node.GetAttributeValue("class", string.Empty), " ",
            node.GetAttributeValue("id", string.Empty)).ToLowerInvariant();

        foreach (string keyword in VehicleKeywords)
        {
            if (combinedSignals.Contains(keyword, StringComparison.Ordinal))
            {
                score += _options.VehicleKeywordBonus;
                break;
            }
        }

        if (IsInsideBoilerplateContainer(node))
        {
            score -= _options.BoilerplateContainerPenalty;
        }

        return score;
    }

    private static bool IsInsideBoilerplateContainer(HtmlNode node)
    {
        for (HtmlNode? current = node.ParentNode; current is not null; current = current.ParentNode)
        {
            string tag = current.Name.ToLowerInvariant();
            if (tag is "header" or "nav" or "footer" or "aside")
            {
                return true;
            }

            string classAndId = string.Concat(
                current.GetAttributeValue("class", string.Empty), " ",
                current.GetAttributeValue("id", string.Empty), " ",
                current.GetAttributeValue("role", string.Empty)).ToLowerInvariant();

            foreach (string marker in BoilerplateContainerMarkers)
            {
                if (classAndId.Contains(marker, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsHtmlErrorPage(HtmlDocument doc)
    {
        string title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim().ToLowerInvariant() ?? string.Empty;
        string h1 = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim().ToLowerInvariant() ?? string.Empty;
        string combined = $"{title} {h1}";

        foreach (string phrase in HtmlErrorPhrases)
        {
            if (combined.Contains(phrase, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private Uri ResolveBaseUri(HtmlDocument doc, Uri pageUrl)
    {
        var baseNode = doc.DocumentNode.SelectSingleNode("//base[@href]");
        string? baseHref = baseNode?.GetAttributeValue("href", null!);
        if (!string.IsNullOrWhiteSpace(baseHref))
        {
            try
            {
                return _urlValidator.ValidateRedirectTarget(pageUrl, baseHref);
            }
            catch (ImageIngestionException)
            {
                return pageUrl;
            }
        }

        return pageUrl;
    }

    private static (string? Url, int? Width) PickLargestSrcsetEntry(string srcset)
    {
        string? bestUrl = null;
        double bestMetric = -1;
        int? bestWidth = null;

        string[] parts = srcset.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            string[] tokens = part.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                continue;
            }

            string url = tokens[0];
            double metric = 1.0;
            int? width = null;

            if (tokens.Length >= 2)
            {
                string descriptor = tokens[^1].ToLowerInvariant();
                if (descriptor.EndsWith('w') && int.TryParse(descriptor[..^1], out int w) && w > 0)
                {
                    metric = w;
                    width = w;
                }
                else if (descriptor.EndsWith('x') && double.TryParse(descriptor[..^1], System.Globalization.CultureInfo.InvariantCulture, out double x) && x > 0)
                {
                    metric = x * 1000.0;
                }
            }

            if (metric > bestMetric)
            {
                bestMetric = metric;
                bestUrl = url;
                bestWidth = width;
            }
        }

        return (bestUrl, bestWidth);
    }

    private static int? ParseDimension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string digits = value.Trim();
        if (digits.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            digits = digits[..^2].Trim();
        }

        return int.TryParse(digits, out int result) && result > 0 ? result : null;
    }

    private static string? CleanCharset(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().Trim('"', '\'');
    }

    private static string EnsureClosedTitleTag(string html)
    {
        int titleStart = html.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
        if (titleStart >= 0 &&
            html.IndexOf("</title>", titleStart, StringComparison.OrdinalIgnoreCase) < 0)
        {
            int nextTag = html.IndexOf('<', titleStart + 6);
            if (nextTag > titleStart)
            {
                return html.Insert(nextTag, "</title>");
            }
        }
        return html;
    }

    [GeneratedRegex(@"charset\s*=\s*[""']?([a-zA-Z0-9_\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MetaCharsetRegex();
}
```

### File: `backend/VehicleImage.Api/Retrieval/RetrievalOrchestrator.cs`

```csharp
using System.Runtime.CompilerServices;
using System.Text;
using VehicleImage.Api.Models;

namespace VehicleImage.Api.Retrieval;

public sealed class RetrievalOrchestrator
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".tif", ".ico", ".svg", ".avif", ".heic"
    ];

    private readonly DirectImageRetriever _directRetriever;
    private readonly HtmlImageRetriever _htmlRetriever;

    public RetrievalOrchestrator(
        DirectImageRetriever directRetriever,
        HtmlImageRetriever htmlRetriever)
    {
        _directRetriever = directRetriever;
        _htmlRetriever = htmlRetriever;
    }

    public async IAsyncEnumerable<RetrievedImage> RetrieveCandidatesAsync(
        Uri source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Single initial GET — no mandatory HEAD request (Decision 4 & Section 9.2)
        var (initialFetch, charset) = await _directRetriever.FetchWithRetriesAsync(
            source,
            "Direct",
            cancellationToken);

        bool transferredOwnership = false;
        try
        {
            if (IsHtmlResponse(initialFetch) && !HasExplicitImageExtension(initialFetch.FinalSourceUrl))
            {
                string html = HtmlImageRetriever.DecodeHtml(initialFetch.Content, charset);
                Uri finalPageUrl = initialFetch.FinalSourceUrl;
                initialFetch.Dispose();
                transferredOwnership = true;

                await foreach (var htmlCandidate in _htmlRetriever.RetrieveCandidatesFromHtmlAsync(
                    html,
                    source,
                    finalPageUrl,
                    cancellationToken))
                {
                    yield return htmlCandidate;
                }

                yield break;
            }

            transferredOwnership = true;
            yield return initialFetch;
        }
        finally
        {
            if (!transferredOwnership)
            {
                initialFetch.Dispose();
            }
        }
    }

    private static bool IsHtmlResponse(RetrievedImage retrieved)
    {
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        Span<byte> prefix = stackalloc byte[(int)Math.Min(512, retrieved.Content.Length)];
        int read = retrieved.Content.Read(prefix);
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        prefix = prefix[..read];

        // If magic bytes match binary image formats, it is NOT HTML regardless of Content-Type header
        if (StartsWithBinaryImageSignature(prefix))
        {
            return false;
        }

        // If magic bytes are SVG, let ImageValidator reject it as UNSUPPORTED_CONTENT_TYPE
        string sample = Encoding.UTF8.GetString(prefix).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (sample.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
            (sample.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
             sample.Contains("<svg", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        bool mediaTypeHtml = string.Equals(retrieved.ContentType, "text/html", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(retrieved.ContentType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

        bool bodyLooksLikeHtml = sample.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
                                 sample.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                                 sample.StartsWith("<head", StringComparison.OrdinalIgnoreCase) ||
                                 sample.StartsWith("<body", StringComparison.OrdinalIgnoreCase) ||
                                 sample.StartsWith("<meta", StringComparison.OrdinalIgnoreCase) ||
                                 sample.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
                                 sample.Contains("<img", StringComparison.OrdinalIgnoreCase) ||
                                 sample.Contains("og:image", StringComparison.OrdinalIgnoreCase) ||
                                 sample.Contains("twitter:image", StringComparison.OrdinalIgnoreCase);

        return mediaTypeHtml || bodyLooksLikeHtml;
    }

    private static bool StartsWithBinaryImageSignature(ReadOnlySpan<byte> header)
    {
        // JPEG
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return true;

        // PNG
        if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return true;

        // WebP
        if (header.Length >= 12 &&
            header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
            header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
            return true;

        // GIF
        if (header.Length >= 6 && header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F')
            return true;

        // BMP
        if (header.Length >= 2 && header[0] == (byte)'B' && header[1] == (byte)'M')
            return true;

        return false;
    }

    private static bool HasExplicitImageExtension(Uri uri)
    {
        string path = uri.AbsolutePath;
        foreach (string ext in ImageExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
```

### File: `backend/VehicleImage.Api/Security/DestinationValidator.cs`

```csharp
using System.Net;
using System.Net.Sockets;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Security;

public class DestinationValidator
{
    private static readonly IPNetwork2[] BlockedNetworks =
    [
        // IPv4 blocked ranges (Section 10.1)
        IPNetwork2.Parse("0.0.0.0", 8),          // "This" network
        IPNetwork2.Parse("10.0.0.0", 8),         // RFC 1918 private
        IPNetwork2.Parse("100.64.0.0", 10),      // Carrier-grade NAT (RFC 6598)
        IPNetwork2.Parse("127.0.0.0", 8),        // Loopback
        IPNetwork2.Parse("168.63.129.16", 32),   // Azure platform wire server
        IPNetwork2.Parse("169.254.0.0", 16),     // Link-local & cloud metadata (169.254.169.254)
        IPNetwork2.Parse("172.16.0.0", 12),      // RFC 1918 private
        IPNetwork2.Parse("192.0.0.0", 24),       // IETF protocol assignments
        IPNetwork2.Parse("192.0.2.0", 24),       // TEST-NET-1 documentation
        IPNetwork2.Parse("192.88.99.0", 24),     // 6to4 relay anycast
        IPNetwork2.Parse("192.168.0.0", 16),     // RFC 1918 private
        IPNetwork2.Parse("198.18.0.0", 15),      // Network benchmark tests
        IPNetwork2.Parse("198.51.100.0", 24),    // TEST-NET-2 documentation
        IPNetwork2.Parse("203.0.113.0", 24),    // TEST-NET-3 documentation
        IPNetwork2.Parse("224.0.0.0", 4),        // Multicast
        IPNetwork2.Parse("240.0.0.0", 4),        // Reserved for future use & broadcast (255.255.255.255)

        // IPv6 blocked ranges (Section 10.1)
        IPNetwork2.Parse("::", 128),             // Unspecified
        IPNetwork2.Parse("::1", 128),            // Loopback
        IPNetwork2.Parse("100::", 64),           // Discard-only (RFC 6666)
        IPNetwork2.Parse("2001::", 32),          // Teredo tunneling
        IPNetwork2.Parse("2001:2::", 48),        // Benchmarking
        IPNetwork2.Parse("2001:db8::", 32),      // Documentation
        IPNetwork2.Parse("fc00::", 7),           // Unique local address (ULA)
        IPNetwork2.Parse("fe80::", 10),          // Link-local
        IPNetwork2.Parse("ff00::", 8)            // Multicast
    ];

    private readonly Func<string, CancellationToken, ValueTask<IPAddress[]>> _dnsResolver;

    public DestinationValidator()
        : this( static (host, ct) => new ValueTask<IPAddress[]>(Dns.GetHostAddressesAsync(host, ct)) )
    {
    }

    public DestinationValidator(Func<string, CancellationToken, ValueTask<IPAddress[]>> dnsResolver)
    {
        _dnsResolver = dnsResolver ?? throw new ArgumentNullException(nameof(dnsResolver));
    }

    public virtual bool IsPortAllowed(int port, IReadOnlyCollection<int> configuredPorts)
    {
        return configuredPorts.Contains(port);
    }

    public virtual bool IsHostNameAllowed(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        string normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized == "localhost" ||
            normalized.EndsWith(".localhost", StringComparison.Ordinal) ||
            normalized.EndsWith(".local", StringComparison.Ordinal) ||
            normalized.EndsWith(".internal", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public virtual bool IsAllowed(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            return IsAllowed(address.MapToIPv4());
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] bytes = address.GetAddressBytes();

            // Check IPv4-compatible IPv6 (::w.x.y.z /96, excluding :: and ::1 which are caught below)
            bool isIpv4Compatible = true;
            for (int i = 0; i < 12; i++)
            {
                if (bytes[i] != 0)
                {
                    isIpv4Compatible = false;
                    break;
                }
            }
            if (isIpv4Compatible && (bytes[12] != 0 || bytes[13] != 0 || bytes[14] != 0 || bytes[15] > 1))
            {
                var embeddedV4 = new IPAddress(new ReadOnlySpan<byte>(bytes, 12, 4));
                return IsAllowed(embeddedV4);
            }

            // Check NAT64 well-known prefix 64:ff9b::/96 and 64:ff9b:1::/48 -> last 4 bytes are IPv4
            if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xff && bytes[3] == 0x9b)
            {
                var embeddedV4 = new IPAddress(new ReadOnlySpan<byte>(bytes, 12, 4));
                return IsAllowed(embeddedV4);
            }

            // Check 6to4 prefix 2002::/16 -> bytes 2..5 are the embedded IPv4 address
            if (bytes[0] == 0x20 && bytes[1] == 0x02)
            {
                var embeddedV4 = new IPAddress(new ReadOnlySpan<byte>(bytes, 2, 4));
                return IsAllowed(embeddedV4);
            }
        }

        foreach (var network in BlockedNetworks)
        {
            if (network.Contains(address))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<IReadOnlyList<IPAddress>> ResolveAndValidateAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        if (!IsHostNameAllowed(host))
        {
            throw ImageIngestionException.UnsafeDestination();
        }

        string cleanHost = host.Trim();
        if (cleanHost.StartsWith('[') && cleanHost.EndsWith(']'))
        {
            cleanHost = cleanHost[1..^1];
        }

        if (IPAddress.TryParse(cleanHost, out var literalAddress))
        {
            if (literalAddress.AddressFamily == AddressFamily.InterNetworkV6 && literalAddress.ScopeId != 0)
            {
                throw ImageIngestionException.UnsafeDestination();
            }

            if (!IsAllowed(literalAddress))
            {
                throw ImageIngestionException.UnsafeDestination();
            }

            return [literalAddress];
        }

        IPAddress[] addresses;
        try
        {
            addresses = await _dnsResolver(cleanHost, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            throw ImageIngestionException.DnsResolutionFailed(inner: ex);
        }

        if (addresses is null || addresses.Length == 0)
        {
            throw ImageIngestionException.DnsResolutionFailed();
        }

        // Every resolved address must be permitted (Section 10.1 / 19.12)
        foreach (var addr in addresses)
        {
            if (!IsAllowed(addr))
            {
                throw ImageIngestionException.UnsafeDestination();
            }
        }

        return addresses;
    }

    private readonly struct IPNetwork2
    {
        private readonly AddressFamily _family;
        private readonly byte[] _networkBytes;
        private readonly int _prefixLength;

        private IPNetwork2(AddressFamily family, byte[] networkBytes, int prefixLength)
        {
            _family = family;
            _networkBytes = networkBytes;
            _prefixLength = prefixLength;
        }

        public static IPNetwork2 Parse(string ipString, int prefixLength)
        {
            var ip = IPAddress.Parse(ipString);
            return new IPNetwork2(ip.AddressFamily, ip.GetAddressBytes(), prefixLength);
        }

        public bool Contains(IPAddress address)
        {
            if (address.AddressFamily != _family)
            {
                return false;
            }

            byte[] addrBytes = address.GetAddressBytes();
            int fullBytes = _prefixLength / 8;
            int remainingBits = _prefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
            {
                if (addrBytes[i] != _networkBytes[i])
                {
                    return false;
                }
            }

            if (remainingBits > 0)
            {
                byte mask = (byte)(0xFF << (8 - remainingBits));
                if ((addrBytes[fullBytes] & mask) != (_networkBytes[fullBytes] & mask))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
```

### File: `backend/VehicleImage.Api/Security/SsrfGuardHttpHandler.cs`

```csharp
using System.Net;
using System.Net.Sockets;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Security;

public static class SsrfGuardHttpHandler
{
    public const string ClientName = "SsrfSafeRetrievalClient";

    public static SocketsHttpHandler Create(DestinationValidator destinationValidator, RetrievalOptions options)
    {
        ArgumentNullException.ThrowIfNull(destinationValidator);
        ArgumentNullException.ThrowIfNull(options);

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds),
            PooledConnectionLifetime = TimeSpan.FromSeconds(options.PooledConnectionLifetimeSeconds),
            MaxResponseHeadersLength = 64,
            ConnectCallback = async (context, cancellationToken) =>
            {
                string host = context.DnsEndPoint.Host;
                int port = context.DnsEndPoint.Port;

                if (!destinationValidator.IsPortAllowed(port, options.AllowedPorts))
                {
                    throw ImageIngestionException.UnsafeDestination($"Port {port} is not permitted.");
                }

                IReadOnlyList<IPAddress> validatedAddresses =
                    await destinationValidator.ResolveAndValidateAsync(host, cancellationToken);

                Exception? lastSocketFailure = null;

                foreach (var address in validatedAddresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };

                    try
                    {
                        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        connectCts.CancelAfter(TimeSpan.FromSeconds(options.ConnectTimeoutSeconds));

                        await socket.ConnectAsync(new IPEndPoint(address, port), connectCts.Token);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        socket.Dispose();
                        lastSocketFailure = ImageIngestionException.ConnectionTimeout(inner: ex);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        socket.Dispose();
                        lastSocketFailure = ex;
                    }
                }

                if (lastSocketFailure is ImageIngestionException ingestionEx)
                {
                    throw ingestionEx;
                }

                throw ImageIngestionException.ConnectionTimeout(
                    "Unable to establish a TCP connection to the supplier server.",
                    lastSocketFailure);
            }
        };

        return handler;
    }
}
```

### File: `backend/VehicleImage.Api/Security/UrlValidator.cs`

```csharp
using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Security;

public sealed class UrlValidator
{
    private readonly RetrievalOptions _options;
    private readonly DestinationValidator _destinationValidator;
    private static readonly IdnMapping Idn = new();

    public UrlValidator(IOptions<RetrievalOptions> options, DestinationValidator destinationValidator)
    {
        _options = options.Value;
        _destinationValidator = destinationValidator;
    }

    public Uri Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw ImageIngestionException.InvalidUrl("The image URL is required.");
        }

        string trimmed = raw.Trim();
        if (trimmed.Length > _options.MaxUrlLength)
        {
            throw ImageIngestionException.InvalidUrl($"The URL exceeds the maximum length of {_options.MaxUrlLength} characters.");
        }

        foreach (char c in trimmed)
        {
            if (char.IsControl(c))
            {
                throw ImageIngestionException.InvalidUrl("The URL contains invalid control characters.");
            }
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw ImageIngestionException.InvalidUrl("The URL must be a valid absolute URI.");
        }

        return ValidateUriPolicy(uri);
    }

    public Uri ValidateRedirectTarget(Uri currentUri, string? locationHeader)
    {
        ArgumentNullException.ThrowIfNull(currentUri);

        if (string.IsNullOrWhiteSpace(locationHeader))
        {
            throw ImageIngestionException.InvalidUrl("Redirect response did not include a valid Location header.");
        }

        string trimmed = locationHeader.Trim();
        if (trimmed.Length > _options.MaxUrlLength)
        {
            throw ImageIngestionException.InvalidUrl($"Redirect target URL exceeds the maximum length of {_options.MaxUrlLength} characters.");
        }

        foreach (char c in trimmed)
        {
            if (char.IsControl(c))
            {
                throw ImageIngestionException.InvalidUrl("Redirect target URL contains invalid control characters.");
            }
        }

        if (!Uri.TryCreate(currentUri, trimmed, out var targetUri))
        {
            throw ImageIngestionException.InvalidUrl("Redirect target URL could not be resolved.");
        }

        return ValidateUriPolicy(targetUri);
    }

    private Uri ValidateUriPolicy(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw ImageIngestionException.InvalidUrl("Only HTTP and HTTPS URLs are permitted.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw ImageIngestionException.InvalidUrl("URLs containing embedded credentials (userinfo) are not permitted.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw ImageIngestionException.InvalidUrl("The URL must specify a valid host.");
        }

        if (!_destinationValidator.IsPortAllowed(uri.Port, _options.AllowedPorts))
        {
            throw ImageIngestionException.UnsafeDestination($"Port {uri.Port} is not permitted.");
        }

        if (!_destinationValidator.IsHostNameAllowed(uri.DnsSafeHost))
        {
            throw ImageIngestionException.UnsafeDestination();
        }

        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            if (!IPAddress.TryParse(uri.DnsSafeHost, out var ipAddress))
            {
                throw ImageIngestionException.InvalidUrl("The IP literal in the URL is malformed.");
            }

            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && ipAddress.ScopeId != 0)
            {
                throw ImageIngestionException.UnsafeDestination();
            }

            if (!_destinationValidator.IsAllowed(ipAddress))
            {
                throw ImageIngestionException.UnsafeDestination();
            }

            return uri;
        }

        // Normalize IDN hostnames and ensure basic hostname validity
        try
        {
            string asciiHost = Idn.GetAscii(uri.DnsSafeHost);
            if (string.IsNullOrWhiteSpace(asciiHost) || !asciiHost.Contains('.'))
            {
                throw ImageIngestionException.InvalidUrl("The URL hostname must be a valid fully qualified domain name.");
            }
        }
        catch (ArgumentException)
        {
            throw ImageIngestionException.InvalidUrl("The URL hostname could not be normalized.");
        }

        return uri;
    }
}
```

### File: `backend/VehicleImage.Api/Services/IImageDeliveryService.cs`

```csharp
namespace VehicleImage.Api.Services;

public sealed record ImageDeliveryResult(
    Stream Stream,
    string ContentType,
    long FileSize,
    string DownloadFileName
);

public interface IImageDeliveryService
{
    Task<ImageDeliveryResult> OpenImageAsync(Guid imageId, CancellationToken cancellationToken = default);
}
```

### File: `backend/VehicleImage.Api/Services/IImageIngestionService.cs`

```csharp
using VehicleImage.Api.Models;

namespace VehicleImage.Api.Services;

public interface IImageIngestionService
{
    Task<IngestedImage> IngestAsync(string imageUrl, CancellationToken cancellationToken = default);
}
```

### File: `backend/VehicleImage.Api/Services/ImageDeliveryService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using VehicleImage.Api.Data;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Storage;

namespace VehicleImage.Api.Services;

public sealed class ImageDeliveryService : IImageDeliveryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<ImageDeliveryService> _logger;

    public ImageDeliveryService(
        ApplicationDbContext dbContext,
        IImageStorage imageStorage,
        ILogger<ImageDeliveryService> logger)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    public async Task<ImageDeliveryResult> OpenImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var imageRecord = await _dbContext.VehicleImages
            .AsNoTracking()
            .Include(i => i.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);

        if (imageRecord is null)
        {
            throw ResourceNotFoundException.Image(imageId);
        }

        Stream fileStream;
        try
        {
            fileStream = await _imageStorage.OpenReadAsync(imageRecord.StorageKey, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(
                ex,
                "Data integrity error: VehicleImage row {ImageId} (VehicleId={VehicleId}) references missing file {StorageKey}.",
                imageRecord.Id,
                imageRecord.VehicleId,
                imageRecord.StorageKey);

            throw ResourceNotFoundException.Image(imageId);
        }

        string extension = Path.GetExtension(imageRecord.StorageKey);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = imageRecord.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        string downloadFileName = BuildDownloadFileName(imageRecord.Vehicle?.Make, imageRecord.Vehicle?.Model, imageRecord.Vehicle?.Year, imageId, extension);

        return new ImageDeliveryResult(
            Stream: fileStream,
            ContentType: imageRecord.ContentType,
            FileSize: imageRecord.FileSize,
            DownloadFileName: downloadFileName);
    }

    private static string BuildDownloadFileName(string? make, string? model, int? year, Guid imageId, string extension)
    {
        if (!string.IsNullOrWhiteSpace(make) && !string.IsNullOrWhiteSpace(model) && year.HasValue)
        {
            string safeMake = SanitizeSegment(make);
            string safeModel = SanitizeSegment(model);
            return $"{safeMake}-{safeModel}-{year.Value}-{imageId.ToString("N")[..8]}{extension}";
        }

        return $"vehicle-image-{imageId:N}{extension}";
    }

    private static string SanitizeSegment(string value)
    {
        Span<char> buffer = stackalloc char[Math.Min(value.Length, 32)];
        int idx = 0;
        foreach (char c in value)
        {
            if (idx >= buffer.Length)
            {
                break;
            }
            if (char.IsLetterOrDigit(c))
            {
                buffer[idx++] = char.ToLowerInvariant(c);
            }
            else if (c is ' ' or '-' or '_' && idx > 0 && buffer[idx - 1] != '-')
            {
                buffer[idx++] = '-';
            }
        }
        string result = new string(buffer[..idx]).Trim('-');
        return string.IsNullOrEmpty(result) ? "vehicle" : result;
    }
}
```

### File: `backend/VehicleImage.Api/Services/ImageIngestionService.cs`

```csharp
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Models;
using VehicleImage.Api.Retrieval;
using VehicleImage.Api.Security;
using VehicleImage.Api.Storage;

namespace VehicleImage.Api.Services;

public sealed class ImageIngestionService : IImageIngestionService
{
    private readonly UrlValidator _urlValidator;
    private readonly RetrievalOrchestrator _orchestrator;
    private readonly ImageValidator _imageValidator;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<ImageIngestionService> _logger;

    public ImageIngestionService(
        UrlValidator urlValidator,
        RetrievalOrchestrator orchestrator,
        ImageValidator imageValidator,
        IImageStorage imageStorage,
        ILogger<ImageIngestionService> logger)
    {
        _urlValidator = urlValidator;
        _orchestrator = orchestrator;
        _imageValidator = imageValidator;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    public async Task<IngestedImage> IngestAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        Uri validatedSourceUri = _urlValidator.Validate(imageUrl);

        ImageIngestionException? lastValidationFailure = null;
        int candidateNumber = 0;

        await foreach (var retrieved in _orchestrator.RetrieveCandidatesAsync(validatedSourceUri, cancellationToken))
        {
            candidateNumber++;
            await using (retrieved)
            {
                ImageMetadata metadata;
                try
                {
                    metadata = await _imageValidator.ValidateAsync(retrieved.Content, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (ImageIngestionException ex)
                {
                    lastValidationFailure = ex;
                    _logger.LogInformation(
                        "Candidate {CandidateNumber} from {FinalSourceUrl} (strategy={Strategy}) failed image validation [{Code}]: {Message}",
                        candidateNumber,
                        retrieved.FinalSourceUrl,
                        retrieved.Strategy,
                        ex.Code,
                        ex.Message);
                    continue;
                }

                string storageKey = await _imageStorage.SaveAsync(
                    retrieved.Content,
                    metadata.FileExtension,
                    cancellationToken);

                string formatString = metadata.Format switch
                {
                    ImageFormatKind.Jpeg => "JPEG",
                    ImageFormatKind.Png => "PNG",
                    ImageFormatKind.WebP => "WebP",
                    _ => metadata.Format.ToString().ToUpperInvariant()
                };

                return new IngestedImage(
                    ImageId: Guid.NewGuid(),
                    StorageKey: storageKey,
                    ContentType: metadata.ContentType,
                    Format: formatString,
                    FileSize: metadata.FileSize,
                    Width: metadata.Width,
                    Height: metadata.Height,
                    Sha256: metadata.Sha256,
                    SourceUrl: validatedSourceUri.AbsoluteUri,
                    FinalSourceUrl: retrieved.FinalSourceUrl.AbsoluteUri,
                    RetrievalStrategy: retrieved.Strategy);
            }
        }

        if (lastValidationFailure is not null)
        {
            throw lastValidationFailure;
        }

        throw ImageIngestionException.NoImageFound();
    }
}
```

### File: `backend/VehicleImage.Api/Services/IVehicleService.cs`

```csharp
using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;

namespace VehicleImage.Api.Services;

public interface IVehicleService
{
    Task<ImageSubmissionResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<ImageSubmissionResponse> ReplaceImageAsync(Guid vehicleId, ReplaceVehicleImageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<VehicleResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<VehicleResponse> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### File: `backend/VehicleImage.Api/Services/VehicleService.cs`

```csharp
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

        var vehicleImage = new VehicleImageEntity
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
            var newImage = new VehicleImageEntity
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
```

### File: `backend/VehicleImage.Api/Storage/IImageStorage.cs`

```csharp
namespace VehicleImage.Api.Storage;

public interface IImageStorage
{
    Task<string> SaveAsync(Stream image, string fileExtension, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
```

### File: `backend/VehicleImage.Api/Storage/LocalFileStorage.cs`

```csharp
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
```

### File: `backend/VehicleImage.Api/VehicleImage.Api.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HtmlAgilityPack" Version="1.11.72" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.15" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.15" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.15">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.15" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.15" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
  </ItemGroup>

</Project>
```

### File: `backend/VehicleImage.Api/VehicleImage.Api.http`

```http
@VehicleImage.Api_HostAddress = http://localhost:5080

GET {{VehicleImage.Api_HostAddress}}/weatherforecast/
Accept: application/json

###
```

## 3. Unit Tests

### File: `tests/UnitTests/DestinationValidatorTests.cs`

```csharp
using System.Net;
using System.Net.Sockets;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Security;

namespace VehicleImage.UnitTests;

public class DestinationValidatorTests
{
    private readonly DestinationValidator _validator = new();

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("93.184.216.34")]
    [InlineData("9.255.255.255")]
    [InlineData("11.0.0.1")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("168.63.129.17")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("192.167.255.255")]
    [InlineData("192.169.0.1")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b::8.8.8.8")]
    public void IsAllowed_PublicAddressesAndBoundaries_ReturnsTrue(string ip)
    {
        Assert.True(_validator.IsAllowed(IPAddress.Parse(ip)));
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("0.255.255.255")]
    [InlineData("10.0.0.0")]
    [InlineData("10.255.255.255")]
    [InlineData("100.64.0.0")]
    [InlineData("100.127.255.255")]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.254")]
    [InlineData("168.63.129.16")]      // Azure wire server
    [InlineData("169.254.0.1")]
    [InlineData("169.254.169.254")]    // Cloud metadata endpoint
    [InlineData("172.16.0.0")]
    [InlineData("172.31.255.255")]
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("192.88.99.1")]
    [InlineData("192.168.0.0")]
    [InlineData("192.168.255.255")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.255")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]          // Multicast
    [InlineData("239.255.255.255")]
    [InlineData("240.0.0.1")]          // Reserved
    [InlineData("255.255.255.255")]    // Broadcast
    [InlineData("::")]                 // IPv6 unspecified
    [InlineData("::1")]                // IPv6 loopback
    [InlineData("100::1")]             // Discard-only
    [InlineData("2001::1")]            // Teredo
    [InlineData("2001:2::1")]          // Benchmarking
    [InlineData("2001:db8::1")]        // Documentation
    [InlineData("fc00::1")]            // Unique local
    [InlineData("fd12:3456:789a::1")]  // Unique local
    [InlineData("fe80::1")]            // Link-local
    [InlineData("ff02::1")]            // Multicast
    [InlineData("::ffff:127.0.0.1")]   // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.1")]    // IPv4-mapped RFC 1918
    [InlineData("::ffff:169.254.169.254")] // IPv4-mapped metadata
    [InlineData("64:ff9b::127.0.0.1")] // NAT64 loopback
    [InlineData("64:ff9b::169.254.169.254")] // NAT64 metadata
    [InlineData("2002:7f00:0001::")]   // 6to4 embedding 127.0.0.1
    [InlineData("2002:0a00:0001::")]   // 6to4 embedding 10.0.0.1
    public void IsAllowed_BlockedIpv4AndIpv6Ranges_ReturnsFalse(string ip)
    {
        Assert.False(_validator.IsAllowed(IPAddress.Parse(ip)));
    }

    [Fact]
    public async Task ResolveAndValidateAsync_MixedPublicAndPrivateAddresses_ThrowsUnsafeDestination()
    {
        var validator = new DestinationValidator((_, _) => new ValueTask<IPAddress[]>(
            [IPAddress.Parse("93.184.216.34"), IPAddress.Parse("10.0.0.5")]));

        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => validator.ResolveAndValidateAsync("dual.supplier.example.com"));

        Assert.Equal(ErrorCodes.UnsafeDestination, ex.Code);
        Assert.DoesNotContain("10.0.0.5", ex.Message); // Never leak internal IP
    }

    [Fact]
    public async Task ResolveAndValidateAsync_EmptyOrFailedDns_ThrowsDnsResolutionFailed()
    {
        var validator = new DestinationValidator((_, _) =>
            throw new SocketException((int)SocketError.HostNotFound));

        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => validator.ResolveAndValidateAsync("nonexistent.example.com"));

        Assert.Equal(ErrorCodes.DnsResolutionFailed, ex.Code);
    }

    [Fact]
    public async Task ResolveAndValidateAsync_DnsRebindingOnSecondCall_RejectsSecondResolution()
    {
        int callCount = 0;
        var validator = new DestinationValidator((_, _) =>
        {
            callCount++;
            return callCount == 1
                ? new ValueTask<IPAddress[]>([IPAddress.Parse("93.184.216.34")])
                : new ValueTask<IPAddress[]>([IPAddress.Parse("169.254.169.254")]);
        });

        var firstResult = await validator.ResolveAndValidateAsync("rebind.example.com");
        Assert.Single(firstResult);

        var secondEx = await Assert.ThrowsAsync<ImageIngestionException>(
            () => validator.ResolveAndValidateAsync("rebind.example.com"));
        Assert.Equal(ErrorCodes.UnsafeDestination, secondEx.Code);
    }
}
```

### File: `tests/UnitTests/GlobalExceptionHandlerTests.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.UnitTests;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_ImageIngestionException_MapsTo422WithCodeAndRetryMetadata()
    {
        var context = CreateHttpContext();
        var ex = ImageIngestionException.HttpRateLimited(TimeSpan.FromSeconds(4));

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
        Assert.Equal("4", context.Response.Headers.RetryAfter.ToString());

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.HttpRateLimited, body.Code);
        Assert.True(body.Retryable);
        Assert.Equal(4, body.RetryAfterSeconds);
    }

    [Fact]
    public async Task TryHandleAsync_ResourceNotFoundException_MapsTo404()
    {
        var context = CreateHttpContext();
        var id = Guid.NewGuid();
        var ex = ResourceNotFoundException.Vehicle(id);

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.VehicleNotFound, body.Code);
        Assert.False(body.Retryable);
    }

    [Fact]
    public async Task TryHandleAsync_ConcurrentUpdateException_MapsTo409()
    {
        var context = CreateHttpContext();
        var ex = new ConcurrentUpdateException();

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.ConcurrentUpdate, body.Code);
        Assert.True(body.Retryable);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_MapsTo400()
    {
        var context = CreateHttpContext();
        var ex = new ValidationException("Page must be positive.");

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.ValidationFailed, body.Code);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_MapsTo500AndHidesInternalDetails()
    {
        var context = CreateHttpContext();
        var ex = new InvalidOperationException("Internal SQL Server IP 10.20.30.40 failed at /var/secret/path");

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.InternalError, body.Code);
        Assert.DoesNotContain("10.20.30.40", body.Detail);
        Assert.DoesNotContain("/var/secret/path", body.Detail);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-123";
        return context;
    }

    private static async Task<ImageFailureResponse> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var result = await JsonSerializer.DeserializeAsync<ImageFailureResponse>(context.Response.Body);
        return result ?? throw new InvalidOperationException("Failed to deserialize response body.");
    }
}
```

### File: `tests/UnitTests/HtmlCandidateRankingTests.cs`

```csharp
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Retrieval;
using VehicleImage.Api.Security;

namespace VehicleImage.UnitTests;

public class HtmlCandidateRankingTests
{
    private readonly HtmlImageRetriever _htmlRetriever;

    public HtmlCandidateRankingTests()
    {
        var retrievalOptions = Options.Create(new RetrievalOptions());
        var retryOptions = Options.Create(new RetryOptions());
        var htmlOptions = Options.Create(new HtmlExtractionOptions());
        var destinationValidator = new DestinationValidator();
        var urlValidator = new UrlValidator(retrievalOptions, destinationValidator);

        var directRetriever = new DirectImageRetriever(
            new DummyHttpClientFactory(),
            urlValidator,
            destinationValidator,
            retrievalOptions,
            retryOptions,
            NullLogger<DirectImageRetriever>.Instance);

        _htmlRetriever = new HtmlImageRetriever(
            directRetriever,
            urlValidator,
            htmlOptions,
            NullLogger<HtmlImageRetriever>.Instance);
    }

    [Fact]
    public void ExtractAndRankCandidates_PrioritizesOgImageOverOtherCandidates_AndFiltersBoilerplate()
    {
        string html = """
            <!DOCTYPE html>
            <html>
            <head>
                <meta property="og:image" content="/media/vehicles/2026-porsche-911-hero.jpg" />
                <meta name="twitter:image" content="https://cdn.dealer.example.com/vehicles/911-twitter.jpg" />
            </head>
            <body>
                <header>
                    <img src="/assets/dealer-logo.png" width="200" height="60" alt="Dealer Logo" />
                </header>
                <main>
                    <img src="/images/pixel-tracker.gif" width="1" height="1" />
                    <img src="/vehicles/gallery-side.jpg" width="800" height="600" alt="Vehicle exterior side view" />
                </main>
                <footer>
                    <img src="/assets/footer-badge.png" width="120" height="120" />
                </footer>
            </body>
            </html>
            """;

        var pageUrl = new Uri("https://dealer.example.com/inventory/2026-porsche-911");
        IReadOnlyList<ImageCandidate> ranked = _htmlRetriever.ExtractAndRankCandidates(html, pageUrl);

        Assert.Equal(3, ranked.Count);
        Assert.Equal("https://dealer.example.com/media/vehicles/2026-porsche-911-hero.jpg", ranked[0].Url.AbsoluteUri);
        Assert.Equal(CandidateSourceKind.OgImage, ranked[0].Source);
        Assert.Equal("https://cdn.dealer.example.com/vehicles/911-twitter.jpg", ranked[1].Url.AbsoluteUri);
        Assert.Equal("https://dealer.example.com/vehicles/gallery-side.jpg", ranked[2].Url.AbsoluteUri);
    }

    [Fact]
    public void ExtractAndRankCandidates_RespectsBaseHrefAndPicksLargestSrcsetAndDataSrc()
    {
        string html = """
            <html>
            <head>
                <base href="https://static.supplier.example.com/catalog/2026/" />
            </head>
            <body>
                <div class="vehicle-gallery">
                    <img srcset="small-car.jpg 320w, medium-car.jpg 640w, hd-vehicle-main.jpg 1600w"
                         src="small-car.jpg"
                         alt="Primary vehicle photo" />
                    <img data-src="interior/cockpit.webp" width="1024" height="768" alt="Vehicle interior" />
                </div>
            </body>
            </html>
            """;

        var pageUrl = new Uri("https://supplier.example.com/listing/123");
        IReadOnlyList<ImageCandidate> ranked = _htmlRetriever.ExtractAndRankCandidates(html, pageUrl);

        Assert.NotEmpty(ranked);
        Assert.Equal("https://static.supplier.example.com/catalog/2026/hd-vehicle-main.jpg", ranked[0].Url.AbsoluteUri);
        Assert.Contains(ranked, c => c.Url.AbsoluteUri == "https://static.supplier.example.com/catalog/2026/interior/cockpit.webp");
    }

    [Fact]
    public void ExtractAndRankCandidates_HtmlErrorPageReturning200_ReturnsEmpty()
    {
        string html = """
            <!DOCTYPE html>
            <html>
            <head><title>404 Not Found - Supplier Portal</title></head>
            <body>
                <h1>Page Not Found</h1>
                <img src="https://supplier.example.com/images/oops-car.jpg" width="600" height="400" />
            </body>
            </html>
            """;

        var pageUrl = new Uri("https://supplier.example.com/missing-vehicle");
        IReadOnlyList<ImageCandidate> ranked = _htmlRetriever.ExtractAndRankCandidates(html, pageUrl);

        Assert.Empty(ranked);
    }

    [Fact]
    public void DecodeHtml_Iso88591MetaCharset_DecodesCorrectly()
    {
        string rawHtml = "<html><head><meta charset=\"iso-8859-1\"></head><body><h1>Citroën Coupé</h1><img src=\"/citroen.jpg\" width=\"600\" height=\"400\"/></body></html>";
        byte[] isoBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(rawHtml);
        using var ms = new MemoryStream(isoBytes);

        string decoded = HtmlImageRetriever.DecodeHtml(ms, null);
        Assert.Contains("Citroën Coupé", decoded);
    }

    private sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
```

### File: `tests/UnitTests/ImageValidatorTests.cs`

```csharp
using System.Text;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;

namespace VehicleImage.UnitTests;

public class ImageValidatorTests
{
    private readonly ImageValidator _validator;

    public ImageValidatorTests()
    {
        var options = Options.Create(new ImageValidationOptions
        {
            AllowedFormats = ["Jpeg", "Png", "WebP"],
            MinWidth = 100,
            MinHeight = 100,
            MaxWidth = 2000,
            MaxHeight = 2000,
            MaxPixels = 2_000_000
        });
        _validator = new ImageValidator(options);
    }

    [Fact]
    public async Task ValidateAsync_ValidJpeg_ReturnsMetadataAndResetsStreamPosition()
    {
        using var stream = await CreateImageStreamAsync(320, 240, "jpeg");
        ImageMetadata metadata = await _validator.ValidateAsync(stream);

        Assert.Equal(ImageFormatKind.Jpeg, metadata.Format);
        Assert.Equal("image/jpeg", metadata.ContentType);
        Assert.Equal(".jpg", metadata.FileExtension);
        Assert.Equal(320, metadata.Width);
        Assert.Equal(240, metadata.Height);
        Assert.Equal(stream.Length, metadata.FileSize);
        Assert.Equal(64, metadata.Sha256.Length);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task ValidateAsync_ValidPng_ReturnsMetadata()
    {
        using var stream = await CreateImageStreamAsync(200, 150, "png");
        ImageMetadata metadata = await _validator.ValidateAsync(stream);

        Assert.Equal(ImageFormatKind.Png, metadata.Format);
        Assert.Equal("image/png", metadata.ContentType);
        Assert.Equal(".png", metadata.FileExtension);
        Assert.Equal(200, metadata.Width);
        Assert.Equal(150, metadata.Height);
    }

    [Fact]
    public async Task ValidateAsync_ValidWebP_ReturnsMetadata()
    {
        using var stream = await CreateImageStreamAsync(250, 180, "webp");
        ImageMetadata metadata = await _validator.ValidateAsync(stream);

        Assert.Equal(ImageFormatKind.WebP, metadata.Format);
        Assert.Equal("image/webp", metadata.ContentType);
        Assert.Equal(".webp", metadata.FileExtension);
        Assert.Equal(250, metadata.Width);
        Assert.Equal(180, metadata.Height);
    }

    [Fact]
    public async Task ValidateAsync_TruncatedJpeg_ThrowsImageDecodeFailed()
    {
        using var fullStream = await CreateImageStreamAsync(300, 200, "jpeg");
        byte[] fullBytes = fullStream.ToArray();
        using var truncatedStream = new MemoryStream(fullBytes, 0, fullBytes.Length / 2);

        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(truncatedStream));

        Assert.Equal(ErrorCodes.ImageDecodeFailed, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_CorruptedPngPayload_ThrowsImageDecodeFailed()
    {
        using var fullStream = await CreateImageStreamAsync(200, 200, "png");
        byte[] bytes = fullStream.ToArray();
        // Corrupt the IDAT compressed chunk data near the middle while keeping header intact
        for (int i = bytes.Length / 2; i < Math.Min(bytes.Length - 12, (bytes.Length / 2) + 32); i++)
        {
            bytes[i] ^= 0xFF;
        }

        using var corruptedStream = new MemoryStream(bytes);
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(corruptedStream));

        Assert.Equal(ErrorCodes.ImageDecodeFailed, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedGifAndBmpFormats_ThrowsUnsupportedImageFormat()
    {
        using var gifStream = await CreateImageStreamAsync(200, 200, "gif");
        var gifEx = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(gifStream));
        Assert.Equal(ErrorCodes.UnsupportedImageFormat, gifEx.Code);

        using var bmpStream = await CreateImageStreamAsync(200, 200, "bmp");
        var bmpEx = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(bmpStream));
        Assert.Equal(ErrorCodes.UnsupportedImageFormat, bmpEx.Code);
    }

    [Theory]
    [InlineData("<!DOCTYPE html><html><body><h1>404 Not Found</h1></body></html>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\"><script>alert(1)</script></svg>")]
    [InlineData("<?xml version=\"1.0\"?><svg><rect width=\"100\" height=\"100\"/></svg>")]
    [InlineData("{\"error\": \"not an image\"}")]
    [InlineData("Plain text pretending to be car.jpg")]
    public async Task ValidateAsync_NonImageOrSvgContent_ThrowsUnsupportedContentType(string payload)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(stream));

        Assert.Equal(ErrorCodes.UnsupportedContentType, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_ImageBelowMinDimensions_ThrowsImageTooSmall()
    {
        using var tinyStream = await CreateImageStreamAsync(64, 64, "png");
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(tinyStream));

        Assert.Equal(ErrorCodes.ImageTooSmall, ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_ImageExceedingMaxDimensionsOrPixels_ThrowsImageDimensionsExceeded()
    {
        // Craft a minimal PNG header (IHDR) declaring 15000 x 15000 dimensions (225M pixels)
        // without allocating a huge image in the test!
        using var basePng = await CreateImageStreamAsync(120, 120, "png");
        byte[] bytes = basePng.ToArray();
        // PNG IHDR width is at bytes 16..19 (big-endian), height at bytes 20..23
        WriteBigEndianInt32(bytes.AsSpan(16, 4), 15000);
        WriteBigEndianInt32(bytes.AsSpan(20, 4), 15000);

        using var bombStream = new MemoryStream(bytes);
        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => _validator.ValidateAsync(bombStream));

        Assert.Equal(ErrorCodes.ImageDimensionsExceeded, ex.Code);
    }

    private static async Task<MemoryStream> CreateImageStreamAsync(int width, int height, string format)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)((x * 7) % 256), (byte)((y * 13) % 256), 140, 255);
            }
        }

        var ms = new MemoryStream();
        switch (format)
        {
            case "jpeg":
                await image.SaveAsJpegAsync(ms);
                break;
            case "png":
                await image.SaveAsPngAsync(ms);
                break;
            case "webp":
                await image.SaveAsWebpAsync(ms);
                break;
            case "gif":
                await image.SaveAsGifAsync(ms);
                break;
            case "bmp":
                await image.SaveAsBmpAsync(ms);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }

        ms.Position = 0;
        return ms;
    }

    private static void WriteBigEndianInt32(Span<byte> target, int value)
    {
        target[0] = (byte)((value >> 24) & 0xFF);
        target[1] = (byte)((value >> 16) & 0xFF);
        target[2] = (byte)((value >> 8) & 0xFF);
        target[3] = (byte)(value & 0xFF);
    }
}
```

### File: `tests/UnitTests/LocalFileStorageTests.cs`

```csharp
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
```

### File: `tests/UnitTests/UrlValidatorTests.cs`

```csharp
using System.Net;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Security;

namespace VehicleImage.UnitTests;

public class UrlValidatorTests
{
    private readonly UrlValidator _validator;

    public UrlValidatorTests()
    {
        var options = Options.Create(new RetrievalOptions
        {
            AllowedPorts = [80, 443],
            MaxUrlLength = 2048
        });
        _validator = new UrlValidator(options, new DestinationValidator());
    }

    [Theory]
    [InlineData("https://cdn.supplier.example.com/vehicles/2026/civic.jpg")]
    [InlineData("http://images.example.org:80/photo.png")]
    [InlineData("https://images.example.org:443/path/to/car?width=1200")]
    [InlineData("https://münchen-autos.de/car.webp")]
    public void Validate_ValidPublicHttpOrHttpsUrl_ReturnsUri(string rawUrl)
    {
        Uri result = _validator.Validate(rawUrl);
        Assert.NotNull(result);
        Assert.True(result.IsAbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative/path/car.jpg")]
    [InlineData("not-a-url")]
    [InlineData("ftp://files.example.com/car.jpg")]
    [InlineData("file:///etc/passwd")]
    [InlineData("gopher://example.com:70/1")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    [InlineData("https://user:password@example.com/car.jpg")]
    [InlineData("https://intranet/car.jpg")]
    public void Validate_MalformedOrDisallowedSchemeOrUserInfo_ThrowsInvalidUrl(string? rawUrl)
    {
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(rawUrl));
        Assert.Equal(ErrorCodes.InvalidUrl, ex.Code);
    }

    [Fact]
    public void Validate_ExceedingMaxUrlLength_ThrowsInvalidUrl()
    {
        string longUrl = "https://example.com/" + new string('a', 2050);
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(longUrl));
        Assert.Equal(ErrorCodes.InvalidUrl, ex.Code);
    }

    [Fact]
    public void Validate_UrlWithControlCharacter_ThrowsInvalidUrl()
    {
        string badUrl = "https://example.com/car\u0007.jpg";
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(badUrl));
        Assert.Equal(ErrorCodes.InvalidUrl, ex.Code);
    }

    [Theory]
    [InlineData("http://localhost/car.jpg")]
    [InlineData("https://api.localhost/car.jpg")]
    [InlineData("http://printer.local/car.jpg")]
    [InlineData("http://metadata.internal/car.jpg")]
    [InlineData("http://127.0.0.1/car.jpg")]
    [InlineData("http://10.0.0.1/car.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://192.168.1.50/car.jpg")]
    [InlineData("http://[::1]/car.jpg")]
    [InlineData("https://example.com:8080/car.jpg")]
    [InlineData("http://example.com:22/car.jpg")]
    public void Validate_UnsafeHostIpOrDisallowedPort_ThrowsUnsafeDestination(string rawUrl)
    {
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(rawUrl));
        Assert.Equal(ErrorCodes.UnsafeDestination, ex.Code);
    }

    [Fact]
    public void ValidateRedirectTarget_ResolvesRelativeUrlAgainstCurrentUri()
    {
        var current = new Uri("https://supplier.example.com/listings/2026/sedan.html");
        Uri target = _validator.ValidateRedirectTarget(current, "../images/hero.jpg");
        Assert.Equal("https://supplier.example.com/listings/images/hero.jpg", target.AbsoluteUri);
    }

    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://localhost/private.jpg")]
    [InlineData("https://supplier.example.com:8443/car.jpg")]
    public void ValidateRedirectTarget_RedirectToPrivateIpOrBadPort_ThrowsUnsafeDestination(string location)
    {
        var current = new Uri("https://supplier.example.com/image");
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.ValidateRedirectTarget(current, location));
        Assert.Equal(ErrorCodes.UnsafeDestination, ex.Code);
    }
}
```

### File: `tests/UnitTests/VehicleImage.UnitTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\backend\VehicleImage.Api\VehicleImage.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

## 4. Integration Tests

### File: `tests/IntegrationTests/ApiFactory.cs`

```csharp
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleImage.Api.Data;
using VehicleImage.Api.Security;

namespace VehicleImage.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly int _allowedMockPort;
    private readonly bool _useStrictProductionSsrfValidator;
    private readonly Func<string, CancellationToken, ValueTask<IPAddress[]>>? _customDnsResolver;
    private readonly Action<IServiceCollection>? _serviceOverrides;

    public string TempStorageRoot { get; }
    public string TempSqliteDbPath { get; }

    public ApiFactory(
        int allowedMockPort,
        bool useStrictProductionSsrfValidator = false,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? customDnsResolver = null,
        Action<IServiceCollection>? serviceOverrides = null)
    {
        _allowedMockPort = allowedMockPort;
        _useStrictProductionSsrfValidator = useStrictProductionSsrfValidator;
        _customDnsResolver = customDnsResolver;
        _serviceOverrides = serviceOverrides;

        string baseTemp = Path.Combine(Path.GetTempPath(), $"vehicle-img-int-{Guid.NewGuid():N}");
        TempStorageRoot = Path.Combine(baseTemp, "storage");
        TempSqliteDbPath = Path.Combine(baseTemp, "test.db");
        Directory.CreateDirectory(TempStorageRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={TempSqliteDbPath}",
                ["Storage:RootPath"] = TempStorageRoot,
                ["Retrieval:MaxDownloadBytes"] = (512 * 1024).ToString(), // 512 KB cap for fast streaming tests
                ["Retrieval:TimeoutSeconds"] = "2",
                ["Retrieval:ConnectTimeoutSeconds"] = "2",
                ["Retrieval:MaxRedirects"] = "5",
                ["Retry:MaxAttempts"] = "3",
                ["Retry:BaseDelayMs"] = "25",
                ["Retry:MaxDelayMs"] = "100",
                ["Retry:MaxRetryAfterSeconds"] = "2"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            if (_useStrictProductionSsrfValidator)
            {
                if (_customDnsResolver is not null)
                {
                    services.RemoveAll<DestinationValidator>();
                    services.AddSingleton(new DestinationValidator(_customDnsResolver));
                }
            }
            else
            {
                // Section 17.5: Permissive test double limited strictly to the MockSupplierServer host/port
                services.RemoveAll<DestinationValidator>();
                services.AddSingleton<DestinationValidator>(
                    new MockServerLoopbackDestinationValidator(_allowedMockPort, _customDnsResolver));
            }

            _serviceOverrides?.Invoke(services);

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                string? parent = Path.GetDirectoryName(TempStorageRoot);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    Directory.Delete(parent, recursive: true);
                }
            }
            catch
            {
                // Ignore temp cleanup race on disposal
            }
        }
    }

    private sealed class MockServerLoopbackDestinationValidator : DestinationValidator
    {
        private readonly int _mockPort;

        public MockServerLoopbackDestinationValidator(
            int mockPort,
            Func<string, CancellationToken, ValueTask<IPAddress[]>>? customResolver)
            : base(customResolver ?? DefaultMockResolver)
        {
            _mockPort = mockPort;
        }

        public override bool IsPortAllowed(int port, IReadOnlyCollection<int> configuredPorts)
        {
            return port == _mockPort || base.IsPortAllowed(port, configuredPorts);
        }

        public override bool IsAllowed(IPAddress address)
        {
            // Permit only IPv4 127.0.0.1 for the local MockSupplierServer; all other private/metadata IPs remain blocked!
            if (address.Equals(IPAddress.Loopback))
            {
                return true;
            }
            return base.IsAllowed(address);
        }

        private static ValueTask<IPAddress[]> DefaultMockResolver(string host, CancellationToken ct)
        {
            if (string.Equals(host, "supplier.test", StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTask<IPAddress[]>([IPAddress.Loopback]);
            }
            return new ValueTask<IPAddress[]>(Dns.GetHostAddressesAsync(host, ct));
        }
    }
}
```

### File: `tests/IntegrationTests/DeliveryTests.cs`

```csharp
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
```

### File: `tests/IntegrationTests/DirectImageIngestionTests.cs`

```csharp
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
```

### File: `tests/IntegrationTests/Fixtures/Html/error-200.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <title>404 Not Found</title>
</head>
<body>
    <h1>Page Not Found</h1>
    <p>The requested vehicle listing does not exist, even though the server returned HTTP 200.</p>
    <img src="/fixtures/images/valid-car.jpg" width="640" height="400" alt="Decorative image" />
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/lazy-data-src.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>Lazy Loaded Vehicle Gallery</title>
</head>
<body>
    <section class="vehicle-detail">
        <img data-src="/fixtures/images/valid-car.webp" src="/fixtures/images/spinner-loading.gif" width="640" height="400" alt="Lazy loaded vehicle exterior" />
    </section>
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/malformed.html`

```html
<html>
<head><title>Malformed Listing
<body>
<div class="vehicle-photo">
    <p>Unclosed tags everywhere
    <img src="/fixtures/images/valid-car.png" width="640" height="400" alt="Vehicle photo in malformed HTML">
</div>
```

### File: `tests/IntegrationTests/Fixtures/Html/multi-candidate.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>2026 Audi RS6 Avant</title>
    <!-- Candidate 1 is og:image, but it points to a tiny 60x60 thumbnail that fails ImageValidator -->
    <meta property="og:image" content="/fixtures/images/too-small.png" />
</head>
<body>
    <header class="site-header">
        <img src="/fixtures/images/site-logo.png" alt="Site Logo" width="180" height="50" />
    </header>
    <main class="vehicle-listing">
        <img src="/fixtures/images/tracking-pixel.gif" width="1" height="1" alt="" />
        <!-- Candidate 2 is the real vehicle hero image and passes ImageValidator -->
        <img class="vehicle-hero-photo" src="/fixtures/images/valid-car.png" width="640" height="400" alt="2026 Audi RS6 Avant exterior" />
    </main>
    <footer>
        <img src="/fixtures/images/ad-banner.jpg" alt="Sponsor ad" />
    </footer>
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/no-image.html`

```html
<!DOCTYPE html>
<html lang="en">
<head><title>Text Only Vehicle Specification</title></head>
<body>
    <h1>Vehicle Specification Sheet</h1>
    <p>This page contains technical specifications only and has no images.</p>
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/og-image.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>2026 Porsche 911 Carrera GTS</title>
    <meta property="og:title" content="2026 Porsche 911 Carrera GTS" />
    <meta property="og:image" content="/fixtures/images/valid-car.jpg" />
</head>
<body>
    <header><img src="/fixtures/images/dealer-logo.png" alt="Logo" width="120" height="40" /></header>
    <h1>2026 Porsche 911 Carrera GTS</h1>
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/relative-urls.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>Relative URLs Listing</title>
</head>
<body>
    <div class="car-gallery">
        <img src="../images/valid-car.jpg" width="640" height="400" alt="Vehicle front view" />
    </div>
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/srcset-gallery.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>Srcset Vehicle Page</title>
</head>
<body>
    <article class="vehicle-card">
        <img srcset="/fixtures/images/too-small.png 300w, /fixtures/images/valid-car.jpg 1200w"
             src="/fixtures/images/too-small.png"
             alt="High resolution vehicle photo" />
    </article>
</body>
</html>
```

### File: `tests/IntegrationTests/Fixtures/Html/twitter-image.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>2026 BMW M3 Competition</title>
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:image" content="/fixtures/images/valid-car.webp" />
</head>
<body>
    <h1>2026 BMW M3 Competition</h1>
</body>
</html>
```

### File: `tests/IntegrationTests/HtmlExtractionTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class HtmlExtractionTests : IAsyncLifetime
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
    [InlineData("/fixtures/html/og-image.html", "JPEG")]
    [InlineData("/fixtures/html/twitter-image.html", "WebP")]
    [InlineData("/fixtures/html/multi-candidate.html", "PNG")]
    [InlineData("/fixtures/html/relative-urls.html", "JPEG")]
    [InlineData("/fixtures/html/lazy-data-src.html", "WebP")]
    [InlineData("/fixtures/html/srcset-gallery.html", "JPEG")]
    [InlineData("/fixtures/html/malformed.html", "PNG")]
    [InlineData("/fixtures/html/charset-iso.html", "JPEG")]
    public async Task CreateVehicle_HtmlPageWithVehicleImage_ExtractsValidatesAndStoresImage(
        string htmlPath,
        string expectedFormat)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Audi",
            Model = "RS6 Avant",
            Year = 2026,
            ImageUrl = _supplier.Url(htmlPath)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var submission = (await response.Content.ReadFromJsonAsync<ImageSubmissionResponse>())!;
        var vehicle = (await _client.GetFromJsonAsync<VehicleResponse>($"/api/vehicles/{submission.VehicleId}"))!;

        Assert.Equal("Html", vehicle.RetrievalStrategy);
        Assert.Equal(expectedFormat, vehicle.Format);
        Assert.Equal(640, vehicle.Width);
        Assert.Equal(400, vehicle.Height);
    }

    [Theory]
    [InlineData("/fixtures/html/no-image.html")]
    [InlineData("/fixtures/html/error-200.html")]
    public async Task CreateVehicle_HtmlPageWithNoImageOrSoft404ErrorPage_Returns422NoImageFound(string htmlPath)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Audi",
            Model = "A4",
            Year = 2026,
            ImageUrl = _supplier.Url(htmlPath)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = (await response.Content.ReadFromJsonAsync<ImageFailureResponse>())!;
        Assert.Equal(ErrorCodes.NoImageFound, failure.Code);
    }
}
```

### File: `tests/IntegrationTests/MockSupplierServer.cs`

```csharp
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace VehicleImage.IntegrationTests;

public sealed class MockSupplierServer : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly ConcurrentDictionary<string, int> _requestCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<string> _requestMethods = [];
    private readonly HashSet<string> _disabledRoutes = new(StringComparer.OrdinalIgnoreCase);

    public Uri BaseUri { get; private set; } = null!;
    public int Port => BaseUri.Port;

    public byte[] ValidJpegBytes { get; private set; } = null!;
    public byte[] ValidPngBytes { get; private set; } = null!;
    public byte[] ValidWebPBytes { get; private set; } = null!;
    public byte[] CorruptedJpegBytes { get; private set; } = null!;
    public byte[] UnsupportedGifBytes { get; private set; } = null!;
    public byte[] UnsupportedBmpBytes { get; private set; } = null!;
    public byte[] TooSmallPngBytes { get; private set; } = null!;
    public byte[] DecompressionBombPngBytes { get; private set; } = null!;

    public static async Task<MockSupplierServer> StartAsync()
    {
        var server = new MockSupplierServer();
        await server.InitializeAsync();
        return server;
    }

    public int GetRequestCount(string path)
        => _requestCounts.TryGetValue(path, out int count) ? count : 0;

    public int TotalRequests => _requestCounts.Values.Sum();

    public IReadOnlyCollection<string> RequestMethods => _requestMethods.ToArray();

    public void DisableRoute(string path) => _disabledRoutes.Add(path);

    public void ResetCounters()
    {
        _requestCounts.Clear();
        _requestMethods.Clear();
    }

    public string Url(string relativePath)
    {
        string clean = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        // Use a realistic public-looking hostname "supplier.test" that resolves to 127.0.0.1 in our test resolver
        return $"http://supplier.test:{Port}{clean}";
    }

    private async Task InitializeAsync()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        await GenerateFixtureBytesAsync();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            string path = context.Request.Path.Value ?? "/";
            _requestMethods.Add(context.Request.Method);
            _requestCounts.AddOrUpdate(path, 1, (_, count) => count + 1);

            if (_disabledRoutes.Contains(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });

        // 1. Image fixtures
        app.MapGet("/fixtures/images/valid-car.jpg", () => Results.Bytes(ValidJpegBytes, "image/jpeg"));
        app.MapGet("/fixtures/images/valid-car.png", () => Results.Bytes(ValidPngBytes, "image/png"));
        app.MapGet("/fixtures/images/valid-car.webp", () => Results.Bytes(ValidWebPBytes, "image/webp"));
        app.MapGet("/fixtures/images/corrupted.jpg", () => Results.Bytes(CorruptedJpegBytes, "image/jpeg"));
        app.MapGet("/fixtures/images/unsupported.gif", () => Results.Bytes(UnsupportedGifBytes, "image/gif"));
        app.MapGet("/fixtures/images/unsupported.bmp", () => Results.Bytes(UnsupportedBmpBytes, "image/bmp"));
        app.MapGet("/fixtures/images/too-small.png", () => Results.Bytes(TooSmallPngBytes, "image/png"));
        app.MapGet("/fixtures/images/decompression-bomb.png", () => Results.Bytes(DecompressionBombPngBytes, "image/png"));
        app.MapGet("/fixtures/images/vector.svg", () => Results.Content(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"300\"><rect width=\"100%\" height=\"100%\" fill=\"red\"/></svg>",
            "image/svg+xml"));
        app.MapGet("/fixtures/images/html-masquerade.jpg", () => Results.Content(
            "<!DOCTYPE html><html><body><h1>Hotlink protection error</h1></body></html>",
            "text/html"));

        // 2. Redirect endpoints
        app.MapGet("/redirect/chain-start", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            ctx.Response.Headers.Location = "/redirect/chain-mid";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/chain-mid", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = $"http://supplier.test:{Port}/fixtures/images/valid-car.jpg";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/to-private", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "http://169.254.169.254/latest/meta-data/";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/to-localhost", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = $"http://localhost:{Port}/fixtures/images/valid-car.jpg";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/loop/{step:int}", (int step, HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = $"/redirect/loop/{step + 1}";
            return Task.CompletedTask;
        });

        // 3. Upstream HTTP status & flaky retry endpoints
        app.MapGet("/status/404", () => Results.StatusCode(StatusCodes.Status404NotFound));
        app.MapGet("/status/401", () => Results.StatusCode(StatusCodes.Status401Unauthorized));
        app.MapGet("/status/403", () => Results.StatusCode(StatusCodes.Status403Forbidden));
        app.MapGet("/status/429-long", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.Response.Headers.RetryAfter = "60";
            return Task.CompletedTask;
        });
        app.MapGet("/status/500", () => Results.StatusCode(StatusCodes.Status500InternalServerError));
        app.MapGet("/flaky/transient-503", (HttpContext ctx) =>
        {
            int count = GetRequestCount("/flaky/transient-503");
            if (count == 1)
            {
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Task.CompletedTask;
            }
            return Results.Bytes(ValidJpegBytes, "image/jpeg").ExecuteAsync(ctx);
        });

        // 4. Streaming limit & timeout endpoints
        app.MapGet("/stream/oversize-no-length", async (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "image/jpeg";
            byte[] block = new byte[32 * 1024];
            block[0] = 0xFF;
            block[1] = 0xD8;
            block[2] = 0xFF;

            for (int i = 0; i < 64; i++) // 2 MB total (test configures MaxDownloadBytes = 512 KB)
            {
                await ctx.Response.Body.WriteAsync(block, ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
        });

        app.MapGet("/stream/oversize-misleading-length", async (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "image/jpeg";
            // Claim a tiny Content-Length or chunked body while sending > cap
            byte[] payload = new byte[800 * 1024]; // 800 KB
            payload[0] = 0xFF;
            payload[1] = 0xD8;
            payload[2] = 0xFF;
            await ctx.Response.Body.WriteAsync(payload, ctx.RequestAborted);
        });

        app.MapGet("/slow/timeout", async (HttpContext ctx) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ctx.RequestAborted);
            await Results.Bytes(ValidJpegBytes, "image/jpeg").ExecuteAsync(ctx);
        });

        // 5. HTML fixture endpoints
        string htmlDir = ResolveFixturesHtmlDir();
        app.MapGet("/fixtures/html/{name}", async (string name, HttpContext ctx) =>
        {
            if (string.Equals(name, "charset-iso.html", StringComparison.OrdinalIgnoreCase))
            {
                string isoHtml = $"<html><head><meta charset=\"iso-8859-1\"></head><body><h1>Citroën</h1><img src=\"/fixtures/images/valid-car.jpg\" width=\"640\" height=\"400\" alt=\"Citroën Coupé\"/></body></html>";
                byte[] isoBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(isoHtml);
                ctx.Response.ContentType = "text/html; charset=iso-8859-1";
                await ctx.Response.Body.WriteAsync(isoBytes);
                return;
            }

            string filePath = Path.Combine(htmlDir, name);
            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            string html = await File.ReadAllTextAsync(filePath);
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        await app.StartAsync();
        _app = app;

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
        BaseUri = new Uri(addresses.First());
    }

    private async Task GenerateFixtureBytesAsync()
    {
        ValidJpegBytes = await CreateImageBytesAsync(640, 400, "jpeg");
        ValidPngBytes = await CreateImageBytesAsync(640, 400, "png");
        ValidWebPBytes = await CreateImageBytesAsync(640, 400, "webp");
        CorruptedJpegBytes = ValidJpegBytes[..(ValidJpegBytes.Length / 2)];
        UnsupportedGifBytes = await CreateImageBytesAsync(300, 200, "gif");
        UnsupportedBmpBytes = await CreateImageBytesAsync(300, 200, "bmp");
        TooSmallPngBytes = await CreateImageBytesAsync(60, 60, "png");

        byte[] baseBomb = (byte[])ValidPngBytes.Clone();
        baseBomb[16] = 0x00;
        baseBomb[17] = 0x00;
        baseBomb[18] = 0x3A;
        baseBomb[19] = 0x98; // 15000
        baseBomb[20] = 0x00;
        baseBomb[21] = 0x00;
        baseBomb[22] = 0x3A;
        baseBomb[23] = 0x98; // 15000
        DecompressionBombPngBytes = baseBomb;

    }

    private static async Task<byte[]> CreateImageBytesAsync(int width, int height, string format)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)((x * 5) % 256), (byte)((y * 9) % 256), 180, 255);
            }
        }

        using var ms = new MemoryStream();
        switch (format)
        {
            case "jpeg":
                await image.SaveAsJpegAsync(ms);
                break;
            case "png":
                await image.SaveAsPngAsync(ms);
                break;
            case "webp":
                await image.SaveAsWebpAsync(ms);
                break;
            case "gif":
                await image.SaveAsGifAsync(ms);
                break;
            case "bmp":
                await image.SaveAsBmpAsync(ms);
                break;
        }
        return ms.ToArray();
    }

    private static string ResolveFixturesHtmlDir() => Path.Combine(ResolveFixturesRootDir(), "Html");

    private static string ResolveFixturesRootDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "VehicleImage.IntegrationTests.csproj")))
        {
            dir = dir.Parent;
        }
        string projectRoot = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(projectRoot, "Fixtures");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
```

### File: `tests/IntegrationTests/RedirectTests.cs`

```csharp
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
    [InlineData("/redirect/to-private")]
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
```

### File: `tests/IntegrationTests/ReplacementTests.cs`

```csharp
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
```

### File: `tests/IntegrationTests/SsrfSecurityTests.cs`

```csharp
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
```

### File: `tests/IntegrationTests/StreamingLimitTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.IntegrationTests;

public class StreamingLimitTests : IAsyncLifetime
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
    [InlineData("/stream/oversize-no-length")]
    [InlineData("/stream/oversize-misleading-length")]
    public async Task CreateVehicle_OversizedStreamedResponse_AbortsAndReturns422ImageTooLarge(string path)
    {
        var request = new CreateVehicleRequest
        {
            Make = "Rivian",
            Model = "R1T",
            Year = 2026,
            ImageUrl = _supplier.Url(path)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.ImageTooLarge, failure.Code);
    }

    [Fact]
    public async Task CreateVehicle_SlowUpstreamExceedingTimeout_Returns422RequestTimeout()
    {
        var request = new CreateVehicleRequest
        {
            Make = "Lucid",
            Model = "Air",
            Year = 2026,
            ImageUrl = _supplier.Url("/slow/timeout")
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/vehicles", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var failure = await response.Content.ReadFromJsonAsync<ImageFailureResponse>();
        Assert.NotNull(failure);
        Assert.Equal(ErrorCodes.RequestTimeout, failure.Code);
        Assert.True(failure.Retryable);
    }
}
```

### File: `tests/IntegrationTests/VehicleImage.IntegrationTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.15" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.15" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\backend\VehicleImage.Api\VehicleImage.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

### File: `tests/IntegrationTests/VehicleServiceCancellationTests.cs`

```csharp
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
```

## 5. Frontend

### File: `frontend/vehicle-image-dashboard/index.html`

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta
      name="description"
      content="Multi-Source Vehicle Image Retrieval, Validation, Internal Storage and Delivery System"
    />
    <title>VehicleImageSystem — Multi-Source Image Ingestion & Delivery</title>
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link
      href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap"
      rel="stylesheet"
    />
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.jsx"></script>
  </body>
</html>
```

### File: `frontend/vehicle-image-dashboard/package-lock.json`

```json
{
  "name": "vehicle-image-dashboard",
  "version": "1.0.0",
  "lockfileVersion": 3,
  "requires": true,
  "packages": {
    "": {
      "name": "vehicle-image-dashboard",
      "version": "1.0.0",
      "dependencies": {
        "react": "^19.0.0",
        "react-dom": "^19.0.0"
      },
      "devDependencies": {
        "@vitejs/plugin-react": "^4.3.4",
        "vite": "^6.2.0"
      }
    },
    "node_modules/@babel/code-frame": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/code-frame/-/code-frame-7.29.7.tgz",
      "integrity": "sha512-Aup7aUOfpbAUg2ROOJN6Iw5f9DMBlzu0mIkm/malLQFN/YQgO48wCj0Kxa3sEHJvPVFg7siR+qRInwXd2qhQKw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/helper-validator-identifier": "^7.29.7",
        "js-tokens": "^4.0.0",
        "picocolors": "^1.1.1"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/compat-data": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/compat-data/-/compat-data-7.29.7.tgz",
      "integrity": "sha512-locTkQyKvwIEgBzVrn8693ebc97F2U8ZHjbXwDXJ5Fn2TCpNwTlKcaKLkdHop5c/icOFE7qt7Q9JC5hnKNa6Gg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/core": {
      "version": "7.29.13",
      "resolved": "https://registry.npmjs.org/@babel/core/-/core-7.29.13.tgz",
      "integrity": "sha512-j1nvOQCZYSgp7HLmisnCXFTpVvFODiijQ9QHkORfGKWH6YDtskTEeCJS7eV545GZ9UBzrq0e2xsOd5QQ5PjIqw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/code-frame": "^7.29.7",
        "@babel/generator": "^7.29.13",
        "@babel/helper-compilation-targets": "^7.29.13",
        "@babel/helper-module-transforms": "^7.29.13",
        "@babel/helpers": "^7.29.13",
        "@babel/parser": "^7.29.13",
        "@babel/template": "^7.29.7",
        "@babel/traverse": "^7.29.13",
        "@babel/types": "^7.29.11",
        "@jridgewell/remapping": "^2.3.5",
        "convert-source-map": "^2.0.0",
        "debug": "^4.1.0",
        "gensync": "^1.0.0-beta.2",
        "json5": "^2.2.3",
        "semver": "^6.3.1"
      },
      "engines": {
        "node": ">=6.9.0"
      },
      "funding": {
        "type": "opencollective",
        "url": "https://opencollective.com/babel"
      }
    },
    "node_modules/@babel/generator": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/generator/-/generator-7.29.14.tgz",
      "integrity": "sha512-qSY56jgW5JnSXzuekLVil6zaOQxOCtOHidr65KkaMBw772IuAY28Qx4ZykAcDn8+FRF15qk80XzKwgSc/RYW6g==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/parser": "^7.29.14",
        "@babel/types": "^7.29.14",
        "@jridgewell/gen-mapping": "^0.3.12",
        "@jridgewell/trace-mapping": "^0.3.28",
        "jsesc": "^3.0.2"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-compilation-targets": {
      "version": "7.29.13",
      "resolved": "https://registry.npmjs.org/@babel/helper-compilation-targets/-/helper-compilation-targets-7.29.13.tgz",
      "integrity": "sha512-8WxJ4/Vmes3ghacj8VOJy3NW8eyHiFY/4thU/LSBuBE470+igjR/j1ZTNn8fV5VdNrLx6iXUwig526UdOvfP0w==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/compat-data": "^7.29.7",
        "@babel/helper-validator-option": "^7.29.7",
        "browserslist": "^4.24.0",
        "lru-cache": "^5.1.1",
        "semver": "^6.3.1"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-globals": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/helper-globals/-/helper-globals-7.29.7.tgz",
      "integrity": "sha512-3nQVUAtvkKH9zahfWgw96Jc/uFOmjACE1kQz82E2lqWmHBgjzbNlsC22nuQTfahmWeQtTq5nQ/4Nnd2A1wj4zA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-module-imports": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/helper-module-imports/-/helper-module-imports-7.29.7.tgz",
      "integrity": "sha512-ejHwrQQYcm9xnTivShn2IDOlIzInN34AXskvq9QicvCtEzq1Vzclu/tKF8Jq1Cg8JG2GL6/EmjgsCT7lXepE3g==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/traverse": "^7.29.7",
        "@babel/types": "^7.29.7"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-module-transforms": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/helper-module-transforms/-/helper-module-transforms-7.29.14.tgz",
      "integrity": "sha512-2XDvQVd9JESaOgGc4jT2/jquTAO7ppA5noTZQqH+MmZ89yjCzScLwtCPbWtkPZylL+TLWR49IwWmk+QkRR4GsA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/helper-module-imports": "^7.29.7",
        "@babel/helper-validator-identifier": "^7.29.7",
        "@babel/traverse": "^7.29.14"
      },
      "engines": {
        "node": ">=6.9.0"
      },
      "peerDependencies": {
        "@babel/core": "^7.0.0"
      }
    },
    "node_modules/@babel/helper-plugin-utils": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/helper-plugin-utils/-/helper-plugin-utils-7.29.7.tgz",
      "integrity": "sha512-G7sHYigPY17oO5SYWnfD/0MTBwVR781S/JI643e/JhUYgVgWE/61SoW3NH9KWUKyKq5LVh3npif99Wkt6j86Jw==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-string-parser": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/helper-string-parser/-/helper-string-parser-7.29.14.tgz",
      "integrity": "sha512-CXvKMFvM80ihI7Vox9jb+kQjnyjESQNPM+vB7U0Zz6TWuZA+Z/FL0QPMH300h33qJtIx+REeBhXqWOPW48TcAQ==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-validator-identifier": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/helper-validator-identifier/-/helper-validator-identifier-7.29.7.tgz",
      "integrity": "sha512-qehxGkRj55h/ff8EMaJ+cYhyaKlHIxqYDn682wQD7RNp9UujOQsHog2uS0r2vzr4pW+sXf90NeeayjcNaX3fFg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helper-validator-option": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/helper-validator-option/-/helper-validator-option-7.29.7.tgz",
      "integrity": "sha512-N9ZErrD+yW5geCDtBqnOoxmR8+tNKiGuxKlDpuJxfsqpa2dFcexaziGAE/qoHLiDDreVNMupxGmSoNlyvsA3gw==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/helpers": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/helpers/-/helpers-7.29.14.tgz",
      "integrity": "sha512-6E3Mi1r0B0umq6vmZa4/78q+64fqO2YY0THUdjDPKUCKhNb0yQy5NQIaPHCSD3ryTgiTss//P4JsWS80AsZ6LQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/template": "^7.29.7",
        "@babel/types": "^7.29.14"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/parser": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/parser/-/parser-7.29.14.tgz",
      "integrity": "sha512-VL220vXuyO2z3rNIQwY7ftj9UwpqOt+33sH+sBWkOekbsSR2g7EEMnOx24ZlCH0xOA3DM/33SifVFGbnJD8r/w==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/types": "^7.29.14"
      },
      "bin": {
        "parser": "bin/babel-parser.js"
      },
      "engines": {
        "node": ">=6.0.0"
      }
    },
    "node_modules/@babel/plugin-transform-react-jsx-self": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/plugin-transform-react-jsx-self/-/plugin-transform-react-jsx-self-7.29.7.tgz",
      "integrity": "sha512-TL0hMc9xzy86VD31nUiwzd5otRAcyEPcsegCxolO0PvcXuH1v0kECe/UIznYFihpkvU5wg/jk4v0TTEFfm53fw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/helper-plugin-utils": "^7.29.7"
      },
      "engines": {
        "node": ">=6.9.0"
      },
      "peerDependencies": {
        "@babel/core": "^7.0.0-0"
      }
    },
    "node_modules/@babel/plugin-transform-react-jsx-source": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/plugin-transform-react-jsx-source/-/plugin-transform-react-jsx-source-7.29.7.tgz",
      "integrity": "sha512-06IyK09H3wi4cGbhDBwp5gUGo0IKtnYa8tyTiephirPCK6fbobVGiXMMI5zLQ4aKEYP3wZ3ArU44o+8KMrSG/Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/helper-plugin-utils": "^7.29.7"
      },
      "engines": {
        "node": ">=6.9.0"
      },
      "peerDependencies": {
        "@babel/core": "^7.0.0-0"
      }
    },
    "node_modules/@babel/template": {
      "version": "7.29.7",
      "resolved": "https://registry.npmjs.org/@babel/template/-/template-7.29.7.tgz",
      "integrity": "sha512-puq+Gf35oI24FeN11LkoUQFqv9uwNeWpxXZi/Ji3rRIoKAzKnxRaZ+Gkj0vKS9ZCiTESfng1N9LyOyXvo+m+Gg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/code-frame": "^7.29.7",
        "@babel/parser": "^7.29.7",
        "@babel/types": "^7.29.7"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/traverse": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/traverse/-/traverse-7.29.14.tgz",
      "integrity": "sha512-RAdQJrBgmIH1FWvN0X+1hoDxzMzlP4cv/1Zi4dn4tG1H0cTb+rw7CzUxQJhp1f4ANd8j5vH3fy8BAWFGkBDEsA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/code-frame": "^7.29.7",
        "@babel/generator": "^7.29.14",
        "@babel/helper-globals": "^7.29.7",
        "@babel/parser": "^7.29.14",
        "@babel/template": "^7.29.7",
        "@babel/types": "^7.29.14",
        "debug": "^4.3.1"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@babel/types": {
      "version": "7.29.14",
      "resolved": "https://registry.npmjs.org/@babel/types/-/types-7.29.14.tgz",
      "integrity": "sha512-UjI/dKmwJwT9pOszJitq2G6EObZPnsZUfdCr92z2TlF3EZK9J3EdfOKUrCzHl6wFthbzwhzl1Soc/UIwxRiq6w==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/helper-string-parser": "^7.29.14",
        "@babel/helper-validator-identifier": "^7.29.7"
      },
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/@esbuild/aix-ppc64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/aix-ppc64/-/aix-ppc64-0.25.12.tgz",
      "integrity": "sha512-Hhmwd6CInZ3dwpuGTF8fJG6yoWmsToE+vYgD4nytZVxcu1ulHpUQRAB1UJ8+N1Am3Mz4+xOByoQoSZf4D+CpkA==",
      "cpu": [
        "ppc64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "aix"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/android-arm": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/android-arm/-/android-arm-0.25.12.tgz",
      "integrity": "sha512-VJ+sKvNA/GE7Ccacc9Cha7bpS8nyzVv0jdVgwNDaR4gDMC/2TTRc33Ip8qrNYUcpkOHUT5OZ0bUcNNVZQ9RLlg==",
      "cpu": [
        "arm"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "android"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/android-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/android-arm64/-/android-arm64-0.25.12.tgz",
      "integrity": "sha512-6AAmLG7zwD1Z159jCKPvAxZd4y/VTO0VkprYy+3N2FtJ8+BQWFXU+OxARIwA46c5tdD9SsKGZ/1ocqBS/gAKHg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "android"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/android-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/android-x64/-/android-x64-0.25.12.tgz",
      "integrity": "sha512-5jbb+2hhDHx5phYR2By8GTWEzn6I9UqR11Kwf22iKbNpYrsmRB18aX/9ivc5cabcUiAT/wM+YIZ6SG9QO6a8kg==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "android"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/darwin-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/darwin-arm64/-/darwin-arm64-0.25.12.tgz",
      "integrity": "sha512-N3zl+lxHCifgIlcMUP5016ESkeQjLj/959RxxNYIthIg+CQHInujFuXeWbWMgnTo4cp5XVHqFPmpyu9J65C1Yg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "darwin"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/darwin-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/darwin-x64/-/darwin-x64-0.25.12.tgz",
      "integrity": "sha512-HQ9ka4Kx21qHXwtlTUVbKJOAnmG1ipXhdWTmNXiPzPfWKpXqASVcWdnf2bnL73wgjNrFXAa3yYvBSd9pzfEIpA==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "darwin"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/freebsd-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/freebsd-arm64/-/freebsd-arm64-0.25.12.tgz",
      "integrity": "sha512-gA0Bx759+7Jve03K1S0vkOu5Lg/85dou3EseOGUes8flVOGxbhDDh/iZaoek11Y8mtyKPGF3vP8XhnkDEAmzeg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "freebsd"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/freebsd-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/freebsd-x64/-/freebsd-x64-0.25.12.tgz",
      "integrity": "sha512-TGbO26Yw2xsHzxtbVFGEXBFH0FRAP7gtcPE7P5yP7wGy7cXK2oO7RyOhL5NLiqTlBh47XhmIUXuGciXEqYFfBQ==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "freebsd"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-arm": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-arm/-/linux-arm-0.25.12.tgz",
      "integrity": "sha512-lPDGyC1JPDou8kGcywY0YILzWlhhnRjdof3UlcoqYmS9El818LLfJJc3PXXgZHrHCAKs/Z2SeZtDJr5MrkxtOw==",
      "cpu": [
        "arm"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-arm64/-/linux-arm64-0.25.12.tgz",
      "integrity": "sha512-8bwX7a8FghIgrupcxb4aUmYDLp8pX06rGh5HqDT7bB+8Rdells6mHvrFHHW2JAOPZUbnjUpKTLg6ECyzvas2AQ==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-ia32": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-ia32/-/linux-ia32-0.25.12.tgz",
      "integrity": "sha512-0y9KrdVnbMM2/vG8KfU0byhUN+EFCny9+8g202gYqSSVMonbsCfLjUO+rCci7pM0WBEtz+oK/PIwHkzxkyharA==",
      "cpu": [
        "ia32"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-loong64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-loong64/-/linux-loong64-0.25.12.tgz",
      "integrity": "sha512-h///Lr5a9rib/v1GGqXVGzjL4TMvVTv+s1DPoxQdz7l/AYv6LDSxdIwzxkrPW438oUXiDtwM10o9PmwS/6Z0Ng==",
      "cpu": [
        "loong64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-mips64el": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-mips64el/-/linux-mips64el-0.25.12.tgz",
      "integrity": "sha512-iyRrM1Pzy9GFMDLsXn1iHUm18nhKnNMWscjmp4+hpafcZjrr2WbT//d20xaGljXDBYHqRcl8HnxbX6uaA/eGVw==",
      "cpu": [
        "mips64el"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-ppc64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-ppc64/-/linux-ppc64-0.25.12.tgz",
      "integrity": "sha512-9meM/lRXxMi5PSUqEXRCtVjEZBGwB7P/D4yT8UG/mwIdze2aV4Vo6U5gD3+RsoHXKkHCfSxZKzmDssVlRj1QQA==",
      "cpu": [
        "ppc64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-riscv64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-riscv64/-/linux-riscv64-0.25.12.tgz",
      "integrity": "sha512-Zr7KR4hgKUpWAwb1f3o5ygT04MzqVrGEGXGLnj15YQDJErYu/BGg+wmFlIDOdJp0PmB0lLvxFIOXZgFRrdjR0w==",
      "cpu": [
        "riscv64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-s390x": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-s390x/-/linux-s390x-0.25.12.tgz",
      "integrity": "sha512-MsKncOcgTNvdtiISc/jZs/Zf8d0cl/t3gYWX8J9ubBnVOwlk65UIEEvgBORTiljloIWnBzLs4qhzPkJcitIzIg==",
      "cpu": [
        "s390x"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/linux-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/linux-x64/-/linux-x64-0.25.12.tgz",
      "integrity": "sha512-uqZMTLr/zR/ed4jIGnwSLkaHmPjOjJvnm6TVVitAa08SLS9Z0VM8wIRx7gWbJB5/J54YuIMInDquWyYvQLZkgw==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/netbsd-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/netbsd-arm64/-/netbsd-arm64-0.25.12.tgz",
      "integrity": "sha512-xXwcTq4GhRM7J9A8Gv5boanHhRa/Q9KLVmcyXHCTaM4wKfIpWkdXiMog/KsnxzJ0A1+nD+zoecuzqPmCRyBGjg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "netbsd"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/netbsd-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/netbsd-x64/-/netbsd-x64-0.25.12.tgz",
      "integrity": "sha512-Ld5pTlzPy3YwGec4OuHh1aCVCRvOXdH8DgRjfDy/oumVovmuSzWfnSJg+VtakB9Cm0gxNO9BzWkj6mtO1FMXkQ==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "netbsd"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/openbsd-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/openbsd-arm64/-/openbsd-arm64-0.25.12.tgz",
      "integrity": "sha512-fF96T6KsBo/pkQI950FARU9apGNTSlZGsv1jZBAlcLL1MLjLNIWPBkj5NlSz8aAzYKg+eNqknrUJ24QBybeR5A==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "openbsd"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/openbsd-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/openbsd-x64/-/openbsd-x64-0.25.12.tgz",
      "integrity": "sha512-MZyXUkZHjQxUvzK7rN8DJ3SRmrVrke8ZyRusHlP+kuwqTcfWLyqMOE3sScPPyeIXN/mDJIfGXvcMqCgYKekoQw==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "openbsd"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/openharmony-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/openharmony-arm64/-/openharmony-arm64-0.25.12.tgz",
      "integrity": "sha512-rm0YWsqUSRrjncSXGA7Zv78Nbnw4XL6/dzr20cyrQf7ZmRcsovpcRBdhD43Nuk3y7XIoW2OxMVvwuRvk9XdASg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "openharmony"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/sunos-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/sunos-x64/-/sunos-x64-0.25.12.tgz",
      "integrity": "sha512-3wGSCDyuTHQUzt0nV7bocDy72r2lI33QL3gkDNGkod22EsYl04sMf0qLb8luNKTOmgF/eDEDP5BFNwoBKH441w==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "sunos"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/win32-arm64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/win32-arm64/-/win32-arm64-0.25.12.tgz",
      "integrity": "sha512-rMmLrur64A7+DKlnSuwqUdRKyd3UE7oPJZmnljqEptesKM8wx9J8gx5u0+9Pq0fQQW8vqeKebwNXdfOyP+8Bsg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/win32-ia32": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/win32-ia32/-/win32-ia32-0.25.12.tgz",
      "integrity": "sha512-HkqnmmBoCbCwxUKKNPBixiWDGCpQGVsrQfJoVGYLPT41XWF8lHuE5N6WhVia2n4o5QK5M4tYr21827fNhi4byQ==",
      "cpu": [
        "ia32"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@esbuild/win32-x64": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/@esbuild/win32-x64/-/win32-x64-0.25.12.tgz",
      "integrity": "sha512-alJC0uCZpTFrSL0CCDjcgleBXPnCrEAhTBILpeAp7M/OFgoqtAetfBzX0xM00MUsVVPpVjlPuMbREqnZCXaTnA==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ],
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/@jridgewell/gen-mapping": {
      "version": "0.3.13",
      "resolved": "https://registry.npmjs.org/@jridgewell/gen-mapping/-/gen-mapping-0.3.13.tgz",
      "integrity": "sha512-2kkt/7niJ6MgEPxF0bYdQ6etZaA+fQvDcLKckhy1yIQOzaoKjBBjSj63/aLVjYE3qhRt5dvM+uUyfCg6UKCBbA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@jridgewell/sourcemap-codec": "^1.5.0",
        "@jridgewell/trace-mapping": "^0.3.24"
      }
    },
    "node_modules/@jridgewell/remapping": {
      "version": "2.3.5",
      "resolved": "https://registry.npmjs.org/@jridgewell/remapping/-/remapping-2.3.5.tgz",
      "integrity": "sha512-LI9u/+laYG4Ds1TDKSJW2YPrIlcVYOwi2fUC6xB43lueCjgxV4lffOCZCtYFiH6TNOX+tQKXx97T4IKHbhyHEQ==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@jridgewell/gen-mapping": "^0.3.5",
        "@jridgewell/trace-mapping": "^0.3.24"
      }
    },
    "node_modules/@jridgewell/resolve-uri": {
      "version": "3.1.3",
      "resolved": "https://registry.npmjs.org/@jridgewell/resolve-uri/-/resolve-uri-3.1.3.tgz",
      "integrity": "sha512-lhTtjtoEArWBjismtfS761S9Hlyn+nIZqOZVYoZ1Xn82PMNMVnk+jj/NhC4fAXGTs7ydONcJiR5vyQBTMDu27g==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.0.0"
      }
    },
    "node_modules/@jridgewell/sourcemap-codec": {
      "version": "1.6.0",
      "resolved": "https://registry.npmjs.org/@jridgewell/sourcemap-codec/-/sourcemap-codec-1.6.0.tgz",
      "integrity": "sha512-T7jf+5zgsZHwNJ4lvQ7/aezbyk0nNX+zJVWpmHA7VYsEx7a7qr5Rg5IbtJFqkgze5Y2sruq1RUY8Q837Od7iFw==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/@jridgewell/trace-mapping": {
      "version": "0.3.31",
      "resolved": "https://registry.npmjs.org/@jridgewell/trace-mapping/-/trace-mapping-0.3.31.tgz",
      "integrity": "sha512-zzNR+SdQSDJzc8joaeP8QQoCQr8NuYx2dIIytl1QeBEZHJ9uW6hebsrYgbz8hJwUQao3TWCMtmfV8Nu1twOLAw==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@jridgewell/resolve-uri": "^3.1.0",
        "@jridgewell/sourcemap-codec": "^1.4.14"
      }
    },
    "node_modules/@rolldown/pluginutils": {
      "version": "1.0.0-beta.27",
      "resolved": "https://registry.npmjs.org/@rolldown/pluginutils/-/pluginutils-1.0.0-beta.27.tgz",
      "integrity": "sha512-+d0F4MKMCbeVUJwG96uQ4SgAznZNSq93I3V+9NHA4OpvqG8mRCpGdKmK8l/dl02h2CCDHwW2FqilnTyDcAnqjA==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/@rollup/rollup-android-arm-eabi": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-android-arm-eabi/-/rollup-android-arm-eabi-4.68.4.tgz",
      "integrity": "sha512-8AfICPTDNFpsRltDrWzlX3iVEwHubamhXc10bjOJRCl4eZXj+BE25uCVMYzFpbcqfuxSfrt4VkSVL5QqiHoVDQ==",
      "cpu": [
        "arm"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "android"
      ]
    },
    "node_modules/@rollup/rollup-android-arm64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-android-arm64/-/rollup-android-arm64-4.68.4.tgz",
      "integrity": "sha512-Vs4CxDXeCrCVNZ9jPnicohYT7aX7cxLzjMepkno/7f1zFhC80OUsKHM03BJsusLfYnjKrUe7D8vPvQNdD+rjCg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "android"
      ]
    },
    "node_modules/@rollup/rollup-android-x64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-android-x64/-/rollup-android-x64-4.68.4.tgz",
      "integrity": "sha512-EEytXPqw9k5OIyx1Cou+Xg8XhTwtWW31WIGq1cWTpX05ek6STIprxJ5wY6cOEZAk5sjXM/mE2/JohNhtUiPukg==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "android"
      ]
    },
    "node_modules/@rollup/rollup-darwin-arm64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-darwin-arm64/-/rollup-darwin-arm64-4.68.4.tgz",
      "integrity": "sha512-eRYMGvFdL3TKnaXlacxifKMltpLxehT0XghbV5pPfzJUNCc+psia9rg/scZZPfeDK+VKpSZx9VoMz236yeYEaQ==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "darwin"
      ]
    },
    "node_modules/@rollup/rollup-darwin-x64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-darwin-x64/-/rollup-darwin-x64-4.68.4.tgz",
      "integrity": "sha512-w+E1P+Ul1PgoAiH22tcONSEyx+3VjUcSIZeqr1QSOSC5vvawduMVtgD1ZtfQsPHkpTN/VJekrgN3JL2+YKhaDQ==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "darwin"
      ]
    },
    "node_modules/@rollup/rollup-freebsd-arm64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-freebsd-arm64/-/rollup-freebsd-arm64-4.68.4.tgz",
      "integrity": "sha512-nukgdVNAx7X07qu8nZjJwuCRW/yDjLIio83yXhKUkuUUXTlmEGjVRN+XxrzxnkhiTRfHnqC/OYAga3t7vMQkAw==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "freebsd"
      ]
    },
    "node_modules/@rollup/rollup-freebsd-x64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-freebsd-x64/-/rollup-freebsd-x64-4.68.4.tgz",
      "integrity": "sha512-o5vn+twqv2XwxEpseC76YCNXpCXoRlPmr+ZYpSnmlxbFh5kdHysap9V93cS1hzAXtK6xuXa5kdtS+/8rkUfs4Q==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "freebsd"
      ]
    },
    "node_modules/@rollup/rollup-linux-arm-gnueabihf": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-arm-gnueabihf/-/rollup-linux-arm-gnueabihf-4.68.4.tgz",
      "integrity": "sha512-b2w9laiL8oWofYqGF+ABzin9wNMPxzHRD21EIPJ5A0iniq19d3flE4UP7uZKl1gj+d3Kv/bt1htVUSzI0bE1TQ==",
      "cpu": [
        "arm"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-arm-musleabihf": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-arm-musleabihf/-/rollup-linux-arm-musleabihf-4.68.4.tgz",
      "integrity": "sha512-Ao858LxFnvGrn4qbin3rr+LmEhOrF9pQKgzEnT14f9KjOpytd5smOuj7OOtkejJ7YVb5cBdPpYdT1zjwNcNq9g==",
      "cpu": [
        "arm"
      ],
      "dev": true,
      "libc": [
        "musl"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-arm64-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-arm64-gnu/-/rollup-linux-arm64-gnu-4.68.4.tgz",
      "integrity": "sha512-B78SoWLJeAleSOIDFZ+twb+7DoBla0g73LrdkxlkNwicAsrLfHeYX+NcPlOmxZ3Z8vKVvj0J/kmIMzEaqbQNGg==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-arm64-musl": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-arm64-musl/-/rollup-linux-arm64-musl-4.68.4.tgz",
      "integrity": "sha512-IK0JVhVLGer5AlmgU0eMbfuldxO1wNACZBwyq+ot9UW899hhkWPcO3k0eIrNlqmuWrySvMtzr7GK9xkbNekOTQ==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "libc": [
        "musl"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-loong64-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-loong64-gnu/-/rollup-linux-loong64-gnu-4.68.4.tgz",
      "integrity": "sha512-6MoYXMYy+6eqtVesSNehfsS3LY4A0P8MpHfhcnMkVsWVhaLv0vvuCj2EGkanchIoyHxAmxY+aR3ankaTkokC2A==",
      "cpu": [
        "loong64"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-loong64-musl": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-loong64-musl/-/rollup-linux-loong64-musl-4.68.4.tgz",
      "integrity": "sha512-mo+MC3fHiG4EOdtp7uGSvBv/4hKpqxhV16QGpXn2D0mTBBiOW6hGIOXUK7nck8THlURRWjYdtUPvvJfxHKZ/2w==",
      "cpu": [
        "loong64"
      ],
      "dev": true,
      "libc": [
        "musl"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-ppc64-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-ppc64-gnu/-/rollup-linux-ppc64-gnu-4.68.4.tgz",
      "integrity": "sha512-uBRu6ztHsmoW/10bPsOVbhW8ie4Puw6+CQD/XLRGqr3ugv6pRNm3b4vebiwouhCgkjJ/ItJdqHKJ3mVhpr/wQA==",
      "cpu": [
        "ppc64"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-ppc64-musl": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-ppc64-musl/-/rollup-linux-ppc64-musl-4.68.4.tgz",
      "integrity": "sha512-XNY+QlYWmhqzeiMdOKjziVnx6aP5cXSxbk4rDqbb+MH2IcNiIZWX1wPRfAVV6ntiM17XHJJ2WUNcxnglDNH69A==",
      "cpu": [
        "ppc64"
      ],
      "dev": true,
      "libc": [
        "musl"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-riscv64-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-riscv64-gnu/-/rollup-linux-riscv64-gnu-4.68.4.tgz",
      "integrity": "sha512-agahuMApMIOQgJMNEaJNtQRD+iUllqAvkF/V7WutWaBLTSeMgFd0urEo6m1scFmG0Cd+2zIPvnu2SWcKeuSB1Q==",
      "cpu": [
        "riscv64"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-riscv64-musl": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-riscv64-musl/-/rollup-linux-riscv64-musl-4.68.4.tgz",
      "integrity": "sha512-1N+ysuGmcw/PAC4E5/vpotbjC7zHBMqOMJ+kdjkuvVGbj4Mkzu1/cNI4IMaU27VP6MWded1fQAU9ME2m/JmiUA==",
      "cpu": [
        "riscv64"
      ],
      "dev": true,
      "libc": [
        "musl"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-s390x-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-s390x-gnu/-/rollup-linux-s390x-gnu-4.68.4.tgz",
      "integrity": "sha512-ZO0fVys5Rtut3W0FcpKl/T/CTuTgaujErjanLJHVSmqhWaXnPr1u1VAinHPb8ash+cHA8yMX4QnRbmXhrrXfAg==",
      "cpu": [
        "s390x"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-x64-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-x64-gnu/-/rollup-linux-x64-gnu-4.68.4.tgz",
      "integrity": "sha512-a4T4Ji3Is/TCh4rUuV6ehDdSMp3hxLH/v7mP4VT0kVB90Hfr9GUHVAiJhiJ3iy+tJ4qpvIG4haP+HpivOyWYpw==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "libc": [
        "glibc"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-linux-x64-musl": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-linux-x64-musl/-/rollup-linux-x64-musl-4.68.4.tgz",
      "integrity": "sha512-I4FcDk1+t2w7/JwKakDbhOKvtP07Q8Y/jqaTTdgVklmfqTqmTwtIk254iSP4iCgXgQWThKm8CN67mDzPGDnpqw==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "libc": [
        "musl"
      ],
      "license": "MIT",
      "optional": true,
      "os": [
        "linux"
      ]
    },
    "node_modules/@rollup/rollup-openbsd-x64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-openbsd-x64/-/rollup-openbsd-x64-4.68.4.tgz",
      "integrity": "sha512-kpekXvZs3E/21ps5tUiPbQggi4C8bCx4jBl2eAIaVTerC7eDLWJunZ6nRw3/K4r20E5Z5f1bmbrfSS6H8GRv+A==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "openbsd"
      ]
    },
    "node_modules/@rollup/rollup-openharmony-arm64": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-openharmony-arm64/-/rollup-openharmony-arm64-4.68.4.tgz",
      "integrity": "sha512-mrnM4nr22FidljIL4vGK1htFcxPNCjLK/g2LQvgpBbq8pKL/zHY1czntTDbtYjoaYWVFu+XX0P2r2h/7zwWlIw==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "openharmony"
      ]
    },
    "node_modules/@rollup/rollup-win32-arm64-msvc": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-win32-arm64-msvc/-/rollup-win32-arm64-msvc-4.68.4.tgz",
      "integrity": "sha512-xKF6HKrBdrb3jpGFlGAGtNF6nMu/TGdnVO38durZTMQUaduIqH/aDIjyi7SXmXNDUXTfTMUEIGVwPQ5FuMhCLQ==",
      "cpu": [
        "arm64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ]
    },
    "node_modules/@rollup/rollup-win32-ia32-msvc": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-win32-ia32-msvc/-/rollup-win32-ia32-msvc-4.68.4.tgz",
      "integrity": "sha512-uu45X0jgiXEab6vcoWuPQLqhBPKUCZkXc+gv0oTI78mhn5DnQC2WGvNJ/s6Cnc68PvB/8vAtlE2dIHrpx/wySw==",
      "cpu": [
        "ia32"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ]
    },
    "node_modules/@rollup/rollup-win32-x64-gnu": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-win32-x64-gnu/-/rollup-win32-x64-gnu-4.68.4.tgz",
      "integrity": "sha512-+sVn72CSCbAemlPieSHWpBsJR5vkWH/xQM0Px0loHUhscMNl3CuH0UwTHuzCIaamlv+ZTi1V44jEKQ45FUaNwg==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ]
    },
    "node_modules/@rollup/rollup-win32-x64-msvc": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/@rollup/rollup-win32-x64-msvc/-/rollup-win32-x64-msvc-4.68.4.tgz",
      "integrity": "sha512-/eMzhxPPUGd5vjHdaNtvxHbblmoe+DyvPnJkZ4uQrDlVq9PQSd8br4pWwamycwRcLoNEzXwFy2mzcGoZTYqU5A==",
      "cpu": [
        "x64"
      ],
      "dev": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "win32"
      ]
    },
    "node_modules/@types/babel__core": {
      "version": "7.20.5",
      "resolved": "https://registry.npmjs.org/@types/babel__core/-/babel__core-7.20.5.tgz",
      "integrity": "sha512-qoQprZvz5wQFJwMDqeseRXWv3rqMvhgpbXFfVyWhbx9X47POIA6i/+dXefEmZKoAgOaTdaIgNSMqMIU61yRyzA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/parser": "^7.20.7",
        "@babel/types": "^7.20.7",
        "@types/babel__generator": "*",
        "@types/babel__template": "*",
        "@types/babel__traverse": "*"
      }
    },
    "node_modules/@types/babel__generator": {
      "version": "7.27.0",
      "resolved": "https://registry.npmjs.org/@types/babel__generator/-/babel__generator-7.27.0.tgz",
      "integrity": "sha512-ufFd2Xi92OAVPYsy+P4n7/U7e68fex0+Ee8gSG9KX7eo084CWiQ4sdxktvdl0bOPupXtVJPY19zk6EwWqUQ8lg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/types": "^7.0.0"
      }
    },
    "node_modules/@types/babel__template": {
      "version": "7.4.4",
      "resolved": "https://registry.npmjs.org/@types/babel__template/-/babel__template-7.4.4.tgz",
      "integrity": "sha512-h/NUaSyG5EyxBIp8YRxo4RMe2/qQgvyowRwVMzhYhBCONbW8PUsg4lkFMrhgZhUe5z3L3MiLDuvyJ/CaPa2A8A==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/parser": "^7.1.0",
        "@babel/types": "^7.0.0"
      }
    },
    "node_modules/@types/babel__traverse": {
      "version": "7.28.0",
      "resolved": "https://registry.npmjs.org/@types/babel__traverse/-/babel__traverse-7.28.0.tgz",
      "integrity": "sha512-8PvcXf70gTDZBgt9ptxJ8elBeBjcLOAcOtoO/mPJjtji1+CdGbHgm77om1GrsPxsiE+uXIpNSK64UYaIwQXd4Q==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/types": "^7.28.2"
      }
    },
    "node_modules/@types/estree": {
      "version": "1.0.10",
      "resolved": "https://registry.npmjs.org/@types/estree/-/estree-1.0.10.tgz",
      "integrity": "sha512-lTyOMnOcmRBzdCv6/4zUZCrEhV0xrqQiUZBZtVcbmLHBXKLnN8q89v/wKRF/Q0HbwVsqbIG3o5HDoFl9sduM+g==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/@vitejs/plugin-react": {
      "version": "4.7.0",
      "resolved": "https://registry.npmjs.org/@vitejs/plugin-react/-/plugin-react-4.7.0.tgz",
      "integrity": "sha512-gUu9hwfWvvEDBBmgtAowQCojwZmJ5mcLn3aufeCsitijs3+f2NsrPtlAWIR6OPiqljl96GVCUbLe0HyqIpVaoA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@babel/core": "^7.28.0",
        "@babel/plugin-transform-react-jsx-self": "^7.27.1",
        "@babel/plugin-transform-react-jsx-source": "^7.27.1",
        "@rolldown/pluginutils": "1.0.0-beta.27",
        "@types/babel__core": "^7.20.5",
        "react-refresh": "^0.17.0"
      },
      "engines": {
        "node": "^14.18.0 || >=16.0.0"
      },
      "peerDependencies": {
        "vite": "^4.2.0 || ^5.0.0 || ^6.0.0 || ^7.0.0"
      }
    },
    "node_modules/baseline-browser-mapping": {
      "version": "2.11.37",
      "resolved": "https://registry.npmjs.org/baseline-browser-mapping/-/baseline-browser-mapping-2.11.37.tgz",
      "integrity": "sha512-QYph+vfurmaxhNwi9FprfmtkT5cx2Keg8FzZ0vnjO8ukscIybVyZGMFwdllKGlTI6qyU8ck43OM9UXh+IFJihQ==",
      "dev": true,
      "license": "Apache-2.0",
      "bin": {
        "baseline-browser-mapping": "dist/cli.cjs"
      },
      "engines": {
        "node": ">=6.0.0"
      }
    },
    "node_modules/browserslist": {
      "version": "4.29.5",
      "resolved": "https://registry.npmjs.org/browserslist/-/browserslist-4.29.5.tgz",
      "integrity": "sha512-khTmKu5NGYTotqcYo8Bew2k4RAE9PuXRSFRXG2lQBrCEQHyeQ/vX6gKMxfokwywBsLiXZMnHsmWMG1pnO8J7uQ==",
      "dev": true,
      "funding": [
        {
          "type": "opencollective",
          "url": "https://opencollective.com/browserslist"
        },
        {
          "type": "tidelift",
          "url": "https://tidelift.com/funding/github/npm/browserslist"
        },
        {
          "type": "github",
          "url": "https://github.com/sponsors/ai"
        }
      ],
      "license": "MIT",
      "dependencies": {
        "baseline-browser-mapping": "^2.11.33",
        "caniuse-lite": "^1.0.30001819",
        "electron-to-chromium": "^1.5.464",
        "node-releases": "^2.0.60",
        "update-browserslist-db": "^1.3.4"
      },
      "bin": {
        "browserslist": "cli.js"
      },
      "engines": {
        "node": "^6 || ^7 || ^8 || ^9 || ^10 || ^11 || ^12 || >=13.7"
      }
    },
    "node_modules/caniuse-lite": {
      "version": "1.0.30001826",
      "resolved": "https://registry.npmjs.org/caniuse-lite/-/caniuse-lite-1.0.30001826.tgz",
      "integrity": "sha512-wTB+9tIbLro/BCXwLuNV1meHE4L9dS6dArMDwgYfNBPMYScqClHyGunhkCSDtHhe/69Xl0sHlKgBVZ+fNsYtWA==",
      "dev": true,
      "funding": [
        {
          "type": "opencollective",
          "url": "https://opencollective.com/browserslist"
        },
        {
          "type": "tidelift",
          "url": "https://tidelift.com/funding/github/npm/caniuse-lite"
        },
        {
          "type": "github",
          "url": "https://github.com/sponsors/ai"
        }
      ],
      "license": "CC-BY-4.0"
    },
    "node_modules/convert-source-map": {
      "version": "2.0.0",
      "resolved": "https://registry.npmjs.org/convert-source-map/-/convert-source-map-2.0.0.tgz",
      "integrity": "sha512-Kvp459HrV2FEJ1CAsi1Ku+MY3kasH19TFykTz2xWmMeq6bk2NU3XXvfJ+Q61m0xktWwt+1HSYf3JZsTms3aRJg==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/debug": {
      "version": "4.4.3",
      "resolved": "https://registry.npmjs.org/debug/-/debug-4.4.3.tgz",
      "integrity": "sha512-RGwwWnwQvkVfavKVt22FGLw+xYSdzARwm0ru6DhTVA3umU5hZc28V3kO4stgYryrTlLpuvgI9GiijltAjNbcqA==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "ms": "^2.1.3"
      },
      "engines": {
        "node": ">=6.0"
      },
      "peerDependenciesMeta": {
        "supports-color": {
          "optional": true
        }
      }
    },
    "node_modules/electron-to-chromium": {
      "version": "1.5.486",
      "resolved": "https://registry.npmjs.org/electron-to-chromium/-/electron-to-chromium-1.5.486.tgz",
      "integrity": "sha512-rpybw4Ac4ak/An6G1Me2+61xzwobowDGyuJe71QHHOVpkZv4fPVobsLl7WH6zHSKusAMpaHxGKTpOvpy7+r3dQ==",
      "dev": true,
      "license": "ISC"
    },
    "node_modules/esbuild": {
      "version": "0.25.12",
      "resolved": "https://registry.npmjs.org/esbuild/-/esbuild-0.25.12.tgz",
      "integrity": "sha512-bbPBYYrtZbkt6Os6FiTLCTFxvq4tt3JKall1vRwshA3fdVztsLAatFaZobhkBC8/BrPetoa0oksYoKXoG4ryJg==",
      "dev": true,
      "hasInstallScript": true,
      "license": "MIT",
      "bin": {
        "esbuild": "bin/esbuild"
      },
      "engines": {
        "node": ">=18"
      },
      "optionalDependencies": {
        "@esbuild/aix-ppc64": "0.25.12",
        "@esbuild/android-arm": "0.25.12",
        "@esbuild/android-arm64": "0.25.12",
        "@esbuild/android-x64": "0.25.12",
        "@esbuild/darwin-arm64": "0.25.12",
        "@esbuild/darwin-x64": "0.25.12",
        "@esbuild/freebsd-arm64": "0.25.12",
        "@esbuild/freebsd-x64": "0.25.12",
        "@esbuild/linux-arm": "0.25.12",
        "@esbuild/linux-arm64": "0.25.12",
        "@esbuild/linux-ia32": "0.25.12",
        "@esbuild/linux-loong64": "0.25.12",
        "@esbuild/linux-mips64el": "0.25.12",
        "@esbuild/linux-ppc64": "0.25.12",
        "@esbuild/linux-riscv64": "0.25.12",
        "@esbuild/linux-s390x": "0.25.12",
        "@esbuild/linux-x64": "0.25.12",
        "@esbuild/netbsd-arm64": "0.25.12",
        "@esbuild/netbsd-x64": "0.25.12",
        "@esbuild/openbsd-arm64": "0.25.12",
        "@esbuild/openbsd-x64": "0.25.12",
        "@esbuild/openharmony-arm64": "0.25.12",
        "@esbuild/sunos-x64": "0.25.12",
        "@esbuild/win32-arm64": "0.25.12",
        "@esbuild/win32-ia32": "0.25.12",
        "@esbuild/win32-x64": "0.25.12"
      }
    },
    "node_modules/escalade": {
      "version": "3.2.0",
      "resolved": "https://registry.npmjs.org/escalade/-/escalade-3.2.0.tgz",
      "integrity": "sha512-WUj2qlxaQtO4g6Pq5c29GTcWGDyd8itL8zTlipgECz3JesAiiOKotd8JU6otB3PACgG6xkJUyVhboMS+bje/jA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6"
      }
    },
    "node_modules/fdir": {
      "version": "6.5.0",
      "resolved": "https://registry.npmjs.org/fdir/-/fdir-6.5.0.tgz",
      "integrity": "sha512-tIbYtZbucOs0BRGqPJkshJUYdL+SDH7dVM8gjy+ERp3WAUjLEFJE+02kanyHtwjWOnwrKYBiwAmM0p4kLJAnXg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=12.0.0"
      },
      "peerDependencies": {
        "picomatch": "^3 || ^4"
      },
      "peerDependenciesMeta": {
        "picomatch": {
          "optional": true
        }
      }
    },
    "node_modules/fsevents": {
      "version": "2.3.3",
      "resolved": "https://registry.npmjs.org/fsevents/-/fsevents-2.3.3.tgz",
      "integrity": "sha512-5xoDfX+fL7faATnagmWPpbFtwh/R77WmMMqqHGS65C3vvB0YHrgF+B1YmZ3441tMj5n63k0212XNoJwzlhffQw==",
      "dev": true,
      "hasInstallScript": true,
      "license": "MIT",
      "optional": true,
      "os": [
        "darwin"
      ],
      "engines": {
        "node": "^8.16.0 || ^10.6.0 || >=11.0.0"
      }
    },
    "node_modules/gensync": {
      "version": "1.0.0-beta.2",
      "resolved": "https://registry.npmjs.org/gensync/-/gensync-1.0.0-beta.2.tgz",
      "integrity": "sha512-3hN7NaskYvMDLQY55gnW3NQ+mesEAepTqlg+VEbj7zzqEMBVNhzcGYYeqFo/TlYz6eQiFcp1HcsCZO+nGgS8zg==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=6.9.0"
      }
    },
    "node_modules/js-tokens": {
      "version": "4.0.0",
      "resolved": "https://registry.npmjs.org/js-tokens/-/js-tokens-4.0.0.tgz",
      "integrity": "sha512-RdJUflcE3cUzKiMqQgsCu06FPu9UdIJO0beYbPhHN4k6apgJtifcoCtT9bcxOpYBtpD2kCM6Sbzg4CausW/PKQ==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/jsesc": {
      "version": "3.1.0",
      "resolved": "https://registry.npmjs.org/jsesc/-/jsesc-3.1.0.tgz",
      "integrity": "sha512-/sM3dO2FOzXjKQhJuo0Q173wf2KOo8t4I8vHy6lF9poUp7bKT0/NHE8fPX23PwfhnykfqnC2xRxOnVw5XuGIaA==",
      "dev": true,
      "license": "MIT",
      "bin": {
        "jsesc": "bin/jsesc"
      },
      "engines": {
        "node": ">=6"
      }
    },
    "node_modules/json5": {
      "version": "2.2.3",
      "resolved": "https://registry.npmjs.org/json5/-/json5-2.2.3.tgz",
      "integrity": "sha512-XmOWe7eyHYH14cLdVPoyg+GOH3rYX++KpzrylJwSW98t3Nk+U8XOl8FWKOgwtzdb8lXGf6zYwDUzeHMWfxasyg==",
      "dev": true,
      "license": "MIT",
      "bin": {
        "json5": "lib/cli.js"
      },
      "engines": {
        "node": ">=6"
      }
    },
    "node_modules/lru-cache": {
      "version": "5.1.1",
      "resolved": "https://registry.npmjs.org/lru-cache/-/lru-cache-5.1.1.tgz",
      "integrity": "sha512-KpNARQA3Iwv+jTA0utUVVbrh+Jlrr1Fv0e56GGzAFOXN7dk/FviaDW8LHmK52DlcH4WP2n6gI8vN1aesBFgo9w==",
      "dev": true,
      "license": "ISC",
      "dependencies": {
        "yallist": "^3.0.2"
      }
    },
    "node_modules/ms": {
      "version": "2.1.3",
      "resolved": "https://registry.npmjs.org/ms/-/ms-2.1.3.tgz",
      "integrity": "sha512-6FlzubTLZG3J2a/NVCAleEhjzq5oxgHyaCU9yYXvcLsvoVaHJq/s5xXI6/XXP6tz7R9xAOtHnSO/tXtF3WRTlA==",
      "dev": true,
      "license": "MIT"
    },
    "node_modules/nanoid": {
      "version": "3.3.20",
      "resolved": "https://registry.npmjs.org/nanoid/-/nanoid-3.3.20.tgz",
      "integrity": "sha512-uKdg2G3GNCKQn9byYOpxbGqrT2fGO5KRt5J/8b3pok8rT6qxGWF6hxMyJiEYtAf+FVyYuD9hRaDqX5uPFYJ4ZQ==",
      "dev": true,
      "funding": [
        {
          "type": "github",
          "url": "https://github.com/sponsors/ai"
        }
      ],
      "license": "MIT",
      "bin": {
        "nanoid": "bin/nanoid.cjs"
      },
      "engines": {
        "node": "^10 || ^12 || ^13.7 || ^14 || >=15.0.1"
      }
    },
    "node_modules/node-releases": {
      "version": "2.0.64",
      "resolved": "https://registry.npmjs.org/node-releases/-/node-releases-2.0.64.tgz",
      "integrity": "sha512-zAPCKqMcnqs2nGy6nlkm7JduFseiBJy3EXHGLh+8pFR+TxGCBxxiEN21+UROU5CKwX/f4TlUcYjiM+vs/tsyxw==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=18"
      }
    },
    "node_modules/picocolors": {
      "version": "1.1.1",
      "resolved": "https://registry.npmjs.org/picocolors/-/picocolors-1.1.1.tgz",
      "integrity": "sha512-xceH2snhtb5M9liqDsmEw56le376mTZkEX/jEb/RxNFyegNul7eNslCXP9FDj/Lcu0X8KEyMceP2ntpaHrDEVA==",
      "dev": true,
      "license": "ISC"
    },
    "node_modules/picomatch": {
      "version": "4.0.7",
      "resolved": "https://registry.npmjs.org/picomatch/-/picomatch-4.0.7.tgz",
      "integrity": "sha512-qcJu88Q2IWqJsDD529JKMdwGm/dvInW4HvQnRwiH9JtihJvzGOscDtHE3x1pBKeUOTysQ8kVmLnJ2kJu7yhcGA==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=12"
      },
      "funding": {
        "url": "https://github.com/sponsors/jonschlinkert"
      }
    },
    "node_modules/postcss": {
      "version": "8.5.33",
      "resolved": "https://registry.npmjs.org/postcss/-/postcss-8.5.33.tgz",
      "integrity": "sha512-RVHEU9/Pp1Fnjr/MpAJsZoADukCKhfIi15OunEYiGktIkYLkPVvC1sioWolGDS7XByMTx9m/IGbro0Js13zhnQ==",
      "dev": true,
      "funding": [
        {
          "type": "opencollective",
          "url": "https://opencollective.com/postcss/"
        },
        {
          "type": "tidelift",
          "url": "https://tidelift.com/funding/github/npm/postcss"
        },
        {
          "type": "github",
          "url": "https://github.com/sponsors/ai"
        }
      ],
      "license": "MIT",
      "dependencies": {
        "nanoid": "^3.3.20",
        "picocolors": "^1.1.1",
        "source-map-js": "^1.2.2"
      },
      "engines": {
        "node": "^10 || ^12 || >=14"
      }
    },
    "node_modules/react": {
      "version": "19.3.1",
      "resolved": "https://registry.npmjs.org/react/-/react-19.3.1.tgz",
      "integrity": "sha512-QNhWdt7nrgVWWQG58QuGsAR3jv3Hbe4mb4vz7w2csnHavciZJgda1CPNaldF6eVxv90ud3jUXpKlHL69gO38yw==",
      "license": "MIT",
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/react-dom": {
      "version": "19.3.1",
      "resolved": "https://registry.npmjs.org/react-dom/-/react-dom-19.3.1.tgz",
      "integrity": "sha512-Y0bARz53lhEC1NMCASs0/j+faB/zqt3R8hpvg1e8+61ZVJ4UL0UdC4cBbXfZMcIuhrfgeILTLe+ZeBCavQPtKQ==",
      "license": "MIT",
      "dependencies": {
        "scheduler": "^0.28.0"
      },
      "peerDependencies": {
        "react": "^19.3.1"
      }
    },
    "node_modules/react-refresh": {
      "version": "0.17.0",
      "resolved": "https://registry.npmjs.org/react-refresh/-/react-refresh-0.17.0.tgz",
      "integrity": "sha512-z6F7K9bV85EfseRCp2bzrpyQ0Gkw1uLoCel9XBVWPg/TjRj94SkJzUTGfOa4bs7iJvBWtQG0Wq7wnI0syw3EBQ==",
      "dev": true,
      "license": "MIT",
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/rollup": {
      "version": "4.68.4",
      "resolved": "https://registry.npmjs.org/rollup/-/rollup-4.68.4.tgz",
      "integrity": "sha512-5acdO1/M9urvHQuZVkoOt8JWQH6XxCnFnox1qodE39qkXaML67S2E7MhHDgx3pQYycM1A5dRJwahYMDLO9a3Yg==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "@types/estree": "^1.0.10"
      },
      "bin": {
        "rollup": "dist/bin/rollup"
      },
      "engines": {
        "node": ">=18.0.0",
        "npm": ">=8.0.0"
      },
      "optionalDependencies": {
        "@rollup/rollup-android-arm-eabi": "4.68.4",
        "@rollup/rollup-android-arm64": "4.68.4",
        "@rollup/rollup-android-x64": "4.68.4",
        "@rollup/rollup-darwin-arm64": "4.68.4",
        "@rollup/rollup-darwin-x64": "4.68.4",
        "@rollup/rollup-freebsd-arm64": "4.68.4",
        "@rollup/rollup-freebsd-x64": "4.68.4",
        "@rollup/rollup-linux-arm-gnueabihf": "4.68.4",
        "@rollup/rollup-linux-arm-musleabihf": "4.68.4",
        "@rollup/rollup-linux-arm64-gnu": "4.68.4",
        "@rollup/rollup-linux-arm64-musl": "4.68.4",
        "@rollup/rollup-linux-loong64-gnu": "4.68.4",
        "@rollup/rollup-linux-loong64-musl": "4.68.4",
        "@rollup/rollup-linux-ppc64-gnu": "4.68.4",
        "@rollup/rollup-linux-ppc64-musl": "4.68.4",
        "@rollup/rollup-linux-riscv64-gnu": "4.68.4",
        "@rollup/rollup-linux-riscv64-musl": "4.68.4",
        "@rollup/rollup-linux-s390x-gnu": "4.68.4",
        "@rollup/rollup-linux-x64-gnu": "4.68.4",
        "@rollup/rollup-linux-x64-musl": "4.68.4",
        "@rollup/rollup-openbsd-x64": "4.68.4",
        "@rollup/rollup-openharmony-arm64": "4.68.4",
        "@rollup/rollup-win32-arm64-msvc": "4.68.4",
        "@rollup/rollup-win32-ia32-msvc": "4.68.4",
        "@rollup/rollup-win32-x64-gnu": "4.68.4",
        "@rollup/rollup-win32-x64-msvc": "4.68.4",
        "fsevents": "~2.3.2"
      }
    },
    "node_modules/scheduler": {
      "version": "0.28.0",
      "resolved": "https://registry.npmjs.org/scheduler/-/scheduler-0.28.0.tgz",
      "integrity": "sha512-juorfCmIkIw8tT+p5BXSm6PJjQF/ycEYmKyzURCIt/RaZIhL+PulbQ9Yu2z1HdOJDdqDTlxA1+xKBmHXJsczAw==",
      "license": "MIT"
    },
    "node_modules/semver": {
      "version": "6.3.2",
      "resolved": "https://registry.npmjs.org/semver/-/semver-6.3.2.tgz",
      "integrity": "sha512-GYrCADwpu6yV5OAvchkQ27Z5wwMxWAEMg5NNWjyHh/5DboFFqbdbvYNIvZouQpxXyeMPoTOh0vdPYKlf0ZswdQ==",
      "dev": true,
      "license": "ISC",
      "bin": {
        "semver": "bin/semver.js"
      }
    },
    "node_modules/source-map-js": {
      "version": "1.2.2",
      "resolved": "https://registry.npmjs.org/source-map-js/-/source-map-js-1.2.2.tgz",
      "integrity": "sha512-KGj/8Y43x35aZVDtt+J4mK1hoLGHULMYfSkODJNQjNDC3oW1PqPoxMwo0pLUsWM/UEGzON/NxeHywEfNXNP3Vw==",
      "dev": true,
      "license": "BSD-3-Clause",
      "engines": {
        "node": ">=0.10.0"
      }
    },
    "node_modules/tinyglobby": {
      "version": "0.2.17",
      "resolved": "https://registry.npmjs.org/tinyglobby/-/tinyglobby-0.2.17.tgz",
      "integrity": "sha512-wXR/dYpcqKmfWpEdZjiKJOwCNFndD0DMnrW/cYjVGttEkBfVgcLFHoNrlj47mjOVic9yyNu65alsgF4NQyTa2g==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "fdir": "^6.5.0",
        "picomatch": "^4.0.4"
      },
      "engines": {
        "node": ">=12.0.0"
      },
      "funding": {
        "url": "https://github.com/sponsors/SuperchupuDev"
      }
    },
    "node_modules/update-browserslist-db": {
      "version": "1.3.4",
      "resolved": "https://registry.npmjs.org/update-browserslist-db/-/update-browserslist-db-1.3.4.tgz",
      "integrity": "sha512-t8wonIOdQyOjtPd2VktlBu6QjTVTks6v8bSi7kd0r2Vll8TK1JYDZtRRdbmCHOAMphkZgrzK+L+vXK4pyZ8t4A==",
      "dev": true,
      "funding": [
        {
          "type": "opencollective",
          "url": "https://opencollective.com/browserslist"
        },
        {
          "type": "tidelift",
          "url": "https://tidelift.com/funding/github/npm/browserslist"
        },
        {
          "type": "github",
          "url": "https://github.com/sponsors/ai"
        }
      ],
      "license": "MIT",
      "dependencies": {
        "escalade": "^3.2.0",
        "picocolors": "^1.1.1"
      },
      "bin": {
        "update-browserslist-db": "cli.js"
      },
      "peerDependencies": {
        "browserslist": ">= 4.21.0"
      }
    },
    "node_modules/vite": {
      "version": "6.4.6",
      "resolved": "https://registry.npmjs.org/vite/-/vite-6.4.6.tgz",
      "integrity": "sha512-RtsraAwEG68idOA6iUOoIpOlcSKROnf815aIq9qHTSrDNpLoBrBa12suyfcEd8qn0xwOVQI1Yhm1GbmTjF600A==",
      "dev": true,
      "license": "MIT",
      "dependencies": {
        "esbuild": "^0.25.0",
        "fdir": "^6.4.4",
        "picomatch": "^4.0.2",
        "postcss": "^8.5.3",
        "rollup": "^4.34.9",
        "tinyglobby": "^0.2.13"
      },
      "bin": {
        "vite": "bin/vite.js"
      },
      "engines": {
        "node": "^18.0.0 || ^20.0.0 || >=22.0.0"
      },
      "funding": {
        "url": "https://github.com/vitejs/vite?sponsor=1"
      },
      "optionalDependencies": {
        "fsevents": "~2.3.3"
      },
      "peerDependencies": {
        "@types/node": "^18.0.0 || ^20.0.0 || >=22.0.0",
        "jiti": ">=1.21.0",
        "less": "*",
        "lightningcss": "^1.21.0",
        "sass": "*",
        "sass-embedded": "*",
        "stylus": "*",
        "sugarss": "*",
        "terser": "^5.16.0",
        "tsx": "^4.8.1",
        "yaml": "^2.4.2"
      },
      "peerDependenciesMeta": {
        "@types/node": {
          "optional": true
        },
        "jiti": {
          "optional": true
        },
        "less": {
          "optional": true
        },
        "lightningcss": {
          "optional": true
        },
        "sass": {
          "optional": true
        },
        "sass-embedded": {
          "optional": true
        },
        "stylus": {
          "optional": true
        },
        "sugarss": {
          "optional": true
        },
        "terser": {
          "optional": true
        },
        "tsx": {
          "optional": true
        },
        "yaml": {
          "optional": true
        }
      }
    },
    "node_modules/yallist": {
      "version": "3.1.1",
      "resolved": "https://registry.npmjs.org/yallist/-/yallist-3.1.1.tgz",
      "integrity": "sha512-a4UGQaWPH59mOXUYnAG2ewncQS4i4F43Tv3JoAM+s2VDAmS9NsK8GpDMLrCHPksFT7h3K6TOoUNn2pb7RoXx4g==",
      "dev": true,
      "license": "ISC"
    }
  }
}
```

### File: `frontend/vehicle-image-dashboard/package.json`

```json
{
  "name": "vehicle-image-dashboard",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^4.3.4",
    "vite": "^6.2.0"
  }
}
```

### File: `frontend/vehicle-image-dashboard/src/App.jsx`

```jsx
import React from 'react';
import DashboardPage from './pages/DashboardPage';

export default function App() {
  return (
    <div>
      <header className="app-header">
        <div className="app-header-inner">
          <div className="brand-group">
            <h1 className="brand-title">VehicleImageSystem</h1>
            <p className="brand-subtitle">
              Multi-Source Vehicle Image Retrieval, Validation, Internal Storage &amp; Delivery
            </p>
          </div>
          <div className="security-summary-bar" aria-label="Active pipeline protections">
            <span className="security-chip">SSRF ConnectGuard</span>
            <span className="security-chip">20 MB Stream Cap</span>
            <span className="security-chip">Magic-Byte &amp; Pixel Decode</span>
            <span className="security-chip">Immutable Internal Delivery</span>
          </div>
        </div>
      </header>
      <DashboardPage />
    </div>
  );
}
```

### File: `frontend/vehicle-image-dashboard/src/components/ImagePlaceholder.jsx`

```jsx
import React from 'react';

export default function ImagePlaceholder({ reason }) {
  return (
    <div className="image-placeholder" role="img" aria-label="No valid stored vehicle image">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75">
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <circle cx="8.5" cy="10.5" r="1.5" />
        <path d="M21 15l-5-5L5 21" />
      </svg>
      <span>{reason || 'Stored image unavailable'}</span>
    </div>
  );
}
```

### File: `frontend/vehicle-image-dashboard/src/components/ImageReplaceForm.jsx`

```jsx
import React, { useState } from 'react';
import { replaceImage } from '../services/vehicleApi';

export default function ImageReplaceForm({ vehicleId, onReplaced, onCancel }) {
  const [newUrl, setNewUrl] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!newUrl.trim()) return;

    setSubmitting(true);
    setError(null);

    try {
      const result = await replaceImage(vehicleId, newUrl.trim());
      setNewUrl('');
      onReplaced(result);
    } catch (err) {
      setError({
        code: err.code || 'REPLACEMENT_FAILED',
        detail: err.detail || err.message || 'Replacement failed. The existing stored image was preserved.',
        retryable: Boolean(err.retryable)
      });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="replace-drawer" onSubmit={handleSubmit}>
      <label className="field-label" htmlFor={`replace-url-${vehicleId}`}>
        Replace Supplier Image URL
      </label>
      <input
        id={`replace-url-${vehicleId}`}
        type="url"
        required
        className="field-input field-input-mono"
        placeholder="https://supplier.example.com/new-photo.jpg"
        value={newUrl}
        onChange={(e) => setNewUrl(e.target.value)}
        disabled={submitting}
      />
      <div className="replace-actions">
        <button
          id={`submit-replace-${vehicleId}`}
          type="submit"
          className="btn-primary"
          disabled={submitting}
        >
          {submitting ? 'Validating & Replacing...' : 'Commit Replacement'}
        </button>
        <button
          id={`cancel-replace-${vehicleId}`}
          type="button"
          className="btn-secondary"
          onClick={onCancel}
          disabled={submitting}
        >
          Cancel
        </button>
      </div>

      {error && (
        <div className="feedback-banner feedback-banner-error" role="alert">
          <div className="feedback-code-header">
            <span className="error-code-pill">{error.code}</span>
            <span>Existing image preserved</span>
          </div>
          <p className="feedback-detail">{error.detail}</p>
        </div>
      )}
    </form>
  );
}
```

### File: `frontend/vehicle-image-dashboard/src/components/StatusBadge.jsx`

```jsx
import React from 'react';

export default function StatusBadge({ status }) {
  const normalized = (status || 'Completed').toLowerCase();

  let badgeClass = 'status-badge status-badge-completed';
  if (normalized === 'processing') {
    badgeClass = 'status-badge status-badge-processing';
  } else if (normalized === 'failed') {
    badgeClass = 'status-badge status-badge-failed';
  }

  return <span className={badgeClass}>{status || 'Completed'}</span>;
}
```

### File: `frontend/vehicle-image-dashboard/src/components/VehicleCard.jsx`

```jsx
import React, { useState } from 'react';
import StatusBadge from './StatusBadge';
import ImagePlaceholder from './ImagePlaceholder';
import ImageReplaceForm from './ImageReplaceForm';
import { resolveInternalImageUrl } from '../services/vehicleApi';

function formatBytes(bytes) {
  if (!bytes || bytes <= 0) return null;
  if (bytes < 1024) return `${bytes} B`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} KB`;
  return `${(kb / 1024).toFixed(2)} MB`;
}

export default function VehicleCard({ vehicle, onUpdated }) {
  const [imageLoadFailed, setImageLoadFailed] = useState(false);
  const [showReplaceForm, setShowReplaceForm] = useState(false);

  const internalImageSrc = resolveInternalImageUrl(vehicle.imageUrl);
  const downloadHref = internalImageSrc ? `${internalImageSrc}/download` : null;

  function handleReplaced() {
    setShowReplaceForm(false);
    setImageLoadFailed(false);
    if (onUpdated) {
      onUpdated();
    }
  }

  return (
    <article className="vehicle-card" id={`vehicle-card-${vehicle.id}`}>
      <div className="vehicle-media-frame">
        {internalImageSrc && !imageLoadFailed ? (
          <img
            src={internalImageSrc}
            alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
            loading="lazy"
            decoding="async"
            className="vehicle-image"
            onError={() => setImageLoadFailed(true)}
          />
        ) : (
          <ImagePlaceholder reason="Stored image unavailable" />
        )}
      </div>

      <div className="vehicle-card-body">
        <div className="vehicle-card-top">
          <div>
            <h3 className="vehicle-title">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h3>
            {vehicle.variant && <p className="vehicle-variant">{vehicle.variant}</p>}
          </div>
          <StatusBadge status={vehicle.imageStatus} />
        </div>

        <div className="metadata-specs">
          {vehicle.format && <span className="spec-tag">{vehicle.format}</span>}
          {vehicle.width && vehicle.height && (
            <span className="spec-tag">
              {vehicle.width}&times;{vehicle.height}px
            </span>
          )}
          {vehicle.fileSize && <span className="spec-tag">{formatBytes(vehicle.fileSize)}</span>}
          {vehicle.retrievalStrategy && (
            <span className="spec-tag">Strategy: {vehicle.retrievalStrategy}</span>
          )}
        </div>

        {vehicle.imageUrl && (
          <div className="internal-route-box" title={`Served internally from ${vehicle.imageUrl}`}>
            Internal API: {vehicle.imageUrl}
          </div>
        )}

        <div className="vehicle-card-actions">
          {downloadHref ? (
            <a
              id={`download-image-${vehicle.id}`}
              href={downloadHref}
              className="btn-secondary"
              download
            >
              Download Stored Copy
            </a>
          ) : (
            <span />
          )}

          <button
            id={`toggle-replace-${vehicle.id}`}
            type="button"
            className="btn-secondary"
            onClick={() => setShowReplaceForm((prev) => !prev)}
          >
            {showReplaceForm ? 'Close Replace' : 'Replace Image'}
          </button>
        </div>

        {showReplaceForm && (
          <ImageReplaceForm
            vehicleId={vehicle.id}
            onReplaced={handleReplaced}
            onCancel={() => setShowReplaceForm(false)}
          />
        )}
      </div>
    </article>
  );
}
```

### File: `frontend/vehicle-image-dashboard/src/components/VehicleForm.jsx`

```jsx
import React, { useState } from 'react';
import { createVehicle } from '../services/vehicleApi';

const DEMO_PRESETS = [
  {
    id: 'preset-vehicle-a',
    label: 'Vehicle A: Webpage URL',
    make: 'Porsche',
    model: '911 Carrera',
    variant: 'GTS Cabriolet',
    year: 2026,
    imageUrl: 'https://en.wikipedia.org/wiki/Porsche_911'
  },
  {
    id: 'preset-vehicle-b',
    label: 'Vehicle B: Direct Image',
    make: 'BMW',
    model: 'M3',
    variant: 'Competition xDrive',
    year: 2026,
    imageUrl: 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/39/2021_BMW_M3_Competition_Automatic_3.0_Front.jpg/960px-2021_BMW_M3_Competition_Automatic_3.0_Front.jpg'
  },
  {
    id: 'preset-vehicle-c',
    label: 'Vehicle C: SSRF / Blocked URL',
    make: 'Audi',
    model: 'RS6 Avant',
    variant: 'Performance',
    year: 2026,
    imageUrl: 'http://169.254.169.254/latest/meta-data/'
  }
];

export default function VehicleForm({ onVehicleCreated }) {
  const [make, setMake] = useState('');
  const [model, setModel] = useState('');
  const [variant, setVariant] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [imageUrl, setImageUrl] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState(null);

  function applyPreset(preset) {
    setMake(preset.make);
    setModel(preset.model);
    setVariant(preset.variant);
    setYear(preset.year);
    setImageUrl(preset.imageUrl);
    setFeedback(null);
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setSubmitting(true);
    setFeedback(null);

    try {
      const result = await createVehicle({
        make: make.trim(),
        model: model.trim(),
        variant: variant.trim() || null,
        year: Number(year),
        imageUrl: imageUrl.trim()
      });

      setFeedback({
        type: 'success',
        code: result.status,
        detail: `Stored internally at ${result.imageUrl} (Vehicle ID: ${result.vehicleId}).`
      });

      setMake('');
      setModel('');
      setVariant('');
      setImageUrl('');

      if (onVehicleCreated) {
        onVehicleCreated(result);
      }
    } catch (err) {
      setFeedback({
        type: 'error',
        code: err.code || 'INGESTION_FAILED',
        detail: err.detail || err.message || 'Image ingestion failed.',
        traceId: err.traceId,
        retryable: err.retryable
      });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <aside className="panel panel-sticky" aria-labelledby="ingest-form-heading">
      <h2 id="ingest-form-heading" className="panel-title">
        Submit Supplier Vehicle &amp; Image URL
      </h2>
      <p className="panel-description">
        Fetches via SSRF-guarded streaming, validates magic bytes &amp; decoded dimensions, and persists internally.
      </p>

      <div className="preset-group">
        <span className="preset-group-label">Quick Verification Presets</span>
        <div className="preset-buttons">
          {DEMO_PRESETS.map((preset) => (
            <button
              key={preset.id}
              id={preset.id}
              type="button"
              className="btn-preset"
              onClick={() => applyPreset(preset)}
            >
              {preset.label}
            </button>
          ))}
        </div>
      </div>

      <form id="vehicle-ingestion-form" className="form-grid" onSubmit={handleSubmit}>
        <div className="form-row-split">
          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-make">
              Make
            </label>
            <input
              id="vehicle-make"
              type="text"
              required
              maxLength={100}
              className="field-input"
              placeholder="e.g. Porsche"
              value={make}
              onChange={(e) => setMake(e.target.value)}
              disabled={submitting}
            />
          </div>

          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-model">
              Model
            </label>
            <input
              id="vehicle-model"
              type="text"
              required
              maxLength={100}
              className="field-input"
              placeholder="e.g. 911"
              value={model}
              onChange={(e) => setModel(e.target.value)}
              disabled={submitting}
            />
          </div>
        </div>

        <div className="form-row-split">
          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-variant">
              Variant <span className="field-optional">(optional)</span>
            </label>
            <input
              id="vehicle-variant"
              type="text"
              maxLength={150}
              className="field-input"
              placeholder="e.g. Carrera GTS"
              value={variant}
              onChange={(e) => setVariant(e.target.value)}
              disabled={submitting}
            />
          </div>

          <div className="field-group">
            <label className="field-label" htmlFor="vehicle-year">
              Year
            </label>
            <input
              id="vehicle-year"
              type="number"
              required
              min={1886}
              max={new Date().getFullYear() + 1}
              className="field-input"
              value={year}
              onChange={(e) => setYear(e.target.value)}
              disabled={submitting}
            />
          </div>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="vehicle-image-url">
            Supplier Image or Webpage URL
          </label>
          <input
            id="vehicle-image-url"
            type="text"
            required
            maxLength={2048}
            className="field-input field-input-mono"
            placeholder="https://supplier.example.com/listing-or-image"
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
            disabled={submitting}
          />
        </div>

        <button id="submit-vehicle-btn" type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Retrieving & Validating Bytes...' : 'Ingest & Store Vehicle Image'}
        </button>
      </form>

      {feedback && (
        <div
          id="ingestion-feedback-banner"
          className={`feedback-banner ${
            feedback.type === 'error' ? 'feedback-banner-error' : 'feedback-banner-success'
          }`}
          role="alert"
        >
          <div className="feedback-code-header">
            <span className="error-code-pill">{feedback.code}</span>
            {feedback.type === 'error' && (
              <span>{feedback.retryable ? 'Transient / Retryable' : 'Permanent Rejection'}</span>
            )}
          </div>
          <p className="feedback-detail">{feedback.detail}</p>
          {feedback.traceId && <div className="feedback-meta">Trace ID: {feedback.traceId}</div>}
        </div>
      )}
    </aside>
  );
}
```

### File: `frontend/vehicle-image-dashboard/src/index.css`

```css
:root {
  --bg-canvas: hsl(215, 28%, 97%);
  --bg-surface: hsl(0, 0%, 100%);
  --bg-subtle: hsl(214, 24%, 94%);
  --bg-code: hsl(215, 25%, 14%);

  --text-primary: hsl(215, 32%, 12%);
  --text-secondary: hsl(215, 18%, 36%);
  --text-muted: hsl(215, 14%, 52%);

  --border-subtle: hsl(214, 22%, 88%);
  --border-strong: hsl(214, 20%, 76%);

  --accent-primary: hsl(212, 86%, 38%);
  --accent-primary-hover: hsl(212, 88%, 31%);
  --accent-surface: hsl(212, 75%, 95%);

  --status-success-bg: hsl(152, 65%, 94%);
  --status-success-text: hsl(154, 76%, 24%);
  --status-success-border: hsl(152, 52%, 78%);

  --status-warning-bg: hsl(38, 92%, 94%);
  --status-warning-text: hsl(32, 84%, 28%);
  --status-warning-border: hsl(38, 78%, 76%);

  --status-danger-bg: hsl(356, 85%, 96%);
  --status-danger-text: hsl(356, 74%, 34%);
  --status-danger-border: hsl(356, 68%, 82%);

  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 14px;

  --shadow-xs: 0 1px 2px rgba(15, 23, 42, 0.05);
  --shadow-sm: 0 2px 8px -2px rgba(15, 23, 42, 0.07), 0 1px 3px rgba(15, 23, 42, 0.05);
  --shadow-md: 0 8px 24px -6px rgba(15, 23, 42, 0.1), 0 2px 6px rgba(15, 23, 42, 0.05);

  --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, monospace;
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: var(--font-sans);
  background-color: var(--bg-canvas);
  color: var(--text-primary);
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
}

/* Header & Layout */
.app-header {
  background-color: var(--bg-surface);
  border-bottom: 1px solid var(--border-subtle);
  position: sticky;
  top: 0;
  z-index: 20;
}

.app-header-inner {
  max-width: 1320px;
  margin: 0 auto;
  padding: 1rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
}

.brand-group {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.brand-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: var(--text-primary);
}

.brand-subtitle {
  margin: 0;
  font-size: 0.825rem;
  color: var(--text-secondary);
}

.security-summary-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.security-chip {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  font-weight: 500;
  padding: 0.25rem 0.6rem;
  border-radius: var(--radius-sm);
  background-color: var(--bg-subtle);
  color: var(--text-secondary);
  border: 1px solid var(--border-subtle);
}

.dashboard-container {
  max-width: 1320px;
  margin: 0 auto;
  padding: 1.75rem 1.5rem 3.5rem;
  display: grid;
  grid-template-columns: 390px 1fr;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 1024px) {
  .dashboard-container {
    grid-template-columns: 1fr;
  }
}

/* Panels & Forms */
.panel {
  background-color: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  padding: 1.5rem;
}

.panel-sticky {
  position: sticky;
  top: 5.25rem;
}

.panel-title {
  margin: 0 0 0.35rem;
  font-size: 1.05rem;
  font-weight: 600;
  letter-spacing: -0.015em;
}

.panel-description {
  margin: 0 0 1.25rem;
  font-size: 0.84rem;
  color: var(--text-secondary);
}

.preset-group {
  margin-bottom: 1.25rem;
  padding: 0.75rem;
  background-color: var(--bg-subtle);
  border-radius: var(--radius-md);
  border: 1px solid var(--border-subtle);
}

.preset-group-label {
  display: block;
  font-size: 0.73rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--text-secondary);
  margin-bottom: 0.5rem;
}

.preset-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.btn-preset {
  font-family: var(--font-sans);
  font-size: 0.75rem;
  font-weight: 500;
  padding: 0.32rem 0.6rem;
  background-color: var(--bg-surface);
  color: var(--text-primary);
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;
}

.btn-preset:hover {
  background-color: var(--accent-surface);
  border-color: var(--accent-primary);
  color: var(--accent-primary);
}

.form-grid {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

.form-row-split {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.field-group {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-primary);
}

.field-optional {
  font-weight: 400;
  color: var(--text-muted);
}

.field-input {
  width: 100%;
  font-family: var(--font-sans);
  font-size: 0.875rem;
  padding: 0.55rem 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  background-color: var(--bg-surface);
  color: var(--text-primary);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.field-input:focus {
  outline: none;
  border-color: var(--accent-primary);
  box-shadow: 0 0 0 3px rgba(19, 102, 198, 0.15);
}

.field-input-mono {
  font-family: var(--font-mono);
  font-size: 0.8rem;
}

.btn-primary {
  font-family: var(--font-sans);
  font-size: 0.875rem;
  font-weight: 600;
  padding: 0.65rem 1rem;
  border: none;
  border-radius: var(--radius-sm);
  background-color: var(--accent-primary);
  color: #ffffff;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.1s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.btn-primary:hover:not(:disabled) {
  background-color: var(--accent-primary-hover);
}

.btn-primary:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.btn-secondary {
  font-family: var(--font-sans);
  font-size: 0.78rem;
  font-weight: 500;
  padding: 0.42rem 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  background-color: var(--bg-surface);
  color: var(--text-primary);
  text-decoration: none;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.btn-secondary:hover:not(:disabled) {
  background-color: var(--bg-subtle);
  border-color: var(--text-secondary);
}

/* Alerts & Feedback */
.feedback-banner {
  margin-top: 1rem;
  padding: 0.85rem 1rem;
  border-radius: var(--radius-md);
  font-size: 0.825rem;
}

.feedback-banner-error {
  background-color: var(--status-danger-bg);
  color: var(--status-danger-text);
  border: 1px solid var(--status-danger-border);
}

.feedback-banner-success {
  background-color: var(--status-success-bg);
  color: var(--status-success-text);
  border: 1px solid var(--status-success-border);
}

.feedback-code-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.3rem;
}

.error-code-pill {
  font-family: var(--font-mono);
  font-size: 0.74rem;
  font-weight: 600;
  padding: 0.15rem 0.45rem;
  background-color: rgba(153, 27, 27, 0.12);
  border-radius: 4px;
}

.feedback-detail {
  margin: 0;
  line-height: 1.45;
}

.feedback-meta {
  margin-top: 0.4rem;
  font-family: var(--font-mono);
  font-size: 0.7rem;
  opacity: 0.85;
}

/* Status Badge */
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.73rem;
  font-weight: 600;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
}

.status-badge-completed {
  background-color: var(--status-success-bg);
  color: var(--status-success-text);
  border: 1px solid var(--status-success-border);
}

.status-badge-processing {
  background-color: var(--status-warning-bg);
  color: var(--status-warning-text);
  border: 1px solid var(--status-warning-border);
}

.status-badge-failed {
  background-color: var(--status-danger-bg);
  color: var(--status-danger-text);
  border: 1px solid var(--status-danger-border);
}

/* Catalog Section & Grid */
.catalog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.25rem;
  flex-wrap: wrap;
}

.catalog-title-group h2 {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 600;
  letter-spacing: -0.015em;
}

.catalog-title-group p {
  margin: 0.15rem 0 0;
  font-size: 0.825rem;
  color: var(--text-secondary);
}

.vehicle-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(310px, 1fr));
  gap: 1.25rem;
}

.vehicle-card {
  background-color: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  transition: box-shadow 0.2s ease, transform 0.2s ease;
}

.vehicle-card:hover {
  box-shadow: var(--shadow-md);
}

.vehicle-media-frame {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  background-color: var(--bg-subtle);
  border-bottom: 1px solid var(--border-subtle);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}

.vehicle-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.image-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.8rem;
  gap: 0.4rem;
}

.vehicle-card-body {
  padding: 1.15rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
  flex: 1;
}

.vehicle-card-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}

.vehicle-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--text-primary);
}

.vehicle-variant {
  margin: 0.15rem 0 0;
  font-size: 0.8rem;
  color: var(--text-secondary);
}

.metadata-specs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.spec-tag {
  font-family: var(--font-mono);
  font-size: 0.71rem;
  padding: 0.18rem 0.48rem;
  border-radius: 4px;
  background-color: var(--bg-subtle);
  color: var(--text-secondary);
  border: 1px solid var(--border-subtle);
}

.internal-route-box {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  padding: 0.4rem 0.6rem;
  background-color: var(--bg-subtle);
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.vehicle-card-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: auto;
  padding-top: 0.5rem;
  border-top: 1px solid var(--border-subtle);
}

.replace-drawer {
  margin-top: 0.65rem;
  padding-top: 0.75rem;
  border-top: 1px dashed var(--border-strong);
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.replace-actions {
  display: flex;
  gap: 0.5rem;
}

.empty-catalog {
  background-color: var(--bg-surface);
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius-lg);
  padding: 3rem 1.5rem;
  text-align: center;
  color: var(--text-secondary);
}

.pagination-bar {
  margin-top: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.85rem 1.15rem;
  background-color: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  font-size: 0.84rem;
}
```

### File: `frontend/vehicle-image-dashboard/src/main.jsx`

```jsx
import React from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import './index.css';

createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

### File: `frontend/vehicle-image-dashboard/src/pages/DashboardPage.jsx`

```jsx
import React, { useCallback, useEffect, useState } from 'react';
import VehicleForm from '../components/VehicleForm';
import VehicleCard from '../components/VehicleCard';
import { listVehicles } from '../services/vehicleApi';

const PAGE_SIZE = 12;

export default function DashboardPage() {
  const [vehicles, setVehicles] = useState([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(null);

  const fetchCatalog = useCallback(async (targetPage = page) => {
    setLoading(true);
    setLoadError(null);
    try {
      const data = await listVehicles(targetPage, PAGE_SIZE);
      setVehicles(data.items || []);
      setPage(data.page || targetPage);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      setLoadError(err.detail || err.message || 'Unable to load stored vehicle records.');
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    fetchCatalog(page);
  }, [page, fetchCatalog]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <main className="dashboard-container">
      <VehicleForm
        onVehicleCreated={() => {
          setPage(1);
          fetchCatalog(1);
        }}
      />

      <section aria-labelledby="catalog-heading">
        <div className="catalog-header">
          <div className="catalog-title-group">
            <h2 id="catalog-heading">Internally Stored Vehicle Catalog</h2>
            <p>
              All images below are served strictly from <code>/api/images/&#123;id&#125;</code> with native lazy loading and zero supplier hotlinking.
            </p>
          </div>

          <button
            id="refresh-catalog-btn"
            type="button"
            className="btn-secondary"
            onClick={() => fetchCatalog(page)}
            disabled={loading}
          >
            {loading ? 'Refreshing...' : `Refresh (${totalCount})`}
          </button>
        </div>

        {loadError && (
          <div className="feedback-banner feedback-banner-error" role="alert">
            <div className="feedback-code-header">
              <span className="error-code-pill">CATALOG_LOAD_ERROR</span>
            </div>
            <p className="feedback-detail">{loadError}</p>
          </div>
        )}

        {!loading && vehicles.length === 0 && !loadError ? (
          <div className="empty-catalog">
            <h3>No vehicles ingested yet</h3>
            <p>
              Submit a supplier vehicle and image URL on the left (or click one of the quick verification presets) to retrieve, validate, and store an image.
            </p>
          </div>
        ) : (
          <div className="vehicle-grid">
            {vehicles.map((vehicle) => (
              <VehicleCard
                key={`${vehicle.id}-${vehicle.imageId}`}
                vehicle={vehicle}
                onUpdated={() => fetchCatalog(page)}
              />
            ))}
          </div>
        )}

        {totalCount > PAGE_SIZE && (
          <nav className="pagination-bar" aria-label="Vehicle catalog pagination">
            <span>
              Page <strong>{page}</strong> of <strong>{totalPages}</strong> ({totalCount} total vehicles)
            </span>
            <div className="replace-actions">
              <button
                id="prev-page-btn"
                type="button"
                className="btn-secondary"
                disabled={page <= 1 || loading}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Previous
              </button>
              <button
                id="next-page-btn"
                type="button"
                className="btn-secondary"
                disabled={page >= totalPages || loading}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                Next
              </button>
            </div>
          </nav>
        )}
      </section>
    </main>
  );
}
```

### File: `frontend/vehicle-image-dashboard/src/services/vehicleApi.js`

```javascript
const API_BASE = import.meta.env.VITE_API_BASE_URL || '';

export class ApiError extends Error {
  constructor({ code, title, detail, status, traceId, retryable, retryAfterSeconds }) {
    super(detail || title || 'Request failed');
    this.name = 'ApiError';
    this.code = code || 'INTERNAL_ERROR';
    this.title = title || 'Request Failed';
    this.detail = detail || 'An unexpected error occurred.';
    this.status = status || 500;
    this.traceId = traceId || null;
    this.retryable = Boolean(retryable);
    this.retryAfterSeconds = retryAfterSeconds ?? null;
  }
}

async function parseResponse(response) {
  if (response.ok) {
    return response.json();
  }

  let errorPayload = null;
  try {
    errorPayload = await response.json();
  } catch {
    // Fallback if non-JSON error response
  }

  if (errorPayload && typeof errorPayload === 'object') {
    throw new ApiError({
      code: errorPayload.code || 'HTTP_ERROR',
      title: errorPayload.title || 'Request Failed',
      detail: errorPayload.detail || `Server responded with status ${response.status}.`,
      status: errorPayload.status || response.status,
      traceId: errorPayload.traceId || null,
      retryable: errorPayload.retryable || false,
      retryAfterSeconds: errorPayload.retryAfterSeconds ?? null
    });
  }

  throw new ApiError({
    code: 'NETWORK_OR_SERVER_ERROR',
    title: 'Request Failed',
    detail: `HTTP ${response.status}: ${response.statusText || 'Unable to process request.'}`,
    status: response.status
  });
}

export async function listVehicles(page = 1, pageSize = 12) {
  const response = await fetch(
    `${API_BASE}/api/vehicles?page=${encodeURIComponent(page)}&pageSize=${encodeURIComponent(pageSize)}`,
    {
      headers: { Accept: 'application/json' }
    }
  );
  return parseResponse(response);
}

export async function getVehicle(id) {
  const response = await fetch(`${API_BASE}/api/vehicles/${encodeURIComponent(id)}`, {
    headers: { Accept: 'application/json' }
  });
  return parseResponse(response);
}

export async function createVehicle(payload) {
  const response = await fetch(`${API_BASE}/api/vehicles`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json'
    },
    body: JSON.stringify(payload)
  });
  return parseResponse(response);
}

export async function replaceImage(vehicleId, imageUrl) {
  const response = await fetch(`${API_BASE}/api/vehicles/${encodeURIComponent(vehicleId)}/image`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json'
    },
    body: JSON.stringify({ imageUrl })
  });
  return parseResponse(response);
}

export function resolveInternalImageUrl(imageUrl) {
  if (!imageUrl) return null;
  return `${API_BASE}${imageUrl}`;
}
```

### File: `frontend/vehicle-image-dashboard/vite.config.js`

```javascript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const apiTarget = process.env.VITE_API_TARGET || 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true
      },
      '/health': {
        target: apiTarget,
        changeOrigin: true
      }
    }
  }
});
```

## 6. Project and Repository Configuration

### File: `.editorconfig`

```text
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{js,jsx,ts,tsx,json,css,html,md,yml,yaml}]
indent_size = 2

[*.cs]
csharp_new_line_before_open_brace = all
csharp_style_namespace_declarations = file_scoped:warning
dotnet_sort_system_directives_first = true
```

### File: `.gitignore`

```text
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio / Rider / VS Code
.vs/
.idea/
.vscode/
*.suo
*.user
*.userosscache
*.sln.docstates
*.DotSettings.user

# .NET Core / User secrets
project.lock.json
project.fragment.lock.json
artifacts/
appsettings.Local.json
secrets.json

# Runtime storage (keep folder placeholder only)
storage/**
!storage/.gitkeep
!storage/vehicles/
!storage/vehicles/.gitkeep
backend/VehicleImage.Api/storage/**

# Test results
TestResults/
coverage/
*.trx
*.coverage

# Frontend
node_modules/
dist/
dist-ssr/
*.local
.env
.env.*
!.env.example

# OS files
.DS_Store
Thumbs.db

.fake
```

### File: `docs/Architecture_and_Decisions.md`

```markdown
# Architecture Decisions, Known Limitations & Operational Notes

## 1. Resolved Architectural Decisions

1. **Failed Initial Submissions (`POST /api/vehicles`)**:
   - Failed submissions return `422 Unprocessable Entity` with a structured RFC 7807 `ImageFailureResponse` (`code`, `title`, `detail`, `status`, `traceId`, `retryable`, `retryAfterSeconds`) and do not persist broken rows in the database.
   - Failed replacements (`PUT /api/vehicles/{id}/image`) likewise return `422` without modifying the existing `VehicleImage` row or deleting the existing stored file.
2. **Error Code Taxonomy (`Exceptions/ErrorCodes.cs`)**:
   - Uses the merged error code table covering validation (`VALIDATION_FAILED`), URL/SSRF checks (`INVALID_URL`, `UNSAFE_DESTINATION`, `DNS_RESOLUTION_FAILED`), transport (`CONNECTION_TIMEOUT`, `REQUEST_TIMEOUT`, `TLS_FAILURE`, `REDIRECT_LIMIT_EXCEEDED`), upstream HTTP (`HTTP_NOT_FOUND`, `HTTP_UNAUTHORIZED`, `HTTP_FORBIDDEN`, `HTTP_RATE_LIMITED`, `HTTP_SERVER_ERROR`, `HTTP_ERROR`), content/image validation (`UNSUPPORTED_SOURCE`, `UNSUPPORTED_CONTENT_TYPE`, `UNSUPPORTED_IMAGE_FORMAT`, `IMAGE_TOO_LARGE`, `IMAGE_DIMENSIONS_EXCEEDED`, `IMAGE_TOO_SMALL`, `IMAGE_DECODE_FAILED`, `NO_IMAGE_FOUND`, `PROVIDER_AUTH_REQUIRED`), and infrastructure (`STORAGE_FAILED`, `DATABASE_PERSISTENCE_FAILED`, `VEHICLE_NOT_FOUND`, `IMAGE_NOT_FOUND`, `CONCURRENT_UPDATE`, `INTERNAL_ERROR`).
3. **Catalog Listing Contract (`GET /api/vehicles`)**:
   - Returns `PagedResponse<VehicleResponse>` (`items`, `page`, `pageSize`, `totalCount`) with `pageSize` capped at 100.
4. **Bounded Seekable Stream (`Models/RetrievedImage.cs`)**:
   - `DirectImageRetriever` streams the decompressed response body into a bounded seekable `MemoryStream` while counting bytes and aborting immediately if `MaxDownloadBytes` (default 20 MB) is exceeded. `RetrievedImage` implements `IDisposable` and `IAsyncDisposable` and is always disposed by `ImageIngestionService`.
5. **Immutable Image Delivery (`PUT /api/vehicles/{id}/image`)**:
   - Every successful replacement issues a new `ImageId` and writes to a new date-partitioned storage key (`vehicles/{yyyy}/{MM}/{guid}{ext}`), allowing `GET /api/images/{id}` to send `Cache-Control: public, max-age=31536000, immutable`.
6. **Single-GET Classification & Multi-Candidate HTML Fallback**:
   - `RetrievalOrchestrator` performs a single initial `GET` through `DirectImageRetriever` (never a mandatory `HEAD` request) and inspects magic bytes and `Content-Type` to route HTML pages to `HtmlImageRetriever`.
   - `HtmlImageRetriever` yields up to `HtmlExtraction:MaxCandidates` ranked candidates (`IAsyncEnumerable<RetrievedImage>`) so `ImageIngestionService` can validate each candidate in turn without coupling retrievers to `ImageValidator`.

## 2. Known Limitations & Production Considerations

1. **Heuristic HTML Image Selection**:
   - HTML candidate extraction prioritizes `og:image`, `twitter:image`, largest `srcset` descriptors, `data-src` lazy-loading attributes, and `<img>` elements scored by vehicle keywords, declared dimensions, and container context. While effective for standard vehicle listing pages, pages that render images exclusively via client-side JavaScript or obfuscated canvas elements require a conditional browser strategy.
2. **Synchronous Ingestion Latency**:
   - Image retrieval, validation, and transient retries run synchronously within the HTTP request lifecycle (bounded by `Retrieval:TimeoutSeconds`). If slow upstream sources or headless browser extraction are introduced later, ingestion can be moved to a background queue; the current implementation persists completed images synchronously.
3. **Cross-Store Compensation**:
   - Local filesystem operations and SQL Server transactions are not two-phase atomic. The pipeline orders writes so that database failures trigger a compensating file delete, and superseded files on replacement are deleted only after the database commit succeeds. Any failed compensating delete logs the orphaned `StorageKey` at `Error` level for scheduled reconciliation.
4. **Network Egress Defense-in-Depth**:
   - Application-level `ConnectCallback` and per-hop DNS validation prevent SSRF and DNS rebinding in code; production deployments should pair this with host/VPC egress firewall rules blocking private address ranges.
5. **EXIF Metadata**:
   - Validated original image bytes are stored unchanged in Phase 1 to preserve exact byte fidelity and SHA-256 digests. If supplier images may contain sensitive EXIF metadata (such as GPS coordinates), metadata stripping via `ImageSharp` (`image.Metadata.ExifProfile = null`) can be enabled before saving.
```

### File: `README.md`

````markdown
# VehicleImageSystem — Multi-Source Vehicle Image Retrieval, Validation, Storage & Delivery

**VehicleImageSystem** (`VehicleImage.Api`) ingests supplier vehicle records and image URLs, retrieves the actual image bytes over an SSRF-guarded streaming HTTP pipeline, validates magic bytes and decoded dimensions with `SixLabors.ImageSharp`, stores validated images in internal storage outside the web root, persists metadata in Microsoft SQL Server via EF Core, and serves stored images exclusively from our own API (`/api/images/{id}`). Once ingested, the application has zero runtime dependency on the supplier's URL.

---

## Architecture & Pipeline Overview

```
Supplier -> React Dashboard -> ASP.NET Core API (VehicleImage.Api)
  VehiclesController -> VehicleService -> ImageIngestionService
     -> UrlValidator (syntax, scheme, userinfo, port & host policy)
     -> RetrievalOrchestrator (single GET sniffing, no mandatory HEAD)
        -> DirectImageRetriever (SSRF-safe ConnectCallback, manual redirects <= 5, 20 MB stream cap, transient retries)
        -> HtmlImageRetriever (og:image, twitter:image, srcset, data-src, img ranking & multi-candidate fallback)
     -> RetrievedImage -> ImageValidator (magic bytes, Image.Identify dimension checks, full pixel decode, SHA-256)
     -> IImageStorage / LocalFileStorage (atomic temp-to-final write under vehicles/{yyyy}/{MM}/{guid}{ext})
  VehicleService -> SQL Server (transactional persistence + compensating file cleanup on failure)

Delivery: React Dashboard -> ImagesController -> ImageDeliveryService
     -> SQL Server lookup + LocalFileStorage stream (nosniff + immutable cache headers)
```

### Key Architectural Guarantees

1. **SSRF & DNS Rebinding Protection (`Security/`)**:
   - `UrlValidator` enforces `http`/`https` schemes, allowed ports (`80`, `443`), no embedded credentials, and FQDN hostnames before opening any socket.
   - `DestinationValidator` blocks loopback (`127.0.0.0/8`, `::1`), RFC 1918 private networks (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`), CGNAT (`100.64.0.0/10`), link-local and cloud metadata endpoints (`169.254.0.0/16`, `169.254.169.254`, `168.63.129.16`), unique local IPv6 (`fc00::/7`), IPv4-mapped IPv6 (`::ffff:0:0/96`), NAT64 (`64:ff9b::/96`), and 6to4 (`2002::/16`). If *any* resolved DNS record for a host is blocked, the destination is rejected.
   - `SsrfGuardHttpHandler` uses `SocketsHttpHandler.ConnectCallback` to resolve and validate addresses at TCP socket connection time while preserving the original hostname on the HTTP request for `Host` header, TLS SNI, and TLS certificate hostname verification.
   - Automatic redirects and proxies are disabled (`AllowAutoRedirect = false`, `UseProxy = false`). Redirects (`301`, `302`, `303`, `307`, `308`) are followed manually up to 5 hops with full URL and DNS re-validation on every hop.

2. **Bounded Streaming & Image Validation (`ImageProcessing/`)**:
   - Downloads are streamed through a running byte counter that aborts immediately with `IMAGE_TOO_LARGE` when decompressed bytes exceed `Retrieval:MaxDownloadBytes` (default 20 MB), regardless of whether `Content-Length` is missing or misleading.
   - `ImageValidator` inspects magic bytes (`JPEG`, `PNG`, `WebP`), rejects HTML/XML/SVG as `UNSUPPORTED_CONTENT_TYPE`, rejects non-whitelisted image formats (`GIF`, `BMP`, `TIFF`, `ICO`, `AVIF`, animated images) as `UNSUPPORTED_IMAGE_FORMAT`, checks dimensions via `Image.IdentifyAsync` before pixel allocation (decompression-bomb defense), performs a full bounded pixel decode to catch truncated or corrupted streams (`IMAGE_DECODE_FAILED`), and computes a `SHA-256` digest.

3. **Static HTML Candidate Extraction (`Retrieval/HtmlImageRetriever.cs`)**:
   - Classification uses the initial `GET` response without issuing a duplicate `HEAD` request.
   - Extracts and ranks candidates from `og:image`, `twitter:image`, `srcset` (largest descriptor), `data-src`/`data-original`, and `<img src>`, resolving relative URLs against `<base href>` or the final redirected page URL.
   - Filters out logos, icons, sprites, tracking pixels, ads, and tiny thumbnails, penalizes `<header>`/`<nav>`/`<footer>`/`<aside>` boilerplate containers, and tries up to `HtmlExtraction:MaxCandidates` (default 3) until one passes `ImageValidator`. Soft-404 HTML error pages returning HTTP 200 are rejected.

4. **Transactional Persistence & Safe Image Replacement (`Services/VehicleService.cs`)**:
   - Failed vehicle creations return `422 Unprocessable Entity` with a structured RFC 7807 `ImageFailureResponse` (`code`, `title`, `detail`, `status`, `traceId`, `retryable`, `retryAfterSeconds`) and persist nothing.
   - If the database write fails after a file is saved, `VehicleService` executes a compensating file delete (and logs an orphan key alert if cleanup fails).
   - On `PUT /api/vehicles/{id}/image`, the new image is retrieved, validated, and written to a new storage key *before* the database reference is updated. If retrieval, validation, or database commit fails, the existing image and file remain untouched. Only after the database transaction commits is the superseded file deleted, and a new `ImageId` is issued so `/api/images/{id}` can be cached with `Cache-Control: public, max-age=31536000, immutable`.

---

## Project Structure

```
VehicleImageSystem/
├── VehicleImageSystem.slnx
├── README.md
├── .gitignore
├── .editorconfig
├── backend/
│   └── VehicleImage.Api/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Configuration/       # Strongly-typed options bound & validated at startup
│       ├── Controllers/         # VehiclesController & ImagesController
│       ├── DTOs/                # Request/response API contracts & PagedResponse<T>
│       ├── Entities/            # Vehicle & VehicleImage (with ImageStatus enum)
│       ├── Data/                # ApplicationDbContext & EF entity configurations
│       ├── Migrations/          # EF Core SQL Server migrations (InitialCreate)
│       ├── Services/            # VehicleService, ImageIngestionService, ImageDeliveryService
│       ├── Retrieval/           # RetrievalOrchestrator, Direct & Html retrievers
│       ├── Security/            # UrlValidator, DestinationValidator, SsrfGuardHttpHandler
│       ├── ImageProcessing/     # ImageValidator & ImageMetadata
│       ├── Storage/             # IImageStorage & LocalFileStorage (outside wwwroot)
│       └── Exceptions/          # ErrorCodes, typed exceptions & GlobalExceptionHandler
├── tests/
│   ├── UnitTests/               # xUnit unit tests (URL, SSRF IP ranges, ImageValidator, Storage, HTML ranking)
│   └── IntegrationTests/        # WebApplicationFactory + local Kestrel MockSupplierServer + Fixtures
├── frontend/
│   └── vehicle-image-dashboard/ # Vite + React dashboard with native lazy loading
├── storage/
│   └── vehicles/                # Runtime image files (outside web root, Git-ignored)
└── docs/
    └── Architecture_and_Decisions.md
```

---

## Getting Started

### Prerequisites

- **.NET 10.0 SDK** (`10.0.100` or later; `.slnx` solution format requires .NET SDK 9.0.200+)
- **Microsoft SQL Server** (Local instance, SQL Server Express, or LocalDB; managed with SSMS)
- **Node.js 20+** and `npm` (for the React dashboard)

### 1. Configure SQL Server & Apply Migrations

Update `ConnectionStrings:DefaultConnection` in `backend/VehicleImage.Api/appsettings.Development.json` (or via User Secrets / environment variables) to match your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=VehicleImageDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Storage": {
    "RootPath": "../../storage"
  }
}
```

Apply the EF Core migration:

```bash
dotnet ef database update \
  --project backend/VehicleImage.Api/VehicleImage.Api.csproj \
  --startup-project backend/VehicleImage.Api/VehicleImage.Api.csproj
```

> **Note:** Storage files are written to `Storage:RootPath` (resolved to an absolute path outside `wwwroot` at startup) and are served exclusively through `GET /api/images/{id}`.

### 2. Run the Backend API

```bash
dotnet run --project backend/VehicleImage.Api/VehicleImage.Api.csproj
```

### 3. Run the React Dashboard

```bash
cd frontend/vehicle-image-dashboard
npm install
npm run dev
```

The Vite dev server runs at `http://localhost:5173` and proxies `/api` requests to the ASP.NET Core backend (`http://localhost:5080` by default, configurable via `VITE_API_TARGET`).

The API currently has no authentication or authorization. Do not expose it publicly without adding appropriate access controls.

### 4. Run the Test Suites

All unit, integration, and SSRF security tests run against an in-process Kestrel `MockSupplierServer` and local fixtures with zero dependency on external websites:

```bash
dotnet test
```

Integration tests use isolated SQLite databases and temporary storage directories. SQL Server remains the default application provider; SQLite `EnsureCreated()` is used only for isolated test databases.

---

## API Reference

| Method | Endpoint | Success | Error Statuses | Description |
| --- | --- | --- | --- | --- |
| `POST` | `/api/vehicles` | `201 Created` | `400`, `422`, `500` | Ingests a supplier vehicle and image URL, stores the validated image, and persists metadata |
| `GET` | `/api/vehicles` | `200 OK` | `400` | Returns a `PagedResponse<VehicleResponse>` ordered newest first |
| `GET` | `/api/vehicles/{id}` | `200 OK` | `404` | Returns vehicle details and stored image metadata |
| `PUT` | `/api/vehicles/{id}/image` | `200 OK` | `400`, `404`, `409`, `422`, `500` | Safely replaces a vehicle's image; preserves the existing image on failure |
| `GET` | `/api/images/{id}` | `200 OK` | `404` | Streams the stored image with `X-Content-Type-Options: nosniff` and `immutable` cache headers |
| `GET` | `/api/images/{id}/download` | `200 OK` | `404` | Streams the stored image as a downloadable attachment |
````

### File: `storage/vehicles/.gitkeep`

```text

```

### File: `VehicleImageSystem.slnx`

```xml
<Solution>
  <Folder Name="/backend/">
    <Project Path="backend/VehicleImage.Api/VehicleImage.Api.csproj" />
  </Folder>
  <Folder Name="/tests/" />
  <Folder Name="/tests/IntegrationTests/">
    <Project Path="tests/IntegrationTests/VehicleImage.IntegrationTests.csproj" />
  </Folder>
  <Folder Name="/tests/UnitTests/">
    <Project Path="tests/UnitTests/VehicleImage.UnitTests.csproj" />
  </Folder>
</Solution>
```

## 7. Export Manifest

- Total files included: **97**.
- Backend files: **50**.
- Unit-test files: **7**.
- Integration-test files: **20**.
- Frontend files: **14**.
- Project, repository configuration, and documentation files: **6**.

Excluded files or directories:

- `bin/` and `obj/`: generated .NET build outputs.
- `node_modules/` and `dist/`/`dist-ssr/`: generated frontend dependencies and bundles.
- `.git/`, IDE cache directories, package caches, temporary files, and binary assets: repository metadata, caches, or non-source binaries.
- `docs/COMPLETE_CODEBASE_REVIEW.md`: the export file itself is excluded to avoid self-inclusion.

- Files that could not be read: **none**.
- Verification: every included file was enumerated before export, read completely, assigned one section, and written with a path heading; no included file was intentionally truncated or duplicated.
