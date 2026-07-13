using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ReservationRateOverrideRepository : BaseRepository<ReservationRateOverride>, IReservationRateOverrideRepository
{
    public ReservationRateOverrideRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<ParentReservationRateOverrideRow>> GetGenelForKartAsync(int year)
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
                KdvRate = r.KdvRate
            })
            .ToListAsync();

    public async Task<List<ReservationRateOverrideListItemDto>> GetUcretKurallariListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(r => r.UnitId == null ? 0 : 1)
            .ThenBy(r => r.Unit != null ? r.Unit.Property.Name : string.Empty)
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
}
