namespace DeviceManager.Infrastructure.AI;

public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.1-flash-lite-preview";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
}
