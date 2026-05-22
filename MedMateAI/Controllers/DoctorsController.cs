using MedMateAI.Application.DTOs.Common;
using MedMateAI.Application.DTOs.Doctors.Requests;
using MedMateAI.Application.DTOs.Doctors.Responses;
using MedMateAI.Application.IService;
using MedMateAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DoctorResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DoctorResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationQuery query,
        [FromQuery] string? search,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? isActive,
        [FromQuery] DepartmentRole? departmentRole,
        CancellationToken cancellationToken = default)
    {
        if (facilityId.HasValue && facilityId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<PagedResponse<DoctorResponse>>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        if (departmentId.HasValue && departmentId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<PagedResponse<DoctorResponse>>
            {
                Success = false,
                Message = "Invalid medical department id.",
            });
        }

        var data = await _doctorService.ListDoctorsAsync(
            query.PageNumber,
            query.PageSize,
            search,
            facilityId,
            departmentId,
            isActive,
            departmentRole,
            cancellationToken);

        return Ok(new ApiResponse<PagedResponse<DoctorResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListActive(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? search,
        [FromQuery] DepartmentRole? departmentRole,
        CancellationToken cancellationToken = default)
    {
        if (facilityId.HasValue && facilityId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<IReadOnlyList<DoctorResponse>>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        if (departmentId.HasValue && departmentId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<IReadOnlyList<DoctorResponse>>
            {
                Success = false,
                Message = "Invalid medical department id.",
            });
        }

        var data = await _doctorService.ListActiveDoctorsAsync(
            facilityId,
            departmentId,
            search,
            departmentRole,
            cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<DoctorResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Invalid doctor id.",
            });
        }

        var data = await _doctorService.GetDoctorByIdAsync(id, cancellationToken);
        if (data is null)
        {
            return NotFound(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Doctor not found.",
            });
        }

        return Ok(new ApiResponse<DoctorResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Create doctor failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        if (request.FacilityDepartmentId == Guid.Empty)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Create doctor failed.",
                Errors = new List<string> { "FacilityDepartmentId is required." },
            });
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Create doctor failed.",
                Errors = new List<string> { "Full name is required." },
            });
        }

        var (ok, errors, data) = await _doctorService.CreateDoctorAsync(request, cancellationToken);
        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Create doctor failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<DoctorResponse>
        {
            Success = true,
            Message = "Doctor created.",
            Data = data,
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Invalid doctor id.",
            });
        }

        if (request is null)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Update doctor failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        if (request.FullName is not null && string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Update doctor failed.",
                Errors = new List<string> { "Full name cannot be empty when provided." },
            });
        }

        if (request.FacilityDepartmentId.HasValue && request.FacilityDepartmentId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Update doctor failed.",
                Errors = new List<string> { "FacilityDepartmentId is invalid." },
            });
        }

        var (ok, notFound, errors, data) = await _doctorService.UpdateDoctorAsync(
            id,
            request,
            cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Doctor not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Update doctor failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<DoctorResponse>
        {
            Success = true,
            Message = "Doctor updated.",
            Data = data,
        });
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateDoctorStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Invalid doctor id.",
            });
        }

        if (request is null)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Update doctor status failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        var (ok, notFound, errors, data) = await _doctorService.UpdateDoctorStatusAsync(
            id,
            request,
            cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Doctor not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<DoctorResponse>
            {
                Success = false,
                Message = "Update doctor status failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<DoctorResponse>
        {
            Success = true,
            Message = "Doctor status updated.",
            Data = data,
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Invalid doctor id.",
            });
        }

        var (ok, notFound, errors) = await _doctorService.SoftDeleteDoctorAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Doctor not found.",
                Data = false,
            });
        }

        if (!ok)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Delete doctor failed.",
                Errors = errors.ToList(),
                Data = false,
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Doctor deleted (soft).",
            Data = true,
        });
    }
}
