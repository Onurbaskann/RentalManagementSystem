using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PropertyTypeRepository : RepositoryBase<PropertyType>, IPropertyTypeRepository
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

    public Task<PagedResult<TasinmazTipiListItemDto>> GetPagedListAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(type => type.Name.Contains(search) || type.Code.Contains(search));
        }
        var items = query
            .OrderBy(type => type.SortOrder).ThenBy(type => type.Name).ThenBy(type => type.Id)
            .Select(type => new TasinmazTipiListItemDto
            {
                Id = type.Id,
                Ad = type.Name,
                Kod = type.Code,
                Sira = type.SortOrder,
                Aktif = type.IsActive,
                TekBirimDestekli = type.SupportsSingleUnit,
                CokluBirimDestekli = type.SupportsMultipleUnits
            });
        return GetPagedResultAsync(query, items, tableQuery);
    }

    public Task<PropertyStructureSupportDto?> GetStructureSupportAsync(int propertyTypeId)
        => _dbSet.AsNoTracking()
            .Where(propertyType => propertyType.Id == propertyTypeId)
            .Select(propertyType => new PropertyStructureSupportDto(
                propertyType.SupportsSingleUnit,
                propertyType.SupportsMultipleUnits))
            .FirstOrDefaultAsync();
}
