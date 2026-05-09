using System.Security.Claims;
using MedMateAI.Application.DTOs;
using MedMateAI.Application.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrent()
    {
        var data = new CurrentUserResponse
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Name = User.FindFirst(ClaimTypes.Name)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
        };

        return Ok(new ApiResponse<CurrentUserResponse>
        {
            Success = true,
            Message = "OK",
            Data = data,
        });
    }
}
