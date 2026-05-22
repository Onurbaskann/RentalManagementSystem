using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KiraTakip.Repositories;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _ctx;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(ApplicationDbContext ctx)
    {
        _ctx = ctx;
        _dbSet = ctx.Set<T>();
    }

    // ── Entity dönen (tracked) ────────────────────────────────────────────
    public virtual async Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> q = _dbSet;
        if (include != null) q = include(q);
        return await q.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> q = _dbSet;
        if (include != null) q = include(q);
        return await q.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> q = _dbSet;
        if (include != null) q = include(q);
        if (filter != null) q = q.Where(filter);
        return await q.ToListAsync();
    }

    // ── Projeksiyon (AsNoTracking) ────────────────────────────────────────
    public virtual async Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<T, TResult>> selector)
        => await _dbSet.AsNoTracking()
                       .Where(e => e.Id == id)
                       .Select(selector)
                       .FirstOrDefaultAsync();

    public virtual async Task<TResult?> GetAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        => await _dbSet.AsNoTracking()
                       .Where(predicate)
                       .Select(selector)
                       .FirstOrDefaultAsync();

    public virtual async Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<T, bool>>? filter, Expression<Func<T, TResult>> selector)
    {
        IQueryable<T> q = _dbSet.AsNoTracking();
        if (filter != null) q = q.Where(filter);
        return await q.Select(selector).ToListAsync();
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => _dbSet.AsNoTracking().AnyAsync(predicate);

    public virtual Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        => filter == null
            ? _dbSet.AsNoTracking().CountAsync()
            : _dbSet.AsNoTracking().CountAsync(filter);

    // ── Yazma ─────────────────────────────────────────────────────────────
    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public Task UpdateAsync(T entity) => Task.CompletedTask;

    public async Task DeleteAsync(int id, bool hardDelete = false)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return;

        if (hardDelete)
            _dbSet.Remove(entity);
        else
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
        }
    }
}
