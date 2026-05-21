namespace MedMateAI.Application.DTOs.MedicalFacilities.Responses;

public sealed class MedicalFacilityDepartmentResponse
{
    public Guid DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public string? Description { get; set; }
}
