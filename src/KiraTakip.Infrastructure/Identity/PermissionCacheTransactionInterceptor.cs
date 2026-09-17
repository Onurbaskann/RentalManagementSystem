using System.Data.Common;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KiraTakip.Services.Identity;

public class PermissionCacheTransactionInterceptor(
    IUserPermissionCache permissionCache,
    IPermissionCacheTransactionState transactionState,
    ILogger<PermissionCacheTransactionInterceptor>? logger = null) : DbTransactionInterceptor
{
    private readonly ILogger<PermissionCacheTransactionInterceptor> _logger =
        logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PermissionCacheTransactionInterceptor>.Instance;

    public override InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result)
    {
        transactionState.ClearAllPending();
        return base.TransactionStarting(connection, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        transactionState.ClearAllPending();
        return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        OnCommitted(eventData.TransactionId);
        base.TransactionCommitted(transaction, eventData);
    }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        OnCommitted(eventData.TransactionId);
        return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        OnTerminated(eventData.TransactionId);
        base.TransactionRolledBack(transaction, eventData);
    }

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        OnTerminated(eventData.TransactionId);
        return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
    {
        OnTerminated(eventData.TransactionId);
        base.TransactionFailed(transaction, eventData);
    }

    public override Task TransactionFailedAsync(
        DbTransaction transaction,
        TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        OnTerminated(eventData.TransactionId);
        return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
    }

    private void OnCommitted(Guid transactionId)
    {
        var userIds = transactionState.RemovePending(transactionId);
        if (userIds.Count == 0) return;

        var failedUsers = new List<string>();
        var succeededUsers = new List<string>();

        foreach (var userId in userIds)
        {
            try
            {
                permissionCache.Invalidate(userId);
                succeededUsers.Add(userId);
            }
            catch (Exception ex)
            {
                failedUsers.Add(userId);
                _logger.LogError(
                    ex,
                    "Post-commit permission cache invalidation failed for User '{UserId}' in Transaction '{TransactionId}'. Cache entry will remain active until TTL expires.",
                    userId,
                    transactionId);
            }
        }

        if (failedUsers.Count > 0)
        {
            _logger.LogWarning(
                "Transaction '{TransactionId}' committed successfully, but {FailedCount}/{TotalCount} user permission cache invalidation(s) failed. Succeeded users: [{SucceededUsers}], Failed users: [{FailedUsers}].",
                transactionId,
                failedUsers.Count,
                userIds.Count,
                string.Join(", ", succeededUsers),
                string.Join(", ", failedUsers));
        }
        else
        {
            _logger.LogDebug(
                "Transaction '{TransactionId}' successfully invalidated permission cache for {Count} user(s): [{Users}].",
                transactionId,
                succeededUsers.Count,
                string.Join(", ", succeededUsers));
        }
    }

    private void OnTerminated(Guid transactionId)
    {
        transactionState.ClearPending(transactionId);
    }
}
