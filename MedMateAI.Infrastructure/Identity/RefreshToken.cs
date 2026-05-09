namespace MedMateAI.Infrastructure.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime AddedDate { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
