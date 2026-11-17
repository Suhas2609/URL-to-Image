using System.Net;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;
using VehicleImage.Api.Security;

namespace VehicleImage.UnitTests;

public class UrlValidatorTests
{
    private readonly UrlValidator _validator;

    public UrlValidatorTests()
    {
        var options = Options.Create(new RetrievalOptions
        {
            AllowedPorts = [80, 443],
            MaxUrlLength = 2048
        });
        _validator = new UrlValidator(options, new DestinationValidator());
    }

    [Theory]
    [InlineData("https://cdn.supplier.example.com/vehicles/2026/civic.jpg")]
    [InlineData("http://images.example.org:80/photo.png")]
    [InlineData("https://images.example.org:443/path/to/car?width=1200")]
    [InlineData("https://münchen-autos.de/car.webp")]
    public void Validate_ValidPublicHttpOrHttpsUrl_ReturnsUri(string rawUrl)
    {
        Uri result = _validator.Validate(rawUrl);
        Assert.NotNull(result);
        Assert.True(result.IsAbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative/path/car.jpg")]
    [InlineData("not-a-url")]
    [InlineData("ftp://files.example.com/car.jpg")]
    [InlineData("file:///etc/passwd")]
    [InlineData("gopher://example.com:70/1")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    [InlineData("https://user:password@example.com/car.jpg")]
    [InlineData("https://intranet/car.jpg")]
    public void Validate_MalformedOrDisallowedSchemeOrUserInfo_ThrowsInvalidUrl(string? rawUrl)
    {
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(rawUrl));
        Assert.Equal(ErrorCodes.InvalidUrl, ex.Code);
    }

    [Fact]
    public void Validate_ExceedingMaxUrlLength_ThrowsInvalidUrl()
    {
        string longUrl = "https://example.com/" + new string('a', 2050);
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(longUrl));
        Assert.Equal(ErrorCodes.InvalidUrl, ex.Code);
    }

    [Fact]
    public void Validate_UrlWithControlCharacter_ThrowsInvalidUrl()
    {
        string badUrl = "https://example.com/car\u0007.jpg";
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(badUrl));
        Assert.Equal(ErrorCodes.InvalidUrl, ex.Code);
    }

    [Theory]
    [InlineData("http://localhost/car.jpg")]
    [InlineData("https://api.localhost/car.jpg")]
    [InlineData("http://printer.local/car.jpg")]
    [InlineData("http://metadata.internal/car.jpg")]
    [InlineData("http://127.0.0.1/car.jpg")]
    [InlineData("http://2130706433/car.jpg")]       // Integer-form IPv4
    [InlineData("http://127.1/car.jpg")]            // Shortened IPv4
    [InlineData("http://0177.0.0.1/car.jpg")]       // Leading-zero IPv4
    [InlineData("http://10.0.0.1/car.jpg")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://192.168.1.50/car.jpg")]
    [InlineData("http://[::1]/car.jpg")]
    [InlineData("http://[::ffff:127.0.0.1]/car.jpg")] // IPv4-mapped IPv6
    [InlineData("http://[::127.0.0.1]/car.jpg")]      // IPv4-compatible IPv6
    [InlineData("https://example.com:8080/car.jpg")]
    [InlineData("http://example.com:22/car.jpg")]
    public void Validate_UnsafeHostIpOrDisallowedPort_ThrowsUnsafeDestination(string rawUrl)
    {
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.Validate(rawUrl));
        Assert.Equal(ErrorCodes.UnsafeDestination, ex.Code);
    }

    [Fact]
    public void ValidateRedirectTarget_ResolvesRelativeUrlAgainstCurrentUri()
    {
        var current = new Uri("https://supplier.example.com/listings/2026/sedan.html");
        Uri target = _validator.ValidateRedirectTarget(current, "../images/hero.jpg");
        Assert.Equal("https://supplier.example.com/listings/images/hero.jpg", target.AbsoluteUri);
    }

    [Fact]
    public void RedactForLogging_RemovesQueryAndFragment()
    {
        var source = new Uri("https://supplier.example.com/car.jpg?api_key=secret#details");

        string redacted = UrlValidator.RedactForLogging(source);

        Assert.Equal("https://supplier.example.com/car.jpg", redacted);
        Assert.DoesNotContain("secret", redacted);
        Assert.DoesNotContain("?", redacted);
        Assert.DoesNotContain("#", redacted);
    }

    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://localhost/private.jpg")]
    [InlineData("https://supplier.example.com:8443/car.jpg")]
    public void ValidateRedirectTarget_RedirectToPrivateIpOrBadPort_ThrowsUnsafeDestination(string location)
    {
        var current = new Uri("https://supplier.example.com/image");
        var ex = Assert.Throws<ImageIngestionException>(() => _validator.ValidateRedirectTarget(current, location));
        Assert.Equal(ErrorCodes.UnsafeDestination, ex.Code);
    }
}
