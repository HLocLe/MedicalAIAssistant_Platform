using MedMateAI.Application.DTOs.Common;
using MedMateAI.Application.DTOs.FacilityDepartments.Responses;
using MedMateAI.Application.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/facility-departments")]
public sealed class FacilityDepartmentsController : ControllerBase
{
    private readonly IFacilityDepartmentService _facilityDepartmentService;

    public FacilityDepartmentsController(IFacilityDepartmentService facilityDepartmentService)
    {
        _facilityDepartmentService = facilityDepartmentService;
    }

    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FacilityDepartmentActiveResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive(
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var data = await _facilityDepartmentService.ListActiveFacilityDepartmentsAsync(
            search,
            cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<FacilityDepartmentActiveResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }
}
