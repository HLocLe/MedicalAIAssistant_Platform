using MedMateAI.Domain.Entities;

namespace MedMateAI.Domain.Repository;

public interface IFacilityDepartmentRepository : IGenericRepository<FacilityDepartment>
{
    Task<IReadOnlyList<FacilityDepartment>> GetActiveFacilityDepartmentsAsync(
        string? search = null,
        CancellationToken cancellationToken = default);
}
