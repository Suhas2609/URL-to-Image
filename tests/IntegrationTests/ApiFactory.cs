using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Data;
using VehicleImage.Api.Retrieval;

namespace VehicleImage.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly int _allowedMockPort;
    private readonly Action<IServiceCollection>? _serviceOverrides;

    public string TempStorageRoot { get; }
    public string TempSqliteDbPath { get; }

    public ApiFactory(
        int allowedMockPort,
        Action<IServiceCollection>? serviceOverrides = null)
    {
        _allowedMockPort = allowedMockPort;
        _serviceOverrides = serviceOverrides;

        string baseTemp = Path.Combine(Path.GetTempPath(), $"vehicle-img-int-{Guid.NewGuid():N}");
        TempStorageRoot = Path.Combine(baseTemp, "storage");
        TempSqliteDbPath = Path.Combine(baseTemp, "test.db");
        Directory.CreateDirectory(TempStorageRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={TempSqliteDbPath}",
                ["Storage:RootPath"] = TempStorageRoot,
                ["Retrieval:MaxDownloadBytes"] = (512 * 1024).ToString(), // 512 KB cap for fast streaming tests
                ["Retrieval:TimeoutSeconds"] = "2",
                ["Retrieval:ConnectTimeoutSeconds"] = "2",
                ["Retrieval:MaxRedirects"] = "5",
                ["Retrieval:AllowedPorts:0"] = "80",
                ["Retrieval:AllowedPorts:1"] = "443",
                ["Retrieval:AllowedPorts:2"] = _allowedMockPort.ToString(),
                ["Retry:MaxAttempts"] = "3",
                ["Retry:BaseDelayMs"] = "25",
                ["Retry:MaxDelayMs"] = "100",
                ["Retry:MaxRetryAfterSeconds"] = "2"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            // Map mock hostname "supplier.test" to local Kestrel MockSupplierServer in test environment
            services.PostConfigure<HttpClientFactoryOptions>(DirectImageRetriever.HttpClientName, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                {
                    var retrievalOptions = handlerBuilder.Services.GetRequiredService<IOptions<RetrievalOptions>>().Value;
                    handlerBuilder.PrimaryHandler = new SocketsHttpHandler
                    {
                        AllowAutoRedirect = false,
                        UseProxy = false,
                        UseCookies = false,
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                        ConnectTimeout = TimeSpan.FromSeconds(retrievalOptions.ConnectTimeoutSeconds),
                        PooledConnectionLifetime = TimeSpan.FromSeconds(retrievalOptions.PooledConnectionLifetimeSeconds),
                        MaxResponseHeadersLength = 64,
                        ConnectCallback = async (context, cancellationToken) =>
                        {
                            IPAddress[] addresses = string.Equals(context.DnsEndPoint.Host, "supplier.test", StringComparison.OrdinalIgnoreCase)
                                ? [IPAddress.Loopback]
                                : await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);

                            var socket = new Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                            {
                                NoDelay = true
                            };
                            try
                            {
                                await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), cancellationToken);
                                return new NetworkStream(socket, ownsSocket: true);
                            }
                            catch
                            {
                                socket.Dispose();
                                throw;
                            }
                        }
                    };
                });
            });

            _serviceOverrides?.Invoke(services);

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                string? parent = Path.GetDirectoryName(TempStorageRoot);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    Directory.Delete(parent, recursive: true);
                }
            }
            catch
            {
                // Ignore temp cleanup race on disposal
            }
        }
    }
}
