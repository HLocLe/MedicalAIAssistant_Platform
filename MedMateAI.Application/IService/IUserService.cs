using MedMateAI.Domain.Entities;

namespace MedMateAI.Application.IService;

public interface IUserService
{
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}

