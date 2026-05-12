namespace MedMateAI.Application.DTOs.Response;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public IEnumerable<string> Roles { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
