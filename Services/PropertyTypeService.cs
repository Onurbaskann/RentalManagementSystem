using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.PropertyType;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Common;

namespace KiraTakip.Services;

public class PropertyTypeService(
    IPropertyTypeRepository tasinmazTipiRepository,
    IUnitOfWork uow) : IPropertyTypeService
{
    public async Task<List<PropertyTypeListItemDto>> GetListAsync()
    {
        var list = await tasinmazTipiRepository.GetListAsync();
        return list.Select(k => new PropertyTypeListItemDto(
            k.Id,
            k.Ad,
            k.Kod,
            k.Sira,
            k.Aktif,
            k.TekBirimDestekli,
            k.CokluBirimDestekli
        )).ToList();
    }

    public async Task<PagedResult<PropertyTypeListItemDto>> GetPagedListAsync(TableQuery query)
    {
        var result = await tasinmazTipiRepository.GetPagedListAsync(query);
        return new PagedResult<PropertyTypeListItemDto>
        {
            Items = result.Items.Select(item => new PropertyTypeListItemDto(
                item.Id,
                item.Ad,
                item.Kod,
                item.Sira,
                item.Aktif,
                item.TekBirimDestekli,
                item.CokluBirimDestekli)).ToList(),
            Total = result.Total,
            Page = result.Page,
            Size = result.Size
        };
    }

    public async Task<int> GetMaxSortOrderAsync()
    {
        return await tasinmazTipiRepository.GetMaxSiraAsync();
    }

    public async Task CreateAsync(CreateInput input)
    {
        var kod = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await tasinmazTipiRepository.KodExistsAsync(kod),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        var entity = new PropertyType
        {
            Name = input.Name,
            Code = kod,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
            SupportsSingleUnit = input.SupportsSingleUnit,
            SupportsMultipleUnits = input.SupportsMultipleUnits
        };

        await tasinmazTipiRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
    }

    public async Task<PropertyType?> GetByIdAsync(int id)
    {
        return await tasinmazTipiRepository.GetByIdAsync(id);
    }

    public async Task UpdateAsync(int id, EditInput input)
    {
        var entity = Guard.NotFound(
            await tasinmazTipiRepository.GetByIdAsync(id),
            "Taşınmaz tipi bulunamadı.");

        var kod = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await tasinmazTipiRepository.KodExistsAsync(kod, excludeId: id),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        entity.Name = input.Name;
        entity.Code = kod;
        entity.SortOrder = input.SortOrder;
        entity.IsActive = input.IsActive;
        entity.SupportsSingleUnit = input.SupportsSingleUnit;
        entity.SupportsMultipleUnits = input.SupportsMultipleUnits;

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var entity = Guard.NotFound(
            await tasinmazTipiRepository.GetByIdAsync(id),
            "Taşınmaz tipi bulunamadı.");

        entity.IsActive = !entity.IsActive;
        await uow.SaveChangesAsync();

        return entity.IsActive;
    }
}
