using System.Net.Http.Json;
using DeviceManager.Application.Interfaces;
using DeviceManager.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace DeviceManager.Infrastructure.Services;

public sealed class GeminiDescriptionGenerator : IDescriptionGenerator
{
    private const string InstructionPrompt = "You are an IT asset management assistant. Generate a single-sentence, professional description of a mobile device based exclusively on the provided technical specifications. Focus on the performance tier, manufacturer identity, and corporate utility. Output only the final description string. Do not include markdown, quotes, or introductory filler text. Maximum length: 30 words.";

    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;

    public GeminiDescriptionGenerator(HttpClient httpClient, IOptions<GeminiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<string> GenerateDescriptionAsync(DeviceSpecifications specs)
    {
        ValidateSettings();

        var prompt = BuildPrompt(specs);
        var endpoint = BuildEndpoint();

        var request = new GeminiGenerateContentRequest
        {
            Contents = new List<GeminiContent>
            {
                new GeminiContent
                {
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart
                        {
                            Text = prompt
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gemini request failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>();
        if (responseBody is null)
        {
            throw new InvalidOperationException("Gemini response body was empty.");
        }

        var description = ExtractDescription(responseBody);
        return description;
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Gemini ApiKey is missing. Configure Gemini:ApiKey in a local appsettings.Development.json file.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Model))
        {
            throw new InvalidOperationException("Gemini Model is missing.");
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("Gemini BaseUrl is missing.");
        }
    }

    private string BuildPrompt(DeviceSpecifications specs)
    {
        var specifications = BuildSpecificationsText(specs);
        return $"{InstructionPrompt}\n\n{specifications}";
    }

    private static string BuildSpecificationsText(DeviceSpecifications specs)
    {
        return $"Name: {specs.Name}, Manufacturer: {specs.Manufacturer}, OS: {specs.OperatingSystem}, Type: {specs.Type}, RAM: {specs.RamAmount}, Processor: {specs.Processor}";
    }

    private string BuildEndpoint()
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
    }

    private static string ExtractDescription(GeminiGenerateContentResponse response)
    {
        if (response.Candidates is null || response.Candidates.Count == 0)
        {
            throw new InvalidOperationException("Gemini response did not contain any candidates.");
        }

        var firstCandidate = response.Candidates[0];
        if (firstCandidate.Content is null || firstCandidate.Content.Parts is null || firstCandidate.Content.Parts.Count == 0)
        {
            throw new InvalidOperationException("Gemini response did not contain any content parts.");
        }

        var text = firstCandidate.Content.Parts[0].Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini response description was empty.");
        }

        return text.Trim();
    }

    private sealed class GeminiGenerateContentRequest
    {
        public List<GeminiContent> Contents { get; set; } = new List<GeminiContent>();
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();
    }

    private sealed class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerateContentResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }
}
