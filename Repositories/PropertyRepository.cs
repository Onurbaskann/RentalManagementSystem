using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PropertyRepository : BaseRepository<Property>, IPropertyRepository
{
    public PropertyRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<PropertyListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();
        var hasScopeFilter = authorizedPropertyIds != null || authorizedUnitIds != null;
        var propertyIds = authorizedPropertyIds ?? [];
        var unitIds = authorizedUnitIds ?? [];

        if (hasScopeFilter)
        {
            query = query.Where(property =>
                propertyIds.Contains(property.Id)
                || property.Units.Any(unit => unitIds.Contains(unit.Id)));
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new PropertyListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                City = t.City,
                District = t.District,
                PropertyTypeName = t.PropertyType != null ? t.PropertyType.Name : string.Empty,
                ClosedArea = t.ClosedArea,
                OpenArea = t.OpenArea,
                UnitStructure = t.UnitStructure,
                UnitCount = t.Units.Count(unit =>
                    !hasScopeFilter || propertyIds.Contains(t.Id) || unitIds.Contains(unit.Id)),
                LeasedUnitCount = t.Units.Count(unit =>
                    (!hasScopeFilter || propertyIds.Contains(t.Id) || unitIds.Contains(unit.Id))
                    && unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now
                        && lease.EndDate > now.AddDays(30))),
                ExpiringSoonUnitCount = t.Units.Count(unit =>
                    (!hasScopeFilter || propertyIds.Contains(t.Id) || unitIds.Contains(unit.Id))
                    && unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now
                        && lease.EndDate <= now.AddDays(30))),
                VacantUnitCount = t.Units.Count(unit =>
                    (!hasScopeFilter || propertyIds.Contains(t.Id) || unitIds.Contains(unit.Id))
                    && !unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now))
            })
            .ToListAsync();
    }

    public async Task<PropertyDetailDto?> GetDetailsAsync(int id)
    {
        var now = DateTime.Now;
        return await _ctx.Properties.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new PropertyDetailDto
            {
                Id = t.Id,
                Name = t.Name,
                City = t.City,
                District = t.District,
                Neighborhood = t.Neighborhood,
                Address = t.Address,
                PropertyTypeName = t.PropertyType != null ? t.PropertyType.Name : string.Empty,
                ClosedArea = t.ClosedArea,
                OpenArea = t.OpenArea,
                UnitStructure = t.UnitStructure,
                Description = t.Description,
                Units = t.Units.Select(b => new UnitDetailDto
                {
                    Id = b.Id,
                    UnitNo = b.UnitNo,
                    Name = b.Name,
                    FloorNo = b.FloorNo,
                    Area = b.Area,
                    UnitTypeName = b.UnitType != null ? b.UnitType.Name : string.Empty,
                    CanBeReserved = b.UnitType != null ? b.UnitType.Usage == UnitTypeUsage.Reservable : false,
                    CanBeRented = b.UnitType != null ? b.UnitType.Usage == UnitTypeUsage.Rentable : false,
                    ActiveLeaseId = b.Leases
                        .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                        .OrderByDescending(s => s.EndDate)
                        .Select(s => (int?)s.Id)
                        .FirstOrDefault(),
                    ActiveLeaseTenantId = b.Leases
                        .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                        .OrderByDescending(s => s.EndDate)
                        .Select(s => (int?)s.TenantId)
                        .FirstOrDefault(),
                    ActiveLeaseTenantDisplayName = b.Leases
                        .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                        .OrderByDescending(s => s.EndDate)
                        .Select(s => s.Tenant.DisplayName)
                        .FirstOrDefault(),
                    ActiveLeaseEndDate = b.Leases
                        .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                        .OrderByDescending(s => s.EndDate)
                        .Select(s => (DateTime?)s.EndDate)
                        .FirstOrDefault(),
                    Status = b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                        ? (b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30))
                            ? OccupancyStatus.ExpiringSoon
                            : OccupancyStatus.Leased)
                        : OccupancyStatus.Vacant,
                    ReservationRateOverrideId = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                        .Select(rt => (int?)rt.Id)
                        .FirstOrDefault(),
                    ReservationPeriodRate = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                        .Select(rt => (decimal?)rt.PeriodRate)
                        .FirstOrDefault(),
                    ReservationBillingPeriodMinutes = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                        .Select(rt => (int?)rt.BillingPeriodMinutes)
                        .FirstOrDefault(),
                    ReservationFreeDurationMinutes = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                        .Select(rt => (int?)rt.FreeDurationMinutes)
                        .FirstOrDefault(),
                    ReservationVatRate = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                        .Select(rt => (decimal?)rt.KdvRate)
                        .FirstOrDefault()
                }).ToList(),
                Reservations = _ctx.Reservations
                    .Where(r => t.Units.Select(b => b.Id).Contains(r.UnitId))
                    .OrderByDescending(r => r.StartDate)
                    .Select(r => new PropertyReservationDto
                    {
                        Id = r.Id,
                        UnitId = r.UnitId,
                        UnitName = r.Unit.Name,
                        TenantId = r.TenantId,
                        TenantDisplayName = r.Tenant.DisplayName,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                        TotalDurationMinutes = r.TotalDurationMinutes,
                        FreeDurationMinutes = r.FreeDurationMinutes,
                        TotalAmount = r.TotalAmount,
                        Status = r.Status
                    }).ToList(),
                UnitReservationRateOverrides = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId != null && t.Units.Select(b => b.Id).Contains(rt.UnitId.Value) && rt.IsActive)
                    .Select(rt => new UnitReservationRateOverrideDto
                    {
                        Id = rt.Id,
                        UnitId = rt.UnitId,
                        UnitName = rt.Unit != null ? rt.Unit.Name : string.Empty,
                        PeriodRate = rt.PeriodRate,
                        BillingPeriodMinutes = rt.BillingPeriodMinutes,
                        FreeDurationMinutes = rt.FreeDurationMinutes,
                        VatRate = rt.KdvRate
                    }).ToList(),
                UnitCustomRates = t.Units
                    .Where(b => b.UnitType != null && b.UnitType.Usage == UnitTypeUsage.Rentable)
                    .Select(b => new UnitCustomRateSummaryDto
                    {
                        UnitId = b.Id,
                        UnitName = b.Name,
                        UnitNo = b.UnitNo,
                        Rates = _ctx.UnitRates
                            .Where(r => r.UnitId == b.Id)
                            .OrderBy(r => r.TenantCategory.Order)
                            .ThenBy(r => r.ChargeType.SortOrder)
                            .Select(r => new UnitCustomRateDto
                            {
                                Id = r.Id,
                                TenantCategoryName = r.TenantCategory.Name,
                                ChargeTypeName = r.ChargeType.Name,
                                CalculationMethod = r.CalculationMethod,
                                UnitValue = r.UnitValue,
                                VatRate = r.KdvRate
                            }).ToList()
                    })
                    .Where(b => b.Rates.Any())
                    .ToList(),
                LeaseHistory = t.Units.SelectMany(b => b.Leases)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new PropertyLeaseHistoryDto
                    {
                        Id = s.Id,
                        UnitId = s.UnitId,
                        UnitName = s.Unit.Name,
                        TenantId = s.TenantId,
                        TenantDisplayName = s.Tenant.DisplayName,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Status = s.Status,
                        MonthlyAmount = 0
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Property?> GetWithUnitsTrackedAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Units)
                .ThenInclude(b => b.UnitType)
            .Include(t => t.Units)
                .ThenInclude(b => b.Leases)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> CanChangeUnitStructureAsync(int propertyId)
    {
        var unitIds = await _ctx.Units
            .IgnoreQueryFilters()
            .Where(unit => unit.PropertyId == propertyId)
            .Select(unit => unit.Id)
            .ToListAsync();

        if (unitIds.Count == 0) return true;

        return !await _ctx.Leases.IgnoreQueryFilters().AnyAsync(lease => unitIds.Contains(lease.UnitId))
            && !await _ctx.Reservations.IgnoreQueryFilters().AnyAsync(reservation => unitIds.Contains(reservation.UnitId))
            && !await _ctx.Charges.IgnoreQueryFilters().AnyAsync(charge => unitIds.Contains(charge.UnitId));
    }

}
