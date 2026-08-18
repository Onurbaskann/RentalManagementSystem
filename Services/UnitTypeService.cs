using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Common;

namespace KiraTakip.Services;

public class UnitTypeService(
    IUnitTypeRepository unitTypeRepository,
    IChargeTypeRepository chargeTypeRepository,
    IChargeRepository chargeRepository,
    IReservationRepository reservationRepository,
    IUnitOfWork uow) : IUnitTypeService
{
    public Task<List<UnitTypeListItemDto>> GetListAsync()
        => unitTypeRepository.GetListAsync();

    public Task<PagedResult<UnitTypeListItemDto>> GetPagedListAsync(TableQuery query)
        => unitTypeRepository.GetPagedListAsync(query);

    public async Task<int> GetNextSortOrderAsync()
        => (await unitTypeRepository.GetMaxSiraAsync()) + 1;

    public async Task<List<UnitTypeChargeTypeCandidateDto>> GetChargeTypeCandidatesAsync()
    {
        var candidates = await chargeTypeRepository.GetRezervasyonAdaylariAsync();
        return candidates.Select(candidate => new UnitTypeChargeTypeCandidateDto(
                             candidate.Id,
                             candidate.Name,
                             candidate.Code))
                         .ToList();
    }

    public async Task<UnitTypeDetailDto?> GetByIdAsync(GetUnitTypeByIdInput input)
    {
        var entity = await unitTypeRepository.GetByIdAsync(input.Id);
        if (entity == null) return null;

        return new UnitTypeDetailDto(
            entity.Id,
            entity.Name,
            entity.SortOrder,
            entity.Usage,
            entity.ChargeTypeId,
            entity.IsActive);
    }

    public async Task CreateAsync(CreateUnitTypeInput input)
    {
        var code = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await unitTypeRepository.KodExistsAsync(code),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        await ValidateChargeTypeAsync(input.Usage, input.ChargeTypeId, nameof(input.ChargeTypeId));

        var entity = new UnitType { Name = input.Name, Code = code };

        entity.SortOrder = input.SortOrder;
        entity.Usage = input.Usage;
        entity.ChargeTypeId = input.Usage == UnitTypeUsage.Reservable ? input.ChargeTypeId : null;
        entity.IsActive = input.IsActive;
        
        await unitTypeRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(EditUnitTypeInput input)
    {
        var entity = Guard.NotFound(
            await unitTypeRepository.GetByIdAsync(input.Id),
            "Birim türü bulunamadı.");

        var code = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await unitTypeRepository.KodExistsAsync(code, input.Id),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        await ValidateChargeTypeAsync(input.Usage, input.ChargeTypeId, nameof(input.ChargeTypeId));

        entity.Name = input.Name;
        entity.Code = code;
        entity.SortOrder = input.SortOrder;
        entity.Usage = input.Usage;
        entity.ChargeTypeId = input.Usage == UnitTypeUsage.Reservable ? input.ChargeTypeId : null;
        entity.IsActive = input.IsActive;

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(ToggleUnitTypeStatusInput input)
    {
        var entity = Guard.NotFound(
            await unitTypeRepository.GetByIdAsync(input.Id),
            "Birim türü bulunamadı.");

        if (entity.IsActive)
        {
            Guard.Conflict(
                await chargeRepository.HasActiveForUnitTypeAsync(input.Id),
                "Bu birim türüne bağlı birimlerde aktif tahakkuku bulunduğu için pasif yapılamaz.");

            Guard.Conflict(
                await reservationRepository.HasConfirmedForUnitTypeAsync(input.Id),
                "Bu birim türüne bağlı birimlerde planlanmış rezervasyon bulunduğu için pasif yapılamaz.");

            if (entity.ChargeTypeId.HasValue)
            {
                var hasOtherActiveUnitType = await unitTypeRepository.AnyAktifByBorcTipiIdAsync(
                    entity.ChargeTypeId.Value,
                    input.Id);
                if (!hasOtherActiveUnitType)
                {
                    var chargeType = await chargeTypeRepository.GetByIdAsync(entity.ChargeTypeId.Value);
                    if (chargeType != null) chargeType.IsActive = false;
                }
            }
        }

        entity.IsActive = !entity.IsActive;
        await uow.SaveChangesAsync();

        return entity.IsActive;
    }

    private async Task ValidateChargeTypeAsync(UnitTypeUsage usage, int? chargeTypeId, string field)
    {
        if (usage != UnitTypeUsage.Reservable) return;

        var selectedChargeTypeId = chargeTypeId.GetValueOrDefault();
        Guard.InvalidField(
            selectedChargeTypeId <= 0,
            field,
            "Rezervasyon birim türü için borç tipi seçilmelidir.");

        Guard.InvalidField(
            !await chargeTypeRepository.IsActiveReservationSpecificAsync(selectedChargeTypeId),
            field,
            "Seçilen borç tipi aktif bir rezervasyon borç tipi değildir.");
    }
}
