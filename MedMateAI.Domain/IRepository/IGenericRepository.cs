using MedMateAI.Domain.Entities;

namespace MedMateAI.Domain.Repository;

public interface IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
