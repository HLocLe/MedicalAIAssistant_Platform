using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MedMateAI.Application.IService;
using MedMateAI.Infrastructure.Auth.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MedMateAI.Infrastructure.Auth.Providers;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        string userId,
        string email,
        string? displayName,
        IReadOnlyList<string> roles)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            claims.Add(new Claim(ClaimTypes.Name, displayName));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt);
    }

    public (string RefreshToken, DateTimeOffset ExpiresAtUtc) CreateRefreshToken()
    {
        var days = Math.Max(1, _options.RefreshTokenExpiryDays);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(days);

        var bytes = RandomNumberGenerator.GetBytes(48);
        var refreshToken = WebEncoders.Base64UrlEncode(bytes);

        return (refreshToken, expiresAt);
    }
}
