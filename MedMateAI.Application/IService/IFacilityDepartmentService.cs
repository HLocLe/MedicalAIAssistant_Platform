using MedMateAI.Application.DTOs.FacilityDepartments.Responses;

namespace MedMateAI.Application.IService;

public interface IFacilityDepartmentService
{
    Task<IReadOnlyList<FacilityDepartmentActiveResponse>> ListActiveFacilityDepartmentsAsync(
        string? search = null,
        CancellationToken cancellationToken = default);
}
