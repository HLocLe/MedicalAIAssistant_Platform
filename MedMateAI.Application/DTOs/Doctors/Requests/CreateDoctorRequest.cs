using MedMateAI.Domain.Enums;

namespace MedMateAI.Application.DTOs.Doctors.Requests;

public sealed class CreateDoctorRequest
{
    public Guid FacilityDepartmentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Specialty { get; set; }

    public string? AcademicTitle { get; set; }

    public DepartmentRole DepartmentRole { get; set; } = DepartmentRole.Staff;

    public int? YearsOfExperience { get; set; }

    public bool IsActive { get; set; } = true;
}
