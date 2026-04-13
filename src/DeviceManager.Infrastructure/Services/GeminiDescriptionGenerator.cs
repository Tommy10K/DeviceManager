using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DeviceManager.Application.Exceptions;
using DeviceManager.Application.Interfaces;
using DeviceManager.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace DeviceManager.Infrastructure.Services;

public sealed class GeminiDescriptionGenerator : IDescriptionGenerator
{
    private const string InstructionPrompt = "You are an IT asset management assistant. Generate a single-sentence, professional description of a mobile device based exclusively on the provided technical specifications. Focus on the performance tier, manufacturer identity, and corporate utility. Output only the final description string. Do not include markdown, quotes, or introductory filler text. Maximum length: 40 words.";

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

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(endpoint, request);
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalServiceException(
                statusCode: 503,
                errorTitle: "AI service unreachable",
                message: "Could not reach the AI provider. Check network connectivity and Gemini base URL.",
                innerException: exception);
        }
        catch (TaskCanceledException exception)
        {
            throw new ExternalServiceException(
                statusCode: 503,
                errorTitle: "AI service timeout",
                message: "The AI provider did not respond in time. Please try again.",
                innerException: exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw BuildUpstreamFailure(response.StatusCode, errorBody);
        }

        var responseBody = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>();
        if (responseBody is null)
        {
            throw new ExternalServiceException(
                statusCode: 502,
                errorTitle: "AI response format error",
                message: "AI provider returned an empty response body.");
        }

        var description = ExtractDescription(responseBody);
        return description;
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new ExternalServiceException(
                statusCode: 500,
                errorTitle: "AI configuration error",
                message: "Gemini ApiKey is missing. Configure Gemini:ApiKey in a local appsettings.Development.json file.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Model))
        {
            throw new ExternalServiceException(
                statusCode: 500,
                errorTitle: "AI configuration error",
                message: "Gemini Model is missing.");
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new ExternalServiceException(
                statusCode: 500,
                errorTitle: "AI configuration error",
                message: "Gemini BaseUrl is missing.");
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
            throw new ExternalServiceException(
                statusCode: 502,
                errorTitle: "AI response format error",
                message: "AI provider response did not contain any candidates.");
        }

        var firstCandidate = response.Candidates[0];
        if (firstCandidate.Content is null || firstCandidate.Content.Parts is null || firstCandidate.Content.Parts.Count == 0)
        {
            throw new ExternalServiceException(
                statusCode: 502,
                errorTitle: "AI response format error",
                message: "AI provider response did not contain any content parts.");
        }

        var text = firstCandidate.Content.Parts[0].Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ExternalServiceException(
                statusCode: 502,
                errorTitle: "AI response format error",
                message: "AI provider returned an empty description.");
        }

        return text.Trim();
    }

    private static ExternalServiceException BuildUpstreamFailure(HttpStatusCode statusCode, string errorBody)
    {
        var providerMessage = ExtractProviderMessage(errorBody);

        if (statusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or HttpStatusCode.TooManyRequests)
        {
            return new ExternalServiceException(
                statusCode: 503,
                errorTitle: "AI service unavailable",
                message: $"AI provider is temporarily unavailable. {providerMessage}");
        }

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new ExternalServiceException(
                statusCode: 502,
                errorTitle: "AI service authentication failed",
                message: $"AI provider rejected authentication. {providerMessage}");
        }

        return new ExternalServiceException(
            statusCode: 502,
            errorTitle: "AI service error",
            message: $"AI provider returned status {(int)statusCode}. {providerMessage}");
    }

    private static string ExtractProviderMessage(string errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody))
        {
            return "No additional error details were provided.";
        }

        try
        {
            using var document = JsonDocument.Parse(errorBody);
            if (document.RootElement.TryGetProperty("error", out var errorNode) &&
                errorNode.TryGetProperty("message", out var nestedMessageNode) &&
                nestedMessageNode.ValueKind == JsonValueKind.String)
            {
                var nestedMessage = nestedMessageNode.GetString();
                return string.IsNullOrWhiteSpace(nestedMessage)
                    ? "No additional error details were provided."
                    : nestedMessage.Trim();
            }

            if (document.RootElement.TryGetProperty("message", out var messageNode) &&
                messageNode.ValueKind == JsonValueKind.String)
            {
                var message = messageNode.GetString();
                return string.IsNullOrWhiteSpace(message)
                    ? "No additional error details were provided."
                    : message.Trim();
            }
        }
        catch (JsonException)
        {
            // Ignore parse failures and return the raw body below.
        }

        return errorBody.Trim();
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
