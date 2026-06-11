namespace MedMateAI.Application.DTOs.DoctorInvitations.Responses;

public sealed class DoctorInvitationResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid? DoctorId { get; set; }

    public string? DoctorName { get; set; }

    public bool IsLinkedToExistingDoctorProfile { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UsedAt { get; set; }
}
