using System.Linq.Expressions;

namespace KiraTakip.Repositories.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    // Entity dönen — sadece CRUD / business logic için (tracked)
    Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? include = null);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null);

    // Projeksiyon — okuma için (AsNoTracking)
    Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<T, TResult>> selector);
    Task<TResult?> GetAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector);
    Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<T, bool>>? filter, Expression<Func<T, TResult>> selector);

    // Yardımcılar
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

    // Yazma
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id, bool hardDelete = false);
}
