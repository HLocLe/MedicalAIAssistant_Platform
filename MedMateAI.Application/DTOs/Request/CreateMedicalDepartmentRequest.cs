namespace MedMateAI.Application.DTOs.Request;

public sealed class CreateMedicalDepartmentRequest
{
    public string? DepartmentName { get; set; }

    public string? Description { get; set; }
}
