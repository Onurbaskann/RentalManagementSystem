using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ReservationRateOverrideRepository : BaseRepository<ReservationRateOverride>, IReservationRateOverrideRepository
{
    public ReservationRateOverrideRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<ParentReservationRateOverrideRow>> GetGeneralRowsAsync(int year)
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId == null
                     && r.UnitTypeId != null
                     && r.Year == year
                     && r.IsActive
                     && r.UnitType!.IsActive)
            .OrderBy(r => r.UnitType!.SortOrder)
            .Select(r => new ParentReservationRateOverrideRow
            {
                UnitTypeName = r.UnitType!.Name,
                FreeDurationMinutes = r.FreeDurationMinutes,
                BillingPeriodMinutes = r.BillingPeriodMinutes,
                PeriodRate = r.PeriodRate,
                VatRate = r.KdvRate
            })
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
