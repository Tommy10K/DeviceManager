namespace DeviceManager.Application.DTOs;

public sealed record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string Location);
