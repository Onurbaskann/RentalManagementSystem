using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Services.Interfaces.Payments;

public interface IPaymentStoreResolver
{
    Task<ResolvedPaymentStoreAccountDto> ResolveAsync(
        int chargeTypeId,
        int? spaceId,
        CancellationToken cancellationToken = default);
}
