using DeviceManager.Application.DTOs;
using DeviceManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}