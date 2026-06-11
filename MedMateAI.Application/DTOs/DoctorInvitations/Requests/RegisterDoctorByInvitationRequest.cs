using MedMateAI.Domain.Enums;

namespace MedMateAI.Application.DTOs.DoctorInvitations.Requests;

public sealed class RegisterDoctorByInvitationRequest
{
    public string Token { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public Guid? FacilityDepartmentId { get; set; }

    public DepartmentRole? DepartmentRole { get; set; }

    public string? Qualification { get; set; }

    public int? YearsOfExperience { get; set; }
}
