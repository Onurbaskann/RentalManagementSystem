using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazRepository : BaseRepository<Property>, ITasinmazRepository
{
    public TasinmazRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
        {
            query = query.Where(t => yetkiliTasinmazIds.Contains(t.Id));
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TasinmazListItemDto
            {
                Id = t.Id,
                Ad = t.Name,
                Il = t.City,
                Ilce = t.District,
                TasinmazTipiAd = t.PropertyType != null ? t.PropertyType.Ad : string.Empty,
                KapaliYuzolcumu = t.ClosedArea,
                AcikYuzolcumu = t.OpenArea,
                RentalMode = t.RentalMode,
                BirimSayisi = t.Units.Count,
                KiraliBirimSayisi = t.Units.Count(b => b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate > now.AddDays(30))),
                SuresiDolmakUzereBirimSayisi = t.Units.Count(b => b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30))),
                BosBirimSayisi = t.Units.Count(b => !b.Leases.Any(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now))
            })
            .ToListAsync();
    }

    public async Task<TasinmazDetayDto?> GetDetayAsync(int id)
    {
        var now = DateTime.Now;
        return await _dbSet.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TasinmazDetayDto
            {
                Id = t.Id,
                Ad = t.Name,
                Il = t.City,
                Ilce = t.District,
                Mahalle = t.Neighborhood,
                AcikAdres = t.Address,
                TasinmazTipiAd = t.PropertyType != null ? t.PropertyType.Ad : string.Empty,
                KapaliYuzolcumu = t.ClosedArea,
                AcikYuzolcumu = t.OpenArea,
                RentalMode = t.RentalMode,
                Aciklama = t.Description,
                Units = t.Units.Select(b => new BirimDetayDto
                {
                    Id = b.Id,
                    BirimNo = b.UnitNo,
                    Ad = b.Name,
                    KatNo = b.FloorNo,
                    Yuzolcumu = b.Area,
                    UnitTypeAd = b.UnitType != null ? b.UnitType.Ad : string.Empty,
                    RezervasyonYapilabilirMi = b.UnitType != null ? b.UnitType.RezervasyonYapilabilirMi : false,
                    KiralanabilirMi = b.UnitType != null ? b.UnitType.KiralanabilirMi : false,
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
                        .FirstOrDefault()
                }).ToList(),
                Rezervasyonlar = _ctx.Reservations
                    .Where(r => t.Units.Select(b => b.Id).Contains(r.UnitId))
                    .OrderByDescending(r => r.StartDate)
                    .Select(r => new TasinmazRezervasyonDto
                    {
                        Id = r.Id,
                        BirimId = r.UnitId,
                        BirimAd = r.Unit.Name,
                        KiraciId = r.TenantId,
                        KiraciGosterimAdi = r.Tenant.DisplayName,
                        StartDate = r.StartDate,
                        EndDate = r.EndDate,
                        TotalDurationMinutes = r.TotalDurationMinutes,
                        FreeDurationMinutes = r.FreeDurationMinutes,
                        ToplamTutar = r.TotalAmount,
                        Durum = r.Status
                    }).ToList(),
                BirimRezervasyonKurallari = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.UnitId != null && t.Units.Select(b => b.Id).Contains(rt.UnitId.Value) && rt.IsActive)
                    .Select(rt => new BirimRezervasyonKuralDto
                    {
                        Id = rt.Id,
                        BirimId = rt.UnitId,
                        BirimAd = rt.Unit != null ? rt.Unit.Name : string.Empty,
                        PeriyotUcreti = rt.PeriyotUcreti,
                        UcretlendirmePeriyoduDakika = rt.UcretlendirmePeriyoduDakika,
                        FreeDurationMinutes = rt.FreeDurationMinutes,
                        KdvRate = rt.KdvRate
                    }).ToList(),
                BirimOzelFiyatlari = t.Units
                    .Where(b => b.UnitType != null && b.UnitType.KiralanabilirMi)
                    .Select(b => new BirimOzelFiyatOzetDto
                    {
                        BirimId = b.Id,
                        BirimAd = b.Name,
                        BirimNo = b.UnitNo,
                        Rateler = _ctx.BirimTarifeler
                            .Where(r => r.UnitId == b.Id)
                            .OrderBy(r => r.KiraciKategori.Sira)
                            .ThenBy(r => r.ChargeType.SortOrder)
                            .Select(r => new BirimOzelFiyatRateDto
                            {
                                Id = r.Id,
                                KiraciKategoriAd = r.KiraciKategori.Ad,
                                ChargeTypeName = r.ChargeType.Name,
                                CalculationMethod = r.CalculationMethod,
                                UnitValue = r.UnitValue,
                                KdvRate = r.KdvRate
                            }).ToList()
                    })
                    .Where(b => b.Rateler.Any())
                    .ToList(),
                SozlesmeGecmisi = t.Units.SelectMany(b => b.Leases)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new TasinmazSozlesmeGecmisiDto
                    {
                        Id = s.Id,
                        BirimId = s.UnitId,
                        BirimAd = s.Unit.Name,
                        KiraciId = s.TenantId,
                        KiraciGosterimAdi = s.Tenant.DisplayName,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Durum = s.Status,
                        AylikBedel = 0
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<BirimLookupDto>> GetBosBirimlerAsync(List<int>? yetkiliTasinmazIds)
    {
        var now = DateTime.Now;
        var query = _ctx.Units.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
        {
            query = query.Where(b => yetkiliTasinmazIds.Contains(b.PropertyId));
        }

        return await query
            .Where(b => b.UnitType != null && b.UnitType.KiralanabilirMi)
            .Where(b => !b.Leases.Any(s =>
                s.Status == LeaseStatus.Active &&
                s.StartDate <= now &&
                s.EndDate >= now))
            .OrderBy(b => b.Property.Name)
            .ThenBy(b => b.Name)
            .Select(b => new BirimLookupDto
            {
                Id = b.Id,
                Ad = b.Name,
                TasinmazAd = b.Property.Name,
                Ilce = b.Property.District,
                Il = b.Property.City,
                Yuzolcumu = b.Area,
                UnitKind = b.UnitKind,
                BirimNo = b.UnitNo,
                KatNo = b.FloorNo
            })
            .ToListAsync();
    }

    public async Task<List<BirimLookupDto>> GetTumBirimlerAsync(List<int>? yetkiliTasinmazIds)
    {
        var query = _ctx.Units.AsNoTracking().AsQueryable();
        if (yetkiliTasinmazIds != null)
            query = query.Where(b => yetkiliTasinmazIds.Contains(b.PropertyId));
        return await query
            .OrderBy(b => b.Property.Name)
            .ThenBy(b => b.Name)
            .Select(b => new BirimLookupDto
            {
                Id = b.Id,
                Ad = b.Name,
                TasinmazAd = b.Property.Name,
                Ilce = b.Property.District,
                Il = b.Property.City,
                Yuzolcumu = b.Area,
                UnitKind = b.UnitKind,
                BirimNo = b.UnitNo,
                KatNo = b.FloorNo
            })
            .ToListAsync();
    }

    public async Task AddRezervasyonTarifeAsync(RezervasyonTarife tarife)
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
