using MedMateAI.Domain.Common;
using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;
using MedMateAI.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace MedMateAI.Infrastructure.Repositories;

public sealed class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
{
    private readonly ApplicationDbContext _context;

    public DoctorRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<PagedResult<Doctor>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        Guid? facilityId = null,
        Guid? departmentId = null,
        bool? isActive = null,
        DepartmentRole? departmentRole = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;
        normalizedPageSize = normalizedPageSize > 100 ? 100 : normalizedPageSize;

        var query = BuildDetailsQuery(onlyActive: false);
        query = ApplyFacilityDepartmentFilters(query, facilityId, departmentId, isActive, departmentRole);
        query = ApplySearch(query, search);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => (x.FullName ?? string.Empty).ToLower())
            .ThenBy(x => x.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        return new PagedResult<Doctor>
        {
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items,
        };
    }

    public async Task<IReadOnlyList<Doctor>> GetActiveWithDetailsAsync(
        Guid? facilityId = null,
        Guid? departmentId = null,
        string? search = null,
        DepartmentRole? departmentRole = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDetailsQuery(onlyActive: true);
        query = ApplyFacilityDepartmentFilters(query, facilityId, departmentId, departmentRole: departmentRole);
        query = ApplySearch(query, search);

        return await query
            .OrderBy(x => (x.FullName ?? string.Empty).ToLower())
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Doctor?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await BuildDetailsQuery(onlyActive: false)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Doctor> BuildDetailsQuery(bool onlyActive)
    {
        var query = _context.Doctors
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted
                && !x.FacilityDepartment.IsDeleted
                && !x.FacilityDepartment.Facility.IsDeleted
                && !x.FacilityDepartment.Department.IsDeleted);

        if (onlyActive)
        {
            query = query.Where(x => x.IsActive);
        }

        return query
            .Include(x => x.FacilityDepartment)
            .ThenInclude(x => x.Facility)
            .Include(x => x.FacilityDepartment)
            .ThenInclude(x => x.Department);
    }

    private static IQueryable<Doctor> ApplyFacilityDepartmentFilters(
        IQueryable<Doctor> query,
        Guid? facilityId = null,
        Guid? departmentId = null,
        bool? isActive = null,
        DepartmentRole? departmentRole = null)
    {
        if (facilityId.HasValue && facilityId.Value != Guid.Empty)
        {
            query = query.Where(x => x.FacilityDepartment.FacilityId == facilityId.Value);
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(x => x.FacilityDepartment.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (departmentRole.HasValue)
        {
            query = query.Where(x => x.DepartmentRole == departmentRole.Value);
        }

        return query;
    }

    private static IQueryable<Doctor> ApplySearch(IQueryable<Doctor> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var normalizedSearch = search.Trim().ToLower();
        var matchStaff = DepartmentRole.Staff.ToString().ToLower().Contains(normalizedSearch);
        var matchDeputyHead = DepartmentRole.DeputyHead.ToString().ToLower().Contains(normalizedSearch);
        var matchHead = DepartmentRole.Head.ToString().ToLower().Contains(normalizedSearch);
        var matchLeadingExpert = DepartmentRole.LeadingExpert.ToString().ToLower().Contains(normalizedSearch);
        var matchConsultant = DepartmentRole.Consultant.ToString().ToLower().Contains(normalizedSearch);

        return query.Where(x =>
            (x.FullName ?? string.Empty).ToLower().Contains(normalizedSearch)
            || (x.Specialty ?? string.Empty).ToLower().Contains(normalizedSearch)
            || (x.AcademicTitle ?? string.Empty).ToLower().Contains(normalizedSearch)
            || (matchStaff && x.DepartmentRole == DepartmentRole.Staff)
            || (matchDeputyHead && x.DepartmentRole == DepartmentRole.DeputyHead)
            || (matchHead && x.DepartmentRole == DepartmentRole.Head)
            || (matchLeadingExpert && x.DepartmentRole == DepartmentRole.LeadingExpert)
            || (matchConsultant && x.DepartmentRole == DepartmentRole.Consultant));
    }
}
