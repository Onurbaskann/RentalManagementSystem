using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Reservations;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Reservations;

public class ReservationRateOverrideRepository(ApplicationDbContext ctx) : RepositoryBase<ReservationRateOverride>(ctx), IReservationRateOverrideRepository
{
    public async Task<List<ParentReservationRateOverrideRowDto>> GetGeneralRowsAsync(int year)
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId == null
                     && r.UnitTypeId != null
                     && r.Year == year
                     && r.IsActive
                     && r.UnitType!.IsActive)
            .OrderBy(r => r.UnitType!.SortOrder)
            .Select(r => new ParentReservationRateOverrideRowDto(
                r.UnitType!.Name,
                r.FreeDurationMinutes,
                r.BillingPeriodMinutes,
                r.PeriodRate,
                r.KdvRate))
            .ToListAsync();

    public async Task<List<ReservationRateOverrideListItemDto>> GetUcretKurallariListAsync()
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId != null)
            .OrderBy(r => r.Unit != null ? r.Unit.Property.Name : string.Empty)
            .ThenBy(r => r.Unit != null ? r.Unit.Name : string.Empty)
            .Select(r => new ReservationRateOverrideListItemDto
            {
                Id = r.Id,
                UnitId = r.UnitId,
                UnitName = r.Unit != null ? r.Unit.Name : null,
                PropertyName = r.Unit != null ? r.Unit.Property.Name : null,
                FreeDurationMinutes = r.FreeDurationMinutes,
                BillingPeriodMinutes = r.BillingPeriodMinutes,
                PeriodRate = r.PeriodRate,
                KdvRate = r.KdvRate,
                IsActive = r.IsActive,
                Description = r.Description
            })
            .ToListAsync();

    public Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking().Where(rate => rate.UnitId != null);
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(rate => rate.Unit != null
                && (rate.Unit.Name.Contains(search) || rate.Unit.Property.Name.Contains(search)));
        }
        var items = query
            .OrderBy(rate => rate.Unit != null ? rate.Unit.Property.Name : string.Empty)
            .ThenBy(rate => rate.Unit != null ? rate.Unit.Name : string.Empty)
            .ThenBy(rate => rate.Id)
            .Select(rate => new ReservationRateOverrideListItemDto
            {
                Id = rate.Id,
                UnitId = rate.UnitId,
                UnitName = rate.Unit != null ? rate.Unit.Name : null,
                PropertyName = rate.Unit != null ? rate.Unit.Property.Name : null,
                FreeDurationMinutes = rate.FreeDurationMinutes,
                BillingPeriodMinutes = rate.BillingPeriodMinutes,
                PeriodRate = rate.PeriodRate,
                KdvRate = rate.KdvRate,
                IsActive = rate.IsActive,
                Description = rate.Description
            });
        return GetPagedResultAsync(query, items, tableQuery);
    }

    public Task<ReservationRateOverride?> GetActiveForUnitAsync(int unitId)
        => _dbSet.FirstOrDefaultAsync(rate => rate.IsActive && rate.UnitId == unitId);

    public Task<ReservationRateOverride?> GetForUnitAsync(int unitId)
        => _dbSet.FirstOrDefaultAsync(rate => rate.UnitId == unitId);

    public Task<ReservationRateOverride?> GetGeneralAsync(int unitTypeId, int year)
        => _dbSet.FirstOrDefaultAsync(rate => rate.UnitId == null && rate.UnitTypeId == unitTypeId && rate.IsActive && rate.Year == year);

    public Task<ReservationRateOverride?> GetWithUnitAsync(int id)
        => _dbSet.Include(rate => rate.Unit)
            .FirstOrDefaultAsync(rate => rate.Id == id && rate.UnitId != null);

    public async Task<Dictionary<int, ReservationRateOverride>> GetByUnitIdsAsync(IReadOnlyCollection<int> unitIds, bool activeOnly)
    {
        var query = _dbSet.Where(rate => rate.UnitId.HasValue && unitIds.Contains(rate.UnitId.Value));
        if (activeOnly) query = query.Where(rate => rate.IsActive);
        return await query.ToDictionaryAsync(rate => rate.UnitId!.Value);
    }

    public void Remove(ReservationRateOverride rate) => _dbSet.Remove(rate);
}
