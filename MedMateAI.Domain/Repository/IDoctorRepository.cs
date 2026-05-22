using MedMateAI.Domain.Common;
using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;

namespace MedMateAI.Domain.Repository;

public interface IDoctorRepository : IGenericRepository<Doctor>
{
    Task<PagedResult<Doctor>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        Guid? facilityId = null,
        Guid? departmentId = null,
        bool? isActive = null,
        DepartmentRole? departmentRole = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Doctor>> GetActiveWithDetailsAsync(
        Guid? facilityId = null,
        Guid? departmentId = null,
        string? search = null,
        DepartmentRole? departmentRole = null,
        CancellationToken cancellationToken = default);

    Task<Doctor?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
