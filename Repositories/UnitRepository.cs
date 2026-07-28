using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UnitRepository : BaseRepository<Unit>, IUnitRepository
{
    public UnitRepository(ApplicationDbContext ctx) : base(ctx) { }

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
        return await _dbSet.AsNoTracking()
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
        return await _dbSet.AsNoTracking()
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
    }

    public async Task<int?> GetPropertyIdAsync(int unitId)
        => await _dbSet.AsNoTracking()
            .Where(b => b.Id == unitId)
            .Select(b => (int?)b.PropertyId)
            .FirstOrDefaultAsync();

    public async Task<List<UnitLookupDto>> GetAvailableAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
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

        return await query
            .Where(unit => unit.UnitType != null && unit.UnitType.Usage == UnitTypeUsage.Rentable)
            .Where(unit => !unit.Leases.Any(lease => lease.Status == LeaseStatus.Active && lease.StartDate <= now && lease.EndDate >= now))
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
                unit.UnitType != null && unit.UnitType.Usage == UnitTypeUsage.Rentable))
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
            .Where(unit => unit.Leases.Any(lease => lease.TenantId == tenantId));
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
            || await _ctx.Charges.IgnoreQueryFilters().AnyAsync(charge => charge.UnitId == unitId);
}
