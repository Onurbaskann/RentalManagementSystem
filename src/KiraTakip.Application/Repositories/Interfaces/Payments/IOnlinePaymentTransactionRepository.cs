using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Payments;

public interface IOnlinePaymentTransactionRepository : IRepositoryBase<OnlinePaymentTransaction>
{
    /// <summary>
    /// Aynı kalem için sonuçlanmamış (Pending) başka bir sanal POS denemesi var mı —
    /// varsa ikinci deneme oluşturulamaz (ana plan §5.3).
    /// </summary>
    Task<bool> HasActiveAttemptAsync(int chargeLineItemId, CancellationToken cancellationToken = default);
}
