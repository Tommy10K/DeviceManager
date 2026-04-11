namespace DeviceManager.Application.Interfaces;

public sealed record DeviceSpecifications(
    string Name,
    string Manufacturer,
    string OperatingSystem,
    string Type,
    string RamAmount,
    string Processor);
