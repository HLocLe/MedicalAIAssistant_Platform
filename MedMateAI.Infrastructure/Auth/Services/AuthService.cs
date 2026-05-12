using MedMateAI.Application.DTOs.Request;
using MedMateAI.Application.DTOs.Response;
using MedMateAI.Application.IService;
using MedMateAI.Infrastructure.Auth.Providers;
using MedMateAI.Infrastructure.Auth.Security;
using MedMateAI.Infrastructure.Identity;
using MedMateAI.Infrastructure;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace MedMateAI.Infrastructure.Auth.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan PasswordResetOtpLifetime = TimeSpan.FromMinutes(1);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IMemoryCache _cache;
    private readonly IEmailOtpSender _emailOtpSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator,
        IMemoryCache cache,
        IEmailOtpSender emailOtpSender,
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _cache = cache;
        _emailOtpSender = emailOtpSender;
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _configuration = configuration;
    }
    
    //
    public async Task<(bool Succeeded, IEnumerable<string> Errors, AuthResponse? Result)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.Password, request.confirmPassword, StringComparison.Ordinal))
        {
            return (false, new[] { "Password and confirmation do not match." }, null);
        }

        var userName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email : request.UserName;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Address = request.Address,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);

        if (!identityResult.Succeeded)
        {
            return (false, identityResult.Errors.Select(e => e.Description), null);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, "User");
        
        if (!addRoleResult.Succeeded)
        {
            return (false, addRoleResult.Errors.Select(e => e.Description), null);
        }

        return (true, Array.Empty<string>(), new AuthResponse
        {
            Email = user.Email ?? string.Empty,
            UserId = user.Id,
            ExpiresAtUtc = default,
        });
    }
    
    //
    public async Task<(bool Succeeded, AuthResponse? Result)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return (false, null);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, null);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return (false, null);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        var (token, expires) = _jwtTokenGenerator.CreateAccessToken(
            user.Id.ToString(),
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray());

        var (refreshToken, refreshExpires) = _jwtTokenGenerator.CreateRefreshToken();
        var refreshTokenHash = RefreshTokenHasher.Sha256Hex(refreshToken);
        var utcNow = DateTime.UtcNow;

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenHash,
            UserId = user.Id,
            ExpiresAt = refreshExpires.UtcDateTime,
            IsUsed = false,
            IsRevoked = false,
            AddedDate = utcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            httpContext.Response.Cookies.Append(
                "refreshToken",
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshExpires,
                    Path = "/",
                });
        }

        return (true, new AuthResponse
        {
            AccessToken = token,
            Email = user.Email ?? string.Empty,
            UserId = user.Id,
            Roles = roles.ToArray(),
            ExpiresAtUtc = expires,
        });
    }
    
    //
    public async Task<(bool Succeeded, IEnumerable<string> Errors, AuthResponse? Result)> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            return (false, new[] { "Credential is required." }, null);
        }

        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return (false, new[] { "Google ClientId is not configured." }, null);
        }

        // Frontend may send a JSON string with quotes; normalize it.
        var credential = request.Credential.Trim().Trim('"');

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId },
                });
        }
        catch
        {
            return (false, Array.Empty<string>(), null);
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return (false, new[] { "Google account does not provide an email." }, null);
        }

        var email = payload.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = payload.Name,
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return (false, createResult.Errors.Select(e => e.Description), null);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!addRoleResult.Succeeded)
            {
                return (false, addRoleResult.Errors.Select(e => e.Description), null);
            }
        }

    
        var roles = await _userManager.GetRolesAsync(user);

        var (token, expires) = _jwtTokenGenerator.CreateAccessToken(
            user.Id.ToString(),
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray());

        var (refreshToken, refreshExpires) = _jwtTokenGenerator.CreateRefreshToken();
        var refreshTokenHash = RefreshTokenHasher.Sha256Hex(refreshToken);
        var utcNow = DateTime.UtcNow;

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenHash,
            UserId = user.Id,
            ExpiresAt = refreshExpires.UtcDateTime,
            IsUsed = false,
            IsRevoked = false,
            AddedDate = utcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            httpContext.Response.Cookies.Append(
                "refreshToken",
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshExpires,
                    Path = "/",
                });
        }

        return (true, Array.Empty<string>(), new AuthResponse
        {
            AccessToken = token,
            Email = user.Email ?? string.Empty,
            UserId = user.Id,
            ExpiresAtUtc = expires,
        });
    }
    
    //
    public async Task<(bool Succeeded, AuthResponse? Result)> RefreshAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null ||
            !httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            return (false, null);
        }

        var hash = RefreshTokenHasher.Sha256Hex(refreshToken.Trim());
        var utcNow = DateTime.UtcNow;

        var existing = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Token == hash && !x.IsRevoked && !x.IsUsed && x.ExpiresAt > utcNow,
                cancellationToken)
            ;

        if (existing is null)
        {
            return (false, null);
        }

        var user = existing.User;
        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, null);
        }

        var roles = await _userManager.GetRolesAsync(user);

        var (accessToken, accessExpires) = _jwtTokenGenerator.CreateAccessToken(
            user.Id.ToString(),
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray());

        return (true, new AuthResponse
        {
            AccessToken = accessToken,
            Email = user.Email ?? string.Empty,
            UserId = user.Id,
            ExpiresAtUtc = accessExpires,
        });
    }
    
    //
    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (false, new[] { "Email is required." });
        }

        var email = request.Email.Trim();
        var cacheKey = GetPasswordResetCacheKey(email);

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return (true, Array.Empty<string>());
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        var (sent, otp) = await _emailOtpSender.SendOtpEmailAsync(email, cancellationToken);
        if (!sent || otp is null)
        {
            return (false, new[] { "Unable to send OTP email. Please try again later." });
        }

        var entry = new PasswordResetOtpCacheEntry
        {
            Otp = otp.Trim(),
            ResetToken = resetToken,
        };

        _cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = PasswordResetOtpLifetime,
        });

        return (true, Array.Empty<string>());
    }

    //
    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordWithOtpAsync(
        ChangePasswordWithOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (false, new[] { "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Otp))
        {
            return (false, new[] { "OTP is required." });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return (false, new[] { "New password is required." });
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return (false, new[] { "New password and confirmation do not match." });
        }

        var email = request.Email.Trim();
        var cacheKey = GetPasswordResetCacheKey(email);

        if (!_cache.TryGetValue(cacheKey, out PasswordResetOtpCacheEntry? entry) || entry is null)
        {
            return (false, new[] { "OTP is invalid or expired." });
        }

        if (!string.Equals(entry.Otp, request.Otp.Trim(), StringComparison.Ordinal))
        {
            return (false, new[] { "OTP is invalid or expired." });
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return (false, new[] { "OTP is invalid or expired." });
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, entry.ResetToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            return (false, resetResult.Errors.Select(e => e.Description));
        }

        _cache.Remove(cacheKey);
        return (true, Array.Empty<string>());
    }

    private static string GetPasswordResetCacheKey(string email)
        => $"pwdreset:{email.Trim().ToLowerInvariant()}";

    
    private sealed class PasswordResetOtpCacheEntry
    {
        public required string Otp { get; init; }

        public required string ResetToken { get; init; }
    }
}

