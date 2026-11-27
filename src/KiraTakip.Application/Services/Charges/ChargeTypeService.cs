using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.ChargeType;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Charges;

namespace KiraTakip.Services.Charges;

public class ChargeTypeService(
    IChargeTypeRepository chargeTypeRepository,
    IReservationRateOverrideRepository reservationRateRepository,
    IPaymentStoreRoutingRepository paymentStoreRoutingRepository,
    IUnitOfWork uow) : IChargeTypeService
{
    public Task<List<ChargeTypeListItemDto>> GetListAsync() => chargeTypeRepository.GetListAsync();
    public Task<PagedResult<ChargeTypeListItemDto>> GetPagedListAsync(TableQuery query)
        => chargeTypeRepository.GetPagedListAsync(query);

    public Task<ChargeType?> GetByIdAsync(int id) => chargeTypeRepository.GetByIdAsync(id);

    public async Task<int> GetNextSortOrderAsync() => (await chargeTypeRepository.GetMaxSortOrderAsync()) + 1;

    public async Task<int> CreateAsync(CreateInput input)
    {
        var code = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await chargeTypeRepository.CodeExistsAsync(code),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        var entity = new ChargeType
        {
            Name = input.Name,
            Code = code,
            Behavior = input.Behavior,
            SortOrder = input.SortOrder,
            IsActive = false,
            IsSystem = false
        };

        await chargeTypeRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(int id, EditInput input)
    {
        var entity = Guard.NotFound(await chargeTypeRepository.GetByIdAsync(id), $"Borç tipi {id} bulunamadı.");

        var code = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await chargeTypeRepository.CodeExistsAsync(code, excludeId: id),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        Guard.Conflict(
            entity.IsSystem && entity.IsActive && !input.IsActive,
            $"'{entity.Name}' bir sistem kaydıdır ve pasif yapılamaz.",
            "CHARGE_TYPE_SYSTEM_DEACTIVATION_FORBIDDEN");

        Guard.Conflict(
            entity.IsActive && !input.IsActive
                && await reservationRateRepository.AnyActiveByChargeTypeAsync(id),
            ActiveReservationRuleMessage,
            "CHARGE_TYPE_USED_BY_ACTIVE_RESERVATION_RULE");

        Guard.Conflict(
            entity.Behavior != input.Behavior
                && await chargeTypeRepository.IsChargeTypeUsedAsync(id),
            "Bu borç tipi rezervasyon kuralı, rezervasyon veya tahakkuk kaleminde kullanıldığı için davranışı değiştirilemez.",
            "CHARGE_TYPE_BEHAVIOR_CHANGE_FORBIDDEN");

        if (!entity.IsActive && input.IsActive)
        {
            Guard.Conflict(
                !await paymentStoreRoutingRepository.HasUsableDefaultAsync(id),
                "Borç tipi aktifleştirilmeden önce kullanılabilir bir genel mağaza yönlendirmesi tanımlanmalıdır.",
                "CHARGE_TYPE_DEFAULT_STORE_REQUIRED");
        }

        entity.Name = input.Name;
        if (!entity.IsSystem)
            entity.Code = code;
        entity.Behavior = input.Behavior;
        entity.SortOrder = input.SortOrder;
        entity.IsActive = input.IsActive;
        // entity.IsSystem hiç değiştirilmez

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var entity = Guard.NotFound(await chargeTypeRepository.GetByIdAsync(id), $"Borç tipi {id} bulunamadı.");

        if (entity.IsActive)
        {
            Guard.Conflict(entity.IsSystem, $"'{entity.Name}' bir sistem kaydıdır ve pasif yapılamaz.");
            Guard.Conflict(
                await reservationRateRepository.AnyActiveByChargeTypeAsync(id),
                ActiveReservationRuleMessage,
                "CHARGE_TYPE_USED_BY_ACTIVE_RESERVATION_RULE");
        }
        else
        {
            Guard.Conflict(
                !await paymentStoreRoutingRepository.HasUsableDefaultAsync(id),
                "Borç tipi aktifleştirilmeden önce kullanılabilir bir genel mağaza yönlendirmesi tanımlanmalıdır.",
                "CHARGE_TYPE_DEFAULT_STORE_REQUIRED");
        }

        entity.IsActive = !entity.IsActive;

        await uow.SaveChangesAsync();

        return entity.IsActive;
    }

    private const string ActiveReservationRuleMessage =
        "Bu borç tipi aktif bir rezervasyon kuralında kullanılıyor. Önce ilgili kuralı pasif yapın veya başka bir borç tipi seçin.";

    public async Task ChangeSortOrderAsync(int id, int newSortOrder)
    {
        var entity = await chargeTypeRepository.GetByIdAsync(id);
        if (entity == null) return;

        entity.SortOrder = newSortOrder;

        await uow.SaveChangesAsync();
    }
}
