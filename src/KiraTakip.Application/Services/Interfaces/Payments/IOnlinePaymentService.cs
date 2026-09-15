using KiraTakip.Models.Dtos.OnlinePayment;

namespace KiraTakip.Services.Interfaces.Payments;

public interface IOnlinePaymentService
{
    /// <summary>
    /// Sanal POS işlemini başlatır (yalnız "initiate" ucu — bkz. İç Faz 6 kapsam kararı).
    /// </summary>
    Task<InitiateOnlinePaymentResult> InitiateAsync(
        InitiateOnlinePaymentInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı Paratika'dan RETURNURL ile döndüğünde çağrılır — QUERYTRANSACTION ile gerçek
    /// sonucu sorgular ve başarılıysa tek bir Approved ödeme oluşturur (İç Faz 7 kapsamı).
    /// Aynı işlem tekrar çağrılırsa (sayfa yenileme) idempotent no-op olarak davranır; hata
    /// fırlatmaz. İdempotent, çoklu-instance güvenli hale getirilmesi (background worker +
    /// NOTIFICATIONURL) İç Faz 8'dedir.
    /// </summary>
    Task<CompleteOnlinePaymentResult> CompleteAsync(
        CompleteOnlinePaymentInput input,
        CancellationToken cancellationToken = default);
}
