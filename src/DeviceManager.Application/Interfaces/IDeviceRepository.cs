using DeviceManager.Domain.Entities;

namespace DeviceManager.Application.Interfaces;

public interface IDeviceRepository
{
    Task<IEnumerable<Device>> GetAllAsync();

    Task<Device?> GetByIdAsync(Guid id);

    // Used by create/update flows to enforce unique physical device identity.
    Task<Device?> GetByTagAsync(string tag);

    Task<Device> AddAsync(Device device);

    Task<Device> UpdateAsync(Device device);

    Task DeleteAsync(Guid id);
}