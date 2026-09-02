using KiraTakip.Models.Dtos.OnlinePayment;

namespace KiraTakip.Services.Interfaces;

public interface IOnlinePaymentService
{
    /// <summary>
    /// Sanal POS işlemini başlatır (yalnız "initiate" ucu — bkz. İç Faz 6 kapsam kararı).
    /// Callback ayrıştırma İç Faz 7'de, idempotent tamamlama İç Faz 8'de eklenecek.
    /// </summary>
    Task<InitiateOnlinePaymentResult> InitiateAsync(
        InitiateOnlinePaymentInput input,
        CancellationToken cancellationToken = default);
}
