using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.Dtos.OnlinePayment;

/// <summary>
/// Sanal POS başlatma girdisi — kiracı ödeme akışı (<see cref="ReportTenantPaymentInput"/>)
/// ile aynı sahiplik/kapsam deseniyle doğrulanır (bkz. IOnlinePaymentService.InitiateAsync).
/// Kalem seçimi zorunludur — kiracı ekranlarında (İç Faz 4/5) kalem radio seçimi ödeme
/// yöntemi sekmelerinden bağımsız ve zaten zorunlu olduğu için burada auto-select yoktur.
/// </summary>
public record InitiateOnlinePaymentInput(
    int TenantId,
    int ChargeId,
    int ChargeLineItemId,
    decimal Amount,
    string InitiatedByUserId,
    PaymentAccessScopeInput AccessScope);

public record InitiateOnlinePaymentResult(
    int OnlinePaymentTransactionId,
    string ProviderCode,
    string MerchantPaymentId,
    string? SessionToken,
    DateTime? SessionExpiresAt,
    OnlinePaymentTransactionStatus Status);
