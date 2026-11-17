# Complete Project Directory & File Reference

This document maps every directory and file in `Suhas2609/URL-to-Image` on branch `feature/google-drive-image-retrieval` (incorporating CSV bulk import and Google Drive image retrieval), separates runtime source from tests, configuration, existing docs, and generated artifacts, compares `main` against feature branches, and explains each file's role in dependency order.

---

## 1. Clean Repository Directory Tree

Excluded build, package, VCS, IDE, and runtime-artifact directories/files: `.git/`, `bin/`, `obj/`, `node_modules/`, `dist/`, `.vs/`, `.idea/`, local runtime SQLite DB (`backend/VehicleImage.Api/vehicleimage.db*`), and locally ingested runtime images (`storage/vehicles/2026/**`).

```text
URL-to-Image/
├── .editorconfig                                    [Configuration] C#/.NET formatting rules
├── .gitignore                                       [Configuration / Modified on feature branch]
├── README.md                                        [Existing Documentation] Top-level project overview
├── VehicleImageSystem.slnx                          [Configuration / Solution] XML solution (.NET 9+/10 format)
│
├── backend/
│   └── VehicleImage.Api/
│       ├── VehicleImage.Api.csproj                  [Configuration / Modified on feature branch] Project & NuGet deps
│       ├── VehicleImage.Api.http                    [Supporting / Scratch HTTP] Default template .http request file
│       ├── appsettings.json                         [Configuration / Modified on feature branch] Base runtime settings
│       ├── appsettings.Development.json             [Configuration] Development logging & connection overrides
│       ├── Program.cs                               [Important Source / Modified on feature branch] Composition root
│       ├── Properties/
│       │   └── launchSettings.json                  [Configuration] Local HTTP/HTTPS launch profiles (:5080, :7206)
│       ├── Configuration/
│       │   ├── BulkImportOptions.cs                 [Important Source / NEW on feature branch]
│       │   ├── HtmlExtractionOptions.cs             [Important Source] HTML scraping bounds & ignore patterns
│       │   ├── ImageValidationOptions.cs            [Important Source] Allowed formats, dimension & pixel ceilings
│       │   ├── RetrievalOptions.cs                  [Important Source] Timeouts, redirects, download byte cap, ports
│       │   ├── RetryOptions.cs                      [Important Source] Exponential backoff, jitter & Retry-After cap
│       │   └── StorageOptions.cs                    [Important Source] Filesystem RootPath option
│       ├── Controllers/
│       │   ├── ImagesController.cs                  [Important Source] Internal image streaming & download endpoints
│       │   └── VehiclesController.cs                [Important Source / Modified on feature branch] Vehicle CRUD + CSV import
│       ├── Data/
│       │   ├── ApplicationDbContext.cs              [Important Source] EF Core DbContext
│       │   └── Configurations/
│       │       ├── VehicleConfiguration.cs          [Important Source] Vehicles table, 1:1 FK, Year check constraint
│       │       └── VehicleImageConfiguration.cs     [Important Source] VehicleImages table, unique indexes & checks
│       ├── DTOs/
│       │   ├── PagedResponse.cs                     [Important Source] Generic paging envelope
│       │   ├── Images/
│       │   │   └── ImageFailureResponse.cs          [Important Source] Structured RFC 7807 / RFC 9110 error DTO
│       │   └── Vehicles/
│       │       ├── CreateVehicleRequest.cs          [Important Source] Input DTO + IValidatableObject year check
│       │       ├── ImageSubmissionResponse.cs       [Important Source] Create/Replace success DTO
│       │       ├── ReplaceVehicleImageRequest.cs    [Important Source] Replace image URL DTO
│       │       ├── VehicleImportResponse.cs         [Important Source / NEW on feature branch] Bulk import DTOs
│       │       └── VehicleResponse.cs               [Important Source] Catalog list/detail DTO
│       ├── Entities/
│       │   ├── Vehicle.cs                           [Important Source] Vehicle domain entity
│       │   └── VehicleImage.cs                      [Important Source] VehicleImage entity + ImageStatus enum
│       ├── Exceptions/
│       │   ├── ConcurrentUpdateException.cs         [Important Source] HTTP 409 conflict exception
│       │   ├── ErrorCodes.cs                        [Important Source] 27 machine-readable error constants
│       │   ├── GlobalExceptionHandler.cs            [Important Source] IExceptionHandler -> ImageFailureResponse
│       │   ├── ImageIngestionException.cs           [Important Source] Typed factory exceptions (422 / 500)
│       │   └── ResourceNotFoundException.cs         [Important Source] HTTP 404 exception
│       ├── ImageProcessing/
│       │   ├── ImageMetadata.cs                     [Important Source] ImageFormatKind enum + ImageMetadata record
│       │   └── ImageValidator.cs                    [Important Source] Magic bytes, bombs, ImageSharp decode, SHA-256
│       ├── Migrations/
│       │   ├── 20261116094554_InitialCreate.cs      [Important Source / Migration] SQL Server DDL Up/Down migration
│       │   ├── 20261116094554_InitialCreate.Designer.cs [Generated by EF Core] Migration model metadata snapshot
│       │   └── ApplicationDbContextModelSnapshot.cs [Generated by EF Core] Current EF Core model snapshot
│       ├── Models/
│       │   ├── IngestedImage.cs                     [Important Source] Internal handoff record (ingestion -> persistence)
│       │   └── RetrievedImage.cs                    [Important Source] Disposable stream wrapper from HTTP retrieval
│       ├── Retrieval/
│       │   ├── DirectImageRetriever.cs              [Important Source] Manual redirects, bounded read, retries, timeouts
│       │   ├── GoogleDriveImageRetriever.cs         [Important Source] Public Google Drive URL download & HTML confirmation flow
│       │   ├── HtmlImageRetriever.cs                [Important Source] HtmlAgilityPack candidate extraction & ranking
│       │   └── RetrievalOrchestrator.cs             [Important Source] Single-GET byte sniffing & strategy dispatch
│       ├── Security/
│       │   └── UrlValidator.cs                      [Important Source] Scheme/port/userinfo/localhost/loopback/IDN & redirect validation
│       ├── Services/
│       │   ├── IImageDeliveryService.cs             [Important Source] Delivery service contract + ImageDeliveryResult
│       │   ├── IImageIngestionService.cs            [Important Source] Ingestion service contract
│       │   ├── IVehicleImportService.cs             [Important Source / NEW on feature branch]
│       │   ├── IVehicleService.cs                   [Important Source] Vehicle catalog/lifecycle contract
│       │   ├── ImageDeliveryService.cs              [Important Source] DB lookup, storage read, safe download name
│       │   ├── ImageIngestionService.cs             [Important Source] Coordinates URL -> Retrieval -> Validate -> Save
│       │   ├── VehicleImportService.cs              [Important Source / NEW on feature branch] CsvHelper two-phase import
│       │   └── VehicleService.cs                    [Important Source] Persistence, safe replace, compensating deletes
│       └── Storage/
│           ├── IImageStorage.cs                     [Important Source] Storage abstraction (Save, OpenRead, Delete)
│           └── LocalFileStorage.cs                  [Important Source] Traversal-safe, atomic temp-move disk storage
│
├── frontend/
│   └── vehicle-image-dashboard/
│       ├── index.html                               [Important Source] Vite HTML shell + Google Fonts (Inter/JetBrains Mono)
│       ├── package.json                             [Configuration] React 19 + Vite 6.2 dependencies & scripts
│       ├── package-lock.json                        [Generated Lockfile] Exact npm dependency graph
│       ├── vite.config.js                           [Configuration] Dev server (:5173) & /api + /health proxy
│       └── src/
│           ├── main.jsx                             [Important Source] ReactDOM root mount in StrictMode
│           ├── App.jsx                              [Important Source] Top header bar + active protection chips + Dashboard
│           ├── index.css                            [Important Source / Modified on feature branch] Design tokens & styles
│           ├── pages/
│           │   └── DashboardPage.jsx                [Important Source / Modified on feature branch] Catalog state & layout
│           ├── services/
│           │   └── vehicleApi.js                    [Important Source / Modified on feature branch] Fetch wrapper + ApiError
│           └── components/
│               ├── BulkImportForm.jsx               [Important Source / NEW on feature branch] CSV upload & results UI
│               ├── ImagePlaceholder.jsx             [Important Source] Fallback SVG box when image load fails
│               ├── ImageReplaceForm.jsx             [Important Source] Inline drawer for PUT /api/vehicles/{id}/image
│               ├── StatusBadge.jsx                  [Important Source] Completed / Processing / Failed status pill
│               ├── VehicleCard.jsx                  [Important Source] Vehicle card, lazy <img>, details, download
│               └── VehicleForm.jsx                  [Important Source / Modified on feature branch] Single URL form & presets
│
├── storage/
│   └── vehicles/
│       └── .gitkeep                                 [Supporting Placeholder] Keeps storage/vehicles/ tracked in Git
│
├── docs/
│   ├── Architecture_and_Decisions.md                [Existing Documentation] Design decisions
│   ├── COMPLETE_CODEBASE_REVIEW.md                  [Existing Documentation] Earlier full-file review snapshot (historical pre-CSV/pre-simplification)
│   └── project-learning/                            [Current Learning Suite] Learning & architecture documentation files
│
└── tests/
    ├── UnitTests/
    │   ├── VehicleImage.UnitTests.csproj            [Supporting / Test Project]
    │   ├── GlobalExceptionHandlerTests.cs           [Supporting / Unit Tests] RFC 7807 / 499 / error status mapping
    │   ├── HtmlCandidateRankingTests.cs             [Supporting / Unit Tests] og:image, srcset, min-dim, soft-404 tests
    │   ├── ImageValidatorTests.cs                   [Supporting / Unit Tests] Magic bytes, bombs, truncation, SHA-256
    │   ├── LocalFileStorageTests.cs                 [Supporting / Unit Tests] Traversal blocking, atomic writes, delete
    │   └── UrlValidatorTests.cs                     [Supporting / Unit Tests] Scheme, userinfo, port, loopback, IDN, redirect validation
    └── IntegrationTests/
        ├── VehicleImage.IntegrationTests.csproj     [Supporting / Test Project]
        ├── ApiFactory.cs                            [Supporting / Test Harness] WebApplicationFactory + SQLite + Mock Supplier Routing
        ├── MockSupplierServer.cs                    [Supporting / Test Double] In-process Kestrel supplier simulator
        ├── DeliveryTests.cs                         [Supporting / Integration Tests] /api/images/{id} headers, 404, probe
        ├── DirectImageIngestionTests.cs             [Supporting / Integration Tests] JPEG/PNG/WebP, MIME masquerade, retries
        ├── HtmlExtractionTests.cs                   [Supporting / Integration Tests] HTML extraction & multi-candidate fallback
        ├── RedirectTests.cs                         [Supporting / Integration Tests] Redirect chain, max hops, unsafe redirect
        ├── ReplacementTests.cs                      [Supporting / Integration Tests] Safe image replacement & failure preservation
        ├── StreamingLimitTests.cs                   [Supporting / Integration Tests] Chunked oversize stream abort tests
        ├── UrlValidationTests.cs                    [Supporting / Integration Tests] Loopback & disallowed scheme rejection end-to-end
        ├── VehicleServiceCancellationTests.cs       [Supporting / Integration Tests] EF interceptor cancellation & cleanup
        └── Fixtures/
            ├── Html/*.html (9 files)                [Supporting / Test Fixtures] Canned HTML supplier pages
            └── Images/* (10 files)                  [Supporting / Test Fixtures] Valid & malicious/corrupted image files
```

### Runtime / Generated Artifacts Excluded From Source Tree

| Path Pattern | Origin | Purpose / Why Excluded |
| :--- | :--- | :--- |
| `backend/VehicleImage.Api/bin/`, `obj/` | `dotnet build` / `dotnet test` | Compiled IL assemblies and MSBuild intermediates |
| `backend/VehicleImage.Api/vehicleimage.db*` | Local SQLite dev runs (`DatabaseProvider=Sqlite`) | Ephemeral local dev database (Git-ignored on feature branch) |
| `storage/vehicles/2026/**` | Local manual/E2E ingestion tests | Locally persisted runtime JPEG/PNG/WebP image blobs (Git-ignored) |
| `frontend/vehicle-image-dashboard/node_modules/` | `npm install` | Third-party JS packages |
| `frontend/vehicle-image-dashboard/dist/` | `npm run build` | Compiled Vite production bundle |
| `backend/VehicleImage.Api/Migrations/*Designer.cs`, `*ModelSnapshot.cs` | `dotnet ef migrations add` | Auto-generated EF Core designer metadata snapshots |

---

## 2. Branch Comparison: `main` (`252579c`) vs `feature/csv-bulk-import` (`49e9d84`) *(Historical Snapshot)*

Verified at the time of `feature/csv-bulk-import` with `git diff --name-status main..feature/csv-bulk-import`: **14 files changed** (`4` backend added, `5` backend modified, `1` frontend added, `4` frontend modified, plus `.gitignore`). *(Later work on `feature/google-drive-image-retrieval` added `GoogleDriveImageRetriever` and simplified URL validation by removing `DestinationValidator` and `SsrfGuardHttpHandler`.)*

| File | Status vs `main` | What Changed & Why |
| :--- | :--- | :--- |
| `.gitignore` | **Modified** | Added `*.db`, `*.db-shm`, `*.db-wal`, `*.sqlite`, `*.sqlite3` so local SQLite dev runs never dirty Git status. |
| `backend/VehicleImage.Api/VehicleImage.Api.csproj` | **Modified** | Added `<PackageReference Include="CsvHelper" Version="33.1.0" />` for RFC 4180 compliant CSV parsing. |
| `backend/VehicleImage.Api/appsettings.json` | **Modified** | Added `"BulkImport": { "MaxRecords": 50, "MaxFileBytes": 1048576 }` configuration block. |
| `backend/VehicleImage.Api/Configuration/BulkImportOptions.cs` | **Added** | Strongly-typed options (`MaxRecords` `[Range(1, 500)]` default `50`; `MaxFileBytes` `[Range(1024, 5MB)]` default `1 MB`; compile-time `AbsoluteMaxFileBytes = 5 MB`). |
| `backend/VehicleImage.Api/DTOs/Vehicles/VehicleImportResponse.cs` | **Added** | Response contract (`TotalRows`, `SuccessfulRows`, `FailedRows`, `Results`) and per-row DTO `VehicleImportRowResult`. |
| `backend/VehicleImage.Api/Services/IVehicleImportService.cs` | **Added** | Abstraction for `ImportCsvAsync(IFormFile file, CancellationToken cancellationToken = default)`. |
| `backend/VehicleImage.Api/Services/VehicleImportService.cs` | **Added** | Parses/validates header + rows upfront with `CsvHelper`, enforces `MaxFileBytes`/`MaxRecords`/`.csv`/row field count/DataAnnotations, and processes each row sequentially in its own `AsyncServiceScope` calling `IVehicleService.CreateAsync`. |
| `backend/VehicleImage.Api/Controllers/VehiclesController.cs` | **Modified** | Injected `IVehicleImportService`; added `POST /api/vehicles/import` (`[Consumes("multipart/form-data")]`, `[RequestSizeLimit]`, `[RequestFormLimits]` set to `5 MB + 64 KB`). |
| `backend/VehicleImage.Api/Program.cs` | **Modified** | Bound/validated `BulkImportOptions` via `.ValidateDataAnnotations().ValidateOnStart()`; registered `AddScoped<IVehicleImportService, VehicleImportService>()`. |
| `frontend/vehicle-image-dashboard/src/services/vehicleApi.js` | **Modified** | Added `importVehiclesCsv(file)` sending `FormData` (`file` field) via `POST /api/vehicles/import`. |
| `frontend/vehicle-image-dashboard/src/components/BulkImportForm.jsx` | **Added** | Complete CSV bulk upload panel: sample CSV downloader, file picker + selected file pill, duplicate-submit prevention, loading state, whole-file error banner, 3-stat summary grid, scrollable per-row results with internal image links and error badges. |
| `frontend/vehicle-image-dashboard/src/components/VehicleForm.jsx` | **Modified** | Removed `panel-sticky` class from `<aside className="panel">` because it now shares the left `.sidebar-stack` above `BulkImportForm`. |
| `frontend/vehicle-image-dashboard/src/pages/DashboardPage.jsx` | **Modified** | Wrapped `VehicleForm` and `BulkImportForm` inside `<div className="sidebar-stack">` and wired `onImportCompleted={handleCatalogRefreshFromPageOne}`. |
| `frontend/vehicle-image-dashboard/src/index.css` | **Modified** | Added CSS classes for `.sidebar-stack`, `.bulk-header-row`, `.csv-file-input`, `.selected-file-pill`, `.import-results-section`, `.import-summary-grid`, `.import-results-list`, `.import-row-card`, etc. |

---

## 3. Detailed File-by-File Breakdown (In Dependency Order)

### Layer A — Root & Solution Configuration
- [`VehicleImageSystem.slnx`](../../VehicleImageSystem.slnx): Solution listing `backend/VehicleImage.Api`, `tests/IntegrationTests`, and `tests/UnitTests`.
- [`.editorconfig`](../../.editorconfig): Enforces consistent C# indentation/encoding styles across IDEs.
- [`.gitignore`](../../.gitignore): Excludes build outputs, IDE folders, `storage/**` (preserving `.gitkeep`), `node_modules/`, `dist/`, and local `.db`/`.sqlite` databases.
- [`README.md`](../../README.md): High-level developer onboarding guide and setup instructions.

### Layer B — Backend Configuration & Contracts (Zero / Leaf Dependencies)
- `Configuration/StorageOptions.cs`: Binds `Storage:RootPath` (`[Required]`). Relative paths are converted to absolute paths against `ContentRootPath` during `Program.cs` `PostConfigure`.
- `Configuration/RetrievalOptions.cs`: Binds `MaxDownloadBytes` (`20 MB`), `TimeoutSeconds` (`15`), `ConnectTimeoutSeconds` (`5`), `MaxRedirects` (`5`), `AllowedPorts` (`[80, 443]`), `MaxUrlLength` (`2048`), `UserAgent` (`VehicleImageSystem/1.0`), `PooledConnectionLifetimeSeconds` (`60`).
- `Configuration/ImageValidationOptions.cs`: Binds `AllowedFormats` (`["Jpeg", "Png", "WebP"]`), `MinWidth`/`MinHeight` (`100`), `MaxWidth`/`MaxHeight` (`10000`), `MaxPixels` (`50_000_000`).
- `Configuration/HtmlExtractionOptions.cs`: Binds `MaxCandidates` (`3`), `MinDeclaredDimension` (`80`), and 17 `IgnoreUrlPatterns` (`logo`, `icon`, `favicon`, `sprite`, `pixel`, etc.).
- `Configuration/RetryOptions.cs`: Binds `MaxAttempts` (`3`), `BaseDelayMs` (`150`), `MaxDelayMs` (`1500`), `EnableJitter` (`true`), `MaxRetryAfterSeconds` (`3`).
- `Configuration/BulkImportOptions.cs`: Binds `MaxRecords` (`50`, range `1..500`) and `MaxFileBytes` (`1 MB`, range `1 KB..5 MB`), with `AbsoluteMaxFileBytes = 5 MB` constant shared with controller request size attributes.
- `Exceptions/ErrorCodes.cs`: Defines all 27 canonical `UPPER_SNAKE_CASE` error constants returned in API errors and CSV row failures.
- `Exceptions/ImageIngestionException.cs`: Exception carrying `Code`, `IsTransient`, `UpstreamStatusCode`, `RetryAfter`, and `HttpStatusCode` (`422` for validation/retrieval failures, `500` for storage/DB failures) via static factory methods with sanitized client-safe messages.
- `Exceptions/ResourceNotFoundException.cs`: Maps missing vehicles/images to `404` (`VEHICLE_NOT_FOUND` / `IMAGE_NOT_FOUND`).
- `Exceptions/ConcurrentUpdateException.cs`: Maps concurrent image replacement races to `409 Conflict` (`CONCURRENT_UPDATE`).
- `Exceptions/GlobalExceptionHandler.cs`: Implements ASP.NET Core `IExceptionHandler`; maps client cancellation (`RequestAborted`) to HTTP `499`, expected domain exceptions to `ImageFailureResponse` (`application/problem+json`), `ValidationException` to `400`, and unexpected exceptions to sanitized `500 INTERNAL_ERROR` with `TraceId`.
- `DTOs/PagedResponse.cs`: Generic `PagedResponse<T>(Items, Page, PageSize, TotalCount)`.
- `DTOs/Images/ImageFailureResponse.cs`: RFC 7807 / RFC 9110 JSON error payload (`code`, `title`, `detail`, `status`, `traceId`, `retryable`, `retryAfterSeconds`, `type`).
- `DTOs/Vehicles/CreateVehicleRequest.cs`: Request DTO with `[Required]`, `[StringLength]`, and `IValidatableObject` validating `1886 <= Year <= DateTime.UtcNow.Year + 1`.
- `DTOs/Vehicles/ReplaceVehicleImageRequest.cs`: Request DTO with required `ImageUrl` (`1..2048` chars).
- `DTOs/Vehicles/ImageSubmissionResponse.cs`: Success DTO for single create & replace (`VehicleId`, `ImageId`, `Status`, `ImageUrl`).
- `DTOs/Vehicles/VehicleResponse.cs`: Read DTO combining `Vehicle` metadata and `VehicleImage` dimensions/format/internal `/api/images/{id}` URL (never exposing supplier source URLs).
- `DTOs/Vehicles/VehicleImportResponse.cs`: Batch summary DTO (`TotalRows`, `SuccessfulRows`, `FailedRows`, `Results`) and per-row `VehicleImportRowResult`.
- `Models/RetrievedImage.cs`: Disposable stream wrapper (`IDisposable`, `IAsyncDisposable`) holding seekable `MemoryStream Content`, `ContentType`, `SourceUrl`, `FinalSourceUrl`, `FileSize`, and `Strategy`.
- `Models/IngestedImage.cs`: Immutable record carrying validated metadata + generated `ImageId` and `StorageKey` from `ImageIngestionService` to `VehicleService`.

### Layer C — Domain Entities & Persistence
- `Entities/Vehicle.cs`: Represents a vehicle (`Id`, `Make`, `Model`, `Variant?`, `Year`, timestamps, `VehicleImage? Image`).
- `Entities/VehicleImage.cs`: Defines `ImageStatus` (`Completed`, `Failed`) and stored image metadata (`Id`, `VehicleId`, `SourceUrl`, `FinalSourceUrl`, `StorageKey`, `ContentType`, `FileSize`, `Width`, `Height`, `Format`, `RetrievalStrategy`, `Status`, `FailureReason?`, `Sha256`, timestamps).
- `Data/ApplicationDbContext.cs`: EF Core `DbContext` exposing `Vehicles` and `VehicleImages`, loading Fluent API configs from assembly.
- `Data/Configurations/VehicleConfiguration.cs`: Configures `Vehicles` table, `CK_Vehicles_Year` (`1886..2100`), column lengths, SQL Server `SYSUTCDATETIME()` defaults, and `1:1` cascade delete to `VehicleImage`.
- `Data/Configurations/VehicleImageConfiguration.cs`: Configures `VehicleImages` table, `ValueGeneratedNever()` for `Id`, unique indexes on `VehicleId` and `StorageKey`, fixed 64-char `Sha256`, string enum conversion for `Status`, and positive check constraints (`FileSize > 0`, `Width > 0`, `Height > 0`).
- `Migrations/20261116094554_InitialCreate.cs`: Initial SQL Server migration (`Up`/`Down`). Paired with generated `20261116094554_InitialCreate.Designer.cs` and `ApplicationDbContextModelSnapshot.cs`.

### Layer D — Security, Retrieval, Validation & Storage Pipelines
- `Security/UrlValidator.cs`: Validates syntax, control characters, length (`<= 2048`), HTTP/HTTPS scheme, absence of `UserInfo`, allowed port (`80`, `443`), blocks `localhost` / `*.localhost` and loopback IP literals (`IPAddress.IsLoopback`), validates IDN domain syntax (`!asciiHost.Contains('.')`), resolves relative redirect targets (`ValidateRedirectTarget`), and redacts queries/fragments for logging (`RedactForLogging`).
- `Retrieval/DirectImageRetriever.cs`: Owns `HttpClientName = "ImageRetrievalClient"`, executes HTTP GET with retries (`FetchWithRetriesAsync`), `15s` per-attempt timeout (`CreateLinkedTokenSource`), manual redirect loop (`<= MaxRedirects`) re-validating each hop via `_urlValidator.ValidateRedirectTarget`, HTTP status mapping, and bounded streaming read (`ReadBoundedStreamAsync`) into a `MemoryStream`.
- `Retrieval/GoogleDriveImageRetriever.cs`: Recognizes public Google Drive file links (`drive.google.com/file/d/{id}`, `drive.google.com/open?id=`, `drive.google.com/uc?id=`), fetches direct download URL (`drive.usercontent.google.com/download`), handles virus-scan confirmation forms, and returns `RetrievedImage` (`Strategy = "GoogleDrive"`).
- `Retrieval/HtmlImageRetriever.cs`: Decodes HTML charset (HTTP header or `<meta charset>`), repairs unclosed `<title>`, detects soft-404 error pages, resolves `<base href>`, extracts `og:image`, `twitter:image`, `srcset`/`data-srcset`, `data-src`/`data-original`/`data-lazy-src`, and `<img>`, filters small dimensions / `.svg` / ignored patterns, deduplicates by URI keeping highest score, and fetches up to 3 candidates via `DirectImageRetriever`.
- `Retrieval/RetrievalOrchestrator.cs`: Routes Google Drive URLs to `GoogleDriveImageRetriever`; otherwise issues single initial GET through `DirectImageRetriever`, inspecting image extension, binary magic bytes, SVG prefixes, `Content-Type`, and HTML tags (`IsHtmlResponse`) to either yield direct bytes or delegate to `HtmlImageRetriever`.
- `ImageProcessing/ImageMetadata.cs`: `ImageFormatKind` (`Jpeg`, `Png`, `WebP`) and `ImageMetadata` record.
- `ImageProcessing/ImageValidator.cs`: Inspects 512-byte magic header (rejecting GIF/BMP/TIFF/ICO/AVIF and HTML/SVG/JSON), validates JPEG EOI `FF D9`, checks PNG `IHDR` before ImageSharp parsing, calls `Image.IdentifyAsync` (bounds + frame count `== 1`), calls bounded `Image.LoadAsync<Rgba32>` (single-threaded, `256 MB` allocator limit), computes `SHA-256`, and rewinds stream.
- `Storage/IImageStorage.cs` & `Storage/LocalFileStorage.cs`: Traversal-protected local storage saving streams atomically (`temp -> File.Move`) under `vehicles/{yyyy}/{MM}/{guid}{ext}`.

### Layer E — Application Services, Controllers & Composition Root
- `Services/IImageIngestionService.cs` & `Services/ImageIngestionService.cs`: Orchestrates `_urlValidator.Validate` -> `_orchestrator.RetrieveCandidatesAsync` -> `_imageValidator.ValidateAsync` -> `_imageStorage.SaveAsync`, returning `IngestedImage` (remembering last validation exception if all HTML candidates fail).
- `Services/IVehicleService.cs` & `Services/VehicleService.cs`: Owns `CreateAsync` (ingest + save DB + compensating delete on failure/cancel), `ReplaceImageAsync` (lookup -> ingest new -> transaction + optimistic `ImageId` check + insert new -> commit -> delete old), `ListAsync`, and `GetAsync`.
- `Services/IImageDeliveryService.cs` & `Services/ImageDeliveryService.cs`: Looks up `VehicleImage` + `Vehicle`, opens file stream via `IImageStorage.OpenReadAsync` (mapping missing file on disk to `404 IMAGE_NOT_FOUND` with error log), and generates sanitized download filename.
- `Services/IVehicleImportService.cs` & `Services/VehicleImportService.cs`: Implements bulk CSV upload (`ValidateFile` -> upfront `ParseAsync` with `CsvHelper` + `CreateVehicleRequest` validation -> sequential `ProcessRowAsync` creating per-row `AsyncServiceScope` -> `VehicleImportResponse`).
- `Controllers/VehiclesController.cs`: Routes `/api/vehicles`: `Create` (`POST`), `Import` (`POST import`), `List` (`GET`), `GetById` (`GET {id:guid}`), `ReplaceImage` (`PUT {id:guid}/image`).
- `Controllers/ImagesController.cs`: Routes `/api/images`: `Get` (`GET {id:guid}`) and `Download` (`GET {id:guid}/download`) setting `X-Content-Type-Options: nosniff` and `Cache-Control: public, max-age=31536000, immutable`.
- `Program.cs`: Registers options + startup validation, EF Core provider switch (`SqlServer` vs `Sqlite`), ModelState `VALIDATION_FAILED` formatter, `GlobalExceptionHandler`, validation/storage singletons (`UrlValidator`, `ImageValidator`, `IImageStorage`), named `ImageRetrievalClient` (`SocketsHttpHandler` with `AllowAutoRedirect = false`, `UseCookies = false`, gzip/deflate/brotli decompression, connect timeout, header length cap), scoped services, CORS (`http://localhost:5173`, `http://127.0.0.1:5173`), `/health`, OpenAPI, startup storage root guard + write probe, and middleware pipeline.

### Layer F — Frontend (`frontend/vehicle-image-dashboard/`)
- `package.json` & `vite.config.js`: React 19 / Vite 6 app with dev proxy forwarding `/api` and `/health` to `VITE_API_TARGET` (default `http://localhost:5080`).
- `index.html` & `src/main.jsx`: HTML entry point and React `StrictMode` mount.
- `src/services/vehicleApi.js`: Central `fetch` client normalizing JSON RFC 7807 errors and network failures into `ApiError`, exporting `listVehicles`, `getVehicle`, `createVehicle`, `replaceImage`, `importVehiclesCsv`, and `resolveInternalImageUrl`.
- `src/App.jsx`: Page shell displaying header, protection chips (`URL Policy Validation`, `20 MB Stream Cap`, `Magic-Byte & Pixel Decode`, `Immutable Internal Delivery`), and `DashboardPage`.
- `src/pages/DashboardPage.jsx`: Coordinates catalog pagination (`PAGE_SIZE = 12`), loading/error states, `sidebar-stack` (`VehicleForm` + `BulkImportForm`), and `VehicleCard` grid (`key={`${vehicle.id}-${vehicle.imageId}`}`).
- `src/components/VehicleForm.jsx`: Controlled form for single vehicle ingestion with quick demo presets (Wikipedia webpage, Wikimedia direct JPEG, blocked `http://localhost/car.jpg` URL, Google Drive public URL).
- `src/components/BulkImportForm.jsx`: CSV upload form with downloadable sample CSV (demonstrating quoted comma escaping), client-side file metadata pill, duplicate-submission prevention, `multipart/form-data` submission, whole-file error banner, 3 summary stat boxes, and per-row status cards.
- `src/components/VehicleCard.jsx`: Displays stored `/api/images/{id}` with native lazy loading, fallback `ImagePlaceholder`, `StatusBadge`, specs pills, download link, lazy `GET /api/vehicles/{id}` details drawer, and toggleable `ImageReplaceForm`.
- `src/components/ImageReplaceForm.jsx`: Form calling `PUT /api/vehicles/{id}/image`; preserves current card image on failure and displays error banner.
- `src/components/StatusBadge.jsx` & `src/components/ImagePlaceholder.jsx`: Small presentational status pill and SVG image fallback components.
- `src/index.css`: Complete CSS design system and responsive layout styling.

### Layer G — Tests (`tests/`)
- `tests/UnitTests/`: Pure xUnit tests (`UrlValidatorTests`, `ImageValidatorTests`, `LocalFileStorageTests`, `HtmlCandidateRankingTests`, `GlobalExceptionHandlerTests`).
- `tests/IntegrationTests/`: Full ASP.NET Core `WebApplicationFactory<Program>` (`ApiFactory` with SQLite + temp storage + `PostConfigure<HttpClientFactoryOptions>` routing `supplier.test` to loopback `127.0.0.1` and permitting `_allowedMockPort`) paired with an in-process Kestrel `MockSupplierServer` serving local `Fixtures/Html/*` and `Fixtures/Images/*`, plus `UrlValidationTests` and `VehicleServiceCancellationTests` using an EF Core command interceptor to simulate client cancellation mid-commit. *(Note: CSV bulk-import tests have not yet been added to `tests/`.)*
