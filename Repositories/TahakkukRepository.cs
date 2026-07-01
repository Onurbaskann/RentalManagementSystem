using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TahakkukRepository : BaseRepository<Tahakkuk>, ITahakkukRepository
{
    public TahakkukRepository(ApplicationDbContext ctx) : base(ctx) { }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Tahakkuk> q = _dbSet.AsNoTracking();

        if (sozlesmeId.HasValue)
            q = q.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.BirimId));
        else if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.Birim.TasinmazId));

        return await q.OrderByDescending(t => t.DonemBaslangic)
                      .Select(t => new TahakkukListItemDto
                      {
                          Id = t.Id,
                          KiraSozlesmesiId = t.KiraSozlesmesiId,
                          KiraciId = t.KiraciId,
                          KiraciGosterimAdi = t.Kiraci.Ad,
                          TasinmazId = t.Birim.TasinmazId,
                          TasinmazAd = t.Birim.Tasinmaz.Ad,
                          BirimId = t.BirimId,
                          BirimAd = t.Birim.Ad,
                          DonemBaslangic = t.DonemBaslangic,
                          VadeTarihi = t.VadeTarihi,
                          ToplamTutar = t.ToplamTutar,
                          OdenenTutar = t.OdenenTutar,
                          Durum = t.Durum,
                          KaynakTipi = t.KaynakTipi,
                          BekleyenOdemeSayisi = _ctx.TahakkukOdemeler.IgnoreQueryFilters()
                              .Count(o => o.TahakkukId == t.Id && !o.IsDeleted && o.Durum == OdemeDurumu.OnayBekliyor),
                          Kalemler = t.Kalemler.Select(k => new TahakkukKalemDto
                          {
                              BorcTipiKod = k.BorcTipi.Kod,
                              BorcTipiSira = k.BorcTipi.Sira,
                              BorcTipiAd = k.BorcTipi.Ad,
                              Aciklama = k.Aciklama,
                              HesaplamaYontemi = k.HesaplamaYontemi,
                              BirimDeger = k.BirimDeger,
                              Carpan = k.Carpan,
                              Tutar = k.Tutar,
                              KdvOrani = k.KdvOrani,
                              KdvTutari = k.KdvTutari,
                              ToplamTutar = k.ToplamTutar,
                              KaynakTipi = k.KaynakTipi
                          }).ToList()
                      })
                      .ToListAsync();
    }

    // ── Sayfalı listeleme (DTO) ───────────────────────────────────────────
    public async Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Tahakkuk> query = _dbSet.AsNoTracking();

        if (sozlesmeId.HasValue)
            query = query.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (yetkiliBirimIds != null)
            query = query.Where(t => yetkiliBirimIds.Contains(t.BirimId));
        else if (yetkiliTasinmazIds != null)
            query = query.Where(t => yetkiliTasinmazIds.Contains(t.Birim.TasinmazId));

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(t => EF.Functions.Like(t.Kiraci.Ad, $"%{s}%") ||
                                     (t.KiraSozlesmesi != null && EF.Functions.Like(t.KiraSozlesmesi.Birim.Tasinmaz.Ad, $"%{s}%")));
        }

        if (q.From.HasValue) query = query.Where(t => t.VadeTarihi >= q.From.Value);
        if (q.To.HasValue) query = query.Where(t => t.VadeTarihi <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(t => t.ToplamTutar >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(t => t.ToplamTutar <= q.Max.Value);
        if (q.TasinmazId.HasValue) query = query.Where(t => t.KiraSozlesmesi!.Birim.TasinmazId == q.TasinmazId.Value);
        if (q.BirimId.HasValue) query = query.Where(t => t.KiraSozlesmesi!.BirimId == q.BirimId.Value);
        if (q.KiraciId.HasValue) query = query.Where(t => t.KiraciId == q.KiraciId.Value);
        if (q.Yil.HasValue) query = query.Where(t => t.DonemBaslangic.Year == q.Yil.Value);

        if (!string.IsNullOrWhiteSpace(q.Kaynak))
        {
            TahakkukKaynakTipi? kt = q.Kaynak.ToLower() switch
            {
                "manuel" => TahakkukKaynakTipi.Manuel,
                "sozlesme" => TahakkukKaynakTipi.Sozlesme,
                "rezervasyon" => TahakkukKaynakTipi.Rezervasyon,
                _ => null
            };
            if (kt.HasValue) query = query.Where(t => t.KaynakTipi == kt.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            if (q.Durum == "odeme_onay")
            {
                query = query.Where(t => t.Odemeler.Any(o =>
                    o.Durum == OdemeDurumu.OnayBekliyor &&
                    o.OdemeKaynakTipi != OdemeKaynakTipi.SanalPos));
            }
            else if (q.Durum == "iptal")
            {
                query = query.Where(t => t.Durum == TahakkukDurumu.IptalEdildi);
            }
            else
            {
                TahakkukDurumu? d = q.Durum.ToLower() switch
                {
                    "bekliyor" => TahakkukDurumu.Bekleniyor,
                    "kismi" => TahakkukDurumu.KismenOdendi,
                    "tamodendi" => TahakkukDurumu.TamOdendi,
                    "gecikti" => TahakkukDurumu.Gecikti,
                    _ => null
                };
                if (d.HasValue) query = query.Where(t => t.Durum == d.Value);
            }
        }
        else
        {
            query = query.Where(t => t.Durum != TahakkukDurumu.IptalEdildi);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.DonemBaslangic)
                               .Skip(q.Skip).Take(q.Take)
                               .Select(t => new TahakkukListItemDto
                               {
                                   Id = t.Id,
                                   KiraSozlesmesiId = t.KiraSozlesmesiId,
                                   KiraciId = t.KiraciId,
                                   KiraciGosterimAdi = t.Kiraci.Ad,
                                   TasinmazId = t.Birim.TasinmazId,
                                   TasinmazAd = t.Birim.Tasinmaz.Ad,
                                   BirimId = t.BirimId,
                                   BirimAd = t.Birim.Ad,
                                   DonemBaslangic = t.DonemBaslangic,
                                   VadeTarihi = t.VadeTarihi,
                                   ToplamTutar = t.ToplamTutar,
                                   OdenenTutar = t.OdenenTutar,
                                   Durum = t.Durum,
                                   KaynakTipi = t.KaynakTipi,
                                   BekleyenOdemeSayisi = t.Odemeler.Count(o => o.Durum == OdemeDurumu.OnayBekliyor),
                                   Kalemler = t.Kalemler.Select(k => new TahakkukKalemDto
                                   {
                                       BorcTipiKod = k.BorcTipi.Kod,
                                       BorcTipiSira = k.BorcTipi.Sira,
                                       BorcTipiAd = k.BorcTipi.Ad,
                                       Aciklama = k.Aciklama,
                                       HesaplamaYontemi = k.HesaplamaYontemi,
                                       BirimDeger = k.BirimDeger,
                                       Carpan = k.Carpan,
                                       Tutar = k.Tutar,
                                       KdvOrani = k.KdvOrani,
                                       KdvTutari = k.KdvTutari,
                                       ToplamTutar = k.ToplamTutar,
                                       KaynakTipi = k.KaynakTipi
                                   }).ToList()
                               })
                               .ToListAsync();

        return new PagedResult<TahakkukListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    // ── Detay (DTO) ───────────────────────────────────────────────────────
    public async Task<TahakkukDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
                           .Where(t => t.Id == id)
                           .Select(t => new TahakkukDetayDto
                           {
                               Id = t.Id,
                               KiraSozlesmesiId = t.KiraSozlesmesiId,
                               KiraciId = t.KiraciId,
                               KiraciGosterimAdi = t.Kiraci.Ad,
                               TasinmazId = t.Birim.TasinmazId,
                               TasinmazAd = t.Birim.Tasinmaz.Ad,
                               BirimId = t.BirimId,
                               BirimAd = t.Birim.Ad,
                               DonemBaslangic = t.DonemBaslangic,
                               DonemBitis = t.DonemBitis,
                               VadeTarihi = t.VadeTarihi,
                               BeklenenTutar = t.BeklenenTutar,
                               KdvTutari = t.KdvTutari,
                               ToplamTutar = t.ToplamTutar,
                               OdenenTutar = t.OdenenTutar,
                               Durum = t.Durum,
                               KaynakTipi = t.KaynakTipi,
                               OlusturmaTarihi = t.CreatedAt,
                               Kalemler = t.Kalemler.Select(k => new TahakkukKalemDto
                               {
                                   BorcTipiKod = k.BorcTipi.Kod,
                                   BorcTipiSira = k.BorcTipi.Sira,
                                   BorcTipiAd = k.BorcTipi.Ad,
                                   Aciklama = k.Aciklama,
                                   HesaplamaYontemi = k.HesaplamaYontemi,
                                   BirimDeger = k.BirimDeger,
                                   Carpan = k.Carpan,
                                   Tutar = k.Tutar,
                                   KdvOrani = k.KdvOrani,
                                   KdvTutari = k.KdvTutari,
                                   ToplamTutar = k.ToplamTutar,
                                   KaynakTipi = k.KaynakTipi
                               }).ToList(),
                               Odemeler = t.Odemeler.Select(o => new TahakkukOdemeDto
                               {
                                   Id = o.Id,
                                   OdemeTarihi = o.OdemeTarihi,
                                   Tutar = o.Tutar,
                                   OdemeKanali = o.OdemeKanali,
                                   Durum = o.Durum,
                                   GirisTarihi = o.GirisTarihi,
                                   Aciklama = o.Aciklama,
                                   RedNedeni = o.RedNedeni
                               }).ToList()
                           })
                           .FirstOrDefaultAsync();
    }

    // ── Manuel Borç — DTO ─────────────────────────────────────────────────
    public async Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliTasinmazIds, string? durum = null, string? baglanti = null, int? sozlesmeId = null, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Tahakkuk> q = _dbSet.AsNoTracking()
            .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Manuel);

        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.BirimId));
        else if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.Birim.TasinmazId));

        if (sozlesmeId.HasValue)
            q = q.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (!string.IsNullOrWhiteSpace(baglanti))
        {
            if (baglanti == "sozlesmeli") q = q.Where(t => t.KiraSozlesmesiId != null);
            else if (baglanti == "sozlesmesiz") q = q.Where(t => t.KiraSozlesmesiId == null);
        }

        if (!string.IsNullOrWhiteSpace(durum) && durum != "tum")
        {
            if (durum == "iptal")
                q = q.Where(t => t.Durum == TahakkukDurumu.IptalEdildi);
            else
            {
                q = q.Where(t => t.Durum != TahakkukDurumu.IptalEdildi);
                TahakkukDurumu? d = durum switch
                {
                    "bekliyor"  => TahakkukDurumu.Bekleniyor,
                    "kismi"     => TahakkukDurumu.KismenOdendi,
                    "tamodendi" => TahakkukDurumu.TamOdendi,
                    "gecikti"   => TahakkukDurumu.Gecikti,
                    _           => null
                };
                if (d.HasValue) q = q.Where(t => t.Durum == d.Value);
            }
        }
        else
        {
            q = q.Where(t => t.Durum != TahakkukDurumu.IptalEdildi);
        }

        return await q.OrderByDescending(t => t.CreatedAt)
                      .Select(t => new ManuelBorcListItemDto
                      {
                          Id = t.Id,
                          KiraSozlesmesiId = t.KiraSozlesmesiId,
                          KiraciId = t.KiraciId,
                          KiraciKategoriAd = t.Kiraci.KiraciKategori != null ? t.Kiraci.KiraciKategori.Ad : null,
                          KiraciGosterimAdi = t.Kiraci.Ad,
                          TasinmazAd = t.Birim.Tasinmaz.Ad,
                          BirimAd = t.Birim.Ad,
                          BorcTipiKod = t.Kalemler
                              .OrderBy(k => k.BorcTipi.Sira)
                              .Select(k => k.BorcTipi.Kod)
                              .FirstOrDefault(),
                          IlkKalemAciklama = t.Kalemler
                              .OrderBy(k => k.BorcTipi.Sira)
                              .Select(k => k.Aciklama)
                              .FirstOrDefault(),
                          BeklenenTutar = t.BeklenenTutar,
                          KdvTutari = t.KdvTutari,
                          ToplamTutar = t.ToplamTutar,
                          OdenenTutar = t.OdenenTutar,
                          VadeTarihi = t.VadeTarihi,
                          Durum = t.Durum,
                          IptalNotu = t.IptalNotu
                      })
                      .ToListAsync();
    }

    public async Task<int> GetManuelBorcIptalSayisiAsync(List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Tahakkuk> q = _dbSet.AsNoTracking()
            .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Manuel && t.Durum == TahakkukDurumu.IptalEdildi);
        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.BirimId));
        else if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.Birim.TasinmazId));
        return await q.CountAsync();
    }

    // ── Business logic — entity döner ─────────────────────────────────────
    public async Task<Tahakkuk?> GetManuelBorcByIdAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Odemeler)
            .FirstOrDefaultAsync(t => t.Id == id && t.KaynakTipi == TahakkukKaynakTipi.Manuel);
    }

    public async Task<List<Tahakkuk>> GetGeciktirileceklerAsync(DateTime bugun)
    {
        return await _dbSet.Where(t => t.Durum != TahakkukDurumu.TamOdendi &&
                                       t.Durum != TahakkukDurumu.IptalEdildi &&
                                       t.VadeTarihi < bugun)
                           .ToListAsync();
    }

    public async Task<List<Tahakkuk>> GetBekleyenBorclarAsync(DateTime limitVade, CancellationToken ct)
        => await _dbSet
            .Include(t => t.Kiraci)
            .Include(t => t.KiraSozlesmesi!).ThenInclude(s => s!.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(t => t.Odemeler)
            .Where(t => t.Durum != TahakkukDurumu.TamOdendi
                     && t.Durum != TahakkukDurumu.IptalEdildi
                     && t.VadeTarihi <= limitVade)
            .ToListAsync(ct);

    // ── Hesaplama ─────────────────────────────────────────────────────────
    public async Task<decimal> GetOdenenTutarAsync(int tahakkukId)
    {
        return await _ctx.TahakkukOdemeler.AsNoTracking()
                                      .Where(o => o.TahakkukId == tahakkukId && o.Durum == OdemeDurumu.Onaylandi)
                                      .SumAsync(o => (decimal?)o.Tutar) ?? 0m;
    }

    // ── Üretim yardımcıları ───────────────────────────────────────────────
    public async Task<List<BorcTipi>> GetAktifUretimBorcTipleriAsync()
        => await _ctx.BorcTipleri.AsNoTracking()
                                 .Where(b => b.Aktif && (b.Davranis == BorcTipiDavranisi.AylikSabit || b.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik))
                                 .OrderBy(b => b.Sira)
                                 .ToListAsync();

    public async Task<List<Tahakkuk>> GetSilineceklerAsync(int sozlesmeId, DateTime ilkGun)
        => await _dbSet.Where(t => t.KiraSozlesmesiId == sozlesmeId
                                && t.DonemBaslangic >= ilkGun
                                && t.Durum != TahakkukDurumu.TamOdendi
                                && t.KaynakTipi == TahakkukKaynakTipi.Sozlesme
                                && !_ctx.TahakkukOdemeler.Any(o => o.TahakkukId == t.Id))
                       .ToListAsync();

    public Task DeleteRangeAsync(IEnumerable<Tahakkuk> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
}
