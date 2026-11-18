using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Properties;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.AdminUser;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Charge;
using KiraTakip.Domain.Leases;
using KiraTakip.Repositories.Leases;

namespace KiraTakip.Repositories.Properties;

public class UnitRepository(ApplicationDbContext ctx) : RepositoryBase<Unit>(ctx), IUnitRepository
{
    public async Task<List<AdminUserUnitOptionDto>> GetAdminUserOptionsAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
            .OrderBy(unit => unit.Property.Name)
            .ThenBy(unit => unit.Name)
            .Select(unit => new AdminUserUnitOptionDto(
                unit.Id,
                unit.Name,
                unit.Property.Name))
            .ToListAsync(ct);

    public async Task<List<UnitListItemDto>> GetByPropertyIdAsync(int propertyId)
    {
        var now = DateTime.Now;
        var list = await _dbSet.AsNoTracking()
            .Where(b => b.PropertyId == propertyId)
            .OrderBy(b => b.Name)
            .Select(b => new UnitListItemDto
            {
                Id = b.Id,
                UnitNo = b.UnitNo,
                Name = b.Name,
                FloorNo = b.FloorNo,
                Area = b.Area,
                UnitTypeName = b.UnitType != null ? b.UnitType.Name : string.Empty,
                PropertyId = b.PropertyId,
                PropertyName = b.Property.Name,
                Status = b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    ? (b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30))
                        ? OccupancyStatus.ExpiringSoon
                        : OccupancyStatus.Leased)
                    : OccupancyStatus.Vacant,
                MonthlyRent = 0
            })
            .ToListAsync();

        var summaries = await GetCapacitySummariesAsync(list.Select(unit => unit.Id).ToList(), now);
        foreach (var unit in list)
        {
            if (!summaries.TryGetValue(unit.Id, out var summary)) continue;
            unit.Status = LeaseOccupancyPolicy.DetermineCapacityStatus(summary.Occupied, summary.Capacity);
            unit.Capacity = summary.Capacity;
            unit.OccupiedCount = summary.Occupied;
            unit.OccupiedPhysicalArea = summary.OccupiedArea;
            unit.ExpiringLeaseCount = summary.Expiring;
        }

        return list;
    }

    private sealed record CapacitySummary(int Capacity, int Occupied, decimal OccupiedArea, int Expiring);

    private async Task<Dictionary<int, CapacitySummary>> GetCapacitySummariesAsync(List<int> unitIds, DateTime now)
    {
        if (unitIds.Count == 0) return [];

        var threshold = now.AddDays(30);
        var rows = await _dbSet.AsNoTracking()
            .Where(unit => unitIds.Contains(unit.Id)
                && unit.UnitType != null && unit.UnitType.RentalMode == UnitRentalMode.CapacityBased
                && unit.Capacity != null && unit.Capacity > 0)
            .Select(unit => new
            {
                unit.Id,
                Capacity = unit.Capacity!.Value,
                Leases = unit.Leases
                    .Where(lease => lease.Status == LeaseStatus.Active && lease.StartDate <= now && lease.EndDate >= now)
                    .Select(lease => new
                    {
                        lease.EndDate,
                        AllocatedArea = lease.LeaseArea != null ? lease.LeaseArea.AllocatedArea : 0m
                    })
                    .ToList()
            })
            .ToListAsync();

        return rows.ToDictionary(
            row => row.Id,
            row => new CapacitySummary(
                row.Capacity,
                row.Leases.Count,
                row.Leases.Sum(lease => lease.AllocatedArea),
                row.Leases.Count(lease => lease.EndDate <= threshold)));
    }

    public async Task<List<UnitListItemDto>> GetReservableUnitsAsync(
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(unit => unit.IsActive
                && unit.UnitType != null
                && unit.UnitType.Usage == UnitTypeUsage.Reservable
                && unit.UnitType.IsActive);

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(unit =>
                propertyIds.Contains(unit.PropertyId)
                || unitIds.Contains(unit.Id));
        }

        return await query
            .OrderBy(b => b.Property.Name).ThenBy(b => b.Name)
            .Select(b => new UnitListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                UnitTypeName = b.UnitType != null ? b.UnitType.Name : string.Empty,
                PropertyId = b.PropertyId,
                PropertyName = b.Property.Name,
                Area = b.Area,
                MonthlyRent = 0
            })
            .ToListAsync();
    }

    public async Task<UnitDetailDto?> GetDetayAsync(int id)
    {
        var now = DateTime.Now;
        var detail = await _dbSet.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new UnitDetailDto
            {
                Id = b.Id,
                UnitNo = b.UnitNo,
                Name = b.Name,
                FloorNo = b.FloorNo,
                Area = b.Area,
                UnitTypeName = b.UnitType != null ? b.UnitType.Name : string.Empty,
                CanBeReserved = b.UnitType != null ? b.UnitType.Usage == UnitTypeUsage.Reservable : false,
                CanBeRented = b.UnitType != null ? b.UnitType.Usage == UnitTypeUsage.Rentable : false,
                PropertyId = b.PropertyId,
                PropertyName = b.Property.Name,
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
                ActiveLeaseIsRentFree = b.Leases
                    .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => s.IsRentFree)
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
                    .FirstOrDefault(),
                MonthlyRent = 0
            })
            .FirstOrDefaultAsync();

        if (detail == null) return null;

        var summaries = await GetCapacitySummariesAsync([detail.Id], now);
        if (summaries.TryGetValue(detail.Id, out var summary))
        {
            detail.Status = LeaseOccupancyPolicy.DetermineCapacityStatus(summary.Occupied, summary.Capacity);
            detail.Capacity = summary.Capacity;
            detail.OccupiedCount = summary.Occupied;
            detail.OccupiedPhysicalArea = summary.OccupiedArea;
            detail.ExpiringLeaseCount = summary.Expiring;
        }

        return detail;
    }

    public async Task<int?> GetPropertyIdAsync(int unitId)
        => await _dbSet.AsNoTracking()
            .Where(b => b.Id == unitId)
            .Select(b => (int?)b.PropertyId)
            .FirstOrDefaultAsync();

    public async Task<List<UnitLookupDto>> GetAvailableAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null,
        int? includedUnitId = null,
        bool includeCapacityBased = false)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(unit =>
                propertyIds.Contains(unit.PropertyId)
                || unitIds.Contains(unit.Id));
        }

        var units = await query
            .Where(unit => unit.UnitType != null && unit.UnitType.Usage == UnitTypeUsage.Rentable)
            .Where(unit => unit.Id == includedUnitId
                || (includeCapacityBased
                    && unit.UnitType.RentalMode == UnitRentalMode.CapacityBased
                    && unit.Capacity != null)
                || (!unit.Leases.Any(lease => lease.Status == LeaseStatus.Active
                        && lease.StartDate <= now
                        && lease.EndDate >= now)
                    && !unit.Leases.Any(lease => lease.Status == LeaseStatus.Draft
                        || lease.Status == LeaseStatus.RevisionRequested)))
            .OrderBy(unit => unit.Property.Name)
            .ThenBy(unit => unit.Name)
            .Select(unit => new UnitLookupDto
            {
                Id = unit.Id,
                Name = unit.Name,
                PropertyName = unit.Property.Name,
                District = unit.Property.District,
                City = unit.Property.City,
                Area = unit.Area,
                UnitStructure = unit.Property.UnitStructure,
                UnitNo = unit.UnitNo,
                FloorNo = unit.FloorNo,
                RentalMode = unit.UnitType.RentalMode,
                Capacity = unit.Capacity
            })
            .ToListAsync();

        if (!includeCapacityBased) return units;

        var capacityUnits = units
            .Where(unit => unit.RentalMode == UnitRentalMode.CapacityBased && unit.Capacity is > 0)
            .ToList();
        if (capacityUnits.Count == 0) return units;

        var today = now.Date;
        var capacityUnitIds = capacityUnits.Select(unit => unit.Id).ToList();
        var leases = await _ctx.Leases.IgnoreQueryFilters().AsNoTracking()
            .Where(lease => capacityUnitIds.Contains(lease.UnitId)
                && !lease.IsDeleted
                && OpenStatuses.Contains(lease.Status)
                && lease.EndDate >= today)
            .Select(lease => new
            {
                lease.UnitId,
                lease.StartDate,
                lease.EndDate,
                AllocatedArea = lease.LeaseArea != null ? lease.LeaseArea.AllocatedArea : 0m
            })
            .ToListAsync();
        var occupantsByUnit = leases
            .GroupBy(item => item.UnitId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new CapacityOccupant(item.StartDate, item.EndDate, item.AllocatedArea))
                    .ToList());

        foreach (var unit in capacityUnits)
        {
            var capacity = unit.Capacity!.Value;
            var occupants = occupantsByUnit.GetValueOrDefault(unit.Id) ?? [];
            var rangeEnd = occupants.Count == 0
                ? today
                : occupants.Max(occupant => occupant.EndDate).Date;
            if (rangeEnd < today) rangeEnd = today;

            var peak = UnitCapacityPolicy.GetPeakUsage(occupants, today, rangeEnd);
            unit.FreeCapacity = capacity - peak.OccupiedCount;
            unit.FreeArea = Math.Max(0m, unit.Area - peak.AllocatedArea);
            unit.NormArea = UnitCapacityPolicy.CalculateNormArea(unit.Area, capacity);
            unit.SuggestedLastSlotArea = UnitCapacityPolicy.SuggestLastSlotArea(
                capacity, unit.Area, peak.OccupiedCount, peak.AllocatedArea);
        }

        return units
            .Where(unit => unit.RentalMode != UnitRentalMode.CapacityBased
                || unit.Id == includedUnitId
                || unit.FreeCapacity > 0)
            .ToList();
    }

    public Task<ReservationUnitContextDto?> GetReservationContextAsync(int unitId)
        => _dbSet.AsNoTracking()
            .Where(unit => unit.Id == unitId)
            .Select(unit => new ReservationUnitContextDto(
                unit.Id,
                unit.PropertyId,
                unit.UnitTypeId,
                unit.UnitType.Name,
                unit.IsActive,
                unit.UnitType.IsActive,
                unit.UnitType.Usage))
            .FirstOrDefaultAsync();

    public Task<LeaseUnitContextDto?> GetLeaseContextAsync(int unitId)
        => _dbSet.AsNoTracking()
            .Where(unit => unit.Id == unitId)
            .Select(unit => new LeaseUnitContextDto(
                unit.Id,
                unit.PropertyId,
                unit.Area,
                unit.UnitType != null && unit.UnitType.Usage == UnitTypeUsage.Rentable,
                unit.UnitType != null ? unit.UnitType.Name : string.Empty,
                unit.UnitType != null ? unit.UnitType.Usage : UnitTypeUsage.Rentable,
                unit.UnitType != null ? unit.UnitType.RentalMode : UnitRentalMode.UnitBased,
                unit.Capacity))
            .FirstOrDefaultAsync();

    public async Task<List<UnitLookupDto>> GetAllOptionsAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(unit =>
                propertyIds.Contains(unit.PropertyId)
                || unitIds.Contains(unit.Id));
        }

        return await query
            .OrderBy(unit => unit.Property.Name)
            .ThenBy(unit => unit.Name)
            .Select(unit => new UnitLookupDto
            {
                Id = unit.Id,
                Name = unit.Name,
                PropertyName = unit.Property.Name,
                District = unit.Property.District,
                City = unit.Property.City,
                Area = unit.Area,
                UnitStructure = unit.Property.UnitStructure,
                UnitNo = unit.UnitNo,
                FloorNo = unit.FloorNo
            })
            .ToListAsync();
    }

    public async Task<List<TenantChargeUnitOptionDto>> GetTenantLeaseOptionsAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(unit => unit.Leases.Any(lease =>
                lease.TenantId == tenantId
                && lease.Status == LeaseStatus.Active));
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(unit =>
                propertyIds.Contains(unit.PropertyId) || unitIds.Contains(unit.Id));
        }

        return await query.OrderBy(unit => unit.Name)
            .Select(unit => new TenantChargeUnitOptionDto(unit.Id, unit.Name))
            .ToListAsync();
    }
    public async Task RemoveStructureDataAsync(IReadOnlyCollection<Unit> units)
    {
        var unitIds = units.Select(unit => unit.Id).ToList();
        var unitRates = await _ctx.UnitRates.IgnoreQueryFilters().Where(rate => unitIds.Contains(rate.UnitId)).ToListAsync();
        var reservationRates = await _ctx.RezervasyonTarifeler.IgnoreQueryFilters()
            .Where(rate => rate.UnitId.HasValue && unitIds.Contains(rate.UnitId.Value)).ToListAsync();
        _ctx.UnitRates.RemoveRange(unitRates);
        _ctx.RezervasyonTarifeler.RemoveRange(reservationRates);
        _dbSet.RemoveRange(units);
    }

    public async Task RemoveWithRatesAsync(Unit unit)
    {
        var rates = await _ctx.UnitRates.Where(rate => rate.UnitId == unit.Id).ToListAsync();
        _ctx.UnitRates.RemoveRange(rates);
        _dbSet.Remove(unit);
    }

    public void Remove(Unit unit) => _dbSet.Remove(unit);

    public async Task<bool> HasHistoricalDependencyAsync(int unitId)
        => await _ctx.Leases.IgnoreQueryFilters().AnyAsync(lease => lease.UnitId == unitId)
            || await _ctx.Reservations.IgnoreQueryFilters().AnyAsync(reservation => reservation.UnitId == unitId)
            || await _ctx.Charges.IgnoreQueryFilters().AnyAsync(charge => charge.UnitId == unitId)
            || await _ctx.PaymentStoreRoutings.IgnoreQueryFilters().AnyAsync(routing => routing.UnitId == unitId);

    public async Task<List<CapacityOccupant>> GetCapacityOccupantsAsync(int unitId, DateTime fromDate)
    {
        var from = fromDate.Date;
        return await CapacityOccupantQuery.OpenForUnit(_ctx, unitId)
            .Where(lease => lease.EndDate.Date >= from)
            .Select(CapacityOccupantQuery.Projection)
            .ToListAsync();
    }

    public async Task<List<CapacityUnitMinimumRow>> GetCapacityUnitsWithOpenLeasesAsync(DateTime fromDate)
    {
        var from = fromDate.Date;
        var units = await _ctx.Units.IgnoreQueryFilters().AsNoTracking()
            .Where(unit => !unit.IsDeleted
                && unit.UnitType.RentalMode == UnitRentalMode.CapacityBased)
            .Select(unit => new
            {
                unit.Id,
                PropertyName = unit.Property.Name,
                UnitName = unit.Name,
                unit.Area,
                unit.Capacity
            })
            .ToListAsync();

        var unitIds = units.Select(unit => unit.Id).ToList();
        var leases = await _ctx.Leases.IgnoreQueryFilters().AsNoTracking()
            .Where(lease => unitIds.Contains(lease.UnitId)
                && !lease.IsDeleted
                && CapacityOccupantQuery.OpenStatuses.Contains(lease.Status)
                && lease.EndDate.Date >= from)
            .Select(lease => new
            {
                lease.UnitId,
                TenantName = lease.Tenant.Name,
                lease.Status,
                Rented = lease.LeaseArea != null ? lease.LeaseArea.RentedArea : 0m,
                Allocated = lease.LeaseArea != null ? lease.LeaseArea.AllocatedArea : 0m
            })
            .ToListAsync();

        var leasesByUnit = leases.ToLookup(lease => lease.UnitId);
        return units
            .OrderBy(unit => unit.PropertyName).ThenBy(unit => unit.UnitName)
            .Select(unit => new CapacityUnitMinimumRow(
                unit.PropertyName,
                unit.UnitName,
                unit.Area,
                unit.Capacity,
                leasesByUnit[unit.Id]
                    .OrderBy(lease => lease.TenantName)
                    .Select(lease => new CapacityLeaseMinimumRow(
                        lease.TenantName, lease.Status, lease.Rented, lease.Allocated))
                    .ToList()))
            .ToList();
    }

    public Task AcquireCapacityLocksAsync(int unitId)
        => AcquireCapacityLocksCoreAsync(unitId);

    public Task AcquireGlobalCapacityLockExclusiveAsync()
        => AcquireCapacityLocksCoreAsync(null);

    private async Task AcquireCapacityLocksCoreAsync(int? unitId)
    {
        if (_ctx.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Kontenjan kilidi aktif transaction gerektirir.");

        if (unitId.HasValue)
        {
            await AcquireAppLockAsync("KiraTakip.Capacity.Global", "Shared");
            await AcquireAppLockAsync($"KiraTakip.Capacity.Unit.{unitId.Value}", "Exclusive");
        }
        else
        {
            await AcquireAppLockAsync("KiraTakip.Capacity.Global", "Exclusive");
        }
    }

    private Task AcquireAppLockAsync(string resource, string mode)
        => _ctx.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = {mode},
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            IF @result < 0
                THROW 51000, 'Kontenjan kilidi alınamadı.', 1;");

    public void AddCapacityHistory(Unit unit, int capacity, DateTime effectiveFrom)
        => _ctx.UnitCapacityHistories.Add(new UnitCapacityHistory
        {
            Unit = unit,
            Capacity = capacity,
            EffectiveFrom = effectiveFrom
        });

    public async Task<HashSet<int>> GetOpenLeaseUnitIdsAsync(IReadOnlyCollection<int> unitIds)
    {
        if (unitIds.Count == 0) return [];
        var ids = await _ctx.Leases.IgnoreQueryFilters().AsNoTracking()
            .Where(lease => unitIds.Contains(lease.UnitId)
                && !lease.IsDeleted
                && OpenStatuses.Contains(lease.Status))
            .Select(lease => lease.UnitId)
            .Distinct()
            .ToListAsync();
        return ids.ToHashSet();
    }

    private static readonly LeaseStatus[] OpenStatuses =
        [LeaseStatus.Active, LeaseStatus.Draft, LeaseStatus.RevisionRequested];
}
