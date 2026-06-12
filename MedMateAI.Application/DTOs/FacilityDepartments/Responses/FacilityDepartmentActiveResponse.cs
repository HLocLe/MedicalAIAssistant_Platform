namespace MedMateAI.Application.DTOs.FacilityDepartments.Responses;

public sealed class FacilityDepartmentActiveResponse
{
    public Guid Id { get; set; }

    public Guid FacilityId { get; set; }

    public string? FacilityName { get; set; }

    public Guid DepartmentId { get; set; }

    public string? DepartmentName { get; set; }
}
