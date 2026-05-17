namespace MedMateAI.Application.DTOs.WebChatbot.Responses;

public sealed class RecommendedFacilityResponse
{
    public Guid Id { get; set; }

    public string? FacilityName { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? OpeningHours { get; set; }

    public string? FacilityType { get; set; }

    public IReadOnlyList<string> MatchedDepartments { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableDoctors { get; set; } = Array.Empty<string>();

    public double? MatchScore { get; set; }

    public string? Reason { get; set; }
}
