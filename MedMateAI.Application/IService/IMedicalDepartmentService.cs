using MedMateAI.Application.DTOs.Request;
using MedMateAI.Application.DTOs.Response;

namespace MedMateAI.Application.IService;

public interface IMedicalDepartmentService
{
    Task<PagedMedicalDepartmentsResponse> ListMedicalDepartmentsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MedicalDepartmentResponse?> GetMedicalDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IEnumerable<string> Errors, MedicalDepartmentResponse? Data)> CreateMedicalDepartmentAsync(
        CreateMedicalDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors, MedicalDepartmentResponse? Data)> UpdateMedicalDepartmentAsync(
        Guid id,
        UpdateMedicalDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors)> SoftDeleteMedicalDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
