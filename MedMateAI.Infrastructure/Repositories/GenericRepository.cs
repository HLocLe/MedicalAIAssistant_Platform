using MedMateAI.Domain.Entities;
using MedMateAI.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace MedMateAI.Infrastructure.Repositories;

public sealed class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly ApplicationDbContext _context;
    
    private readonly DbSet<TEntity> _set;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _set = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync(new object?[] { id! }, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _set.AsNoTracking().ToListAsync(cancellationToken);
    }

    public void Add(TEntity entity) => _set.Add(entity);

    public void Update(TEntity entity) => _set.Update(entity);

    public void Remove(TEntity entity) => _set.Remove(entity);
}

