using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UnitTypeRepository : RepositoryBase<UnitType>, IUnitTypeRepository
{
    public UnitTypeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<UnitTypeListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new UnitTypeListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                SortOrder = b.SortOrder,
                CanBeRented = b.Usage == UnitTypeUsage.Rentable,
                CanBeReserved = b.Usage == UnitTypeUsage.Reservable,
                ChargeTypeId = b.ChargeTypeId,
                ChargeTypeName = b.ChargeType != null ? b.ChargeType.Name : null,
                IsActive = b.IsActive
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.SortOrder) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Code == kod && (excludeId == null || b.Id != excludeId));

    public async Task<bool> AnyAktifByBorcTipiIdAsync(int chargeTypeId, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.ChargeTypeId == chargeTypeId && b.IsActive && (excludeId == null || b.Id != excludeId));

    public Task<List<UnitTypeOptionDto>> GetActiveOptionsAsync()
        => _dbSet.AsNoTracking()
            .Where(unitType => unitType.IsActive)
            .OrderBy(unitType => unitType.SortOrder)
            .Select(unitType => new UnitTypeOptionDto(unitType.Id, unitType.Name, unitType.Usage))
            .ToListAsync();

    public Task<PagedResult<UnitTypeListItemDto>> GetPagedListAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(type => type.Name.Contains(search) || type.Code.Contains(search));
        }
        var items = query
            .OrderBy(type => type.SortOrder).ThenBy(type => type.Name).ThenBy(type => type.Id)
            .Select(type => new UnitTypeListItemDto
            {
                Id = type.Id,
                Name = type.Name,
                Code = type.Code,
                SortOrder = type.SortOrder,
                CanBeRented = type.Usage == UnitTypeUsage.Rentable,
                CanBeReserved = type.Usage == UnitTypeUsage.Reservable,
                ChargeTypeId = type.ChargeTypeId,
                ChargeTypeName = type.ChargeType != null ? type.ChargeType.Name : null,
                IsActive = type.IsActive
            });
        return GetPagedResultAsync(query, items, tableQuery);
    }

    public Task<List<UnitTypeUsageDto>> GetActiveUsagesAsync(IReadOnlyCollection<int> unitTypeIds)
        => _dbSet.AsNoTracking()
            .Where(unitType => unitTypeIds.Contains(unitType.Id) && unitType.IsActive)
            .Select(unitType => new UnitTypeUsageDto(unitType.Id, unitType.Usage))
            .ToListAsync();
}
