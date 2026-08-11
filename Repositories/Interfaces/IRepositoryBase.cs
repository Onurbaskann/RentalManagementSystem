namespace KiraTakip.Repositories.Interfaces;

public interface IRepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : notnull
{
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TKey id, bool hardDelete = false);
}

public interface IRepositoryBase<TEntity> : IRepositoryBase<TEntity, int>
    where TEntity : BaseEntity
{
}
