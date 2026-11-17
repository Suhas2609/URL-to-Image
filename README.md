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

### Frontend workflow

The React dashboard uses only the backend API:

1. Submit make, model, optional variant, year, and a supplier URL.
2. Wait for `POST /api/vehicles` to confirm retrieval, validation, storage, and persistence.
3. Browse the paged vehicle catalog and load optional details with `GET /api/vehicles/{id}`.
4. View images through the returned internal `/api/images/{id}` URL. Supplier image URLs are never used as image sources.
5. Replace an image with `PUT /api/vehicles/{id}/image`; a failed replacement leaves the current stored image displayed.

For a separately hosted frontend, set `VITE_API_BASE_URL` to the API origin. During local Vite development, `VITE_API_TARGET` controls the `/api` proxy target (default `http://localhost:5080`).

The backend accepts direct image URLs, redirecting image URLs, and static HTML pages containing supported image metadata or image elements. Images are limited to validated JPEG, PNG, and WebP content, with magic-byte checks, dimension and pixel limits, bounded streaming, and full decoding before local storage.

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
