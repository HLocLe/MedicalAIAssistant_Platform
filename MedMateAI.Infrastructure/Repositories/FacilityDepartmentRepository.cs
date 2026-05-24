using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Repository;

namespace MedMateAI.Infrastructure.Repositories;

public sealed class FacilityDepartmentRepository
    : GenericRepository<FacilityDepartment>, IFacilityDepartmentRepository
{
    public FacilityDepartmentRepository(ApplicationDbContext context)
        : base(context)
    {
    }
}
