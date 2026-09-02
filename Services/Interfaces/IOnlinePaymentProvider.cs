using KiraTakip.Models.Dtos.OnlinePayment;

namespace KiraTakip.Services.Interfaces;

/// <summary>
/// Sanal POS sağlayıcısı soyutu (ana plan §6). Provider seçimi
/// IBankaHareketiParser/AkbankCsvParser deseniyle aynı — DI'a AddSingleton ile
/// kaydedilir, tüketen servis IEnumerable&lt;IOnlinePaymentProvider&gt; enjekte edip
/// ProviderCode'a göre seçer. Provider sınıfı EF entity/repository bilmez.
/// </summary>
public interface IOnlinePaymentProvider
{
    string ProviderCode { get; }

    Task<CreatePaymentSessionResult> CreateSessionAsync(
        CreatePaymentSessionRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken);

    Task<PaymentInquiryResult> QueryAsync(
        PaymentInquiryRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken);

    Task<PaymentCallbackResult> ValidateCallbackAsync(
        PaymentCallbackRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken);
}
