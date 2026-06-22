using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class OdemeRepository : BaseRepository<KiraOdeme>, IOdemeRepository
{
    public OdemeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<OdemeListItemDto>> GetListAsync(int? tahakkukId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<KiraOdeme> query = _dbSet.AsNoTracking();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.KiraTahakkukId == tahakkukId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(o => o.KiraTahakkuk.KiraSozlesmesiId != null && yetkiliTasinmazIds.Contains(o.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId));

        return await query
            .OrderByDescending(o => o.GirisTarihi)
            .Select(o => new OdemeListItemDto
            {
                Id = o.Id,
                KiraTahakkukId = o.KiraTahakkukId,
                KiraSozlesmesiId = o.KiraSozlesmesiId,
                OdemeTarihi = o.OdemeTarihi,
                Tutar = o.Tutar,
                OdemeKanali = o.OdemeKanali,
                OdemeKaynakTipi = o.OdemeKaynakTipi,
                Durum = o.Durum,
                GirisTarihi = o.GirisTarihi,
                Aciklama = o.Aciklama,
                KiraciGosterimAdi = o.KiraSozlesmesi != null && o.KiraSozlesmesi.Kiraci != null ? o.KiraSozlesmesi.Kiraci.GosterimAdi : string.Empty,
                TahakkukDonemBaslangic = o.KiraTahakkuk.DonemBaslangic,
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();
    }

    public async Task<PagedResult<OdemeListItemDto>> GetPagedListAsync(TableQuery q, int? tahakkukId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<KiraOdeme> query = _dbSet.AsNoTracking();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.KiraTahakkukId == tahakkukId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(o => o.KiraTahakkuk.KiraSozlesmesiId != null && yetkiliTasinmazIds.Contains(o.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId));

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(o =>
                (o.KiraSozlesmesi != null && o.KiraSozlesmesi.Kiraci != null && EF.Functions.Like(o.KiraSozlesmesi.Kiraci.Ad, $"%{s}%")) ||
                (o.Aciklama != null && EF.Functions.Like(o.Aciklama, $"%{s}%")));
        }

        if (q.From.HasValue) query = query.Where(o => o.OdemeTarihi >= q.From.Value);
        if (q.To.HasValue) query = query.Where(o => o.OdemeTarihi <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(o => o.Tutar >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(o => o.Tutar <= q.Max.Value);

        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            OdemeDurumu? d = q.Durum switch
            {
                "onaybekliyor" => OdemeDurumu.OnayBekliyor,
                "onaylandi" => OdemeDurumu.Onaylandi,
                "reddedildi" => OdemeDurumu.Reddedildi,
                _ => null
            };
            if (d.HasValue) query = query.Where(o => o.Durum == d.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.GirisTarihi)
            .Skip(q.Skip).Take(q.Take)
            .Select(o => new OdemeListItemDto
            {
                Id = o.Id,
                KiraTahakkukId = o.KiraTahakkukId,
                KiraSozlesmesiId = o.KiraSozlesmesiId,
                OdemeTarihi = o.OdemeTarihi,
                Tutar = o.Tutar,
                OdemeKanali = o.OdemeKanali,
                OdemeKaynakTipi = o.OdemeKaynakTipi,
                Durum = o.Durum,
                GirisTarihi = o.GirisTarihi,
                Aciklama = o.Aciklama,
                KiraciGosterimAdi = o.KiraSozlesmesi != null && o.KiraSozlesmesi.Kiraci != null ? o.KiraSozlesmesi.Kiraci.GosterimAdi : string.Empty,
                TahakkukDonemBaslangic = o.KiraTahakkuk.DonemBaslangic,
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();

        return new PagedResult<OdemeListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    public async Task<OdemeDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OdemeDetayDto
            {
                Id = o.Id,
                KiraTahakkukId = o.KiraTahakkukId,
                KiraSozlesmesiId = o.KiraSozlesmesiId,
                OdemeTarihi = o.OdemeTarihi,
                Tutar = o.Tutar,
                OdemeKanali = o.OdemeKanali,
                OdemeKaynakTipi = o.OdemeKaynakTipi,
                PosReferansNo = o.PosReferansNo,
                Aciklama = o.Aciklama,
                Durum = o.Durum,
                GirisTarihi = o.GirisTarihi,
                OnayTarihi = o.OnayTarihi,
                RedNedeni = o.RedNedeni,
                TasinmazId = o.KiraTahakkuk.KiraSozlesmesi != null && o.KiraTahakkuk.KiraSozlesmesi.Birim != null ? (int?)o.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId : null,
                KiraciGosterimAdi = o.KiraTahakkuk.KiraSozlesmesi != null && o.KiraTahakkuk.KiraSozlesmesi.Kiraci != null ? o.KiraTahakkuk.KiraSozlesmesi.Kiraci.GosterimAdi : "Rezervasyon Ödemesi",
                TahakkukDonemBaslangic = o.KiraTahakkuk.DonemBaslangic,
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null,
                OnaylayanUserGosterimAdi = o.OnaylayanUser != null ? o.OnaylayanUser.AdSoyad : null,
                Dekontlar = o.Dekontlar.Select(d => new OdemeDekontDto
                {
                    Id = d.Id,
                    OrijinalDosyaAdi = d.OrijinalDosyaAdi,
                    DiskDosyaAdi = d.DiskDosyaAdi,
                    DosyaYolu = d.DosyaYolu,
                    DosyaTipi = d.DosyaTipi,
                    DosyaBoyutu = d.DosyaBoyutu,
                    YuklemeTarihi = d.YuklemeTarihi
                }).ToList(),
                BankaEslesmeleri = o.BankaEslesmeleri.Select(e => new OdemeBankaEslesmeDto
                {
                    Id = e.Id,
                    EslesmeTipi = e.EslesmeTipi,
                    BankaHareketiTutar = e.BankaHareketi.Tutar,
                    BankaHareketiTarih = e.BankaHareketi.HareketTarihi,
                    BankaHareketiAciklama = e.BankaHareketi.Aciklama ?? string.Empty
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
