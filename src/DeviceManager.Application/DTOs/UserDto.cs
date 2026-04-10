using DeviceManager.Domain.Enums;

namespace DeviceManager.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    string Location,
    DateTime CreatedAt,
    DateTime UpdatedAt
);