using MedMateAI.Application.DTOs.Common;
using MedMateAI.Application.DTOs.Doctors.Requests;
using MedMateAI.Application.DTOs.Doctors.Responses;
using MedMateAI.Domain.Enums;

namespace MedMateAI.Application.IService;

public interface IDoctorService
{
    Task<PagedResponse<DoctorResponse>> ListDoctorsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        Guid? facilityId = null,
        Guid? departmentId = null,
        bool? isActive = null,
        DepartmentRole? departmentRole = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorResponse>> ListActiveDoctorsAsync(
        Guid? facilityId = null,
        Guid? departmentId = null,
        string? search = null,
        DepartmentRole? departmentRole = null,
        CancellationToken cancellationToken = default);

    Task<DoctorResponse?> GetDoctorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IEnumerable<string> Errors, DoctorResponse? Data)> CreateDoctorAsync(
        CreateDoctorRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors, DoctorResponse? Data)> UpdateDoctorAsync(
        Guid id,
        UpdateDoctorRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors, DoctorResponse? Data)> UpdateDoctorStatusAsync(
        Guid id,
        UpdateDoctorStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, bool NotFound, IEnumerable<string> Errors)> SoftDeleteDoctorAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
