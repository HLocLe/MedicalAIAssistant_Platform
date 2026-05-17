using System.Globalization;
using System.Text;
using System.Text.Json;
using MedMateAI.Application.DTOs.AIConfigs.Responses;
using MedMateAI.Application.DTOs.SubscriptionPlans.Responses;
using MedMateAI.Application.DTOs.WebChatbot.Requests;
using MedMateAI.Application.DTOs.WebChatbot.Responses;
using MedMateAI.Application.IService;
using MedMateAI.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedMateAI.Infrastructure.AI;

public sealed class WebChatbotService : IWebChatbotService
{
    private const string PrimaryTaskType = "WebFrontDeskAssistant";
    private const string LegacyTaskType = "WebSubscriptionAdvisor";
    private const string DefaultModel = "deepseek/deepseek-v4-flash:free";
    private const decimal DefaultTemperature = 0.3m;
    private const int DefaultMaxTokens = 800;
    private const int MaxMessageLength = 2000;
    private const string DefaultIntent = "Unknown";
    private const string FacilityRecommendationIntent = "FacilityRecommendation";
    private const string MedicalSafetyRedirectIntent = "MedicalSafetyRedirect";

    private const string FallbackParseAnswer =
        "Xin lỗi, hiện tại tôi chưa thể xử lý yêu cầu này. Bạn có thể mô tả lại nhu cầu hoặc triệu chứng rõ hơn.";

    private const string FallbackEmptyAnswer =
        "Mình đã nhận được thông tin của bạn. Bạn có thể mô tả rõ hơn để mình hỗ trợ phù hợp hơn.";

    // The primary system prompt should come from AISystemConfig.
    // This fallback prompt is used only when there is no active WebFrontDeskAssistant/WebSubscriptionAdvisor config
    // or the configured SystemPrompt is empty. It keeps the chatbot usable in a fresh/local
    // database, but production should configure the prompt through AISystemConfig.
    private const string FallbackSystemPrompt = """
        You are the AI front-desk assistant for the MedMateAI website.
        You can:
        1) greet users,
        2) advise subscription plans,
        3) answer basic website/service information,
        4) suggest suitable medical departments and facility directions based on symptoms and the user-provided area.

        Safety rules:
        - Do not diagnose diseases.
        - Do not prescribe medication.
        - Do not replace a doctor.
        - If dangerous symptoms appear, advise the user to contact emergency services or go to the nearest medical facility immediately.

        Data rules:
        - Only recommend subscription plans from active plans provided in the prompt.
        - Only select departments from available departments provided in the prompt.
        - Do not invent plan names, department names, or facility names.
        - Medical facilities are selected by backend database query; do not invent facilities.

        Location rule:
        - The user may type location directly in the message.
        - Extract that location into extractedLocation when present.

        Output rule:
        - Always return valid JSON matching the schema exactly.
        - Do not include markdown.
        """;

    private static readonly JsonSerializerOptions PromptJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly HashSet<string> LocationStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "quan",
        "q",
        "phuong",
        "p",
        "duong",
        "street",
        "district",
        "ward",
        "city",
        "thanh",
        "pho",
        "tp",
        "huyen",
        "xa",
        "thi",
        "tran",
        "tinh",
    };

    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IAIConfigService _aiConfigService;
    private readonly IAIChatProvider _aiChatProvider;
    private readonly ApplicationDbContext _dbContext;

    public WebChatbotService(
        ISubscriptionPlanService subscriptionPlanService,
        IAIConfigService aiConfigService,
        IAIChatProvider aiChatProvider,
        ApplicationDbContext dbContext)
    {
        _subscriptionPlanService = subscriptionPlanService;
        _aiConfigService = aiConfigService;
        _aiChatProvider = aiChatProvider;
        _dbContext = dbContext;
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
        var availableDepartments = await GetAvailableDepartmentsAsync(cancellationToken);

        var aiConfig = await GetActiveFrontDeskConfigAsync(cancellationToken);
        var resolvedConfig = ResolveConfig(aiConfig);
        var prompt = BuildUserPrompt(trimmedMessage, activePlans, availableDepartments);

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

        var departmentSelection = ResolveSuggestedDepartments(aiJsonResponse, availableDepartments);
        var selectedDepartments = departmentSelection.SelectedDepartments;
        var selectedDepartmentNames = selectedDepartments
            .Select(x => x.DepartmentName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var intent = NormalizeIntent(aiJsonResponse.Intent);
        var extractedLocation = NormalizeOptionalText(aiJsonResponse.ExtractedLocation);
        var shouldRecommendFacilities = IsFacilityRecommendationIntent(intent) || aiJsonResponse.IsEmergencySuggested;

        IReadOnlyList<RecommendedFacilityResponse> recommendedFacilities = Array.Empty<RecommendedFacilityResponse>();
        if (shouldRecommendFacilities && selectedDepartments.Count > 0)
        {
            var selectedDepartmentIds = selectedDepartments.Select(x => x.Id).ToHashSet();
            recommendedFacilities = await QueryRecommendedFacilitiesAsync(
                selectedDepartmentIds,
                extractedLocation,
                aiJsonResponse.IsEmergencySuggested,
                cancellationToken);
        }

        var suggestedDepartments = selectedDepartmentNames.Count > 0
            ? selectedDepartmentNames
            : departmentSelection.SuggestedDepartmentNamesFromAI;

        var needsMoreInformation = aiJsonResponse.NeedsMoreInformation;
        if (shouldRecommendFacilities && selectedDepartments.Count == 0)
        {
            needsMoreInformation = true;
        }

        return new WebChatbotResponse
        {
            Answer = string.IsNullOrWhiteSpace(aiJsonResponse.Answer)
                ? FallbackEmptyAnswer
                : aiJsonResponse.Answer.Trim(),
            RecommendedPlans = recommendedPlans,
            RecommendedFacilities = recommendedFacilities,
            SuggestedDepartments = suggestedDepartments,
            LocationUsed = extractedLocation,
            Intent = intent,
            NeedsMoreInformation = needsMoreInformation,
            IsEmergencySuggested = aiJsonResponse.IsEmergencySuggested,
        };
    }

    private static WebChatbotResponse BuildSafeParseFallbackResponse()
    {
        return new WebChatbotResponse
        {
            Answer = FallbackParseAnswer,
            RecommendedPlans = Array.Empty<SubscriptionPlanResponse>(),
            RecommendedFacilities = Array.Empty<RecommendedFacilityResponse>(),
            SuggestedDepartments = Array.Empty<string>(),
            LocationUsed = null,
            Intent = DefaultIntent,
            NeedsMoreInformation = true,
            IsEmergencySuggested = false,
        };
    }

    private async Task<IReadOnlyList<DepartmentPromptItem>> GetAvailableDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.MedicalDepartments
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.DepartmentName)
            .Select(x => new DepartmentPromptItem(
                x.Id,
                x.DepartmentName,
                x.Description))
            .ToListAsync(cancellationToken);
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

    private static string BuildUserPrompt(
        string message,
        IReadOnlyList<SubscriptionPlanResponse> activePlans,
        IReadOnlyList<DepartmentPromptItem> availableDepartments)
    {
        var plansPayload = activePlans.Select(plan => new
        {
            id = plan.Id,
            planName = plan.PlanName,
            price = plan.Price,
            durationInDays = plan.DurationInDays,
            featureLimitJson = plan.FeatureLimitJson,
        });

        var departmentsPayload = availableDepartments.Select(department => new
        {
            id = department.Id,
            departmentName = department.DepartmentName,
            description = department.Description,
        });

        var plansJson = JsonSerializer.Serialize(plansPayload, PromptJsonOptions);
        var departmentsJson = JsonSerializer.Serialize(departmentsPayload, PromptJsonOptions);

        var builder = new StringBuilder();
        builder.AppendLine("User message:");
        builder.AppendLine(message);
        builder.AppendLine();
        builder.AppendLine("Active subscription plans:");
        builder.AppendLine(plansJson);
        builder.AppendLine();
        builder.AppendLine("Available medical departments:");
        builder.AppendLine(departmentsJson);
        builder.AppendLine();
        builder.AppendLine("Return only valid JSON. Do not wrap in markdown. Do not include explanation outside JSON.");
        builder.AppendLine("Schema:");
        builder.AppendLine("{");
        builder.AppendLine("  \"answer\": \"Vietnamese answer\",");
        builder.AppendLine("  \"intent\": \"Greeting | SubscriptionRecommendation | FacilityRecommendation | WebInformation | MedicalSafetyRedirect | Unknown\",");
        builder.AppendLine("  \"needsMoreInformation\": false,");
        builder.AppendLine("  \"recommendedPlanIds\": [\"guid\"],");
        builder.AppendLine("  \"suggestedDepartmentIds\": [\"guid\"],");
        builder.AppendLine("  \"suggestedDepartments\": [\"department name\"],");
        builder.AppendLine("  \"symptomSummary\": \"short symptom summary or null\",");
        builder.AppendLine("  \"severityLevel\": \"Low | Medium | High | Emergency\",");
        builder.AppendLine("  \"isEmergencySuggested\": false,");
        builder.AppendLine("  \"extractedLocation\": \"location extracted from user message or null\"");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Rules:");
        builder.AppendLine("- answer must be in Vietnamese.");
        builder.AppendLine("- recommendedPlanIds must only contain ids from active subscription plans.");
        builder.AppendLine("- suggestedDepartmentIds must only contain ids from available medical departments.");
        builder.AppendLine("- If user asks about symptoms or where to get checked, use intent FacilityRecommendation or MedicalSafetyRedirect.");
        builder.AppendLine("- If user gives a location in the message, extract it to extractedLocation.");
        builder.AppendLine("- Do not invent medical facility names.");
        builder.AppendLine("- Do not diagnose disease.");
        builder.AppendLine("- Do not prescribe medication.");
        builder.AppendLine("- If emergency symptoms appear, set isEmergencySuggested true and severityLevel Emergency or High.");
        builder.AppendLine("- If location is missing for facility recommendation, needsMoreInformation can be true and answer should ask user to provide their area/district.");

        return builder.ToString();
    }

    private async Task<IReadOnlyList<RecommendedFacilityResponse>> QueryRecommendedFacilitiesAsync(
        IReadOnlySet<Guid> selectedDepartmentIds,
        string? extractedLocation,
        bool isEmergencySuggested,
        CancellationToken cancellationToken)
    {
        var facilityDepartments = await _dbContext.FacilityDepartments
            .AsNoTracking()
            .Include(fd => fd.Facility)
            .Include(fd => fd.Department)
            .Include(fd => fd.Doctors)
            .Where(fd =>
                selectedDepartmentIds.Contains(fd.DepartmentId) &&
                !fd.IsDeleted &&
                fd.Facility != null &&
                !fd.Facility.IsDeleted &&
                fd.Facility.IsActive &&
                fd.Department != null &&
                !fd.Department.IsDeleted)
            .ToListAsync(cancellationToken);

        if (facilityDepartments.Count == 0)
        {
            return Array.Empty<RecommendedFacilityResponse>();
        }

        var facilities = facilityDepartments
            .GroupBy(fd => fd.Facility.Id)
            .Select(group =>
            {
                var facility = group.First().Facility;

                var matchedDepartmentNames = group
                    .Select(fd => fd.Department.DepartmentName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var availableDoctors = group
                    .SelectMany(fd => fd.Doctors)
                    .Where(doctor => !doctor.IsDeleted && doctor.IsActive && !string.IsNullOrWhiteSpace(doctor.FullName))
                    .Select(doctor => doctor.FullName!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var locationMatchScore = CalculateLocationMatchScore(facility.Address, extractedLocation);
                var departmentScore = matchedDepartmentNames.Count * 10;
                var doctorScore = availableDoctors.Count > 0 ? 2 : 0;
                var emergencyBonus = isEmergencySuggested && IsEmergencyFriendlyFacilityType(facility.FacilityType) ? 10 : 0;
                var matchScore = departmentScore + doctorScore + locationMatchScore + emergencyBonus;

                return new FacilityScoreItem(
                    new RecommendedFacilityResponse
                    {
                        Id = facility.Id,
                        FacilityName = facility.FacilityName,
                        Address = facility.Address,
                        Phone = facility.Phone,
                        Website = facility.Website,
                        OpeningHours = facility.OpeningHours,
                        FacilityType = facility.FacilityType,
                        MatchedDepartments = matchedDepartmentNames,
                        AvailableDoctors = availableDoctors,
                        MatchScore = matchScore,
                        Reason = BuildFacilityReason(extractedLocation, locationMatchScore, isEmergencySuggested),
                    },
                    locationMatchScore);
            })
            .OrderByDescending(x => x.Facility.MatchScore ?? 0)
            .ThenByDescending(x => x.LocationMatchScore)
            .ThenBy(x => x.Facility.FacilityName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(x => x.Facility)
            .ToList();

        return facilities;
    }

    private static DepartmentSelection ResolveSuggestedDepartments(
        WebChatbotAIJsonResponse aiJsonResponse,
        IReadOnlyList<DepartmentPromptItem> availableDepartments)
    {
        if (availableDepartments.Count == 0)
        {
            return new DepartmentSelection(
                Array.Empty<DepartmentPromptItem>(),
                NormalizeNameList(aiJsonResponse.SuggestedDepartments));
        }

        var departmentsById = availableDepartments.ToDictionary(x => x.Id, x => x);
        var selectedById = aiJsonResponse.SuggestedDepartmentIds
            .Where(departmentsById.ContainsKey)
            .Distinct()
            .Select(id => departmentsById[id])
            .ToList();

        if (selectedById.Count > 0)
        {
            return new DepartmentSelection(
                selectedById,
                NormalizeNameList(aiJsonResponse.SuggestedDepartments));
        }

        var normalizedNameLookup = availableDepartments
            .Where(x => !string.IsNullOrWhiteSpace(x.DepartmentName))
            .GroupBy(x => NormalizeForComparison(x.DepartmentName!))
            .ToDictionary(group => group.Key, group => group.First());

        var selectedByName = new List<DepartmentPromptItem>();
        foreach (var name in aiJsonResponse.SuggestedDepartments)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var normalizedName = NormalizeForComparison(name);
            if (normalizedNameLookup.TryGetValue(normalizedName, out var department)
                && selectedByName.All(x => x.Id != department.Id))
            {
                selectedByName.Add(department);
            }
        }

        return new DepartmentSelection(
            selectedByName,
            NormalizeNameList(aiJsonResponse.SuggestedDepartments));
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
            aiJsonResponse.SuggestedDepartmentIds = ParseGuidCollectionFromProperty(document.RootElement, "suggestedDepartmentIds");
            aiJsonResponse.SuggestedDepartments = ParseStringCollectionFromProperty(document.RootElement, "suggestedDepartments");
            aiJsonResponse.SymptomSummary = NormalizeOptionalText(TryGetStringProperty(document.RootElement, "symptomSummary"));
            aiJsonResponse.SeverityLevel = NormalizeOptionalText(TryGetStringProperty(document.RootElement, "severityLevel"));
            aiJsonResponse.IsEmergencySuggested = TryGetBooleanProperty(document.RootElement, "isEmergencySuggested");
            aiJsonResponse.ExtractedLocation = NormalizeOptionalText(TryGetStringProperty(document.RootElement, "extractedLocation"));

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

    private static IReadOnlyList<string> ParseStringCollectionFromProperty(JsonElement root, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var element))
        {
            return Array.Empty<string>();
        }

        var values = new List<string>();
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var value = ParseStringFromElement(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value.Trim());
                }
            }
        }
        else
        {
            var value = ParseStringFromElement(element);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value.Trim());
            }
        }

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static string? ParseStringFromElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            return TryGetStringProperty(element, "name")
                ?? TryGetStringProperty(element, "departmentName")
                ?? TryGetStringProperty(element, "value");
        }

        if (element.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            return element.ToString();
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

    private static bool IsFacilityRecommendationIntent(string? intent)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            return false;
        }

        return string.Equals(intent, FacilityRecommendationIntent, StringComparison.OrdinalIgnoreCase)
            || string.Equals(intent, MedicalSafetyRedirectIntent, StringComparison.OrdinalIgnoreCase);
    }

    private static int CalculateLocationMatchScore(string? address, string? extractedLocation)
    {
        if (string.IsNullOrWhiteSpace(extractedLocation))
        {
            return 0;
        }

        var normalizedAddress = NormalizeForComparison(address);
        var normalizedLocation = NormalizeForComparison(extractedLocation);
        if (string.IsNullOrWhiteSpace(normalizedAddress) || string.IsNullOrWhiteSpace(normalizedLocation))
        {
            return 0;
        }

        var score = 0;
        if (normalizedAddress.Contains(normalizedLocation, StringComparison.Ordinal))
        {
            score += 20;
        }

        var importantTokens = normalizedLocation
            .Split(new[] { ' ', ',', ';', '-', '/', '\\', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1 && !LocationStopWords.Contains(token))
            .Distinct(StringComparer.Ordinal);

        foreach (var token in importantTokens)
        {
            if (normalizedAddress.Contains(token, StringComparison.Ordinal))
            {
                score += 5;
            }
        }

        return score;
    }

    private static bool IsEmergencyFriendlyFacilityType(string? facilityType)
    {
        if (string.IsNullOrWhiteSpace(facilityType))
        {
            return false;
        }

        var normalized = NormalizeForComparison(facilityType);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return normalized.Contains("hospital", StringComparison.Ordinal)
            || normalized.Contains("benh vien", StringComparison.Ordinal)
            || normalized.Contains("emergency", StringComparison.Ordinal)
            || normalized.Contains("cap cuu", StringComparison.Ordinal);
    }

    private static string BuildFacilityReason(
        string? extractedLocation,
        int locationMatchScore,
        bool isEmergencySuggested)
    {
        string reason;
        if (!string.IsNullOrWhiteSpace(extractedLocation) && locationMatchScore > 0)
        {
            reason = $"Cơ sở này có chuyên khoa phù hợp và địa chỉ khớp với khu vực {extractedLocation} bạn cung cấp.";
        }
        else if (string.IsNullOrWhiteSpace(extractedLocation))
        {
            reason = "Cơ sở này có chuyên khoa phù hợp với triệu chứng bạn mô tả. Bạn có thể cung cấp khu vực/quận để mình gợi ý sát hơn.";
        }
        else
        {
            reason = $"Cơ sở này có chuyên khoa phù hợp. Bạn có thể kiểm tra thêm địa chỉ theo khu vực {extractedLocation} đã cung cấp.";
        }

        if (isEmergencySuggested)
        {
            reason += " Nếu triệu chứng nghiêm trọng hoặc diễn tiến nhanh, hãy liên hệ cấp cứu hoặc đến cơ sở y tế gần nhất ngay.";
        }

        return reason;
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
        return string.IsNullOrWhiteSpace(intent) ? DefaultIntent : intent.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static IReadOnlyList<string> NormalizeNameList(IReadOnlyList<string> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutDiacritics = RemoveDiacritics(value);
        var collapsedWhitespace = string.Join(
            " ",
            withoutDiacritics
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsedWhitespace.ToLowerInvariant();
    }

    private static string RemoveDiacritics(string input)
    {
        var normalizedString = input
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
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

    private sealed record DepartmentPromptItem(
        Guid Id,
        string? DepartmentName,
        string? Description);

    private sealed record DepartmentSelection(
        IReadOnlyList<DepartmentPromptItem> SelectedDepartments,
        IReadOnlyList<string> SuggestedDepartmentNamesFromAI);

    private sealed record FacilityScoreItem(
        RecommendedFacilityResponse Facility,
        int LocationMatchScore);
}
