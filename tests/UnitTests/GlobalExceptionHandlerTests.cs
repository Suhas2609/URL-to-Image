using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using VehicleImage.Api.DTOs.Images;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.UnitTests;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_ImageIngestionException_MapsTo422WithCodeAndRetryMetadata()
    {
        var context = CreateHttpContext();
        var ex = ImageIngestionException.HttpRateLimited(TimeSpan.FromSeconds(4));

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
        Assert.Equal("4", context.Response.Headers.RetryAfter.ToString());

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.HttpRateLimited, body.Code);
        Assert.True(body.Retryable);
        Assert.Equal(4, body.RetryAfterSeconds);
    }

    [Fact]
    public async Task TryHandleAsync_ResourceNotFoundException_MapsTo404()
    {
        var context = CreateHttpContext();
        var id = Guid.NewGuid();
        var ex = ResourceNotFoundException.Vehicle(id);

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.VehicleNotFound, body.Code);
        Assert.False(body.Retryable);
    }

    [Fact]
    public async Task TryHandleAsync_ConcurrentUpdateException_MapsTo409()
    {
        var context = CreateHttpContext();
        var ex = new ConcurrentUpdateException();

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.ConcurrentUpdate, body.Code);
        Assert.True(body.Retryable);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_MapsTo400()
    {
        var context = CreateHttpContext();
        var ex = new ValidationException("Page must be positive.");

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.ValidationFailed, body.Code);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_MapsTo500AndHidesInternalDetails()
    {
        var context = CreateHttpContext();
        var ex = new InvalidOperationException("Internal SQL Server IP 10.20.30.40 failed at /var/secret/path");

        bool handled = await _handler.TryHandleAsync(context, ex, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        ImageFailureResponse body = await ReadBodyAsync(context);
        Assert.Equal(ErrorCodes.InternalError, body.Code);
        Assert.DoesNotContain("10.20.30.40", body.Detail);
        Assert.DoesNotContain("/var/secret/path", body.Detail);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-123";
        return context;
    }

    private static async Task<ImageFailureResponse> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var result = await JsonSerializer.DeserializeAsync<ImageFailureResponse>(context.Response.Body);
        return result ?? throw new InvalidOperationException("Failed to deserialize response body.");
    }
}
