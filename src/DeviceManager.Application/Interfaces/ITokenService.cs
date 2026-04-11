using DeviceManager.Domain.Entities;

namespace DeviceManager.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
