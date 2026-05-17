namespace MedMateAI.Application.DTOs.WebChatbot.Responses;

public sealed class WebChatbotAIJsonResponse
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<Guid> RecommendedPlanIds { get; set; } = Array.Empty<Guid>();

    public IReadOnlyList<Guid> SuggestedDepartmentIds { get; set; } = Array.Empty<Guid>();

    public IReadOnlyList<string> SuggestedDepartments { get; set; } = Array.Empty<string>();

    public string? SymptomSummary { get; set; }

    public string? SeverityLevel { get; set; }

    public bool IsEmergencySuggested { get; set; }

    public string? ExtractedLocation { get; set; }

    public string? Intent { get; set; }

    public bool NeedsMoreInformation { get; set; }
}
