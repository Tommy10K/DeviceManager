using DeviceManager.Domain.Enums;

namespace DeviceManager.Application.DTOs;

public sealed record UpdateDeviceRequest(
    string Tag,
    string Name,
    string Manufacturer,
    DeviceType Type,
    string OperatingSystem,
    string OSVersion,
    string Processor,
    string RamAmount,
    string? Description,
    Guid? AssignedUserId
);