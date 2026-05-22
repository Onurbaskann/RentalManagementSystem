using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class SozlesmeRepository : BaseRepository<KiraSozlesmesi>, ISozlesmeRepository
{
    public SozlesmeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<SozlesmeListItemDto>> GetListAsync(string? filtre, List<int>? yetkiliTasinmazIds)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
        {
            query = query.Where(s => yetkiliTasinmazIds.Contains(s.Birim.TasinmazId));
        }

        query = filtre switch
        {
            "aktif" => query.Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now),
            "surek" => query.Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi <= now.AddDays(30)),
            "gecmis" => query.Where(s => s.Durum == SozlesmeDurumu.SonaErdi),
            "feshedildi" => query.Where(s => s.Durum == SozlesmeDurumu.Feshedildi),
            _ => query
        };

        return await query
            .OrderByDescending(s => s.BaslangicTarihi)
            .Select(s => new SozlesmeListItemDto
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                KiraciGosterimAdi = s.Kiraci.GosterimAdi,
                KiraciTuru = s.Kiraci.KiraciTuru,
                KiraciKategoriAd = s.Kiraci.KiraciKategori != null ? s.Kiraci.KiraciKategori.Ad : string.Empty,
                BirimId = s.BirimId,
                BirimAd = s.Birim.Ad,
                TasinmazId = s.Birim.TasinmazId,
                TasinmazAd = s.Birim.Tasinmaz.Ad,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                AylikBedel = 0,
                Durum = s.Durum,
                BirimYuzolcumu = s.Birim.Yuzolcumu
            })
            .ToListAsync();
    }

    public async Task<SozlesmeDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SozlesmeDetayDto
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                KiraciGosterimAdi = s.Kiraci.GosterimAdi,
                KiraciTelefon = s.Kiraci.Telefon,
                KiraciEmail = s.Kiraci.Email,
                KiraciTuru = s.Kiraci.KiraciTuru,
                KiraciKategoriId = s.Kiraci.KiraciKategoriId,
                KiraciKategoriAd = s.Kiraci.KiraciKategori != null ? s.Kiraci.KiraciKategori.Ad : string.Empty,
                BirimId = s.BirimId,
                BirimAd = s.Birim.Ad,
                BirimNo = s.Birim.BirimNo,
                BirimKatNo = s.Birim.KatNo,
                BirimYuzolcumu = s.Birim.Yuzolcumu,
                BirimTipi = s.Birim.BirimTipi,
                TasinmazId = s.Birim.TasinmazId,
                TasinmazAd = s.Birim.Tasinmaz.Ad,
                TasinmazIl = s.Birim.Tasinmaz.Il,
                TasinmazIlce = s.Birim.Tasinmaz.Ilce,
                TasinmazMahalle = s.Birim.Tasinmaz.Mahalle,
                TasinmazAcikAdres = s.Birim.Tasinmaz.AcikAdres,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                Notlar = s.Notlar,
                Durum = s.Durum,
                FesihTarihi = s.FesihTarihi,
                FesihNedeni = s.FesihNedeni,
                KdvUygulanacakMi = s.KdvUygulanacakMi,
                IslemGecmisi = s.IslemGecmisi
                    .OrderByDescending(ig => ig.IslemTarihi)
                    .Select(ig => new SozlesmeIslemGecmisiDto
                    {
                        Id = ig.Id,
                        IslemTarihi = ig.IslemTarihi,
                        IslemTipi = ig.IslemTipi,
                        Aciklama = ig.Aciklama,
                        EskiKiraBedeli = ig.EskiKiraBedeli,
                        YeniKiraBedeli = ig.YeniKiraBedeli,
                        EskiBitisTarihi = ig.EskiBitisTarihi,
                        YeniBitisTarihi = ig.YeniBitisTarihi,
                        TufeOrani = ig.TufeOrani,
                        KdvUygulandiMi = ig.KdvUygulandiMi ?? false,
                        KdvOrani = ig.KdvOrani,
                        KdvTutari = ig.KdvTutari,
                        KdvDahilTutar = ig.KdvDahilTutar
                    }).ToList(),
                SozlesmeTarifeler = s.SozlesmeTarifeler
                    .Select(st => new SozlesmeTarifeDto
                    {
                        Id = st.Id,
                        BorcTipiId = st.BorcTipiId,
                        BorcTipiKod = st.BorcTipi.Kod,
                        BorcTipiAd = st.BorcTipi.Ad,
                        BorcTipiDavranis = st.BorcTipi.Davranis,
                        BirimDeger = st.BirimDeger,
                        HesaplamaYontemi = st.HesaplamaYontemi,
                        KdvOrani = st.KdvOrani
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<SozlesmeListItemDto>> GetByKiraciIdAsync(int kiraciId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.KiraciId == kiraciId)
            .OrderByDescending(s => s.BaslangicTarihi)
            .Select(s => new SozlesmeListItemDto
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                KiraciGosterimAdi = s.Kiraci.GosterimAdi,
                KiraciTuru = s.Kiraci.KiraciTuru,
                KiraciKategoriAd = s.Kiraci.KiraciKategori != null ? s.Kiraci.KiraciKategori.Ad : string.Empty,
                BirimId = s.BirimId,
                BirimAd = s.Birim.Ad,
                TasinmazId = s.Birim.TasinmazId,
                TasinmazAd = s.Birim.Tasinmaz.Ad,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                AylikBedel = 0,
                Durum = s.Durum,
                BirimYuzolcumu = s.Birim.Yuzolcumu
            })
            .ToListAsync();
    }

    public async Task<List<SozlesmeListItemDto>> GetByBirimIdAsync(int birimId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.BirimId == birimId)
            .OrderByDescending(s => s.BaslangicTarihi)
            .Select(s => new SozlesmeListItemDto
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                KiraciGosterimAdi = s.Kiraci.GosterimAdi,
                KiraciTuru = s.Kiraci.KiraciTuru,
                KiraciKategoriAd = s.Kiraci.KiraciKategori != null ? s.Kiraci.KiraciKategori.Ad : string.Empty,
                BirimId = s.BirimId,
                BirimAd = s.Birim.Ad,
                TasinmazId = s.Birim.TasinmazId,
                TasinmazAd = s.Birim.Tasinmaz.Ad,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                AylikBedel = 0,
                Durum = s.Durum,
                BirimYuzolcumu = s.Birim.Yuzolcumu
            })
            .ToListAsync();
    }

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds)
    {
        var ids = sozlesmeIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal?>();

        var kalemler = await _ctx.TahakkukKalemleri
            .Where(k => k.Tahakkuk.KiraSozlesmesiId.HasValue
                && ids.Contains(k.Tahakkuk.KiraSozlesmesiId.Value)
                && k.BorcTipi.Kod == "DEPOZITO"
                && k.Tahakkuk.Durum != TahakkukDurumu.IptalEdildi)
            .Select(k => new
            {
                SozlesmeId = k.Tahakkuk.KiraSozlesmesiId!.Value,
                Donem = k.Tahakkuk.DonemBaslangic,
                Tutar = k.ToplamTutar
            })
            .ToListAsync();

        return kalemler
            .GroupBy(x => x.SozlesmeId)
            .ToDictionary(g => g.Key, g => (decimal?)g.OrderBy(x => x.Donem).First().Tutar);
    }

    public async Task<List<KiraSozlesmesi>> GetAktiflerAsync()
        => await _dbSet
            .Include(s => s.Kiraci)
            .Include(s => s.Birim).ThenInclude(b => b.Tasinmaz)
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .OrderBy(s => s.Kiraci.Ad)
            .ToListAsync();

    public async Task<(int TasinmazId, int? KategoriId)?> GetTasinmazVeKategoriAsync(int sozlesmeId)
    {
        var info = await _dbSet.AsNoTracking()
            .Where(s => s.Id == sozlesmeId)
            .Select(s => new { s.Birim.TasinmazId, s.Kiraci.KiraciKategoriId })
            .FirstOrDefaultAsync();
        return info == null ? null : (info.TasinmazId, info.KiraciKategoriId);
    }

    public async Task<List<SozlesmeDropdownDto>> GetAktifDropdownAsync()
        => await _dbSet.AsNoTracking()
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .OrderBy(s => s.Kiraci.Ad)
            .Select(s => new SozlesmeDropdownDto
            {
                Id = s.Id,
                KiraciGosterimAdi = s.Kiraci.GosterimAdi,
                BirimAd = s.Birim.Ad,
                TasinmazAd = s.Birim.Tasinmaz.Ad
            })
            .ToListAsync();
}
