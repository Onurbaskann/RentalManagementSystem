using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class OnlinePaymentTransactionRepository(ApplicationDbContext context)
    : RepositoryBase<OnlinePaymentTransaction>(context), IOnlinePaymentTransactionRepository
{
    public Task<bool> HasActiveAttemptAsync(int chargeLineItemId, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().AnyAsync(
            transaction => transaction.ChargeLineItemId == chargeLineItemId
                && transaction.Status == OnlinePaymentTransactionStatus.Pending,
            cancellationToken);
}
