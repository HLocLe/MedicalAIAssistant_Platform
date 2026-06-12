using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace MedMateAI.Infrastructure.Repositories;

public sealed class FacilityDepartmentRepository
    : GenericRepository<FacilityDepartment>, IFacilityDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public FacilityDepartmentRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FacilityDepartment>> GetActiveFacilityDepartmentsAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FacilityDepartments
            .AsNoTracking()
            .Include(fd => fd.Facility)
            .Include(fd => fd.Department)
            .Where(fd =>
                !fd.IsDeleted
                && !fd.Facility.IsDeleted
                && fd.Facility.IsActive
                && !fd.Department.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(fd =>
                (fd.Facility.FacilityName ?? string.Empty).ToLower().Contains(normalizedSearch)
                || (fd.Department.DepartmentName ?? string.Empty).ToLower().Contains(normalizedSearch));
        }

        return await query
            .OrderBy(fd => fd.Facility.FacilityName)
            .ThenBy(fd => fd.Department.DepartmentName)
            .ToListAsync(cancellationToken);
    }
}
