using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UnitRepository : BaseRepository<Unit>, IUnitRepository
{
    public UnitRepository(ApplicationDbContext ctx) : base(ctx) { }

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

    public async Task<List<UnitListItemDto>> GetRezervasyonBirimleriAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(b => b.UnitType != null && b.UnitType.Usage == UnitTypeUsage.Reservable && b.UnitType.IsActive)
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
                RezKuralId = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (int?)rt.Id)
                    .FirstOrDefault(),
                RezKuralPeriyotUcreti = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (decimal?)rt.PeriodRate)
                    .FirstOrDefault(),
                RezKuralUcretlendirmePeriyoduDakika = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (int?)rt.BillingPeriodMinutes)
                    .FirstOrDefault(),
                RezKuralUcretsizSureDakika = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (int?)rt.FreeDurationMinutes)
                    .FirstOrDefault(),
                RezKuralKdvOrani = _ctx.RezervasyonTarifeler
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
}
