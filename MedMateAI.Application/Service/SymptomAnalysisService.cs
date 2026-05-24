using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedMateAI.Application.DTOs.AIConfigs.Responses;
using MedMateAI.Application.DTOs.SymptomAnalysis.Requests;
using MedMateAI.Application.DTOs.SymptomAnalysis.Responses;
using MedMateAI.Application.DTOs.WebChatbot.Requests;
using MedMateAI.Application.IService;
using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;
using MedMateAI.Domain.Persistence;
using Microsoft.Extensions.Logging;

namespace MedMateAI.Application.Service;

public sealed class SymptomAnalysisService : ISymptomAnalysisService
{
    private const string TaskType = "SymptomAnalysis";
    private const decimal DefaultTemperature = 0.2m;
    private const int DefaultMaxTokens = 1500;

    private static readonly JsonSerializerOptions AiJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
    private const int MaxMessageLength = 2000;

    private const string FallbackSystemPrompt = """
        Ban la tro ly y te tham khao cua MedMateAI. Nhiem vu: phan tich mo ta trieu chung cua nguoi dung va goi y khoa kham phu hop.

        Quy tac:
        - Khong chan doan benh, khong ke don thuoc, khong thay the bac si.
        - Chi goi y khoa tu danh sach departments duoc cung cap.
        - Neu trieu chung nghiem trong, dat isEmergencySuggested = true va khuyen den cap cuu.
        - Tra loi bang tieng Viet.
        - Bat buoc tra JSON hop le theo schema, khong bọc markdown.
        """;

    private static readonly JsonSerializerOptions PromptJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIConfigService _aiConfigService;
    private readonly IAIChatProvider _aiChatProvider;
    private readonly IUserService _userService;
    private readonly ILogger<SymptomAnalysisService> _logger;

    public SymptomAnalysisService(
        IUnitOfWork unitOfWork,
        IAIConfigService aiConfigService,
        IAIChatProvider aiChatProvider,
        IUserService userService,
        ILogger<SymptomAnalysisService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiConfigService = aiConfigService;
        _aiChatProvider = aiChatProvider;
        _userService = userService;
        _logger = logger;
    }

    public async Task<SymptomAnalysisResponse> AnalyzeAsync(
        AnalyzeSymptomsRequest request,
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

        var currentUser = await _userService.GetCurrentUserAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;

        var session = new SymptomAnalysisSession
        {
            Id = Guid.NewGuid(),
            UserId = currentUser?.Id,
            InputText = trimmedMessage,
            Status = SymptomAnalysisSessionStatus.Processing,
            DisclaimerShown = request.DisclaimerShown,
            CreatedAt = utcNow,
        };

        _unitOfWork.SymptomAnalysisSessions.Add(session);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var departments = await LoadActiveDepartmentsAsync(cancellationToken);
            var aiConfig = await _aiConfigService.GetActiveAIConfigByTaskTypeAsync(TaskType, cancellationToken);
            var resolvedConfig = ResolveConfig(aiConfig);

            var aiRequest = new AIProviderChatRequest
            {
                SystemPrompt = resolvedConfig.SystemPrompt,
                UserMessage = BuildUserPrompt(trimmedMessage, departments),
                Model = resolvedConfig.Model,
                Temperature = resolvedConfig.Temperature,
                MaxTokens = resolvedConfig.MaxTokens,
            };

            var aiResult = await _aiChatProvider.GenerateAsync(aiRequest, cancellationToken);
            if (!TryParseAiJsonResponse(aiResult.Content, out var aiJson))
            {
                await MarkSessionFailedAsync(session, cancellationToken);
                throw new InvalidOperationException("Unable to parse AI symptom analysis response.");
            }

            await PersistAnalysisResultsAsync(session, aiJson, departments, utcNow, cancellationToken);

            return await BuildSessionResponseAsync(session, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Symptom analysis failed for session {SessionId}.", session.Id);
            await MarkSessionFailedAsync(session, cancellationToken);
            throw;
        }
    }

    public async Task<SymptomAnalysisResponse?> GetSessionByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            return null;
        }

        var session = await _unitOfWork.SymptomAnalysisSessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || session.IsDeleted)
        {
            return null;
        }

        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    private async Task<IReadOnlyList<MedicalDepartment>> LoadActiveDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        var all = await _unitOfWork.MedicalDepartments.GetAllAsync(cancellationToken);
        return all
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DepartmentName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task PersistAnalysisResultsAsync(
        SymptomAnalysisSession session,
        SymptomAnalysisAiJsonResponse aiJson,
        IReadOnlyList<MedicalDepartment> departments,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        session.SeverityLevel = NormalizeTextAllowEmpty(aiJson.SeverityLevel);
        session.Status = SymptomAnalysisSessionStatus.Completed;
        session.CompletedAt = utcNow;
        session.UpdatedAt = utcNow;
        _unitOfWork.SymptomAnalysisSessions.Update(session);

        foreach (var symptom in aiJson.Symptoms)
        {
            if (string.IsNullOrWhiteSpace(symptom.SymptomName)
                && string.IsNullOrWhiteSpace(symptom.ExtractedText))
            {
                continue;
            }

            _unitOfWork.SessionSymptoms.Add(new SessionSymptom
            {
                Id = Guid.NewGuid(),
                SymptomAnalysisSessionId = session.Id,
                SymptomName = NormalizeTextAllowEmpty(symptom.SymptomName),
                ConfidenceScore = symptom.ConfidenceScore,
                ExtractedText = NormalizeTextAllowEmpty(symptom.ExtractedText),
                CreatedAt = utcNow,
            });
        }

        var departmentLookup = BuildDepartmentLookup(departments);

        var rank = 1;

        var orderedRecommendedDepartments =aiJson.RecommendedDepartments.OrderBy(

        recommendedDepartment =>

        {
            var hasInvalidPriorityRank =recommendedDepartment.PriorityRank <= 0;

            if (hasInvalidPriorityRank)
            {
                return int.MaxValue;
            }

            return recommendedDepartment.PriorityRank;
        });

        foreach (var recommended in orderedRecommendedDepartments)
        {
            if (string.IsNullOrWhiteSpace(recommended.DepartmentName))
            {
                continue;
            }
             
            bool found = TryResolveDepartment(recommended.DepartmentName, departmentLookup, departments, out var department);

            if (found==false)
            {
                _logger.LogWarning(
                    "AI recommended department '{DepartmentName}' was not found in database for session {SessionId}.",
                    recommended.DepartmentName,
                    session.Id);
                continue;
            }

            _unitOfWork.DepartmentRecommendations.Add(new DepartmentRecommendation
            {
                Id = Guid.NewGuid(),
                SymptomAnalysisSessionId = session.Id,
                DepartmentId = department.Id,
                ConfidenceScore = recommended.ConfidenceScore,
                Reason = NormalizeTextAllowEmpty(recommended.Reason),
                PriorityRank = recommended.PriorityRank > 0 ? recommended.PriorityRank : rank,
                IsEmergencySuggested = recommended.IsEmergencySuggested || aiJson.IsEmergency,
                CreatedAt = utcNow,
            });

            rank++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkSessionFailedAsync(
        SymptomAnalysisSession session,
        CancellationToken cancellationToken)
    {
        session.Status = SymptomAnalysisSessionStatus.Failed;
        session.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.SymptomAnalysisSessions.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<SymptomAnalysisResponse> BuildSessionResponseAsync(
        SymptomAnalysisSession session,
        CancellationToken cancellationToken)
    {
        var sessionId = session.Id;

        var symptomsPaged = await _unitOfWork.SessionSymptoms.GetPagedAsync(
            1,
            100,
            s => !s.IsDeleted && s.SymptomAnalysisSessionId == sessionId,
            q => q.OrderBy(s => s.SymptomName),
            cancellationToken: cancellationToken);

        var recommendationsPaged = await _unitOfWork.DepartmentRecommendations.GetPagedAsync(
            1,
            50,
            d => !d.IsDeleted && d.SymptomAnalysisSessionId == sessionId,
            q => q.OrderBy(d => d.PriorityRank),
            cancellationToken: cancellationToken);

        var departmentRecommendations = recommendationsPaged.Items;

        if (departmentRecommendations.Count > 0)
        {
            var departments = await LoadActiveDepartmentsAsync(cancellationToken);
            var departmentById = departments.ToDictionary(d => d.Id, d => d);

            foreach (var recommendation in departmentRecommendations)
            {
                if (departmentById.TryGetValue(recommendation.DepartmentId, out var department))
                {
                    recommendation.Department = department;
                }
            }
        }

        IReadOnlyList<RecommendedFacilityResponse> recommendedFacilities;

        if (departmentRecommendations.Count == 0)
        {
            recommendedFacilities = Array.Empty<RecommendedFacilityResponse>();
        }
        else
        {
            var departmentIds = departmentRecommendations
                .Select(d => d.DepartmentId)
                .Distinct()
                .ToList();

            var facilityDepartments = await _unitOfWork.MedicalFacilities
                .GetActiveFacilityDepartmentsByDepartmentIdsAsync(departmentIds, cancellationToken);

            var recommendationByDepartmentId = departmentRecommendations
                .GroupBy(d => d.DepartmentId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.PriorityRank).First());

            recommendedFacilities = facilityDepartments
                .Select(fd =>
                {
                    recommendationByDepartmentId.TryGetValue(fd.DepartmentId, out var recommendation);
                    return new RecommendedFacilityResponse
                    {
                        FacilityId = fd.Facility.Id,
                        FacilityName = fd.Facility.FacilityName,
                        Address = fd.Facility.Address,
                        Latitude = fd.Facility.Latitude,
                        Longitude = fd.Facility.Longitude,
                        Phone = fd.Facility.Phone,
                        Website = fd.Facility.Website,
                        OpeningHours = fd.Facility.OpeningHours,
                        FacilityType = fd.Facility.FacilityType,
                        DepartmentId = fd.Department.Id,
                        DepartmentName = fd.Department.DepartmentName,
                        ConfidenceScore = recommendation?.ConfidenceScore,
                        PriorityRank = recommendation?.PriorityRank ?? 0,
                    };
                })
                .Where(f => f.Latitude.HasValue && f.Longitude.HasValue)
                .OrderBy(f => f.PriorityRank)
                .ThenByDescending(f => f.ConfidenceScore ?? 0)
                .ThenBy(f => f.FacilityName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new SymptomAnalysisResponse
        {
            SessionId = session.Id,
            InputText = session.InputText,
            SeverityLevel = session.SeverityLevel,
            Status = session.Status,
            Symptoms = symptomsPaged.Items.Select(s => new SessionSymptomResponse
            {
                Id = s.Id,
                SymptomName = s.SymptomName,
                ConfidenceScore = s.ConfidenceScore,
                ExtractedText = s.ExtractedText,
            }).ToList(),
            RecommendedDepartments = departmentRecommendations.Select(d => new RecommendedDepartmentResponse
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department?.DepartmentName,
                ConfidenceScore = d.ConfidenceScore,
                Reason = d.Reason,
                PriorityRank = d.PriorityRank,
                IsEmergencySuggested = d.IsEmergencySuggested,
            }).ToList(),
            RecommendedFacilities = recommendedFacilities,
        };
    }

    private static ResolvedChatConfig ResolveConfig(AIConfigResponse? aiConfig)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(aiConfig?.SystemPrompt)
            ? FallbackSystemPrompt
            : aiConfig.SystemPrompt.Trim();

        var model = string.Empty;
        var temperature = DefaultTemperature;
        var maxTokens = DefaultMaxTokens;

        if (!string.IsNullOrWhiteSpace(aiConfig?.Model))
        {
            model = aiConfig.Model.Trim();
        }

        if (aiConfig?.Temperature is >= 0 and <= 2)
        {
            temperature = aiConfig.Temperature.Value;
        }

        if (aiConfig?.MaxTokens is > 0)
        {
            maxTokens = aiConfig.MaxTokens.Value;
        }

        return new ResolvedChatConfig(systemPrompt, model, temperature, maxTokens);
    }

    private static string BuildUserPrompt(string message, IReadOnlyList<MedicalDepartment> departments)
    {
        var departmentPayload = departments.Select(d => new
        {
            departmentName = d.DepartmentName,
        });

        var departmentsJson = JsonSerializer.Serialize(departmentPayload, PromptJsonOptions);

        var builder = new StringBuilder();
        builder.AppendLine("User symptom description:");
        builder.AppendLine(message);
        builder.AppendLine();
        builder.AppendLine("Available departments (departmentName must match exactly from this list):");
        builder.AppendLine(departmentsJson);
        builder.AppendLine();
        builder.AppendLine("Return only valid JSON. Do not wrap in markdown.");
        builder.AppendLine("Schema:");
        builder.AppendLine("{");
        builder.AppendLine("  \"symptoms\": [");
        builder.AppendLine("    { \"symptomName\": \"string\", \"confidenceScore\": 0.0, \"extractedText\": \"string\" }");
        builder.AppendLine("  ],");
        builder.AppendLine("  \"severityLevel\": \"Mild | Moderate | Severe\",");
        builder.AppendLine("  \"isEmergency\": true | false,");
        builder.AppendLine("  \"recommendedDepartments\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"departmentName\": \"string\",");
        builder.AppendLine("      \"confidenceScore\": 0.0,");
        builder.AppendLine("      \"reason\": \"string\",");
        builder.AppendLine("      \"priorityRank\": 1,");
        builder.AppendLine("      \"isEmergencySuggested\": true | false");
        builder.AppendLine("    }");
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Rules:");
        builder.AppendLine("- recommendedDepartments.departmentName must use exact names from the departments list.");
        builder.AppendLine("- isEmergency and isEmergencySuggested must be boolean: true or false (not strings).");
        builder.AppendLine("- Set isEmergency/isEmergencySuggested to true only for severe or life-threatening symptoms.");
        builder.AppendLine("- Do not diagnose or prescribe medication.");
        builder.AppendLine("- priorityRank starts at 1 for the best match.");

        return builder.ToString();
    }

    private static bool TryParseAiJsonResponse(string content, out SymptomAnalysisAiJsonResponse aiJson)
    {
        aiJson = new SymptomAnalysisAiJsonResponse();

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var normalizedJson = content.Trim();

        if (string.IsNullOrWhiteSpace(normalizedJson))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<SymptomAnalysisAiJsonResponse>(normalizedJson, AiJsonOptions);
            if (parsed is null)
            {
                return false;
            }

            aiJson = parsed;

            aiJson.Symptoms ??= [];

            aiJson.RecommendedDepartments ??= [];

            return aiJson.Symptoms.Count > 0 || aiJson.RecommendedDepartments.Count > 0;

        }

        catch (JsonException)

        {
            return false;
        }
    }

   private static Dictionary<string, MedicalDepartment> BuildDepartmentLookup(
    IReadOnlyList<MedicalDepartment> departments)
{
   
    var lookup = new Dictionary<string, MedicalDepartment>(StringComparer.OrdinalIgnoreCase);
    foreach (var department in departments)
    {
        var normalizedName = NormalizeDepartmentName(department.DepartmentName);
        if (string.IsNullOrEmpty(normalizedName))
        {
            continue;
        }
        
        if (lookup.ContainsKey(normalizedName))
        {
            continue;
        }

            lookup[normalizedName] = department;
    }
    return lookup;
}

    private static bool TryResolveDepartment(
        string departmentName,
        Dictionary<string, MedicalDepartment> lookup,
        IReadOnlyList<MedicalDepartment> departments,
        out MedicalDepartment department)
    {
        department = null!;

        var normalized = NormalizeDepartmentName(departmentName);

        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        if (lookup.TryGetValue(normalized, out var found))
        {
            department = found;
            return true;
        }

        var match = departments.FirstOrDefault(d =>
        {
            var dbName = NormalizeDepartmentName(d.DepartmentName);
            return dbName.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(dbName, StringComparison.OrdinalIgnoreCase);
        });

        if (match is null)
        {
            return false;
        }

        department = match;
        return true;
    }


    private static string NormalizeDepartmentName(string? departmentName)
    {
        var hasInvalidDepartmentName = string.IsNullOrWhiteSpace(departmentName);

        if (hasInvalidDepartmentName)
        {
            return string.Empty;
        }

        return departmentName!.Trim().ToLowerInvariant();
    }

    private static string? NormalizeTextAllowEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record ResolvedChatConfig(
        string SystemPrompt,
        string Model,
        decimal Temperature,
        int MaxTokens);
}
