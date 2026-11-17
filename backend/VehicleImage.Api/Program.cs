using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Data;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Retrieval;
using VehicleImage.Api.Security;
using VehicleImage.Api.Services;
using VehicleImage.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Bind and validate configuration options on startup
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .PostConfigure(options =>
    {
        if (!string.IsNullOrWhiteSpace(options.RootPath) && !Path.IsPathRooted(options.RootPath))
        {
            options.RootPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, options.RootPath));
        }
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RetrievalOptions>()
    .Bind(builder.Configuration.GetSection(RetrievalOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ImageValidationOptions>()
    .Bind(builder.Configuration.GetSection(ImageValidationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<HtmlExtractionOptions>()
    .Bind(builder.Configuration.GetSection(HtmlExtractionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RetryOptions>()
    .Bind(builder.Configuration.GetSection(RetryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 2. Database registration (SQL Server primary; supports Sqlite provider when configured for local non-Windows/test runs)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// 3. Controllers, Validation error shaping (400 VALIDATION_FAILED), ProblemDetails, and GlobalExceptionHandler
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            string detail = string.Join("; ", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m)));

            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "One or more request validation errors occurred.";
            }

            string traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var failure = new ImageFailureResponse(
                Code: ErrorCodes.ValidationFailed,
                Title: "Validation Failed",
                Detail: detail,
                Status: StatusCodes.Status400BadRequest,
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null,
                StatusType: "https://tools.ietf.org/html/rfc9110#section-15.5.1");

            return new BadRequestObjectResult(failure)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 4. Security singletons and SSRF-safe HttpClient
builder.Services.AddSingleton<DestinationValidator>();
builder.Services.AddSingleton<UrlValidator>();
builder.Services.AddSingleton<ImageValidator>();
builder.Services.AddSingleton<IImageStorage, LocalFileStorage>();

builder.Services.AddHttpClient(SsrfGuardHttpHandler.ClientName, client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan; // Enforced per-request by DirectImageRetriever
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var destinationValidator = sp.GetRequiredService<DestinationValidator>();
    var retrievalOptions = sp.GetRequiredService<IOptions<RetrievalOptions>>().Value;
    return SsrfGuardHttpHandler.Create(destinationValidator, retrievalOptions);
});

// 5. Retrieval and application services
builder.Services.AddScoped<DirectImageRetriever>();
builder.Services.AddScoped<HtmlImageRetriever>();
builder.Services.AddScoped<RetrievalOrchestrator>();
builder.Services.AddScoped<ImageIngestionService>();
builder.Services.AddScoped<IImageIngestionService>(sp => sp.GetRequiredService<ImageIngestionService>());
builder.Services.AddScoped<ImageDeliveryService>();
builder.Services.AddScoped<IImageDeliveryService>(sp => sp.GetRequiredService<ImageDeliveryService>());
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<IVehicleService>(sp => sp.GetRequiredService<VehicleService>());

// 6. CORS for React Vite dashboard and health checks
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. Startup storage root verification: outside web root, created, and writable
InitializeAndVerifyStorageRoot(app.Services, app.Environment);

if (string.Equals(builder.Configuration.GetValue<string>("DatabaseProvider"), "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("DashboardCors");
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static void InitializeAndVerifyStorageRoot(IServiceProvider services, IWebHostEnvironment env)
{
    var storageOptions = services.GetRequiredService<IOptions<StorageOptions>>().Value;
    string fullStorageRoot = Path.GetFullPath(storageOptions.RootPath);

    string webRoot = !string.IsNullOrWhiteSpace(env.WebRootPath)
        ? Path.GetFullPath(env.WebRootPath)
        : Path.GetFullPath(Path.Combine(env.ContentRootPath, "wwwroot"));

    StringComparison pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    string webRootWithSep = webRoot.EndsWith(Path.DirectorySeparatorChar)
        ? webRoot
        : webRoot + Path.DirectorySeparatorChar;

    if (string.Equals(fullStorageRoot, webRoot, pathComparison) ||
        fullStorageRoot.StartsWith(webRootWithSep, pathComparison))
    {
        throw new InvalidOperationException(
            $"Storage:RootPath ('{fullStorageRoot}') must be outside the web root ('{webRoot}').");
    }

    Directory.CreateDirectory(fullStorageRoot);
    VerifyStorageRootWritable(fullStorageRoot);
}

static void VerifyStorageRootWritable(string storageRoot)
{
    string probePath = Path.Combine(
        storageRoot,
        $".vehicle-image-write-check-{Guid.NewGuid():N}.tmp");
    Exception? writeFailure = null;

    try
    {
        using var probe = new FileStream(
            probePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 256,
            options: FileOptions.WriteThrough);
        probe.Write([0x56, 0x49]);
        probe.Flush(flushToDisk: true);
    }
    catch (Exception ex)
    {
        writeFailure = ex;
    }

    try
    {
        if (File.Exists(probePath))
        {
            File.Delete(probePath);
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "Storage:RootPath verification temporary file could not be removed.",
            ex);
    }

    if (writeFailure is not null)
    {
        throw new InvalidOperationException(
            "Storage:RootPath could not be verified as writable.",
            writeFailure);
    }
}

public partial class Program;
