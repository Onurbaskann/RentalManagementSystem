using KiraTakip.Domain.Payments;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Payments;

namespace KiraTakip.Services.Payments;

public class PaymentStoreRoutingBusinessRules(
    IChargeTypeRepository chargeTypeRepository,
    ISpaceRepository spaceRepository,
    IStoreRepository storeRepository,
    IPaymentStoreRoutingRepository routingRepository) : IPaymentStoreRoutingBusinessRules
{
    public async Task EnsureUpsertAllowedAsync(UpsertPaymentStoreRoutingInput input)
    {
        Guard.NotFound(
            await chargeTypeRepository.GetByIdAsync(input.ChargeTypeId),
            "Borç tipi bulunamadı.",
            "PAYMENT_ROUTING_CHARGE_TYPE_NOT_FOUND");

        ValidateScope(input);

        if (input.SpaceId.HasValue)
        {
            var space = Guard.NotFound(
                await spaceRepository.GetNoTrackingAsync(input.SpaceId.Value),
                "Alan bulunamadı.",
                "PAYMENT_ROUTING_SPACE_NOT_FOUND");
            Guard.InvalidField(
                !space.IsActive,
                nameof(input.SpaceId),
                "Pasif alan seçilemez.",
                "PAYMENT_ROUTING_SPACE_INACTIVE");
        }

        var store = Guard.NotFound(
            await storeRepository.GetWithAccountsByIdAsync(input.StoreId),
            "Mağaza bulunamadı.",
            "PAYMENT_ROUTING_STORE_NOT_FOUND");
        Guard.InvalidField(
            !store.IsActive,
            nameof(input.StoreId),
            "Yalnız aktif bir mağaza seçilebilir.",
            "PAYMENT_ROUTING_STORE_INACTIVE");

        var activeAccountCount = store.Accounts.Count(account => account.IsActive);
        Guard.InvalidField(
            !StoreAccountPolicy.HasActiveAccount(activeAccountCount),
            nameof(input.StoreId),
            "Seçilen mağazanın aktif hesabı bulunmuyor.",
            "PAYMENT_ROUTING_ACTIVE_ACCOUNT_NOT_FOUND");
        Guard.InvalidField(
            StoreAccountPolicy.HasAccountConflict(activeAccountCount),
            nameof(input.StoreId),
            "Seçilen mağazada birden fazla aktif hesap bulunuyor.",
            "PAYMENT_ROUTING_ACTIVE_ACCOUNT_CONFLICT");
    }

    public async Task<PaymentStoreRouting> GetActiveOverrideAsync(int id)
    {
        var routing = Guard.NotFound(
            await routingRepository.GetTrackedByIdAsync(id),
            "Ödeme yönlendirmesi bulunamadı.",
            "PAYMENT_ROUTING_NOT_FOUND");
        Guard.Conflict(
            !routing.IsActive,
            "Ödeme yönlendirmesi zaten pasif.",
            "PAYMENT_ROUTING_ALREADY_INACTIVE");
        Guard.Conflict(
            !PaymentRoutingPolicy.CanDeactivateOverride(routing.SpaceId),
            "Genel mağaza yönlendirmesi pasifleştirilemez; yalnız başka mağazaya güncellenebilir.",
            "PAYMENT_ROUTING_DEFAULT_CANNOT_DEACTIVATE");
        return routing;
    }

    private static void ValidateScope(UpsertPaymentStoreRoutingInput input)
    {
        Guard.InvalidField(
            !PaymentRoutingPolicy.HasValidScopeSelection(input.SpaceId),
            nameof(input.SpaceId),
            "Geçersiz alan seçimi.",
            "PAYMENT_ROUTING_SCOPE_INVALID");
    }
}
