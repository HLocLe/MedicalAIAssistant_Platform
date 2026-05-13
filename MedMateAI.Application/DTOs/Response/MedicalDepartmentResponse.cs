namespace MedMateAI.Application.DTOs.Response;

public sealed class MedicalDepartmentResponse
{
    public Guid Id { get; set; }

    public string? DepartmentName { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
