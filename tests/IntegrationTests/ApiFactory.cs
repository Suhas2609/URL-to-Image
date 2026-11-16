using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VehicleImage.Api.Data;
using VehicleImage.Api.Security;

namespace VehicleImage.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly int _allowedMockPort;
    private readonly bool _useStrictProductionSsrfValidator;
    private readonly Func<string, CancellationToken, ValueTask<IPAddress[]>>? _customDnsResolver;
    private readonly Action<IServiceCollection>? _serviceOverrides;

    public string TempStorageRoot { get; }
    public string TempSqliteDbPath { get; }

    public ApiFactory(
        int allowedMockPort,
        bool useStrictProductionSsrfValidator = false,
        Func<string, CancellationToken, ValueTask<IPAddress[]>>? customDnsResolver = null,
        Action<IServiceCollection>? serviceOverrides = null)
    {
        _allowedMockPort = allowedMockPort;
        _useStrictProductionSsrfValidator = useStrictProductionSsrfValidator;
        _customDnsResolver = customDnsResolver;
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
                ["Retry:MaxAttempts"] = "3",
                ["Retry:BaseDelayMs"] = "25",
                ["Retry:MaxDelayMs"] = "100",
                ["Retry:MaxRetryAfterSeconds"] = "2"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            if (_useStrictProductionSsrfValidator)
            {
                if (_customDnsResolver is not null)
                {
                    services.RemoveAll<DestinationValidator>();
                    services.AddSingleton(new DestinationValidator(_customDnsResolver));
                }
            }
            else
            {
                // Section 17.5: Permissive test double limited strictly to the MockSupplierServer host/port
                services.RemoveAll<DestinationValidator>();
                services.AddSingleton<DestinationValidator>(
                    new MockServerLoopbackDestinationValidator(_allowedMockPort, _customDnsResolver));
            }

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

    private sealed class MockServerLoopbackDestinationValidator : DestinationValidator
    {
        private readonly int _mockPort;

        public MockServerLoopbackDestinationValidator(
            int mockPort,
            Func<string, CancellationToken, ValueTask<IPAddress[]>>? customResolver)
            : base(customResolver ?? DefaultMockResolver)
        {
            _mockPort = mockPort;
        }

        public override bool IsPortAllowed(int port, IReadOnlyCollection<int> configuredPorts)
        {
            return port == _mockPort || base.IsPortAllowed(port, configuredPorts);
        }

        public override bool IsAllowed(IPAddress address)
        {
            // Permit only IPv4 127.0.0.1 for the local MockSupplierServer; all other private/metadata IPs remain blocked!
            if (address.Equals(IPAddress.Loopback))
            {
                return true;
            }
            return base.IsAllowed(address);
        }

        private static ValueTask<IPAddress[]> DefaultMockResolver(string host, CancellationToken ct)
        {
            if (string.Equals(host, "supplier.test", StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTask<IPAddress[]>([IPAddress.Loopback]);
            }
            return new ValueTask<IPAddress[]>(Dns.GetHostAddressesAsync(host, ct));
        }
    }
}
