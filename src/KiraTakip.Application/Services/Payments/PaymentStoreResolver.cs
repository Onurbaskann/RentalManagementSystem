using KiraTakip.Domain.Payments;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Payments;

namespace KiraTakip.Services.Payments;

public class PaymentStoreResolver(IPaymentStoreRoutingRepository routingRepository)
    : IPaymentStoreResolver
{
    public async Task<ResolvedPaymentStoreAccountDto> ResolveAsync(
        int chargeTypeId,
        int unitId,
        CancellationToken cancellationToken = default)
    {
        var candidate = Guard.NotFound(
            await routingRepository.GetResolutionCandidateAsync(chargeTypeId, unitId, cancellationToken),
            "Birim bulunamadı.",
            "PAYMENT_ROUTING_UNIT_NOT_FOUND");

        Guard.Conflict(
            !candidate.RoutingId.HasValue,
            "Bu tahakkuk kalemi için ödeme mağazası yönlendirmesi bulunamadı.",
            "PAYMENT_ROUTING_NOT_FOUND");
        Guard.Conflict(
            !candidate.IsStoreActive,
            "Yönlendirilen mağaza aktif değil.",
            "PAYMENT_ROUTING_STORE_INACTIVE");
        Guard.Conflict(
            !StoreAccountPolicy.HasActiveAccount(candidate.ActiveAccountCount),
            "Yönlendirilen mağazanın aktif hesabı bulunmuyor.",
            "PAYMENT_ROUTING_ACTIVE_ACCOUNT_NOT_FOUND");
        Guard.Conflict(
            StoreAccountPolicy.HasAccountConflict(candidate.ActiveAccountCount),
            "Yönlendirilen mağazada birden fazla aktif hesap bulunuyor.",
            "PAYMENT_ROUTING_ACTIVE_ACCOUNT_CONFLICT");

        return new ResolvedPaymentStoreAccountDto(
            candidate.RoutingId!.Value,
            candidate.MatchedScope!.Value,
            chargeTypeId,
            candidate.UnitId,
            candidate.PropertyId,
            candidate.StoreId!.Value,
            candidate.StoreAccountId!.Value,
            candidate.ProviderCode!,
            candidate.Currency!);
    }
}
