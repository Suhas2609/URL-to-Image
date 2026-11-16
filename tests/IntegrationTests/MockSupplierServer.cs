using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace VehicleImage.IntegrationTests;

public sealed class MockSupplierServer : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly ConcurrentDictionary<string, int> _requestCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<string> _requestMethods = [];
    private readonly HashSet<string> _disabledRoutes = new(StringComparer.OrdinalIgnoreCase);

    public Uri BaseUri { get; private set; } = null!;
    public int Port => BaseUri.Port;

    public byte[] ValidJpegBytes { get; private set; } = null!;
    public byte[] ValidPngBytes { get; private set; } = null!;
    public byte[] ValidWebPBytes { get; private set; } = null!;
    public byte[] CorruptedJpegBytes { get; private set; } = null!;
    public byte[] UnsupportedGifBytes { get; private set; } = null!;
    public byte[] UnsupportedBmpBytes { get; private set; } = null!;
    public byte[] TooSmallPngBytes { get; private set; } = null!;
    public byte[] DecompressionBombPngBytes { get; private set; } = null!;

    public static async Task<MockSupplierServer> StartAsync()
    {
        var server = new MockSupplierServer();
        await server.InitializeAsync();
        return server;
    }

    public int GetRequestCount(string path)
        => _requestCounts.TryGetValue(path, out int count) ? count : 0;

    public int TotalRequests => _requestCounts.Values.Sum();

    public IReadOnlyCollection<string> RequestMethods => _requestMethods.ToArray();

    public void DisableRoute(string path) => _disabledRoutes.Add(path);

    public void ResetCounters()
    {
        _requestCounts.Clear();
        _requestMethods.Clear();
    }

    public string Url(string relativePath)
    {
        string clean = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        // Use a realistic public-looking hostname "supplier.test" that resolves to 127.0.0.1 in our test resolver
        return $"http://supplier.test:{Port}{clean}";
    }

    private async Task InitializeAsync()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        await GenerateFixtureBytesAsync();

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            string path = context.Request.Path.Value ?? "/";
            _requestMethods.Add(context.Request.Method);
            _requestCounts.AddOrUpdate(path, 1, (_, count) => count + 1);

            if (_disabledRoutes.Contains(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });

        // 1. Image fixtures
        app.MapGet("/fixtures/images/valid-car.jpg", () => Results.Bytes(ValidJpegBytes, "image/jpeg"));
        app.MapGet("/fixtures/images/valid-car.png", () => Results.Bytes(ValidPngBytes, "image/png"));
        app.MapGet("/fixtures/images/valid-car.webp", () => Results.Bytes(ValidWebPBytes, "image/webp"));
        app.MapGet("/fixtures/images/corrupted.jpg", () => Results.Bytes(CorruptedJpegBytes, "image/jpeg"));
        app.MapGet("/fixtures/images/unsupported.gif", () => Results.Bytes(UnsupportedGifBytes, "image/gif"));
        app.MapGet("/fixtures/images/unsupported.bmp", () => Results.Bytes(UnsupportedBmpBytes, "image/bmp"));
        app.MapGet("/fixtures/images/too-small.png", () => Results.Bytes(TooSmallPngBytes, "image/png"));
        app.MapGet("/fixtures/images/decompression-bomb.png", () => Results.Bytes(DecompressionBombPngBytes, "image/png"));
        app.MapGet("/fixtures/images/vector.svg", () => Results.Content(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"300\"><rect width=\"100%\" height=\"100%\" fill=\"red\"/></svg>",
            "image/svg+xml"));
        app.MapGet("/fixtures/images/html-masquerade.jpg", () => Results.Content(
            "<!DOCTYPE html><html><body><h1>Hotlink protection error</h1></body></html>",
            "text/html"));

        // 2. Redirect endpoints
        app.MapGet("/redirect/chain-start", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            ctx.Response.Headers.Location = "/redirect/chain-mid";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/chain-mid", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = $"http://supplier.test:{Port}/fixtures/images/valid-car.jpg";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/to-private", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "http://169.254.169.254/latest/meta-data/";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/to-localhost", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = $"http://localhost:{Port}/fixtures/images/valid-car.jpg";
            return Task.CompletedTask;
        });
        app.MapGet("/redirect/loop/{step:int}", (int step, HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = $"/redirect/loop/{step + 1}";
            return Task.CompletedTask;
        });

        // 3. Upstream HTTP status & flaky retry endpoints
        app.MapGet("/status/404", () => Results.StatusCode(StatusCodes.Status404NotFound));
        app.MapGet("/status/401", () => Results.StatusCode(StatusCodes.Status401Unauthorized));
        app.MapGet("/status/403", () => Results.StatusCode(StatusCodes.Status403Forbidden));
        app.MapGet("/status/429-long", (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.Response.Headers.RetryAfter = "60";
            return Task.CompletedTask;
        });
        app.MapGet("/status/500", () => Results.StatusCode(StatusCodes.Status500InternalServerError));
        app.MapGet("/flaky/transient-503", (HttpContext ctx) =>
        {
            int count = GetRequestCount("/flaky/transient-503");
            if (count == 1)
            {
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Task.CompletedTask;
            }
            return Results.Bytes(ValidJpegBytes, "image/jpeg").ExecuteAsync(ctx);
        });

        // 4. Streaming limit & timeout endpoints
        app.MapGet("/stream/oversize-no-length", async (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "image/jpeg";
            byte[] block = new byte[32 * 1024];
            block[0] = 0xFF;
            block[1] = 0xD8;
            block[2] = 0xFF;

            for (int i = 0; i < 64; i++) // 2 MB total (test configures MaxDownloadBytes = 512 KB)
            {
                await ctx.Response.Body.WriteAsync(block, ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
        });

        app.MapGet("/stream/oversize-misleading-length", async (HttpContext ctx) =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "image/jpeg";
            // Claim a tiny Content-Length or chunked body while sending > cap
            byte[] payload = new byte[800 * 1024]; // 800 KB
            payload[0] = 0xFF;
            payload[1] = 0xD8;
            payload[2] = 0xFF;
            await ctx.Response.Body.WriteAsync(payload, ctx.RequestAborted);
        });

        app.MapGet("/slow/timeout", async (HttpContext ctx) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ctx.RequestAborted);
            await Results.Bytes(ValidJpegBytes, "image/jpeg").ExecuteAsync(ctx);
        });

        // 5. HTML fixture endpoints
        string htmlDir = ResolveFixturesHtmlDir();
        app.MapGet("/fixtures/html/{name}", async (string name, HttpContext ctx) =>
        {
            if (string.Equals(name, "charset-iso.html", StringComparison.OrdinalIgnoreCase))
            {
                string isoHtml = $"<html><head><meta charset=\"iso-8859-1\"></head><body><h1>Citroën</h1><img src=\"/fixtures/images/valid-car.jpg\" width=\"640\" height=\"400\" alt=\"Citroën Coupé\"/></body></html>";
                byte[] isoBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(isoHtml);
                ctx.Response.ContentType = "text/html; charset=iso-8859-1";
                await ctx.Response.Body.WriteAsync(isoBytes);
                return;
            }

            string filePath = Path.Combine(htmlDir, name);
            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            string html = await File.ReadAllTextAsync(filePath);
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(html);
        });

        await app.StartAsync();
        _app = app;

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
        BaseUri = new Uri(addresses.First());
    }

    private async Task GenerateFixtureBytesAsync()
    {
        ValidJpegBytes = await CreateImageBytesAsync(640, 400, "jpeg");
        ValidPngBytes = await CreateImageBytesAsync(640, 400, "png");
        ValidWebPBytes = await CreateImageBytesAsync(640, 400, "webp");
        CorruptedJpegBytes = ValidJpegBytes[..(ValidJpegBytes.Length / 2)];
        UnsupportedGifBytes = await CreateImageBytesAsync(300, 200, "gif");
        UnsupportedBmpBytes = await CreateImageBytesAsync(300, 200, "bmp");
        TooSmallPngBytes = await CreateImageBytesAsync(60, 60, "png");

        byte[] baseBomb = (byte[])ValidPngBytes.Clone();
        baseBomb[16] = 0x00;
        baseBomb[17] = 0x00;
        baseBomb[18] = 0x3A;
        baseBomb[19] = 0x98; // 15000
        baseBomb[20] = 0x00;
        baseBomb[21] = 0x00;
        baseBomb[22] = 0x3A;
        baseBomb[23] = 0x98; // 15000
        DecompressionBombPngBytes = baseBomb;

        // Also persist physical fixture files under Fixtures/Images for inspection
        string imagesDir = Path.Combine(ResolveFixturesRootDir(), "Images");
        Directory.CreateDirectory(imagesDir);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "valid-car.jpg"), ValidJpegBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "valid-car.png"), ValidPngBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "valid-car.webp"), ValidWebPBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "corrupted.jpg"), CorruptedJpegBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "unsupported.gif"), UnsupportedGifBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "unsupported.bmp"), UnsupportedBmpBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "too-small.png"), TooSmallPngBytes);
        await File.WriteAllBytesAsync(Path.Combine(imagesDir, "decompression-bomb.png"), DecompressionBombPngBytes);
        await File.WriteAllTextAsync(Path.Combine(imagesDir, "vector.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"300\"/>");
        await File.WriteAllTextAsync(Path.Combine(imagesDir, "html-masquerade.jpg"), "<!DOCTYPE html><html><body>Not an image</body></html>");
    }

    private static async Task<byte[]> CreateImageBytesAsync(int width, int height, string format)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)((x * 5) % 256), (byte)((y * 9) % 256), 180, 255);
            }
        }

        using var ms = new MemoryStream();
        switch (format)
        {
            case "jpeg":
                await image.SaveAsJpegAsync(ms);
                break;
            case "png":
                await image.SaveAsPngAsync(ms);
                break;
            case "webp":
                await image.SaveAsWebpAsync(ms);
                break;
            case "gif":
                await image.SaveAsGifAsync(ms);
                break;
            case "bmp":
                await image.SaveAsBmpAsync(ms);
                break;
        }
        return ms.ToArray();
    }

    private static string ResolveFixturesHtmlDir() => Path.Combine(ResolveFixturesRootDir(), "Html");

    private static string ResolveFixturesRootDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "VehicleImage.IntegrationTests.csproj")))
        {
            dir = dir.Parent;
        }
        string projectRoot = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(projectRoot, "Fixtures");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
