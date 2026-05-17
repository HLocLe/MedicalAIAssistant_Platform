using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MedMateAI.Application.DTOs.WebChatbot.Requests;
using MedMateAI.Application.DTOs.WebChatbot.Responses;
using MedMateAI.Application.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedMateAI.Infrastructure.AI;

public sealed class OpenRouterChatProvider : IAIChatProvider
{
    private const string ProviderName = "openrouter";
    private const string DefaultBaseUrl = "https://openrouter.ai/api/v1";
    private const string DefaultModel = "openai/gpt-oss-20b:free";
    private const string DefaultHttpReferer = "http://localhost:3000";
    private const string DefaultXTitle = "MedMateAI";
    private const decimal DefaultTemperature = 0.3m;
    private const int DefaultMaxTokens = 800;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterChatProvider> _logger;

    public OpenRouterChatProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenRouterChatProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AIProviderChatResult> GenerateAsync(
        AIProviderChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentException("Request is required.");
        }

        var apiKey = _configuration["OpenRouter:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey)
            || string.Equals(apiKey, "YOUR_OPENROUTER_API_KEY", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "OpenRouter API key is not configured. Please set OpenRouter:ApiKey locally or OpenRouter__ApiKey in environment variables.");
        }

        var baseUrl = _configuration["OpenRouter:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultBaseUrl;
        }

        var defaultModel = _configuration["OpenRouter:DefaultModel"];
        if (string.IsNullOrWhiteSpace(defaultModel))
        {
            defaultModel = DefaultModel;
        }

        var httpReferer = _configuration["OpenRouter:HttpReferer"];
        if (string.IsNullOrWhiteSpace(httpReferer))
        {
            httpReferer = DefaultHttpReferer;
        }

        var xTitle = _configuration["OpenRouter:XTitle"];
        if (string.IsNullOrWhiteSpace(xTitle))
        {
            xTitle = DefaultXTitle;
        }

        var model = string.IsNullOrWhiteSpace(request.Model) ? defaultModel : request.Model.Trim();

        var payload = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = request.SystemPrompt,
                },
                new
                {
                    role = "user",
                    content = request.UserMessage,
                },
            },
            temperature = request.Temperature ?? DefaultTemperature,
            max_tokens = request.MaxTokens ?? DefaultMaxTokens,
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/chat/completions");

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Add("HTTP-Referer", httpReferer);
        httpRequest.Headers.Add("X-Title", xTitle);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OpenRouter request failed with status code {StatusCode}.",
                (int)httpResponse.StatusCode);

            throw new InvalidOperationException(
                $"OpenRouter request failed with status code {(int)httpResponse.StatusCode}. Response: {Truncate(responseBody, 500)}");
        }

        if (!TryExtractContent(responseBody, out var content, out var returnedModel))
        {
            throw new InvalidOperationException("OpenRouter response does not contain message content.");
        }

        return new AIProviderChatResult
        {
            Content = content,
            Model = returnedModel ?? model,
            Provider = ProviderName,
        };
    }

    private static bool TryExtractContent(
        string responseBody,
        out string content,
        out string? model)
    {
        content = string.Empty;
        model = null;

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            model = TryGetStringProperty(document.RootElement, "model");

            if (!TryGetPropertyIgnoreCase(document.RootElement, "choices", out var choicesElement)
                || choicesElement.ValueKind != JsonValueKind.Array
                || choicesElement.GetArrayLength() == 0)
            {
                return false;
            }

            var firstChoice = choicesElement[0];
            if (!TryGetPropertyIgnoreCase(firstChoice, "message", out var messageElement))
            {
                return false;
            }

            var extractedContent = TryGetStringProperty(messageElement, "content");
            if (string.IsNullOrWhiteSpace(extractedContent))
            {
                return false;
            }

            content = extractedContent.Trim();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? TryGetStringProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement propertyValue)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            propertyValue = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                propertyValue = property.Value;
                return true;
            }
        }

        propertyValue = default;
        return false;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
