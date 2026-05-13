using MedMateAI.Application.DTOs;
using MedMateAI.Application.DTOs.Request;
using MedMateAI.Application.DTOs.Response;
using MedMateAI.Application.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedMateAI.Controllers;

[ApiController]
[Route("api/authentication")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, errors, result) = await _authService.RegisterAsync(request, cancellationToken);
        if (!succeeded)
        {
            return BadRequest(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Registration failed",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Registration succeeded",
            Data = result,
        });
    }

    [HttpPost("register/staff")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, errors, result) = await _authService.RegisterForStaffAsync(request, cancellationToken);
        if (!succeeded)
        {
            return BadRequest(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Staff registration failed",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Staff account created. Status is pending until approved.",
            Data = result,
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, result) = await _authService.LoginAsync(request, cancellationToken);
        if (!succeeded || result is null)
        {
            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Invalid email or password.",
            });
        }

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Login succeeded",
            Data = result,
        });
    }

    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, errors, result) = await _authService.LoginWithGoogleAsync(request, cancellationToken);
        if (!succeeded || result is null)
        { 
        
         if (errors.Any())
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Google login failed",
                    Errors = errors.ToList(),
                });
            }

            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Invalid Google credential.",
            });
        }

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Login succeeded",
            Data = result,
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var (succeeded, result) = await _authService.RefreshAccessTokenAsync(cancellationToken);
        if (!succeeded || result is null)
        {
            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "Refresh token missing, invalid, or expired.",
            });
        }

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Token refreshed.",
            Data = result,
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(cancellationToken);
        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Logged out.",
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, errors) = await _authService.ForgotPasswordAsync(request, cancellationToken);
        if (!succeeded)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Forgot password failed",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "If the email exists, an OTP has been sent.",
        });
    }

    [HttpPost("change-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordWithOtpRequest request, CancellationToken cancellationToken)
    {
        var (succeeded, errors) = await _authService.ChangePasswordWithOtpAsync(request, cancellationToken);
        if (!succeeded)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Change password failed",
                Errors = errors.ToList(),
            });
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Password changed successfully.",
        });
    }
}
