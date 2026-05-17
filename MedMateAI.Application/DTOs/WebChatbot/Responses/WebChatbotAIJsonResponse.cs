namespace MedMateAI.Application.DTOs.WebChatbot.Responses;

public sealed class WebChatbotAIJsonResponse
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<Guid> RecommendedPlanIds { get; set; } = Array.Empty<Guid>();

    public string? Intent { get; set; }

    public bool NeedsMoreInformation { get; set; }
}
