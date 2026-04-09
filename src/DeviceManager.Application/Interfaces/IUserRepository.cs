using DeviceManager.Domain.Entities;

namespace DeviceManager.Application.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();

    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByEmailAsync(string email);

    Task<User> AddAsync(User user);

    Task<bool> EmailExistsAsync(string email);
}