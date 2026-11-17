# VehicleImageSystem (`URL-to-Image`) — Complete Project Learning & Technical Reference Suite

**Repository:** `Suhas2609/URL-to-Image`  
**Documented Branch:** `feature/csv-bulk-import` (commit `49e9d84`, two commits ahead of `main` at `252579c`)  
**Stack:** ASP.NET Core 10 Web API (`VehicleImage.Api`) · Entity Framework Core 10 (SQL Server primary, SQLite supported) · `SixLabors.ImageSharp` · `HtmlAgilityPack` · `CsvHelper` · React 19 + Vite 6  

> [!IMPORTANT]
> **Source-of-Truth Guarantee:** Every architecture claim, file path, class name, method signature, error code, configuration key, and source snippet in this learning suite was verified directly against the checked-out `feature/csv-bulk-import` tree. Intentional limitations and behaviors (such as the absence of authentication, synchronous execution without a background job queue, and why JS-rendered / Akamai-protected URLs like `magnific.com` return `NO_IMAGE_FOUND`) are documented explicitly rather than glossed over.

---

## 1. Document Index & Recommended Reading Order

| Order | Document | Purpose |
| :--- | :--- | :--- |
| **1** | [`01-complete-architecture-and-learning-guide.md`](./01-complete-architecture-and-learning-guide.md) | 15 sequential learning modules (startup to React UI), 20 Mermaid architecture/sequence/ER diagrams with written walkthroughs, and 32+ end-to-end execution scenarios |
| **2** | [`02-complete-project-directory-and-file-reference.md`](./02-complete-project-directory-and-file-reference.md) | Clean repository tree (excluding generated/runtime artifacts), `main` vs `feature/csv-bulk-import` diff table, and file-by-file breakdown in dependency order |
| **3** | [`03-complete-source-code-backend.md`](./03-complete-source-code-backend.md) | Verbatim, unabbreviated source code of every runtime backend file with purpose notes before each block |
| **4** | [`04-complete-source-code-frontend.md`](./04-complete-source-code-frontend.md) | Verbatim, unabbreviated source code of all 14 frontend files (`index.html`, Vite config, API client, React pages/components, CSS) |
| **5** | [`05-interview-code-defence-question-bank.md`](./05-interview-code-defence-question-bank.md) | 14-category interview question bank (60+ repo-grounded Q&As including deep-dive code-defense scenarios) with interviewer intent, answers, file/method anchors, follow-ups, and pitfalls |
| **6** | [`06-step-by-step-learning-roadmap.md`](./06-step-by-step-learning-roadmap.md) | 7-day structured study plan prioritizing backend pipeline → security & validation → storage & DB → CSV bulk import → frontend → mock defense |
| **7** | [`07-google-drive-image-retrieval-service-and-flow.md`](./07-google-drive-image-retrieval-service-and-flow.md) | Complete deep dive on Google Drive public image retrieval: problem statement, component design, sequence diagrams, URL translation regex, interstitial handling, security controls, and defense Q&A |

---

## 2. System at a Glance

```text
React Dashboard (Vite :5173)
   ├── POST /api/vehicles              ──▶ VehiclesController.Create ──▶ VehicleService.CreateAsync
   ├── POST /api/vehicles/import       ──▶ VehiclesController.Import ──▶ VehicleImportService.ImportCsvAsync ──(per-row AsyncServiceScope)──▶ VehicleService.CreateAsync
   ├── GET  /api/vehicles              ──▶ VehiclesController.List   ──▶ VehicleService.ListAsync
   ├── GET  /api/vehicles/{id}         ──▶ VehiclesController.GetById──▶ VehicleService.GetAsync
   ├── PUT  /api/vehicles/{id}/image   ──▶ VehiclesController.ReplaceImage ──▶ VehicleService.ReplaceImageAsync
   ├── GET  /api/images/{id}           ──▶ ImagesController.Get      ──▶ ImageDeliveryService.OpenImageAsync
   └── GET  /api/images/{id}/download  ──▶ ImagesController.Download ──▶ ImageDeliveryService.OpenImageAsync
```

### Core Ingestion Pipeline (`ImageIngestionService.IngestAsync`)

Every single submission, image replacement, and CSV bulk-import row executes the exact same 6-stage pipeline:

1. **URL Policy Check (`UrlValidator`)**: Enforces `http`/`https`, no userinfo, length `<= 2048`, no control chars, ports in `{80, 443}`, non-local hostnames, and valid FQDN or allowed public IP literal.
2. **SSRF-Safe Retrieval (`RetrievalOrchestrator` -> `DirectImageRetriever` / `HtmlImageRetriever` + `SsrfGuardHttpHandler` + `DestinationValidator`)**: Single initial `GET` (no mandatory `HEAD`), DNS resolution + IP blocklist check inside `ConnectCallback` at TCP socket creation time, manual redirects up to 5 hops with full re-validation per hop, 15s request timeout, 20 MB streaming byte cap, exponential backoff retries on transient errors.
3. **Format-Agnostic Classification (`RetrievalOrchestrator.IsHtmlResponse`)**: Inspects magic bytes and text prefixes (never trusting `.jpg` extensions or HTTP `Content-Type` alone) to dispatch static HTML pages to `HtmlImageRetriever` (`og:image`, `twitter:image`, `srcset`, `data-src`, `<img>`, max 3 ranked candidates).
4. **Binary & Pixel Validation (`ImageValidator`)**: Verifies magic bytes (`JPEG`, `PNG`, `WebP`), rejects JPEG missing `FF D9` EOI marker, checks PNG `IHDR` bounds + `Image.IdentifyAsync` dimensions (`100x100` min to `10000x10000` max, `<= 50M` pixels) to stop decompression bombs, performs full bounded `Image.LoadAsync<Rgba32>` pixel decode, and hashes SHA-256.
5. **Atomic Internal Storage (`LocalFileStorage`)**: Resolves path safely under `Storage:RootPath` (verified outside `wwwroot` at startup), writes to `.guid.tmp`, flushes, and atomically moves to `vehicles/{yyyy}/{MM}/{guid}{ext}`.
6. **Persistence & Compensation (`VehicleService`)**: Writes `Vehicle` + `VehicleImage` to SQL Server/SQLite; if DB persistence or cancellation hits after file creation, executes a compensating file delete so orphaned files do not accumulate silently.

---

## 3. What Was Added on `feature/csv-bulk-import`

Compared to `main` (`252579c`), the current branch adds end-to-end CSV bulk vehicle ingestion while preserving every guarantee of the core pipeline:

- **Backend**: Added `BulkImportOptions`, `VehicleImportResponse` / `VehicleImportRowResult`, `IVehicleImportService`, `VehicleImportService` (using `CsvHelper 33.1.0`), `POST /api/vehicles/import` on `VehiclesController`, DI & option validation wiring in `Program.cs`, and `BulkImport` config section in `appsettings.json`.
- **Frontend**: Added `BulkImportForm.jsx` (file picker, sample CSV download, duplicate-submit guard, loading state, whole-file error banner, summary counters, per-row result cards), added `importVehiclesCsv(file)` in `vehicleApi.js`, composed the sidebar stack in `DashboardPage.jsx`, removed sticky class conflict in `VehicleForm.jsx`, and styled bulk import cards in `index.css`.
- **Repository Hygiene**: Updated `.gitignore` to ignore local SQLite database files (`*.db`, `*.db-shm`, `*.db-wal`, `*.sqlite`, `*.sqlite3`).

---

## 4. Explicit Verification & Truthfulness Notes

- **Automated Test Suite (`dotnet test`)**: Passed (`138` unit tests and `59` integration tests = `197` total) after backend commit `a13313b`. Commit `49e9d84` subsequently modified `VehicleImportService.cs` alongside frontend files; after `49e9d84`, `dotnet build`, `npm run build`, and live headless browser checks passed, while `dotnet test` was **not** re-run after `49e9d84` (dedicated unit/integration tests for `POST /api/vehicles/import` are also not yet added in `tests/`).
- **Important Naming Reality**: Retrievers (`DirectImageRetriever`, `HtmlImageRetriever`) are concrete scoped classes orchestrated directly by `RetrievalOrchestrator` — there is **no** `IImageRetriever` interface in the checked-out codebase (it was removed during cleanup in `main` commit `4ae357f`). Similarly, `ImageStatus` only has `Completed` and `Failed`. Always state the actual checked-out design during interviews.
