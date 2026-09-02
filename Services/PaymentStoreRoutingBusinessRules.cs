using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

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
            await storeRepository.GetByIdAsync(
                input.StoreId,
                query => query.Include(item => item.Accounts)),
            "Mağaza bulunamadı.",
            "PAYMENT_ROUTING_STORE_NOT_FOUND");
        Guard.InvalidField(
            !store.IsActive,
            nameof(input.StoreId),
            "Yalnız aktif bir mağaza seçilebilir.",
            "PAYMENT_ROUTING_STORE_INACTIVE");

        var activeAccountCount = store.Accounts.Count(account => account.IsActive);
        Guard.InvalidField(
            activeAccountCount == 0,
            nameof(input.StoreId),
            "Seçilen mağazanın aktif hesabı bulunmuyor.",
            "PAYMENT_ROUTING_ACTIVE_ACCOUNT_NOT_FOUND");
        Guard.InvalidField(
            activeAccountCount > 1,
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
            routing.PropertyId == null && routing.UnitId == null,
            "Genel mağaza yönlendirmesi pasifleştirilemez; yalnız başka mağazaya güncellenebilir.",
            "PAYMENT_ROUTING_DEFAULT_CANNOT_DEACTIVATE");
        return routing;
    }

    private static void ValidateScope(UpsertPaymentStoreRoutingInput input)
    {
        Guard.InvalidField(
            !Enum.IsDefined(input.Scope),
            nameof(input.Scope),
            "Geçersiz yönlendirme kapsamı.",
            "PAYMENT_ROUTING_SCOPE_INVALID");

        var valid = input.Scope switch
        {
            PaymentRoutingScope.General => input.PropertyId == null && input.UnitId == null,
            PaymentRoutingScope.Property => input.PropertyId > 0 && input.UnitId == null,
            PaymentRoutingScope.Unit => input.PropertyId == null && input.UnitId > 0,
            _ => false
        };
        Guard.InvalidField(
            !valid,
            input.Scope == PaymentRoutingScope.Unit ? nameof(input.UnitId) : nameof(input.PropertyId),
            "Yönlendirme kapsamı ile taşınmaz/birim seçimi uyumlu değil.",
            "PAYMENT_ROUTING_SCOPE_INVALID");
    }
}
