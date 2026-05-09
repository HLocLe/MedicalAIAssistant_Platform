using MedMateAI.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MedMateAI.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public string? DisplayName { get; set; }

    public string? Address { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
