using Castle.DynamicProxy;
using KiraTakip.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KiraTakip.Infrastructure.Transactions;

/// <summary>
/// ITransactionalService implement eden servislerin async metot çağrılarını
/// DB transaction'ı içinde çalıştırır. Nested call'larda mevcut transaction'a join eder.
/// </summary>
public class TransactionInterceptor : IAsyncInterceptor
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TransactionInterceptor> _logger;

    public TransactionInterceptor(ApplicationDbContext db, ILogger<TransactionInterceptor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public void InterceptSynchronous(IInvocation invocation)
    {
        // Senkron metotlar — proje genelinde async kullanılıyor, fallback olarak destek
        if (_db.Database.CurrentTransaction != null)
        {
            invocation.Proceed();
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        strategy.Execute(() =>
        {
            using var tx = _db.Database.BeginTransaction();
            try
            {
                invocation.Proceed();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public void InterceptAsynchronous(IInvocation invocation)
    {
        invocation.ReturnValue = InternalInterceptAsync(invocation);
    }

    public void InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        invocation.ReturnValue = InternalInterceptAsync<TResult>(invocation);
    }

    private async Task InternalInterceptAsync(IInvocation invocation)
    {
        if (_db.Database.CurrentTransaction != null)
        {
            invocation.Proceed();
            await (Task)invocation.ReturnValue;
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                invocation.Proceed();
                await (Task)invocation.ReturnValue;
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning(ex, "Transaction rollback: {Type}.{Method}",
                    invocation.TargetType?.Name, invocation.Method.Name);
                throw;
            }
        });
    }

    private async Task<TResult> InternalInterceptAsync<TResult>(IInvocation invocation)
    {
        if (_db.Database.CurrentTransaction != null)
        {
            invocation.Proceed();
            return await (Task<TResult>)invocation.ReturnValue;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                invocation.Proceed();
                var result = await (Task<TResult>)invocation.ReturnValue;
                await tx.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogWarning(ex, "Transaction rollback: {Type}.{Method}",
                    invocation.TargetType?.Name, invocation.Method.Name);
                throw;
            }
        });
    }
}
