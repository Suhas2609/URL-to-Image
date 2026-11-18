# Interview & Code-Defence Question Bank

This bank contains **61 deep, repository-grounded interview questions** organized across **14 topic categories**. Every question uses a 6-part structure anchored directly to exact files, classes, methods, and behaviors on branch `feature/google-drive-image-retrieval` (built upon `feature/csv-bulk-import` and `main`).

---

## Category 1: Overall Architecture & Design Philosophy

### Q1.1 Why does this application download, decode, and store supplier images instead of storing and rendering the supplier URL directly?
- **What the interviewer is testing:** Security awareness (SSRF, XSS, tracking), availability guarantees, and systems ownership boundaries.
- **Technically correct answer grounded in this repo:** Storing or rendering external URLs (`<img src="supplierUrl">`) causes four failures: (1) broken links / hotlink blocking when suppliers remove or rate-limit images, (2) mixed-content/privacy leaks to supplier servers, (3) content mutability (supplier swaps a clean car photo for offensive content or an SVG `<script>`), and (4) lack of verified format/dimension guarantees. Instead, `ImageIngestionService.IngestAsync` retrieves the bytes server-side, `ImageValidator.ValidateAsync` proves the payload decodes cleanly as JPEG/PNG/WebP within bounds, `LocalFileStorage.SaveAsync` stores an independent immutable copy outside `wwwroot`, and all read DTOs (`VehicleResponse`, `ImageSubmissionResponse`, `VehicleImportRowResult`) return only `/api/images/{id}`.
- **Relevant file and method:** `Services/ImageIngestionService.cs` (`IngestAsync`), `Services/VehicleService.cs` (`BuildInternalImageUrl`, `MapToResponse`), `Controllers/ImagesController.cs` (`Get`).
- **Likely follow-up question:** Are supplier URLs ever returned by read endpoints?
- **Common superficial or incorrect answer to avoid:** "To make images load faster via local caching" — speed is secondary; decoupling runtime availability and security validation is primary. (Also note: `VehicleImage` *does* store `SourceUrl` and `FinalSourceUrl` in the database for auditability, while DTOs omit them.)

### Q1.2 Why does `RetrievalOrchestrator` perform a single `GET` instead of sending a `HEAD` request first to check `Content-Type` and `Content-Length`?
- **What the interviewer is testing:** Practical HTTP protocol experience and latency/correctness trade-offs.
- **Technically correct answer grounded in this repo:** Many CDNs and image hosts return `405 Method Not Allowed`, `403`, or inaccurate/missing `Content-Type` and `Content-Length` headers on `HEAD` requests, and a `HEAD` adds an extra RTT and doubles redirect hops. `RetrievalOrchestrator.RetrieveCandidatesAsync` issues one initial `GET` with `HttpCompletionOption.ResponseHeadersRead` via `DirectImageRetriever.FetchWithRetriesAsync`. Instead of buffering the entire response before making a decision, `DirectImageRetriever.ReadBoundedWithClassificationAsync` sniffs the first 1 KB (`ClassificationPrefixBytes = 1024`): if image magic bytes match, it continues streaming image bytes up to 20 MB; if HTML or text markup is detected, it reads up to `MaxHtmlBytes` (2 MB) and returns `RetrievedImage` for `RetrievalOrchestrator.IsHtmlResponse` and `HtmlImageRetriever`; if unsupported content is detected, it aborts immediately after 1 KB without downloading the rest of the payload.
- **Relevant file and method:** `Retrieval/RetrievalOrchestrator.cs` (`RetrieveCandidatesAsync`, `IsHtmlResponse`), `Retrieval/DirectImageRetriever.cs` (`ReadBoundedWithClassificationAsync`).
- **Likely follow-up question:** What happens if the URL points to a 500 MB video file since you didn't send `HEAD` first? (The 1 KB prefix sniffing detects neither an image binary signature nor HTML markup and aborts early after reading only 1 KB, throwing `UNSUPPORTED_CONTENT_TYPE` without buffering the remaining 499.9 MB!)
- **Common superficial or incorrect answer to avoid:** "Because `HttpClient` doesn't support `HEAD`" or "Because we inspect `Content-Length` on the `GET` response instead" (we stream bounded bytes in chunks, sniffing the first 1 KB to classify content early).

### Q1.3 Why is ingestion synchronous during the HTTP request right now, and when would you move it to a background worker queue?
- **What the interviewer is testing:** Pragmatic MVP scoping vs distributed scaling awareness.
- **Technically correct answer grounded in this repo:** Both `POST /api/vehicles` and `POST /api/vehicles/import` execute `IngestAsync` synchronously within the request lifecycle (bounded by 15s upstream timeouts and max 50 CSV rows). This eliminates queue infrastructure, polling endpoints, and pending-row states (`ImageStatus` only contains `Completed` and `Failed`), giving users immediate `201 Created` or `422` feedback and atomic CSV per-row results. You would move to a background queue (`Channel<T>` / Hangfire / Azure Service Bus + outbox) when batch sizes exceed ~50 rows, headless browser rendering is introduced, or SLA requires sub-second HTTP `202 Accepted` webhook/polling workflows.
- **Relevant file and method:** `Services/VehicleService.cs` (`CreateAsync`), `Services/VehicleImportService.cs` (`ImportCsvAsync`), `Entities/VehicleImage.cs` (`ImageStatus`).
- **Likely follow-up question:** If a CSV import takes 90 seconds in Kestrel, what risk arises? (Client/reverse-proxy HTTP timeout -> `HttpContext.RequestAborted` fires -> `ProcessRowAsync` rethrows `OperationCanceledException` -> partial already-committed rows stay persisted while remaining rows stop and in-flight row deletes its temp file.)
- **Common superficial or incorrect answer to avoid:** "It already uses background jobs with `ImageStatus.Processing`" — `Processing` exists in CSS/frontend `StatusBadge.jsx` only as a display guard, not in the backend `ImageStatus` enum!

### Q1.4 What architectural changes would be required if a supplier site renders vehicle images only via client-side JavaScript or anti-bot challenges (like `magnific.com`)?
- **What the interviewer is testing:** Understanding why `HtmlAgilityPack` fails on SPAs/WAF challenges and ethical/operational constraints.
- **Technically correct answer grounded in this repo:** `HtmlImageRetriever` only parses static HTML DOM elements. On `https://www.magnific.com/free-psd/silver-sedan-car_3993591.htm`, Akamai Bot Manager serves an HTTP `200 OK` 2,313-byte JavaScript proof-of-work challenge containing zero `<img>`, `og:image`, or `twitter:image` tags, so `ExtractAndRankCandidates` returns an empty list and ingestion fails cleanly with `422 NO_IMAGE_FOUND`. Note two distinct problems: (1) legitimate JS-rendered SPAs (React/Next client hydration) could be solved via an isolated, egress-restricted headless browser/Playwright fallback behind `IImageRetriever` + an async job queue; (2) anti-bot/CAPTCHA/WAF challenges must **not** be bypassed—those should surface `NO_IMAGE_FOUND` or require official supplier API feeds / direct CDN URLs.
- **Relevant file and method:** `Retrieval/RetrievalOrchestrator.cs` (`IsHtmlResponse`), `Retrieval/HtmlImageRetriever.cs` (`ExtractCandidates`).
- **Likely follow-up question:** Why does `magnific.com` return `NO_IMAGE_FOUND` instead of `HTTP_FORBIDDEN`? (Because Akamai sends HTTP `200 OK` HTML with a `<script>` challenge, not HTTP `403`!)
- **Common superficial or incorrect answer to avoid:** "Add headers or headless Chromium to solve Cloudflare/Akamai bot challenges" — violates security/legal boundaries and shouldn't be mixed with legitimate SPA fallback.

---

## Category 2: ASP.NET Core Startup, DI Lifetimes & Configuration

### Q2.1 How does `Program.cs` validate configuration, and why resolve `StorageOptions.RootPath` in `PostConfigure`?
- **What the interviewer is testing:** .NET Options pattern mastery and fail-fast deployment safety.
- **Technically correct answer grounded in this repo:** Every options class (`StorageOptions`, `RetrievalOptions`, `ImageValidationOptions`, `HtmlExtractionOptions`, `RetryOptions`, `BulkImportOptions`) uses `.Bind(...).ValidateDataAnnotations().ValidateOnStart()`. Misconfigurations (e.g., `BulkImport:MaxRecords = 1000` violating `[Range(1, 500)]`) throw `OptionsValidationException` at boot rather than mid-request. `StorageOptions` chains `.PostConfigure(...)` to convert relative paths like `"../../storage"` via `Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, options.RootPath))` before `ValidateOnStart()` and `LocalFileStorage` run, ensuring consistent resolution whether started from IDE, `dotnet run`, or systemd.
- **Relevant file and method:** `Program.cs` (Lines 18–53), `Configuration/BulkImportOptions.cs`, `Configuration/StorageOptions.cs`.
- **Likely follow-up question:** Why keep `AbsoluteMaxFileBytes` as a compile-time `const` in `BulkImportOptions`? (Because ASP.NET Core `[RequestSizeLimit(...)]` and `[RequestFormLimits(...)]` attributes on `VehiclesController.Import` require compile-time constants.)
- **Common superficial or incorrect answer to avoid:** "We read `IConfiguration` lazily inside services" — incorrect; validated typed `IOptions<T>` are bound and verified at startup.

### Q2.2 Explain the DI service lifetimes in `Program.cs`. What bug would occur if `VehicleService` or `ApplicationDbContext` were registered as `Singleton`?
- **What the interviewer is testing:** .NET Dependency Injection captive dependency and concurrency bugs.
- **Technically correct answer grounded in this repo:** Stateless or thread-safe shared components (`UrlValidator`, `ImageValidator`, `LocalFileStorage`) are `Singleton`. Note `ImageValidator` clones a single `SixLabors.ImageSharp.Configuration` with capped `MemoryAllocator` (`256 MB` limit) and uses stateless local streams in `ValidateAsync`, making singleton reuse both safe and memory-efficient. Conversely, `ApplicationDbContext` is `Scoped` (not thread-safe, tracks per-request UoW state), so `VehicleService`, `ImageDeliveryService`, `RetrievalOrchestrator`, `ImageIngestionService`, and `VehicleImportService` are `Scoped`. Making `VehicleService` Singleton would cause EF Core captive-dependency validation failures at startup and catastrophic cross-request `DbContext` concurrency corruption.
- **Relevant file and method:** `Program.cs` (Lines 56–70, 110–136), `ImageProcessing/ImageValidator.cs` (constructor).
- **Likely follow-up question:** If `VehicleImportService` is `Scoped`, why does it inject `IServiceScopeFactory` instead of `IVehicleService` directly? (To give each CSV row its own fresh `DbContext`!)
- **Common superficial or incorrect answer to avoid:** "Making everything `Scoped` is always best" — recreating `ImageValidator`'s `MemoryAllocator` pool or `SocketsHttpHandler` per request would waste native memory and exhaust ephemeral TCP sockets (`SNAT`/port exhaustion).

### Q2.3 Why does `Program.cs` verify `Storage:RootPath` is outside `wwwroot` and write a physical probe file on startup?
- **What the interviewer is testing:** Defense against static-file exposure and operational readiness.
- **Technically correct answer grounded in this repo:** `InitializeAndVerifyStorageRoot` canonicalizes both `Storage:RootPath` and `WebRootPath` (`wwwroot`), throwing `InvalidOperationException` if storage equals or is a subdirectory of `wwwroot`. This prevents Kestrel's static file middleware (if ever enabled) from bypassing `/api/images/{id}` and serving files without `X-Content-Type-Options: nosniff` or DB checks. `VerifyStorageRootWritable` immediately writes `[0x56, 0x49]` with `FileOptions.WriteThrough` + `Flush(true)` into a GUID `.tmp` file and deletes it—failing fast on container permission errors rather than failing the first upload.
- **Relevant file and method:** `Program.cs` (`InitializeAndVerifyStorageRoot`, `VerifyStorageRootWritable`).
- **Likely follow-up question:** Look closely at `Path.Combine` checks: why does `InitializeAndVerifyStorageRoot` append `Path.DirectorySeparatorChar` to `webRoot` before `StartsWith`? (To prevent `/app/wwwroot2` from false-matching prefix `/app/wwwroot`!)
- **Common superficial or incorrect answer to avoid:** "It just calls `Directory.CreateDirectory`" — creating an existing directory does not prove write/delete permissions inside mounted Docker volumes or SELinux/Windows ACL paths.

### Q2.4 Why is `UseExceptionHandler()` placed before `UseCors()`, `UseAuthorization()`, and `MapControllers()` in `Program.cs`?
- **What the interviewer is testing:** ASP.NET Core middleware execution pipeline ordering.
- **Technically correct answer grounded in this repo:** Middleware executes as a nested Russian-doll pipeline: outer middleware wraps inner middleware on the return path. Putting `app.UseExceptionHandler()` first guarantees any unhandled exception or domain exception (`ImageIngestionException`, `ValidationException`, etc.) thrown in `Cors`, routing, model binding/action execution, or services unwinds into `GlobalExceptionHandler.TryHandleAsync` and returns a clean `application/problem+json` response.
- **Relevant file and method:** `Program.cs` (Lines 164–174).
- **Likely follow-up question:** Why does `InvalidModelStateResponseFactory` also exist if `GlobalExceptionHandler` handles exceptions? (Because ASP.NET `[ApiController]` handles `ModelState` validation failures by returning a `BadRequestObjectResult` *without* throwing an exception, so `InvalidModelStateResponseFactory` ensures malformed JSON request bodies match the exact same `ImageFailureResponse` schema!)
- **Common superficial or incorrect answer to avoid:** "`GlobalExceptionHandler` catches 400 `[ApiController]` model binding errors too" — false unless explicitly shaped in `ConfigureApiBehaviorOptions`.

---

## Category 3: URL Policy & Retrieval Security

### Q3.1 Walk through what URL policy validation checks are enforced before and during outbound HTTP retrieval.
- **What the interviewer is testing:** Practical URL policy enforcement, redirect validation, and architectural tradeoffs.
- **Technically correct answer grounded in this repo:** Two checkpoints guard outbound calls:
  1. **`UrlValidator.Validate` (up front):** Enforces non-blank input, length `<= 2048`, no control characters, `http`/`https` only, no `UserInfo`, `AllowedPorts` (`80`, `443`), rejects `localhost` / `*.localhost` and `IPAddress.IsLoopback` IP literals (`127.0.0.1`, `[::1]`), and normalizes domain hosts via `IdnMapping.GetAscii` requiring `.` (`INVALID_URL` otherwise).
  2. **`DirectImageRetriever` manual redirect check:** With `AllowAutoRedirect = false`, every `301`/`302`/`303`/`307`/`308` hop runs `UrlValidator.ValidateRedirectTarget(currentUri, location)`.
- **Relevant file and method:** `Security/UrlValidator.cs` (`Validate`, `ValidateRedirectTarget`, `ValidateUriPolicy`), `Retrieval/DirectImageRetriever.cs` (`ExecuteSingleFetchAsync`).
- **Likely follow-up question:** Why did this project intentionally omit full CIDR blocklists, DNS pre-resolution, and `ConnectCallback` socket pinning? (For this controlled portfolio workflow where image URLs are operator-provided, `DestinationValidator` and `SsrfGuardHttpHandler` were removed to keep retrieval simple and proxy-compatible; if exposed to untrusted multi-tenant internet users, private-IP CIDR filtering, DNS/socket pinning, or VPC egress firewall rules would need to be restored.)
- **Common superficial or incorrect answer to avoid:** "We block `localhost` with a simple `Contains(\"localhost\")` substring check" — breaks on `http://example.com/localhost-car.jpg` and misses `127.0.0.1`, `[::1]`, disallowed ports, and open redirects!

### Q3.2 Why is HTTP handler configuration in `Program.cs` still critical after simplifying URL validation?
- **What the interviewer is testing:** Understanding HTTP transport safety defaults in `SocketsHttpHandler`.
- **Technically correct answer grounded in this repo:** `Program.cs` registers `DirectImageRetriever.HttpClientName` (`"ImageRetrievalClient"`) with a configured `SocketsHttpHandler`:
  - `AllowAutoRedirect = false` ensures `HttpClient` never silently follows a `302` to `file://`, `localhost`, or a non-standard port without passing through `UrlValidator.ValidateRedirectTarget`.
  - `UseCookies = false` prevents cross-request cookie leakage inside the pooled handler.
  - `AutomaticDecompression = GZip | Deflate | Brotli` lets `ReadBoundedStreamAsync` count *decompressed* bytes against the 20 MB cap.
  - `ConnectTimeout = 5s`, `PooledConnectionLifetime = 60s`, and `MaxResponseHeadersLength = 64` KB prevent slowloris/header-bloat stalls.
- **Relevant file and method:** `Program.cs` (Lines 114–130), `Retrieval/DirectImageRetriever.cs` (`ExecuteSingleFetchAsync`).
- **Likely follow-up question:** Why keep `UseProxy` at its default (`true`) in `Program.cs`? (Controlled/cloud environments that route outbound web requests via `HTTP_PROXY`/`HTTPS_PROXY` work seamlessly, whereas integration tests override `UseProxy = false` inside `ApiFactory.cs` via `PostConfigure<HttpClientFactoryOptions>` for local `MockSupplierServer` traffic.)

### Q3.3 Why does `UrlValidator` validate redirect targets differently from initial URLs, and how are query parameters handled in logs?
- **What the interviewer is testing:** Relative URL resolution rules and PII/token hygiene in logs.
- **Technically correct answer grounded in this repo:** Initial inputs use `UrlValidator.Validate` (`UriKind.Absolute`), whereas HTTP `3xx` `Location` headers and HTML `<base href>` / `<img src>` tags are often relative (e.g., `/images/car.jpg` or `../hero.webp`). `UrlValidator.ValidateRedirectTarget(currentUri, locationHeader)` calls `Uri.TryCreate(currentUri, trimmed, out targetUri)` to resolve relative targets against the current page URL and then runs full `ValidateUriPolicy(targetUri)`—preventing a public page from redirecting or linking via `<img src="http://localhost/admin">` or `file:///etc/passwd`. When logging candidate URLs (`HtmlImageRetriever`, `ImageIngestionService`), `UrlValidator.RedactForLogging(uri)` calls `uri.GetLeftPart(UriPartial.Path)` to strip signed S3/CDN query tokens (`?X-Amz-Signature=...`).
- **Relevant file and method:** `Security/UrlValidator.cs` (`ValidateRedirectTarget`, `RedactForLogging`).
- **Likely follow-up question:** Does `VehicleImage` database persistence redact `SourceUrl`/`FinalSourceUrl`? (No, `SourceUrl` and `FinalSourceUrl` are stored up to 2048 chars in SQL Server for auditing, but are never logged or returned in API DTOs.)
- **Common superficial or incorrect answer to avoid:** "Redirects use `HttpClient`'s built-in redirect handler" — built-in auto-redirect bypasses `ValidateRedirectTarget` and `MaxRedirects` enforcement.

---

## Category 4: HTTP Retrieval, Timeouts, Redirects & Retries

### Q4.1 How does `DirectImageRetriever` enforce download caps and early rejection even if the server omits `Content-Length`, spoofs a small `Content-Length`, or sends compressed `gzip`?
- **What the interviewer is testing:** Streaming I/O, compression amplification bombs, bounded buffering, and early format classification.
- **Technically correct answer grounded in this repo:** First, `HttpClient.SendAsync` uses `HttpCompletionOption.ResponseHeadersRead` so ASP.NET Core does **not** buffer the entire response body into memory before returning control. Second, `Program.cs` configures `SocketsHttpHandler` with `AutomaticDecompression` (`GZip | Deflate | Brotli`), meaning `await content.ReadAsStreamAsync(effectiveToken)` reads *already-decompressed* bytes—stopping a 50 KB gzip bomb that expands to 500 MB! Third, `ReadBoundedWithClassificationAsync` sniffs the first `<= 1024` bytes (`ClassificationPrefixBytes = 1024`). If the payload matches recognized but unsupported formats (`GIF`, `BMP`, `TIFF`, etc.) or non-image text/binary when `allowHtml: false`, it throws immediately—preventing downloading up to 20 MB. If allowed, it writes the prefix into `MemoryStream` and streams the remainder with a 16 KB buffer incrementing `totalRead`; the instant `totalRead > maxBytes` (`MaxImageBytes = 20 MB`, or `MaxHtmlBytes = 2 MB` for HTML), it disposes `MemoryStream` and throws `ImageIngestionException.ImageTooLarge`—completely ignoring any lying `Content-Length` header.
- **Relevant file and method:** `Retrieval/DirectImageRetriever.cs` (`ExecuteSingleFetchAsync`, `ReadBoundedWithClassificationAsync`).
- **Likely follow-up question:** Why buffer into a `MemoryStream` instead of streaming straight from network to `ImageSharp`? (Because `ImageIngestionService` and `ImageValidator` need a *seekable* stream to sniff bytes in `RetrievalOrchestrator`, inspect magic bytes, check JPEG EOI, run `IdentifyAsync`, run `LoadAsync`, compute `SHA256`, and copy to `LocalFileStorage` without re-downloading over the network.)
- **Common superficial or incorrect answer to avoid:** "We check `response.Content.Headers.ContentLength <= 20MB` before reading" — fails on `Transfer-Encoding: chunked` (null `ContentLength`) and gzip bombs!

### Q4.2 How does `DirectImageRetriever` distinguish a request timeout from a client HTTP cancellation, and why are timeouts configured on `DirectImageRetriever` instead of `HttpClient.Timeout`?
- **What the interviewer is testing:** `CancellationTokenSource.CreateLinkedTokenSource` semantics and retry behavior.
- **Technically correct answer grounded in this repo:** `Program.cs` sets `client.Timeout = Timeout.InfiniteTimeSpan` on `ImageRetrievalClient`. Inside `ExecuteSingleFetchAsync`, `DirectImageRetriever` creates `using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` and sets `timeoutCts.CancelAfter(TimeSpan.FromSeconds(15))` (`effectiveToken = timeoutCts.Token`). Keeping the timeout *per attempt* lets `FetchWithRetriesAsync` retry a timed-out attempt cleanly with a fresh 15s budget. In `catch` blocks, `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)` lets caller cancellations abort immediately (HTTP `499`), whereas `catch (OperationCanceledException ex)` (where `callerToken` is *not* canceled) recognizes an internal 15s timeout and maps it to transient `ImageIngestionException.RequestTimeout` so retries can fire. Stream reading and prefix classification inside `ReadBoundedWithClassificationAsync` pass `effectiveToken` to ensure slow streams time out cleanly and dispose resources.
- **Relevant file and method:** `Program.cs` (Lines 114–130), `Retrieval/DirectImageRetriever.cs` (`ExecuteSingleFetchAsync`, `ReadBoundedWithClassificationAsync`).
- **Likely follow-up question:** How is TCP connection timeout bounded? (`SocketsHttpHandler.ConnectTimeout` is configured in `Program.cs` to `RetrievalOptions.ConnectTimeoutSeconds = 5`.)
- **Common superficial or incorrect answer to avoid:** "Set `HttpClient.Timeout = 15s` in `Program.cs`" — `HttpClient.Timeout` applies the same hard failure across handler execution and is harder to coordinate with per-attempt retry budgets.

### Q4.3 Which errors does `DirectImageRetriever` retry automatically, and how does it handle HTTP `429 Too Many Requests`?
- **What the interviewer is testing:** Selective idempotent retries and backpressure/thread-pool protection.
- **Technically correct answer grounded in this repo:** `FetchWithRetriesAsync` runs up to `Retry:MaxAttempts` (default `3`). `ShouldRetry` rejects non-transient errors and explicitly excludes permanent codes (`InvalidUrl`, `UnsafeDestination`, `RedirectLimitExceeded`, `404`, `401`, `403`, `UnsupportedContentType`, `UnsupportedImageFormat`, `ImageTooLarge`, `ImageDimensionsExceeded`, `ImageTooSmall`, `TlsFailure`). Only transient errors retry: `DnsResolutionFailed`, `ConnectionTimeout`, `RequestTimeout`, `HttpServerError` (`5xx`), `HttpRateLimited` (`429`), and truncated/dropped stream transfer (`ImageDecodeFailed` thrown from `ReadBoundedWithClassificationAsync`). Note that early rejection of unsupported formats or non-image content in `ReadBoundedWithClassificationAsync` throws permanent `UnsupportedContentType` or `UnsupportedImageFormat`, which `ShouldRetry` recognizes as non-retryable and aborts immediately. On `429` with a `Retry-After` Delta or HTTP Date, if `RetryAfter <= MaxRetryAfterSeconds` (`3s`), it waits exactly `RetryAfter`; if `RetryAfter > 3s`, `ShouldRetry` returns `false` immediately so an external server cannot pin an ASP.NET request thread waiting 10 minutes. Otherwise it applies exponential backoff (`150ms * 2^(attempt-1)`, capped at `1500ms`) with optional ±25% random jitter.
- **Relevant file and method:** `Retrieval/DirectImageRetriever.cs` (`FetchWithRetriesAsync`, `ShouldRetry`, `EnsureSuccessOrThrowMappedStatus`, `ReadBoundedWithClassificationAsync`).
- **Likely follow-up question:** Notice `ImageValidator`'s `IMAGE_DECODE_FAILED` exception has `isTransient: true`, yet `ImageIngestionService` catches it and tries the next HTML candidate instead of retrying HTTP—why? (Retries only wrap `DirectImageRetriever.FetchWithRetriesAsync`; when validation fails on a candidate, trying candidate #2/#3 is better than re-fetching the same broken image URL 3 times, while `IsTransient = true` still informs API callers via `retryable: true`.)
- **Common superficial or incorrect answer to avoid:** "We retry all non-200 HTTP errors 3 times" — retrying `404`, `403`, `422`, or `UNSAFE_DESTINATION` wastes latency and hammers suppliers.

---

## Category 5: Single-GET Sniffing & HTML Candidate Extraction

### Q5.1 How does `RetrievalOrchestrator.IsHtmlResponse` decide whether a response is a direct image or an HTML page without trusting headers?
- **What the interviewer is testing:** MIME sniffing order of precedence, content-first classification, and masquerading extension handling.
- **Technically correct answer grounded in this repo:** `IsHtmlResponse` inspects actual content bytes before extensions:
  1. **Binary Magic Bytes (Highest Precedence):** Sniffs the initial 1 KB buffer using `ImageValidator.TryMatchBinaryImageSignature`. If it matches JPEG (`FF D8 FF`), PNG (`89 50 4E 47`), WebP (`RIFF....WEBP`), or recognized unsupported formats (`GIF`, `BMP`, `TIFF`, `ICO`, `AVIF`), it immediately returns `false` (direct image path), even if a misconfigured server sent `Content-Type: text/html`!
  2. **SVG / XML Markup:** If the trimmed UTF-8 prefix starts with `<svg` or `<?xml` containing `<svg`, it returns `false` so `ImageValidator` rejects it cleanly as `UNSUPPORTED_CONTENT_TYPE` (not parsed as an HTML webpage).
  3. **Declared HTML Content-Type:** If `Content-Type` is explicitly `text/html` or `application/xhtml+xml`, returns `true`.
  4. **HTML Markup Tags:** Scans the UTF-8 prefix for HTML markers (`<!doctype html`, `<html`, `<head`, `<body`, `<meta`, `<img`, `og:image`, `twitter:image`). If present, returns `true`.
  5. **Extension Fallback:** Only if no HTML markers are present does it check `HasKnownImageExtension(FinalSourceUrl)`: if yes, returns `false` (direct image path).
- **Relevant file and method:** `Retrieval/RetrievalOrchestrator.cs` (`IsHtmlResponse`, `RetrieveCandidatesAsync`).
- **Likely follow-up question:** What if someone submits a URL ending in `.jpg` (e.g. `/photo.jpg`) that returns an HTML error page with 0 images? (Because binary magic bytes do not match and HTML markers are present, Step 4 classifies it as HTML. `HtmlImageRetriever` extracts 0 candidates. `RetrievalOrchestrator` catches `NO_IMAGE_FOUND` and, recognizing the URL had a known image extension, rethrows `UNSUPPORTED_CONTENT_TYPE`—preventing masquerading HTML from being mistaken for a missing vehicle image.)
- **Common superficial or incorrect answer to avoid:** "It simply checks `response.ContentType == \"text/html\"` or trusts the `.jpg` extension first" — extension-first routing misclassifies HTML error pages, and header-first classification fails when CDNs send inaccurate MIME types.

### Q5.2 Explain how `HtmlImageRetriever` extracts, filters, scores, and falls back across image candidates.
- **What the interviewer is testing:** Real-world HTML parsing quirks (`HtmlAgilityPack`, charsets, `srcset`, relative URLs, multi-candidate fallback, and candidate streaming protection).
- **Technically correct answer grounded in this repo:**
  1. `DecodeHtml` respects header charset or regex-scans the first 2048 ASCII bytes for `<meta charset=...>` (with `CodePagesEncodingProvider` registered).
  2. `EnsureClosedTitleTag` repairs unclosed `<title>` tags that otherwise break `HtmlAgilityPack` DOM parsing; `IsHtmlErrorPage` inspects `<title>` and `<h1>` for phrases like `"404 not found"` or `"access denied"` -> returning `[]` (`NO_IMAGE_FOUND`).
  3. `ResolveBaseUri` resolves and URL-validates `<base href>` via `UrlValidator.ValidateRedirectTarget`.
  4. Extracts `<meta>` (`og:image` base score `400`, `twitter:image` base score `300`) and `<img>` (`srcset`/`data-srcset` picking largest `w` or `x` descriptor score `200`; `data-src`/`data-original`/`data-lazy-src` score `100`; `src` score `100`), adding `+50` if declared width `>= 400` or height `>= 300`.
  5. Drops `< 80px` declared dimensions, `data:`/`javascript:`/`about:`/`#`, `.svg`, anything failing `UrlValidator.ValidateRedirectTarget`, and URLs or `class`/`id`/`alt` strings matching `IgnoreUrlPatterns` (`logo`, `icon`, `favicon`, `sprite`, `pixel`, `/ads/`, etc.).
  6. Deduplicates by `AbsoluteUri` keeping the max score, sorts descending, and lazily yields up to `Math.Min(3, MaxCandidates)` candidates via `IAsyncEnumerable<RetrievedImage>`.
  7. **Candidate Streaming Protection:** When fetching each candidate, `RetrieveCandidatesFromHtmlAsync` passes `allowHtml: false` to `DirectImageRetriever.FetchWithRetriesAsync(candidateUri, "HtmlCandidate", cancellationToken, allowHtml: false)`. If a candidate URL returns an HTML error page or hotlink challenge, `ReadBoundedWithClassificationAsync` sniffs the 1 KB prefix and aborts early after reading only 1 KB—allowing `ImageIngestionService` to fall back immediately to Candidate #2 without downloading megabytes of HTML.
- **Relevant file and method:** `Retrieval/HtmlImageRetriever.cs` (`DecodeHtml`, `ExtractCandidates`, `Rank`, `RetrieveCandidatesFromHtmlAsync`), `Retrieval/DirectImageRetriever.cs` (`ReadBoundedWithClassificationAsync`).
- **Likely follow-up question:** If Candidate #1 returns `200 OK` HTML (a tracking link) or a 40x40 icon while Candidate #2 is a valid `1200x800` JPEG, what happens? (`DirectImageRetriever` aborts Candidate #1 after 1 KB with `UNSUPPORTED_CONTENT_TYPE`, `ImageIngestionService` catches the exception, logs it, and advances the `await foreach` loop to fetch and validate Candidate #2!)
- **Common superficial or incorrect answer to avoid:** Mentioning `<header>`/`<nav>`/`<footer>` container penalties or `"car"`/`"vehicle"` filename keyword boosts — those were removed in `main` commit `252579c` to simplify `HtmlImageRetriever`!

### Q5.3 How does the initial 1 KB inspection differ fundamentally from full image validation?
- **What the interviewer is testing:** Architectural separation of concerns between HTTP streaming routing and cryptographic/format validation.
- **Technically correct answer grounded in this repo:** The 1 KB inspection in `DirectImageRetriever.ReadBoundedWithClassificationAsync` is **classification**, NOT image validation. Its sole purpose is routing and fast rejection: determining whether to stream up to 20 MB as an image, stream up to 2 MB as HTML for extraction, or abort immediately on unsupported payloads (e.g. PDF, video, audio, or GIF/BMP). Validating that an image is genuinely valid requires completing the full download and running `ImageValidator.ValidateAsync`: verifying the JPEG EOI tail marker (`FF D9`), parsing animation frames and dimension bounds via `Image.IdentifyAsync`, fully decoding all scanlines to catch corrupt bitstreams via `Image.LoadAsync<Rgba32>`, and computing a SHA-256 hash. An initial magic byte match (`FF D8 FF`) only indicates the stream starts like a JPEG; only full validation proves it is uncorrupted and renderable.
- **Relevant file and method:** `Retrieval/DirectImageRetriever.cs` (`ReadBoundedWithClassificationAsync`), `ImageProcessing/ImageValidator.cs` (`ValidateAsync`).
- **Likely follow-up question:** What would happen if you skipped full validation after the 1 KB prefix matched? (Corrupted images, truncated downloads, decompression bombs, or multi-frame animated files would be stored in the database and saved to disk, breaking frontend display.)
- **Common superficial or incorrect answer to avoid:** "The 1 KB check validates the image format, so we know it's a good image" — magic bytes only classify file type; full pixel decoding is essential to guarantee image integrity.

### Q5.4 How does the 1 KB classification handle whitespace, UTF Byte Order Marks (BOM), and unusual HTML prefixes?
- **What the interviewer is testing:** Robust text decoding and edge cases in prefix sniffing.
- **Technically correct answer grounded in this repo:** Both `ImageValidator.IsLikelyTextMarkupOrJson` and `DirectImageRetriever.ReadBoundedWithClassificationAsync` decode the prefix as UTF-8 and strip whitespace and Unicode BOMs before inspection:
  ```csharp
  string textSample = Encoding.UTF8.GetString(header).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
  ```
  `\uFEFF` is the UTF-8 Byte Order Mark (`EF BB BF`). Stripping it along with leading spaces, tabs, carriage returns, and newlines ensures that HTML files starting with blank lines, indentation, XML declarations (`<?xml version="1.0"?>`), or comments (`<!--`) correctly expose their opening markup tags (`<html`, `<!doctype`, `<head`) rather than slipping past classification as unrecognized binary content.
- **Relevant file and method:** `ImageProcessing/ImageValidator.cs` (`IsLikelyTextMarkupOrJson`), `Retrieval/DirectImageRetriever.cs` (`ReadBoundedWithClassificationAsync`).
- **Likely follow-up question:** What if the response contains invalid UTF-8 sequences in the 1 KB buffer? (`Encoding.UTF8.GetString` replaces invalid bytes with `\uFFFD` without throwing, so subsequent `StartsWith` and `Contains` checks complete safely.)
- **Common superficial or incorrect answer to avoid:** "We only check if byte 0 is `<`" — fails completely on UTF-8 BOMs, leading newlines, and indented HTML.

### Q5.5 How does `DirectImageRetriever` preserve the first 1 KB when continuing the download so bytes are never lost?
- **What the interviewer is testing:** Accurate streaming I/O buffer management.
- **Technically correct answer grounded in this repo:** In `ReadBoundedWithClassificationAsync`, the prefix bytes are read into an array `byte[] prefixBuffer = new byte[ClassificationPrefixBytes]`. Once content classification determines that streaming should proceed (e.g. a valid image or allowed HTML), `memoryStream.Write(prefixBuffer, 0, prefixCount)` writes the entire prefix buffer into the target `MemoryStream` **before** entering the remainder read loop. The subsequent loop reads remaining chunks (`16 KB`) directly from the network stream into `memoryStream`. Finally, `memoryStream.Position = 0` rewinds the stream to the beginning. The consumer receives the complete, uninterrupted payload with zero missing bytes.
- **Relevant file and method:** `Retrieval/DirectImageRetriever.cs` (`ReadBoundedWithClassificationAsync`, lines 180–235).
- **Likely follow-up question:** What if the total response is smaller than 1 KB (e.g. an 800-byte small thumbnail)? (The prefix loop reads until EOF, `prefixCount` is 800, `memoryStream.Write(prefixBuffer, 0, 800)` writes all 800 bytes, and the remainder loop reads 0 bytes and exits immediately—correctly handling sub-1 KB payloads without errors.)
- **Common superficial or incorrect answer to avoid:** "It rewinds the network stream with `stream.Position = 0`" — HTTP response network streams (`SocketsHttpHandler`) are forward-only and non-seekable; buffering the prefix into `MemoryStream` is required.

### Q5.6 How are download-size limits enforced across the entire response, including already-read prefix bytes?
- **What the interviewer is testing:** Accounting precision in bounded streaming.
- **Technically correct answer grounded in this repo:** In `ReadBoundedWithClassificationAsync`, the variable `long totalRead` is initialized to the exact number of bytes read during the prefix inspection phase (`totalRead = prefixCount`). Before any subsequent 16 KB chunk is appended, `totalRead += read` is evaluated; if `totalRead > maxAllowedBytes` (`MaxImageBytes = 20 MB`, or `MaxHtmlBytes = 2 MB` for HTML), the `MemoryStream` is immediately disposed and an `ImageIngestionException.ImageTooLarge` exception is thrown. Prefix bytes are never excluded from size accounting, preventing an attacker from sneaking extra bytes past the limit.
- **Relevant file and method:** `Retrieval/DirectImageRetriever.cs` (`ReadBoundedWithClassificationAsync`, lines 215–245).
- **Likely follow-up question:** Why is `maxAllowedBytes` different for HTML (2 MB) versus images (20 MB)? (An HTML webpage containing vehicle metadata and image links should never exceed 2 MB; allocating up to 20 MB for an HTML DOM parse would waste server memory, whereas high-resolution vehicle photographs legitimately require up to 20 MB.)
- **Common superficial or incorrect answer to avoid:** "The remainder read loop starts counting `totalRead` from 0" — that would allow `prefixCount + maxBytes`, creating an off-by-1KB size discrepancy.

### Q5.7 How does the pipeline ensure misleading or missing `Content-Type` headers cannot cause incorrect classification?
- **What the interviewer is testing:** Security against MIME-confusion attacks and CDN misconfigurations.
- **Technically correct answer grounded in this repo:** Header inspection alone is never trusted. In `RetrievalOrchestrator.IsHtmlResponse`, binary image signatures via `ImageValidator.TryMatchBinaryImageSignature` are checked **before** examining the `Content-Type` header:
  - If a server sends a valid JPEG with `Content-Type: text/html` or `application/octet-stream`, the binary magic bytes (`FF D8 FF`) match immediately, returning `false` (direct image). The misleading header is ignored.
  - If a server sends an HTML error page at `/car.jpg` with `Content-Type: image/jpeg`, magic bytes do not match. UTF-8 inspection reveals `<html` or `<!doctype`, returning `true` (HTML response). The misleading header and `.jpg` extension are overridden by actual content.
  - If `Content-Type` is completely omitted (`null`), binary magic bytes or HTML tag detection classify the payload reliably from the 1 KB sample.
- **Relevant file and method:** `Retrieval/RetrievalOrchestrator.cs` (`IsHtmlResponse`), `ImageProcessing/ImageValidator.cs` (`TryMatchBinaryImageSignature`, `IsLikelyTextMarkupOrJson`).
- **Likely follow-up question:** What if a response is pure random garbage or binary video data with no header? (It matches neither an image signature nor HTML markup; `ReadBoundedWithClassificationAsync` throws `UNSUPPORTED_CONTENT_TYPE` early after 1 KB.)
- **Common superficial or incorrect answer to avoid:** "We trust `Content-Type` first and only fall back to sniffing if `Content-Type` is missing."

---

## Category 6: Binary Image Validation (`ImageValidator`)

### Q6.1 Why does `ImageValidator` run FOUR separate checks (magic bytes, header geometry / EOI, `Image.IdentifyAsync`, and `Image.LoadAsync<Rgba32>`)?
- **What the interviewer is testing:** Image security (polyglots, decompression bombs, corrupt bitstreams) vs performance.
- **Technically correct answer grounded in this repo:** Each stage catches an attack or corruption vector the others miss:
  1. **1 KB magic inspection (`InspectSignature` / `TryMatchBinaryImageSignature`):** Never trusts extensions or `Content-Type`; enforces strict whitelist (`JPEG`, `PNG`, `WebP`) while giving explicit error distinction between other image formats (`GIF`, `BMP`, `TIFF`, `ICO`, `AVIF/HEIF` -> `UNSUPPORTED_IMAGE_FORMAT`) and scripts/documents (HTML, SVG, JSON -> `UNSUPPORTED_CONTENT_TYPE`). This signature matching is shared with `DirectImageRetriever` for streaming classification.
  2. **Early structural checks:** JPEG missing `FF D9` End-Of-Image tail fails fast (`HasValidJpegEndOfImage`) as truncated; PNG reads BigEndian `IHDR` width/height at bytes `16..23` to reject huge pixel counts *before* ImageSharp parses compressed chunks.
  3. **`Image.IdentifyAsync` (metadata-only parse):** Checks multi-frame animation (`FrameMetadataCollection.Count > 1` -> rejected as `"Animated"`) and enforces `[100..10000]` width/height and `<= 50,000,000` pixel ceilings *before* allocating RGBA pixel buffers (stopping decompression bombs!).
  4. **Full pixel decode (`Image.LoadAsync<Rgba32>`):** Uses a bounded `MemoryAllocator` (`256 MB` limit, single-threaded `MaxDegreeOfParallelism = 1`) to decompress every pixel, catching truncated scanlines/corrupt entropy data mid-stream that headers alone pass. Then `SHA256.HashDataAsync` hashes the validated stream and resets `Position = 0`.
- **Relevant file and method:** `ImageProcessing/ImageValidator.cs` (`ValidateAsync`, `InspectSignature`, `TryMatchBinaryImageSignature`, `HasValidJpegEndOfImage`).
- **Likely follow-up question:** Why limit `MaxDegreeOfParallelism = 1` on ImageSharp's `Configuration`? (Prevents large decodes from spawning multiple worker threads and spiking CPU/memory under concurrent API requests or bulk imports.)
- **Common superficial or incorrect answer to avoid:** "Calling `Image.IdentifyAsync` is enough to validate an image" — `IdentifyAsync` only reads file headers; corrupt pixel data mid-file would still persist a broken grey/unrenderable image!

### Q6.2 Why does `ImageValidator` allow `GIF` and `BMP` recognition if they are rejected anyway?
- **What the interviewer is testing:** Precision in error taxonomy and UX feedback.
- **Technically correct answer grounded in this repo:** `ErrorCodes` distinguishes `UNSUPPORTED_CONTENT_TYPE` (e.g., an HTML webpage, SVG script, PDF, or JSON returned at a broken URL) from `UNSUPPORTED_IMAGE_FORMAT` (it *is* a valid image, such as GIF, BMP, TIFF, ICO, AVIF/HEIC, or multi-frame animated WebP/PNG, just not in `{Jpeg, Png, WebP}`). Giving callers the exact format name in `UnsupportedImageFormat("AVIF/HEIF")` gives far clearer feedback in both API responses and CSV row errors.
- **Relevant file and method:** `ImageProcessing/ImageValidator.cs` (`InspectSignature`, lines 107–110, 216–263).
- **Likely follow-up question:** What if an admin adds `"Gif"` to `appsettings.json -> ImageValidation:AllowedFormats` without updating `ImageValidator.cs`? (It would still reject GIF because `InspectSignature` hardcodes `throw ImageIngestionException.UnsupportedImageFormat("GIF")`—changing supported formats requires code updates in `ImageFormatKind`, `ImageValidator`, and `LocalFileStorage.AllowedExtensions`.)
- **Common superficial or incorrect answer to avoid:** "Changing `appsettings.json` automatically enables GIF or AVIF ingestion."

---

## Category 7: Storage Safety & Path Traversal (`LocalFileStorage`)

### Q7.1 How does `LocalFileStorage` prevent path traversal and partial/corrupted files on disk?
- **What the interviewer is testing:** Secure filesystem programming.
- **Technically correct answer grounded in this repo:**
  1. **Zero User Input in File Names:** `SaveAsync` accepts only `(Stream image, string fileExtension)` checked against `{ ".jpg", ".png", ".webp" }`, generating `vehicles/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{ext}`. Supplier URLs or CSV vehicle names never become filenames.
  2. **Canonical Boundary Guard (`ResolveSafeFullPath`):** Rejects null/whitespace, `..`, `\`, leading `/`, `\0`, `Path.IsPathRooted`, computes `Path.GetFullPath(Path.Combine(_rootPath, relativeOsPath))`, and verifies `combined.StartsWith(_rootPathWithSeparator, comparison)` (OS-aware case sensitivity).
  3. **Atomic Write Pattern:** Writes bytes to `.{Guid:N}.tmp` using `FileMode.CreateNew` + `FileShare.None`, calls `await fileStream.FlushAsync(cancellationToken)`, closes the write stream, and executes `File.Move(tempPath, fullPath, overwrite: false)`. In `catch`, `TryDeleteTempFile` removes any incomplete `.tmp` file.
- **Relevant file and method:** `Storage/LocalFileStorage.cs` (`SaveAsync`, `ResolveSafeFullPath`, `TryDeleteTempFile`).
- **Likely follow-up question:** Why date-partition keys into `vehicles/{yyyy}/{MM}/...` instead of dumping all images into one directory? (Avoids directory inode contention and filesystem degradation when thousands/millions of images accumulate in a single folder, while keeping `StorageKey` clean and portable across S3/Blob migrations.)
- **Common superficial or incorrect answer to avoid:** "We sanitize `fileName.Replace(\"..\", \"\")`" — naive string replacement fails against `....//` or URL-encoded sequences; full canonical resolution checked against `_rootPathWithSeparator` is the right pattern.

---

## Category 8: Entity Framework Core Schema, Indexing & Constraints

### Q8.1 Why is `VehicleImage.Id` configured as `ValueGeneratedNever()` and why are there unique indexes on BOTH `VehicleId` and `StorageKey`?
- **What the interviewer is testing:** EF Core key generation behavior and relational data integrity.
- **Technically correct answer grounded in this repo:** `ImageIngestionService.IngestAsync` generates `ImageId = Guid.NewGuid()` up front when producing `IngestedImage`, and `VehicleService.CreateVehicleImage` sets `Id = ingested.ImageId`. Marking `.ValueGeneratedNever()` in `VehicleImageConfiguration` tells EF Core we provide the PK client-side rather than waiting for SQL Server to assign/generate it. The two unique indexes guarantee domain invariants at the DB level: `IX_VehicleImages_VehicleId` enforces strict 1:1 cardinality (one image per vehicle), while `IX_VehicleImages_StorageKey` ensures two rows can never accidentally alias and later cross-delete each other's physical disk file.
- **Relevant file and method:** `Data/Configurations/VehicleImageConfiguration.cs` (Lines 18–26), `Services/ImageIngestionService.cs` (Line 82).
- **Likely follow-up question:** How does `IX_VehicleImages_VehicleId` affect `ReplaceImageAsync`? (Inside the transaction, `VehicleService` must remove the old `VehicleImage` and call `SaveChangesAsync` *before* adding and saving the new `VehicleImage`, otherwise EF Core's INSERT can violate the unique index before the DELETE executes!)
- **Common superficial or incorrect answer to avoid:** "We let SQL Server generate the GUID with `NEWSEQUENTIALID()`" — no, `InitialCreate.cs` creates `Id` without default SQL and `.ValueGeneratedNever()`.

### Q8.2 Explain how `ApplicationDbContext` supports both SQL Server and SQLite without breaking EF Core constraints or migrations.
- **What the interviewer is testing:** Dual-provider EF Core subtleties (`Program.cs`, `ApiFactory`, migrations).
- **Technically correct answer grounded in this repo:** `appsettings.json` defaults `DatabaseProvider` to `"SqlServer"`. In `Program.cs`, if `DatabaseProvider == "Sqlite"`, DI configures `UseSqlite(connectionString)` and startup runs `db.Database.EnsureCreated()` instead of SQL Server's `Migrations/20261116094554_InitialCreate.cs` (which uses SQL Server types like `uniqueidentifier`, `nvarchar`, and `SYSUTCDATETIME()`). Integration tests (`ApiFactory.cs`) and local non-Windows dev runs leverage this SQLite `EnsureCreated()` mode cleanly while keeping SQL Server as the production migration target. Note in `VehicleConfiguration`/`VehicleImageConfiguration`: SQL Server's `SYSUTCDATETIME()` default SQL is explicitly configured for `CreatedAt`/`UpdatedAt`, though application code also assigns `DateTime.UtcNow` explicitly in C#.
- **Relevant file and method:** `Program.cs` (Lines 56–70, 157–162), `Data/Configurations/VehicleConfiguration.cs`, `Migrations/20261116094554_InitialCreate.cs`.
- **Likely follow-up question:** Why don't we run `db.Database.EnsureCreated()` unconditionally for SQL Server too? (`EnsureCreated()` bypasses `__EFMigrationsHistory` completely and prevents `dotnet ef database update` migrations from working cleanly later.)
- **Common superficial or incorrect answer to avoid:** "Integration tests run `db.Database.Migrate()` on SQLite" — wrong! `InitialCreate.cs` contains SQL Server-specific column types, which is why SQLite uses `EnsureCreated()`!

---

## Category 9: Transactional Persistence, Compensation & Safe Image Replacement

### Q9.1 Why does `VehicleService.CreateAsync` run `IngestAsync` BEFORE touching the database instead of opening a `DbTransaction` first?
- **What the interviewer is testing:** Database connection pool scalability and distributed transaction patterns.
- **Technically correct answer grounded in this repo:** Holding a SQL Server transaction open while downloading up to 20 MB over potentially slow external URLs (with up to 5 redirects and 3 retries = 15–45 seconds!) would tie up DB connections, cause lock/latch contention, and exhaust the ADO.NET connection pool under load. Instead, `CreateAsync` performs all HTTP retrieval, validation, and disk writing first, and only opens a short millisecond DB save (`_dbContext.SaveChangesAsync`). If DB save fails or cancels, `CompensateFileDeleteAsync` deletes the newly saved disk file.
- **Relevant file and method:** `Services/VehicleService.cs` (`CreateAsync`, `CompensateFileDeleteAsync`).
- **Likely follow-up question:** What happens if the process crashes (`SIGKILL` / power loss) right between `SaveAsync` and `SaveChangesAsync`? (That creates an orphan file on disk; fixing hard crash orphans requires a periodic storage reconciliation sweep checking disk keys against `VehicleImages.StorageKey`, noted right in the `"Orphaned file alert"` log message!)
- **Common superficial or incorrect answer to avoid:** "Using `TransactionScope` rolls back both SQL Server and `LocalFileStorage` together" — NTFS/ext4 filesystem operations do not participate in SQL Server transactions.

### Q9.2 Why does `CompensateFileDeleteAsync` pass `CancellationToken.None` instead of the incoming `cancellationToken`?
- **What the interviewer is testing:** Subtle async cancellation bugs (`VehicleServiceCancellationTests`).
- **Technically correct answer grounded in this repo:** When a user closes their browser tab or their client timeout fires during `SaveChangesAsync`, `cancellationToken` (`HttpContext.RequestAborted`) is **already signaled as canceled**. If `CompensateFileDeleteAsync` passed that canceled `cancellationToken` to `_imageStorage.DeleteAsync`, `LocalFileStorage.DeleteAsync` calls `cancellationToken.ThrowIfCancellationRequested()` on its very first line and aborts the cleanup—leaving an orphaned image file behind on disk! Using `CancellationToken.None` guarantees best-effort disk cleanup still runs even after client disconnect (verified explicitly by `VehicleServiceCancellationTests`).
- **Relevant file and method:** `Services/VehicleService.cs` (`CreateAsync` Line 65, `CompensateFileDeleteAsync` Line 327), `Storage/LocalFileStorage.cs` (Line 137).
- **Likely follow-up question:** Why does `ReplaceImageAsync` track `bool commitAttempted` and skip `CompensateFileDeleteAsync` when `commitAttempted == true`? (Once `CommitAsync(cancellationToken)` starts, an `OperationCanceledException` or network blip between app and SQL Server leaves commit outcome uncertain: SQL Server might actually have committed the new `VehicleImage` row! Deleting the new storage file in that ambiguous window would leave the DB pointing at a missing file—better to retain the file than break an active record.)
- **Common superficial or incorrect answer to avoid:** "Always delete the new file in `catch`" — dangerous once `CommitAsync` has been initiated!

### Q9.3 Walk through `ReplaceImageAsync` step by step. How does it guarantee zero broken images on failure AND detect concurrent races (`409 Conflict`)?
- **What the interviewer is testing:** Concurrency control, non-destructive updates, and cache busting.
- **Technically correct answer grounded in this repo:**
  1. `AsNoTracking()` lookup verifies vehicle exists (`404 VEHICLE_NOT_FOUND` before any outbound network call) and snapshots `previousImageId` and `previousStorageKey`.
  2. `_ingestionService.IngestAsync` downloads, validates, and saves the replacement image under a *new* GUID `ImageId` and `StorageKey`. If retrieval/validation fails (`422`), the DB row and old file remain 100% untouched.
  3. Starts explicit DB transaction (`BeginTransactionAsync`), loads tracked `Vehicle` + `Image`. If missing -> rollback, delete new file, throw `404`.
  4. **Optimistic Race Check:** Checks `if (vehicle.Image?.Id != previousImageId)`. If another concurrent request replaced the image first, rolls back, deletes the new file, and throws `ConcurrentUpdateException` (`409 CONCURRENT_UPDATE`, `retryable: true`).
  5. Removes old `VehicleImage` -> `SaveChangesAsync` (clears unique `VehicleId` index), sets `vehicle.UpdatedAt`, adds new `VehicleImage` -> `SaveChangesAsync` -> `commitAttempted = true` -> `CommitAsync`.
  6. Only **after** transaction commits does it delete `previousStorageKey` with `CancellationToken.None` (logging orphan warning if disk delete fails without failing the `200 OK` response).
- **Relevant file and method:** `Services/VehicleService.cs` (`ReplaceImageAsync`).
- **Likely follow-up question:** Why remove the old `VehicleImage` row and insert a new row with a new `ImageId` instead of updating `StorageKey` on the existing row? (Updating in place keeps the same `/api/images/{oldId}` URL, which clients and CDNs cached for 1 year with `immutable`!)
- **Common superficial or incorrect answer to avoid:** "We overwrite the old file at `previousStorageKey` on disk" — breaks atomicity on failed uploads/DB rollback AND poisons `immutable` browser caches.

---

## Category 10: Internal Image Delivery & Caching

### Q10.1 Why can `ImagesController` return `Cache-Control: public, max-age=31536000, immutable` safely, and why does it send `X-Content-Type-Options: nosniff`?
- **What the interviewer is testing:** HTTP caching architecture and browser security headers.
- **Technically correct answer grounded in this repo:** Sending `immutable` for 1 year (`31,536,000` seconds) is safe strictly because stored blobs are content-immutable by `ImageId`: `SaveAsync` creates unique GUID keys and `ReplaceImageAsync` always mints a brand-new `ImageId` rather than mutating bytes at an existing ID. Sending `X-Content-Type-Options: nosniff` prevents legacy browser MIME-sniffing attacks by forcing browsers to honor the backend's validated `ContentType` (`image/jpeg`, `image/png`, `image/webp`). Additionally, `ImageDeliveryService.BuildDownloadFileName` sanitizes `Make`/`Model` via `SanitizeSegment` before passing `result.DownloadFileName` to `ControllerBase.File(..., fileDownloadName)` for `/download`.
- **Relevant file and method:** `Controllers/ImagesController.cs` (`Get`, `Download`), `Services/ImageDeliveryService.cs` (`OpenImageAsync`, `BuildDownloadFileName`, `SanitizeSegment`).
- **Likely follow-up question:** What happens if the DB row exists in `VehicleImages`, but someone manually deleted the `.jpg` file on disk? (`LocalFileStorage.OpenReadAsync` throws `FileNotFoundException`; `ImageDeliveryService` catches it, logs a `"Data integrity error"` at Error level, and throws `ResourceNotFoundException.Image(imageId)` returning clean `404`.)
- **Common superficial or incorrect answer to avoid:** "`immutable` means the browser caches for 1 year and checks `ETag` on every load" — no! `immutable` tells the browser not even to revalidate on reload during `max-age`, which is why minting a new `ImageId` on replacement is mandatory!

---

## Category 11: Global Exception Handling & Error Taxonomy

### Q11.1 How does `GlobalExceptionHandler` prevent leaking internal details while still giving actionable client errors (and `499` on cancellation)?
- **What the interviewer is testing:** Centralized RFC 7807 error shaping and information-disclosure security.
- **Technically correct answer grounded in this repo:** `GlobalExceptionHandler.TryHandleAsync` inspects every uncaught exception:
  - Client disconnect (`OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested`) sets status `499` quietly without polluting Error logs or writing to a dead socket.
  - `ImageIngestionException` already carries sanitized client-safe messages created by factory methods, `HttpStatusCode` (`422` or `500`), `IsTransient` -> `retryable`, and optional `RetryAfter` -> both JSON `retryAfterSeconds` and HTTP `Retry-After` response header.
  - Unknown exceptions (`_` catch-all) log full stack traces internally via `_logger.LogError`, while returning generic `"An unexpected internal error occurred while processing the request."` with `INTERNAL_ERROR`, `status: 500`, and `traceId` (`Activity.Current?.Id ?? httpContext.TraceIdentifier`) so callers can report incidents without seeing SQL/OS paths.
- **Relevant file and method:** `Exceptions/GlobalExceptionHandler.cs` (`TryHandleAsync`, `MapIngestionException`), `DTOs/Images/ImageFailureResponse.cs`.
- **Likely follow-up question:** Why does `ImageIngestionException.HttpForbidden` return HTTP `422` instead of forwarding HTTP `403` to our React frontend? (Because **our** API did not forbid the React user—the *external supplier* forbade our server! Returning `403` to the frontend would falsely imply our dashboard session lacked permission, whereas `422 Unprocessable Entity` with `code: "HTTP_FORBIDDEN"` accurately states why the submitted URL failed ingestion.)
- **Common superficial or incorrect answer to avoid:** "Forward `UpstreamStatusCode` directly as our API response code" — confuses upstream supplier failures (like supplier `401`/`403`/`404`) with client auth/route errors on our own API!

---

## Category 12: CSV Bulk Vehicle Ingestion (`feature/csv-bulk-import`)

### Q12.1 Why use `CsvHelper` (`CsvParser`) instead of `string.Split(',')`, and how does `VehicleImportService` handle quoted commas, extra/missing columns, and blank separator rows?
- **What the interviewer is testing:** RFC 4180 CSV edge cases vs brittle naive parsing.
- **Technically correct answer grounded in this repo:** Vehicle variants and URLs commonly contain commas inside quotes (for example, `BMW,M3,"Competition, xDrive",2026,...`, demonstrated in `BulkImportForm`'s sample CSV). Naive `line.Split(',')` corrupts field count and shifts columns. `VehicleImportService.ParseAsync` uses `CsvHelper 33.1.0` (`CsvParser` + `CsvConfiguration(CultureInfo.InvariantCulture)` with `HasHeaderRecord = true`, `IgnoreBlankLines = true`, `TrimOptions = TrimOptions.Trim`). It handles UTF-8 BOMs (`detectEncodingFromByteOrderMarks: true`), normalizes header spacing/casing in `BuildColumnMap`, rejects duplicate headers, skips all-comma empty rows via `record.All(string.IsNullOrWhiteSpace)`, catches `CsvHelperException` (unterminated quotes) as `400 VALIDATION_FAILED`, and checks per-row `record.Length != headerFieldCount` as an isolated row validation failure.
- **Relevant file and method:** `Services/VehicleImportService.cs` (`ParseAsync`, `BuildColumnMap`, `BuildRow`).
- **Likely follow-up question:** Why did you configure `DetectDelimiter = false`? (To require strict RFC 4180 comma separation rather than guessing semicolons/tabs when headers or quoted URLs contain other punctuation.)
- **Common superficial or incorrect answer to avoid:** "Just split lines on `\n` and fields on `,`" — fails on `"Competition, xDrive"`, `\r\n` vs `\n`, UTF-8 BOMs, and embedded newlines inside quotes.

### Q12.2 Explain why `VehicleImportService` validates ALL rows upfront before processing, then executes rows sequentially inside per-row `AsyncServiceScope`s.
- **What the interviewer is testing:** Transactional isolation, EF Core change tracker poisoning, resource exhaustion, and partial success semantics.
- **Technically correct answer grounded in this repo:**
  1. **Two-Phase Whole-File Safety:** Phase 1 reads the file cleanly (`ValidateFile` + `ParseAsync`) checking `.csv` extension, size `<= 1 MB`, required columns (`Make`, `Model`, `Year`, `ImageUrl`), record cap `<= 50`, and row structure *before* making a single network request or SQL insert. So an invalid file or row #51 fails fast with `400` and zero partial side effects. Meanwhile, row-level field errors (`int.TryParse` Year failure or `Validator.TryValidateObject(CreateVehicleRequest)` failure) are recorded onto `CsvRow.ValidationError` so other valid rows still import (`200 OK` partial success).
  2. **Per-Row `AsyncServiceScope`:** In Phase 2 (`ProcessRowAsync`), `VehicleImportService` creates a fresh `AsyncServiceScope` via `IServiceScopeFactory.CreateAsyncScope()` for every valid row. Because `ApplicationDbContext` is scoped, sharing one `DbContext` across 50 rows risks catastrophic change-tracker poisoning: if Row #5 throws during `SaveChangesAsync`, its unsaved `Vehicle` stays tracked as `Added`, causing Rows #6–#50 to fail every subsequent `SaveChangesAsync`. A fresh scope guarantees a pristine `DbContext` per row.
  3. **Sequential vs Parallel (`Task.WhenAll`):** Sequential execution caps outbound sockets, protects the 256 MB ImageSharp memory pool, avoids spiking SQL Server/SQLite locks, and respects per-row ordering cleanly for `<= 50` records.
- **Relevant file and method:** `Services/VehicleImportService.cs` (`ImportCsvAsync`, `BuildRow`, `ProcessRowAsync`).
- **Likely follow-up question:** Which exceptions inside `ProcessRowAsync` become a failed `VehicleImportRowResult`, and which abort the entire import? (`ValidationError` upfront + caught `ImageIngestionException` produce failed rows while continuing; `OperationCanceledException` propagates HTTP `499`; any unexpected `Exception` is logged at Error level and rethrown as `500 INTERNAL_ERROR`.)
- **Common superficial or incorrect answer to avoid:** "Inject `IVehicleService` directly into `VehicleImportService` and run `Task.WhenAll(rows.Select(...))`" — calling `Task.WhenAll` on a single scoped `IVehicleService`/`DbContext` causes immediate EF Core concurrency exceptions (`A second operation was started on this context instance`) and spikes 50 simultaneous 20 MB image buffers in RAM!

### Q12.3 Why are request size limits enforced in TWO places (`VehiclesController.Import` attributes AND `BulkImportOptions.MaxFileBytes`)?
- **What the interviewer is testing:** Defense-in-depth between Kestrel multipart buffering and application config.
- **Technically correct answer grounded in this repo:** `VehiclesController.Import` applies `[RequestSizeLimit(ImportRequestSizeLimit)]` and `[RequestFormLimits(MultipartBodyLengthLimit = ImportRequestSizeLimit)]` (`AbsoluteMaxFileBytes + 64 KB` = `5 MB + 64 KB`) so Kestrel rejects huge multi-gigabyte uploads at the HTTP framing layer *before* buffering streams to disk/memory. Inside `VehicleImportService.ValidateFile`, `_options.MaxFileBytes` enforces the configurable runtime policy (default `1 MB`, `[Range(1024, 5MB)]`) yielding a clean structured `400 VALIDATION_FAILED` JSON response (`The uploaded CSV file exceeds the maximum allowed size of 1024 KB.`). Note that rejecting via Kestrel's `[RequestSizeLimit]` stops gigantic payloads early (returning framework 413/400), while `MaxFileBytes` covers the normal `[1 MB..5 MB]` operational window cleanly with RFC 7807 errors.
- **Relevant file and method:** `Controllers/VehiclesController.cs` (`Import`, Lines 15, 43–56), `Configuration/BulkImportOptions.cs`, `Services/VehicleImportService.cs` (`ValidateFile`).
- **Likely follow-up question:** Why does `VehicleImportResponse` return `ImageUrl` (`/api/images/{id}`) on success and `ErrorCode`/`ErrorMessage` on failure, without echoing the CSV's external supplier URL? (Consistency with API hygiene: avoid reflecting arbitrary unverified URLs back in API responses, keeping all frontend links anchored to internal delivery endpoints.)
- **Common superficial or incorrect answer to avoid:** "Only check `file.Length` inside the service" — without Kestrel `RequestSizeLimit`/`MultipartBodyLengthLimit`, an attacker can still force the server to parse a 500 MB multipart body before controller code runs!

---

## Category 13: React + Vite Frontend Architecture & State

### Q13.1 How does the React frontend communicate with the ASP.NET Core backend in development vs separate hosting, and how are errors normalized?
- **What the interviewer is testing:** Vite proxy vs CORS mechanics and typed error handling in React.
- **Technically correct answer grounded in this repo:** In local development, `vehicleApi.js` uses `API_BASE = import.meta.env.VITE_API_BASE_URL || ''`, so calls like `/api/vehicles` hit Vite (`:5173`) same-origin and `vite.config.js` proxies `/api` and `/health` to `VITE_API_TARGET` (`http://localhost:5080`). When deployed separately (setting `VITE_API_BASE_URL`), cross-origin browser requests are allowed by `Program.cs`'s `DashboardCors` policy. Every call passes through `parseResponse` / `request`: JSON failures (RFC 7807 `ImageFailureResponse` or ASP.NET `errors` dictionaries) and `fetch` network rejections are normalized into `ApiError` carrying `code`, `title`, `detail`, `status`, `traceId`, `retryable`, and `retryAfterSeconds`. Note that `importVehiclesCsv(file)` constructs `new FormData()`, appends `'file'`, and intentionally omits a manual `'Content-Type'` header so the browser automatically sets `multipart/form-data; boundary=...`.
- **Relevant file and method:** `frontend/vehicle-image-dashboard/vite.config.js`, `src/services/vehicleApi.js` (`parseResponse`, `request`, `importVehiclesCsv`, `resolveInternalImageUrl`).
- **Likely follow-up question:** What happens if someone manually sets `'Content-Type': 'multipart/form-data'` in `fetch` options when sending `FormData`? (It omits the `boundary=...` parameter, causing ASP.NET Core multipart form binding to fail with `400`/`415`!)
- **Common superficial or incorrect answer to avoid:** "Always pass `'Content-Type': 'multipart/form-data'` on file uploads."

### Q13.2 Why does `DashboardPage.jsx` use `key={`${vehicle.id}-${vehicle.imageId}`}` on `<VehicleCard>`, and how do single create, bulk import, and replacement refresh the catalog?
- **What the interviewer is testing:** React reconciliation/key identity and state invalidation.
- **Technically correct answer grounded in this repo:** `VehicleCard` holds internal local state (`imageLoadFailed`, `showDetails`, `details`, `showReplaceForm`, `replacementSuccess`). If `key` were only `vehicle.id`, replacing a vehicle's image (`PUT /api/vehicles/{id}/image`) would reuse the same mounted component instance, leaving stale `details` (showing old `imageId` and dimensions) or a sticky `imageLoadFailed = true`. Using composite `key={`${vehicle.id}-${vehicle.imageId}`}` forces React to remount the card with fresh initial state whenever `imageId` changes, while `<img>` immediately loads the new `/api/images/{newImageId}` URL with `loading="lazy"`. Meanwhile, `VehicleForm` (`onVehicleCreated`) and `BulkImportForm` (`onImportCompleted`) both invoke `handleCatalogRefreshFromPageOne`, resetting `page` to `1` (or refetching page 1) because the backend orders newest vehicles first (`OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)`).
- **Relevant file and method:** `src/pages/DashboardPage.jsx` (`handleCatalogRefreshFromPageOne`, Lines 95–101), `src/components/VehicleCard.jsx`, `src/components/BulkImportForm.jsx`.
- **Likely follow-up question:** Wait—in `VehicleCard.jsx` `handleReplaced()`, after replacing, it sets `setReplacementSuccess(true)` AND calls `onUpdated()` (`fetchCatalog(page)`). Because the parent updates `vehicle.imageId`, the composite key remounts `VehicleCard`—what happens to `replacementSuccess`? (Good catch inspecting the live code! Remounting resets `replacementSuccess` back to `false` once `fetchCatalog` returns the new `imageId`, so the user sees the freshly replaced image immediately in the updated card!)
- **Common superficial or incorrect answer to avoid:** "Using array index or only `vehicle.id` is fine for cards that swap images."

---

## Category 14: Testing Strategy, Tradeoffs & Production Evolution

### Q14.1 How does `IntegrationTests` verify URL policy and cancellation safety without making flaky external internet calls?
- **What the interviewer is testing:** Test isolation architecture (`WebApplicationFactory`, Kestrel test doubles, EF interceptors).
- **Technically correct answer grounded in this repo:** `IntegrationTests` runs zero live internet requests:
  - `MockSupplierServer` starts a real local Kestrel server on `127.0.0.1:<port>` serving fixture HTML/images, oversized chunked streams, redirect chains, and flaky `429`/`503` responses.
  - `ApiFactory` overrides `DatabaseProvider` to an isolated temp SQLite DB and temp storage folder, appends the mock server port to `Retrieval:AllowedPorts`, and uses `PostConfigure<HttpClientFactoryOptions>(DirectImageRetriever.HttpClientName, ...)` with a test-only `SocketsHttpHandler` (`UseProxy = false` and `ConnectCallback` routing `supplier.test` to `127.0.0.1`) so tests do not require weakening production `UrlValidator` loopback blocking.
  - `UrlValidationTests` tests the unmodified production `UrlValidator` end-to-end against `localhost`, `127.0.0.1`, `[::1]`, `file:///`, and `ftp://` targets, verifying zero requests reach `MockSupplierServer`.
  - `VehicleServiceCancellationTests` registers a custom `CancelOnCommandInterceptor` (`DbCommandInterceptor`) to trigger cancellation mid-`INSERT`, proving `CompensateFileDeleteAsync` deletes the disk file even when `CancellationToken` is canceled.
- **Relevant file and method:** `tests/IntegrationTests/ApiFactory.cs`, `MockSupplierServer.cs`, `UrlValidationTests.cs`, `VehicleServiceCancellationTests.cs`.
- **Likely follow-up question:** How were CSV bulk import and Google Drive features verified, and what test gap remains? (Unit + integration tests cover the core pipeline, while CSV bulk import and Google Drive retrieval were verified via live API and UI checks; dedicated automated xUnit tests for `VehicleImportService` and `GoogleDriveImageRetriever` have not yet been added.)
- **Common superficial or incorrect answer to avoid:** "Integration tests mock `HttpClient` via `HttpMessageHandler`" — using real Kestrel + `SocketsHttpHandler` exercises real HTTP parsing, chunked streaming, redirects, and cancellation tokens end-to-end.

### Q14.2 If this system needed to scale to 10,000-row CSV imports across multiple API instances behind a load balancer, what exact components would you change?
- **What the interviewer is testing:** System design evolution grounded cleanly in existing abstractions.
- **Technically correct answer grounded in this repo:** The codebase already isolated the right boundaries to evolve without rewriting core logic:
  1. **Storage (`IImageStorage`):** Swap `LocalFileStorage` singleton for an `S3ImageStorage` / `AzureBlobImageStorage` implementation using the exact same `vehicles/{yyyy}/{MM}/{guid}{ext}` key scheme so load-balanced API pods share object storage cleanly.
  2. **Async Bulk Processing:** Keep `POST /api/vehicles/import` validating CSV structure up front, store an `ImportJob` + `ImportJobRow` table in SQL Server, return `202 Accepted` with a job status URL (`/api/imports/{jobId}`), and push row work items to a message queue (`Service Bus` / `RabbitMQ` / `Channel<T>`). Workers still invoke `IImageIngestionService.IngestAsync` + `IVehicleService` safely with bounded concurrency (`SemaphoreSlim` / worker prefetch count).
  3. **Authentication, Rate Limiting & Network Egress:** Configure ASP.NET Core JWT Bearer / API Key auth before the already-placed `app.UseAuthorization()`, add ASP.NET Core rate limiting middleware, and add VPC egress firewall rules or full CIDR/DNS network isolation before opening the service to untrusted external URLs.
- **Relevant file and method:** `Storage/IImageStorage.cs`, `Services/ImageIngestionService.cs`, `Services/VehicleImportService.cs`, `Program.cs`.
- **Likely follow-up question:** Why would you add VPC/network egress controls in Kubernetes or cloud worker pods? (Cloud pods sit inside VPCs with access to private internal services and instance metadata endpoints; if arbitrary user-submitted URLs are accepted from untrusted tenants, basic `UrlValidator` rules should be paired with strict network egress controls.)
- **Common superficial or incorrect answer to avoid:** "Just raise `BulkImportOptions.MaxRecords` to 10,000 and add `Parallel.ForEachAsync`" — hitting HTTP request timeouts, exhausting EF Core connections, and blowing past container RAM limits.
