using MedMateAI.Application.DTOs.Common;
using MedMateAI.Application.DTOs.MedicalFacilities.Requests;
using MedMateAI.Application.DTOs.MedicalFacilities.Responses;
using MedMateAI.Application.IService;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/medical-facilities")]
public sealed class MedicalFacilitiesController : ControllerBase
{
    private readonly IMedicalFacilityService _medicalFacilityService;

    public MedicalFacilitiesController(IMedicalFacilityService medicalFacilityService)
    {
        _medicalFacilityService = medicalFacilityService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MedicalFacilityResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationQuery query,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var data = await _medicalFacilityService.ListMedicalFacilitiesAsync(
            query.PageNumber,
            query.PageSize,
            search,
            isActive,
            cancellationToken);

        return Ok(new ApiResponse<PagedResponse<MedicalFacilityResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MedicalFacilityResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MedicalFacilityResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListActive(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue && departmentId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<IReadOnlyList<MedicalFacilityResponse>>
            {
                Success = false,
                Message = "Invalid medical department id.",
            });
        }

        var data = await _medicalFacilityService.ListActiveMedicalFacilitiesAsync(
            departmentId,
            search,
            cancellationToken);

        return Ok(new ApiResponse<IReadOnlyList<MedicalFacilityResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        var data = await _medicalFacilityService.GetMedicalFacilityByIdAsync(id, cancellationToken);
        if (data is null)
        {
            return NotFound(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Medical facility not found.",
            });
        }

        return Ok(new ApiResponse<MedicalFacilityResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMedicalFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Create medical facility failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        if (string.IsNullOrWhiteSpace(request.FacilityName))
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Create medical facility failed.",
                Errors = new List<string> { "Facility name is required." },
            });
        }

        var (ok, errors, data) = await _medicalFacilityService.CreateMedicalFacilityAsync(request, cancellationToken);
        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Create medical facility failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<MedicalFacilityResponse>
        {
            Success = true,
            Message = "Medical facility created.",
            Data = data,
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMedicalFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        if (request is null)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Update medical facility failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        if (request.FacilityName is not null && string.IsNullOrWhiteSpace(request.FacilityName))
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Update medical facility failed.",
                Errors = new List<string> { "Facility name cannot be empty when provided." },
            });
        }

        var (ok, notFound, errors, data) = await _medicalFacilityService.UpdateMedicalFacilityAsync(id, request, cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Medical facility not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Update medical facility failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<MedicalFacilityResponse>
        {
            Success = true,
            Message = "Medical facility updated.",
            Data = data,
        });
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MedicalFacilityResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateMedicalFacilityStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        if (request is null)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Update medical facility status failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        var (ok, notFound, errors, data) = await _medicalFacilityService.UpdateMedicalFacilityStatusAsync(
            id,
            request,
            cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Medical facility not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<MedicalFacilityResponse>
            {
                Success = false,
                Message = "Update medical facility status failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<MedicalFacilityResponse>
        {
            Success = true,
            Message = "Medical facility status updated.",
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
                Message = "Invalid medical facility id.",
            });
        }

        var (ok, notFound, errors) = await _medicalFacilityService.SoftDeleteMedicalFacilityAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Medical facility not found.",
                Data = false,
            });
        }

        if (!ok)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Delete medical facility failed.",
                Errors = errors.ToList(),
                Data = false,
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Medical facility deleted (soft).",
            Data = true,
        });
    }
}
