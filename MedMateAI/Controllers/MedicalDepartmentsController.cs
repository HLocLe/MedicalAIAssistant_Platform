using MedMateAI.Application.DTOs;
using MedMateAI.Application.DTOs.Request;
using MedMateAI.Application.DTOs.Response;
using MedMateAI.Application.IService;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/medical-departments")]
public sealed class MedicalDepartmentsController : ControllerBase
{
    private readonly IMedicalDepartmentService _medicalDepartmentService;

    public MedicalDepartmentsController(IMedicalDepartmentService medicalDepartmentService)
    {
        _medicalDepartmentService = medicalDepartmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedMedicalDepartmentsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var data = await _medicalDepartmentService.ListMedicalDepartmentsAsync(page, pageSize, cancellationToken);
        return Ok(new ApiResponse<PagedMedicalDepartmentsResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Invalid medical department id.",
            });
        }

        var data = await _medicalDepartmentService.GetMedicalDepartmentByIdAsync(id, cancellationToken);
        if (data is null)
        {
            return NotFound(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Medical department not found.",
            });
        }

        return Ok(new ApiResponse<MedicalDepartmentResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMedicalDepartmentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            return BadRequest(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Create medical department failed.",
                Errors = new List<string> { "Department name is required." },
            });
        }

        var (ok, errors, data) = await _medicalDepartmentService.CreateMedicalDepartmentAsync(request, cancellationToken);
        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Create medical department failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<MedicalDepartmentResponse>
        {
            Success = true,
            Message = "Medical department created.",
            Data = data,
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MedicalDepartmentResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMedicalDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Invalid medical department id.",
            });
        }

        if (request.DepartmentName is not null && string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            return BadRequest(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Update medical department failed.",
                Errors = new List<string> { "Department name cannot be empty when provided." },
            });
        }

        var (ok, notFound, errors, data) = await _medicalDepartmentService.UpdateMedicalDepartmentAsync(id, request, cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Medical department not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<MedicalDepartmentResponse>
            {
                Success = false,
                Message = "Update medical department failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<MedicalDepartmentResponse>
        {
            Success = true,
            Message = "Medical department updated.",
            Data = data,
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Invalid medical department id.",
            });
        }

        var (ok, notFound, errors) = await _medicalDepartmentService.SoftDeleteMedicalDepartmentAsync(id, cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                Message = "Medical department not found.",
            });
        }

        if (!ok)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Delete medical department failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Medical department deleted (soft).",
        });
    }
}
