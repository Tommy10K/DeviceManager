using AutoMapper;
using DeviceManager.Application.DTOs;
using DeviceManager.Domain.Entities;

namespace DeviceManager.Application.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<Device, DeviceDto>();

        CreateMap<CreateDeviceRequest, Device>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.AssignedUser, options => options.Ignore());

        CreateMap<UpdateDeviceRequest, Device>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.AssignedUser, options => options.Ignore());
    }
}