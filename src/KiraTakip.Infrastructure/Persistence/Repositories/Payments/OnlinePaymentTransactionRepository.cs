using KiraTakip.Data;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Payments;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Payments;

public class OnlinePaymentTransactionRepository(ApplicationDbContext context)
    : RepositoryBase<OnlinePaymentTransaction>(context), IOnlinePaymentTransactionRepository
{
    public Task<bool> HasActiveAttemptAsync(int chargeLineItemId, CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking().AnyAsync(
            transaction => transaction.ChargeLineItemId == chargeLineItemId
                && transaction.Status == OnlinePaymentTransactionStatus.Pending,
            cancellationToken);

    public Task<OnlinePaymentTransaction?> GetByMerchantPaymentIdAsync(
        string merchantPaymentId,
        CancellationToken cancellationToken = default)
        => _dbSet
            .Include(transaction => transaction.ChargeLineItem)
                .ThenInclude(lineItem => lineItem.Charge)
            .FirstOrDefaultAsync(
                transaction => transaction.MerchantPaymentId == merchantPaymentId,
                cancellationToken);
}
