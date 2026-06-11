using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using MedMateAI.Application.IService;

namespace MedMateAI.Application.Service;

public sealed class InvitationTokenService : IInvitationTokenService
{
    private const int TokenByteLength = 32;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public string HashToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Token is required.", nameof(rawToken));
        }

        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(bytes);
    }
}
