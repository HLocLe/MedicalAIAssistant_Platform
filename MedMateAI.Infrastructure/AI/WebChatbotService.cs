using System.Text;
using System.Text.Json;
using System.Globalization;
using MedMateAI.Application.DTOs.AIConfigs.Responses;
using MedMateAI.Application.DTOs.SubscriptionPlans.Responses;
using MedMateAI.Application.DTOs.WebChatbot.Requests;
using MedMateAI.Application.DTOs.WebChatbot.Responses;
using MedMateAI.Application.IService;

namespace MedMateAI.Infrastructure.AI;

public sealed class WebChatbotService : IWebChatbotService
{
    private const string TaskType = "WebSubscriptionAdvisor";
    private const string DefaultModel = "deepseek/deepseek-v4-flash:free";
    private const decimal DefaultTemperature = 0.3m;
    private const int DefaultMaxTokens = 800;
    private const int MaxMessageLength = 2000;
    private const string DefaultIntent = "SubscriptionRecommendation";

    private const string NoActivePlansAnswer =
        "Hiện tại hệ thống chưa có gói subscription nào đang mở bán. Vui lòng quay lại sau hoặc liên hệ hỗ trợ.";

    private const string FallbackParseAnswer =
        "Xin lỗi, hiện tại tôi chưa thể tư vấn chính xác gói phù hợp. Bạn có thể tham khảo các gói đang mở bán bên dưới.";

    private const string FallbackEmptyAnswer =
        "Bạn có thể tham khảo các gói đang mở bán bên dưới để chọn gói phù hợp.";

    // The primary system prompt should come from AISystemConfig.
    // This fallback prompt is used only when there is no active WebSubscriptionAdvisor config
    // or the configured SystemPrompt is empty. It keeps the chatbot usable in a fresh/local
    // database, but production should configure the prompt through AISystemConfig.
    private const string FallbackSystemPrompt = """
        Bạn là chatbot tư vấn gói subscription cho website MedMateAI.
        Nhiệm vụ của bạn là tư vấn thông tin dịch vụ và đề xuất gói subscription phù hợp dựa trên nhu cầu người dùng.
        Chỉ được đề xuất các gói subscription có trong danh sách active subscription plans được cung cấp.
        Không được tự tạo tên gói, giá, quyền lợi hoặc giới hạn không có trong dữ liệu.
        Nếu dữ liệu FeatureLimitJson không đủ rõ, hãy nói rõ rằng thông tin giới hạn tính năng hiện chưa đầy đủ và tư vấn dựa trên tên gói, giá, thời hạn.
        Nếu người dùng hỏi câu hỏi y tế/chẩn đoán/thuốc điều trị, hãy nói rằng bạn chỉ hỗ trợ thông tin dịch vụ và gói đăng ký, không thay thế bác sĩ.
        Trả lời bằng tiếng Việt, thân thiện, ngắn gọn.
        Bắt buộc trả về JSON hợp lệ theo schema được yêu cầu.
        """;

    private static readonly JsonSerializerOptions PromptJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IAIConfigService _aiConfigService;
    private readonly IAIChatProvider _aiChatProvider;

    public WebChatbotService(
        ISubscriptionPlanService subscriptionPlanService,
        IAIConfigService aiConfigService,
        IAIChatProvider aiChatProvider)
    {
        _subscriptionPlanService = subscriptionPlanService;
        _aiConfigService = aiConfigService;
        _aiChatProvider = aiChatProvider;
    }

    public async Task<WebChatbotResponse> SendMessageAsync(
        WebChatbotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentException("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.");
        }

        var trimmedMessage = request.Message.Trim();
        if (trimmedMessage.Length > MaxMessageLength)
        {
            throw new ArgumentException($"Message must be {MaxMessageLength} characters or fewer.");
        }

        var activePlans = await _subscriptionPlanService.ListActiveSubscriptionPlansAsync(cancellationToken);
        if (activePlans.Count == 0)
        {
            return new WebChatbotResponse
            {
                Answer = NoActivePlansAnswer,
                RecommendedPlans = Array.Empty<SubscriptionPlanResponse>(),
                Intent = DefaultIntent,
                NeedsMoreInformation = false,
            };
        }

        var aiConfig = await _aiConfigService.GetActiveAIConfigByTaskTypeAsync(TaskType, cancellationToken);
        var resolvedConfig = ResolveConfig(aiConfig);

        var prompt = BuildUserPrompt(trimmedMessage, activePlans);

        var aiRequest = new AIProviderChatRequest
        {
            SystemPrompt = resolvedConfig.SystemPrompt,
            UserMessage = prompt,
            Model = resolvedConfig.Model,
            Temperature = resolvedConfig.Temperature,
            MaxTokens = resolvedConfig.MaxTokens,
        };

        var aiResult = await _aiChatProvider.GenerateAsync(aiRequest, cancellationToken);
        if (!TryParseAIJsonResponse(aiResult.Content, out var aiJsonResponse))
        {
            return new WebChatbotResponse
            {
                Answer = FallbackParseAnswer,
                RecommendedPlans = activePlans,
                Intent = DefaultIntent,
                NeedsMoreInformation = true,
            };
        }

        var activePlanLookup = activePlans.ToDictionary(x => x.Id, x => x);
        var validPlanIds = aiJsonResponse.RecommendedPlanIds
            .Where(activePlanLookup.ContainsKey)
            .Distinct()
            .ToList();

        var recommendedPlans = validPlanIds
            .Select(id => activePlanLookup[id])
            .ToList();

        return new WebChatbotResponse
        {
            Answer = string.IsNullOrWhiteSpace(aiJsonResponse.Answer)
                ? FallbackEmptyAnswer
                : aiJsonResponse.Answer.Trim(),
            RecommendedPlans = recommendedPlans,
            Intent = string.IsNullOrWhiteSpace(aiJsonResponse.Intent)
                ? DefaultIntent
                : aiJsonResponse.Intent.Trim(),
            NeedsMoreInformation = aiJsonResponse.NeedsMoreInformation,
        };
    }

    private static ResolvedChatConfig ResolveConfig(AIConfigResponse? aiConfig)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(aiConfig?.SystemPrompt)
            ? FallbackSystemPrompt
            : aiConfig.SystemPrompt.Trim();

        var model = DefaultModel;
        var temperature = DefaultTemperature;
        var maxTokens = DefaultMaxTokens;

        if (!string.IsNullOrWhiteSpace(aiConfig?.ModelParams)
            && TryParseModelParams(aiConfig.ModelParams, out var parsedModelParams))
        {
            if (!string.IsNullOrWhiteSpace(parsedModelParams.Model))
            {
                model = parsedModelParams.Model.Trim();
            }

            if (parsedModelParams.Temperature.HasValue && parsedModelParams.Temperature.Value is >= 0 and <= 2)
            {
                temperature = parsedModelParams.Temperature.Value;
            }

            if (parsedModelParams.MaxTokens.HasValue && parsedModelParams.MaxTokens.Value > 0)
            {
                maxTokens = parsedModelParams.MaxTokens.Value;
            }
        }

        return new ResolvedChatConfig(systemPrompt, model, temperature, maxTokens);
    }

    private static string BuildUserPrompt(string message, IReadOnlyList<SubscriptionPlanResponse> activePlans)
    {
        var plansPayload = activePlans.Select(plan => new
        {
            id = plan.Id,
            planName = plan.PlanName,
            price = plan.Price,
            durationInDays = plan.DurationInDays,
            featureLimitJson = plan.FeatureLimitJson,
        });

        var plansJson = JsonSerializer.Serialize(plansPayload, PromptJsonOptions);

        var builder = new StringBuilder();
        builder.AppendLine("User asked:");
        builder.AppendLine(message);
        builder.AppendLine();
        builder.AppendLine("Active subscription plans:");
        builder.AppendLine(plansJson);
        builder.AppendLine();
        builder.AppendLine("Return only valid JSON. Do not wrap in markdown. Do not include explanation outside JSON.");
        builder.AppendLine("Schema:");
        builder.AppendLine("{");
        builder.AppendLine("  \"answer\": \"Vietnamese answer\",");
        builder.AppendLine("  \"recommendedPlanIds\": [\"guid-1\", \"guid-2\"],");
        builder.AppendLine("  \"intent\": \"SubscriptionRecommendation\",");
        builder.AppendLine("  \"needsMoreInformation\": false");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Rules:");
        builder.AppendLine("- recommendedPlanIds must only contain ids from the provided active subscription plans.");
        builder.AppendLine("- If you cannot choose a specific plan, set recommendedPlanIds to [] and needsMoreInformation to true.");
        builder.AppendLine("- Do not invent plan names, prices, or features.");
        builder.AppendLine("- answer must be in Vietnamese.");

        return builder.ToString();
    }

    private static bool TryParseModelParams(string modelParamsJson, out ModelParams parsedModelParams)
    {
        parsedModelParams = new ModelParams();

        try
        {
            using var document = JsonDocument.Parse(modelParamsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            parsedModelParams.Provider = TryGetStringProperty(document.RootElement, "provider");
            parsedModelParams.Model = TryGetStringProperty(document.RootElement, "model");
            parsedModelParams.Temperature = TryGetDecimalProperty(document.RootElement, "temperature");
            parsedModelParams.MaxTokens = TryGetIntProperty(document.RootElement, "maxTokens")
                ?? TryGetIntProperty(document.RootElement, "max_tokens");

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseAIJsonResponse(string content, out WebChatbotAIJsonResponse aiJsonResponse)
    {
        aiJsonResponse = new WebChatbotAIJsonResponse();

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var normalizedJson = StripMarkdownCodeFence(content);
        if (string.IsNullOrWhiteSpace(normalizedJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(normalizedJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            aiJsonResponse.Answer = TryGetStringProperty(document.RootElement, "answer")?.Trim() ?? string.Empty;
            aiJsonResponse.Intent = TryGetStringProperty(document.RootElement, "intent")?.Trim();
            aiJsonResponse.NeedsMoreInformation = TryGetBooleanProperty(document.RootElement, "needsMoreInformation");
            aiJsonResponse.RecommendedPlanIds = ParseRecommendedPlanIds(document.RootElement);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Guid> ParseRecommendedPlanIds(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "recommendedPlanIds", out var idsElement))
        {
            return Array.Empty<Guid>();
        }

        var ids = new List<Guid>();

        if (idsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in idsElement.EnumerateArray())
            {
                var id = ParseGuidFromElement(element);
                if (id.HasValue)
                {
                    ids.Add(id.Value);
                }
            }

            return ids;
        }

        var singleId = ParseGuidFromElement(idsElement);
        if (singleId.HasValue)
        {
            ids.Add(singleId.Value);
        }

        return ids;
    }

    private static Guid? ParseGuidFromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String
            && Guid.TryParse(element.GetString(), out var parsedGuid))
        {
            return parsedGuid;
        }

        if (element.ValueKind == JsonValueKind.Object
            && TryGetPropertyIgnoreCase(element, "id", out var nestedIdElement)
            && nestedIdElement.ValueKind == JsonValueKind.String
            && Guid.TryParse(nestedIdElement.GetString(), out parsedGuid))
        {
            return parsedGuid;
        }

        return null;
    }

    private static string StripMarkdownCodeFence(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        trimmed = trimmed[3..];
        if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[4..];
        }

        trimmed = trimmed.TrimStart('\r', '\n', ' ');

        var closingFenceIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFenceIndex >= 0)
        {
            trimmed = trimmed[..closingFenceIndex];
        }

        return trimmed.Trim();
    }

    private static string? TryGetStringProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static decimal? TryGetDecimalProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        if (property.ValueKind == JsonValueKind.String
            && decimal.TryParse(
                property.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimalValue))
        {
            return decimalValue;
        }

        return null;
    }

    private static int? TryGetIntProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        if (property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), out intValue))
        {
            return intValue;
        }

        return null;
    }

    private static bool TryGetBooleanProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String
            && bool.TryParse(property.GetString(), out var boolValue))
        {
            return boolValue;
        }

        return false;
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

    private sealed record ResolvedChatConfig(
        string SystemPrompt,
        string Model,
        decimal Temperature,
        int MaxTokens);

    private sealed class ModelParams
    {
        public string? Provider { get; set; }

        public string? Model { get; set; }

        public decimal? Temperature { get; set; }

        public int? MaxTokens { get; set; }
    }
}
