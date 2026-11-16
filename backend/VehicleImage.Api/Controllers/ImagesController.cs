using Microsoft.AspNetCore.Mvc;
using VehicleImage.Api.Services;

namespace VehicleImage.Api.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController : ControllerBase
{
    private readonly IImageDeliveryService _deliveryService;

    public ImagesController(IImageDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        ImageDeliveryResult result = await _deliveryService.OpenImageAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.ContentLength = result.FileSize;

        return File(result.Stream, result.ContentType);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        ImageDeliveryResult result = await _deliveryService.OpenImageAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.ContentLength = result.FileSize;

        return File(result.Stream, result.ContentType, result.DownloadFileName);
    }
}
