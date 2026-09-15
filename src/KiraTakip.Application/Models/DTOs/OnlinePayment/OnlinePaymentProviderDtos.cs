namespace KiraTakip.Models.Dtos.OnlinePayment;

/// <summary>
/// Provider-nötr mağaza hesap bilgisi. Provider sınıfı EF entity/repository bilmez —
/// yalnız bu DTO üzerinden çalışır (ana plan §6 kararı). Secret zaten çözülmüş halde gelir.
/// </summary>
public record PaymentProviderAccount(
    string ProviderCode,
    string MerchantId,
    string MerchantUser,
    string MerchantPassword,
    string Currency);

/// <summary>
/// SESSIONTOKEN isteği için gerekli alanlar (Paratika API v2 dokümanı — kullanıcı tarafından
/// doğrulanmış ham metin, 2026-09-11). CustomerCode/Name/Email/Phone kaynağı Tenant entity'sinin
/// TenantNo/Name/Email/Phone alanları — provider sınıfı Tenant/EF bilmediği için bu alanlar
/// orkestrasyon servisi tarafından geçirilir. ReturnUrl/SessionType gibi Paratika'ya özgü,
/// ortam bazlı sabit config değerleri ise (ParatikaOptions) bilinçli olarak bu DTO'da YOK —
/// provider kendi ayarını kendi okur, ortak servis provider-config'e bağımlı olmaz.
/// </summary>
public record CreatePaymentSessionRequest(
    string MerchantPaymentId,
    decimal Amount,
    string Currency,
    string CustomerCode,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone);

public record CreatePaymentSessionResult(
    bool IsSuccessful,
    string? ProviderTransactionId,
    string? SessionToken,
    DateTime? SessionExpiresAt,
    string? ResponseCode,
    string? TransactionStatus,
    string? ErrorCode,
    string? SafeMessage);

public record PaymentInquiryRequest(
    string MerchantPaymentId);

public record PaymentInquiryResult(
    bool IsSuccessful,
    string? ProviderTransactionId,
    string? ResponseCode,
    string? TransactionStatus,
    string? ErrorCode,
    string? SafeMessage);

public record PaymentCallbackRequest(
    IReadOnlyDictionary<string, string> Fields);

public record PaymentCallbackResult(
    bool IsValid,
    string? MerchantPaymentId,
    string? ProviderTransactionId,
    string? ResponseCode,
    string? TransactionStatus,
    string? ErrorCode,
    string? SafeMessage);
