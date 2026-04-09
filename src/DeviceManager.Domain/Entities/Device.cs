using DeviceManager.Domain.Enums;

namespace DeviceManager.Domain.Entities;

public class Device
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public DeviceType Type { get; set; }

    public string OperatingSystem { get; set; } = string.Empty;

    public string OSVersion { get; set; } = string.Empty;

    public string Processor { get; set; } = string.Empty;

    public string RamAmount { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? AssignedUserId { get; set; }

    public User? AssignedUser { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}