using MedMateAI.Application.DTOs.SubscriptionPlans.Responses;

namespace MedMateAI.Application.DTOs.WebChatbot.Responses;

public sealed class WebChatbotResponse
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<SubscriptionPlanResponse> RecommendedPlans { get; set; } = Array.Empty<SubscriptionPlanResponse>();

    public IReadOnlyList<RecommendedFacilityResponse> RecommendedFacilities { get; set; } = Array.Empty<RecommendedFacilityResponse>();

    public IReadOnlyList<string> SuggestedDepartments { get; set; } = Array.Empty<string>();

    public string? LocationUsed { get; set; }

    public string? Intent { get; set; }

    public bool NeedsMoreInformation { get; set; }

    public bool IsEmergencySuggested { get; set; }
}
