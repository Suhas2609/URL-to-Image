using System.Net;
using System.Net.Sockets;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Security;

namespace VehicleImage.UnitTests;

public class DestinationValidatorTests
{
    private readonly DestinationValidator _validator = new();

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("93.184.216.34")]
    [InlineData("9.255.255.255")]
    [InlineData("11.0.0.1")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("168.63.129.17")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("192.167.255.255")]
    [InlineData("192.169.0.1")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b::8.8.8.8")]
    public void IsAllowed_PublicAddressesAndBoundaries_ReturnsTrue(string ip)
    {
        Assert.True(_validator.IsAllowed(IPAddress.Parse(ip)));
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("0.255.255.255")]
    [InlineData("10.0.0.0")]
    [InlineData("10.255.255.255")]
    [InlineData("100.64.0.0")]
    [InlineData("100.127.255.255")]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.254")]
    [InlineData("168.63.129.16")]      // Azure wire server
    [InlineData("169.254.0.1")]
    [InlineData("169.254.169.254")]    // Cloud metadata endpoint
    [InlineData("172.16.0.0")]
    [InlineData("172.31.255.255")]
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("192.88.99.1")]
    [InlineData("192.168.0.0")]
    [InlineData("192.168.255.255")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.255")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]          // Multicast
    [InlineData("239.255.255.255")]
    [InlineData("240.0.0.1")]          // Reserved
    [InlineData("255.255.255.255")]    // Broadcast
    [InlineData("::")]                 // IPv6 unspecified
    [InlineData("::1")]                // IPv6 loopback
    [InlineData("100::1")]             // Discard-only
    [InlineData("2001::1")]            // Teredo
    [InlineData("2001:2::1")]          // Benchmarking
    [InlineData("2001:db8::1")]        // Documentation
    [InlineData("fc00::1")]            // Unique local
    [InlineData("fd12:3456:789a::1")]  // Unique local
    [InlineData("fe80::1")]            // Link-local
    [InlineData("ff02::1")]            // Multicast
    [InlineData("::ffff:127.0.0.1")]   // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.1")]    // IPv4-mapped RFC 1918
    [InlineData("::ffff:169.254.169.254")] // IPv4-mapped metadata
    [InlineData("64:ff9b::127.0.0.1")] // NAT64 loopback
    [InlineData("64:ff9b::169.254.169.254")] // NAT64 metadata
    [InlineData("2002:7f00:0001::")]   // 6to4 embedding 127.0.0.1
    [InlineData("2002:0a00:0001::")]   // 6to4 embedding 10.0.0.1
    public void IsAllowed_BlockedIpv4AndIpv6Ranges_ReturnsFalse(string ip)
    {
        Assert.False(_validator.IsAllowed(IPAddress.Parse(ip)));
    }

    [Fact]
    public async Task ResolveAndValidateAsync_MixedPublicAndPrivateAddresses_ThrowsUnsafeDestination()
    {
        var validator = new DestinationValidator((_, _) => new ValueTask<IPAddress[]>(
            [IPAddress.Parse("93.184.216.34"), IPAddress.Parse("10.0.0.5")]));

        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => validator.ResolveAndValidateAsync("dual.supplier.example.com"));

        Assert.Equal(ErrorCodes.UnsafeDestination, ex.Code);
        Assert.DoesNotContain("10.0.0.5", ex.Message); // Never leak internal IP
    }

    [Fact]
    public async Task ResolveAndValidateAsync_EmptyOrFailedDns_ThrowsDnsResolutionFailed()
    {
        var validator = new DestinationValidator((_, _) =>
            throw new SocketException((int)SocketError.HostNotFound));

        var ex = await Assert.ThrowsAsync<ImageIngestionException>(
            () => validator.ResolveAndValidateAsync("nonexistent.example.com"));

        Assert.Equal(ErrorCodes.DnsResolutionFailed, ex.Code);
    }

    [Fact]
    public async Task ResolveAndValidateAsync_DnsRebindingOnSecondCall_RejectsSecondResolution()
    {
        int callCount = 0;
        var validator = new DestinationValidator((_, _) =>
        {
            callCount++;
            return callCount == 1
                ? new ValueTask<IPAddress[]>([IPAddress.Parse("93.184.216.34")])
                : new ValueTask<IPAddress[]>([IPAddress.Parse("169.254.169.254")]);
        });

        var firstResult = await validator.ResolveAndValidateAsync("rebind.example.com");
        Assert.Single(firstResult);

        var secondEx = await Assert.ThrowsAsync<ImageIngestionException>(
            () => validator.ResolveAndValidateAsync("rebind.example.com"));
        Assert.Equal(ErrorCodes.UnsafeDestination, secondEx.Code);
    }
}
