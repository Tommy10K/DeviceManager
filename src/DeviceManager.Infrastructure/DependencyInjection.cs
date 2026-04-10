using DeviceManager.Application.Interfaces;
using DeviceManager.Domain.Entities;
using DeviceManager.Infrastructure.Persistence;
using DeviceManager.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeviceManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}