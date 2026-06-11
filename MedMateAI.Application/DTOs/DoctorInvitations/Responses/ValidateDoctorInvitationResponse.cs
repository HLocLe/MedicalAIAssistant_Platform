namespace MedMateAI.Application.DTOs.DoctorInvitations.Responses;

public sealed class ValidateDoctorInvitationResponse
{
    public bool IsValid { get; set; }

    public string? Email { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public Guid? DoctorId { get; set; }

    public bool IsLinkedToExistingDoctorProfile { get; set; }

    public string? DoctorName { get; set; }

    public string? SuggestedFullName { get; set; }

    public string Message { get; set; } = string.Empty;
}
