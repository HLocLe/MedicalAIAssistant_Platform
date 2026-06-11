namespace MedMateAI.Application.DTOs.DoctorInvitations.Responses;

public sealed class RegisterDoctorByInvitationResponse
{
    public Guid UserId { get; set; }

    public Guid? DoctorId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
