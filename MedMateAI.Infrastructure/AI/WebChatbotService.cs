using System.Globalization;
using System.Text;
using System.Text.Json;
using MedMateAI.Application.DTOs.AIConfigs.Responses;
using MedMateAI.Application.DTOs.SubscriptionPlans.Responses;
using MedMateAI.Application.DTOs.WebChatbot.Requests;
using MedMateAI.Application.DTOs.WebChatbot.Responses;
using MedMateAI.Application.IService;

namespace MedMateAI.Infrastructure.AI;

public sealed class WebChatbotService : IWebChatbotService
{
    private const string PrimaryTaskType = "WebFrontDeskAssistant";
    private const string LegacyTaskType = "WebSubscriptionAdvisor";
    private const decimal DefaultTemperature = 0.3m;
    private const int DefaultMaxTokens = 800;
    private const int MaxMessageLength = 2000;
    private const string DefaultIntent = "Unknown";

    private const string FallbackParseAnswer =
        "Xin loi, hien tai minh chua the xu ly yeu cau nay. Ban vui long thu lai sau nhe.";

    private const string FallbackEmptyAnswer =
        "Minh da nhan duoc thong tin cua ban. Ban co the mo ta ro hon de minh ho tro tot hon.";

    // The primary system prompt should come from AISystemConfig.
    // This fallback prompt is used only when there is no active WebFrontDeskAssistant/WebSubscriptionAdvisor config
    // or the configured SystemPrompt is empty. It keeps the chatbot usable in a fresh/local
    // database, but production should configure the prompt through AISystemConfig.
    private const string FallbackSystemPrompt = """
        Ban la AI assistant ho tro khach truy cap website MedMateAI.

        Ban co the:
        1. Chao hoi khach hang.
        2. Giai thich thong tin co ban ve website/dich vu MedMateAI.
        3. Tu van goi subscription phu hop dua tren nhu cau nguoi dung.
        4. Gioi thieu cac dich vu ma he thong MedMateAI cung cap.

        Cac dich vu MedMateAI co the gioi thieu:
        - Tu van/goi y goi subscription phu hop.
        - Ho tro phan tich trieu chung o muc tham khao.
        - Ho tro goi y co so y te phu hop dua tren trieu chung/khu vuc nguoi dung cung cap.
        - Ho tro scan/phan tich thong tin thuoc.
        - Ho tro phan tich/tom tat ho so y khoa.
        - Ho tro tao recovery plan sau khi kham/chua benh.

        Quy tac quan trong:
        - Chatbot web nay chi cung cap thong tin dich vu va tu van goi subscription.
        - Khong truc tiep chan doan benh.
        - Khong ke thuoc.
        - Khong truc tiep tra danh sach benh vien/co so y te.
        - Khong thay the bac si.
        - Neu nguoi dung hoi trieu chung, benh vien gan nhat, ho so y khoa, thuoc hoac recovery plan, hay giai thich rang MedMateAI co cac dich vu ho tro tuong ung va huong dan nguoi dung su dung tinh nang phu hop trong he thong.
        - Neu trieu chung nghiem trong, khuyen nguoi dung lien he bac si/cap cuu/co so y te gan nhat.
        - Chi duoc de xuat subscription plans tu danh sach active plans duoc cung cap.
        - Khong duoc tu bia ten goi, gia, thoi han hoac quyen loi.
        - Neu FeatureLimitJson khong du ro, hay noi ro thong tin gioi han tinh nang chua day du.
        - Tra loi bang tieng Viet, than thien, ngan gon.
        - Bat buoc tra JSON hop le theo schema.
        - Khong boc markdown.
        """;

    private static readonly JsonSerializerOptions PromptJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly string[] SupportedIntents =
    [
        "Greeting",
        "SubscriptionRecommendation",
        "WebInformation",
        DefaultIntent,
    ];

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

        var aiConfig = await GetActiveFrontDeskConfigAsync(cancellationToken);
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
            return BuildSafeParseFallbackResponse();
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
            Intent = NormalizeIntent(aiJsonResponse.Intent),
            NeedsMoreInformation = aiJsonResponse.NeedsMoreInformation,
        };
    }

    private static WebChatbotResponse BuildSafeParseFallbackResponse()
    {
        return new WebChatbotResponse
        {
            Answer = FallbackParseAnswer,
            RecommendedPlans = Array.Empty<SubscriptionPlanResponse>(),
            Intent = DefaultIntent,
            NeedsMoreInformation = true,
        };
    }

    private async Task<AIConfigResponse?> GetActiveFrontDeskConfigAsync(CancellationToken cancellationToken)
    {
        var frontDeskConfig = await _aiConfigService.GetActiveAIConfigByTaskTypeAsync(
            PrimaryTaskType,
            cancellationToken);
        if (frontDeskConfig is not null)
        {
            return frontDeskConfig;
        }

        return await _aiConfigService.GetActiveAIConfigByTaskTypeAsync(
            LegacyTaskType,
            cancellationToken);
    }

    private static ResolvedChatConfig ResolveConfig(AIConfigResponse? aiConfig)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(aiConfig?.SystemPrompt)
            ? FallbackSystemPrompt
            : aiConfig.SystemPrompt.Trim();

        var model = string.Empty;
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

    private static string BuildUserPrompt(
        string message,
        IReadOnlyList<SubscriptionPlanResponse> activePlans)
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
        builder.AppendLine("User message:");
        builder.AppendLine(message);
        builder.AppendLine();
        builder.AppendLine("Active subscription plans:");
        builder.AppendLine(plansJson);
        builder.AppendLine();
        builder.AppendLine("Return only valid JSON. Do not wrap in markdown.");
        builder.AppendLine("Schema:");
        builder.AppendLine("{");
        builder.AppendLine("  \"answer\": \"Vietnamese answer\",");
        builder.AppendLine("  \"intent\": \"Greeting | SubscriptionRecommendation | WebInformation | Unknown\",");
        builder.AppendLine("  \"needsMoreInformation\": false,");
        builder.AppendLine("  \"recommendedPlanIds\": [\"guid\"]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Rules:");
        builder.AppendLine("- answer must be in Vietnamese.");
        builder.AppendLine("- recommendedPlanIds must only contain ids from active subscription plans.");
        builder.AppendLine("- If user greets, use Greeting.");
        builder.AppendLine("- If user asks about subscription/service plan, use SubscriptionRecommendation.");
        builder.AppendLine("- If user asks what MedMateAI can do, features, services, how to use the system, use WebInformation.");
        builder.AppendLine("- If user asks about symptoms, hospitals, medical records, medicine analysis, or recovery plan, do not perform those tasks directly. Explain that MedMateAI has services to support those needs and guide the user to use the appropriate feature.");
        builder.AppendLine("- Do not diagnose.");
        builder.AppendLine("- Do not prescribe medication.");
        builder.AppendLine("- Do not provide a specific hospital/facility list.");
        builder.AppendLine("- Do not invent plan names, prices, durations, or features.");
        builder.AppendLine("- If more information is needed to recommend a plan, set recommendedPlanIds to [] and needsMoreInformation to true.");

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
            aiJsonResponse.RecommendedPlanIds = ParseGuidCollectionFromProperty(document.RootElement, "recommendedPlanIds");

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Guid> ParseGuidCollectionFromProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var idsElement))
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

    private static string NormalizeIntent(string? intent)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            return DefaultIntent;
        }

        var normalized = intent.Trim();
        foreach (var supportedIntent in SupportedIntents)
        {
            if (string.Equals(normalized, supportedIntent, StringComparison.OrdinalIgnoreCase))
            {
                return supportedIntent;
            }
        }

        return DefaultIntent;
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
