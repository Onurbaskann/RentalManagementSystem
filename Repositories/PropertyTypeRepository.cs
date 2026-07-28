using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PropertyTypeRepository : BaseRepository<PropertyType>, IPropertyTypeRepository
{
    public PropertyTypeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazTipiListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(k => k.SortOrder).ThenBy(k => k.Name)
            .Select(k => new TasinmazTipiListItemDto
            {
                Id = k.Id,
                Ad = k.Name,
                Kod = k.Code,
                Sira = k.SortOrder,
                Aktif = k.IsActive,
                TekBirimDestekli = k.SupportsSingleUnit,
                CokluBirimDestekli = k.SupportsMultipleUnits
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking()
            .MaxAsync(k => (int?)k.SortOrder) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(k => k.Code == kod && (excludeId == null || k.Id != excludeId));

    public Task<List<PropertyTypeOptionDto>> GetActiveOptionsAsync()
        => _dbSet.AsNoTracking()
            .Where(propertyType => propertyType.IsActive)
            .OrderBy(propertyType => propertyType.SortOrder)
            .Select(propertyType => new PropertyTypeOptionDto(
                propertyType.Id,
                propertyType.Name,
                propertyType.SupportsSingleUnit,
                propertyType.SupportsMultipleUnits))
            .ToListAsync();

    public Task<PropertyStructureSupportDto?> GetStructureSupportAsync(int propertyTypeId)
        => _dbSet.AsNoTracking()
            .Where(propertyType => propertyType.Id == propertyTypeId)
            .Select(propertyType => new PropertyStructureSupportDto(
                propertyType.SupportsSingleUnit,
                propertyType.SupportsMultipleUnits))
            .FirstOrDefaultAsync();
}
