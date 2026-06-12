using MedMateAI.Application.DTOs.FacilityDepartments.Responses;
using MedMateAI.Application.IService;
using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Persistence;

namespace MedMateAI.Application.Service;

public sealed class FacilityDepartmentService : IFacilityDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public FacilityDepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<FacilityDepartmentActiveResponse>> ListActiveFacilityDepartmentsAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.FacilityDepartments.GetActiveFacilityDepartmentsAsync(
            search,
            cancellationToken);

        return entities.Select(MapToResponse).ToList();
    }

    private static FacilityDepartmentActiveResponse MapToResponse(FacilityDepartment entity)
    {
        return new FacilityDepartmentActiveResponse
        {
            Id = entity.Id,
            FacilityId = entity.FacilityId,
            FacilityName = entity.Facility.FacilityName,
            DepartmentId = entity.DepartmentId,
            DepartmentName = entity.Department.DepartmentName,
        };
    }
}
