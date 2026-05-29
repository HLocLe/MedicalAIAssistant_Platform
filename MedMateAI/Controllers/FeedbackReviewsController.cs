using MedMateAI.Application.DTOs.Common;
using MedMateAI.Application.DTOs.FeedbackReviews.Requests;
using MedMateAI.Application.DTOs.FeedbackReviews.Responses;
using MedMateAI.Application.IService;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/feedback-reviews")]
public sealed class FeedbackReviewsController : ControllerBase
{
    private const string UnauthenticatedError = "User is not authenticated.";

    private readonly IFeedbackReviewService _feedbackReviewService;

    public FeedbackReviewsController(IFeedbackReviewService feedbackReviewService)
    {
        _feedbackReviewService = feedbackReviewService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FeedbackReviewResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FeedbackReviewResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] PaginationQuery query,
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] int? rating,
        CancellationToken cancellationToken = default)
    {
        if (facilityId.HasValue && facilityId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<PagedResponse<FeedbackReviewResponse>>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        if (userId.HasValue && userId.Value == Guid.Empty)
        {
            return BadRequest(new ApiResponse<PagedResponse<FeedbackReviewResponse>>
            {
                Success = false,
                Message = "Invalid user id.",
            });
        }

        if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
        {
            return BadRequest(new ApiResponse<PagedResponse<FeedbackReviewResponse>>
            {
                Success = false,
                Message = "Invalid rating filter.",
                Errors = new List<string> { "Rating filter must be between 1 and 5." },
            });
        }

        var data = await _feedbackReviewService.ListFeedbackReviewsAsync(
            query.PageNumber,
            query.PageSize,
            facilityId,
            userId,
            status,
            rating,
            cancellationToken);

        return Ok(new ApiResponse<PagedResponse<FeedbackReviewResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("facility/{facilityId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FeedbackReviewResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FeedbackReviewResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListByFacility(
        Guid facilityId,
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (facilityId == Guid.Empty)
        {
            return BadRequest(new ApiResponse<PagedResponse<FeedbackReviewResponse>>
            {
                Success = false,
                Message = "Invalid medical facility id.",
            });
        }

        var data = await _feedbackReviewService.ListApprovedFacilityReviewsAsync(
            facilityId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Ok(new ApiResponse<PagedResponse<FeedbackReviewResponse>>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Invalid feedback review id.",
            });
        }

        var data = await _feedbackReviewService.GetFeedbackReviewByIdAsync(id, cancellationToken);
        if (data is null)
        {
            return NotFound(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Feedback review not found.",
            });
        }

        return Ok(new ApiResponse<FeedbackReviewResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeedbackReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Create feedback review failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        if (request.FacilityId == Guid.Empty)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Create feedback review failed.",
                Errors = new List<string> { "FacilityId is required." },
            });
        }

        var (ok, errors, data) = await _feedbackReviewService.CreateFeedbackReviewAsync(request, cancellationToken);
        if (!ok || data is null)
        {
            var errorList = errors.ToList();

            if (errorList.Contains(UnauthenticatedError, StringComparer.Ordinal))
            {
                return Unauthorized(new ApiResponse<FeedbackReviewResponse>
                {
                    Success = false,
                    Message = "Unauthorized",
                    Errors = errorList,
                });
            }

            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Create feedback review failed.",
                Errors = errorList,
            });
        }

        return Ok(new ApiResponse<FeedbackReviewResponse>
        {
            Success = true,
            Message = "Feedback review created.",
            Data = data,
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFeedbackReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Invalid feedback review id.",
            });
        }

        if (request is null)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Update feedback review failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        var (ok, notFound, errors, data) = await _feedbackReviewService.UpdateFeedbackReviewAsync(
            id,
            request,
            cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Feedback review not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Update feedback review failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<FeedbackReviewResponse>
        {
            Success = true,
            Message = "Feedback review updated.",
            Data = data,
        });
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackReviewResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateFeedbackReviewStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Invalid feedback review id.",
            });
        }

        if (request is null)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Update feedback review status failed.",
                Errors = new List<string> { "Request body is required." },
            });
        }

        var (ok, notFound, errors, data) = await _feedbackReviewService.UpdateFeedbackReviewStatusAsync(
            id,
            request,
            cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Feedback review not found.",
            });
        }

        if (!ok || data is null)
        {
            return BadRequest(new ApiResponse<FeedbackReviewResponse>
            {
                Success = false,
                Message = "Update feedback review status failed.",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<FeedbackReviewResponse>
        {
            Success = true,
            Message = "Feedback review status updated.",
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
                Message = "Invalid feedback review id.",
            });
        }

        var (ok, notFound, errors) = await _feedbackReviewService.SoftDeleteFeedbackReviewAsync(
            id,
            cancellationToken);

        if (notFound)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = "Feedback review not found.",
                Data = false,
            });
        }

        if (!ok)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Delete feedback review failed.",
                Errors = errors.ToList(),
                Data = false,
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Message = "Feedback review deleted (soft).",
            Data = true,
        });
    }
}
