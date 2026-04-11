using DeviceManager.Application.DTOs;
using DeviceManager.Application.Exceptions;
using DeviceManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeviceManager.API.Controllers;

/// <summary>
/// Exposes CRUD endpoints for managing devices.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    /// <summary>
    /// Returns all devices.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAll()
    {
        var devices = await _deviceService.GetAllDevicesAsync();
        return Ok(devices);
    }

    /// <summary>
    /// Returns a single device by identifier.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetById(Guid id)
    {
        var device = await _deviceService.GetDeviceByIdAsync(id);
        return Ok(device);
    }

    /// <summary>
    /// Creates a new device.
    /// </summary>
    /// <param name="request">Device payload.</param>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceDto>> Create([FromBody] CreateDeviceRequest request)
    {
        var createdDevice = await _deviceService.CreateDeviceAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = createdDevice.Id }, createdDevice);
    }

    /// <summary>
    /// Updates an existing device.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    /// <param name="request">Updated device payload.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceDto>> Update(Guid id, [FromBody] UpdateDeviceRequest request)
    {
        var updatedDevice = await _deviceService.UpdateDeviceAsync(id, request);
        return Ok(updatedDevice);
    }

    /// <summary>
    /// Deletes a device by identifier.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deviceService.DeleteDeviceAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Assigns a device to the authenticated user.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceDto>> Assign(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var updatedDevice = await _deviceService.AssignDeviceToUserAsync(id, currentUserId);
        return Ok(updatedDevice);
    }

    /// <summary>
    /// Unassigns a device from the authenticated user.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    [HttpPost("{id:guid}/unassign")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> Unassign(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var updatedDevice = await _deviceService.UnassignDeviceFromUserAsync(id, currentUserId);
        return Ok(updatedDevice);
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new BadRequestException("Authenticated user id claim is missing or invalid.");
        }

        return userId;
    }
}