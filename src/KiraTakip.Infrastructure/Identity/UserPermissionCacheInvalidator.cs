using KiraTakip.Data;
using KiraTakip.Services.Interfaces.Identity;

namespace KiraTakip.Services.Identity;

public class UserPermissionCacheInvalidator(
    IUserPermissionCache permissionCache,
    ApplicationDbContext db,
    IPermissionCacheTransactionState transactionState) : IUserPermissionCacheInvalidator
{
    public void InvalidateAfterCommit(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var currentTx = db.Database.CurrentTransaction;
        if (currentTx == null)
        {
            permissionCache.Invalidate(userId);
        }
        else
        {
            transactionState.AddPending(currentTx.TransactionId, userId);
        }
    }

    public void InvalidateManyAfterCommit(IEnumerable<string> userIds)
    {
        if (userIds == null) return;

        var currentTx = db.Database.CurrentTransaction;
        if (currentTx == null)
        {
            permissionCache.InvalidateMany(userIds);
        }
        else
        {
            transactionState.AddPendingRange(currentTx.TransactionId, userIds);
        }
    }
}
