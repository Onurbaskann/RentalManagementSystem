using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _ctx;

    public UnitOfWork(ApplicationDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _ctx.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_ctx.Database.CurrentTransaction is not null)
            return await operation(cancellationToken);

        var strategy = _ctx.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _ctx.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
