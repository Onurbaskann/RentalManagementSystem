using KiraTakip.Data;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KiraTakip.Repositories;

public abstract class Repository<TEntity, TKey>(
    ApplicationDbContext ctx,
    Expression<Func<TEntity, TKey>> keySelector) : IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    protected readonly ApplicationDbContext _ctx = ctx;
    protected readonly DbSet<TEntity> _dbSet = ctx.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(
        TKey id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        IQueryable<TEntity> query = _dbSet;
        if (include != null) query = include(query);
        return await query.FirstOrDefaultAsync(CreateKeyPredicate(id));
    }

    public virtual async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        IQueryable<TEntity> query = _dbSet;
        if (include != null) query = include(query);
        return await query.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        IQueryable<TEntity> query = _dbSet;
        if (include != null) query = include(query);
        if (filter != null) query = query.Where(filter);
        return await query.ToListAsync();
    }

    public virtual async Task<TResult?> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector)
        => await _dbSet.AsNoTracking()
            .Where(CreateKeyPredicate(id))
            .Select(selector)
            .FirstOrDefaultAsync();

    public virtual async Task<TResult?> GetAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector)
        => await _dbSet.AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .FirstOrDefaultAsync();

    public virtual async Task<List<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, bool>>? filter,
        Expression<Func<TEntity, TResult>> selector)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();
        if (filter != null) query = query.Where(filter);
        return await query.Select(selector).ToListAsync();
    }

    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        => _dbSet.AsNoTracking().AnyAsync(predicate);

    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
        => filter == null
            ? _dbSet.AsNoTracking().CountAsync()
            : _dbSet.AsNoTracking().CountAsync(filter);

    protected static Task<PagedResult<TResult>> GetPagedResultAsync<TResult>(
        IQueryable<TEntity> countQuery,
        IQueryable<TResult> orderedItemsQuery,
        TableQuery query,
        CancellationToken cancellationToken = default)
        => PagedQuery.CreateAsync(
            countQuery,
            orderedItemsQuery,
            query,
            cancellationToken);

    protected static Task<PagedResult<TResult>> GetPagedResultAsync<TResult>(
        IQueryable<TEntity> countQuery,
        IQueryable<TResult> orderedItemsQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => PagedQuery.CreateAsync(
            countQuery,
            orderedItemsQuery,
            page,
            pageSize,
            cancellationToken);

    private Expression<Func<TEntity, bool>> CreateKeyPredicate(TKey id)
    {
        var equality = Expression.Equal(
            keySelector.Body,
            Expression.Constant(id, typeof(TKey)));

        return Expression.Lambda<Func<TEntity, bool>>(
            equality,
            keySelector.Parameters);
    }
}
