using DeviceManager.Application.Interfaces;
using DeviceManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DeviceManager.Infrastructure.Services;

public sealed class PasswordService : IPasswordService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordService(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
