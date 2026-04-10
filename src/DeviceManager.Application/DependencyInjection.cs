using AutoMapper;
using DeviceManager.Application.DTOs;
using DeviceManager.Application.Mappings;
using DeviceManager.Application.Services;
using DeviceManager.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DeviceManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeviceService, DeviceService>();

        services.AddAutoMapper(configuration =>
        {
            configuration.AddProfile<MappingProfile>();
        });

        services.AddScoped<IValidator<CreateDeviceRequest>, CreateDeviceRequestValidator>();
        services.AddScoped<IValidator<UpdateDeviceRequest>, UpdateDeviceRequestValidator>();

        return services;
    }
}