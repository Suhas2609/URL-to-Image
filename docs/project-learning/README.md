# VehicleImageSystem (`URL-to-Image`) — Complete Project Learning & Technical Reference Suite

**Repository:** `Suhas2609/URL-to-Image`
**Documented Branch:** `feature/google-drive-image-retrieval` (built upon `feature/csv-bulk-import` and `main`)
**Stack:** ASP.NET Core 10 Web API (`VehicleImage.Api`) · Entity Framework Core 10 (SQL Server primary, SQLite supported) · `SixLabors.ImageSharp` · `HtmlAgilityPack` · `CsvHelper` · React 19 + Vite 6

> [!IMPORTANT]
> **Source-of-Truth Guarantee:** Every architecture claim, file path, class name, method signature, error code, configuration key, and source snippet in this learning suite was verified directly against the checked-out `feature/google-drive-image-retrieval` tree. Intentional limitations and behaviors (such as the absence of authentication, synchronous execution without a background job queue, and why JS-rendered / Akamai-protected URLs like `magnific.com` return `NO_IMAGE_FOUND`) are documented explicitly rather than glossed over.

---

## 1. Document Index & Recommended Reading Order

| Order | Document | Purpose |
| :--- | :--- | :--- |
| **1** | [`01-complete-architecture-and-learning-guide.md`](./01-complete-architecture-and-learning-guide.md) | 15 sequential learning modules (startup to React UI), 20 Mermaid architecture/sequence/ER diagrams with written walkthroughs, and 32+ end-to-end execution scenarios |
| **2** | [`02-complete-project-directory-and-file-reference.md`](./02-complete-project-directory-and-file-reference.md) | Clean repository tree (excluding generated/runtime artifacts), `main` vs `feature/csv-bulk-import` vs `feature/google-drive-image-retrieval` diff table, and file-by-file breakdown in dependency order |
| **3** | [`03-complete-source-code-backend.md`](./03-complete-source-code-backend.md) | Verbatim, unabbreviated source code of every runtime backend file with purpose notes before each block |
| **4** | [`04-complete-source-code-frontend.md`](./04-complete-source-code-frontend.md) | Verbatim, unabbreviated source code of all 14 frontend files (`index.html`, Vite config, API client, React pages/components, CSS) |
| **5** | [`05-interview-code-defence-question-bank.md`](./05-interview-code-defence-question-bank.md) | 14-category interview question bank (60+ repo-grounded Q&As including deep-dive code-defense scenarios) with interviewer intent, answers, file/method anchors, follow-ups, and pitfalls |
| **6** | [`06-step-by-step-learning-roadmap.md`](./06-step-by-step-learning-roadmap.md) | 7-day structured study plan prioritizing backend pipeline → security & validation → storage & DB → CSV bulk import → Google Drive retrieval → frontend → mock defense |
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

1. **URL Policy Check (`UrlValidator`)**: Enforces `http`/`https`, no userinfo, length `<= 2048`, no control chars, ports in `{80, 443}`, rejects `localhost` / loopback IPs (`127.0.0.1`, `[::1]`), and requires a valid IDN domain name.
2. **Bounded HTTP Retrieval & Early Prefix Classification (`RetrievalOrchestrator` -> `DirectImageRetriever` / `HtmlImageRetriever` / `GoogleDriveImageRetriever`)**: Single initial `GET` (no mandatory `HEAD`) via `ImageRetrievalClient` (`SocketsHttpHandler` with `AllowAutoRedirect = false`, `UseCookies = false`, gzip/deflate/brotli decompression, manual 5-hop redirect loop with per-hop revalidation, 15s timeout). Inspects the initial 1 KB prefix (`ClassificationPrefixBytes = 1024`) directly from the network stream under `HttpCompletionOption.ResponseHeadersRead` before buffering the rest of the body:
   - **Supported images (`JPEG`, `PNG`, `WebP`)**: Preserves the 1 KB prefix and streams the remainder into `MemoryStream` up to `MaxDownloadBytes = 20 MB`.
   - **HTML pages (`allowHtml: true`)**: Preserves the 1 KB prefix, finishes reading up to 20 MB, and passes to `HtmlImageRetriever` for candidate extraction (`og:image`, `twitter:image`, `srcset`, `data-src`, `<img>`).
   - **Unsupported formats (`GIF`, `BMP`, `TIFF`, `ICO`, `AVIF`) & non-image streams (PDF, video, JSON, SVG)**: Aborts immediately after <= 1024 bytes without allocating memory or wasting bandwidth downloading megabytes of dead payloads.
   - **HTML candidate fetches (`allowHtml: false`)**: Candidate images returning HTML abort immediately after reading the prefix, falling back to the next candidate.
3. **Format-Agnostic Content-First Classification (`RetrievalOrchestrator`)**: Inspects binary magic bytes and HTML markers (never trusting `.jpg` extensions or HTTP `Content-Type` alone) to dispatch Google Drive URLs to `GoogleDriveImageRetriever`, HTML pages to `HtmlImageRetriever`, and direct payloads to `ImageValidator`. If an image-extension URL yields an HTML error page with no images, it reports `UNSUPPORTED_CONTENT_TYPE`.
4. **Binary & Pixel Validation (`ImageValidator`)**: Verifies magic bytes (`JPEG`, `PNG`, `WebP`), rejects JPEG missing `FF D9` EOI marker, checks PNG `IHDR` bounds + `Image.IdentifyAsync` dimensions (`100x100` min to `10000x10000` max, `<= 50M` pixels) to stop decompression bombs, performs full bounded `Image.LoadAsync<Rgba32>` pixel decode, and hashes SHA-256.
5. **Atomic Internal Storage (`LocalFileStorage`)**: Resolves path safely under `Storage:RootPath` (verified outside `wwwroot` at startup), writes to `.guid.tmp`, flushes, and atomically moves to `vehicles/{yyyy}/{MM}/{guid}{ext}`.
6. **Persistence & Compensation (`VehicleService`)**: Writes `Vehicle` + `VehicleImage` to SQL Server/SQLite; if DB persistence or cancellation hits after file creation, executes a compensating file delete so orphaned files do not accumulate silently.

---

## 3. What Was Added on `feature/csv-bulk-import`

Compared to `main` (`252579c`), this branch added end-to-end CSV bulk vehicle ingestion:

- **Backend**: Added `BulkImportOptions`, `VehicleImportResponse` / `VehicleImportRowResult`, `IVehicleImportService`, `VehicleImportService` (using `CsvHelper 33.1.0`), `POST /api/vehicles/import` on `VehiclesController`, DI & option validation wiring in `Program.cs`, and `BulkImport` config section in `appsettings.json`.
- **Frontend**: Added `BulkImportForm.jsx` (file picker, sample CSV download, duplicate-submit guard, loading state, whole-file error banner, summary counters, per-row result cards), added `importVehiclesCsv(file)` in `vehicleApi.js`, composed the sidebar stack in `DashboardPage.jsx`, and styled bulk import cards in `index.css`.
- **Repository Hygiene**: Updated `.gitignore` to ignore local SQLite database files (`*.db`, `*.db-shm`, `*.db-wal`, `*.sqlite`, `*.sqlite3`).

---

## 4. What Was Added on `feature/google-drive-image-retrieval`

Building on top of CSV bulk import, the `feature/google-drive-image-retrieval` branch adds:

1. **Google Drive Public Image Retrieval (`GoogleDriveImageRetriever`)**:
   - Detects public Google Drive sharing links (`https://drive.google.com/file/d/{id}/view...`, `/open?id={id}`, `/uc?id={id}`).
   - Translates them into direct download endpoints (`/uc?export=download&id={id}`).
   - Handles Google Drive large-file virus-scan warning pages by extracting `confirm={token}` cookies/query params and resending the request.
   - Enforces domain boundaries (`drive.google.com`, `drive.usercontent.google.com`), 15s timeouts, and 20 MB bounded streaming.
   - Registers `Strategy = "GoogleDrive"` in `VehicleImage` metadata.
2. **Simplified URL Validation**:
   - Streamlined `UrlValidator` to enforce scheme (`http`/`https`), allowed ports (`80`, `443`), length, no userinfo, valid domain syntax, and `localhost` / loopback literal blocking.
   - Removed complex CIDR blocklists and DNS IP-pinning socket callbacks (`DestinationValidator`, `SsrfGuardHttpHandler`) to maintain a clean portfolio architecture.
   - Renamed integration suite to `UrlValidationTests.cs`.
3. **Static Sidebar & Scrollable Catalog Dashboard**:
   - Styled the left submission section as a static console window with macOS-style window control dots (`● ● ●`), title bar, and action dots.
   - Made the right vehicle catalog independently scrollable with a sticky header, ensuring submission forms remain permanently visible while browsing vehicle cards.

---

## 5. Verification & Test Suite Status

- **Automated Test Suite (`dotnet test --configuration Release`)**: Passed (`115/115` total — `65` unit tests, `50` integration tests, `0` failed, `0` skipped).
- **Frontend Build (`npm run build`)**: Passed (`vite build` in 1.14s, 37 modules transformed).
- **Live Servers**: Backend running on port 5080 with SQLite; Frontend running on Vite port 5173 proxying `/api` and `/health`.
