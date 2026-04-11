using DeviceManager.Application.DTOs;

namespace DeviceManager.Application.Services;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetAllDevicesAsync();

    Task<DeviceDto> GetDeviceByIdAsync(Guid id);

    Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request);

    Task<DeviceDto> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request);

    Task DeleteDeviceAsync(Guid id);

    Task<DeviceDto> AssignDeviceToUserAsync(Guid deviceId, Guid userId);

    Task<DeviceDto> UnassignDeviceFromUserAsync(Guid deviceId, Guid userId);
}