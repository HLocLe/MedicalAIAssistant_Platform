using MedMateAI.Domain.Repository;

namespace MedMateAI.Domain.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    IMedicalFacilityRepository MedicalFacilities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
