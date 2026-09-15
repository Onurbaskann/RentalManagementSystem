using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.Payment;

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
    OnlinePaymentTransactionStatus Status,
    string? RedirectUrl);

/// <summary>
/// Paratika'dan RETURNURL ile dönüşü tamamlama girdisi. TenantId, dönen kullanıcının kimlik
/// bilgisinden alınır (IDOR koruması) — merchantPaymentId yalnız sorgu anahtarıdır, Paratika'nın
/// gönderdiği hiçbir başka alana (tutar, durum) güvenilmez; asıl karar QUERYTRANSACTION
/// sonucuna göre verilir (bkz. İç Faz 7 planı).
/// </summary>
public record CompleteOnlinePaymentInput(
    string MerchantPaymentId,
    int TenantId);

public record CompleteOnlinePaymentResult(
    OnlinePaymentTransactionStatus Status,
    int? PaymentAllocationId,
    int ChargeId);
