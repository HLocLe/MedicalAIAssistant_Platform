namespace MedMateAI.Application.IService;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        string userId,
        string email,
        string? displayName,
        IReadOnlyList<string> roles);

    (string RefreshToken, DateTimeOffset ExpiresAtUtc) CreateRefreshToken();
}
