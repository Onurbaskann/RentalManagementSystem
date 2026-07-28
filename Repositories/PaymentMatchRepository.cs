using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PaymentMatchRepository(ApplicationDbContext context)
    : BaseRepository<PaymentMatch>(context), IPaymentMatchRepository
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
