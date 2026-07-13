using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PropertyRepository : BaseRepository<Property>, IPropertyRepository
{
    public PropertyRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliPropertyIds != null)
        {
            query = query.Where(t => yetkiliPropertyIds.Contains(t.Id));
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TasinmazListItemDto
            {
                Id = t.Id,
                Ad = t.Name,
                Il = t.City,
                Ilce = t.District,
                TasinmazTipiAd = t.PropertyType != null ? t.PropertyType.Name : string.Empty,
                KapaliYuzolcumu = t.ClosedArea,
                AcikYuzolcumu = t.OpenArea,
                UnitStructure = t.UnitStructure,
                BirimSayisi = t.Units.Count,
                KiraliBirimSayisi = t.Units.Count(b => b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate > now.AddDays(30))),
                SuresiDolmakUzereBirimSayisi = t.Units.Count(b => b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30))),
                BosBirimSayisi = t.Units.Count(b => !b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now))
            })
            .ToListAsync();
    }

    public async Task<PropertyDetailDto?> GetDetayAsync(int id)
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
                        KdvRate = rt.KdvRate
                    }).ToList(),
                UnitCustomRates = t.Units
                    .Where(b => b.UnitType != null && b.UnitType.Usage == UnitTypeUsage.Rentable)
                    .Select(b => new UnitCustomRateSummaryDto
                    {
                        UnitId = b.Id,
                        UnitName = b.Name,
                        UnitNo = b.UnitNo,
                        Rateler = _ctx.UnitRates
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
                                KdvRate = r.KdvRate
                            }).ToList()
                    })
                    .Where(b => b.Rateler.Any())
                    .ToList(),
                LeaseHistory = t.Units.SelectMany(b => b.Leases)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new TasinmazSozlesmeGecmisiDto
                    {
                        Id = s.Id,
                        UnitId = s.UnitId,
                        UnitName = s.Unit.Name,
                        TenantId = s.TenantId,
                        TenantDisplayName = s.Tenant.DisplayName,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Durum = s.Status,
                        AylikBedel = 0
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<UnitLookupDto>> GetBosBirimlerAsync(List<int>? yetkiliPropertyIds)
    {
        var now = DateTime.Now;
        var query = _ctx.Units.AsNoTracking().AsQueryable();

        if (yetkiliPropertyIds != null)
        {
            query = query.Where(b => yetkiliPropertyIds.Contains(b.PropertyId));
        }

        return await query
            .Where(b => b.UnitType != null && b.UnitType.Usage == UnitTypeUsage.Rentable)
            .Where(b => !b.Leases.Any(s =>
                s.Status == LeaseStatus.Active &&
                s.StartDate <= now &&
                s.EndDate >= now))
            .OrderBy(b => b.Property.Name)
            .ThenBy(b => b.Name)
            .Select(b => new UnitLookupDto
            {
                Id = b.Id,
                Name = b.Name,
                PropertyName = b.Property.Name,
                District = b.Property.District,
                City = b.Property.City,
                Area = b.Area,
                UnitStructure = b.Property.UnitStructure,
                UnitNo = b.UnitNo,
                FloorNo = b.FloorNo
            })
            .ToListAsync();
    }

    public async Task<List<UnitLookupDto>> GetTumBirimlerAsync(List<int>? yetkiliPropertyIds)
    {
        var query = _ctx.Units.AsNoTracking().AsQueryable();
        if (yetkiliPropertyIds != null)
            query = query.Where(b => yetkiliPropertyIds.Contains(b.PropertyId));
        return await query
            .OrderBy(b => b.Property.Name)
            .ThenBy(b => b.Name)
            .Select(b => new UnitLookupDto
            {
                Id = b.Id,
                Name = b.Name,
                PropertyName = b.Property.Name,
                District = b.Property.District,
                City = b.Property.City,
                Area = b.Area,
                UnitStructure = b.Property.UnitStructure,
                UnitNo = b.UnitNo,
                FloorNo = b.FloorNo
            })
            .ToListAsync();
    }

    public async Task AddReservationRateOverrideAsync(ReservationRateOverride tarife)
    {
        await _ctx.RezervasyonTarifeler.AddAsync(tarife);
    }

    public async Task<Property?> GetWithBirimlerTrackedAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Units)
                .ThenInclude(b => b.UnitType)
            .Include(t => t.Units)
                .ThenInclude(b => b.Leases)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
