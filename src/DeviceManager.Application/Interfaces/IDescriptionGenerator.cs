namespace DeviceManager.Application.Interfaces;

public interface IDescriptionGenerator
{
    Task<string> GenerateDescriptionAsync(DeviceSpecifications specs);
}
