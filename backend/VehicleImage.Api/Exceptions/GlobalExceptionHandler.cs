using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VehicleImage.Api.DTOs.Images;

namespace VehicleImage.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = 499;
            }
            return true;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        ImageFailureResponse response = exception switch
        {
            ImageIngestionException ingestionEx => MapIngestionException(ingestionEx, traceId),
            ResourceNotFoundException notFoundEx => new ImageFailureResponse(
                Code: notFoundEx.Code,
                Title: "Resource Not Found",
                Detail: notFoundEx.Message,
                Status: StatusCodes.Status404NotFound,
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null),
            ConcurrentUpdateException concurrentEx => new ImageFailureResponse(
                Code: concurrentEx.Code,
                Title: "Concurrent Modification",
                Detail: concurrentEx.Message,
                Status: StatusCodes.Status409Conflict,
                TraceId: traceId,
                Retryable: true,
                RetryAfterSeconds: null),
            ValidationException validationEx => new ImageFailureResponse(
                Code: ErrorCodes.ValidationFailed,
                Title: "Validation Failed",
                Detail: validationEx.Message,
                Status: StatusCodes.Status400BadRequest,
                StatusType: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null),
            _ => new ImageFailureResponse(
                Code: ErrorCodes.InternalError,
                Title: "Internal Server Error",
                Detail: "An unexpected internal error occurred while processing the request.",
                Status: StatusCodes.Status500InternalServerError,
                StatusType: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                TraceId: traceId,
                Retryable: false,
                RetryAfterSeconds: null)
        };

        if (response.Status >= 500)
        {
            _logger.LogError(exception, "Unhandled or infrastructure failure [{Code}] TraceId={TraceId}", response.Code, traceId);
        }
        else
        {
            _logger.LogWarning("Request failed with [{Code}] ({Status}) TraceId={TraceId}: {Detail}",
                response.Code, response.Status, traceId, response.Detail);
        }

        httpContext.Response.StatusCode = response.Status;
        httpContext.Response.ContentType = "application/problem+json";

        if (response.RetryAfterSeconds is > 0)
        {
            httpContext.Response.Headers.RetryAfter = response.RetryAfterSeconds.Value.ToString();
        }

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static ImageFailureResponse MapIngestionException(ImageIngestionException ex, string traceId)
    {
        int? retryAfterSeconds = ex.RetryAfter.HasValue
            ? Math.Max(1, (int)Math.Ceiling(ex.RetryAfter.Value.TotalSeconds))
            : null;

        string title = ex.HttpStatusCode >= 500
            ? "Internal Processing Failure"
            : "Image Ingestion Failed";

        return new ImageFailureResponse(
            Code: ex.Code,
            Title: title,
            Detail: ex.Message,
            Status: ex.HttpStatusCode,
            TraceId: traceId,
            Retryable: ex.IsTransient,
            RetryAfterSeconds: retryAfterSeconds);
    }
}
