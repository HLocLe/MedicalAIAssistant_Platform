using MedMateAI.Domain.Entities;

namespace MedMateAI.Domain.Repository;

public interface IDoctorInvitationRepository : IGenericRepository<DoctorInvitation>
{
    Task<DoctorInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<DoctorInvitation?> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<DoctorInvitation?> GetPendingByDoctorIdAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default);
}
