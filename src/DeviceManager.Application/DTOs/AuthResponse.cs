namespace DeviceManager.Application.DTOs;

public sealed record AuthResponse(
    string Token,
    string Email,
    string Role);
