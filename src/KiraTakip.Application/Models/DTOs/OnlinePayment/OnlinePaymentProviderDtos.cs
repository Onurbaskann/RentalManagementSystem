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

public record CreatePaymentSessionRequest(
    string MerchantPaymentId,
    decimal Amount,
    string Currency);

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
