using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Enums;
using MedMateAI.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace MedMateAI.Infrastructure.Repositories;

public sealed class DoctorInvitationRepository
    : GenericRepository<DoctorInvitation>, IDoctorInvitationRepository
{
    private readonly ApplicationDbContext _context;

    public DoctorInvitationRepository(ApplicationDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<DoctorInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        return await _context.DoctorInvitations
            .Include(x => x.Doctor)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash && !x.IsDeleted,
                cancellationToken);
    }

    public async Task<DoctorInvitation?> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var utcNow = DateTime.UtcNow;

        return await _context.DoctorInvitations
            .Where(x =>
                x.Email == normalizedEmail
                && x.Status == DoctorInvitationStatus.Pending
                && x.UsedAt == null
                && x.RevokedAt == null
                && x.ExpiresAt > utcNow
                && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DoctorInvitation?> GetPendingByDoctorIdAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;

        return await _context.DoctorInvitations
            .Where(x =>
                x.DoctorId == doctorId
                && x.Status == DoctorInvitationStatus.Pending
                && x.UsedAt == null
                && x.RevokedAt == null
                && x.ExpiresAt > utcNow
                && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
