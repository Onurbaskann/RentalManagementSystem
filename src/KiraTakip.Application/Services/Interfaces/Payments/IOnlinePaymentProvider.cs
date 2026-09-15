using KiraTakip.Models.Dtos.OnlinePayment;

namespace KiraTakip.Services.Interfaces.Payments;

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

    /// <summary>
    /// Başarılı bir oturumdan sonra kullanıcının tarayıcısının yönlendirileceği hosted ödeme
    /// sayfası adresini üretir. Saf, senkron bir URL birleştirme — sağlayıcıya özgü format
    /// bilgisi yalnız provider'da yaşar, ortak servis bu formatı bilmez.
    /// </summary>
    string BuildHostedPaymentPageUrl(string sessionToken);
}
