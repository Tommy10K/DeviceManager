using DeviceManager.Application.Interfaces;
using DeviceManager.Domain.Entities;
using DeviceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeviceManager.Infrastructure.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _dbContext;

    public DeviceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Device>> GetAllAsync()
    {
        return await _dbContext.Devices
            .Include(device => device.AssignedUser)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Device?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Devices
            .Include(device => device.AssignedUser)
            .FirstOrDefaultAsync(device => device.Id == id);
    }

    public async Task<Device?> GetByTagAsync(string tag)
    {
        return await _dbContext.Devices
            .FirstOrDefaultAsync(device => device.Tag == tag);
    }

    public async Task<Device> AddAsync(Device device)
    {
        await _dbContext.Devices.AddAsync(device);
        await _dbContext.SaveChangesAsync();
        return device;
    }

    public async Task<Device> UpdateAsync(Device device)
    {
        _dbContext.Devices.Update(device);
        await _dbContext.SaveChangesAsync();
        return device;
    }

    public async Task DeleteAsync(Guid id)
    {
        var device = await _dbContext.Devices.FindAsync(id);
        if (device is null)
        {
            return;
        }

        _dbContext.Devices.Remove(device);
        await _dbContext.SaveChangesAsync();
    }
}