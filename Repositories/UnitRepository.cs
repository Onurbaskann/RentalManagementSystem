using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UnitRepository : BaseRepository<Unit>, IUnitRepository
{
    public UnitRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<BirimListItemDto>> GetByPropertyIdAsync(int propertyId)
    {
        var now = DateTime.Now;
        return await _dbSet.AsNoTracking()
            .Where(b => b.PropertyId == propertyId)
            .OrderBy(b => b.Name)
            .Select(b => new BirimListItemDto
            {
                Id = b.Id,
                BirimNo = b.UnitNo,
                Ad = b.Name,
                KatNo = b.FloorNo,
                Yuzolcumu = b.Area,
                UnitTypeAd = b.UnitType != null ? b.UnitType.Ad : string.Empty,
                TasinmazId = b.PropertyId,
                TasinmazAd = b.Property.Name,
                Durum = b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    ? (b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30))
                        ? OccupancyStatus.ExpiringSoon
                        : OccupancyStatus.Leased)
                    : OccupancyStatus.Vacant,
                AylikBedel = 0
            })
            .ToListAsync();
    }

    public async Task<List<BirimListItemDto>> GetRezervasyonBirimleriAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(b => b.UnitType != null && b.UnitType.RezervasyonYapilabilirMi && b.UnitType.IsActive)
            .OrderBy(b => b.Property.Name).ThenBy(b => b.Name)
            .Select(b => new BirimListItemDto
            {
                Id = b.Id,
                Ad = b.Name,
                UnitTypeAd = b.UnitType != null ? b.UnitType.Ad : string.Empty,
                TasinmazId = b.PropertyId,
                TasinmazAd = b.Property.Name,
                Yuzolcumu = b.Area,
                AylikBedel = 0
            })
            .ToListAsync();
    }

    public async Task<BirimDetayDto?> GetDetayAsync(int id)
    {
        var now = DateTime.Now;
        return await _dbSet.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BirimDetayDto
            {
                Id = b.Id,
                BirimNo = b.UnitNo,
                Ad = b.Name,
                KatNo = b.FloorNo,
                Yuzolcumu = b.Area,
                UnitTypeAd = b.UnitType != null ? b.UnitType.Ad : string.Empty,
                RezervasyonYapilabilirMi = b.UnitType != null ? b.UnitType.RezervasyonYapilabilirMi : false,
                KiralanabilirMi = b.UnitType != null ? b.UnitType.KiralanabilirMi : false,
                TasinmazId = b.PropertyId,
                TasinmazAd = b.Property.Name,
                AktifSozlesmeId = b.Leases
                    .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefault(),
                AktifSozlesmeKiraciId = b.Leases
                    .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => (int?)s.TenantId)
                    .FirstOrDefault(),
                AktifSozlesmeKiraciGosterimAdi = b.Leases
                    .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => s.Tenant.DisplayName)
                    .FirstOrDefault(),
                AktifSozlesmeBitisTarihi = b.Leases
                    .Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => (DateTime?)s.EndDate)
                    .FirstOrDefault(),
                Durum = b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now)
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
                    .Select(rt => (decimal?)rt.PeriyotUcreti)
                    .FirstOrDefault(),
                RezKuralUcretlendirmePeriyoduDakika = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (int?)rt.UcretlendirmePeriyoduDakika)
                    .FirstOrDefault(),
                RezKuralUcretsizSureDakika = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (int?)rt.FreeDurationMinutes)
                    .FirstOrDefault(),
                RezKuralKdvOrani = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId == b.Id && rt.IsActive)
                    .Select(rt => (decimal?)rt.KdvRate)
                    .FirstOrDefault(),
                AylikBedel = 0
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetPropertyIdAsync(int unitId)
        => await _dbSet.AsNoTracking()
            .Where(b => b.Id == unitId)
            .Select(b => (int?)b.PropertyId)
            .FirstOrDefaultAsync();
}
