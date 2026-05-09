using System.Security.Claims;
using AutoMapper;
using MedMateAI.Application.IService;
using MedMateAI.Domain.Entities;
using MedMateAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace MedMateAI.Infrastructure.Auth.Services;

public sealed class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public UserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var id))
        {
            return null;
        }

        return await GetUserByIdAsync(id, cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        return appUser is null ? null : _mapper.Map<User>(appUser);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var appUser = await _userManager.FindByEmailAsync(email.Trim());
        return appUser is null ? null : _mapper.Map<User>(appUser);
    }

    public async Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default)
        => await _userManager.FindByEmailAsync(email.Trim()) is not null;

    public async Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        return appUser is not null && await _userManager.IsInRoleAsync(appUser, role);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Array.Empty<string>();
        }

        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(appUser);
        return roles.ToArray();
    }
}

