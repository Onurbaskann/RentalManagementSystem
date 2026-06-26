using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class TasinmazService : ITasinmazService
{
    private readonly ITasinmazRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IIstatistikService _istatistikService;
    private readonly ApplicationDbContext _ctx;

    public TasinmazService(
        ITasinmazRepository repo,
        IUnitOfWork uow,
        IIstatistikService istatistikService,
        ApplicationDbContext ctx)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = istatistikService;
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
        foreach (var b in dto.Birimler)
        {
            if (b.AktifSozlesmeId.HasValue)
            {
                var dummySozlesme = new Sozlesme
                {
                    Id = b.AktifSozlesmeId.Value,
                    KiraciId = b.AktifSozlesmeKiraciId ?? 0,
                    BirimId = b.Id,
                    Birim = new Birim { Id = b.Id, Yuzolcumu = b.Yuzolcumu }
                };
                b.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
            }
        }

        // Sözleşme geçmişindeki sözleşmelerin aylık bedellerini hesapla
        foreach (var s in dto.SozlesmeGecmisi)
        {
            var birimYuzolcumu = dto.Birimler.FirstOrDefault(b => b.Id == s.BirimId)?.Yuzolcumu ?? 0m;
            var dummySozlesme = new Sozlesme
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                BirimId = s.BirimId,
                Birim = new Birim { Id = s.BirimId, Yuzolcumu = birimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }

        return dto;
    }

    public async Task<Tasinmaz> CreateAsync(Tasinmaz t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null)
    {
        if (t.KiralamaSekli == KiralamaSekli.BirimBazli && birimler != null && birimler.Count > 0)
        {
            foreach (var b in birimler)
            {
                var ad = string.IsNullOrWhiteSpace(b.Ad) ? $"Birim {b.BirimNo}" : b.Ad;
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = b.BirimNo,
                    KatNo = b.KatNo,
                    Ad = ad,
                    Yuzolcumu = b.Yuzolcumu,
                    Aciklama = b.Aciklama,
                    BirimTuruId = b.BirimTuruId
                });
            }
        }
        else
        {
            t.Birimler.Add(new Birim
            {
                BirimTipi = BirimTipi.Komple,
                Ad = "Komple",
                Yuzolcumu = t.KapaliYuzolcumu > 0 ? t.KapaliYuzolcumu : t.AcikYuzolcumu
            });
        }

        if (rezervasyonAlanlari != null && rezervasyonAlanlari.Count > 0)
        {
            foreach (var r in rezervasyonAlanlari)
            {
                var birim = new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = r.BirimNo,
                    Ad = string.IsNullOrWhiteSpace(r.Ad) ? "Rezervasyon Alanı" : r.Ad,
                    Yuzolcumu = r.Yuzolcumu,
                    Aciklama = r.Aciklama,
                    BirimTuruId = r.BirimTuruId
                };
                t.Birimler.Add(birim);

                // Ücret kuralını ekle
                await _repo.AddRezervasyonTarifeAsync(new RezervasyonTarife
                {
                    Birim = birim,
                    UcretsizSureDakika = r.UcretsizSureDakika,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = r.SaatlikUcret,
                    KdvOrani = r.KdvOrani,
                    Aciklama = $"{r.Ad} için otomatik oluşturuldu"
                });
            }
        }

        await _repo.AddAsync(t);
        await _uow.SaveChangesAsync();
        return t;
    }

    public async Task UpdateAsync(Tasinmaz t)
    {
        await _repo.UpdateAsync(t);
        await _uow.SaveChangesAsync();
    }

    public async Task<TasinmazDuzenleViewModel?> GetForEditAsync(int id)
    {
        var t = await _repo.GetWithBirimlerTrackedAsync(id);
        if (t == null) return null;

        var now = DateTime.Now;
        var birimIds = t.Birimler.Select(b => b.Id).ToList();

        var rezTarife = await _ctx.RezervasyonTarifeler
            .Where(rt => rt.BirimId != null && birimIds.Contains(rt.BirimId.Value) && rt.IsActive)
            .ToListAsync();
        var rezTarifeByBirimId = rezTarife.ToDictionary(rt => rt.BirimId!.Value);

        var aktifRezBirimIds = await _ctx.Rezervasyonlari
            .Where(r => birimIds.Contains(r.BirimId)
                        && r.Durum == RezervasyonDurumu.Planlandi
                        && r.BitisTarihi >= now)
            .Select(r => r.BirimId)
            .Distinct()
            .ToListAsync();

        var birimler = new List<BirimDuzenleViewModel>();
        var rezAlanlari = new List<RezervasyonAlaniDuzenleViewModel>();

        foreach (var b in t.Birimler)
        {
            if (b.BirimTipi == BirimTipi.Komple) continue;

            var hasRezTarife = rezTarifeByBirimId.ContainsKey(b.Id);
            var aktifSoz = b.Sozlesmeler.Any(s =>
                s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now);

            if (hasRezTarife)
            {
                var rt = rezTarifeByBirimId[b.Id];
                rezAlanlari.Add(new RezervasyonAlaniDuzenleViewModel
                {
                    Id = b.Id,
                    BirimNo = b.BirimNo ?? string.Empty,
                    Ad = b.Ad,
                    Yuzolcumu = b.Yuzolcumu,
                    BirimTuruId = b.BirimTuruId,
                    Aciklama = b.Aciklama,
                    UcretsizSureDakika = rt.UcretsizSureDakika,
                    SaatlikUcret = rt.PeriyotUcreti,
                    KdvOrani = rt.KdvOrani,
                    AktifRezervasyonuVar = aktifRezBirimIds.Contains(b.Id)
                });
            }
            else
            {
                birimler.Add(new BirimDuzenleViewModel
                {
                    Id = b.Id,
                    BirimNo = b.BirimNo ?? string.Empty,
                    KatNo = b.KatNo,
                    Ad = b.Ad,
                    Yuzolcumu = b.Yuzolcumu,
                    Aciklama = b.Aciklama,
                    BirimTuruId = b.BirimTuruId,
                    AktifSozlesmesiVar = aktifSoz
                });
            }
        }

        return new TasinmazDuzenleViewModel
        {
            Id = t.Id,
            Ad = t.Ad,
            TasinmazTipiId = t.TasinmazTipiId,
            KiralamaSekli = t.KiralamaSekli,
            Il = t.Il,
            Ilce = t.Ilce,
            Mahalle = t.Mahalle,
            AcikAdres = t.AcikAdres,
            AcikYuzolcumu = t.AcikYuzolcumu,
            KapaliYuzolcumu = t.KapaliYuzolcumu,
            KatSayisi = t.KatSayisi,
            Aciklama = t.Aciklama,
            Birimler = birimler,
            RezervasyonAlanlari = rezAlanlari
        };
    }

    public async Task UpdateWithChildrenAsync(TasinmazDuzenleViewModel vm)
    {
        var t = await _repo.GetWithBirimlerTrackedAsync(vm.Id);
        if (t == null) return;

        t.Ad = vm.Ad;
        t.TasinmazTipiId = vm.TasinmazTipiId;
        t.Il = vm.Il;
        t.Ilce = vm.Ilce;
        t.Mahalle = vm.Mahalle;
        t.AcikAdres = vm.AcikAdres;
        t.AcikYuzolcumu = vm.AcikYuzolcumu;
        t.KapaliYuzolcumu = vm.KapaliYuzolcumu;
        t.KatSayisi = vm.KatSayisi;
        t.Aciklama = vm.Aciklama;

        var now = DateTime.Now;
        var birimIds = t.Birimler.Select(b => b.Id).ToList();
        var rezTarifeler = await _ctx.RezervasyonTarifeler
            .Where(rt => rt.BirimId != null && birimIds.Contains(rt.BirimId.Value) && rt.IsActive)
            .ToListAsync();
        var rezTarifeByBirimId = rezTarifeler.ToDictionary(rt => rt.BirimId!.Value);

        // ---- Birim diff ----
        var gelenBirimIds = vm.Birimler.Where(b => b.Id.HasValue).Select(b => b.Id!.Value).ToHashSet();
        foreach (var mevcut in t.Birimler.Where(b => b.BirimTipi == BirimTipi.Birim && !rezTarifeByBirimId.ContainsKey(b.Id)).ToList())
        {
            if (!gelenBirimIds.Contains(mevcut.Id))
            {
                var aktifSoz = mevcut.Sozlesmeler.Any(s =>
                    s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now);
                if (!aktifSoz)
                    _ctx.Birimler.Remove(mevcut);
            }
        }

        foreach (var b in vm.Birimler)
        {
            var ad = string.IsNullOrWhiteSpace(b.Ad) && !string.IsNullOrWhiteSpace(b.BirimNo)
                ? "Birim " + b.BirimNo : b.Ad ?? string.Empty;

            if (b.Id.HasValue)
            {
                var mevcut = t.Birimler.FirstOrDefault(x => x.Id == b.Id.Value);
                if (mevcut != null)
                {
                    mevcut.BirimNo = b.BirimNo;
                    mevcut.KatNo = b.KatNo;
                    mevcut.Ad = ad;
                    mevcut.Yuzolcumu = b.Yuzolcumu;
                    mevcut.Aciklama = b.Aciklama;
                    mevcut.BirimTuruId = b.BirimTuruId;
                }
            }
            else
            {
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = b.BirimNo,
                    KatNo = b.KatNo,
                    Ad = ad,
                    Yuzolcumu = b.Yuzolcumu,
                    Aciklama = b.Aciklama,
                    BirimTuruId = b.BirimTuruId
                });
            }
        }

        // ---- Komple birim m² senkronu ----
        if (t.KiralamaSekli == KiralamaSekli.TekParca)
        {
            var komple = t.Birimler.FirstOrDefault(b => b.BirimTipi == BirimTipi.Komple);
            if (komple != null)
                komple.Yuzolcumu = vm.KapaliYuzolcumu > 0 ? vm.KapaliYuzolcumu : vm.AcikYuzolcumu;
        }

        // ---- Rezervasyon alanı diff ----
        var gelenRezIds = vm.RezervasyonAlanlari.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();
        var aktifRezBirimIds = await _ctx.Rezervasyonlari
            .Where(r => birimIds.Contains(r.BirimId)
                        && r.Durum == RezervasyonDurumu.Planlandi
                        && r.BitisTarihi >= now)
            .Select(r => r.BirimId)
            .Distinct()
            .ToListAsync();

        foreach (var mevcut in t.Birimler.Where(b => rezTarifeByBirimId.ContainsKey(b.Id)).ToList())
        {
            if (!gelenRezIds.Contains(mevcut.Id) && !aktifRezBirimIds.Contains(mevcut.Id))
            {
                var tarife = rezTarifeByBirimId[mevcut.Id];
                _ctx.RezervasyonTarifeler.Remove(tarife);
                _ctx.Birimler.Remove(mevcut);
            }
        }

        foreach (var r in vm.RezervasyonAlanlari)
        {
            if (r.Id.HasValue)
            {
                var mevcut = t.Birimler.FirstOrDefault(x => x.Id == r.Id.Value);
                if (mevcut != null)
                {
                    mevcut.BirimNo = r.BirimNo;
                    mevcut.Ad = r.Ad ?? string.Empty;
                    mevcut.Yuzolcumu = r.Yuzolcumu;
                    mevcut.Aciklama = r.Aciklama;
                    mevcut.BirimTuruId = r.BirimTuruId;
 
                    if (rezTarifeByBirimId.TryGetValue(mevcut.Id, out var tarife))
                    {
                        tarife.UcretsizSureDakika = r.UcretsizSureDakika;
                        tarife.PeriyotUcreti = r.SaatlikUcret;
                        tarife.KdvOrani = r.KdvOrani;
                    }
                }
            }
            else
            {
                var yeniBirim = new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = r.BirimNo,
                    Ad = r.Ad ?? "Rezervasyon Alanı",
                    Yuzolcumu = r.Yuzolcumu,
                    Aciklama = r.Aciklama,
                    BirimTuruId = r.BirimTuruId
                };
                t.Birimler.Add(yeniBirim);
                await _ctx.RezervasyonTarifeler.AddAsync(new RezervasyonTarife
                {
                    Birim = yeniBirim,
                    UcretsizSureDakika = r.UcretsizSureDakika,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = r.SaatlikUcret,
                    KdvOrani = r.KdvOrani,
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
