using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VehicleImage.Api.DTOs;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Services;

namespace VehicleImage.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ImageSubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ImageSubmissionResponse>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        ImageSubmissionResponse result = await _vehicleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.VehicleId }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<VehicleResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Query parameters 'page' must be >= 1 and 'pageSize' must be between 1 and 100.");
        }

        PagedResponse<VehicleResponse> result = await _vehicleService.ListAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        VehicleResponse vehicle = await _vehicleService.GetAsync(id, cancellationToken);
        return Ok(vehicle);
    }

    [HttpPut("{id:guid}/image")]
    [ProducesResponseType(typeof(ImageSubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ImageSubmissionResponse>> ReplaceImage(
        Guid id,
        [FromBody] ReplaceVehicleImageRequest request,
        CancellationToken cancellationToken)
    {
        ImageSubmissionResponse result = await _vehicleService.ReplaceImageAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
