using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BankaHareketiRepository : BaseRepository<BankaHareketi>, IBankaHareketiRepository
{
    public BankaHareketiRepository(ApplicationDbContext ctx) : base(ctx) { }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<BankaHareketiListItemDto>> GetListAsync(BankaEslesmeDurumu? durum = null)
    {
        IQueryable<BankaHareketi> q = _dbSet.AsNoTracking();
        if (durum.HasValue) q = q.Where(b => b.EslesmeDurumu == durum.Value);
        return await q.OrderByDescending(b => b.HareketTarihi)
                      .Select(b => new BankaHareketiListItemDto
                      {
                          Id = b.Id,
                          HareketTarihi = b.HareketTarihi,
                          Tutar = b.Tutar,
                          Aciklama = b.Aciklama,
                          KarsiHesap = b.KarsiHesap,
                          KarsiUnvan = b.KarsiUnvan,
                          BankaKodu = b.BankaKodu,
                          EslesmeDurumu = b.EslesmeDurumu,
                          ImportTarihi = b.ImportTarihi,
                          ImportEdenUserAdi = b.ImportEdenUser != null ? b.ImportEdenUser.UserName : null
                      })
                      .ToListAsync();
    }

    public async Task<PagedResult<BankaHareketiListItemDto>> GetPagedListAsync(TableQuery q)
    {
        IQueryable<BankaHareketi> query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(b =>
                EF.Functions.Like(b.Aciklama, $"%{s}%") ||
                (b.KarsiUnvan != null && EF.Functions.Like(b.KarsiUnvan, $"%{s}%")) ||
                (b.KarsiHesap != null && EF.Functions.Like(b.KarsiHesap, $"%{s}%")));
        }
        if (q.From.HasValue) query = query.Where(b => b.HareketTarihi >= q.From.Value);
        if (q.To.HasValue) query = query.Where(b => b.HareketTarihi <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(b => b.Tutar >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(b => b.Tutar <= q.Max.Value);
        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            BankaEslesmeDurumu? d = q.Durum switch
            {
                "eslestirilmedi" => BankaEslesmeDurumu.Eslestirilmedi,
                "eslesti" => BankaEslesmeDurumu.Eslesti,
                "manuel" => BankaEslesmeDurumu.ManuelEslesti,
                _ => null
            };
            if (d.HasValue) query = query.Where(b => b.EslesmeDurumu == d.Value);
        }

        int total = await query.CountAsync();
        var items = await query.OrderByDescending(b => b.HareketTarihi)
                               .Skip(q.Skip).Take(q.Take)
                               .Select(b => new BankaHareketiListItemDto
                               {
                                   Id = b.Id,
                                   HareketTarihi = b.HareketTarihi,
                                   Tutar = b.Tutar,
                                   Aciklama = b.Aciklama,
                                   KarsiHesap = b.KarsiHesap,
                                   KarsiUnvan = b.KarsiUnvan,
                                   BankaKodu = b.BankaKodu,
                                   EslesmeDurumu = b.EslesmeDurumu,
                                   ImportTarihi = b.ImportTarihi,
                                   ImportEdenUserAdi = b.ImportEdenUser != null ? b.ImportEdenUser.UserName : null
                               })
                               .ToListAsync();

        return new PagedResult<BankaHareketiListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    public async Task<BankaHareketiDetayDto?> GetDetayAsync(int id)
        => await _dbSet.AsNoTracking()
                       .Where(b => b.Id == id)
                       .Select(b => new BankaHareketiDetayDto
                       {
                           Id = b.Id,
                           HareketTarihi = b.HareketTarihi,
                           Tutar = b.Tutar,
                           Aciklama = b.Aciklama,
                           KarsiHesap = b.KarsiHesap,
                           KarsiUnvan = b.KarsiUnvan,
                           Bakiye = b.Bakiye,
                           BankaKodu = b.BankaKodu,
                           EslesmeDurumu = b.EslesmeDurumu,
                           ImportTarihi = b.ImportTarihi,
                           ImportEdenUserAdi = b.ImportEdenUser != null ? b.ImportEdenUser.UserName : null,
                           Eslesmeleri = b.OdemeEslesmeleri.Select(e => new OdemeBankaEslesmeDto
                           {
                               Id = e.Id,
                               EslesmeTipi = e.EslesmeTipi,
                               BankaHareketiTutar = b.Tutar,
                               BankaHareketiTarih = b.HareketTarihi,
                               BankaHareketiAciklama = b.Aciklama
                           }).ToList()
                       })
                       .FirstOrDefaultAsync();

    // ── Eşleştirme adayları ───────────────────────────────────────────────
    public async Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, string? userId = null)
    {
        var hareketi = await _dbSet.AsNoTracking()
                                   .Where(b => b.Id == bankaHareketiId)
                                   .Select(b => new { b.Tutar, b.HareketTarihi })
                                   .FirstOrDefaultAsync();
        if (hareketi == null) return [];

        decimal tutar = hareketi.Tutar;
        DateTime tarih = hareketi.HareketTarihi;
        decimal tolerans = tutar * 0.02m;

        IQueryable<KiraOdeme> query = _ctx.KiraOdemeler.AsNoTracking()
            .Where(o => o.Durum == OdemeDurumu.OnayBekliyor || o.Durum == OdemeDurumu.Onaylandi)
            .Where(o => !_ctx.OdemeBankaEslesmeleri.Any(e => e.KiraOdemeId == o.Id && e.BankaHareketiId == bankaHareketiId));

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(o => o.KiraTahakkuk.KiraSozlesmesiId != null
                && yetkiliIds.Contains(o.KiraTahakkuk.KiraSozlesmesi!.Birim.TasinmazId));
        }

        var liste = await query.Select(o => new OdemeAdayDto
        {
            Id = o.Id,
            Tutar = o.Tutar,
            OdemeTarihi = o.OdemeTarihi,
            Durum = o.Durum,
            KiraciGosterimAdi = o.KiraTahakkuk.KiraSozlesmesi != null
                ? (o.KiraTahakkuk.KiraSozlesmesi.Kiraci.KiraciTuru == KiraciTuru.Gercek
                    ? (o.KiraTahakkuk.KiraSozlesmesi.Kiraci.Ad + " " + o.KiraTahakkuk.KiraSozlesmesi.Kiraci.Soyad).Trim()
                    : o.KiraTahakkuk.KiraSozlesmesi.Kiraci.Ad)
                : "Rezervasyon Ödemesi",
            DonemBaslangic = o.KiraTahakkuk.DonemBaslangic
        }).ToListAsync();

        return liste.OrderBy(o =>
        {
            bool tutarExact = o.Tutar == tutar;
            bool tutarClose = Math.Abs(o.Tutar - tutar) <= tolerans;
            int gunFark = Math.Abs((o.OdemeTarihi - tarih).Days);
            if (tutarExact && gunFark <= 15) return 0;
            if (tutarExact) return 1;
            if (tutarClose && gunFark <= 15) return 2;
            return 3;
        })
        .ThenBy(o => Math.Abs((o.OdemeTarihi - tarih).Days))
        .ThenBy(o => Math.Abs(o.Tutar - tutar))
        .ToList();
    }

    public async Task<List<BankaHareketiListItemDto>> GetHareketAdaylariAsync(int odemeId)
    {
        var odeme = await _ctx.KiraOdemeler.AsNoTracking()
                              .Where(o => o.Id == odemeId)
                              .Select(o => new { o.Tutar, o.OdemeTarihi })
                              .FirstOrDefaultAsync();
        if (odeme == null) return [];

        decimal tutar = odeme.Tutar;
        DateTime tarih = odeme.OdemeTarihi;
        decimal tolerans = tutar * 0.02m;

        var liste = await _dbSet.AsNoTracking()
            .Where(b => b.EslesmeDurumu == BankaEslesmeDurumu.Eslestirilmedi)
            .Where(b => !_ctx.OdemeBankaEslesmeleri.Any(e => e.BankaHareketiId == b.Id && e.KiraOdemeId == odemeId))
            .Select(b => new BankaHareketiListItemDto
            {
                Id = b.Id,
                HareketTarihi = b.HareketTarihi,
                Tutar = b.Tutar,
                Aciklama = b.Aciklama,
                KarsiHesap = b.KarsiHesap,
                KarsiUnvan = b.KarsiUnvan,
                BankaKodu = b.BankaKodu,
                EslesmeDurumu = b.EslesmeDurumu,
                ImportTarihi = b.ImportTarihi
            })
            .ToListAsync();

        return liste.OrderBy(b =>
        {
            bool tutarExact = b.Tutar == tutar;
            bool tutarClose = Math.Abs(b.Tutar - tutar) <= tolerans;
            int gunFark = Math.Abs((b.HareketTarihi - tarih).Days);
            if (tutarExact && gunFark <= 15) return 0;
            if (tutarExact) return 1;
            if (tutarClose && gunFark <= 15) return 2;
            return 3;
        })
        .ThenBy(b => Math.Abs((b.HareketTarihi - tarih).Days))
        .ThenBy(b => Math.Abs(b.Tutar - tutar))
        .ToList();
    }

    // ── Eşleştirme yazma işlemleri ────────────────────────────────────────
    public Task<bool> EslesmeVarMiAsync(int kiraOdemeId, int bankaHareketiId)
        => _ctx.OdemeBankaEslesmeleri.AsNoTracking()
               .AnyAsync(e => e.KiraOdemeId == kiraOdemeId && e.BankaHareketiId == bankaHareketiId);

    public async Task AddEslesmeAsync(OdemeBankaEslesme eslesme)
        => await _ctx.OdemeBankaEslesmeleri.AddAsync(eslesme);

    public Task<OdemeBankaEslesme?> GetEslesmeWithBankaHareketiAsync(int eslesmeId)
        => _ctx.OdemeBankaEslesmeleri
               .Include(e => e.BankaHareketi)
               .FirstOrDefaultAsync(e => e.Id == eslesmeId);

    public Task RemoveEslesmeAsync(OdemeBankaEslesme eslesme)
    {
        _ctx.OdemeBankaEslesmeleri.Remove(eslesme);
        return Task.CompletedTask;
    }

    public Task<bool> KalanEslesmeVarMiAsync(int bankaHareketiId, int excludeEslesmeId)
        => _ctx.OdemeBankaEslesmeleri.AsNoTracking()
               .AnyAsync(e => e.BankaHareketiId == bankaHareketiId && e.Id != excludeEslesmeId);

    // ── Toplu ekleme (CSV import) ─────────────────────────────────────────
    public async Task AddRangeAsync(IEnumerable<BankaHareketi> entities)
        => await _ctx.BankaHareketleri.AddRangeAsync(entities);
}
