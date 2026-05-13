namespace MedMateAI.Application.DTOs.Response;

public sealed class PagedMedicalDepartmentsResponse
{
    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<MedicalDepartmentResponse> Items { get; set; } = Array.Empty<MedicalDepartmentResponse>();
}
