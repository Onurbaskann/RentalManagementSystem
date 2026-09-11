using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Payments;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Payments;

public class PaymentMatchRepository(ApplicationDbContext context)
    : RepositoryBase<PaymentMatch>(context), IPaymentMatchRepository
{
    public Task<bool> ExistsForPaymentAsync(int paymentId)
        => _dbSet.AsNoTracking().AnyAsync(match => match.PaymentAllocationId == paymentId);

    public Task<bool> ExistsForBankTransactionAsync(int bankTransactionId)
        => _dbSet.AsNoTracking().AnyAsync(match => match.BankTransactionId == bankTransactionId);

    public Task<PaymentMatch?> GetWithDetailsAsync(int matchId)
        => _dbSet
            .Include(match => match.BankTransaction)
            .Include(match => match.PaymentAllocation)
                .ThenInclude(payment => payment.Charge)
                    .ThenInclude(charge => charge.Unit)
            .FirstOrDefaultAsync(match => match.Id == matchId);

    public Task RemoveAsync(PaymentMatch match)
    {
        _dbSet.Remove(match);
        return Task.CompletedTask;
    }
}
