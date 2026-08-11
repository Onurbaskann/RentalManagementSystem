using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Repositories;

public abstract class RepositoryBase<TEntity, TKey>(ApplicationDbContext ctx)
    : Repository<TEntity, TKey>(ctx, entity => entity.Id), IRepositoryBase<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : notnull
{
    public async Task AddAsync(TEntity entity) => await _dbSet.AddAsync(entity);

    public Task UpdateAsync(TEntity entity) => Task.CompletedTask;

    public async Task DeleteAsync(TKey id, bool hardDelete = false)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return;

        if (hardDelete)
        {
            _dbSet.Remove(entity);
            return;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
    }
}

public abstract class RepositoryBase<TEntity>(ApplicationDbContext ctx)
    : RepositoryBase<TEntity, int>(ctx), IRepositoryBase<TEntity>
    where TEntity : BaseEntity;