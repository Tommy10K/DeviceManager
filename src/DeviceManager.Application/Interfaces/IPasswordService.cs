using DeviceManager.Domain.Entities;

namespace DeviceManager.Application.Interfaces;

public interface IPasswordService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string providedPassword);
}
