# Architecture Decisions, Known Limitations & Operational Notes

## 1. Resolved Architectural Decisions

1. **Failed Initial Submissions (`POST /api/vehicles`)**:
   - Failed submissions return `422 Unprocessable Entity` with a structured RFC 7807 `ImageFailureResponse` (`code`, `title`, `detail`, `status`, `traceId`, `retryable`, `retryAfterSeconds`) and do not persist broken rows in the database.
   - Failed replacements (`PUT /api/vehicles/{id}/image`) likewise return `422` without modifying the existing `VehicleImage` row or deleting the existing stored file.
2. **Error Code Taxonomy (`Exceptions/ErrorCodes.cs`)**:
   - Uses the merged error code table covering validation (`VALIDATION_FAILED`), URL checks (`INVALID_URL`, `UNSAFE_DESTINATION`, `DNS_RESOLUTION_FAILED`), transport (`CONNECTION_TIMEOUT`, `REQUEST_TIMEOUT`, `TLS_FAILURE`, `REDIRECT_LIMIT_EXCEEDED`), upstream HTTP (`HTTP_NOT_FOUND`, `HTTP_UNAUTHORIZED`, `HTTP_FORBIDDEN`, `HTTP_RATE_LIMITED`, `HTTP_SERVER_ERROR`, `HTTP_ERROR`), content/image validation (`UNSUPPORTED_SOURCE`, `UNSUPPORTED_CONTENT_TYPE`, `UNSUPPORTED_IMAGE_FORMAT`, `IMAGE_TOO_LARGE`, `IMAGE_DIMENSIONS_EXCEEDED`, `IMAGE_TOO_SMALL`, `IMAGE_DECODE_FAILED`, `NO_IMAGE_FOUND`, `PROVIDER_AUTH_REQUIRED`), and infrastructure (`STORAGE_FAILED`, `DATABASE_PERSISTENCE_FAILED`, `VEHICLE_NOT_FOUND`, `IMAGE_NOT_FOUND`, `CONCURRENT_UPDATE`, `INTERNAL_ERROR`).
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
4. **Basic URL Validation**:
   - `UrlValidator` validates scheme (`http`/`https`), URL length, port restrictions (`80`/`443`), hostname format, and blocks `localhost`/loopback targets both on initial input and on every manual redirect hop. For untrusted multi-tenant internet deployments, add host/VPC egress firewall rules or dedicated SSRF network isolation.
5. **EXIF Metadata**:
   - Validated original image bytes are stored unchanged in Phase 1 to preserve exact byte fidelity and SHA-256 digests. If supplier images may contain sensitive EXIF metadata (such as GPS coordinates), metadata stripping via `ImageSharp` (`image.Metadata.ExifProfile = null`) can be enabled before saving.
