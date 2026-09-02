using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentStoreResolver
{
    Task<ResolvedPaymentStoreAccountDto> ResolveAsync(
        int chargeTypeId,
        int unitId,
        CancellationToken cancellationToken = default);
}
