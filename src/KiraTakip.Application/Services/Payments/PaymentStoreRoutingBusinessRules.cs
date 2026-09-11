using KiraTakip.Domain.Payments;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Payments;

namespace KiraTakip.Services.Payments;

public class PaymentStoreRoutingBusinessRules(
    IChargeTypeRepository chargeTypeRepository,
    IPropertyRepository propertyRepository,
    IUnitRepository unitRepository,
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

        if (input.PropertyId.HasValue)
            Guard.NotFound(
                await propertyRepository.GetByIdAsync(input.PropertyId.Value),
                "Taşınmaz bulunamadı.",
                "PAYMENT_ROUTING_PROPERTY_NOT_FOUND");

        if (input.UnitId.HasValue)
            Guard.NotFound(
                await unitRepository.GetByIdAsync(input.UnitId.Value),
                "Birim bulunamadı.",
                "PAYMENT_ROUTING_UNIT_NOT_FOUND");

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
            !PaymentRoutingPolicy.CanDeactivateOverride(
                routing.PropertyId,
                routing.UnitId),
            "Genel mağaza yönlendirmesi pasifleştirilemez; yalnız başka mağazaya güncellenebilir.",
            "PAYMENT_ROUTING_DEFAULT_CANNOT_DEACTIVATE");
        return routing;
    }

    private static void ValidateScope(UpsertPaymentStoreRoutingInput input)
    {
        Guard.InvalidField(
            !PaymentRoutingPolicy.IsDefinedScope(input.Scope),
            nameof(input.Scope),
            "Geçersiz yönlendirme kapsamı.",
            "PAYMENT_ROUTING_SCOPE_INVALID");

        Guard.InvalidField(
            !PaymentRoutingPolicy.HasValidScopeSelection(
                input.Scope,
                input.PropertyId,
                input.UnitId),
            input.Scope == PaymentRoutingScope.Unit ? nameof(input.UnitId) : nameof(input.PropertyId),
            "Yönlendirme kapsamı ile taşınmaz/birim seçimi uyumlu değil.",
            "PAYMENT_ROUTING_SCOPE_INVALID");
    }
}
