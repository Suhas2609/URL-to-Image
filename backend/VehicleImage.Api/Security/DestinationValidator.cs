using System.Net;
using System.Net.Sockets;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Security;

public class DestinationValidator
{
    private static readonly IPNetwork2[] BlockedNetworks =
    [
        // IPv4 blocked ranges (Section 10.1)
        IPNetwork2.Parse("0.0.0.0", 8),          // "This" network
        IPNetwork2.Parse("10.0.0.0", 8),         // RFC 1918 private
        IPNetwork2.Parse("100.64.0.0", 10),      // Carrier-grade NAT (RFC 6598)
        IPNetwork2.Parse("127.0.0.0", 8),        // Loopback
        IPNetwork2.Parse("168.63.129.16", 32),   // Azure platform wire server
        IPNetwork2.Parse("169.254.0.0", 16),     // Link-local & cloud metadata (169.254.169.254)
        IPNetwork2.Parse("172.16.0.0", 12),      // RFC 1918 private
        IPNetwork2.Parse("192.0.0.0", 24),       // IETF protocol assignments
        IPNetwork2.Parse("192.0.2.0", 24),       // TEST-NET-1 documentation
        IPNetwork2.Parse("192.88.99.0", 24),     // 6to4 relay anycast
        IPNetwork2.Parse("192.168.0.0", 16),     // RFC 1918 private
        IPNetwork2.Parse("198.18.0.0", 15),      // Network benchmark tests
        IPNetwork2.Parse("198.51.100.0", 24),    // TEST-NET-2 documentation
        IPNetwork2.Parse("203.0.113.0", 24),    // TEST-NET-3 documentation
        IPNetwork2.Parse("224.0.0.0", 4),        // Multicast
        IPNetwork2.Parse("240.0.0.0", 4),        // Reserved for future use & broadcast (255.255.255.255)

        // IPv6 blocked ranges (Section 10.1)
        IPNetwork2.Parse("::", 128),             // Unspecified
        IPNetwork2.Parse("::1", 128),            // Loopback
        IPNetwork2.Parse("100::", 64),           // Discard-only (RFC 6666)
        IPNetwork2.Parse("2001::", 32),          // Teredo tunneling
        IPNetwork2.Parse("2001:2::", 48),        // Benchmarking
        IPNetwork2.Parse("2001:db8::", 32),      // Documentation
        IPNetwork2.Parse("fc00::", 7),           // Unique local address (ULA)
        IPNetwork2.Parse("fe80::", 10),          // Link-local
        IPNetwork2.Parse("ff00::", 8)            // Multicast
    ];

    private readonly Func<string, CancellationToken, ValueTask<IPAddress[]>> _dnsResolver;

    public DestinationValidator()
        : this( static (host, ct) => new ValueTask<IPAddress[]>(Dns.GetHostAddressesAsync(host, ct)) )
    {
    }

    public DestinationValidator(Func<string, CancellationToken, ValueTask<IPAddress[]>> dnsResolver)
    {
        _dnsResolver = dnsResolver ?? throw new ArgumentNullException(nameof(dnsResolver));
    }

    public virtual bool IsPortAllowed(int port, IReadOnlyCollection<int> configuredPorts)
    {
        return configuredPorts.Contains(port);
    }

    public virtual bool IsHostNameAllowed(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        string normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized == "localhost" ||
            normalized.EndsWith(".localhost", StringComparison.Ordinal) ||
            normalized.EndsWith(".local", StringComparison.Ordinal) ||
            normalized.EndsWith(".internal", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public virtual bool IsAllowed(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            return IsAllowed(address.MapToIPv4());
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] bytes = address.GetAddressBytes();

            // Check IPv4-compatible IPv6 (::w.x.y.z /96, excluding :: and ::1 which are caught below)
            bool isIpv4Compatible = true;
            for (int i = 0; i < 12; i++)
            {
                if (bytes[i] != 0)
                {
                    isIpv4Compatible = false;
                    break;
                }
            }
            if (isIpv4Compatible && (bytes[12] != 0 || bytes[13] != 0 || bytes[14] != 0 || bytes[15] > 1))
            {
                var embeddedV4 = new IPAddress(new ReadOnlySpan<byte>(bytes, 12, 4));
                return IsAllowed(embeddedV4);
            }

            // Check NAT64 well-known prefix 64:ff9b::/96 and 64:ff9b:1::/48 -> last 4 bytes are IPv4
            if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xff && bytes[3] == 0x9b)
            {
                var embeddedV4 = new IPAddress(new ReadOnlySpan<byte>(bytes, 12, 4));
                return IsAllowed(embeddedV4);
            }

            // Check 6to4 prefix 2002::/16 -> bytes 2..5 are the embedded IPv4 address
            if (bytes[0] == 0x20 && bytes[1] == 0x02)
            {
                var embeddedV4 = new IPAddress(new ReadOnlySpan<byte>(bytes, 2, 4));
                return IsAllowed(embeddedV4);
            }
        }

        foreach (var network in BlockedNetworks)
        {
            if (network.Contains(address))
            {
                return false;
            }
        }

        return true;
    }

    public virtual async Task<IReadOnlyList<IPAddress>> ResolveAndValidateAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        if (!IsHostNameAllowed(host))
        {
            throw ImageIngestionException.UnsafeDestination();
        }

        string cleanHost = host.Trim();
        if (cleanHost.StartsWith('[') && cleanHost.EndsWith(']'))
        {
            cleanHost = cleanHost[1..^1];
        }

        if (IPAddress.TryParse(cleanHost, out var literalAddress))
        {
            if (literalAddress.AddressFamily == AddressFamily.InterNetworkV6 && literalAddress.ScopeId != 0)
            {
                throw ImageIngestionException.UnsafeDestination();
            }

            if (!IsAllowed(literalAddress))
            {
                throw ImageIngestionException.UnsafeDestination();
            }

            return [literalAddress];
        }

        IPAddress[] addresses;
        try
        {
            addresses = await _dnsResolver(cleanHost, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            throw ImageIngestionException.DnsResolutionFailed(inner: ex);
        }

        if (addresses is null || addresses.Length == 0)
        {
            throw ImageIngestionException.DnsResolutionFailed();
        }

        // Every resolved address must be permitted (Section 10.1 / 19.12)
        foreach (var addr in addresses)
        {
            if (!IsAllowed(addr))
            {
                throw ImageIngestionException.UnsafeDestination();
            }
        }

        return addresses;
    }

    private readonly struct IPNetwork2
    {
        private readonly AddressFamily _family;
        private readonly byte[] _networkBytes;
        private readonly int _prefixLength;

        private IPNetwork2(AddressFamily family, byte[] networkBytes, int prefixLength)
        {
            _family = family;
            _networkBytes = networkBytes;
            _prefixLength = prefixLength;
        }

        public static IPNetwork2 Parse(string ipString, int prefixLength)
        {
            var ip = IPAddress.Parse(ipString);
            return new IPNetwork2(ip.AddressFamily, ip.GetAddressBytes(), prefixLength);
        }

        public bool Contains(IPAddress address)
        {
            if (address.AddressFamily != _family)
            {
                return false;
            }

            byte[] addrBytes = address.GetAddressBytes();
            int fullBytes = _prefixLength / 8;
            int remainingBits = _prefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
            {
                if (addrBytes[i] != _networkBytes[i])
                {
                    return false;
                }
            }

            if (remainingBits > 0)
            {
                byte mask = (byte)(0xFF << (8 - remainingBits));
                if ((addrBytes[fullBytes] & mask) != (_networkBytes[fullBytes] & mask))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
