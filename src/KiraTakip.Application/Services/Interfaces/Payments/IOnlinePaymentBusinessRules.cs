using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Services.Interfaces.Payments;

public interface IOnlinePaymentBusinessRules : IBusinessRules
{
    void EnsureAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount);

    /// <summary>
    /// Durum geçiş kuralını doğrular — yalnız Pending/Unknown'dan terminal duruma
    /// (Approved/Failed/Cancelled) geçilebilir, terminal bir durumdan geri dönüş yoktur.
    /// Saf fonksiyon; İç Faz 7'de QUERYTRANSACTION tamamlama akışında kullanılır.
    /// </summary>
    bool IsValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to);

    void EnsureValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to);

    /// <summary>
    /// Paratika'nın ham responseCode/transactionStatus alanlarını normalize eder (ana plan §5.4):
    /// AP→Approved, FA/CA→Failed, VD→Cancelled, IP→Pending, MR/diğer→Unknown. Saf fonksiyon.
    /// </summary>
    OnlinePaymentTransactionStatus NormalizeProviderStatus(string? responseCode, string? transactionStatus);
}
