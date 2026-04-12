namespace DeviceManager.Application.DTOs;

public sealed record GenerateDescriptionRequest(
    string Name,
    string Manufacturer,
    string OperatingSystem,
    string Type,
    string RamAmount,
    string Processor);
