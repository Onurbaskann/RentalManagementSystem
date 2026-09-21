using Castle.DynamicProxy;
using KiraTakip.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace KiraTakip.Infrastructure.Transactions;

/// <summary>
/// ITransactionalService implement eden servislerin tüm metotlarını, seçici servislerin
/// ise yalnız [Transactional] metotlarını DB transaction'ı içinde çalıştırır.
/// Nested call'larda mevcut transaction'a join eder.
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
        if (!ShouldUseTransaction(invocation))
        {
            invocation.Proceed();
            return;
        }

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
        if (!ShouldUseTransaction(invocation))
        {
            invocation.Proceed();
            await GetTask(invocation);
            return;
        }

        if (_db.Database.CurrentTransaction != null)
        {
            invocation.Proceed();
            await GetTask(invocation);
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                invocation.Proceed();
                await GetTask(invocation);
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
        if (!ShouldUseTransaction(invocation))
        {
            invocation.Proceed();
            return await GetTask<TResult>(invocation);
        }

        if (_db.Database.CurrentTransaction != null)
        {
            invocation.Proceed();
            return await GetTask<TResult>(invocation);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                invocation.Proceed();
                var result = await GetTask<TResult>(invocation);
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

    private static bool ShouldUseTransaction(IInvocation invocation)
    {
        if (invocation.InvocationTarget is ITransactionalService)
            return true;

        var targetType = invocation.InvocationTarget?.GetType() ?? invocation.TargetType;
        var targetMethod = invocation.MethodInvocationTarget
            ?? targetType?.GetMethod(
                invocation.Method.Name,
                invocation.Method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());

        if (targetMethod?.GetCustomAttribute<TransactionalAttribute>(inherit: true) is not null)
            return true;

        // Attribute kullanan servislerde yalnız işaretli metotlar transaction'a girer.
        // Attribute kullanmayan ve interceptor'a doğrudan bağlanan eski proxy'lerde ise
        // geriye dönük olarak tüm metotlar transaction davranışını korur.
        var hasSelectiveTransactionMethods = targetType?
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Any(method => method.GetCustomAttribute<TransactionalAttribute>(inherit: true) is not null) == true;

        return !hasSelectiveTransactionMethods;
    }

    private static Task GetTask(IInvocation invocation)
        => invocation.ReturnValue as Task
           ?? throw new InvalidOperationException(
               $"{invocation.TargetType?.Name}.{invocation.Method.Name} Task döndürmedi.");

    private static Task<TResult> GetTask<TResult>(IInvocation invocation)
        => invocation.ReturnValue as Task<TResult>
           ?? throw new InvalidOperationException(
               $"{invocation.TargetType?.Name}.{invocation.Method.Name} Task<{typeof(TResult).Name}> döndürmedi.");
}
