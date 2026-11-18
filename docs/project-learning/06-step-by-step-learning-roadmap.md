# Step-by-Step Learning Roadmap (7-Day Mastery Plan)

This structured study plan takes you from startup wiring through URL policy validation, binary image validation, transactional database persistence, CSV bulk ingestion, Google Drive image retrieval, and the React dashboard so you can explain, modify, debug, and defend the entire `URL-to-Image` (`feature/google-drive-image-retrieval`) codebase independently.

---

## Study Strategy Rules
- **Always read code before diagrams:** Open the listed source files in `03-complete-source-code-backend.md` or `04-complete-source-code-frontend.md` (or directly in your editor), trace the listed methods line by line, then redraw the day's Mermaid diagrams on paper from memory.
- **Prioritization Order:** Core ASP.NET pipeline & ingestion (Days 1–3) → DB persistence & CSV bulk ingestion (Days 4–5) → React dashboard (Day 6) → Integration testing, failure debugging & mock interview defense (Day 7).

---

## Day 1: Startup Composition, Configuration, API Contracts & Error Handling

### Focus
How the API boots, validates config before accepting traffic, shapes validation errors, and guarantees zero internal stack-trace leaks.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Program.cs` (`ValidateOnStart`, `PostConfigure`, `InvalidModelStateResponseFactory`, `InitializeAndVerifyStorageRoot`, `VerifyStorageRootWritable`, middleware pipeline)
2. `backend/VehicleImage.Api/Configuration/*.cs` (all 6 Options classes + `appsettings.json`)
3. `backend/VehicleImage.Api/Exceptions/ErrorCodes.cs`, `ImageIngestionException.cs`, `GlobalExceptionHandler.cs` (`TryHandleAsync`, `MapIngestionException`)
4. `backend/VehicleImage.Api/DTOs/Images/ImageFailureResponse.cs` & `Controllers/VehiclesController.cs`

### Concepts to Master
- Why `ImageValidator` and `LocalFileStorage` are `Singleton` while `VehicleService`, `ImageIngestionService`, and `VehicleImportService` are `Scoped`.
- Why `UseExceptionHandler()` must precede routing/controllers AND why `InvalidModelStateResponseFactory` is still needed.
- How `GlobalExceptionHandler` handles HTTP `499` on client cancellation vs `422` vs `500`.

### Diagrams to Redraw From Memory (from [`01-complete-architecture-and-learning-guide.md`](./01-complete-architecture-and-learning-guide.md))
- Diagram 1: High-Level System Architecture
- Diagram 2: ASP.NET Core Startup & Middleware Pipeline
- Diagram 19: Global Exception Handling & Error Taxonomy Mapping

### Practice Questions (from [`05-interview-code-defence-question-bank.md`](./05-interview-code-defence-question-bank.md))
- Q2.1, Q2.2, Q2.3, Q2.4, Q11.1

### Verification Exercise
1. Temporarily inspect `appsettings.json`: trace what exact startup error would happen if `BulkImport:MaxRecords` were set to `600` or `Storage:RootPath` pointed inside `wwwroot` (remember: read-only mental trace or revert cleanly if testing).
2. Hit `GET http://localhost:5080/health` and `GET http://localhost:5080/api/vehicles?page=0` with `curl` to observe `400 VALIDATION_FAILED` JSON formatting.

---

## Day 2: URL Policy Validation & Bounded Streaming HTTP Retrieval (`DirectImageRetriever`)

### Focus
How basic URL policy validation (`UrlValidator`), manual redirects, `SocketsHttpHandler` HTTP safeguards, 1 KB prefix classification (`ReadBoundedWithClassificationAsync`), chunked stream caps, and selective retries work together.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Security/UrlValidator.cs` (`Validate`, `ValidateRedirectTarget`, `ValidateUriPolicy`, `RedactForLogging`)
2. `backend/VehicleImage.Api/Program.cs` (`ImageRetrievalClient` `SocketsHttpHandler` configuration)
3. `backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs` (`FetchWithRetriesAsync`, `ShouldRetry`, `ExecuteSingleFetchAsync`, `ReadBoundedWithClassificationAsync`)
4. `backend/VehicleImage.Api/ImageProcessing/ImageValidator.cs` (`TryMatchBinaryImageSignature`, `IsLikelyTextMarkupOrJson`)

### Concepts to Master
- Why `UrlValidator` rejects `localhost`/`*.localhost`, loopback IP literals (`127.0.0.1`, `[::1]`), embedded `UserInfo`, control characters, non-HTTP/HTTPS schemes, and non-`80`/`443` ports both initially and on redirect targets.
- Why `AllowAutoRedirect` and `UseCookies` are disabled on `SocketsHttpHandler`.
- Why `HttpClient.Timeout` is `InfiniteTimeSpan` while `DirectImageRetriever` creates per-attempt linked timeout CTS.
- **Header-First 1 KB Prefix Classification:** How `ReadBoundedWithClassificationAsync` sniffs the first 1024 bytes during streaming to classify binary images vs HTML vs unsupported content, aborting non-images and unsupported formats early.
- **Prefix Preservation & Full Accounting:** How the first 1 KB is written to `MemoryStream` before reading the remainder, and how `totalRead` bounds prefix + chunks against `MaxImageBytes` (20 MB) or `MaxHtmlBytes` (2 MB).
- How `allowHtml: false` ensures candidate image fetches abort after 1 KB if a candidate URL unexpectedly returns an HTML error page.

### Diagrams to Redraw From Memory
- Diagram 4: URL Policy Validation & Retrieval Flow
- Diagram 5: Redirect Hop Validation & `SocketsHttpHandler` Safeguards
- Diagram 6: Manual Redirect Validation Loop
- Diagram 7: Retry Decision & Backoff Flow
- Diagram 8: Header-First 1 KB Prefix Reading & Early Content Classification

### Practice Questions
- Q3.1, Q3.2, Q3.3, Q4.1, Q4.2, Q4.3, Q5.3, Q5.4, Q5.5, Q5.6

### Verification Exercise
1. Trace `POST /api/vehicles` with `http://localhost/car.jpg` and with `https://example.com:8443/car.jpg` step-by-step through `UrlValidator.Validate` and `ValidateUriPolicy`.
2. Inspect `tests/IntegrationTests/StreamingLimitTests.cs` to trace how early abort terminates oversized non-image streams after reading only 1 KB.

---

## Day 3: Pipeline Routing, HTML Extraction, Google Drive & Image Validation

### Focus
Mastering the 5-step retrieval pipeline: (1) `RetrievalOrchestrator` routing, (2) `DirectImageRetriever` streaming, (3) `HtmlImageRetriever` parsing with `allowHtml: false`, (4) `GoogleDriveImageRetriever` direct export, and (5) end-to-end format/dimension validation in `ImageValidator`.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Retrieval/RetrievalOrchestrator.cs` (`RetrieveCandidatesAsync`, `IsHtmlResponse`, `HasKnownImageExtension`)
2. `backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs` (`FetchWithRetriesAsync`, `ReadBoundedWithClassificationAsync`)
3. `backend/VehicleImage.Api/Retrieval/HtmlImageRetriever.cs` (`RetrieveCandidatesFromHtmlAsync`, `ExtractCandidates`, `Rank`, `DecodeHtml`, `IsHtmlErrorPage`)
4. `backend/VehicleImage.Api/Retrieval/GoogleDriveImageRetriever.cs` (`TryExtractFileId`, `RetrieveDriveImageAsync`)
5. `backend/VehicleImage.Api/ImageProcessing/ImageValidator.cs` (`ValidateAsync`, `InspectSignature`, `TryMatchBinaryImageSignature`, `IsLikelyTextMarkupOrJson`, `HasValidJpegEndOfImage`)
6. `backend/VehicleImage.Api/Storage/LocalFileStorage.cs` (`SaveAsync`, `ResolveSafeFullPath`, `TryDeleteTempFile`)
7. `backend/VehicleImage.Api/Services/ImageIngestionService.cs` (`IngestAsync`)

### Concepts to Master (The 5-Step Retrieval Sequence)
1. **`RetrievalOrchestrator` Routing:** Initial fetch via `DirectImageRetriever.FetchWithRetriesAsync(..., allowHtml: true)`; routes Google Drive URLs directly to `GoogleDriveImageRetriever`; uses `IsHtmlResponse` to branch between direct image validation and `HtmlImageRetriever`.
2. **`DirectImageRetriever` Streaming:** GET with `ResponseHeadersRead`, 1 KB prefix sniffing, early rejection on unsupported formats, prefix preservation into `MemoryStream`, bounded remainder reading.
3. **`HtmlImageRetriever` Candidate Extraction:** Decoding HTML, repairing unclosed `<title>`, filtering/scoring `og:image`, `twitter:image`, and `<img>` tags, and fetching candidates with `allowHtml: false` so candidate error pages abort after 1 KB.
4. **Google Drive Retrieval:** Parsing file IDs from `drive.google.com/file/d/{id}` or `id={id}` links, constructing `drive.google.com/uc?export=download&id={id}`, and retrieving binary image bytes without HTML preview wrappers.
5. **Complete End-to-End Image Validation:** Strict signature matching (JPEG, PNG, WebP) -> structural bounds & JPEG EOI -> `Image.IdentifyAsync` dimension limits -> full `Image.LoadAsync<Rgba32>` pixel decode -> `SHA256` hashing and atomic storage.

### Diagrams to Redraw From Memory
- Diagram 8: Header-First 1 KB Prefix Reading & Early Content Classification
- Diagram 9: Overall Retrieval & Routing Decision in `RetrievalOrchestrator`
- Diagram 10: HTML Extraction, Ranking & Multi-Candidate Fallback
- Diagram 11: `ImageValidator` Validation Stages
- Diagram 12: `LocalFileStorage` Atomic Write & Path Traversal Defense

### Practice Questions
- Q1.2, Q1.4, Q5.1, Q5.2, Q5.7, Q6.1, Q6.2, Q7.1

### Verification Exercise
1. Trace how a masquerading URL `/photo.jpg` that returns HTML is handled: `DirectImageRetriever` fetches 1 KB, `IsHtmlResponse` detects HTML tags, routes to `HtmlImageRetriever`; with 0 candidates found, `RetrievalOrchestrator` catches `NO_IMAGE_FOUND` and rethrows `UNSUPPORTED_CONTENT_TYPE`.
2. Trace a Google Drive sharing URL `https://drive.google.com/file/d/1A2B3C4D5E/view` through `RetrievalOrchestrator` -> `GoogleDriveImageRetriever.TryExtractFileId` -> direct download endpoint -> `ImageValidator`.

---

## Day 4: EF Core Schema, Persistence Compensation, Safe Replacement & Image Delivery

### Focus
Zero-downtime replacements, preventing orphan files on DB crashes/cancellations, optimistic `409` concurrency, and immutable caching.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Entities/Vehicle.cs`, `Entities/VehicleImage.cs`
2. `backend/VehicleImage.Api/Data/ApplicationDbContext.cs`, `Data/Configurations/*.cs`, `Migrations/20261116094554_InitialCreate.cs`
3. `backend/VehicleImage.Api/Services/VehicleService.cs` (`CreateAsync`, `ReplaceImageAsync`, `CompensateFileDeleteAsync`, `ListAsync`, `GetAsync`)
4. `backend/VehicleImage.Api/Services/ImageDeliveryService.cs` & `Controllers/ImagesController.cs`

### Concepts to Master
- Why DB transactions do **not** wrap outbound HTTP retrieval.
- Why `CompensateFileDeleteAsync` uses `CancellationToken.None` AND why `ReplaceImageAsync` tracks `commitAttempted`.
- Why deleting old `VehicleImage` before inserting new `VehicleImage` is required inside the replacement transaction (`IX_VehicleImages_VehicleId`).
- Why minting a new `ImageId` makes `Cache-Control: public, max-age=31536000, immutable` bug-free.

### Diagrams to Redraw From Memory
- Diagram 3: End-to-End Single Vehicle Ingestion Sequence
- Diagram 13: Compensating Delete on DB Persistence Failure
- Diagram 14 & Diagram 15: Safe Replacement & `409 CONCURRENT_UPDATE` Conflict
- Diagram 16: Entity Relationship & Schema Constraints

### Practice Questions
- Q8.1, Q8.2, Q9.1, Q9.2, Q9.3, Q10.1

### Verification Exercise
1. Trace `VehicleServiceCancellationTests.cs` to see how `CancelOnCommandInterceptor` forces cancellation during SQL `INSERT` and proves file deletion still executes.

---

## Day 5: CSV Bulk Vehicle Ingestion

### Focus
Mastering RFC 4180 CSV parsing, two-phase validation, partial success, controller limits, and per-row `AsyncServiceScope`.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Configuration/BulkImportOptions.cs`
2. `backend/VehicleImage.Api/DTOs/Vehicles/VehicleImportResponse.cs`
3. `backend/VehicleImage.Api/Services/IVehicleImportService.cs` & `VehicleImportService.cs` (`ImportCsvAsync`, `ValidateFile`, `ParseAsync`, `BuildColumnMap`, `BuildRow`, `ProcessRowAsync`)
4. `backend/VehicleImage.Api/Controllers/VehiclesController.cs` (`Import`)

### Concepts to Master
- `CsvHelper 33.1.0` (`CsvParser`) vs naive comma splitting (quoted variants `"Competition, xDrive"`).
- Whole-file `400 VALIDATION_FAILED` rejections (0 side effects) vs per-row failures returning `200 OK` with `FailedRows > 0`.
- Why `BuildRow` reuses `Validator.TryValidateObject(CreateVehicleRequest)` instead of duplicating Year/length rules.
- Why `ProcessRowAsync` allocates a new `AsyncServiceScope` per row (defeating poisoned EF Core `DbContext` change trackers) and runs sequentially.

### Diagrams to Redraw From Memory
- Diagram 17: CSV Bulk Import Pipeline — Two-Phase Execution
- Diagram 18: Per-Row `AsyncServiceScope` Isolation & Partial Success

### Practice Questions
- Q12.1, Q12.2, Q12.3

### Verification Exercise
1. Construct a 4-row test CSV containing: (a) a valid direct Wikimedia URL with quoted `"Competition, xDrive"` variant, (b) an empty `,,,,` separator row, (c) an invalid year `1800`, and (d) `http://localhost/car.jpg`. Predict the exact JSON output (`TotalRows`, `SuccessfulRows`, `FailedRows`, and each `RowNumber`/`ErrorCode`) before checking against `VehicleImportService.cs`.

---

## Day 6: React 19 + Vite Dashboard & API Integration

### Focus
How the UI integrates cleanly with `/api/*`, prevents duplicate CSV uploads, normalizes errors, refreshes catalogs, and never leaks supplier URLs.

### Files & Methods to Read
1. `frontend/vehicle-image-dashboard/vite.config.js` & `package.json`
2. `frontend/vehicle-image-dashboard/src/services/vehicleApi.js` (`ApiError`, `parseResponse`, `importVehiclesCsv`, `resolveInternalImageUrl`)
3. `frontend/vehicle-image-dashboard/src/App.jsx` & `src/pages/DashboardPage.jsx`
4. `frontend/vehicle-image-dashboard/src/components/VehicleForm.jsx`, `BulkImportForm.jsx`, `VehicleCard.jsx`, `ImageReplaceForm.jsx`

### Concepts to Master
- Same-origin Vite proxy (`/api` -> `:5080`) vs `VITE_API_BASE_URL` + CORS.
- Why `importVehiclesCsv` lets `fetch` auto-generate the multipart boundary instead of manually setting `Content-Type`.
- Composite key `key={`${vehicle.id}-${vehicle.imageId}`}` forcing clean card remounts after image replacement.
- `BulkImportForm` state transitions (file chosen -> `submitting` lock -> clear file input on `200`, retain on `400`, render stats & row cards, refresh catalog from page 1).

### Diagrams to Redraw From Memory
- Diagram 20: React Frontend Component Tree & Catalog Refresh Data Flow

### Practice Questions
- Q13.1, Q13.2

### Verification Exercise
1. Open the React dashboard (`npm run dev`), click `Sample CSV` in `BulkImportForm.jsx`, inspect the downloaded `sample-vehicles.csv`, and trace every network request in Chrome DevTools Network tab (`POST /api/vehicles/import` -> `GET /api/vehicles?page=1&pageSize=12` -> lazy `GET /api/images/{id}`).

---

## Day 7: Testing Architecture, Evolution Tradeoffs & Full Mock Code-Defence

### Focus
Tie the whole system together under interview pressure, practice explaining bugs and fixes, and articulate scaling paths honestly.

### Files & Methods to Read
1. `tests/IntegrationTests/ApiFactory.cs` & `MockSupplierServer.cs`
2. Scan `tests/UnitTests/*.cs` and `tests/IntegrationTests/*.cs`
3. Re-read `05-interview-code-defence-question-bank.md` Categories 1 & 14

### Mock Defence Drill (Time-Boxed 30 Minutes — No Notes)
Answer these 8 questions aloud cleanly in under 3 minutes each:
1. Trace a single URL submission from React click to SQL commit and `<img src="/api/images/{id}">`.
2. Explain what `UrlValidator` and `DirectImageRetriever` validate on initial URLs and redirects, and what additional network-level protections would be needed before exposing this service to untrusted public users.
3. Explain what happens when a supplier returns a 50 MB gzipped HTML page masquerading as `.jpg`.
4. Explain what happens if SQL Server throws an error *after* `LocalFileStorage.SaveAsync` finishes—and what happens if the user closed their browser tab mid-commit.
5. Walk through `PUT /api/vehicles/{id}/image` under a concurrent race between two admins.
6. Walk through `POST /api/vehicles/import` from multipart framing down to why `AsyncServiceScope` is created per CSV row.
7. Explain why `https://www.magnific.com/free-psd/silver-sedan-car_3993591.htm` returned `NO_IMAGE_FOUND` and what you would (and would *not*) change.
8. Explain how you would scale CSV bulk import to 10,000 rows behind a 3-node load balancer, referencing `IImageStorage`, `IImageIngestionService`, and async queues.

### Final Verification Command Check
If you want to run the existing unit and integration suite locally to observe tests in action:
```bash
dotnet test
```
*(Reminder: existing tests in `tests/` cover core single/replace ingestion, security, HTML extraction, delivery, and cancellation; adding dedicated xUnit tests for `POST /api/vehicles/import` is a clean next engineering step after this documentation pass.)*
