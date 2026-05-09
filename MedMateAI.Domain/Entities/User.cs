using MedMateAI.Domain.Enums;

namespace MedMateAI.Domain.Entities;

public sealed class User
{
    public Guid IdentityId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Address { get; set; }

    public Gender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}

