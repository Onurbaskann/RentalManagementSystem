using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Properties;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Properties;

public class PropertyRepository(ApplicationDbContext ctx) : RepositoryBase<Property>(ctx), IPropertyRepository
{
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
                        && lease.EndDate >= now)),
                LeasedUnitArea = t.Units
                    .Where(unit => (!hasScopeFilter || propertyIds.Contains(t.Id) || unitIds.Contains(unit.Id))
                        && unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                            && lease.StartDate <= now
                            && lease.EndDate >= now))
                    .Sum(unit => (decimal?)unit.Area) ?? 0,
                VacantUnitArea = t.Units
                    .Where(unit => (!hasScopeFilter || propertyIds.Contains(t.Id) || unitIds.Contains(unit.Id))
                        && !unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                            && lease.StartDate <= now
                            && lease.EndDate >= now))
                    .Sum(unit => (decimal?)unit.Area) ?? 0
            })
            .ToListAsync();
    }

    public async Task<PagedResult<PropertyListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
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

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(property =>
                EF.Functions.Like(property.Name, $"%{search}%")
                || EF.Functions.Like(property.City, $"%{search}%")
                || EF.Functions.Like(property.District, $"%{search}%")
                || (property.PropertyType != null
                    && EF.Functions.Like(property.PropertyType.Name, $"%{search}%")));
        }

        var itemsQuery = query
            .OrderBy(property => property.Name)
            .ThenBy(property => property.Id)
            .Select(property => new PropertyListItemDto
            {
                Id = property.Id,
                Name = property.Name,
                City = property.City,
                District = property.District,
                PropertyTypeName = property.PropertyType != null ? property.PropertyType.Name : string.Empty,
                ClosedArea = property.ClosedArea,
                OpenArea = property.OpenArea,
                UnitStructure = property.UnitStructure,
                UnitCount = property.Units.Count(unit =>
                    !hasScopeFilter || propertyIds.Contains(property.Id) || unitIds.Contains(unit.Id)),
                LeasedUnitCount = property.Units.Count(unit =>
                    (!hasScopeFilter || propertyIds.Contains(property.Id) || unitIds.Contains(unit.Id))
                    && unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now
                        && lease.EndDate > now.AddDays(30))),
                ExpiringSoonUnitCount = property.Units.Count(unit =>
                    (!hasScopeFilter || propertyIds.Contains(property.Id) || unitIds.Contains(unit.Id))
                    && unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now
                        && lease.EndDate <= now.AddDays(30))),
                VacantUnitCount = property.Units.Count(unit =>
                    (!hasScopeFilter || propertyIds.Contains(property.Id) || unitIds.Contains(unit.Id))
                    && !unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now))
            });

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public async Task<PropertyDetailDto?> GetDetailsAsync(int id)
    {
        var now = DateTime.Now;
        var property = await _ctx.Properties.AsNoTracking()
            // Units/Reservations/UnitReservationRateOverrides/UnitCustomRates/LeaseHistory — 5 ayrı
            // koleksiyon aynı projeksiyonda. AsSplitQuery olmadan EF Core bunları tek dev JOIN'de
            // birleştirir (kartezyen çarpım — birim×rezervasyon×geçmiş sayısı katlanarak büyür).
            .AsSplitQuery()
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
                // Not: ActiveLease*/Status/Reservation* alanları burada DOLDURULMAZ — aşağıda
                // birim sayısından bağımsız, sabit sayıda bulk sorguyla doldurulur. Eskiden burada
                // birim başına 12 ayrı correlated subquery vardı (OUTER APPLY × birim sayısı) —
                // çok birimli taşınmazlarda asıl performans sorunu buydu.
                Units = t.Units.Select(b => new UnitDetailDto
                {
                    Id = b.Id,
                    UnitNo = b.UnitNo,
                    Name = b.Name,
                    FloorNo = b.FloorNo,
                    Area = b.Area,
                    UnitTypeName = b.UnitType != null ? b.UnitType.Name : string.Empty,
                    CanBeReserved = b.UnitType != null && b.UnitType.Usage == UnitTypeUsage.Reservable,
                    CanBeRented = b.UnitType != null && b.UnitType.Usage == UnitTypeUsage.Rentable
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
                        IsRentFree = s.IsRentFree,
                        MonthlyAmount = 0
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (property == null) return null;

        var unitIds = property.Units.Select(unit => unit.Id).ToList();
        if (unitIds.Count == 0) return property;

        var activeLeases = await _ctx.Leases.AsNoTracking()
            .Where(s => unitIds.Contains(s.UnitId)
                && s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
            .Select(s => new
            {
                s.UnitId,
                s.Id,
                s.TenantId,
                s.IsRentFree,
                TenantDisplayName = s.Tenant.DisplayName,
                s.EndDate
            })
            .ToListAsync();
        var activeLeaseByUnit = activeLeases
            .GroupBy(lease => lease.UnitId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(lease => lease.EndDate).First());

        var reservationRates = await _ctx.RezervasyonTarifeler.AsNoTracking()
            .Where(rt => rt.UnitId != null && unitIds.Contains(rt.UnitId.Value) && rt.IsActive)
            .Select(rt => new { UnitId = rt.UnitId!.Value, rt.Id, rt.PeriodRate, rt.BillingPeriodMinutes, rt.FreeDurationMinutes, rt.KdvRate })
            .ToListAsync();
        var reservationRateByUnit = reservationRates
            .GroupBy(rate => rate.UnitId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var unit in property.Units)
        {
            if (activeLeaseByUnit.TryGetValue(unit.Id, out var lease))
            {
                unit.ActiveLeaseId = lease.Id;
                unit.ActiveLeaseTenantId = lease.TenantId;
                unit.ActiveLeaseIsRentFree = lease.IsRentFree;
                unit.ActiveLeaseTenantDisplayName = lease.TenantDisplayName;
                unit.ActiveLeaseEndDate = lease.EndDate;
                unit.Status = lease.EndDate <= now.AddDays(30) ? OccupancyStatus.ExpiringSoon : OccupancyStatus.Leased;
            }
            else
            {
                unit.Status = OccupancyStatus.Vacant;
            }

            if (reservationRateByUnit.TryGetValue(unit.Id, out var rate))
            {
                unit.ReservationRateOverrideId = rate.Id;
                unit.ReservationPeriodRate = rate.PeriodRate;
                unit.ReservationBillingPeriodMinutes = rate.BillingPeriodMinutes;
                unit.ReservationFreeDurationMinutes = rate.FreeDurationMinutes;
                unit.ReservationVatRate = rate.KdvRate;
            }
        }

        return property;
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

        var hasRoutingDependency = await _ctx.PaymentStoreRoutings.IgnoreQueryFilters().AnyAsync(routing =>
            routing.PropertyId == propertyId || (routing.UnitId.HasValue && unitIds.Contains(routing.UnitId.Value)));
        if (hasRoutingDependency) return false;
        if (unitIds.Count == 0) return true;

        return !await _ctx.Leases.IgnoreQueryFilters().AnyAsync(lease => unitIds.Contains(lease.UnitId))
            && !await _ctx.Reservations.IgnoreQueryFilters().AnyAsync(reservation => unitIds.Contains(reservation.UnitId))
            && !await _ctx.Charges.IgnoreQueryFilters().AnyAsync(charge => unitIds.Contains(charge.UnitId));
    }

}
