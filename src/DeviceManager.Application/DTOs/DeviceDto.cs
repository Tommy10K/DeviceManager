using DeviceManager.Domain.Enums;

namespace DeviceManager.Application.DTOs;

public sealed record DeviceDto(
    Guid Id,
    string Tag,
    string Name,
    string Manufacturer,
    DeviceType Type,
    string OperatingSystem,
    string OSVersion,
    string Processor,
    string RamAmount,
    string? Description,
    Guid? AssignedUserId,
    UserDto? AssignedUser,
    DateTime CreatedAt,
    DateTime UpdatedAt
);