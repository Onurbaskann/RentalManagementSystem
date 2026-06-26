using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazRepository : BaseRepository<Tasinmaz>, ITasinmazRepository
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
            .OrderBy(t => t.Ad)
            .Select(t => new TasinmazListItemDto
            {
                Id = t.Id,
                Ad = t.Ad,
                Il = t.Il,
                Ilce = t.Ilce,
                TasinmazTipiAd = t.TasinmazTipi != null ? t.TasinmazTipi.Ad : string.Empty,
                KapaliYuzolcumu = t.KapaliYuzolcumu,
                AcikYuzolcumu = t.AcikYuzolcumu,
                KiralamaSekli = t.KiralamaSekli,
                BirimSayisi = t.Birimler.Count,
                KiraliBirimSayisi = t.Birimler.Count(b => b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi > now.AddDays(30))),
                SuresiDolmakUzereBirimSayisi = t.Birimler.Count(b => b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi <= now.AddDays(30))),
                BosBirimSayisi = t.Birimler.Count(b => !b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now))
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
                Ad = t.Ad,
                Il = t.Il,
                Ilce = t.Ilce,
                Mahalle = t.Mahalle,
                AcikAdres = t.AcikAdres,
                TasinmazTipiAd = t.TasinmazTipi != null ? t.TasinmazTipi.Ad : string.Empty,
                KapaliYuzolcumu = t.KapaliYuzolcumu,
                AcikYuzolcumu = t.AcikYuzolcumu,
                KiralamaSekli = t.KiralamaSekli,
                Aciklama = t.Aciklama,
                Birimler = t.Birimler.Select(b => new BirimDetayDto
                {
                    Id = b.Id,
                    BirimNo = b.BirimNo,
                    Ad = b.Ad,
                    KatNo = b.KatNo,
                    Yuzolcumu = b.Yuzolcumu,
                    BirimTuruAd = b.BirimTuru != null ? b.BirimTuru.Ad : string.Empty,
                    RezervasyonYapilabilirMi = b.BirimTuru != null ? b.BirimTuru.RezervasyonYapilabilirMi : false,
                    KiralanabilirMi = b.BirimTuru != null ? b.BirimTuru.KiralanabilirMi : false,
                    AktifSozlesmeId = b.Sozlesmeler
                        .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                        .OrderByDescending(s => s.BitisTarihi)
                        .Select(s => (int?)s.Id)
                        .FirstOrDefault(),
                    AktifSozlesmeKiraciId = b.Sozlesmeler
                        .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                        .OrderByDescending(s => s.BitisTarihi)
                        .Select(s => (int?)s.KiraciId)
                        .FirstOrDefault(),
                    AktifSozlesmeKiraciGosterimAdi = b.Sozlesmeler
                        .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                        .OrderByDescending(s => s.BitisTarihi)
                        .Select(s => s.Kiraci.GosterimAdi)
                        .FirstOrDefault(),
                    AktifSozlesmeBitisTarihi = b.Sozlesmeler
                        .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                        .OrderByDescending(s => s.BitisTarihi)
                        .Select(s => (DateTime?)s.BitisTarihi)
                        .FirstOrDefault(),
                    Durum = b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                        ? (b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi <= now.AddDays(30))
                            ? KiraDurumu.SuresiDolmakUzere
                            : KiraDurumu.Kirali)
                        : KiraDurumu.Bos,
                    RezKuralId = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.BirimId == b.Id && rt.IsActive)
                        .Select(rt => (int?)rt.Id)
                        .FirstOrDefault(),
                    RezKuralPeriyotUcreti = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.BirimId == b.Id && rt.IsActive)
                        .Select(rt => (decimal?)rt.PeriyotUcreti)
                        .FirstOrDefault(),
                    RezKuralUcretlendirmePeriyoduDakika = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.BirimId == b.Id && rt.IsActive)
                        .Select(rt => (int?)rt.UcretlendirmePeriyoduDakika)
                        .FirstOrDefault(),
                    RezKuralUcretsizSureDakika = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.BirimId == b.Id && rt.IsActive)
                        .Select(rt => (int?)rt.UcretsizSureDakika)
                        .FirstOrDefault(),
                    RezKuralKdvOrani = _ctx.RezervasyonTarifeler
                        .Where(rt => rt.BirimId == b.Id && rt.IsActive)
                        .Select(rt => (decimal?)rt.KdvOrani)
                        .FirstOrDefault()
                }).ToList(),
                Rezervasyonlar = _ctx.Rezervasyonlari
                    .Where(r => t.Birimler.Select(b => b.Id).Contains(r.BirimId))
                    .OrderByDescending(r => r.BaslangicTarihi)
                    .Select(r => new TasinmazRezervasyonDto
                    {
                        Id = r.Id,
                        BirimId = r.BirimId,
                        BirimAd = r.Birim.Ad,
                        KiraciId = r.KiraciId,
                        KiraciGosterimAdi = r.Kiraci.GosterimAdi,
                        BaslangicTarihi = r.BaslangicTarihi,
                        BitisTarihi = r.BitisTarihi,
                        ToplamSureDakika = r.ToplamSureDakika,
                        UcretsizSureDakika = r.UcretsizSureDakika,
                        ToplamTutar = r.ToplamTutar,
                        Durum = r.Durum
                    }).ToList(),
                BirimRezervasyonKurallari = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.BirimId != null && t.Birimler.Select(b => b.Id).Contains(rt.BirimId.Value) && rt.IsActive)
                    .Select(rt => new BirimRezervasyonKuralDto
                    {
                        Id = rt.Id,
                        BirimId = rt.BirimId,
                        BirimAd = rt.Birim != null ? rt.Birim.Ad : string.Empty,
                        PeriyotUcreti = rt.PeriyotUcreti,
                        UcretlendirmePeriyoduDakika = rt.UcretlendirmePeriyoduDakika,
                        UcretsizSureDakika = rt.UcretsizSureDakika,
                        KdvOrani = rt.KdvOrani
                    }).ToList(),
                BirimOzelFiyatlari = t.Birimler
                    .Where(b => b.BirimTuru != null && b.BirimTuru.KiralanabilirMi)
                    .Select(b => new BirimOzelFiyatOzetDto
                    {
                        BirimId = b.Id,
                        BirimAd = b.Ad,
                        BirimNo = b.BirimNo,
                        Rateler = _ctx.BirimTarifeler
                            .Where(r => r.BirimId == b.Id)
                            .OrderBy(r => r.KiraciKategori.Sira)
                            .ThenBy(r => r.BorcTipi.Sira)
                            .Select(r => new BirimOzelFiyatRateDto
                            {
                                Id = r.Id,
                                KiraciKategoriAd = r.KiraciKategori.Ad,
                                BorcTipiAd = r.BorcTipi.Ad,
                                HesaplamaYontemi = r.HesaplamaYontemi,
                                BirimDeger = r.BirimDeger,
                                KdvOrani = r.KdvOrani
                            }).ToList()
                    })
                    .Where(b => b.Rateler.Any())
                    .ToList(),
                SozlesmeGecmisi = t.Birimler.SelectMany(b => b.Sozlesmeler)
                    .OrderByDescending(s => s.BaslangicTarihi)
                    .Select(s => new TasinmazSozlesmeGecmisiDto
                    {
                        Id = s.Id,
                        BirimId = s.BirimId,
                        BirimAd = s.Birim.Ad,
                        KiraciId = s.KiraciId,
                        KiraciGosterimAdi = s.Kiraci.GosterimAdi,
                        BaslangicTarihi = s.BaslangicTarihi,
                        BitisTarihi = s.BitisTarihi,
                        Durum = s.Durum,
                        AylikBedel = 0
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<BirimLookupDto>> GetBosBirimlerAsync(List<int>? yetkiliTasinmazIds)
    {
        var now = DateTime.Now;
        var query = _ctx.Birimler.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
        {
            query = query.Where(b => yetkiliTasinmazIds.Contains(b.TasinmazId));
        }

        return await query
            .Where(b => !b.Sozlesmeler.Any(s =>
                s.Durum == SozlesmeDurumu.Aktif &&
                s.BaslangicTarihi <= now &&
                s.BitisTarihi >= now))
            .OrderBy(b => b.Tasinmaz.Ad)
            .ThenBy(b => b.Ad)
            .Select(b => new BirimLookupDto
            {
                Id = b.Id,
                Ad = b.Ad,
                TasinmazAd = b.Tasinmaz.Ad,
                Ilce = b.Tasinmaz.Ilce,
                Il = b.Tasinmaz.Il,
                Yuzolcumu = b.Yuzolcumu,
                BirimTipi = b.BirimTipi,
                BirimNo = b.BirimNo,
                KatNo = b.KatNo
            })
            .ToListAsync();
    }

    public async Task AddRezervasyonTarifeAsync(RezervasyonTarife tarife)
    {
        await _ctx.RezervasyonTarifeler.AddAsync(tarife);
    }

    public async Task<Tasinmaz?> GetWithBirimlerTrackedAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Birimler)
                .ThenInclude(b => b.BirimTuru)
            .Include(t => t.Birimler)
                .ThenInclude(b => b.Sozlesmeler)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
