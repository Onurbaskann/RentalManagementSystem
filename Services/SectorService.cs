using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class SectorService(
    ICategoryRepository categoryRepository,
    IUnitOfWork uow) : ISectorService
{
    private const CategoryType Type = CategoryType.Sector;

    public Task<List<CategoryListItemDto>> GetSectorsAsync()
        => categoryRepository.GetListByTipiAsync(Type);

    public async Task<int> GetNextOrderAsync()
        => (await categoryRepository.GetMaxSiraByTipiAsync(Type)) + 1;

    public async Task<CategoryListItemDto?> GetByIdAsync(GetSectorByIdInput input)
    {
        var e = await categoryRepository.GetByIdAndTipiAsync(input.Id, Type);
        if (e == null) return null;

        return new CategoryListItemDto
        {
            Id = e.Id,
            Type = e.Type,
            Name = e.Name,
            Code = e.Code,
            Order = e.Order,
            IsActive = e.IsActive
        };
    }

    public async Task CreateAsync(CreateSectorInput input)
    {
        var kod = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await categoryRepository.KodExistsByTipiAsync(Type, kod),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        var entity = new Category
        {
            Type = Type,
            Name = input.Name,
            Code = kod,
            Order = input.Order,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await categoryRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(EditSectorInput input)
    {
        var entity = Guard.NotFound(
            await categoryRepository.GetByIdAndTipiAsync(input.Id, Type),
            "Sektör bulunamadı.");

        var kod = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await categoryRepository.KodExistsByTipiAsync(Type, kod, input.Id),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        entity.Name = input.Name;
        entity.Code = kod;
        entity.Order = input.Order;
        entity.IsActive = input.IsActive;

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(ToggleSectorStatusInput input)
    {
        var entity = Guard.NotFound(
            await categoryRepository.GetByIdAndTipiAsync(input.Id, Type),
            "Sektör bulunamadı.");

        entity.IsActive = !entity.IsActive;
        await uow.SaveChangesAsync();

        return entity.IsActive;
    }
}
