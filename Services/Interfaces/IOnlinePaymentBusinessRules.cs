using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IOnlinePaymentBusinessRules : IBusinessRules
{
    void EnsureAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount);

    /// <summary>
    /// Durum geçiş kuralını doğrular — yalnız Pending/Unknown'dan terminal duruma
    /// (Approved/Failed/Cancelled) geçilebilir, terminal bir durumdan geri dönüş yoktur.
    /// Saf fonksiyon; İç Faz 6'da henüz hiçbir yerden çağrılmaz, İç Faz 7/8 için hazırlanır.
    /// </summary>
    bool IsValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to);

    void EnsureValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to);
}
