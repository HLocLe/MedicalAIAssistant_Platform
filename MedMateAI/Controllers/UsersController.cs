using MedMateAI.Application.DTOs;
using MedMateAI.Application.DTOs.Response;
using MedMateAI.Application.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var current = await _userService.GetCurrentUserAsync(cancellationToken);
        if (current is null)
        {
            return Unauthorized(new ApiResponse<CurrentUserResponse>
            {
                Success = false,
                Message = "Unauthorized",
            });
        }

        var roles = await _userService.GetRolesAsync(current.IdentityId, cancellationToken);

        var data = new CurrentUserResponse
        {
            UserId = current.IdentityId,
            Email = current.Email,
            Name = current.DisplayName,
            Roles = roles,
        };

        return Ok(new ApiResponse<CurrentUserResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }
}
