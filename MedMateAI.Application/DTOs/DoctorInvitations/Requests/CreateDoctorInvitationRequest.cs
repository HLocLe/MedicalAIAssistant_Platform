namespace MedMateAI.Application.DTOs.DoctorInvitations.Requests;

public sealed class CreateDoctorInvitationRequest
{
    public string Email { get; set; } = string.Empty;

    public Guid? DoctorId { get; set; }
}
