using MedMateAI.Application.DTOs.SubscriptionPlans.Responses;

namespace MedMateAI.Application.DTOs.WebChatbot.Responses;

public sealed class WebChatbotResponse
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<SubscriptionPlanResponse> RecommendedPlans { get; set; } = Array.Empty<SubscriptionPlanResponse>();

    public string? Intent { get; set; }

    public bool NeedsMoreInformation { get; set; }
}
