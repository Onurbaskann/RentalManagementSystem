using System.Linq.Expressions;

namespace KiraTakip.Repositories.Interfaces;

public interface IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(
        TKey id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);
    Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);

    Task<TResult?> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector);
    Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector);
    Task<List<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, bool>>? filter,
        Expression<Func<TEntity, TResult>> selector);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null);
}
