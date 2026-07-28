namespace KiraTakip.Repositories.Interfaces;

public interface IPaymentMatchRepository : IBaseRepository<PaymentMatch>
{
    Task<bool> ExistsForPaymentAsync(int paymentId);
    Task<bool> ExistsForBankTransactionAsync(int bankTransactionId);
    Task<PaymentMatch?> GetWithDetailsAsync(int matchId);
    Task RemoveAsync(PaymentMatch match);
}
