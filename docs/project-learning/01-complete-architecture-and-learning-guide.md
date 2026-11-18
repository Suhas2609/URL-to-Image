# Complete Architecture & Technical Learning Guide

This guide teaches the **VehicleImageSystem** (`URL-to-Image`) codebase on branch `feature/google-drive-image-retrieval` (built upon `feature/csv-bulk-import` and `main`) from the ground up: first high-level architecture and honest boundaries, then 15 sequential learning modules mapped directly to classes and methods, followed by 20 Mermaid architectural/sequence/ER diagrams and 32 end-to-end execution scenarios.

---

## Part 1: High-Level Architecture & System Boundaries

### 1. Core Architectural Purpose
VehicleImageSystem solves an enterprise ingestion problem: **never trust or hotlink external supplier image URLs at runtime**. When a user submits a vehicle record (either one-by-one via JSON or up to 50 rows via CSV upload), the backend must:
1. Validate the URL against basic URL policy rules (`UrlValidator`) before opening any socket.
2. Fetch the target safely using a configured `SocketsHttpHandler` (`AllowAutoRedirect = false`, `UseCookies = false`, gzip/deflate/brotli decompression, connect timeout), following redirects manually with per-hop URL policy re-validation, capping timeouts and stream bytes, and retrying only safe transient errors.
3. Handle direct image URLs, Google Drive public links, and static HTML listing pages without double-fetching via mandatory `HEAD`.
4. Verify actual binary signatures (`JPEG`, `PNG`, `WebP`), reject spoofing/bombs/corrupted images by full pixel decoding via `SixLabors.ImageSharp`, and compute SHA-256.
5. Persist an independent copy outside the web root via atomic write-then-move (`LocalFileStorage`).
6. Commit metadata (`Vehicle` + `VehicleImage`) transactionally to SQL Server (or SQLite in local dev/test), running compensating file deletes if persistence or client connection aborts.
7. Serve stored images exclusively through `/api/images/{id}` with `X-Content-Type-Options: nosniff` and `immutable` caching.

### 2. Explicit Scope Boundaries (Know These Cold for Interviews)
Being honest about what **is not** built is as important as explaining what **is** built:
- **No Authentication / Authorization Middleware Scheme:** `app.UseAuthorization()` is wired in `Program.cs`, and `AddCors("DashboardCors")` allows `localhost:5173` / `127.0.0.1:5173`, but no JWT/cookie/API-key auth handler is registered yet.
- **No Swagger UI Package:** `AddOpenApi()` + `app.MapOpenApi()` exposes OpenAPI JSON at `/openapi/v1.json` in `Development`, without Swagger UI.
- **No `IImageRetriever` Polymorphic Strategy Interface:** Earlier commits refactored `DirectImageRetriever`, `HtmlImageRetriever`, and `GoogleDriveImageRetriever` into concrete scoped classes orchestrated directly by `RetrievalOrchestrator`.
- **Intentional Basic URL Policy Only (No Advanced Network/DNS SSRF Guard):** Earlier iterations included full IPv4/IPv6 CIDR blocklists, DNS pre-resolution, NAT64/6to4/IPv4-mapped decoding, and socket `ConnectCallback` IP pinning (`DestinationValidator`/`SsrfGuardHttpHandler`). Those were removed to keep this controlled portfolio project simple: `UrlValidator` enforces scheme (`http`/`https`), allowed ports (`80`/`443`), length/control characters, no `UserInfo`, valid domain syntax, and blocks `localhost` / loopback IP literals (`127.0.0.1`, `[::1]`). Untrusted public/multi-tenant deployments should pair this with host/VPC egress firewall rules.
- **No JavaScript Execution Engine (Headless Browser):** `HtmlImageRetriever` parses static HTML using `HtmlAgilityPack`. Single-page apps or anti-bot WAF/JS proof-of-work pages (such as `magnific.com`, which serves HTTP `200 OK` Akamai JS challenge HTML without static `<img>` or `og:image` tags) fail cleanly with `NO_IMAGE_FOUND`.
- **Synchronous Request Processing (No Background Worker Queue):** Both single vehicle ingestion and CSV bulk import (up to 50 rows, processed sequentially) complete within the HTTP request lifecycle.
- **No CSV Row Deduplication or Partial Re-run Token:** Identical vehicles in a CSV create separate `Vehicle` rows (`Guid.NewGuid()`). Duplicate column headers inside the CSV *are* rejected as `400 VALIDATION_FAILED`.
- **Automated Test Coverage:** Unit and integration tests cover the core pipeline (`UrlValidator`, `ImageValidator`, `LocalFileStorage`, `HtmlCandidateRanking`, `GlobalExceptionHandler`, `Delivery`, `DirectImageIngestion`, `HtmlExtraction`, `Redirects`, `Replacement`, `UrlValidation`, `StreamingLimit`, `VehicleServiceCancellation`). CSV bulk ingestion and Google Drive retrieval were verified via builds and live API/browser checks.

---

## Part 2: 15 Sequential Learning Modules

Read these modules in order; each builds on the previous layer.

### Module 1: Startup, Options Validation & Composition Root (`Program.cs`, `Configuration/*`)
- **Files:** `Program.cs`, `Configuration/StorageOptions.cs`, `RetrievalOptions.cs`, `ImageValidationOptions.cs`, `HtmlExtractionOptions.cs`, `RetryOptions.cs`, `BulkImportOptions.cs`, `appsettings.json`, `appsettings.Development.json`.
- **How Startup Works:**
  1. All 6 option classes register via `builder.Services.AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`. Bad config (e.g., `MaxRecords: 999`, `MaxFileBytes > 5 MB`, empty `Storage:RootPath`) crashes fast at boot instead of failing mid-request.
  2. `StorageOptions` uses `.PostConfigure(...)` to resolve relative `RootPath` (default `"../../storage"`) relative to `builder.Environment.ContentRootPath` so `LocalFileStorage` always gets a canonical absolute path regardless of working directory.
  3. `ApplicationDbContext` inspects `DatabaseProvider`: `"Sqlite"` uses `UseSqlite`, anything else defaults to `UseSqlServer`. When SQLite is selected, startup executes `db.Database.EnsureCreated()` inside a scope; SQL Server relies on EF Core migrations (`20261116094554_InitialCreate`).
  4. **Service Lifetimes (Critical Interview Topic):**
     - `Singleton`: `UrlValidator`, `ImageValidator` (owns a shared `SixLabors.ImageSharp.Configuration` with `MemoryAllocator` pool caps), `IImageStorage -> LocalFileStorage`. None depend on scoped `DbContext`.
     - `HttpClientFactory` named client (`DirectImageRetriever.HttpClientName = "ImageRetrievalClient"`): sets `client.Timeout = Timeout.InfiniteTimeSpan` so per-request timeouts can be controlled precisely by `DirectImageRetriever`, and configures a standard `SocketsHttpHandler` (`AllowAutoRedirect = false`, `UseCookies = false`, `AutomaticDecompression = GZip | Deflate | Brotli`, `ConnectTimeout = 5s`, `PooledConnectionLifetime = 60s`, `MaxResponseHeadersLength = 64`).
     - `Scoped`: `DirectImageRetriever`, `HtmlImageRetriever`, `GoogleDriveImageRetriever`, `RetrievalOrchestrator`, `ImageIngestionService`, `ImageDeliveryService`, `VehicleService`, `VehicleImportService`.
  5. **Startup Storage Safety Check (`InitializeAndVerifyStorageRoot`):** Runs right after `builder.Build()` before `app.Run()`. Verifies `Storage:RootPath` is **neither equal to nor inside** `wwwroot` (`env.WebRootPath`), creates the folder, and runs `VerifyStorageRootWritable` (opens a `.vehicle-image-write-check-*.tmp` stream with `FileOptions.WriteThrough`, writes 2 bytes `[0x56, 0x49]`, flushes to disk, deletes the probe).
  6. **Middleware Order:** `UseExceptionHandler()` -> (if Development) `MapOpenApi()` -> `UseCors("DashboardCors")` -> `UseAuthorization()` -> `MapHealthChecks("/health")` -> `MapControllers()`. Placing `UseExceptionHandler()` first ensures exceptions in downstream middleware or controllers are formatted consistently.

### Module 2: Controllers, Validation Shaping & Exception Mapping (`Controllers/*`, `DTOs/*`, `Exceptions/*`)
- **Files:** `VehiclesController.cs`, `ImagesController.cs`, `ErrorCodes.cs`, `ImageIngestionException.cs`, `ResourceNotFoundException.cs`, `ConcurrentUpdateException.cs`, `GlobalExceptionHandler.cs`, `DTOs/Images/ImageFailureResponse.cs`, `DTOs/PagedResponse.cs`, `DTOs/Vehicles/*`.
- **Two Sources of Validation Responses:**
  1. **ASP.NET Model Binding Errors:** Configured in `Program.cs` via `.ConfigureApiBehaviorOptions(InvalidModelStateResponseFactory)`. Collects `ModelState` messages into an `ImageFailureResponse` (`code: "VALIDATION_FAILED"`, `status: 400`, `traceId`) with content type `application/problem+json`.
  2. **Service-Thrown Exceptions (`GlobalExceptionHandler.TryHandleAsync`):**
     - `OperationCanceledException` when `httpContext.RequestAborted.IsCancellationRequested` -> HTTP `499` (returns immediately without writing body if `!Response.HasStarted`).
     - `ImageIngestionException` -> status `422` (or `500` for storage/DB failures), sets `Retry-After` response header if `RetryAfterSeconds > 0`, maps `ex.IsTransient` -> `retryable`.
     - `ResourceNotFoundException` -> HTTP `404` (`VEHICLE_NOT_FOUND` or `IMAGE_NOT_FOUND`).
     - `ConcurrentUpdateException` -> HTTP `409 Conflict` (`CONCURRENT_UPDATE`, `retryable: true`).
     - `System.ComponentModel.DataAnnotations.ValidationException` -> HTTP `400` (`VALIDATION_FAILED`).
     - Any other `Exception` -> logged at Error level, sanitized to `500 INTERNAL_ERROR` with zero stack trace leakage.
- **`ErrorCodes.cs` Taxonomy (27 Constants):** Covers validation (`VALIDATION_FAILED`), URL/network (`INVALID_URL`, `UNSAFE_DESTINATION`, `DNS_RESOLUTION_FAILED`, `CONNECTION_TIMEOUT`, `REQUEST_TIMEOUT`, `TLS_FAILURE`, `REDIRECT_LIMIT_EXCEEDED`), HTTP (`HTTP_NOT_FOUND`, `HTTP_UNAUTHORIZED`, `HTTP_FORBIDDEN`, `HTTP_RATE_LIMITED`, `HTTP_SERVER_ERROR`, `HTTP_ERROR`), content/image (`UNSUPPORTED_CONTENT_TYPE`, `UNSUPPORTED_IMAGE_FORMAT`, `IMAGE_TOO_LARGE`, `IMAGE_DIMENSIONS_EXCEEDED`, `IMAGE_TOO_SMALL`, `IMAGE_DECODE_FAILED`, `NO_IMAGE_FOUND`), and internal/domain (`STORAGE_FAILED`, `DATABASE_PERSISTENCE_FAILED`, `VEHICLE_NOT_FOUND`, `IMAGE_NOT_FOUND`, `CONCURRENT_UPDATE`, `INTERNAL_ERROR`).

### Module 3: Domain Entities, EF Core Mapping & Migrations (`Entities/*`, `Data/*`, `Migrations/*`)
- **Files:** `Vehicle.cs`, `VehicleImage.cs`, `ApplicationDbContext.cs`, `VehicleConfiguration.cs`, `VehicleImageConfiguration.cs`, `20261116094554_InitialCreate.cs`.
- **Schema Design:**
  - `Vehicle` has `Id` (`Guid`), `Make` (max 100), `Model` (max 100), `Variant?` (max 150), `Year`, `CreatedAt`, `UpdatedAt`, and navigation `Image?`.
  - `VehicleImage` has `Id` (`Guid`, `ValueGeneratedNever()` because `ImageIngestionService` assigns `Guid.NewGuid()` *before* saving DB row), `VehicleId` (unique FK with `OnDelete(DeleteBehavior.Cascade)`), `SourceUrl` (2048), `FinalSourceUrl` (2048), `StorageKey` (500, unique index `IX_VehicleImages_StorageKey`), `ContentType` (100), `FileSize` (`> 0`), `Width` (`> 0`), `Height` (`> 0`), `Format` (20), `RetrievalStrategy` (50), `Status` (`ImageStatus.Completed` / `ImageStatus.Failed` stored as string max 30), `FailureReason?`, `Sha256` (`nchar(64)` fixed length), `CreatedAt`, `UpdatedAt`.
  - **Check Constraints (`ToTable`):** `CK_Vehicles_Year` (`1886..2100`), `CK_VehicleImages_FileSize > 0`, `CK_VehicleImages_Width > 0`, `CK_VehicleImages_Height > 0`.
  - **No Partial Rows:** Only `Completed` images get committed on create; failed ingestions return `422` without writing `Vehicle` or `VehicleImage` rows.

### Module 4: URL Policy Validation (`Security/UrlValidator.cs`)
- **File:** `UrlValidator.cs`.
- **What `UrlValidator` Checks:**
  - Rejects `null`/blank strings, lengths `> 2048` (`MaxUrlLength`), control characters (`char.IsControl`), non-`http`/`https` schemes, embedded credentials (`!string.IsNullOrEmpty(uri.UserInfo)`), and non-whitelisted ports (`AllowedPorts`: `80`, `443`).
  - Rejects `localhost` and `*.localhost` (`UNSAFE_DESTINATION`).
  - For IPv4/IPv6 literals: parses `IPAddress` and rejects loopback literals (`IPAddress.IsLoopback(ipAddress)` -> `127.0.0.0/8`, `::1` -> `UNSAFE_DESTINATION`).
  - For hostnames: normalizes internationalized domains via `IdnMapping.GetAscii(uri.DnsSafeHost)` and rejects malformed or single-label hostnames lacking `.` (`!asciiHost.Contains('.')` -> `INVALID_URL`).
  - `ValidateRedirectTarget(currentUri, locationHeader)` resolves relative redirects (`Uri.TryCreate(currentUri, trimmed, out targetUri)`) and applies the exact same `ValidateUriPolicy` rules on every redirect hop.
  - `RedactForLogging(uri)` strips query parameters and fragments (`uri.GetLeftPart(UriPartial.Path)`) to avoid logging tokens/signed URLs.
- **Boundary / Design Note:** Complex DNS pre-resolution, IPv4/IPv6 private CIDR blocklists, NAT64/6to4/IPv4-mapped decoding, and socket `ConnectCallback` IP pinning (`DestinationValidator` / `SsrfGuardHttpHandler`) were intentionally removed for this controlled portfolio workflow. Untrusted multi-tenant deployments should pair `UrlValidator` with host/VPC egress controls.

### Module 5: Header-First Streaming Retrieval, 1 KB Prefix Classification & Bounded Reads (`Retrieval/DirectImageRetriever.cs`, `Models/RetrievedImage.cs`)
- **HttpClient Registration (`Program.cs`):**
  - Registers named client `DirectImageRetriever.HttpClientName` (`"ImageRetrievalClient"`) with `client.Timeout = Timeout.InfiniteTimeSpan` and a `SocketsHttpHandler` configured with `AllowAutoRedirect = false`, `UseCookies = false`, `AutomaticDecompression = GZip | Deflate | Brotli`, `ConnectTimeout = 5s`, `PooledConnectionLifetime = 60s`, and `MaxResponseHeadersLength = 64`.
- **Manual Redirect Loop (`ExecuteSingleFetchAsync`):**
  - Creates a per-attempt linked `CancellationTokenSource` bounded by `Retrieval:TimeoutSeconds` (15s).
  - Loops `hop = 0` up to `MaxRedirects` (5): calls `SendAsync(..., HttpCompletionOption.ResponseHeadersRead, effectiveToken)`.
  - If status is `301`, `302`, `303`, `307`, `308`: checks `hop >= MaxRedirects` (throws `REDIRECT_LIMIT_EXCEEDED`), calls `_urlValidator.ValidateRedirectTarget`, and continues loop.
  - Otherwise validates `2xx` (`404` -> `HttpNotFound`, `401` -> `HttpUnauthorized`, `403` -> `HttpForbidden`, `429` -> parses `Retry-After` Delta or HTTP Date -> `HttpRateLimited`, `5xx` -> `HttpServerError`, others -> `HttpError`).
- **Initial 1 KB Prefix Reading & Early Classification (`ReadBoundedWithClassificationAsync`):**
  - **Classification vs. Full Validation**: The 1 KB inspection is *classification*, not full image validation. It determines whether to continue streaming the payload as an image or webpage, or to abort immediately. Full image integrity, dimensions, decompression bounds, and pixel decoding are performed subsequently by `ImageValidator.ValidateAsync`.
  - Reads up to `ClassificationPrefixBytes = 1024` bytes directly from `networkStream` under `ResponseHeadersRead`:
    1. **BOM & Whitespace Resilient**: Strips UTF-8/Unicode BOM (`\uFEFF`) and leading whitespace (`' '`, `\t`, `\r`, `\n`) before evaluating markup samples.
    2. **Supported Binary Images (`JPEG`, `PNG`, `WebP`)**: Detected via `ImageValidator.TryMatchBinaryImageSignature`. Writes the 1 KB prefix into `MemoryStream` and continues streaming the remaining body.
    3. **Unsupported Formats (`GIF`, `BMP`, `TIFF`, `ICO`, `AVIF/HEIF`)**: Recognized immediately and rejected with `UNSUPPORTED_IMAGE_FORMAT` without buffering the remaining payload.
    4. **Non-Image Streams (PDF, MP4, JSON, SVG)**: If `allowHtml == false` or the payload does not match HTML markers, throws `UNSUPPORTED_CONTENT_TYPE` after reading <= 1 KB, discarding the network stream.
    5. **HTML Webpages (`allowHtml: true`)**: Detected via `RetrievalOrchestrator.IsHtmlPayload` (`Content-Type` or text markers like `<!doctype html`, `<html`, `<head`, `<meta`, `og:image`). Preserves prefix in `MemoryStream` and streams remainder up to `MaxDownloadBytes`.
  - **Prefix Preservation**: The initial 1 KB buffer is written to `memoryStream.Write(chunk, 0, prefixCount)` before continuing to read. Those bytes are never lost.
  - **Complete Response Size Accounting**: Running `totalRead` counts the prefix plus all subsequent chunks against `MaxDownloadBytes` (20 MB), defeating chunked streams with missing or spoofed `Content-Length`.
  - **Stream Disposal & Cancellation**: Stream and `HttpResponseMessage` are cleanly disposed in `finally` and exception blocks; distinguishes caller cancellation (`callerToken`) from HTTP timeout (`effectiveToken`).
- **Transient Retry Loop (`FetchWithRetriesAsync` / `ShouldRetry`):**
  - Max 3 attempts (`Retry:MaxAttempts`). Only retries when `ex.IsTransient == true` AND not in the permanent exclusion list.
  - Eligible transient codes: `DnsResolutionFailed`, `ConnectionTimeout`, `RequestTimeout`, `HttpServerError` (`5xx`), `HttpRateLimited` (`429`), and stream interruption (`ImageDecodeFailed` thrown by `ReadBoundedWithClassificationAsync`).
  - For `429`: respects `Retry-After` if `<= MaxRetryAfterSeconds` (3s); if server demands longer, fails immediately instead of stalling the API worker thread.
  - Otherwise computes exponential backoff `BaseDelayMs * 2^(attempt - 1)` capped at `MaxDelayMs` (1500ms) with optional ±25% jitter (`0.75 + Random.Shared.NextDouble() * 0.5`).

### Module 6: Retrieval Orchestration, Routing & HTML Candidate Extraction (`RetrievalOrchestrator.cs`, `HtmlImageRetriever.cs`)
- **Updated Retrieval Sequence:**
  1. `RetrievalOrchestrator`: Initiates initial fetch via `DirectImageRetriever.FetchWithRetriesAsync(source, "Direct", allowHtml: true)` and routes the result:
     - Google Drive URL -> routed to `GoogleDriveImageRetriever`.
     - HTML response (`IsHtmlResponse == true`) -> routed to `HtmlImageRetriever`.
     - Direct image -> yielded directly to `ImageValidator`.
  2. `DirectImageRetriever`: Executes single GET, inspects initial 1 KB prefix, classifies content, continues bounded streaming, and returns `RetrievedImage`.
  3. `HtmlImageRetriever`: Parses HTML, ranks candidates, and fetches candidate images with `allowHtml: false` (aborting immediately if a candidate returns an HTML error/hotlink page).
  4. `GoogleDriveImageRetriever`: Handles Drive sharing URLs via public `/uc` download endpoint with confirmation-page resolution.
- **Content-First Classification (`RetrievalOrchestrator.IsHtmlResponse`):**
  - Binary image magic bytes (`FF D8 FF`, `89 50 4E 47`, `RIFF....WEBP`) **always take precedence** over `Content-Type` headers or URL extensions. A URL `/car.jpg` returning HTML is correctly classified as HTML; a URL `/vehicle` without extension returning JPEG is correctly classified as an image.
  - Trimmed text starting with `<svg` or `<?xml` + `<svg` returns `false` (passed to `ImageValidator` to produce `UNSUPPORTED_CONTENT_TYPE`).
  - If a URL with an image extension (e.g. `html-masquerade.jpg` or `/photo.jpg`) returns an HTML error page with no usable vehicle images, `RetrievalOrchestrator` maps `NO_IMAGE_FOUND` to `UNSUPPORTED_CONTENT_TYPE` so masquerading non-image files are rejected with the correct semantic code.
- **HTML Pipeline (`HtmlImageRetriever`):**
  1. **Decoding (`DecodeHtml`):** Uses HTTP charset or regex-scans first 2048 ASCII bytes for `<meta charset=...>`, falling back to UTF-8. Registers `CodePagesEncodingProvider` for legacy charsets.
  2. **Pre-parse Fixes:** `EnsureClosedTitleTag` inserts `</title>` if unclosed (prevents `HtmlAgilityPack` swallowing `</head>`/body tags).
  3. **Soft-404 Detection (`IsHtmlErrorPage`):** Checks `<title>` and first `<h1>` against error phrases (`"404 not found"`, `"page not found"`, `"403 forbidden"`, `"access denied"`, etc.) and returns `[]` -> yields `NO_IMAGE_FOUND`.
  4. **Candidate Extraction (`ExtractCandidates`):** Resolves `<base href>` safely via `_urlValidator.ValidateRedirectTarget(pageUrl, baseHref)`. Extracts:
     - `<meta property/name="og:image...">` (excluding `:width`, `:height`, `:type`, `:alt`) -> base score `400`.
     - `<meta property/name="twitter:image">` (excluding `:alt`) -> base score `300`.
     - For each `<img>`: skips if declared `width` or `height` is `< MinDeclaredDimension` (80). Checks `srcset`/`data-srcset` (picks largest `w` width or `x * 1000` density descriptor -> score `200`), `data-src`/`data-original`/`data-lazy-src` (score `100`), and `src` (score `100`). Adds `+50` bonus if `declaredWidth >= 400 || declaredHeight >= 300`.
     - Filters out `data:`, `javascript:`, `about:`, `#`, `.svg`, and `IgnoreUrlPatterns` matching `uri.AbsolutePath` or node `class`/`id`/`alt`.
  5. **Ranking & Fallback (`Rank`, `RetrieveCandidatesFromHtmlAsync`):** Groups duplicate URLs keeping highest score, sorts descending, tries up to `Math.Min(3, Math.Max(1, _options.MaxCandidates))` (3) via `_directRetriever.FetchWithRetriesAsync(candidate.Url, "Html", allowHtml: false)`, and yields each successfully fetched `RetrievedImage` (`Strategy = "Html"`). Because `allowHtml: false` is passed, candidate URLs pointing to HTML error pages abort after reading the 1 KB prefix, cleanly triggering fallback to candidate #2 without downloading megabytes of HTML.

### Module 7: Magic-Byte Inspection, Bomb Defense & Full Decode (`ImageValidator.cs`, `ImageMetadata.cs`)
- **Why Three ImageSharp Stages + Byte Checks?** Trusting extensions or `Content-Type` invites XSS (SVG), polyglots, decompression bombs, and truncated/broken images.
- **Stage 1 — 512-Byte Signature Check (`InspectSignature`):**
  - Empty stream -> `UNSUPPORTED_CONTENT_TYPE`.
  - Whitelisted signatures: JPEG (`FF D8 FF`), PNG (`89 50 4E 47 0D 0A 1A 0A`), WebP (`RIFF....WEBP`). Also verifies `_options.AllowedFormats`.
  - Recognized but banned image formats: GIF (`GIF87a`/`GIF89a`), BMP (`BM`), TIFF (`II*\0`/`MM\0*`), ICO (`00 00 01 00`), AVIF/HEIF (`....ftyp`) -> `UNSUPPORTED_IMAGE_FORMAT`.
  - Text payloads (`<`, `<svg`, `<html`, `{`, `[`) or unknown binary -> `UNSUPPORTED_CONTENT_TYPE`.
- **Stage 2 — JPEG Truncation & PNG IHDR Pre-Check:**
  - For JPEG: scans last 16 bytes for `FF D9` End-Of-Image marker (`HasValidJpegEndOfImage`). Missing EOI throws transient `IMAGE_DECODE_FAILED` so incomplete transfers fail fast.
  - For PNG: reads BigEndian bytes `16..23` directly from header (`IHDR` chunk) to reject oversize width/height/pixel-count bombs *before* invoking ImageSharp metadata parsers.
- **Stage 3 — `Image.IdentifyAsync`:**
  - Uses shared cloned `SixLabors.ImageSharp.Configuration` (`MaxDegreeOfParallelism = 1`, `MemoryAllocator` max pool 64 MB, allocation limit 256 MB).
  - Rejects animated images (`FrameMetadataCollection.Count > 1` -> `UNSUPPORTED_IMAGE_FORMAT`), too large (`width > 10000`, `height > 10000`, or `width * height > 50_000_000` -> `IMAGE_DIMENSIONS_EXCEEDED`), or too small (`< 100x100` -> `IMAGE_TOO_SMALL`).
- **Stage 4 — Full Pixel Decode & Hash:**
  - `await Image.LoadAsync<Rgba32>(decoderOptions, content, cancellationToken)` (`MaxFrames = 1`) decompresses every scanline to catch corrupt bitstreams mid-file, throwing transient `IMAGE_DECODE_FAILED` on decode errors.
  - Rewinds stream, hashes with `SHA256.HashDataAsync`, rewinds stream to `0` again so `LocalFileStorage.SaveAsync` can copy directly without extra allocations.

### Module 8: Internal Storage & Path Safety (`LocalFileStorage.cs`, `IImageStorage.cs`, `StorageOptions.cs`)
- **Constructor:** Validates `Storage:RootPath`, canonicalizes `_rootPath = Path.GetFullPath(...)` and `_rootPathWithSeparator`.
- **Key Generation (`SaveAsync`):** Checks extension against strict `{ ".jpg", ".png", ".webp" }`; ignores external filenames completely and builds `vehicles/{yyyy}/{MM}/{Guid:N}{ext}`.
- **Atomic Temp-to-Final Write:** Writes to `.{Guid:N}.tmp` in target directory with `FileMode.CreateNew` + `FileShare.None` + `FlushAsync`, then calls `File.Move(tempPath, fullPath, overwrite: false)`. In `catch`, `TryDeleteTempFile` removes partial `.tmp` files so crash/cancellation never leaves corrupt final files.
- **Path Traversal Prevention (`ResolveSafeFullPath`):** Rejects empty keys, `..`, `\`, leading `/`, `\0`, `Path.IsPathRooted(storageKey)`, resolves `Path.GetFullPath(Path.Combine(_rootPath, relativeOsPath))`, and asserts prefix match against `_rootPathWithSeparator` (case-sensitive on Linux, case-insensitive on Windows).

### Module 9: Ingestion Orchestration & Resource Disposal (`ImageIngestionService.cs`, `IngestedImage.cs`)
- **Files:** `Services/IImageIngestionService.cs`, `Services/ImageIngestionService.cs`, `Models/IngestedImage.cs`.
- **Execution Flow (`IngestAsync`):**
  1. Calls `_urlValidator.Validate(imageUrl)` up front before any network I/O.
  2. Iterates `await foreach (var retrieved in _orchestrator.RetrieveCandidatesAsync(validatedSourceUri, cancellationToken))`.
  3. Wraps each `retrieved` candidate inside `await using (retrieved)` so every underlying `MemoryStream` (especially rejected HTML candidates) is immediately disposed.
  4. Passes `retrieved.Content` to `_imageValidator.ValidateAsync(...)`. If `ImageIngestionException` occurs (e.g., Candidate #1 is 50x50px or corrupt), logs the redacted URL (`UrlValidator.RedactForLogging`), records `lastValidationFailure`, and `continue`s to the next HTML candidate.
  5. On the first candidate that passes validation, calls `_imageStorage.SaveAsync(retrieved.Content, metadata.FileExtension, cancellationToken)` and returns `IngestedImage` with a fresh `Guid.NewGuid()`.
  6. If the `await foreach` finishes without saving any candidate: throws `lastValidationFailure` (if at least one candidate was fetched and failed validation) or `ImageIngestionException.NoImageFound()` (if 0 candidates).

### Module 10: Transactional Vehicle Lifecycle, Replacement & Compensation (`VehicleService.cs`, `IVehicleService.cs`)
- **Files:** `Services/IVehicleService.cs`, `Services/VehicleService.cs`.
- **1. `CreateAsync`:**
  - Ingests image *outside* any DB transaction (`await _ingestionService.IngestAsync`) so slow network downloads never hold database locks.
  - Builds `Vehicle` (`Trim()` on `Make`, `Model`, nullable trimmed `Variant`) + `VehicleImage` (`Status = Completed`), adds to `_dbContext.Vehicles`, calls `SaveChangesAsync(cancellationToken)`.
  - **Compensating File Delete:** If `SaveChangesAsync` throws `OperationCanceledException` or any `Exception`, calls `CompensateFileDeleteAsync(ingested.StorageKey)` using `CancellationToken.None` (so an aborted client request cannot abort our cleanup!). If cleanup itself fails, logs an `"Orphaned file alert"` at Error level.
- **2. `ReplaceImageAsync` (Zero-Downtime Safe Swap):**
  1. `AsNoTracking()` pre-check verifies vehicle exists (`404 VEHICLE_NOT_FOUND`) before making outbound HTTP calls, capturing `previousImageId` and `previousStorageKey`.
  2. Runs `_ingestionService.IngestAsync(request.ImageUrl, cancellationToken)` to store the new image under a *brand-new* `ImageId` and `StorageKey`. If this fails, existing DB row and file are completely untouched.
  3. Opens explicit transaction (`BeginTransactionAsync`), re-queries tracked `Vehicle` + `Image`. If vehicle vanished -> rollback + compensating delete of new key + `404`. If `vehicle.Image?.Id != previousImageId` -> rollback + compensating delete of new key + `ConcurrentUpdateException` (`409 CONCURRENT_UPDATE`).
  4. Removes old `VehicleImage` row and calls `SaveChangesAsync` *first* so the unique index `IX_VehicleImages_VehicleId` permits inserting the new `VehicleImage` row cleanly inside the same transaction.
  5. Updates `vehicle.UpdatedAt`, inserts new `VehicleImage`, calls `SaveChangesAsync`, sets `commitAttempted = true`, and commits transaction.
  6. **Why `commitAttempted` Matters:** If an error or cancellation happens *after* `SaveChangesAsync` and right as `CommitAsync` runs, the DB commit outcome may be ambiguous; retaining the new key avoids deleting an image that SQL Server might actually have committed, whereas pre-commit failures safely run `CompensateFileDeleteAsync(ingested.StorageKey)`.
  7. After successful commit, deletes `previousStorageKey` with `CancellationToken.None` (best-effort with orphan warning log; never fails the HTTP `200` response).
- **3. `ListAsync` & `GetAsync`:**
  - `ListAsync` clamps `pageSize` (`1..100`), orders newest first (`OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)` for deterministic pagination ties), uses `AsNoTracking()`, and projects `VehicleResponse` containing **only** internal `/api/images/{id}` URLs (never `SourceUrl` or `FinalSourceUrl`).

### Module 11: Internal Image Streaming & Download Delivery (`ImageDeliveryService.cs`, `ImagesController.cs`)
- **Files:** `Services/IImageDeliveryService.cs`, `Services/ImageDeliveryService.cs`, `Controllers/ImagesController.cs`.
- **Delivery Pipeline:**
  - `ImageDeliveryService.OpenImageAsync` queries `VehicleImages` (`AsNoTracking().Include(i => i.Vehicle)`) -> `404 IMAGE_NOT_FOUND` if missing.
  - Calls `_imageStorage.OpenReadAsync` -> catches `FileNotFoundException`, logs `"Data integrity error"` at Error level, and returns sanitized `404` to the client.
  - Derives extension from `StorageKey` (fallback to `ContentType`) and builds SEO/user-friendly `DownloadFileName` via `BuildDownloadFileName` + `SanitizeSegment` (`{make}-{model}-{year}-{shortGuid}{ext}`, max 32 chars per segment, lowercased alphanumerics and hyphens).
  - `ImagesController.Get` and `Download` both set:
    - `X-Content-Type-Options: nosniff` (blocks MIME sniffing XSS)
    - `Cache-Control: public, max-age=31536000, immutable` (safe forever because every `ReplaceImageAsync` generates a brand-new `ImageId`!)
    - `Response.ContentLength = result.FileSize`
    - Returns ASP.NET Core `File(result.Stream, result.ContentType)` (or with `result.DownloadFileName` to trigger `Content-Disposition: attachment`).

### Module 12: CSV Bulk Vehicle Ingestion (`VehicleImportService.cs`, `VehiclesController.Import`, `BulkImportOptions.cs`, `VehicleImportResponse.cs`)
- **Files:** `Configuration/BulkImportOptions.cs`, `DTOs/Vehicles/VehicleImportResponse.cs`, `Services/IVehicleImportService.cs`, `Services/VehicleImportService.cs`, `Controllers/VehiclesController.cs`.
- **Request Limits at Two Gates:**
  1. Kestrel / Form parser limits on `VehiclesController.Import`: `[RequestSizeLimit(ImportRequestSizeLimit)]` and `[RequestFormLimits(MultipartBodyLengthLimit = ImportRequestSizeLimit)]` set to `5 MB + 64 KB` multipart framing (`BulkImportOptions.AbsoluteMaxFileBytes + 64 * 1024`).
  2. Service-level config check: `BulkImportOptions.MaxFileBytes` (default `1 MB`, configurable up to `5 MB`) and `.csv` extension check.
- **Two-Phase Processing (`ImportCsvAsync`):**
  - **Phase 1 — Upfront Parse & Structural Validation (`ValidateFile` + `ParseAsync`):**
    - Uses `CsvHelper 33.1.0` (`CsvParser` with `CultureInfo.InvariantCulture`, `HasHeaderRecord = true`, `IgnoreBlankLines = true`, `TrimOptions = TrimOptions.Trim`, `DetectDelimiter = false`) so quoted fields containing commas (`"Competition, xDrive"`) work properly.
    - `BuildColumnMap`: trims spaces inside header names (`"Image Url"` -> `"imageurl"`), rejects duplicate headers, verifies all 4 required columns (`Make`, `Model`, `Year`, `ImageUrl`) exist (`Variant` is optional).
    - Reads rows sequentially: skips completely blank separator rows (`,,,,`), rejects files exceeding `_options.MaxRecords` (`50`), catches `CsvHelperException` (e.g., unterminated quotes) and throws `ValidationException` (`400 VALIDATION_FAILED`) with exact row number. Rejects files with 0 data rows.
    - For each row (`BuildRow`): checks `record.Length == headerFieldCount`, parses `Year` via `int.TryParse`, and validates a `CreateVehicleRequest` using `System.ComponentModel.DataAnnotations.Validator.TryValidateObject(..., validateAllProperties: true)` so CSV rows obey the **exact same rules** (string lengths, required fields, `1886..NextYear`) as single JSON submissions! Note: row-level validation errors are stored on `CsvRow.ValidationError` instead of aborting the whole file.
  - **Phase 2 — Sequential Scoped Row Execution (`ProcessRowAsync`):**
    - Iterates sequentially across `rows` (checks `cancellationToken.ThrowIfCancellationRequested()` between rows).
    - Pre-invalidated rows immediately return `Failed(row, ErrorCodes.ValidationFailed, ...)`.
    - Valid rows create a brand-new `AsyncServiceScope` via `IServiceScopeFactory.CreateAsyncScope()`, resolve a fresh `IVehicleService`, and call `await vehicleService.CreateAsync(row.Request, cancellationToken)`.
    - **Why Fresh `AsyncServiceScope` Per Row Is Critical:** `ApplicationDbContext` is scoped. If row #3 fails `SaveChangesAsync`, EF Core leaves the invalid tracked `Vehicle` entity inside the change tracker (`EntityState.Added`). Reusing the same `DbContext` for row #4 would call `SaveChangesAsync` and fail again trying to re-insert row #3! A fresh scope per row isolates every `DbContext` completely.
    - Catches `ImageIngestionException` per row -> records `ErrorCode` & `ErrorMessage` and continues (`partial success`).
    - Propagates `OperationCanceledException` and logs + rethrows unexpected `Exception` (so `500` infrastructure errors abort cleanly rather than silently masking bugs).
    - Returns HTTP `200 OK` with `VehicleImportResponse` containing `TotalRows`, `SuccessfulRows`, `FailedRows`, and `Results` (omitting external `ImageUrl`).

### Module 13: React + Vite Frontend Architecture & API Client (`vite.config.js`, `App.jsx`, `DashboardPage.jsx`, `vehicleApi.js`)
- **Files:** `frontend/vehicle-image-dashboard/vite.config.js`, `index.html`, `src/main.jsx`, `src/App.jsx`, `src/pages/DashboardPage.jsx`, `src/services/vehicleApi.js`.
- **Vite Proxy & Separate Hosting:** `vite.config.js` listens on `:5173` and proxies `/api` and `/health` to `VITE_API_TARGET` (`http://localhost:5080`). `vehicleApi.js` uses `import.meta.env.VITE_API_BASE_URL || ''` so same-origin proxy works automatically in dev, while cross-origin deployments can set `VITE_API_BASE_URL` (matching backend `DashboardCors` policy).
- **Error Normalization (`vehicleApi.js`):**
  - `parseResponse` checks `response.ok`. On HTTP failure, parses JSON and extracts `code`, `title`, `detail` (or ASP.NET `errors` object), `status`, `traceId`, `retryable`, `retryAfterSeconds` into a custom `ApiError` subclass.
  - Network failures (`fetch` rejecting) become `ApiError` with `code: 'NETWORK_ERROR'`, `status: 0`, `retryable: true`.
  - Exports `listVehicles`, `getVehicle`, `createVehicle`, `replaceImage`, `importVehiclesCsv` (sends `FormData` without manually setting `Content-Type`, letting browser add `multipart/form-data; boundary=...`), and `resolveInternalImageUrl`.
- **State Orchestration (`DashboardPage.jsx`):**
  - Holds `vehicles`, `page`, `totalCount`, `loading`, `loadError` (`PAGE_SIZE = 12`).
  - `handleCatalogRefreshFromPageOne` jumps to page 1 (or re-fetches page 1 if already on page 1) whenever either `VehicleForm` or `BulkImportForm` succeeds, keeping the newest vehicles visible first.
  - Uses composite React key `key={`${vehicle.id}-${vehicle.imageId}`}` on `<VehicleCard>` so replacing a vehicle's image forces a clean remount of card local state (`imageLoadFailed`, `details`, `showReplaceForm`).

### Module 14: Dashboard Components, Bulk Import UI & Styling (`src/components/*`, `src/index.css`)
- **`VehicleForm.jsx`:** Includes quick demo presets (`Porsche 911` Wikipedia HTML page, `BMW M3` direct Wikimedia JPEG, `Audi RS6` blocked `http://localhost/car.jpg` URL, plus a public Google Drive URL preset), controlled inputs, `submitting` lock, and structured `feedback-banner`.
- **`BulkImportForm.jsx` *(New)*:**
  - `SAMPLE_CSV_CONTENT` generates an in-browser `Blob` download (`sample-vehicles.csv`) including a quoted variant (`"Competition, xDrive"`) showcasing RFC 4180 comma escaping.
  - Displays selected filename + formatted size (`selected-file-pill`).
  - Guards against double-submit (`if (submitting || !selectedFile) return`), clears file input only on HTTP `200` API completion, keeps previous file on `400` whole-file rejection.
  - Renders 3 stat cards (`Total Records`, `Successful`, `Failed`) and a scrollable (`max-height: 360px`) list of row cards linking to internal `/api/images/{id}` on success or displaying `ErrorCode` badge + `ErrorMessage` on failure.
- **`VehicleCard.jsx` & `ImageReplaceForm.jsx`:**
  - Renders `<img src={resolveInternalImageUrl(vehicle.imageUrl)} loading="lazy" decoding="async" />`; switches to `<ImagePlaceholder />` on `onError`.
  - Lazily fetches `GET /api/vehicles/{id}` when `View Details` is clicked (caching in `details` state until `imageId` remounts card).
  - `ImageReplaceForm` submits `PUT /api/vehicles/{id}/image`; if replacement fails, displays error banner inside the drawer (`Existing image preserved`) without altering the displayed vehicle card image.

### Module 15: Automated Test Harness & Live Edge-Case Insights (`tests/*`, `magnific.com` Analysis)
- **`tests/UnitTests` (5 test classes):** Pure unit coverage without network/DB: `UrlValidatorTests`, `ImageValidatorTests`, `LocalFileStorageTests`, `HtmlCandidateRankingTests`, `GlobalExceptionHandlerTests`.
- **`tests/IntegrationTests` (8 test classes + `ApiFactory` + `MockSupplierServer`):**
  - `MockSupplierServer` boots an ephemeral Kestrel server on `127.0.0.1:<randomPort>` serving local `Fixtures/Html/*` and `Fixtures/Images/*`, dynamic chunked oversize streams, redirects, and simulated `429`/`503` flaky routes.
  - `ApiFactory` overrides `DatabaseProvider` to isolated temp `Sqlite`, `Storage:RootPath` to temp dir, tight timeouts/retry delays, adds the mock server port to `Retrieval:AllowedPorts`, and uses `PostConfigure<HttpClientFactoryOptions>(DirectImageRetriever.HttpClientName, ...)` with a test-only `SocketsHttpHandler` (`UseProxy = false` and `ConnectCallback` routing `supplier.test` -> `127.0.0.1`) so `MockSupplierServer` tests work cleanly without modifying production `UrlValidator` loopback blocking (`UrlValidationTests`).
  - `VehicleServiceCancellationTests` injects a custom EF `DbCommandInterceptor` (`CancelOnCommandInterceptor`) to cancel the token *inside SQL INSERT execution* and assert compensating storage deletion works under real EF Core cancellation.
- **Why `https://www.magnific.com/free-psd/silver-sedan-car_3993591.htm` Returns `NO_IMAGE_FOUND`:**
  - Inspected behavior during diagnostics: Akamai Bot Manager returns HTTP `200 OK` with a tiny (2,313-byte) HTML JavaScript proof-of-work challenge page containing zero `og:image`, zero `twitter:image`, and zero `<img>` tags. `RetrievalOrchestrator` sees HTTP 200 HTML -> dispatches to `HtmlImageRetriever` -> `ExtractAndRankCandidates` returns `0` candidates -> throws `NO_IMAGE_FOUND` (`422`). This confirms the pipeline behaves cleanly without attempting to bypass anti-bot protections.

---

## Part 3: 20 Architecture, Pipeline & Security Diagrams (With Explanations)

### Diagram 1: High-Level System Architecture

```mermaid
flowchart LR
    User["User / Browser (:5173)"] --> ReactApp["React 19 + Vite Dashboard"]
    ReactApp -->|"REST JSON & Multipart (Vite /api proxy)"| Api["ASP.NET Core 10 API (:5080)"]
    Api --> Controllers["VehiclesController & ImagesController"]
    Controllers --> CoreServices["VehicleService / VehicleImportService / ImageDeliveryService"]
    CoreServices --> Ingestion["ImageIngestionService"]
    Ingestion --> Security["UrlValidator"]
    Ingestion --> Retrieval["RetrievalOrchestrator (Direct, Html, GoogleDrive)"]
    Retrieval -->|"Configured SocketsHttpHandler"| Suppliers["External Supplier Web / CDN"]
    Ingestion --> Validator["ImageValidator (ImageSharp + SHA-256)"]
    Ingestion --> Storage["LocalFileStorage (Outside wwwroot)"]
    CoreServices --> Db["ApplicationDbContext (SQL Server / SQLite)"]
    Controllers -->|"Streams /api/images/{id}"| Storage
```

**Explanation:** The browser **never** fetches vehicle images directly from supplier websites. Both single submissions (`POST /api/vehicles`) and CSV bulk imports (`POST /api/vehicles/import`) flow into `ImageIngestionService`, pass through URL policy validation, HTTP retrieval, binary image validation, and internal disk storage (`LocalFileStorage`), and only expose internal `/api/images/{id}` URLs back to the React dashboard.

---

### Diagram 2: ASP.NET Core Startup, Options Validation & Middleware Pipeline

```mermaid
flowchart TD
    Start["Program.cs Entry"] --> Opts["Bind 6 Options + ValidateDataAnnotations + ValidateOnStart"]
    Opts --> PostCfg["PostConfigure Storage:RootPath (resolve against ContentRootPath)"]
    PostCfg --> DbReg["Register ApplicationDbContext (SqlServer or Sqlite)"]
    DbReg --> ExShape["Configure InvalidModelStateResponseFactory + GlobalExceptionHandler"]
    ExShape --> HttpReg["Register Singletons + Named ImageRetrievalClient + Scoped Services"]
    HttpReg --> Build["builder.Build()"]
    Build --> StorageCheck["InitializeAndVerifyStorageRoot (Assert outside wwwroot + WriteThrough Probe)"]
    StorageCheck --> SqliteCheck{"DatabaseProvider == Sqlite?"}
    SqliteCheck -->|"Yes"| EnsureDb["db.Database.EnsureCreated()"]
    SqliteCheck -->|"No (SqlServer)"| Mw["Configure Middleware Pipeline"]
    EnsureDb --> Mw
    Mw --> Mw1["UseExceptionHandler()"] --> Mw2["Dev: MapOpenApi()"] --> Mw3["UseCors('DashboardCors')"] --> Mw4["UseAuthorization()"] --> Mw5["MapHealthChecks('/health') & MapControllers()"]
```

**Explanation:** Configuration integrity and disk safety are enforced **before** the server begins accepting traffic. `ValidateOnStart()` catches out-of-range settings immediately, `PostConfigure` resolves relative paths safely, `InitializeAndVerifyStorageRoot` guarantees storage is outside `wwwroot` and genuinely writable, and `UseExceptionHandler()` sits first in the request pipeline so all downstream errors become structured `ImageFailureResponse` payloads.

---

### Diagram 3: End-to-End Single Vehicle Ingestion Sequence

```mermaid
sequenceDiagram
    actor User
    participant UI as VehicleForm
    participant Ctrl as VehiclesController
    participant VService as VehicleService
    participant Ingest as ImageIngestionService
    participant Ret as RetrievalOrchestrator
    participant Val as ImageValidator
    participant Disk as LocalFileStorage
    participant DB as ApplicationDbContext

    User->>UI: Submit Make, Model, Year, ImageUrl
    UI->>Ctrl: POST /api/vehicles (JSON)
    Ctrl->>VService: CreateAsync(request, ct)
    VService->>Ingest: IngestAsync(request.ImageUrl, ct)
    Ingest->>Ingest: UrlValidator.Validate(imageUrl)
    Ingest->>Ret: RetrieveCandidatesAsync(uri, ct)
    Ret-->>Ingest: RetrievedImage (seekable MemoryStream)
    Ingest->>Val: ValidateAsync(content, ct)
    Val-->>Ingest: ImageMetadata (format, dimensions, sha256)
    Ingest->>Disk: SaveAsync(content, ext, ct)
    Disk-->>Ingest: StorageKey (vehicles/yyyy/MM/guid.ext)
    Ingest-->>VService: IngestedImage
    VService->>DB: Add(Vehicle + VehicleImage) + SaveChangesAsync(ct)
    DB-->>VService: Persisted
    VService-->>Ctrl: ImageSubmissionResponse (201 Created)
    Ctrl-->>UI: 201 Created (/api/images/{imageId})
    UI->>Ctrl: onVehicleCreated -> GET /api/vehicles
```

**Explanation:** Network I/O (`IngestAsync`) completes before any database transaction starts, avoiding long-held DB locks. Only once bytes are retrieved, decoded in memory, hashed, and safely moved to disk does `VehicleService` persist the `Vehicle` and `VehicleImage` rows together.

---

### Diagram 4: URL Policy Validation Flow (`UrlValidator`)

```mermaid
flowchart TD
    Raw["Raw ImageUrl String"] --> L1["UrlValidator.Validate"]
    L1 -->|"Empty / >2048 chars / Control chars"| FailUrl["Throw INVALID_URL"]
    L1 -->|"Not http/https or has UserInfo"| FailUrl
    L1 -->|"Port not in {80,443} / localhost / *.localhost / loopback IP"| FailUnsafe["Throw UNSAFE_DESTINATION"]
    L1 -->|"IDN domain lacks '.' or invalid"| FailUrl
    L1 -->|"Valid HTTP/HTTPS URI"| L2["DirectImageRetriever (ImageRetrievalClient)"]
    L2 -->|"301/302/303/307/308"| RedirVal["UrlValidator.ValidateRedirectTarget -> Re-apply policy"]
    RedirVal -->|"Disallowed scheme / port / localhost / loopback"| FailUnsafe
    RedirVal -->|"Valid target"| L2
```

**Explanation:** `UrlValidator` enforces basic structural and destination rules (`http`/`https`, no `UserInfo`, allowed ports `80`/`443`, no `localhost` or loopback IP literals, valid IDN domain syntax) both on initial user input and on every manual redirect `Location` target before opening the next HTTP request.

---

### Diagram 5: `SocketsHttpHandler` HTTP Safeguards Flow

```mermaid
flowchart TD
    Client["HttpClientFactory: DirectImageRetriever.HttpClientName ('ImageRetrievalClient')"] --> H1["AllowAutoRedirect = false (manual loop enforces ValidateRedirectTarget)"]
    Client --> H2["UseCookies = false (no cross-request cookie leakage)"]
    Client --> H3["AutomaticDecompression = GZip | Deflate | Brotli"]
    Client --> H4["ConnectTimeout = 5s & PooledConnectionLifetime = 60s"]
    Client --> H5["MaxResponseHeadersLength = 64 KB"]
    H3 --> StreamCap["DirectImageRetriever.ReadBoundedStreamAsync checks total decompressed bytes <= 20 MB"]
```

**Explanation:** Because `Program.cs` disables auto-redirects and cookies and sets `HttpClient.Timeout = Timeout.InfiniteTimeSpan`, [`DirectImageRetriever`](../../../backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs) retains complete control over per-attempt timeouts (`15s`), redirect hop validation (`<= 5`), and decompressed download size limits (`20 MB`).

---

### Diagram 6: Manual Redirect Validation Loop

```mermaid
flowchart TD
    Start["hop = 0, currentUri = validatedSource"] --> LoopCheck{"hop <= MaxRedirects (5)?"}
    LoopCheck -->|"No"| ErrMax["Throw REDIRECT_LIMIT_EXCEEDED"]
    LoopCheck -->|"Yes"| Send["HttpClient.SendAsync(GET currentUri, ResponseHeadersRead)"]
    Send --> IsRedir{"Status in {301, 302, 303, 307, 308}?"}
    IsRedir -->|"No"| Check2xx["EnsureSuccessOrThrowMappedStatus -> Stream Body"]
    IsRedir -->|"Yes"| HopCap{"hop >= MaxRedirects?"}
    HopCap -->|"Yes"| ErrMax
    HopCap -->|"No"| ReadLoc["Read Location Header"]
    ReadLoc --> ValTarget["currentUri = UrlValidator.ValidateRedirectTarget(currentUri, location)"]
    ValTarget -->|"Localhost / Loopback / Bad Scheme / Port / Empty Location"| Fail["Throw INVALID_URL or UNSAFE_DESTINATION"]
    ValTarget -->|"Valid Target"| Inc["hop++"] --> LoopCheck
```

**Explanation:** Because `AllowAutoRedirect = false`, `HttpClient` yields `3xx` responses directly to `DirectImageRetriever`. Note `hop` counts `0..5` (allowing up to 5 redirect transitions, throwing `REDIRECT_LIMIT_EXCEEDED` on the 6th redirect response), and every relative `Location` header is resolved against `currentUri` and vetted via `UrlValidator.ValidateRedirectTarget` before the next hop executes.

---

### Diagram 7: Transient Retry & Exponential Backoff / `Retry-After` Decision

```mermaid
flowchart TD
    Attempt["ExecuteSingleFetchAsync (attempt++)"] -->|"Success"| Done["Return (RetrievedImage, charset)"]
    Attempt -->|"Caller CancellationToken Canceled"| RethrowCancel["Propagate OperationCanceledException"]
    Attempt -->|"Throws ImageIngestionException ex"| CheckRetry{"ShouldRetry(ex, attempt, maxAttempts=3)?"}
    CheckRetry -->|"!ex.IsTransient OR attempt >= 3 OR Code in Permanent List"| Fail["Rethrow ex (e.g. 404/403/TlsFailure)"]
    CheckRetry -->|"ex.Code == HTTP_RATE_LIMITED (429) AND RetryAfter.HasValue"| CheckRA{"RetryAfter <= MaxRetryAfterSeconds (3s)?"}
    CheckRA -->|"No (e.g. 60s)"| Fail
    CheckRA -->|"Yes"| DelayRA["delay = ex.RetryAfter.Value"]
    CheckRetry -->|"Other Transient (Timeout / DNS / 5xx / Stream Cut)"| CalcExp["cappedMs = min(BaseDelayMs * 2^(attempt-1), MaxDelayMs)"]
    CalcExp --> Jitter{"EnableJitter == true?"}
    Jitter -->|"Yes"| ApplyJ["delay = max(10ms, cappedMs * random(0.75..1.25))"]
    Jitter -->|"No"| DelayRaw["delay = max(10ms, cappedMs)"]
    DelayRA --> Wait["await Task.Delay(delay, ct)"] --> Attempt
    ApplyJ --> Wait
    DelayRaw --> Wait
```

**Explanation:** Retries protect against transient upstream blips without wasting time on permanent client/security errors (`404`, `403`, `INVALID_URL`, `UNSAFE_DESTINATION`, `TLS_FAILURE`). Respecting short `Retry-After` headers (capped at 3s) prevents thread exhaustion from long-wait `429` responses.

---

### Diagram 8: Header-First 1 KB Prefix Reading & Early Content Classification

```mermaid
flowchart TD
    Resp["2xx HttpResponseMessage (Headers Only via ResponseHeadersRead)"] --> Open["await content.ReadAsStreamAsync(effectiveToken)"]
    Open --> PrefixLoop["Read initial chunk up to 1024 bytes directly into stack buffer"]
    PrefixLoop --> PrefixCheck{"Prefix Content Classification"}

    PrefixCheck -->|"Binary Magic Bytes (JPEG, PNG, WebP)"| KeepPrefix["memoryStream = new MemoryStream(); write prefix (0..prefixCount)"]
    PrefixCheck -->|"Unsupported Format (GIF, BMP, TIFF, ICO, AVIF)"| ErrFormat["Throw UNSUPPORTED_IMAGE_FORMAT (early abort without buffering)"]
    PrefixCheck -->|"HTML Webpage (Content-Type or HTML tags) AND allowHtml==true"| KeepPrefix
    PrefixCheck -->|"HTML Webpage AND allowHtml==false (Candidate Fetch)"| ErrHtmlCandidate["Throw UNSUPPORTED_CONTENT_TYPE (early abort, fall back to next candidate)"]
    PrefixCheck -->|"Non-Image / Non-HTML (PDF, MP4, JSON, SVG)"| ErrType["Throw UNSUPPORTED_CONTENT_TYPE (early abort without buffering)"]

    KeepPrefix --> BodyLoop["Read remaining 16 KB chunks from networkStream"]
    BodyLoop -->|"bytesRead == 0 (EOF)"| Finish["Reset MemoryStream.Position = 0 & Return"]
    BodyLoop -->|"bytesRead > 0"| Add["totalRead += bytesRead"]
    Add --> CheckCap{"totalRead > MaxDownloadBytes (20 MB)?"}
    CheckCap -->|"Yes"| Abort["Dispose MemoryStream & Throw IMAGE_TOO_LARGE"]
    CheckCap -->|"No"| Write["memoryStream.Write(chunk, 0, bytesRead)"] --> BodyLoop

    BodyLoop -->|"callerToken canceled"| AbortCancel["Dispose MemoryStream & Rethrow OperationCanceledException"]
    BodyLoop -->|"effectiveToken timeout"| AbortTimeout["Dispose MemoryStream & Throw REQUEST_TIMEOUT"]
    BodyLoop -->|"Network stream broken"| AbortCut["Dispose MemoryStream & Throw IMAGE_DECODE_FAILED (transient)"]
```

**Explanation:** Rather than downloading up to 20 MB before checking content, `DirectImageRetriever` reads up to 1 KB under `ResponseHeadersRead`, evaluates binary signatures and markup prefixes, and aborts immediately if the payload is an unsupported image or non-image stream. When classified as a supported image or HTML, the initial 1 KB is preserved in `MemoryStream` and subsequent chunks are read with running `totalRead` bounded by `MaxDownloadBytes = 20 MB`.

---

### Diagram 9: Overall Retrieval & Routing Decision in `RetrievalOrchestrator`

```mermaid
flowchart TD
    In["Customer submits image URL"] --> DriveCheck{"Host is Google Drive (drive/docs.google.com)?"}
    DriveCheck -->|"Yes"| DriveRet["Route to GoogleDriveImageRetriever.RetrieveAsync"]
    DriveCheck -->|"No"| InitialGet["DirectImageRetriever.FetchWithRetriesAsync(allowHtml: true)"]

    InitialGet --> PrefixClass["Read 1 KB prefix & classify (DirectImageRetriever)"]
    PrefixClass -->|"Non-image / Non-HTML / Unsupported format"| ErrEarly["Early Abort (422 UNSUPPORTED_CONTENT_TYPE / UNSUPPORTED_IMAGE_FORMAT)"]
    PrefixClass -->|"Supported Binary Image (JPEG/PNG/WebP)"| StreamImg["Stream remainder -> RetrievedImage (Strategy = 'Direct')"]
    PrefixClass -->|"HTML Page"| StreamHtml["Stream remainder -> RetrievedImage"]

    StreamImg --> ImageVal["ImageValidator.ValidateAsync (Identify, bomb bounds, pixel decode)"]
    StreamHtml --> CheckFinalDrive{"FinalSourceUrl is Google Drive?"}
    CheckFinalDrive -->|"Yes (Redirect to Drive)"| DriveRet
    CheckFinalDrive -->|"No"| HtmlCandidates["HtmlImageRetriever.RetrieveCandidatesFromHtmlAsync"]

    HtmlCandidates --> Extract["Extract and rank candidates (og:image, twitter:image, srcset, img)"]
    Extract --> CandFetch["Fetch candidate with allowHtml: false"]
    CandFetch -->|"Candidate returns valid image"| ImageVal
    CandFetch -->|"Candidate returns HTML error page"| AbortCand["Abort after 1 KB -> Try next candidate"]
    Extract -->|"0 usable candidates on image-extension URL"| ErrMasquerade["Throw UNSUPPORTED_CONTENT_TYPE (Hotlink/404 masquerade)"]
    Extract -->|"0 candidates on standard webpage"| ErrNoImg["Throw NO_IMAGE_FOUND"]
```

**Explanation:** `RetrievalOrchestrator` coordinates the high-level routing while `DirectImageRetriever` protects system bandwidth with 1 KB prefix classification. Note the distinction: 1 KB prefix inspection is *classification* (dispatching to HTML vs image pipeline), whereas *image validation* (dimensions, pixel decoding, memory allocator limits) is performed after full streaming by `ImageValidator`.

---

### Diagram 10: HTML Extraction, Filtering, Ranking & Multi-Candidate Fallback

```mermaid
flowchart TD
    HtmlIn["Decoded HTML + FinalPageUrl"] --> CloseTitle["EnsureClosedTitleTag(html) + HtmlDocument.LoadHtml"]
    CloseTitle --> ErrCheck{"Title or H1 contains Soft-404 / 403 / 500 error phrase?"}
    ErrCheck -->|"Yes"| NoImg["Throw NO_IMAGE_FOUND"]
    ErrCheck -->|"No"| Base["ResolveBaseUri(<base href> validated via UrlValidator)"]
    Base --> Extract["Extract meta (og:image=400, twitter:image=300) + img (srcset=200, data-src=100, src=100)"]
    Extract --> Filter["Filter: dim < MinDeclaredDimension (80), .svg, data:, js:, IgnoreUrlPatterns"]
    Filter --> Bonus["Add +50 if declaredWidth >= 400 or declaredHeight >= 300"]
    Bonus --> Dedupe["GroupBy Url.AbsoluteUri -> Max Score -> OrderByDescending"]
    Dedupe --> EmptyCheck{"Any candidates remain?"}
    EmptyCheck -->|"No"| NoImg
    EmptyCheck -->|"Yes"| TryLoop["Loop up to min(3, MaxCandidates): DirectImageRetriever.FetchWithRetriesAsync"]
    TryLoop -->|"Candidate succeeds fetch & ImageValidator"| Win["Save to LocalFileStorage & Return IngestedImage"]
    TryLoop -->|"Candidate fails fetch OR ImageValidator"| Next["Log & try next candidate"]
    Next -->|"All <= 3 candidates fail"| LastErr["Throw last failure (or NO_IMAGE_FOUND)"]
```

**Explanation:** Metadata tags (`og:image`, `twitter:image`) rank highest because publisher CMSs explicitly curate them as hero images, followed by responsive `srcset` (choosing the largest width/density entry) and `data-src`/`<img>` elements. Crucially, HTML extraction only fetches & yields candidates; `ImageIngestionService` validates each one and automatically falls back if candidate #1 turns out to be corrupt or tiny.

---

### Diagram 11: `ImageValidator` Validation Stages

```mermaid
flowchart TD
    In["Seekable MemoryStream"] --> EmptyCheck{"Length == 0?"}
    EmptyCheck -->|"Yes"| ErrType["Throw UNSUPPORTED_CONTENT_TYPE"]
    EmptyCheck -->|"No"| Magic["InspectSignature (512-byte magic bytes)"]
    Magic -->|"GIF/BMP/TIFF/ICO/AVIF/HEIF"| ErrFmt["Throw UNSUPPORTED_IMAGE_FORMAT"]
    Magic -->|"HTML/SVG/XML/JSON/Other"| ErrType
    Magic -->|"JPEG/PNG/WebP in AllowedFormats"| JpegEoi{"If JPEG: ends with FF D9 in last 16 bytes?"}
    JpegEoi -->|"No"| ErrTrunc["Throw IMAGE_DECODE_FAILED (transient)"]
    JpegEoi -->|"Yes / Not JPEG"| PngIhdr{"If PNG: IHDR dims > MaxWidth/MaxHeight/MaxPixels?"}
    PngIhdr -->|"Yes"| ErrBomb["Throw IMAGE_DIMENSIONS_EXCEEDED"]
    PngIhdr -->|"No"| Identify["Image.IdentifyAsync (single-thread, 256MB cap)"]
    Identify -->|"FrameCount > 1"| ErrAnim["Throw UNSUPPORTED_IMAGE_FORMAT ('Animated')"]
    Identify -->|"Too large or pixels > 50M"| ErrBomb
    Identify -->|"Width < 100 or Height < 100"| ErrSmall["Throw IMAGE_TOO_SMALL"]
    Identify -->|"Valid bounds"| Decode["Image.LoadAsync<Rgba32> (Full Pixel Decode, MaxFrames=1)"]
    Decode -->|"Corrupted scanlines"| ErrCorrupt["Throw IMAGE_DECODE_FAILED (transient)"]
    Decode -->|"Decoded ok"| Hash["SHA256.HashDataAsync + Reset Position=0 -> Return ImageMetadata"]
```

**Explanation:** `ImageValidator` refuses to rely on HTTP headers or file extensions. It inspects magic bytes, spots truncated JPEGs before decoding, reads PNG `IHDR` dimensions directly to block multi-gigapixel decompression bombs *before* ImageSharp allocates buffers, enforces min/max pixel geometry via `IdentifyAsync`, performs a full pixel decode under a capped `MemoryAllocator`, and finishes with a SHA-256 hash.

---

### Diagram 12: `LocalFileStorage` Atomic Write & Path Traversal Defense

```mermaid
flowchart TD
    Call["SaveAsync(stream, fileExtension)"] --> ExtCheck{"Ext in {.jpg, .png, .webp}?"}
    ExtCheck -->|"No"| ErrExt["Throw STORAGE_FAILED"]
    ExtCheck -->|"Yes"| GenKey["storageKey = vehicles/{yyyy}/{MM}/{Guid:N}{ext}"]
    GenKey --> Resolve["ResolveSafeFullPath(storageKey)"]
    Resolve --> Guard{"Contains '..', '\\', leading '/', '\\0', IsPathRooted, or outside _rootPath?"}
    Guard -->|"Yes"| ErrPath["Throw STORAGE_FAILED"]
    Guard -->|"No"| TempFile["CreateDirectory -> Write & FlushAsync to .{Guid:N}.tmp"]
    TempFile -->|"Exception / Cancel"| CleanTmp["TryDeleteTempFile(tempPath) -> Rethrow"]
    TempFile -->|"Flushed"| Move["File.Move(tempPath, fullPath, overwrite: false)"]
    Move -->|"Success"| RetKey["Return storageKey"]
```

**Explanation:** Supplier filenames never touch disk. Keys are generated exclusively from a GUID and validated lowercase extension, canonicalized inside `_rootPath` (which `Program.cs` proves is outside `wwwroot`), written completely to a hidden `.tmp` file first, and swapped atomically into place via `File.Move`.

---

### Diagram 13: Compensating Delete on DB Persistence Failure

```mermaid
sequenceDiagram
    participant VS as VehicleService
    participant Ingest as ImageIngestionService
    participant Store as LocalFileStorage
    participant DB as ApplicationDbContext

    VS->>Ingest: IngestAsync(imageUrl, ct)
    Ingest->>Store: SaveAsync(stream, ext, ct)
    Store-->>Ingest: "vehicles/2026/11/abc.jpg"
    Ingest-->>VS: IngestedImage
    VS->>DB: Vehicles.Add(vehicle) + SaveChangesAsync(ct)
    DB--xVS: Throws DbUpdateException / OperationCanceledException
    VS->>Store: CompensateFileDeleteAsync("vehicles/2026/11/abc.jpg", CancellationToken.None)
    Note over Store: Uses CancellationToken.None so client disconnect cannot abort file cleanup!
    Store-->>VS: File deleted (or logs "Orphaned file alert" if delete fails)
    VS-->>User: Rethrow OperationCanceledException (499) or DATABASE_PERSISTENCE_FAILED (500)
```

**Explanation:** Because filesystem writes and SQL commits cannot share a two-phase distributed lock here, `VehicleService` writes the file first and uses a **compensating delete** (`CancellationToken.None`) if `SaveChangesAsync` throws or cancels. This eliminates silent orphaned image blobs on persistence failures.

---

### Diagram 14: Safe Image Replacement Sequence & Immutable Cache Strategy

```mermaid
sequenceDiagram
    participant Client
    participant VS as VehicleService
    participant DB as ApplicationDbContext
    participant Ingest as ImageIngestionService
    participant Store as LocalFileStorage

    Client->>VS: PUT /api/vehicles/{id}/image
    VS->>DB: AsNoTracking() lookup vehicle + current Image
    Note over VS: Capture previousImageId & previousStorageKey (404 if missing)
    VS->>Ingest: IngestAsync(newImageUrl, ct)
    Ingest->>Store: SaveAsync -> new StorageKey (new GUID!)
    Ingest-->>VS: New IngestedImage (new ImageId!)
    Note over VS,DB: If IngestAsync fails, old DB row & old file remain 100% untouched!
    VS->>DB: BeginTransactionAsync(ct)
    VS->>DB: Re-load tracked Vehicle + Check vehicle.Image?.Id == previousImageId
    VS->>DB: Remove(old Image) -> SaveChangesAsync (free unique IX_VehicleImages_VehicleId)
    VS->>DB: Add(new Image) + Update vehicle.UpdatedAt -> SaveChangesAsync
    VS->>DB: CommitAsync(ct) (commitAttempted = true)
    VS->>Store: Best-effort DeleteAsync(previousStorageKey, CancellationToken.None)
    VS-->>Client: 200 OK (new ImageId -> /api/images/{newImageId})
```

**Explanation:** Replacement follows a **prepare-then-swap** design. Generating a brand-new `ImageId` on every replacement eliminates stale browser/CDN cache bugs, enabling `/api/images/{id}` to emit `Cache-Control: public, max-age=31536000, immutable` safely. Deleting the old row before inserting the new row inside one DB transaction satisfies SQL Server's unique index on `VehicleId`.

---

### Diagram 15: Concurrent Replacement Conflict (`409 CONCURRENT_UPDATE`) Sequence

```mermaid
sequenceDiagram
    participant ReqA as Request A (new URL 1)
    participant ReqB as Request B (new URL 2)
    participant DB as ApplicationDbContext
    participant Store as LocalFileStorage

    ReqA->>DB: Snapshot check -> previousImageId = ID_0
    ReqB->>DB: Snapshot check -> previousImageId = ID_0
    par Parallel Retrieval & Validation
        ReqA->>Store: Save newKeyA
        ReqB->>Store: Save newKeyB
    end
    ReqA->>DB: Transaction commits -> Vehicle.Image.Id is now ID_A!
    ReqA->>Store: Deletes ID_0 file
    ReqB->>DB: BeginTransactionAsync -> reloads vehicle
    Note over ReqB,DB: vehicle.Image.Id (ID_A) != previousImageId (ID_0)!
    ReqB->>DB: RollbackAsync
    ReqB->>Store: CompensateFileDeleteAsync(newKeyB)
    ReqB-->>Client: Throw ConcurrentUpdateException -> 409 CONFLICT (retryable: true)
```

**Explanation:** Instead of holding a database lock across slow HTTP downloads, `ReplaceImageAsync` uses optimistic compare-and-swap on `previousImageId`. If another request replaced the image while Request B was downloading from a supplier, Request B rolls back, cleans up its newly written file (`newKeyB`), and returns `409 CONCURRENT_UPDATE`.

---

### Diagram 16: Entity Relationship & Schema Constraints

```mermaid
erDiagram
    VEHICLES ||--o| VEHICLE_IMAGES : "1-to-1 Cascade Delete"
    VEHICLES {
        uniqueidentifier Id PK
        nvarchar_100 Make "Required"
        nvarchar_100 Model "Required"
        nvarchar_150 Variant "Nullable"
        int Year "CHECK 1886..2100"
        datetime2 CreatedAt "SYSUTCDATETIME()"
        datetime2 UpdatedAt "SYSUTCDATETIME()"
    }
    VEHICLE_IMAGES {
        uniqueidentifier Id PK "ValueGeneratedNever"
        uniqueidentifier VehicleId FK "Unique IX_VehicleImages_VehicleId"
        nvarchar_2048 SourceUrl "Required"
        nvarchar_2048 FinalSourceUrl "Required"
        nvarchar_500 StorageKey "Unique IX_VehicleImages_StorageKey"
        nvarchar_100 ContentType "Required"
        bigint FileSize "CHECK > 0"
        int Width "CHECK > 0"
        int Height "CHECK > 0"
        nvarchar_20 Format "JPEG / PNG / WebP"
        nvarchar_50 RetrievalStrategy "Direct / Html"
        nvarchar_30 Status "Completed / Failed"
        nvarchar_max FailureReason "Nullable"
        nchar_64 Sha256 "FixedLength Required"
        datetime2 CreatedAt "SYSUTCDATETIME()"
        datetime2 UpdatedAt "SYSUTCDATETIME()"
    }
```

**Explanation:** Relational integrity guarantees one stored image per vehicle (`IX_VehicleImages_VehicleId` unique), zero duplicate storage keys (`IX_VehicleImages_StorageKey` unique), and database-enforced invariants (`CK_Vehicles_Year`, positive size/width/height checks) backing up DTO validation.

---

### Diagram 17: CSV Bulk Import Pipeline — Two-Phase Execution

```mermaid
flowchart TD
    Upload["POST /api/vehicles/import (multipart/form-data)"] --> ReqLimit["Controller Limits: 5 MB + 64 KB"]
    ReqLimit --> Gate["ValidateFile: non-empty, <= MaxFileBytes (1 MB), .csv extension"]
    Gate -->|"Fails"| BadReq["Throw ValidationException -> 400 VALIDATION_FAILED"]
    Gate -->|"Passes"| Parse["Phase 1: CsvHelper ParseAsync (HasHeader=true, IgnoreBlankLines=true, Trim)"]
    Parse -->|"CsvHelperException / 0 rows / > 50 rows / Missing or Duplicate Header"| BadReq
    Parse --> BuildRow["BuildRow: Check field count, parse Year, Validator.TryValidateObject(CreateVehicleRequest)"]
    BuildRow -->|"Row invalid"| MarkRow["Store ValidationError on CsvRow (do NOT abort whole CSV)"]
    BuildRow -->|"Row valid"| StoreRow["Store CreateVehicleRequest on CsvRow"]
    MarkRow --> Loop
    StoreRow --> Loop["Phase 2: Sequential foreach (CsvRow row in rows)"]
    Loop --> Return["Return 200 OK VehicleImportResponse (TotalRows, SuccessfulRows, FailedRows, Results)"]
```

**Explanation:** Whole-file structural defects fail fast with `400 VALIDATION_FAILED` *before* any outbound HTTP call or DB insert occurs. Once the file structure and record count (`<= 50`) pass, row-level validation failures are isolated per row so valid records still succeed (`partial success`).

---

### Diagram 18: Per-Row `AsyncServiceScope` Isolation & Partial Success

```mermaid
sequenceDiagram
    participant Imp as VehicleImportService
    participant SF as IServiceScopeFactory
    participant S1 as Scope Row 1 (DbContext 1 + VehicleService 1)
    participant S2 as Scope Row 2 (DbContext 2 + VehicleService 2)
    participant S3 as Scope Row 3 (DbContext 3 + VehicleService 3)

    Imp->>Imp: Row 1 has ValidationError? No -> create Scope 1
    Imp->>S1: CreateAsync(row1.Request, ct)
    S1-->>Imp: ImageIngestionException (422 NO_IMAGE_FOUND)
    Note over Imp: Catches ImageIngestionException -> adds Failed result, continues! Scope 1 disposed.
    Imp->>SF: CreateAsyncScope() for Row 2
    Imp->>S2: CreateAsync(row2.Request, ct)
    S2-->>Imp: ImageSubmissionResponse (VehicleId, ImageId, InternalUrl)
    Note over Imp: Adds Succeeded result! Scope 2 disposed cleanly.
    Imp->>SF: CreateAsyncScope() for Row 3
    Imp->>S3: CreateAsync(row3.Request, ct)
    S3--xImp: Throws unexpected non-ImageIngestionException (e.g., OutOfMemory/IO crash)
    Note over Imp: Logs Error & rethrows -> GlobalExceptionHandler returns 500 INTERNAL_ERROR
```

**Explanation:** Isolating each CSV row in its own `AsyncServiceScope` gives every row a clean EF Core `ApplicationDbContext`. If row 1 fails database persistence, its tracked entities are discarded when `scope.DisposeAsync()` runs, preventing row 2 from inheriting a corrupted change tracker.

---

### Diagram 19: Global Exception Handling & Error Taxonomy Mapping

```mermaid
flowchart TD
    Throw["Exception Thrown in Pipeline"] --> ModelCheck{"ASP.NET ModelState Failure before Action?"}
    ModelCheck -->|"Yes"| ModelFmt["InvalidModelStateResponseFactory -> 400 VALIDATION_FAILED"]
    ModelCheck -->|"No (Runtime Exception)"| GEH["GlobalExceptionHandler.TryHandleAsync"]
    GEH --> CancelCheck{"OperationCanceledException AND RequestAborted?"}
    CancelCheck -->|"Yes"| Http499["Status 499 (Client Closed Request)"]
    CancelCheck -->|"No"| TypeSwitch{"Exception Type?"}
    TypeSwitch -->|"ImageIngestionException"| MapIng["422 (or 500 for Storage/DB), Retryable=IsTransient, Retry-After header"]
    TypeSwitch -->|"ResourceNotFoundException"| Map404["404 VEHICLE_NOT_FOUND / IMAGE_NOT_FOUND"]
    TypeSwitch -->|"ConcurrentUpdateException"| Map409["409 CONCURRENT_UPDATE (Retryable=true)"]
    TypeSwitch -->|"ValidationException"| Map400["400 VALIDATION_FAILED"]
    TypeSwitch -->|"Any Other Exception"| Map500["500 INTERNAL_ERROR (Sanitized message + Error log)"]
```

**Explanation:** All controllers remain thin because domain and infrastructure exceptions flow through one central mapper (`GlobalExceptionHandler`), ensuring every failure returns RFC 7807 / RFC 9110 `ImageFailureResponse` JSON with a `traceId` and never exposes internal stack traces or filesystem paths.

---

### Diagram 20: React Frontend Component Tree & Catalog Refresh Data Flow

```mermaid
flowchart LR
    App["App.jsx (Header + Protection Chips)"] --> Page["DashboardPage.jsx (page, totalCount, loading, vehicles)"]
    Page --> Sidebar["div.sidebar-stack"]
    Sidebar --> VForm["VehicleForm.jsx (POST /api/vehicles + Presets)"]
    Sidebar --> BForm["BulkImportForm.jsx (POST /api/vehicles/import + Sample CSV)"]
    VForm -->|"onVehicleCreated"| Refresh["handleCatalogRefreshFromPageOne()"]
    BForm -->|"onImportCompleted"| Refresh
    Refresh -->|"GET /api/vehicles?page=1&pageSize=12"| Api["vehicleApi.js (fetch + ApiError)"]
    Page --> Grid["div.vehicle-grid"]
    Grid --> Card["VehicleCard.jsx (key = id-imageId)"]
    Card -->|"img src = /api/images/{imageId}"|ImgCtrl["ImagesController.Get"]
    Card -->|"View Details"| GetOne["GET /api/vehicles/{id}"]
    Card --> Replace["ImageReplaceForm.jsx (PUT /api/vehicles/{id}/image)"]
    Replace -->|"onReplaced -> onUpdated"| Page
```

**Explanation:** Both submission forms live inside `.sidebar-stack` and share `handleCatalogRefreshFromPageOne` to reset pagination to page 1 so newly imported vehicles appear immediately at the top of the catalog. Using `key={`${vehicle.id}-${vehicle.imageId}`}` guarantees replaced images cleanly reset per-card UI state without manual state cleanup bugs.

---

## Part 4: 32 End-to-End Scenario Walkthroughs

### Category A — Valid Ingestion & Content Sniffing

1. **Valid Direct JPEG URL Submission (`POST /api/vehicles` -> `201 Created`)**
   - `VehiclesController.Create` validates `[FromBody] CreateVehicleRequest` -> `VehicleService.CreateAsync` -> `ImageIngestionService.IngestAsync`.
   - `UrlValidator.Validate` accepts `https://cdn.example/car.jpg` -> `DirectImageRetriever.ExecuteSingleFetchAsync` issues `GET` via `ImageRetrievalClient`, reads initial 1 KB prefix directly from `networkStream`.
   - `ImageValidator.TryMatchBinaryImageSignature` matches `FF D8 FF`, preserves prefix in `MemoryStream`, streams remainder up to `MaxDownloadBytes`, and sets `Strategy = "Direct"`.
   - `ImageValidator.ValidateAsync` verifies `FF D8 FF` magic bytes, `FF D9` EOI tail, `IdentifyAsync` dimensions (`100..10000`, `<= 50M` pixels, `1` frame), decodes pixels with `LoadAsync<Rgba32>`, hashes SHA-256 -> `LocalFileStorage.SaveAsync` moves temp file to `vehicles/YYYY/MM/guid.jpg` -> `DbContext.SaveChangesAsync` -> `201 Created` returning `/api/images/{imageId}`.

2. **Direct PNG URL With Misleading Extension or Missing/Mismatched `Content-Type` (`201 Created`)**
   - `FinalSourceUrl` is `/vehicle` (no extension) or `/photo.jpg` containing PNG bytes, or server sends `Content-Type: text/html`.
   - `DirectImageRetriever.ReadBoundedWithClassificationAsync` inspects the initial 1 KB directly: detects PNG signature `89 50 4E 47 0D 0A 1A 0A` in `TryMatchBinaryImageSignature`.
   - Binary image magic bytes take precedence over headers; streams the remaining body, and `RetrievalOrchestrator` passes it directly to `ImageValidator`.
   - `ImageValidator` inspects the actual PNG bytes, checks `IHDR` dimensions, decodes, saves as `.png`, and returns `201 Created`.

3. **Valid Redirecting Image URL (`301` -> `302` -> Final `.jpg` -> `201 Created`)**
   - `SocketsHttpHandler` has `AllowAutoRedirect = false`, so `DirectImageRetriever` sees `301`, increments `hop`, calls `UrlValidator.ValidateRedirectTarget` on `Location`, and issues the next `GET`.
   - After resolving `302` (`hop = 2 <= 5`), fetches final `200 OK` bytes; `VehicleImage` persists both `SourceUrl` (initial input) and `FinalSourceUrl` (final destination URI).

4. **Static HTML Webpage Containing `og:image` Tag (`201 Created`)**
   - Initial `GET` returns `200 OK` (`text/html`). `IsHtmlResponse` returns `true`, decodes charset (`DecodeHtml`), disposes HTML stream, and calls `HtmlImageRetriever.RetrieveCandidatesFromHtmlAsync`.
   - `ExtractAndRankCandidates` scores `<meta property="og:image">` at `400`, fetches image via `DirectImageRetriever` (`Strategy = "Html"`), validates via `ImageValidator`, stores copy, and persists `RetrievalStrategy = "Html"`.

5. **HTML Multi-Candidate Fallback (`og:image` Too Small -> `srcset` Succeeds -> `201 Created`)**
   - Candidate #1 (`og:image`, score `400`) returns a valid `50x50` PNG thumbnail.
   - `ImageIngestionService.IngestAsync` passes it to `ImageValidator.ValidateAsync` -> throws `IMAGE_TOO_SMALL` (`< 100x100`).
   - `IngestAsync` disposes Candidate #1's stream, logs the failure with `RedactForLogging`, saves `lastValidationFailure`, and advances the `await foreach` loop to Candidate #2 (largest `srcset` width entry, score `200+`).
   - Candidate #2 passes validation (`1200x800`), saves to storage, and succeeds (`201 Created`).

6. **HTML Page With Relative URLs & `<base href>` (`201 Created`)**
   - HTML has `<base href="https://assets.example.com/vehicles/">` and `<img src="sedan-front.webp" width="900">`.
   - `HtmlImageRetriever.ResolveBaseUri` validates `<base href>` against `UrlValidator.ValidateRedirectTarget`.
   - `TryAddCandidate` resolves `"sedan-front.webp"` -> `https://assets.example.com/vehicles/sedan-front.webp` and ingests cleanly.

7. **Lazy-Loaded `data-src` / `data-srcset` Fallback (`201 Created`)**
   - `<img src="placeholder.gif" data-src="real-car.jpg">` -> `placeholder.gif` is dropped by `IgnoreUrlPatterns` (`"placeholder"`), while `data-src` is extracted as `CandidateSourceKind.DataSrc` (score `100`) and ingested.

8. **Dynamic JS-Only / Akamai Bot Challenge URL (e.g. `magnific.com` -> `422 NO_IMAGE_FOUND`)**
   - Server responds with HTTP `200 OK` and a 2,313-byte Akamai JS challenge script page (`<html ...><script>...</script></html>`).
   - `RetrievalOrchestrator` sees `text/html` -> delegates to `HtmlImageRetriever`.
   - Neither `og:image`, `twitter:image`, nor `<img>` exist in the static challenge HTML (`ExtractAndRankCandidates` returns `0` items) -> throws `ImageIngestionException.NoImageFound()` -> `422 NO_IMAGE_FOUND`. No anti-bot bypass is attempted.

---

### Category B — URL Policy Rejections

9. **Localhost Hostname Blocked (`http://localhost/car.jpg` -> `422 UNSAFE_DESTINATION`)**
   - `UrlValidator.ValidateUriPolicy` normalizes host (`normalizedHost == "localhost" || normalizedHost.EndsWith(".localhost")`) and throws `UNSAFE_DESTINATION` before any HTTP client call executes.

10. **Loopback IPv4/IPv6 Literal Blocked (`http://127.0.0.1/car.jpg`, `http://[::1]/car.jpg` -> `422 UNSAFE_DESTINATION`)**
    - `UrlValidator.ValidateUriPolicy` sees `UriHostNameType.IPv4` / `IPv6` -> parses `IPAddress` -> `IPAddress.IsLoopback(ipAddress)` returns `true` -> throws `UNSAFE_DESTINATION`.

11. **Non-Standard Port Rejected (`https://example.com:8443/car.jpg` -> `422 UNSAFE_DESTINATION`)**
    - `UrlValidator.ValidateUriPolicy` checks `_options.AllowedPorts.Contains(8443)` (`[80, 443]`) -> returns `false` -> rejects immediately (`Port 8443 is not permitted.`).

12. **Embedded Credentials Rejected (`https://admin:secret@example.com/car.jpg` -> `422 INVALID_URL`)**
    - `!string.IsNullOrEmpty(uri.UserInfo)` triggers `ImageIngestionException.InvalidUrl`. Note: logged URLs use `RedactForLogging`, and userinfo URLs are never fetched.

13. **Disallowed Scheme Rejected (`file:///etc/passwd`, `ftp://supplier.example/car.jpg` -> `422 INVALID_URL`)**
    - Scheme check fails (`http` / `https` only) and throws `INVALID_URL` immediately.

14. **Open Redirect to Loopback or Disallowed Port Blocked (`https://public.example/go` -> `302` -> `http://127.0.0.1/` or `http://example.com:8080/` -> `422 UNSAFE_DESTINATION`)**
    - Hop 0 succeeds against public server returning `302 Location: ...`.
    - `DirectImageRetriever` calls `_urlValidator.ValidateRedirectTarget` -> parses target -> fails `IsLoopback` or `AllowedPorts` -> aborts with `422 UNSAFE_DESTINATION`.

---

### Category C — HTTP Limits, Image Validation & Retries

15. **Redirect Chain Exceeds `MaxRedirects = 5` (`422 REDIRECT_LIMIT_EXCEEDED`)**
    - Hop 0..5 follow 5 redirects (`hop < 5` continues), and when the 6th redirect response arrives (`hop == 5 >= _retrievalOptions.MaxRedirects`), `DirectImageRetriever` throws `RedirectLimitExceeded(5)`.

16. **Oversized Chunked Stream Without `Content-Length` Aborted Mid-Stream (`422 IMAGE_TOO_LARGE`)**
    - Server streams chunks totaling 25 MB with `Transfer-Encoding: chunked` and no `Content-Length`.
    - `ReadBoundedWithClassificationAsync` tracks `totalRead` across initial 1 KB prefix and subsequent 16 KB chunks; on the chunk pushing `totalRead > 20,971,520`, throws `IMAGE_TOO_LARGE`, disposes `MemoryStream`, and drops the connection.

17. **PNG Decompression Bomb Caught via `IHDR` Before Decode (`422 IMAGE_DIMENSIONS_EXCEEDED`)**
    - Malicious tiny 1 KB PNG declares `20000 x 20000` (`400M` pixels) in `IHDR` bytes `16..23`.
    - `ImageValidator.ValidateAsync` inspects `BinaryPrimitives.ReadInt32BigEndian` on bytes `16..23` immediately and throws `IMAGE_DIMENSIONS_EXCEEDED` before allocating pixel buffers.

18. **Disguised SVG or Non-Image Stream Renamed `.jpg` Rejected (`422 UNSUPPORTED_CONTENT_TYPE`)**
    - `https://supplier.test/car.jpg` returns `<svg xmlns="http://www.w3.org/2000/svg"><script>alert(1)</script></svg>` or a 15 MB PDF/video stream.
    - `DirectImageRetriever.ReadBoundedWithClassificationAsync` inspects the initial 1 KB: detects text/SVG or non-image bytes; aborts immediately with `UNSUPPORTED_CONTENT_TYPE` without downloading the remaining megabytes.

19. **GIF / BMP / AVIF Image Rejected Early After Prefix (`422 UNSUPPORTED_IMAGE_FORMAT`)**
    - Server returns valid `GIF89a`, `BM`, or `ftypavif` bytes. `ImageValidator.TryMatchBinaryImageSignature` identifies the unsupported format explicitly (`"GIF"`, `"BMP"`, `"AVIF/HEIF"`) in the first 1 KB prefix and throws `UNSUPPORTED_IMAGE_FORMAT` immediately (never retried, no megabytes buffered).

20. **Truncated JPEG Missing `FF D9` EOI Marker Rejected & Retried (`422 IMAGE_DECODE_FAILED`)**
    - A JPEG starts with `FF D8 FF` but drops halfway through transfer (last 16 bytes lack `FF D9`).
    - `HasValidJpegEndOfImage` returns `false`, throwing `IMAGE_DECODE_FAILED`. Note: `DirectImageRetriever` doesn't catch `ImageValidator` failures (only `DirectImageRetriever` HTTP retries loop inside `FetchWithRetriesAsync`), so `ImageValidator`'s exception flows up with `retryable: true` in `ImageFailureResponse` so the client can safely retry.

21. **Soft-404 HTML Error Page Returning HTTP `200 OK` (`422 NO_IMAGE_FOUND`)**
    - Supplier returns `200 OK` HTML with `<title>404 Not Found - Dealer Portal</title>` plus header/footer logo `<img>` elements.
    - `HtmlImageRetriever.IsHtmlErrorPage` detects `"404 not found"` in `<title>`, returns `[]`, and `RetrieveCandidatesFromHtmlAsync` throws `NO_IMAGE_FOUND`.

22. **Upstream `429` Rate Limit With Short vs Long `Retry-After`**
    - Short `Retry-After: 1` (`<= MaxRetryAfterSeconds = 3`): `ShouldRetry` pauses `1s` via `Task.Delay` and retries up to 3 attempts; succeeds if attempt 2/3 returns `200 OK`.
    - Long `Retry-After: 30` (`> 3s`): `ShouldRetry` returns `false` immediately -> returns `422 HTTP_RATE_LIMITED` with `Retry-After: 30` and `retryable: true`.

---

### Category D — Persistence, Compensation, Replacement & Delivery

23. **Database Failure After File Saved Triggers Compensating Delete (`500 DATABASE_PERSISTENCE_FAILED`)**
    - `ImageIngestionService.IngestAsync` writes `vehicles/2026/11/xyz.jpg`.
    - `_dbContext.SaveChangesAsync` throws `DbUpdateException` (e.g., SQL constraint or DB offline).
    - `VehicleService.CreateAsync` catches `Exception`, calls `CompensateFileDeleteAsync("vehicles/2026/11/xyz.jpg")` with `CancellationToken.None`, deletes the file, and throws `DatabasePersistenceFailed` (`500`).

24. **Client Aborts HTTP Request During Persistence (`499` + File Deleted)**
    - Client disconnects while `SaveChangesAsync` is running (`HttpContext.RequestAborted` fires).
    - `VehicleService.CreateAsync` catches `OperationCanceledException`, calls `CompensateFileDeleteAsync` with `CancellationToken.None`, removes the newly stored disk file, and rethrows; `GlobalExceptionHandler` logs nothing noisy and sets HTTP `499`.

25. **Safe Image Replacement (`PUT /api/vehicles/{id}/image` -> `200 OK`)**
    - Captures `previousImageId` and `previousStorageKey`.
    - Ingests new image into a brand-new `ImageId` + `StorageKey`.
    - Inside explicit DB transaction: verifies `vehicle.Image?.Id == previousImageId`, removes old `VehicleImage` + `SaveChangesAsync`, adds new `VehicleImage` + `SaveChangesAsync`, commits transaction, then deletes `previousStorageKey`.
    - Returns new `ImageUrl: /api/images/{newImageId}` (`200 OK`). Old cached `/api/images/{oldId}` browsers aren't poisoned because the ID changed.

26. **Failed Image Replacement Preserves Existing Image (`422` -> Old Image Intact)**
    - `PUT /api/vehicles/{id}/image` supplies an unreachable or non-image URL.
    - `IngestAsync` fails *before* any DB transaction begins; existing `VehicleImage` row and disk file remain completely untouched (`Existing image preserved` shown in `ImageReplaceForm`).

27. **Concurrent Replacement Race (`409 CONCURRENT_UPDATE`)**
    - Two admins submit `PUT /api/vehicles/{id}/image` simultaneously for the same vehicle.
    - Admin A commits first (`vehicle.Image.Id` changes). Admin B's transaction checks `vehicle.Image?.Id != previousImageId`, rolls back, deletes Admin B's newly uploaded temp blob via `CompensateFileDeleteAsync`, and throws `ConcurrentUpdateException` -> `409 CONCURRENT_UPDATE` (`retryable: true`).

28. **Internal Image Delivery & Sanitized Download (`GET /api/images/{id}` & `/download`)**
    - `ImageDeliveryService.OpenImageAsync` verifies DB record + file on disk (`FileNotFoundException` logs `"Data integrity error"` and returns `404 IMAGE_NOT_FOUND`).
    - Sets `X-Content-Type-Options: nosniff`, `Cache-Control: public, max-age=31536000, immutable`, `Content-Length`, and for `/download` computes sanitized `{make}-{model}-{year}-{shortGuid}{ext}`.

---

### Category E — CSV Bulk Ingestion & React Integration

29. **Whole-File CSV Rejection Before Any Network/DB Work (`400 VALIDATION_FAILED`)**
    - User uploads a `.txt` file, empty file, file `> 1 MB`, CSV missing required header `Year`, CSV with duplicate header `Make,Make`, CSV with `51` non-empty rows, or CSV with an unterminated quote on row 4.
    - `VehicleImportService.ValidateFile` / `ParseAsync` throws `ValidationException` -> `GlobalExceptionHandler` returns `400 VALIDATION_FAILED`. Zero outbound HTTP requests and zero DB inserts occur.

30. **Mixed-Success CSV Upload (`POST /api/vehicles/import` -> `200 OK`) + Frontend Refresh**
    - User selects a 4-row CSV:
      - Row 2 (data row 1): Valid Wikipedia URL with quoted variant `"GTS, AWD"` -> succeeds (`201`-equivalent persistence via `VehicleService.CreateAsync`), `RowNumber: 2, Success: true, ImageUrl: "/api/images/..."`.
      - Row 3 (data row 2): Blank row `,,,,` -> skipped silently by `ParseAsync` (`record.All(string.IsNullOrWhiteSpace)`).
      - Row 4 (data row 3): Invalid year `"not-a-year"` -> caught upfront by `BuildRow`, fails immediately in `ProcessRowAsync` with `ErrorCode: "VALIDATION_FAILED"` (0 network calls).
      - Row 5 (data row 4): `http://localhost/car.jpg` -> `VehicleService.CreateAsync` throws `ImageIngestionException(UNSAFE_DESTINATION)` -> caught by `ProcessRowAsync`, logged, recorded as `Success: false, ErrorCode: "UNSAFE_DESTINATION"`.
      - Row 6 (data row 5): Valid direct Wikimedia image -> processed in a **fresh `AsyncServiceScope`** (`DbContext` unaffected by previous rows), succeeds cleanly.
    - Backend returns `200 OK` `{ totalRows: 4, successfulRows: 2, failedRows: 2, results: [...] }` with zero external URLs.
    - `BulkImportForm.jsx` clears selected file, renders 3 summary stat boxes (`4`, `2`, `2`) + row cards, and invokes `onImportCompleted` -> `DashboardPage.jsx` resets to page 1 and fetches `GET /api/vehicles?page=1&pageSize=12` so both newly imported vehicles render via internal `/api/images/{id}`.
