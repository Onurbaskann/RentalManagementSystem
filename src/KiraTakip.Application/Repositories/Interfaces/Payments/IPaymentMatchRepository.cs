using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Payments;

public interface IPaymentMatchRepository : IRepositoryBase<PaymentMatch>
{
    Task<bool> ExistsForPaymentAsync(int paymentId);
    Task<bool> ExistsForBankTransactionAsync(int bankTransactionId);
    Task<PaymentMatch?> GetWithDetailsAsync(int matchId);
    Task RemoveAsync(PaymentMatch match);
}
