using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStatisticsService _istatistikService;
    private readonly ApplicationDbContext _ctx;

    public PropertyService(
        IPropertyRepository repo,
        IUnitOfWork uow,
        IStatisticsService statisticsService,
        ApplicationDbContext ctx)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = statisticsService;
        _ctx = ctx;
    }

    public async Task<List<TasinmazListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetListAsync(tasinmazIds?.ToList());
    }

    public async Task<TasinmazDetayDto?> GetByIdAsync(int id)
    {
        var dto = await _repo.GetDetayAsync(id);
        if (dto == null) return null;

        // Birimlerin aktif sözleşmelerinin aylık bedellerini hesapla
        foreach (var b in dto.Units)
        {
            if (b.AktifSozlesmeId.HasValue)
            {
                var dummySozlesme = new Lease
                {
                    Id = b.AktifSozlesmeId.Value,
                    TenantId = b.AktifSozlesmeKiraciId ?? 0,
                    UnitId = b.Id,
                    Unit = new Unit { Id = b.Id, Area = b.Yuzolcumu }
                };
                b.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
            }
        }

        // Sözleşme geçmişindeki sözleşmelerin aylık bedellerini hesapla
        foreach (var s in dto.SozlesmeGecmisi)
        {
            var birimYuzolcumu = dto.Units.FirstOrDefault(b => b.Id == s.BirimId)?.Yuzolcumu ?? 0m;
            var dummySozlesme = new Lease
            {
                Id = s.Id,
                TenantId = s.KiraciId,
                UnitId = s.BirimId,
                Unit = new Unit { Id = s.BirimId, Area = birimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }

        return dto;
    }

    public async Task<Property> CreateAsync(Property t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null)
    {
        if (t.RentalMode == RentalMode.UnitBased && birimler != null && birimler.Count > 0)
        {
            foreach (var b in birimler)
            {
                var ad = string.IsNullOrWhiteSpace(b.Ad) ? $"Unit {b.BirimNo}" : b.Ad;
                t.Units.Add(new Unit
                {
                    UnitKind = UnitKind.Unit,
                    UnitNo = b.BirimNo,
                    FloorNo = b.KatNo,
                    Name = ad,
                    Area = b.Yuzolcumu,
                    Description = b.Aciklama,
                    UnitTypeId = b.UnitTypeId
                });
            }
        }
        else
        {
            t.Units.Add(new Unit
            {
                UnitKind = UnitKind.Whole,
                Name = "Komple",
                Area = t.ClosedArea > 0 ? t.ClosedArea : t.OpenArea
            });
        }

        if (rezervasyonAlanlari != null && rezervasyonAlanlari.Count > 0)
        {
            foreach (var r in rezervasyonAlanlari)
            {
                var birim = new Unit
                {
                    UnitKind = UnitKind.Unit,
                    UnitNo = r.BirimNo,
                    Name = string.IsNullOrWhiteSpace(r.Ad) ? "Reservation Alanı" : r.Ad,
                    Area = r.Yuzolcumu,
                    Description = r.Aciklama,
                    UnitTypeId = r.UnitTypeId
                };
                t.Units.Add(birim);

                // Ücret kuralını ekle
                await _repo.AddRezervasyonTarifeAsync(new RezervasyonTarife
                {
                    Unit = birim,
                    FreeDurationMinutes = r.FreeDurationMinutes,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = r.SaatlikUcret,
                    KdvRate = r.KdvRate,
                    Aciklama = $"{r.Ad} için otomatik oluşturuldu"
                });
            }
        }

        await _repo.AddAsync(t);
        await _uow.SaveChangesAsync();
        return t;
    }

    public async Task UpdateAsync(Property t)
    {
        await _repo.UpdateAsync(t);
        await _uow.SaveChangesAsync();
    }

    public async Task<TasinmazDuzenleViewModel?> GetForEditAsync(int id)
    {
        var t = await _repo.GetWithBirimlerTrackedAsync(id);
        if (t == null) return null;

        var now = DateTime.Now;
        var birimIds = t.Units.Select(b => b.Id).ToList();

        var rezTarife = await _ctx.RezervasyonTarifeler
            .Where(rt => rt.UnitId != null && birimIds.Contains(rt.UnitId.Value) && rt.IsActive)
            .ToListAsync();
        var rezTarifeByBirimId = rezTarife.ToDictionary(rt => rt.UnitId!.Value);

        var aktifRezBirimIds = await _ctx.Reservations
            .Where(r => birimIds.Contains(r.UnitId)
                        && r.Status == ReservationStatus.Planned
                        && r.EndDate >= now)
            .Select(r => r.UnitId)
            .Distinct()
            .ToListAsync();

        var birimler = new List<BirimDuzenleViewModel>();
        var rezAlanlari = new List<RezervasyonAlaniDuzenleViewModel>();

        foreach (var b in t.Units)
        {
            if (b.UnitKind == UnitKind.Whole) continue;

            var hasRezTarife = rezTarifeByBirimId.ContainsKey(b.Id);
            var aktifSoz = b.Leases.Any(s =>
                s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now);

            if (hasRezTarife)
            {
                var rt = rezTarifeByBirimId[b.Id];
                rezAlanlari.Add(new RezervasyonAlaniDuzenleViewModel
                {
                    Id = b.Id,
                    BirimNo = b.UnitNo ?? string.Empty,
                    Ad = b.Name,
                    Yuzolcumu = b.Area,
                    UnitTypeId = b.UnitTypeId,
                    Aciklama = b.Description,
                    FreeDurationMinutes = rt.FreeDurationMinutes,
                    SaatlikUcret = rt.PeriyotUcreti,
                    KdvRate = rt.KdvRate,
                    AktifRezervasyonuVar = aktifRezBirimIds.Contains(b.Id)
                });
            }
            else
            {
                birimler.Add(new BirimDuzenleViewModel
                {
                    Id = b.Id,
                    BirimNo = b.UnitNo ?? string.Empty,
                    KatNo = b.FloorNo,
                    Ad = b.Name,
                    Yuzolcumu = b.Area,
                    Aciklama = b.Description,
                    UnitTypeId = b.UnitTypeId,
                    AktifSozlesmesiVar = aktifSoz
                });
            }
        }

        return new TasinmazDuzenleViewModel
        {
            Id = t.Id,
            Ad = t.Name,
            TasinmazTipiId = t.PropertyTypeId,
            RentalMode = t.RentalMode,
            Il = t.City,
            Ilce = t.District,
            Mahalle = t.Neighborhood,
            AcikAdres = t.Address,
            AcikYuzolcumu = t.OpenArea,
            KapaliYuzolcumu = t.ClosedArea,
            KatSayisi = t.FloorCount,
            Aciklama = t.Description,
            Units = birimler,
            RezervasyonAlanlari = rezAlanlari
        };
    }

    public async Task UpdateWithChildrenAsync(TasinmazDuzenleViewModel vm)
    {
        var t = await _repo.GetWithBirimlerTrackedAsync(vm.Id);
        if (t == null) return;

        t.Name = vm.Ad;
        t.PropertyTypeId = vm.TasinmazTipiId;
        t.City = vm.Il;
        t.District = vm.Ilce;
        t.Neighborhood = vm.Mahalle;
        t.Address = vm.AcikAdres;
        t.OpenArea = vm.AcikYuzolcumu;
        t.ClosedArea = vm.KapaliYuzolcumu;
        t.FloorCount = vm.KatSayisi;
        t.Description = vm.Aciklama;

        var now = DateTime.Now;
        var birimIds = t.Units.Select(b => b.Id).ToList();
        var rezTarifeler = await _ctx.RezervasyonTarifeler
            .Where(rt => rt.UnitId != null && birimIds.Contains(rt.UnitId.Value) && rt.IsActive)
            .ToListAsync();
        var rezTarifeByBirimId = rezTarifeler.ToDictionary(rt => rt.UnitId!.Value);

        // ---- Unit diff ----
        var gelenBirimIds = vm.Units.Where(b => b.Id.HasValue).Select(b => b.Id!.Value).ToHashSet();
        foreach (var mevcut in t.Units.Where(b => b.UnitKind == UnitKind.Unit && !rezTarifeByBirimId.ContainsKey(b.Id)).ToList())
        {
            if (!gelenBirimIds.Contains(mevcut.Id))
            {
                var aktifSoz = mevcut.Leases.Any(s =>
                    s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now);
                if (!aktifSoz)
                    _ctx.Units.Remove(mevcut);
            }
        }

        foreach (var b in vm.Units)
        {
            var ad = string.IsNullOrWhiteSpace(b.Ad) && !string.IsNullOrWhiteSpace(b.BirimNo)
                ? "Unit " + b.BirimNo : b.Ad ?? string.Empty;

            if (b.Id.HasValue)
            {
                var mevcut = t.Units.FirstOrDefault(x => x.Id == b.Id.Value);
                if (mevcut != null)
                {
                    mevcut.UnitNo = b.BirimNo;
                    mevcut.FloorNo = b.KatNo;
                    mevcut.Name = ad;
                    mevcut.Area = b.Yuzolcumu;
                    mevcut.Description = b.Aciklama;
                    mevcut.UnitTypeId = b.UnitTypeId;
                }
            }
            else
            {
                t.Units.Add(new Unit
                {
                    UnitKind = UnitKind.Unit,
                    UnitNo = b.BirimNo,
                    FloorNo = b.KatNo,
                    Name = ad,
                    Area = b.Yuzolcumu,
                    Description = b.Aciklama,
                    UnitTypeId = b.UnitTypeId
                });
            }
        }

        // ---- Komple birim m² senkronu ----
        if (t.RentalMode == RentalMode.WholeProperty)
        {
            var komple = t.Units.FirstOrDefault(b => b.UnitKind == UnitKind.Whole);
            if (komple != null)
                komple.Area = vm.KapaliYuzolcumu > 0 ? vm.KapaliYuzolcumu : vm.AcikYuzolcumu;
        }

        // ---- Reservation alanı diff ----
        var gelenRezIds = vm.RezervasyonAlanlari.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();
        var aktifRezBirimIds = await _ctx.Reservations
            .Where(r => birimIds.Contains(r.UnitId)
                        && r.Status == ReservationStatus.Planned
                        && r.EndDate >= now)
            .Select(r => r.UnitId)
            .Distinct()
            .ToListAsync();

        foreach (var mevcut in t.Units.Where(b => rezTarifeByBirimId.ContainsKey(b.Id)).ToList())
        {
            if (!gelenRezIds.Contains(mevcut.Id) && !aktifRezBirimIds.Contains(mevcut.Id))
            {
                var tarife = rezTarifeByBirimId[mevcut.Id];
                _ctx.RezervasyonTarifeler.Remove(tarife);
                _ctx.Units.Remove(mevcut);
            }
        }

        foreach (var r in vm.RezervasyonAlanlari)
        {
            if (r.Id.HasValue)
            {
                var mevcut = t.Units.FirstOrDefault(x => x.Id == r.Id.Value);
                if (mevcut != null)
                {
                    mevcut.UnitNo = r.BirimNo;
                    mevcut.Name = r.Ad ?? string.Empty;
                    mevcut.Area = r.Yuzolcumu;
                    mevcut.Description = r.Aciklama;
                    mevcut.UnitTypeId = r.UnitTypeId;
 
                    if (rezTarifeByBirimId.TryGetValue(mevcut.Id, out var tarife))
                    {
                        tarife.FreeDurationMinutes = r.FreeDurationMinutes;
                        tarife.PeriyotUcreti = r.SaatlikUcret;
                        tarife.KdvRate = r.KdvRate;
                    }
                }
            }
            else
            {
                var yeniBirim = new Unit
                {
                    UnitKind = UnitKind.Unit,
                    UnitNo = r.BirimNo,
                    Name = r.Ad ?? "Reservation Alanı",
                    Area = r.Yuzolcumu,
                    Description = r.Aciklama,
                    UnitTypeId = r.UnitTypeId
                };
                t.Units.Add(yeniBirim);
                await _ctx.RezervasyonTarifeler.AddAsync(new RezervasyonTarife
                {
                    Unit = yeniBirim,
                    FreeDurationMinutes = r.FreeDurationMinutes,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = r.SaatlikUcret,
                    KdvRate = r.KdvRate,
                    Aciklama = $"{r.Ad} için otomatik oluşturuldu"
                });
            }
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<List<BirimLookupDto>> GetBosBirimlerAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetBosBirimlerAsync(tasinmazIds?.ToList());
    }
}
