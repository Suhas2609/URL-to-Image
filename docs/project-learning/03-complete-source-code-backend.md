# Complete Backend Source Code Appendix

This appendix contains the runtime, configuration, entity, migration, security, retrieval, validation, storage, service, and controller files in `backend/VehicleImage.Api/`.

> [!NOTE]
> Auto-generated EF Core metadata snapshots (`Migrations/20261116094554_InitialCreate.Designer.cs` and `Migrations/ApplicationDbContextModelSnapshot.cs`) and the default Visual Studio scratch request template (`VehicleImage.Api.http`) are excluded here to keep focus on active runtime code (they remain cataloged in [`02-complete-project-directory-and-file-reference.md`](./02-complete-project-directory-and-file-reference.md)). All 50 active runtime backend source and configuration files, including `GoogleDriveImageRetriever.cs`, are included in full below.

## Table of Contents

1. `backend/VehicleImage.Api/VehicleImage.Api.csproj` — VehicleImageSystem Solution Project & Dependencies
2. `backend/VehicleImage.Api/appsettings.json` — Base Runtime Configuration
3. `backend/VehicleImage.Api/appsettings.Development.json` — Development Configuration Overrides
4. `backend/VehicleImage.Api/Properties/launchSettings.json` — Local Launch Profiles
5. `backend/VehicleImage.Api/Program.cs` — Application Composition Root & Middleware Pipeline
6. `backend/VehicleImage.Api/Configuration/StorageOptions.cs` — Configuration Option: StorageOptions
7. `backend/VehicleImage.Api/Configuration/RetrievalOptions.cs` — Configuration Option: RetrievalOptions
8. `backend/VehicleImage.Api/Configuration/ImageValidationOptions.cs` — Configuration Option: ImageValidationOptions
9. `backend/VehicleImage.Api/Configuration/HtmlExtractionOptions.cs` — Configuration Option: HtmlExtractionOptions
10. `backend/VehicleImage.Api/Configuration/RetryOptions.cs` — Configuration Option: RetryOptions
11. `backend/VehicleImage.Api/Configuration/BulkImportOptions.cs` — Configuration Option: BulkImportOptions
12. `backend/VehicleImage.Api/Exceptions/ErrorCodes.cs` — Canonical Error Codes
13. `backend/VehicleImage.Api/Exceptions/ImageIngestionException.cs` — Typed Ingestion Exception
14. `backend/VehicleImage.Api/Exceptions/ResourceNotFoundException.cs` — Resource Not Found Exception (404)
15. `backend/VehicleImage.Api/Exceptions/ConcurrentUpdateException.cs` — Concurrent Update Exception (409)
16. `backend/VehicleImage.Api/Exceptions/GlobalExceptionHandler.cs` — Global Exception Handler
17. `backend/VehicleImage.Api/DTOs/PagedResponse.cs` — Generic Paging Envelope DTO
18. `backend/VehicleImage.Api/DTOs/Images/ImageFailureResponse.cs` — Structured Failure Response DTO
19. `backend/VehicleImage.Api/DTOs/Vehicles/CreateVehicleRequest.cs` — Single Vehicle Creation Request DTO
20. `backend/VehicleImage.Api/DTOs/Vehicles/ReplaceVehicleImageRequest.cs` — Replace Vehicle Image Request DTO
21. `backend/VehicleImage.Api/DTOs/Vehicles/ImageSubmissionResponse.cs` — Image Submission Success DTO
22. `backend/VehicleImage.Api/DTOs/Vehicles/VehicleResponse.cs` — Vehicle Catalog Read DTO
23. `backend/VehicleImage.Api/DTOs/Vehicles/VehicleImportResponse.cs` — CSV Bulk Import Response DTOs
24. `backend/VehicleImage.Api/Entities/Vehicle.cs` — Vehicle Domain Entity
25. `backend/VehicleImage.Api/Entities/VehicleImage.cs` — VehicleImage Domain Entity & ImageStatus Enum
26. `backend/VehicleImage.Api/Data/ApplicationDbContext.cs` — EF Core ApplicationDbContext
27. `backend/VehicleImage.Api/Data/Configurations/VehicleConfiguration.cs` — Vehicle Entity Fluent Configuration
28. `backend/VehicleImage.Api/Data/Configurations/VehicleImageConfiguration.cs` — VehicleImage Entity Fluent Configuration
29. `backend/VehicleImage.Api/Migrations/20261116094554_InitialCreate.cs` — Initial EF Core Migration
30. `backend/VehicleImage.Api/Models/RetrievedImage.cs` — RetrievedImage Disposable Stream Model
31. `backend/VehicleImage.Api/Models/IngestedImage.cs` — IngestedImage Handoff Model
32. `backend/VehicleImage.Api/Security/UrlValidator.cs` — URL Policy & Redirect Target Validator
33. `backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs` — Direct HTTP Retriever, Manual Redirects & Retry Loop
34. `backend/VehicleImage.Api/Retrieval/GoogleDriveImageRetriever.cs` — Public Google Drive URL Download & HTML Confirmation Flow
35. `backend/VehicleImage.Api/Retrieval/HtmlImageRetriever.cs` — Static HTML Candidate Extractor & Ranker
36. `backend/VehicleImage.Api/Retrieval/RetrievalOrchestrator.cs` — Single-GET Sniffing Retrieval Orchestrator
37. `backend/VehicleImage.Api/ImageProcessing/ImageMetadata.cs` — Validated Image Metadata Models
38. `backend/VehicleImage.Api/ImageProcessing/ImageValidator.cs` — ImageSharp Magic-Byte, Bomb & Full Pixel Validator
39. `backend/VehicleImage.Api/Storage/IImageStorage.cs` — Internal Storage Abstraction
40. `backend/VehicleImage.Api/Storage/LocalFileStorage.cs` — Traversal-Safe Atomic Local Disk Storage
41. `backend/VehicleImage.Api/Services/IImageIngestionService.cs` — Image Ingestion Service Contract
42. `backend/VehicleImage.Api/Services/ImageIngestionService.cs` — Image Ingestion Service
43. `backend/VehicleImage.Api/Services/IVehicleService.cs` — Vehicle Lifecycle Service Contract
44. `backend/VehicleImage.Api/Services/VehicleService.cs` — Vehicle Persistence & Safe Replacement Service
45. `backend/VehicleImage.Api/Services/IImageDeliveryService.cs` — Image Delivery Service Contract
46. `backend/VehicleImage.Api/Services/ImageDeliveryService.cs` — Image Delivery Service
47. `backend/VehicleImage.Api/Services/IVehicleImportService.cs` — CSV Bulk Import Service Contract
48. `backend/VehicleImage.Api/Services/VehicleImportService.cs` — CSV Bulk Import Service
49. `backend/VehicleImage.Api/Controllers/VehiclesController.cs` — Vehicles API Controller
50. `backend/VehicleImage.Api/Controllers/ImagesController.cs` — Internal Images API Controller

---

## 1. VehicleImageSystem Solution Project & Dependencies

**File:** `backend/VehicleImage.Api/VehicleImage.Api.csproj`  
**Purpose:** Defines the .NET 10 Web API project and its NuGet dependencies: CsvHelper (33.1.0), HtmlAgilityPack (1.11.72), OpenAPI, EF Core 10 (SQL Server + Sqlite + Design), and SixLabors.ImageSharp (3.1.12).

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CsvHelper" Version="33.1.0" />
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

## 2. Base Runtime Configuration

**File:** `backend/VehicleImage.Api/appsettings.json`  
**Purpose:** Configures logging, DatabaseProvider (SqlServer default), ConnectionStrings, Storage:RootPath, Retrieval limits, ImageValidation bounds, HtmlExtraction filters, Retry backoff, and BulkImport limits (MaxRecords=50, MaxFileBytes=1MB).

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
    ]
  },
  "Retry": {
    "MaxAttempts": 3,
    "BaseDelayMs": 150,
    "MaxDelayMs": 1500,
    "EnableJitter": true,
    "MaxRetryAfterSeconds": 3
  },
  "BulkImport": {
    "MaxRecords": 50,
    "MaxFileBytes": 1048576
  }
}
```

## 3. Development Configuration Overrides

**File:** `backend/VehicleImage.Api/appsettings.Development.json`  
**Purpose:** Enables Debug logging and EF Core SQL command logging during local Development runs.

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

## 4. Local Launch Profiles

**File:** `backend/VehicleImage.Api/Properties/launchSettings.json`  
**Purpose:** Defines local HTTP (port 5080) and HTTPS (ports 7206/5080) developer launch profiles matching Vite's default proxy target.

```json
﻿{
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

## 5. Application Composition Root & Middleware Pipeline

**File:** `backend/VehicleImage.Api/Program.cs`  
**Purpose:** Binds and validates all 6 configuration option classes at startup, registers SQL Server/SQLite DbContext, shapes ModelState errors into 400 ImageFailureResponse, registers singletons/named HttpClient (`ImageRetrievalClient`)/scoped services, verifies Storage:RootPath is outside wwwroot and writable, and wires middleware.

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

builder.Services.AddOptions<BulkImportOptions>()
    .Bind(builder.Configuration.GetSection(BulkImportOptions.SectionName))
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

// 4. Validation, storage singletons and HttpClient
builder.Services.AddSingleton<UrlValidator>();
builder.Services.AddSingleton<ImageValidator>();
builder.Services.AddSingleton<IImageStorage, LocalFileStorage>();

builder.Services.AddHttpClient(DirectImageRetriever.HttpClientName, client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan; // Enforced per-request by DirectImageRetriever
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var retrievalOptions = sp.GetRequiredService<IOptions<RetrievalOptions>>().Value;
    return new System.Net.Http.SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli,
        ConnectTimeout = TimeSpan.FromSeconds(retrievalOptions.ConnectTimeoutSeconds),
        PooledConnectionLifetime = TimeSpan.FromSeconds(retrievalOptions.PooledConnectionLifetimeSeconds),
        MaxResponseHeadersLength = 64
    };
});

// 5. Retrieval and application services
builder.Services.AddScoped<DirectImageRetriever>();
builder.Services.AddScoped<HtmlImageRetriever>();
builder.Services.AddScoped<GoogleDriveImageRetriever>();
builder.Services.AddScoped<RetrievalOrchestrator>();
builder.Services.AddScoped<ImageIngestionService>();
builder.Services.AddScoped<IImageIngestionService>(sp => sp.GetRequiredService<ImageIngestionService>());
builder.Services.AddScoped<ImageDeliveryService>();
builder.Services.AddScoped<IImageDeliveryService>(sp => sp.GetRequiredService<ImageDeliveryService>());
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<IVehicleService>(sp => sp.GetRequiredService<VehicleService>());
builder.Services.AddScoped<IVehicleImportService, VehicleImportService>();

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
    VerifyStorageRootWritable(fullStorageRoot);
}

static void VerifyStorageRootWritable(string storageRoot)
{
    string probePath = Path.Combine(
        storageRoot,
        $".vehicle-image-write-check-{Guid.NewGuid():N}.tmp");
    Exception? writeFailure = null;

    try
    {
        using var probe = new FileStream(
            probePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 256,
            options: FileOptions.WriteThrough);
        probe.Write([0x56, 0x49]);
        probe.Flush(flushToDisk: true);
    }
    catch (Exception ex)
    {
        writeFailure = ex;
    }

    try
    {
        if (File.Exists(probePath))
        {
            File.Delete(probePath);
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "Storage:RootPath verification temporary file could not be removed.",
            ex);
    }

    if (writeFailure is not null)
    {
        throw new InvalidOperationException(
            "Storage:RootPath could not be verified as writable.",
            writeFailure);
    }
}

public partial class Program;
```

## 6. Configuration Option: StorageOptions

**File:** `backend/VehicleImage.Api/Configuration/StorageOptions.cs`  
**Purpose:** Strongly-typed option holding the required RootPath where validated vehicle images are persisted.

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

## 7. Configuration Option: RetrievalOptions

**File:** `backend/VehicleImage.Api/Configuration/RetrievalOptions.cs`  
**Purpose:** Bounds outbound HTTP downloads: 20 MB stream limit, 15s request timeout, 5s connect timeout, 5 max redirects, allowed ports (80, 443), max URL length, User-Agent, and connection pool lifetime.

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

## 8. Configuration Option: ImageValidationOptions

**File:** `backend/VehicleImage.Api/Configuration/ImageValidationOptions.cs`  
**Purpose:** Enforces allowed formats (Jpeg, Png, WebP), minimum dimensions (100x100), maximum dimensions (10000x10000), and max total pixel count (50,000,000) to block decompression bombs.

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

## 9. Configuration Option: HtmlExtractionOptions

**File:** `backend/VehicleImage.Api/Configuration/HtmlExtractionOptions.cs`  
**Purpose:** Controls HTML scraping caps (MaxCandidates=3, MinDeclaredDimension=80) and URL/attribute patterns ignored as UI boilerplate (logos, icons, trackers, ads).

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class HtmlExtractionOptions
{
    public const string SectionName = "HtmlExtraction";

    [Range(1, 3)]
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

}
```

## 10. Configuration Option: RetryOptions

**File:** `backend/VehicleImage.Api/Configuration/RetryOptions.cs`  
**Purpose:** Configures transient HTTP retry limits: max 3 attempts, 150ms base delay, 1500ms cap, +/-25% jitter, and 3s ceiling on honoring upstream Retry-After headers.

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

## 11. Configuration Option: BulkImportOptions (New on feature/csv-bulk-import)

**File:** `backend/VehicleImage.Api/Configuration/BulkImportOptions.cs`  
**Purpose:** Bounds CSV bulk ingestion to 50 records and 1 MB default file size (hard-capped at AbsoluteMaxFileBytes = 5 MB).

```csharp
using System.ComponentModel.DataAnnotations;

namespace VehicleImage.Api.Configuration;

public sealed class BulkImportOptions
{
    public const string SectionName = "BulkImport";

    /// <summary>
    /// Hard upper bound enforced by the controller's request size limit.
    /// MaxFileBytes cannot be configured above this value.
    /// </summary>
    public const long AbsoluteMaxFileBytes = 5 * 1024 * 1024;

    [Range(1, 500)]
    public int MaxRecords { get; set; } = 50;

    [Range(1024, AbsoluteMaxFileBytes)]
    public long MaxFileBytes { get; set; } = 1024 * 1024; // 1 MB
}
```

## 12. Canonical Error Codes

**File:** `backend/VehicleImage.Api/Exceptions/ErrorCodes.cs`  
**Purpose:** Central static catalog of 29 UPPER_SNAKE_CASE error codes shared across validation, URL policy, HTTP, Google Drive retrieval, image decoding, storage, database, and CSV row results.

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
    public const string UnsupportedSource = "UNSUPPORTED_SOURCE";
    public const string ProviderAuthRequired = "PROVIDER_AUTH_REQUIRED";
    public const string StorageFailed = "STORAGE_FAILED";
    public const string DatabasePersistenceFailed = "DATABASE_PERSISTENCE_FAILED";
    public const string VehicleNotFound = "VEHICLE_NOT_FOUND";
    public const string ImageNotFound = "IMAGE_NOT_FOUND";
    public const string ConcurrentUpdate = "CONCURRENT_UPDATE";
    public const string InternalError = "INTERNAL_ERROR";
}
```

## 13. Typed Ingestion Exception

**File:** `backend/VehicleImage.Api/Exceptions/ImageIngestionException.cs`  
**Purpose:** Domain exception carrying machine Code, IsTransient retry flag, UpstreamStatusCode, RetryAfter, and target HttpStatusCode (422 or 500) via static factory methods.

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

## 14. Resource Not Found Exception (404)

**File:** `backend/VehicleImage.Api/Exceptions/ResourceNotFoundException.cs`  
**Purpose:** Domain exception mapped to HTTP 404 for missing vehicles or missing/corrupted stored images.

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

## 15. Concurrent Update Exception (409)

**File:** `backend/VehicleImage.Api/Exceptions/ConcurrentUpdateException.cs`  
**Purpose:** Optimistic concurrency exception thrown when two requests race to replace the same vehicle image simultaneously.

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

## 16. Global Exception Handler

**File:** `backend/VehicleImage.Api/Exceptions/GlobalExceptionHandler.cs`  
**Purpose:** ASP.NET Core IExceptionHandler converting cancellations to HTTP 499, domain/validation exceptions to structured RFC 7807 ImageFailureResponse, and unexpected exceptions to sanitized 500 INTERNAL_ERROR.

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

## 17. Generic Paging Envelope DTO

**File:** `backend/VehicleImage.Api/DTOs/PagedResponse.cs`  
**Purpose:** Reusable pagination record wrapping catalog items alongside current page, pageSize, and totalCount.

```csharp
namespace VehicleImage.Api.DTOs;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
);
```

## 18. Structured Failure Response DTO

**File:** `backend/VehicleImage.Api/DTOs/Images/ImageFailureResponse.cs`  
**Purpose:** RFC 7807 / RFC 9110 error payload exposed on API failures, including retryable hints and TraceId correlation.

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

## 19. Single Vehicle Creation Request DTO

**File:** `backend/VehicleImage.Api/DTOs/Vehicles/CreateVehicleRequest.cs`  
**Purpose:** Validates Make, Model, optional Variant, Year (1886..NextYear via IValidatableObject), and ImageUrl; reused identically by both POST /api/vehicles and CSV row validation.

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

## 20. Replace Vehicle Image Request DTO

**File:** `backend/VehicleImage.Api/DTOs/Vehicles/ReplaceVehicleImageRequest.cs`  
**Purpose:** Request body contract for PUT /api/vehicles/{id}/image requiring a non-empty supplier ImageUrl up to 2048 chars.

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

## 21. Image Submission Success DTO

**File:** `backend/VehicleImage.Api/DTOs/Vehicles/ImageSubmissionResponse.cs`  
**Purpose:** Response contract returned after creating a vehicle or replacing its image, exposing only the internal /api/images/{id} URL.

```csharp
namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record ImageSubmissionResponse(
    Guid VehicleId,
    Guid ImageId,
    string Status,
    string ImageUrl
);
```

## 22. Vehicle Catalog Read DTO

**File:** `backend/VehicleImage.Api/DTOs/Vehicles/VehicleResponse.cs`  
**Purpose:** Read model for catalog listing and vehicle details containing internal image metadata without leaking supplier URLs.

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

## 23. CSV Bulk Import Response DTOs (New on feature/csv-bulk-import)

**File:** `backend/VehicleImage.Api/DTOs/Vehicles/VehicleImportResponse.cs`  
**Purpose:** Summary counters and per-row outcome records (VehicleId/ImageId/ImageUrl on success, ErrorCode/ErrorMessage on failure) returned by POST /api/vehicles/import.

```csharp
namespace VehicleImage.Api.DTOs.Vehicles;

public sealed record VehicleImportResponse(
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    IReadOnlyList<VehicleImportRowResult> Results
);

public sealed record VehicleImportRowResult(
    int RowNumber,
    string? Make,
    string? Model,
    int? Year,
    bool Success,
    Guid? VehicleId,
    Guid? ImageId,
    string? ImageUrl,
    string? ErrorCode,
    string? ErrorMessage
);
```

## 24. Vehicle Domain Entity

**File:** `backend/VehicleImage.Api/Entities/Vehicle.cs`  
**Purpose:** EF Core entity representing a vehicle and its optional 1:1 VehicleImage navigation property.

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

## 25. VehicleImage Domain Entity & ImageStatus Enum

**File:** `backend/VehicleImage.Api/Entities/VehicleImage.cs`  
**Purpose:** EF Core entity persisting source/final URLs, internal StorageKey, decoded dimensions, format, strategy, SHA-256 hash, and ImageStatus (Completed/Failed).

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

## 26. EF Core ApplicationDbContext

**File:** `backend/VehicleImage.Api/Data/ApplicationDbContext.cs`  
**Purpose:** Database context exposing Vehicles and VehicleImages DbSets and loading Fluent API entity configurations from the assembly.

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

## 27. Vehicle Entity Fluent Configuration

**File:** `backend/VehicleImage.Api/Data/Configurations/VehicleConfiguration.cs`  
**Purpose:** Configures Vehicles table constraints (CK_Vehicles_Year), column bounds, SYSUTCDATETIME() defaults, and cascade delete to VehicleImage.

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

## 28. VehicleImage Entity Fluent Configuration

**File:** `backend/VehicleImage.Api/Data/Configurations/VehicleImageConfiguration.cs`  
**Purpose:** Configures unique indexes on VehicleId and StorageKey, fixed-length 64-char Sha256, string conversion for ImageStatus, and positive check constraints.

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

## 29. Initial EF Core Migration

**File:** `backend/VehicleImage.Api/Migrations/20261116094554_InitialCreate.cs`  
**Purpose:** Database migration creating Vehicles, VehicleImages, check constraints, foreign keys, and unique indexes for SQL Server.

```csharp
﻿using System;
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

## 30. RetrievedImage Disposable Stream Model

**File:** `backend/VehicleImage.Api/Models/RetrievedImage.cs`  
**Purpose:** Owns the bounded seekable MemoryStream fetched from an upstream supplier and ensures deterministic stream disposal across multi-candidate fallback.

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

## 31. IngestedImage Handoff Model

**File:** `backend/VehicleImage.Api/Models/IngestedImage.cs`  
**Purpose:** Immutable internal record passing validated image metadata and generated StorageKey from ImageIngestionService to VehicleService.

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

## 32. URL Policy & Redirect Target Validator

**File:** `backend/VehicleImage.Api/Security/UrlValidator.cs`  
**Purpose:** Validates syntax, control chars, length, HTTP/HTTPS scheme, absence of UserInfo, port allowlist, localhost/loopback rejection, IDN domain validity, relative redirect resolution, and query redaction for logs.

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
    private static readonly IdnMapping Idn = new();

    public UrlValidator(IOptions<RetrievalOptions> options)
    {
        _options = options.Value;
    }

    public Uri Validate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw ImageIngestionException.InvalidUrl("The image URL is required.");
        }

        string trimmed = ValidateUrlText(raw, "URL");

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

        string trimmed = ValidateUrlText(locationHeader, "Redirect target URL");

        if (!Uri.TryCreate(currentUri, trimmed, out var targetUri))
        {
            throw ImageIngestionException.InvalidUrl("Redirect target URL could not be resolved.");
        }

        return ValidateUriPolicy(targetUri);
    }

    public static string RedactForLogging(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.GetLeftPart(UriPartial.Path);
    }

    private string ValidateUrlText(string raw, string description)
    {
        string trimmed = raw.Trim();
        if (trimmed.Length > _options.MaxUrlLength)
        {
            throw ImageIngestionException.InvalidUrl(
                $"{description} exceeds the maximum length of {_options.MaxUrlLength} characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            throw ImageIngestionException.InvalidUrl(
                $"{description} contains invalid control characters.");
        }

        return trimmed;
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

        if (!_options.AllowedPorts.Contains(uri.Port))
        {
            throw ImageIngestionException.UnsafeDestination($"Port {uri.Port} is not permitted.");
        }

        string normalizedHost = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalizedHost == "localhost" || normalizedHost.EndsWith(".localhost", StringComparison.Ordinal))
        {
            throw ImageIngestionException.UnsafeDestination("Localhost destinations are not permitted.");
        }

        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            if (!IPAddress.TryParse(uri.DnsSafeHost, out var ipAddress))
            {
                throw ImageIngestionException.InvalidUrl("The IP literal in the URL is malformed.");
            }

            if (IPAddress.IsLoopback(ipAddress))
            {
                throw ImageIngestionException.UnsafeDestination("Loopback destinations are not permitted.");
            }

            return uri;
        }

        // Normalize IDN hostnames and ensure basic hostname validity
        try
        {
            string asciiHost = Idn.GetAscii(uri.DnsSafeHost);
            if (string.IsNullOrWhiteSpace(asciiHost) || !asciiHost.Contains('.'))
            {
                throw ImageIngestionException.InvalidUrl("The URL hostname must be a valid domain name.");
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

## 33. Direct HTTP Retriever, Manual Redirects & Retry Loop

**File:** `backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs`  
**Purpose:** Executes GET requests with per-request timeouts, manual redirect validation (<= 5 hops), 16 KB chunked streaming bounded by MaxDownloadBytes (20 MB), and jittered exponential retries.

```csharp
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

public sealed class DirectImageRetriever
{
    public const string HttpClientName = "ImageRetrievalClient";
    internal const int ClassificationPrefixBytes = 1024;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UrlValidator _urlValidator;
    private readonly RetrievalOptions _retrievalOptions;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<DirectImageRetriever> _logger;

    public DirectImageRetriever(
        IHttpClientFactory httpClientFactory,
        UrlValidator urlValidator,
        IOptions<RetrievalOptions> retrievalOptions,
        IOptions<RetryOptions> retryOptions,
        ILogger<DirectImageRetriever> logger)
    {
        _httpClientFactory = httpClientFactory;
        _urlValidator = urlValidator;
        _retrievalOptions = retrievalOptions.Value;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    internal async Task<(RetrievedImage Image, string? Charset)> FetchWithRetriesAsync(
        Uri source,
        string strategyName,
        CancellationToken cancellationToken = default,
        bool allowHtml = true)
    {
        ArgumentNullException.ThrowIfNull(source);

        int maxAttempts = Math.Max(1, _retryOptions.MaxAttempts);
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                return await ExecuteSingleFetchAsync(source, strategyName, allowHtml, cancellationToken);
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
        bool allowHtml,
        CancellationToken cancellationToken)
    {
        Uri currentUri = _urlValidator.Validate(source.OriginalString);
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_retrievalOptions.TimeoutSeconds));
        CancellationToken effectiveToken = timeoutCts.Token;

        for (int hop = 0; hop <= _retrievalOptions.MaxRedirects; hop++)
        {
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

                MemoryStream buffer = await ReadBoundedWithClassificationAsync(
                    response.Content,
                    mediaType,
                    allowHtml,
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

    private static async Task<MemoryStream> ReadBoundedWithClassificationAsync(
        HttpContent content,
        string? mediaType,
        bool allowHtml,
        long maxBytes,
        CancellationToken effectiveToken,
        CancellationToken callerToken)
    {
        MemoryStream? memoryStream = null;
        try
        {
            await using Stream networkStream = await content.ReadAsStreamAsync(effectiveToken);
            byte[] chunk = new byte[16 * 1024];
            int prefixCap = Math.Min(ClassificationPrefixBytes, chunk.Length);
            int prefixCount = 0;
            long totalRead = 0;

            while (prefixCount < prefixCap)
            {
                int bytesRead = await networkStream.ReadAsync(
                    chunk.AsMemory(prefixCount, prefixCap - prefixCount),
                    effectiveToken);
                if (bytesRead == 0)
                {
                    break;
                }

                totalRead += bytesRead;
                if (totalRead > maxBytes)
                {
                    throw ImageIngestionException.ImageTooLarge(maxBytes);
                }

                prefixCount += bytesRead;
            }

            if (prefixCount == 0)
            {
                return new MemoryStream(0);
            }

            ReadOnlySpan<byte> prefix = chunk.AsSpan(0, prefixCount);
            if (ImageValidator.TryMatchBinaryImageSignature(
                prefix,
                out _,
                out _,
                out _,
                out string? unsupportedFormatName))
            {
                if (unsupportedFormatName is not null)
                {
                    throw ImageIngestionException.UnsupportedImageFormat(unsupportedFormatName);
                }
            }
            else if (!allowHtml || !RetrievalOrchestrator.IsHtmlPayload(mediaType, prefix))
            {
                throw ImageValidator.IsLikelyTextMarkupOrJson(prefix)
                    ? ImageIngestionException.UnsupportedContentType(
                        "The retrieved resource is HTML, XML, SVG or JSON rather than a binary image.")
                    : ImageIngestionException.UnsupportedContentType();
            }

            memoryStream = new MemoryStream();
            memoryStream.Write(chunk, 0, prefixCount);

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
            memoryStream?.Dispose();
            throw;
        }
        catch (OperationCanceledException ex)
        {
            memoryStream?.Dispose();
            throw ImageIngestionException.RequestTimeout(inner: ex);
        }
        catch (ImageIngestionException)
        {
            memoryStream?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            memoryStream?.Dispose();
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

## 34. Public Google Drive URL Download & HTML Confirmation Flow

**File:** `backend/VehicleImage.Api/Retrieval/GoogleDriveImageRetriever.cs`
**Purpose:** Translates public Google Drive sharing URLs into direct download endpoints, handles virus-scan warning confirmation forms, enforces domain boundaries, and rewraps metadata to the customer-submitted URL.

```csharp
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Models;
using VehicleImage.Api.Security;

namespace VehicleImage.Api.Retrieval;

/// <summary>
/// Retrieves publicly shared ("Anyone with the link") Google Drive image files by translating
/// sharing URLs into direct-download requests executed through <see cref="DirectImageRetriever"/>
/// (preserving URL validation, timeout, bounded-streaming, and retry guarantees).
/// OAuth and private Google Drive files are intentionally out of scope.
/// </summary>
public sealed partial class GoogleDriveImageRetriever
{
    private const string StrategyName = "GoogleDrive";
    private const string DriveHost = "drive.google.com";
    private const string DriveUserContentHost = "drive.usercontent.google.com";
    private const string DocsHost = "docs.google.com";

    private readonly DirectImageRetriever _directRetriever;
    private readonly UrlValidator _urlValidator;
    private readonly ILogger<GoogleDriveImageRetriever> _logger;

    public GoogleDriveImageRetriever(
        DirectImageRetriever directRetriever,
        UrlValidator urlValidator,
        ILogger<GoogleDriveImageRetriever> logger)
    {
        _directRetriever = directRetriever;
        _urlValidator = urlValidator;
        _logger = logger;
    }

    public bool CanHandle(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            return false;
        }

        string host = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        return host is DriveHost or DriveUserContentHost or DocsHost;
    }

    public async Task<RetrievedImage> RetrieveAsync(
        Uri source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        (string fileId, string? resourceKey) = ParseAndValidateDriveTarget(source);
        Uri downloadUri = BuildDownloadUri(fileId, resourceKey);

        _logger.LogInformation(
            "Delegating Google Drive file {FileId} (hasResourceKey={HasResourceKey}) to public download endpoint.",
            fileId,
            !string.IsNullOrEmpty(resourceKey));

        var (initialResponse, initialCharset) = await FetchDriveResponseAsync(downloadUri, cancellationToken);

        if (!RetrievalOrchestrator.IsHtmlResponse(initialResponse))
        {
            return RewrapSourceUrl(initialResponse, source);
        }

        string initialHtml;
        Uri initialFinalUrl = initialResponse.FinalSourceUrl;
        await using (initialResponse)
        {
            initialHtml = HtmlImageRetriever.DecodeHtml(initialResponse.Content, initialCharset);
        }

        if (TryExtractConfirmationUri(initialHtml, initialFinalUrl, fileId, resourceKey, out Uri? confirmUri) &&
            confirmUri is not null)
        {
            _logger.LogInformation(
                "Google Drive returned a download confirmation page for file {FileId}; following confirmation link.",
                fileId);

            var (confirmedResponse, confirmedCharset) = await FetchDriveResponseAsync(confirmUri, cancellationToken);

            if (!RetrievalOrchestrator.IsHtmlResponse(confirmedResponse))
            {
                return RewrapSourceUrl(confirmedResponse, source);
            }

            string confirmedHtml;
            Uri confirmedFinalUrl = confirmedResponse.FinalSourceUrl;
            await using (confirmedResponse)
            {
                confirmedHtml = HtmlImageRetriever.DecodeHtml(confirmedResponse.Content, confirmedCharset);
            }

            throw ClassifyHtmlFailure(
                confirmedHtml,
                confirmedFinalUrl,
                fallbackMessage: "Google Drive confirmation page could not be resolved into downloadable image bytes.");
        }

        throw ClassifyHtmlFailure(
            initialHtml,
            initialFinalUrl,
            fallbackMessage: "Google Drive returned an HTML page instead of downloadable image bytes. Ensure the file is a supported image shared with 'Anyone with the link'.");
    }

    private async Task<(RetrievedImage Image, string? Charset)> FetchDriveResponseAsync(
        Uri targetUri,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _directRetriever.FetchWithRetriesAsync(targetUri, StrategyName, cancellationToken);
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpNotFound)
        {
            throw ImageIngestionException.HttpNotFound();
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpUnauthorized)
        {
            throw ImageIngestionException.ProviderAuthRequired(
                "This Google Drive file requires authentication and is not publicly accessible. Set sharing to 'Anyone with the link'.");
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpForbidden)
        {
            throw new ImageIngestionException(
                ErrorCodes.HttpForbidden,
                "Access to this Google Drive file was denied or restricted by the owner (HTTP 403).",
                isTransient: false,
                upstreamStatusCode: 403,
                innerException: ex);
        }
        catch (ImageIngestionException ex) when (ex.Code == ErrorCodes.HttpError && ex.UpstreamStatusCode == 400)
        {
            throw ImageIngestionException.UnsupportedSource(
                "Google Drive rejected the download request (HTTP 400). Ensure the URL points to a binary image file rather than a Google Docs, Sheets, or Slides file.");
        }
    }

    private static (string FileId, string? ResourceKey) ParseAndValidateDriveTarget(Uri uri)
    {
        string host = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(uri.Query);

        string? resourceKey = GetQueryValue(query, "resourcekey");
        if (!string.IsNullOrEmpty(resourceKey) && !ResourceKeyRegex().IsMatch(resourceKey))
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive resource key is malformed.");
        }

        if (host == DocsHost)
        {
            throw ImageIngestionException.UnsupportedSource(
                "Google Docs, Sheets, Slides, and Forms URLs are not supported. Share a downloadable image file stored in Google Drive.");
        }

        if (host == DriveUserContentHost)
        {
            if (segments.Length == 1 &&
                string.Equals(segments[0], "download", StringComparison.OrdinalIgnoreCase))
            {
                string? id = GetQueryValue(query, "id");
                return (ValidateFileId(id), resourceKey);
            }

            throw ImageIngestionException.InvalidUrl("Unsupported Google Drive download URL format.");
        }

        if (host != DriveHost)
        {
            throw ImageIngestionException.InvalidUrl("The URL does not belong to Google Drive.");
        }

        if (segments.Length == 0)
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive URL is missing a file path and file ID.");
        }

        // Reject folders and workspace views explicitly
        if (ContainsSegment(segments, "folders") ||
            (string.Equals(segments[0], "drive", StringComparison.OrdinalIgnoreCase) &&
             !ContainsSegment(segments, "file")))
        {
            throw ImageIngestionException.UnsupportedSource(
                "Google Drive folders and directory views are not supported. Provide a sharing URL to an individual image file.");
        }

        // 1. /file/d/{FILE_ID}[/view|edit|preview|...] or /file/u/{n}/d/{FILE_ID}[/...]
        if (string.Equals(segments[0], "file", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length >= 3 &&
                string.Equals(segments[1], "d", StringComparison.OrdinalIgnoreCase))
            {
                return (ValidateFileId(segments[2]), resourceKey);
            }

            if (segments.Length >= 5 &&
                string.Equals(segments[1], "u", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(segments[2], out _) &&
                string.Equals(segments[3], "d", StringComparison.OrdinalIgnoreCase))
            {
                return (ValidateFileId(segments[4]), resourceKey);
            }

            throw ImageIngestionException.InvalidUrl("The Google Drive file URL is missing a valid file ID.");
        }

        // 2. /open?id={FILE_ID} or /uc?id={FILE_ID}[&export=download]
        if (segments.Length == 1 &&
            (string.Equals(segments[0], "open", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(segments[0], "uc", StringComparison.OrdinalIgnoreCase)))
        {
            string? id = GetQueryValue(query, "id");
            return (ValidateFileId(id), resourceKey);
        }

        throw ImageIngestionException.InvalidUrl(
            "Unsupported Google Drive URL format. Expected a sharing URL such as https://drive.google.com/file/d/{FILE_ID}/view.");
    }

    private static string ValidateFileId(string? rawId)
    {
        string trimmed = rawId?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive URL is missing a file ID.");
        }

        if (!FileIdRegex().IsMatch(trimmed))
        {
            throw ImageIngestionException.InvalidUrl("The Google Drive file ID is invalid or malformed.");
        }

        return trimmed;
    }

    private static Uri BuildDownloadUri(string fileId, string? resourceKey)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["export"] = "download",
            ["id"] = fileId
        };

        if (!string.IsNullOrEmpty(resourceKey))
        {
            queryParams["resourcekey"] = resourceKey;
        }

        string url = QueryHelpers.AddQueryString($"https://{DriveHost}/uc", queryParams);
        return new Uri(url, UriKind.Absolute);
    }

    private bool TryExtractConfirmationUri(
        string html,
        Uri currentUri,
        string fileId,
        string? resourceKey,
        out Uri? confirmUri)
    {
        confirmUri = null;
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Modern Google Drive confirmation page uses <form id="download-form" action="https://drive.usercontent.google.com/download" method="get">
        HtmlNode? form = doc.DocumentNode.SelectSingleNode("//form[@id='download-form']")
            ?? doc.DocumentNode.SelectNodes("//form")
                ?.FirstOrDefault(f =>
                {
                    string act = f.GetAttributeValue("action", string.Empty);
                    return act.Contains("usercontent.google.com/download", StringComparison.OrdinalIgnoreCase) ||
                           act.Contains("drive.google.com/uc", StringComparison.OrdinalIgnoreCase);
                });

        if (form is not null)
        {
            string action = WebUtility.HtmlDecode(form.GetAttributeValue("action", string.Empty)).Trim();
            Uri baseActionUri;
            try
            {
                baseActionUri = string.IsNullOrWhiteSpace(action)
                    ? currentUri
                    : _urlValidator.ValidateRedirectTarget(currentUri, action);
            }
            catch (ImageIngestionException)
            {
                return false;
            }

            if (!IsAllowedConfirmationHost(baseActionUri))
            {
                return false;
            }

            var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in QueryHelpers.ParseQuery(baseActionUri.Query))
            {
                string? val = existing.Value.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    parameters[existing.Key] = val;
                }
            }

            HtmlNodeCollection? inputs = form.SelectNodes(".//input[@name]");
            if (inputs is not null)
            {
                foreach (HtmlNode input in inputs)
                {
                    string type = input.GetAttributeValue("type", string.Empty).Trim().ToLowerInvariant();
                    if (type is "submit" or "button" or "reset" or "image")
                    {
                        continue;
                    }

                    string name = WebUtility.HtmlDecode(input.GetAttributeValue("name", string.Empty)).Trim();
                    string value = WebUtility.HtmlDecode(input.GetAttributeValue("value", string.Empty));
                    if (!string.IsNullOrEmpty(name))
                    {
                        parameters[name] = value;
                    }
                }
            }

            if (parameters.Count > 0)
            {
                if (!parameters.ContainsKey("id"))
                {
                    parameters["id"] = fileId;
                }
                if (!parameters.ContainsKey("export"))
                {
                    parameters["export"] = "download";
                }
                if (!string.IsNullOrEmpty(resourceKey) && !parameters.ContainsKey("resourcekey"))
                {
                    parameters["resourcekey"] = resourceKey;
                }

                string cleanBase = baseActionUri.GetLeftPart(UriPartial.Path);
                string builtUrl = QueryHelpers.AddQueryString(cleanBase, parameters);

                try
                {
                    Uri candidate = _urlValidator.Validate(builtUrl);
                    if (IsAllowedConfirmationHost(candidate) &&
                        !string.Equals(candidate.AbsoluteUri, currentUri.AbsoluteUri, StringComparison.Ordinal))
                    {
                        confirmUri = candidate;
                        return true;
                    }
                }
                catch (ImageIngestionException)
                {
                    return false;
                }
            }
        }

        // Legacy anchor confirmation link (<a id="uc-download-link" href="...">)
        HtmlNode? link = doc.DocumentNode.SelectSingleNode("//a[@id='uc-download-link']")
            ?? doc.DocumentNode.SelectNodes("//a[@href]")
                ?.FirstOrDefault(a => a.GetAttributeValue("href", string.Empty).Contains("confirm=", StringComparison.OrdinalIgnoreCase));

        if (link is not null)
        {
            string href = WebUtility.HtmlDecode(link.GetAttributeValue("href", string.Empty)).Trim();
            if (!string.IsNullOrWhiteSpace(href))
            {
                try
                {
                    Uri candidate = _urlValidator.ValidateRedirectTarget(currentUri, href);
                    if (IsAllowedConfirmationHost(candidate) &&
                        !string.Equals(candidate.AbsoluteUri, currentUri.AbsoluteUri, StringComparison.Ordinal))
                    {
                        confirmUri = candidate;
                        return true;
                    }
                }
                catch (ImageIngestionException)
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static ImageIngestionException ClassifyHtmlFailure(
        string html,
        Uri finalUrl,
        string fallbackMessage)
    {
        string finalHost = finalUrl.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        if (finalHost == "accounts.google.com" ||
            finalHost.EndsWith(".accounts.google.com", StringComparison.Ordinal) ||
            finalUrl.AbsolutePath.Contains("ServiceLogin", StringComparison.OrdinalIgnoreCase))
        {
            return ImageIngestionException.ProviderAuthRequired(
                "This Google Drive file is private or requires Google sign-in. Share it as 'Anyone with the link'.");
        }

        string visibleText = ExtractVisibleText(html);

        if (ContainsAny(
            visibleText,
            "file you have requested does not exist",
            "make sure that you have the correct url",
            "page not found",
            "error 404"))
        {
            return ImageIngestionException.HttpNotFound();
        }

        if (ContainsAny(
            visibleText,
            "you need access",
            "ask the owner for access",
            "sign in to continue",
            "sign in"))
        {
            return ImageIngestionException.ProviderAuthRequired(
                "This Google Drive file is not publicly accessible ('Anyone with the link' permission is required).");
        }

        if (ContainsAny(
            visibleText,
            "too many users have viewed or downloaded this file recently",
            "sorry, you can't view or download this file",
            "you don't have permission",
            "download is disabled",
            "owner has",
            "violates our terms of service"))
        {
            return new ImageIngestionException(
                ErrorCodes.HttpForbidden,
                "Google Drive download is restricted by the file owner or temporary download quota limits.",
                isTransient: false,
                upstreamStatusCode: 403);
        }

        if (ContainsAny(
            visibleText,
            "only files with binary content can be downloaded",
            "cannot be exported"))
        {
            return ImageIngestionException.UnsupportedSource(
                "This Google Drive item is not a downloadable binary image file.");
        }

        if (ContainsAny(
            visibleText,
            "can't scan this file for viruses",
            "download anyway"))
        {
            return ImageIngestionException.UnsupportedContentType(
                "Google Drive returned a virus-scan confirmation page that could not be resolved into image bytes.");
        }

        return ImageIngestionException.UnsupportedContentType(fallbackMessage);
    }

    private static string ExtractVisibleText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection? hiddenNodes = doc.DocumentNode.SelectNodes("//script|//style|//noscript");
        if (hiddenNodes is not null)
        {
            foreach (HtmlNode node in hiddenNodes)
            {
                node.Remove();
            }
        }

        string title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? string.Empty;
        string body = doc.DocumentNode.SelectSingleNode("//body")?.InnerText ?? doc.DocumentNode.InnerText ?? string.Empty;
        return WebUtility.HtmlDecode($"{title} {body}").ToLowerInvariant();
    }

    private static bool ContainsAny(string text, params string[] markers)
    {
        foreach (string marker in markers)
        {
            if (text.Contains(marker, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsAllowedConfirmationHost(Uri uri)
    {
        string host = uri.DnsSafeHost.Trim().TrimEnd('.').ToLowerInvariant();
        return host is DriveHost or DriveUserContentHost;
    }

    private static bool ContainsSegment(string[] segments, string target) =>
        segments.Any(s => string.Equals(s, target, StringComparison.OrdinalIgnoreCase));

    private static string? GetQueryValue(Dictionary<string, StringValues> query, string key)
    {
        foreach (var pair in query)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                string? val = pair.Value.FirstOrDefault();
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
        }
        return null;
    }

    private static RetrievedImage RewrapSourceUrl(RetrievedImage fetched, Uri originalSourceUrl)
    {
        if (fetched.SourceUrl == originalSourceUrl)
        {
            return fetched;
        }

        return new RetrievedImage
        {
            Content = fetched.Content,
            ContentType = fetched.ContentType,
            SourceUrl = originalSourceUrl,
            FinalSourceUrl = fetched.FinalSourceUrl,
            FileSize = fetched.FileSize,
            Strategy = fetched.Strategy
        };
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{10,128}$")]
    private static partial Regex FileIdRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{4,128}$")]
    private static partial Regex ResourceKeyRegex();
}
```

## 35. Static HTML Candidate Extractor & Ranker

**File:** `backend/VehicleImage.Api/Retrieval/HtmlImageRetriever.cs`  
**Purpose:** Decodes HTML charset, blocks soft-404 error pages, resolves <base href>, extracts og:image/twitter:image/srcset/data-src/img candidates, scores/deduplicates them, and fetches up to 3 candidates.

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

        int maxToTry = Math.Min(3, Math.Max(1, _options.MaxCandidates));
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
                    cancellationToken,
                    allowHtml: false);

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
                    attempted, UrlValidator.RedactForLogging(candidate.Url), ex.Code);
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

        candidates.Add(new ImageCandidate(
            Url: resolvedUri,
            Source: source,
            DeclaredWidth: declaredWidth,
            DeclaredHeight: declaredHeight,
            Score: GetCandidatePriority(source, declaredWidth, declaredHeight)));
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

    private static int GetCandidatePriority(
        CandidateSourceKind source,
        int? declaredWidth,
        int? declaredHeight)
    {
        int priority = source switch
        {
            CandidateSourceKind.OgImage => 400,
            CandidateSourceKind.TwitterImage => 300,
            CandidateSourceKind.Srcset => 200,
            CandidateSourceKind.DataSrc or CandidateSourceKind.Img => 100,
            _ => 0
        };

        if (declaredWidth >= 400 || declaredHeight >= 300)
        {
            priority += 50;
        }

        return priority;
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

## 36. Single-GET Sniffing Retrieval Orchestrator

**File:** `backend/VehicleImage.Api/Retrieval/RetrievalOrchestrator.cs`  
**Purpose:** Performs one initial GET through DirectImageRetriever and sniffs URL extension, binary magic bytes, SVG markers, and Content-Type to dispatch HTML pages without extra HEAD requests.

```csharp
using System.Runtime.CompilerServices;
using System.Text;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Models;

namespace VehicleImage.Api.Retrieval;

public sealed class RetrievalOrchestrator
{
    private readonly DirectImageRetriever _directRetriever;
    private readonly HtmlImageRetriever _htmlRetriever;
    private readonly GoogleDriveImageRetriever _googleDriveRetriever;

    public RetrievalOrchestrator(
        DirectImageRetriever directRetriever,
        HtmlImageRetriever htmlRetriever,
        GoogleDriveImageRetriever googleDriveRetriever)
    {
        _directRetriever = directRetriever;
        _htmlRetriever = htmlRetriever;
        _googleDriveRetriever = googleDriveRetriever;
    }

    public async IAsyncEnumerable<RetrievedImage> RetrieveCandidatesAsync(
        Uri source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_googleDriveRetriever.CanHandle(source))
        {
            yield return await _googleDriveRetriever.RetrieveAsync(source, cancellationToken);
            yield break;
        }

        // Single initial GET — no mandatory HEAD request (Decision 4 & Section 9.2)
        var (initialFetch, charset) = await _directRetriever.FetchWithRetriesAsync(
            source,
            "Direct",
            cancellationToken,
            allowHtml: true);

        bool transferredOwnership = false;
        try
        {
            if (_googleDriveRetriever.CanHandle(initialFetch.FinalSourceUrl))
            {
                Uri redirectedDriveUrl = initialFetch.FinalSourceUrl;
                initialFetch.Dispose();
                transferredOwnership = true;

                RetrievedImage driveImage = await _googleDriveRetriever.RetrieveAsync(
                    redirectedDriveUrl,
                    cancellationToken);
                yield return new RetrievedImage
                {
                    Content = driveImage.Content,
                    ContentType = driveImage.ContentType,
                    SourceUrl = source,
                    FinalSourceUrl = driveImage.FinalSourceUrl,
                    FileSize = driveImage.FileSize,
                    Strategy = driveImage.Strategy
                };
                yield break;
            }

            if (IsHtmlResponse(initialFetch))
            {
                string html = HtmlImageRetriever.DecodeHtml(initialFetch.Content, charset);
                Uri finalPageUrl = initialFetch.FinalSourceUrl;
                initialFetch.Dispose();
                transferredOwnership = true;

                await using var enumerator = _htmlRetriever
                    .RetrieveCandidatesFromHtmlAsync(html, source, finalPageUrl, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                while (true)
                {
                    bool hasNext;
                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                    }
                    catch (ImageIngestionException ex) when (
                        ex.Code == ErrorCodes.NoImageFound &&
                        (HasKnownImageExtension(finalPageUrl) || HasKnownImageExtension(source)))
                    {
                        // A URL with an image extension returning an HTML error/interstitial page with no usable images
                        // should report UNSUPPORTED_CONTENT_TYPE rather than NO_IMAGE_FOUND.
                        throw ImageIngestionException.UnsupportedContentType(
                            "The retrieved resource is HTML, XML, SVG or JSON rather than a binary image.");
                    }

                    if (!hasNext)
                    {
                        break;
                    }

                    yield return enumerator.Current;
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

    internal static bool IsHtmlResponse(RetrievedImage retrieved)
    {
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        Span<byte> prefix = stackalloc byte[(int)Math.Min(1024, retrieved.Content.Length)];
        int read = retrieved.Content.Read(prefix);
        if (retrieved.Content.CanSeek)
        {
            retrieved.Content.Position = 0;
        }

        return IsHtmlPayload(retrieved.ContentType, prefix[..read]);
    }

    internal static bool IsHtmlPayload(string? contentType, ReadOnlySpan<byte> prefix)
    {
        if (prefix.IsEmpty)
        {
            return IsHtmlMediaType(contentType);
        }

        // Binary image magic bytes always take precedence over Content-Type or URL extension
        if (ImageValidator.TryMatchBinaryImageSignature(prefix, out _, out _, out _, out _))
        {
            return false;
        }

        // If magic bytes are SVG, let ImageValidator reject it as UNSUPPORTED_CONTENT_TYPE
        string sample = Encoding.UTF8.GetString(prefix).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (IsSvgSample(sample))
        {
            return false;
        }

        return IsHtmlMediaType(contentType) ||
               sample.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<head", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<body", StringComparison.OrdinalIgnoreCase) ||
               sample.StartsWith("<meta", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("<img", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("og:image", StringComparison.OrdinalIgnoreCase) ||
               sample.Contains("twitter:image", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHtmlMediaType(string? contentType) =>
        string.Equals(contentType, "text/html", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contentType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsSvgSample(string trimmedSample) =>
        trimmedSample.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
        (trimmedSample.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
         trimmedSample.Contains("<svg", StringComparison.OrdinalIgnoreCase));

    private static bool HasKnownImageExtension(Uri uri)
    {
        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant() is
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" or
            ".tiff" or ".tif" or ".ico" or ".svg" or ".avif" or ".heic";
    }
}
```

## 37. Validated Image Metadata Models

**File:** `backend/VehicleImage.Api/ImageProcessing/ImageMetadata.cs`  
**Purpose:** Defines ImageFormatKind (Jpeg, Png, WebP) and the validated ImageMetadata summary record.

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

## 38. ImageSharp Magic-Byte, Bomb & Full Pixel Validator

**File:** `backend/VehicleImage.Api/ImageProcessing/ImageValidator.cs`  
**Purpose:** Verifies binary signatures, JPEG FF D9 EOI tail, PNG IHDR dimension bounds, IdentifyAsync frame/size limits, full single-threaded LoadAsync<Rgba32> pixel decoding, and SHA-256 hashing.

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
        if (TryMatchBinaryImageSignature(
            header,
            out ImageFormatKind kind,
            out string contentType,
            out string extension,
            out string? unsupportedFormatName))
        {
            if (unsupportedFormatName is not null)
            {
                throw ImageIngestionException.UnsupportedImageFormat(unsupportedFormatName);
            }

            return (kind, contentType, extension);
        }

        // Check for HTML, XML, SVG, or JSON text content -> UNSUPPORTED_CONTENT_TYPE
        if (IsLikelyTextMarkupOrJson(header))
        {
            throw ImageIngestionException.UnsupportedContentType(
                "The retrieved resource is HTML, XML, SVG or JSON rather than a binary image.");
        }

        throw ImageIngestionException.UnsupportedContentType();
    }

    internal static bool TryMatchBinaryImageSignature(
        ReadOnlySpan<byte> header,
        out ImageFormatKind kind,
        out string contentType,
        out string extension,
        out string? unsupportedFormatName)
    {
        // JPEG: FF D8 FF
        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            kind = ImageFormatKind.Jpeg;
            contentType = "image/jpeg";
            extension = ".jpg";
            unsupportedFormatName = null;
            return true;
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
            kind = ImageFormatKind.Png;
            contentType = "image/png";
            extension = ".png";
            unsupportedFormatName = null;
            return true;
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
            kind = ImageFormatKind.WebP;
            contentType = "image/webp";
            extension = ".webp";
            unsupportedFormatName = null;
            return true;
        }

        kind = default;
        contentType = string.Empty;
        extension = string.Empty;

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
            unsupportedFormatName = "GIF";
            return true;
        }

        // BMP: "BM"
        if (header.Length >= 2 &&
            header[0] == (byte)'B' &&
            header[1] == (byte)'M')
        {
            unsupportedFormatName = "BMP";
            return true;
        }

        // TIFF: "II*\0" or "MM\0*"
        if (header.Length >= 4 &&
            ((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00) ||
             (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A)))
        {
            unsupportedFormatName = "TIFF";
            return true;
        }

        // ICO: 00 00 01 00
        if (header.Length >= 4 &&
            header[0] == 0x00 &&
            header[1] == 0x00 &&
            header[2] == 0x01 &&
            header[3] == 0x00)
        {
            unsupportedFormatName = "ICO";
            return true;
        }

        // ISO BMFF (AVIF / HEIC): bytes 4..7 == "ftyp"
        if (header.Length >= 12 &&
            header[4] == (byte)'f' &&
            header[5] == (byte)'t' &&
            header[6] == (byte)'y' &&
            header[7] == (byte)'p')
        {
            unsupportedFormatName = "AVIF/HEIF";
            return true;
        }

        unsupportedFormatName = null;
        return false;
    }

    internal static bool IsLikelyTextMarkupOrJson(ReadOnlySpan<byte> header)
    {
        string textSample = Encoding.UTF8.GetString(header).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return textSample.StartsWith('<') ||
               textSample.Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
               textSample.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
               textSample.StartsWith('{') ||
               textSample.StartsWith('[');
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

## 39. Internal Storage Abstraction

**File:** `backend/VehicleImage.Api/Storage/IImageStorage.cs`  
**Purpose:** Storage contract decoupling application services from local disk implementation (SaveAsync, OpenReadAsync, DeleteAsync).

```csharp
namespace VehicleImage.Api.Storage;

public interface IImageStorage
{
    Task<string> SaveAsync(Stream image, string fileExtension, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
```

## 40. Traversal-Safe Atomic Local Disk Storage

**File:** `backend/VehicleImage.Api/Storage/LocalFileStorage.cs`  
**Purpose:** Enforces strict path canonicalization inside Storage:RootPath, writes to hidden .tmp files with FlushAsync, and atomically swaps to vehicles/yyyy/MM/guid.ext via File.Move.

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

## 41. Image Ingestion Service Contract

**File:** `backend/VehicleImage.Api/Services/IImageIngestionService.cs`  
**Purpose:** Defines the single-URL ingestion contract reused by single creation, image replacement, and CSV bulk import.

```csharp
using VehicleImage.Api.Models;

namespace VehicleImage.Api.Services;

public interface IImageIngestionService
{
    Task<IngestedImage> IngestAsync(string imageUrl, CancellationToken cancellationToken = default);
}
```

## 42. Image Ingestion Service

**File:** `backend/VehicleImage.Api/Services/ImageIngestionService.cs`  
**Purpose:** Coordinates UrlValidator -> RetrievalOrchestrator -> ImageValidator -> LocalFileStorage, disposing rejected candidates and surfacing the most specific failure if none succeed.

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
                        UrlValidator.RedactForLogging(retrieved.FinalSourceUrl),
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

## 43. Vehicle Lifecycle Service Contract

**File:** `backend/VehicleImage.Api/Services/IVehicleService.cs`  
**Purpose:** Defines vehicle creation, safe image replacement, paged listing, and detail lookup operations.

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

## 44. Vehicle Persistence & Safe Replacement Service

**File:** `backend/VehicleImage.Api/Services/VehicleService.cs`  
**Purpose:** Manages Vehicle + VehicleImage database persistence, compensating deletes on DB/cancellation failures, and optimistic transactional image replacement preserving old images until commit.

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
```

## 45. Image Delivery Service Contract

**File:** `backend/VehicleImage.Api/Services/IImageDeliveryService.cs`  
**Purpose:** Contract and result record for streaming internally stored image blobs.

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

## 46. Image Delivery Service

**File:** `backend/VehicleImage.Api/Services/ImageDeliveryService.cs`  
**Purpose:** Looks up image metadata in DB, opens storage stream (logging integrity errors if missing), and generates sanitized download filenames.

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

## 47. CSV Bulk Import Service Contract (New on feature/csv-bulk-import)

**File:** `backend/VehicleImage.Api/Services/IVehicleImportService.cs`  
**Purpose:** Defines bulk CSV ingestion contract accepting an uploaded IFormFile.

```csharp
using VehicleImage.Api.DTOs.Vehicles;

namespace VehicleImage.Api.Services;

public interface IVehicleImportService
{
    Task<VehicleImportResponse> ImportCsvAsync(IFormFile file, CancellationToken cancellationToken = default);
}
```

## 48. CSV Bulk Import Service (New on feature/csv-bulk-import)

**File:** `backend/VehicleImage.Api/Services/VehicleImportService.cs`  
**Purpose:** Uses CsvHelper 33.1.0 to validate file limits/headers/rows upfront (<= 50 rows) and sequentially processes each row in a fresh AsyncServiceScope via IVehicleService.CreateAsync.

```csharp
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace VehicleImage.Api.Services;

/// <summary>
/// Imports vehicles from a CSV file. Parsing and row validation happen up front; each valid row
/// is then created sequentially through <see cref="IVehicleService.CreateAsync"/>, which already
/// owns image ingestion (URL validation, retrieval, image validation, storage), persistence and the
/// compensating file delete on persistence failure.
/// </summary>
public sealed class VehicleImportService : IVehicleImportService
{
    private const string MakeColumn = "make";
    private const string ModelColumn = "model";
    private const string VariantColumn = "variant";
    private const string YearColumn = "year";
    private const string ImageUrlColumn = "imageurl";

    private static readonly string[] RequiredColumns = [MakeColumn, ModelColumn, YearColumn, ImageUrlColumn];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BulkImportOptions _options;
    private readonly ILogger<VehicleImportService> _logger;

    public VehicleImportService(
        IServiceScopeFactory scopeFactory,
        IOptions<BulkImportOptions> options,
        ILogger<VehicleImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VehicleImportResponse> ImportCsvAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ValidateFile(file);

        // 1. Parse and validate the whole file before any network or database work, so a malformed
        //    or oversized CSV is rejected without partially importing it.
        List<CsvRow> rows;
        await using (Stream stream = file.OpenReadStream())
        {
            rows = await ParseAsync(stream, cancellationToken);
        }

        _logger.LogInformation(
            "Starting CSV vehicle import of {RecordCount} record(s) from {FileName}.",
            rows.Count,
            file.FileName);

        // 2. Process rows sequentially. One failed row never stops the remaining rows.
        var results = new List<VehicleImportRowResult>(rows.Count);
        foreach (CsvRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ProcessRowAsync(row, cancellationToken));
        }

        int successfulRows = results.Count(r => r.Success);

        _logger.LogInformation(
            "CSV vehicle import from {FileName} finished: {SuccessfulRows} succeeded, {FailedRows} failed.",
            file.FileName,
            successfulRows,
            results.Count - successfulRows);

        return new VehicleImportResponse(
            TotalRows: results.Count,
            SuccessfulRows: successfulRows,
            FailedRows: results.Count - successfulRows,
            Results: results);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("The uploaded CSV file is empty.");
        }

        if (file.Length > _options.MaxFileBytes)
        {
            throw new ValidationException(
                $"The uploaded CSV file exceeds the maximum allowed size of {_options.MaxFileBytes / 1024} KB.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Only .csv files are accepted.");
        }
    }

    private async Task<List<CsvRow>> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            DetectDelimiter = false
        };

        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        using var parser = new CsvParser(reader, configuration);

        var rows = new List<CsvRow>();
        int currentRow = 0;

        try
        {
            if (!await parser.ReadAsync())
            {
                throw new ValidationException("The CSV file does not contain a header row.");
            }

            Dictionary<string, int> columns = BuildColumnMap(parser.Record ?? []);
            int headerFieldCount = parser.Record?.Length ?? 0;

            while (await parser.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentRow = parser.Row;
                string[] record = parser.Record ?? [];

                // Skip rows that only contain separators/whitespace (e.g. ",,,,").
                if (record.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (rows.Count >= _options.MaxRecords)
                {
                    throw new ValidationException(
                        $"The CSV file contains more than the maximum of {_options.MaxRecords} vehicle records per import.");
                }

                rows.Add(BuildRow(parser.Row, record, headerFieldCount, columns));
            }
        }
        catch (CsvHelperException ex)
        {
            _logger.LogInformation(ex, "Rejected malformed CSV upload near row {Row}.", currentRow);
            throw new ValidationException(
                currentRow > 0
                    ? $"The CSV file is malformed near row {currentRow} (for example an unterminated quoted field)."
                    : "The CSV file is malformed and could not be parsed.");
        }

        if (rows.Count == 0)
        {
            throw new ValidationException("The CSV file does not contain any vehicle records.");
        }

        return rows;
    }

    private static Dictionary<string, int> BuildColumnMap(string[] header)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            string name = (header[i] ?? string.Empty).Trim().Replace(" ", string.Empty);
            if (name.Length == 0)
            {
                continue;
            }

            if (!columns.TryAdd(name, i))
            {
                throw new ValidationException($"The CSV header contains the duplicate column '{header[i].Trim()}'.");
            }
        }

        string[] missing = RequiredColumns.Where(c => !columns.ContainsKey(c)).ToArray();
        if (missing.Length > 0)
        {
            throw new ValidationException(
                $"The CSV header is missing required column(s): {string.Join(", ", missing.Select(DisplayName))}. " +
                "Required columns: Make, Model, Year, ImageUrl (Variant is optional).");
        }

        return columns;
    }

    private static CsvRow BuildRow(int rowNumber, string[] record, int headerFieldCount, Dictionary<string, int> columns)
    {
        string? Field(string column) =>
            columns.TryGetValue(column, out int index) && index < record.Length
                ? NullIfWhiteSpace(record[index])
                : null;

        string? rawYear = Field(YearColumn);
        int? parsedYear = int.TryParse(rawYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out int yearValue)
            ? yearValue
            : null;

        var row = new CsvRow(
            RowNumber: rowNumber,
            Make: Field(MakeColumn),
            Model: Field(ModelColumn),
            Variant: Field(VariantColumn),
            Year: parsedYear,
            ImageUrl: Field(ImageUrlColumn));

        if (record.Length != headerFieldCount)
        {
            return row with
            {
                ValidationError = $"Row has {record.Length} field(s) but the header defines {headerFieldCount} column(s)."
            };
        }

        if (!parsedYear.HasValue)
        {
            return row with
            {
                ValidationError = rawYear is null
                    ? "Year is required."
                    : $"Year '{rawYear}' is not a valid whole number."
            };
        }

        // Reuse the single-vehicle request DTO and its DataAnnotations/IValidatableObject rules
        // (required fields, lengths, year range) so CSV rows obey exactly the same validation.
        var request = new CreateVehicleRequest
        {
            Make = row.Make ?? string.Empty,
            Model = row.Model ?? string.Empty,
            Variant = row.Variant,
            Year = parsedYear.Value,
            ImageUrl = row.ImageUrl ?? string.Empty
        };

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
        {
            return row with
            {
                ValidationError = string.Join(" ", validationResults
                    .Select(r => r.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m)))
            };
        }

        return row with { Request = request };
    }

    private async Task<VehicleImportRowResult> ProcessRowAsync(CsvRow row, CancellationToken cancellationToken)
    {
        if (row.Request is null)
        {
            return Failed(row, ErrorCodes.ValidationFailed, row.ValidationError ?? "The record is invalid.");
        }

        // A fresh scope (and therefore a fresh DbContext) per row: if one row's SaveChanges fails,
        // its unsaved entities must not stay tracked and be re-inserted with the next row.
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

        try
        {
            ImageSubmissionResponse created = await vehicleService.CreateAsync(row.Request, cancellationToken);

            return new VehicleImportRowResult(
                RowNumber: row.RowNumber,
                Make: row.Make,
                Model: row.Model,
                Year: row.Year,
                Success: true,
                VehicleId: created.VehicleId,
                ImageId: created.ImageId,
                ImageUrl: created.ImageUrl,
                ErrorCode: null,
                ErrorMessage: null);
        }
        catch (ImageIngestionException ex)
        {
            // Expected, client-safe failures (invalid/unsafe URL, HTTP errors, invalid image,
            // storage or database failure). VehicleService has already removed any stored file.
            _logger.LogInformation(
                "CSV import row {Row} failed with [{Code}]: {Message}",
                row.RowNumber,
                ex.Code,
                ex.Message);

            return Failed(row, ex.Code, ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while importing CSV row {Row}; aborting the import.", row.RowNumber);
            throw;
        }
    }

    private static VehicleImportRowResult Failed(CsvRow row, string code, string errorMessage) =>
        new(
            RowNumber: row.RowNumber,
            Make: row.Make,
            Model: row.Model,
            Year: row.Year,
            Success: false,
            VehicleId: null,
            ImageId: null,
            ImageUrl: null,
            ErrorCode: code,
            ErrorMessage: errorMessage);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DisplayName(string column) => column switch
    {
        MakeColumn => "Make",
        ModelColumn => "Model",
        YearColumn => "Year",
        ImageUrlColumn => "ImageUrl",
        _ => column
    };

    private sealed record CsvRow(
        int RowNumber,
        string? Make,
        string? Model,
        string? Variant,
        int? Year,
        string? ImageUrl)
    {
        public CreateVehicleRequest? Request { get; init; }
        public string? ValidationError { get; init; }
    }
}
```

## 49. Vehicles API Controller (Modified on feature/csv-bulk-import)

**File:** `backend/VehicleImage.Api/Controllers/VehiclesController.cs`  
**Purpose:** Exposes POST /api/vehicles, POST /api/vehicles/import (multipart/form-data), GET /api/vehicles, GET /api/vehicles/{id}, and PUT /api/vehicles/{id}/image.

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Services;

namespace VehicleImage.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    // Multipart framing overhead allowed on top of the largest permitted CSV file.
    private const long ImportRequestSizeLimit = BulkImportOptions.AbsoluteMaxFileBytes + 64 * 1024;

    private readonly IVehicleService _vehicleService;
    private readonly IVehicleImportService _importService;

    public VehiclesController(IVehicleService vehicleService, IVehicleImportService importService)
    {
        _vehicleService = vehicleService;
        _importService = importService;
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

    /// <summary>
    /// Imports vehicles from a CSV file (columns: Make, Model, Variant, Year, ImageUrl).
    /// Returns 200 with a per-record result even when some records fail; returns 400 when the
    /// file itself is invalid (missing columns, malformed CSV, too many records, empty file).
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportRequestSizeLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportRequestSizeLimit)]
    [ProducesResponseType(typeof(VehicleImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DTOs.Images.ImageFailureResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<VehicleImportResponse>> Import(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        VehicleImportResponse result = await _importService.ImportCsvAsync(file, cancellationToken);
        return Ok(result);
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

## 50. Internal Images API Controller

**File:** `backend/VehicleImage.Api/Controllers/ImagesController.cs`  
**Purpose:** Serves stored image bytes at GET /api/images/{id} and /download with X-Content-Type-Options: nosniff and 1-year immutable Cache-Control headers.

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

