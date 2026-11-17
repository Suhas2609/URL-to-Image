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

## Day 2: URL Policy Validation, Streaming HTTP Retrieval & Retries

### Focus
How basic URL policy validation (`UrlValidator`), manual redirects, `SocketsHttpHandler` HTTP safeguards, chunked stream caps, and selective retries work together.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Security/UrlValidator.cs` (`Validate`, `ValidateRedirectTarget`, `ValidateUriPolicy`, `RedactForLogging`)
2. `backend/VehicleImage.Api/Program.cs` (`ImageRetrievalClient` `SocketsHttpHandler` configuration)
3. `backend/VehicleImage.Api/Retrieval/DirectImageRetriever.cs` (`FetchWithRetriesAsync`, `ShouldRetry`, `ExecuteSingleFetchAsync`, `ReadBoundedStreamAsync`)

### Concepts to Master
- Why `UrlValidator` rejects `localhost`/`*.localhost`, loopback IP literals (`127.0.0.1`, `[::1]`), embedded `UserInfo`, control characters, non-HTTP/HTTPS schemes, and non-`80`/`443` ports both initially and on redirect targets.
- Why `AllowAutoRedirect` and `UseCookies` are disabled on `SocketsHttpHandler`.
- Why `HttpClient.Timeout` is `InfiniteTimeSpan` while `DirectImageRetriever` creates per-attempt linked timeout CTS.
- How `ReadBoundedStreamAsync` defeats missing/spoofed `Content-Length` headers and gzip bombs.

### Diagrams to Redraw From Memory
- Diagram 4: URL Policy Validation & Retrieval Flow
- Diagram 5: Redirect Hop Validation & `SocketsHttpHandler` Safeguards
- Diagram 6: Manual Redirect Validation Loop
- Diagram 7 & Diagram 8: Retry Decision + Bounded Streaming Cap

### Practice Questions
- Q3.1, Q3.2, Q3.3, Q3.4, Q4.1, Q4.2, Q4.3

### Verification Exercise
1. Trace `POST /api/vehicles` with `http://localhost/car.jpg` and with `https://example.com:8443/car.jpg` step-by-step through `UrlValidator.Validate` and `ValidateUriPolicy`.
2. Read `tests/UnitTests/UrlValidatorTests.cs` and `tests/IntegrationTests/UrlValidationTests.cs` to see every scheme, port, localhost, loopback, and redirect validation test case.

---

## Day 3: Single-GET Sniffing, HTML Extraction, Magic Bytes & ImageSharp Validation

### Focus
Classifying images vs HTML in one GET, extracting/ranking HTML candidates, rejecting disguised SVGs/bombs/truncated JPEGs, and storing safely on disk.

### Files & Methods to Read
1. `backend/VehicleImage.Api/Retrieval/RetrievalOrchestrator.cs` (`RetrieveCandidatesAsync`, `IsHtmlResponse`, `StartsWithBinaryImageSignature`)
2. `backend/VehicleImage.Api/Retrieval/HtmlImageRetriever.cs` (`RetrieveCandidatesFromHtmlAsync`, `ExtractCandidates`, `Rank`, `DecodeHtml`, `IsHtmlErrorPage`)
3. `backend/VehicleImage.Api/ImageProcessing/ImageValidator.cs` (`ValidateAsync`, `InspectSignature`, `HasValidJpegEndOfImage`)
4. `backend/VehicleImage.Api/Storage/LocalFileStorage.cs` (`SaveAsync`, `ResolveSafeFullPath`, `TryDeleteTempFile`)
5. `backend/VehicleImage.Api/Services/ImageIngestionService.cs` (`IngestAsync`)

### Concepts to Master
- Precedence in `IsHtmlResponse` (extension -> binary magic bytes -> `<svg` guard -> HTML markers/Content-Type).
- Candidate ranking scores (`og:image=400`, `twitter:image=300`, `srcset=200`, `data-src/img=100`, `+50` large dims) and lazy `IAsyncEnumerable` fallback (`MaxCandidates <= 3`).
- Why `ImageValidator` runs signature -> JPEG EOI / PNG `IHDR` -> `IdentifyAsync` -> bounded `LoadAsync<Rgba32>` -> `SHA256`.
- Atomic `.tmp -> File.Move` writes and traversal guards in `LocalFileStorage`.

### Diagrams to Redraw From Memory
- Diagram 9: Single-GET Content Sniffing
- Diagram 10: HTML Extraction, Ranking & Multi-Candidate Fallback
- Diagram 11: `ImageValidator` Validation Stages
- Diagram 12: `LocalFileStorage` Atomic Write & Path Traversal Defense

### Practice Questions
- Q1.2, Q1.4, Q5.1, Q5.2, Q6.1, Q6.2, Q7.1

### Verification Exercise
1. Trace why `https://en.wikipedia.org/wiki/Porsche_911` succeeds while `https://www.magnific.com/free-psd/silver-sedan-car_3993591.htm` fails with `NO_IMAGE_FOUND`.
2. Inspect `tests/IntegrationTests/Fixtures/Images/` and trace which exact stage in `ImageValidator` catches `html-masquerade.jpg`, `vector.svg`, `decompression-bomb.png`, `corrupted.jpg`, and `too-small.png`.

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

## Day 5: CSV Bulk Vehicle Ingestion (`feature/csv-bulk-import` Deep Dive)

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
