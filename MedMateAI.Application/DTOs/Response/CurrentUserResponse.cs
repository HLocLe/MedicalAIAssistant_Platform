namespace MedMateAI.Application.DTOs.Response;

public sealed class CurrentUserResponse
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
