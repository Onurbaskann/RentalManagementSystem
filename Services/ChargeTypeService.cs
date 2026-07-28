using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.ChargeType;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ChargeTypeService(
    IChargeTypeRepository chargeTypeRepository,
    IUnitTypeRepository unitTypeRepository,
    IUnitOfWork uow) : IChargeTypeService
{
    public Task<List<ChargeTypeListItemDto>> GetListAsync() => chargeTypeRepository.GetListAsync();

    public Task<ChargeType?> GetByIdAsync(int id) => chargeTypeRepository.GetByIdAsync(id);

    public async Task<int> GetNextSortOrderAsync() => (await chargeTypeRepository.GetMaxSortOrderAsync()) + 1;

    public async Task CreateAsync(CreateInput input)
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
            IsActive = input.IsActive,
            IsSystem = false
        };

        await chargeTypeRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(int id, EditInput input)
    {
        var entity = Guard.NotFound(await chargeTypeRepository.GetByIdAsync(id), $"Borç tipi {id} bulunamadı.");

        var code = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await chargeTypeRepository.CodeExistsAsync(code, excludeId: id),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        entity.Name = input.Name;
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
                await unitTypeRepository.AnyAktifByBorcTipiIdAsync(id),
                "Bu borç tipi aktif bir birim türüne bağlı. Önce ilgili birim türünü pasif yapın.");
        }

        entity.IsActive = !entity.IsActive;

        await uow.SaveChangesAsync();

        return entity.IsActive;
    }

    public async Task ChangeSortOrderAsync(int id, int newSortOrder)
    {
        var entity = await chargeTypeRepository.GetByIdAsync(id);
        if (entity == null) return;

        entity.SortOrder = newSortOrder;
        
        await uow.SaveChangesAsync();
    }
}
