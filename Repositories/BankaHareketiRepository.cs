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
    public async Task<List<BankaHareketiListItemDto>> GetListAsync(BankMatchStatus? durum = null)
    {
        IQueryable<BankaHareketi> q = _dbSet.AsNoTracking();
        if (durum.HasValue) q = q.Where(b => b.EslesmeDurumu == durum.Value);
        return await q.OrderByDescending(b => b.IslemTarihi)
                      .Select(b => new BankaHareketiListItemDto
                      {
                          Id = b.Id,
                          IslemTarihi = b.IslemTarihi,
                          IslemTutari = b.IslemTutari,
                          Aciklama = b.Aciklama,
                          GonderenIban = b.GonderenIban,
                          GonderenBilgisi = b.GonderenBilgisi,
                          BankaKodu = b.BankaKodu,
                          EslesmeDurumu = b.EslesmeDurumu,
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
                (b.GonderenBilgisi != null && EF.Functions.Like(b.GonderenBilgisi, $"%{s}%")) ||
                (b.GonderenIban != null && EF.Functions.Like(b.GonderenIban, $"%{s}%")));
        }
        if (q.From.HasValue) query = query.Where(b => b.IslemTarihi >= q.From.Value);
        if (q.To.HasValue) query = query.Where(b => b.IslemTarihi <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(b => b.IslemTutari >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(b => b.IslemTutari <= q.Max.Value);
        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            BankMatchStatus? d = q.Durum switch
            {
                "eslestirilmedi" => BankMatchStatus.Unmatched,
                "eslesti" => BankMatchStatus.Matched,
                "manuel" => BankMatchStatus.ManuallyMatched,
                _ => null
            };
            if (d.HasValue) query = query.Where(b => b.EslesmeDurumu == d.Value);
        }

        int total = await query.CountAsync();
        var items = await query.OrderByDescending(b => b.IslemTarihi)
                               .Skip(q.Skip).Take(q.Take)
                               .Select(b => new BankaHareketiListItemDto
                               {
                                   Id = b.Id,
                                   IslemTarihi = b.IslemTarihi,
                                   IslemTutari = b.IslemTutari,
                                   Aciklama = b.Aciklama,
                                   GonderenIban = b.GonderenIban,
                                   GonderenBilgisi = b.GonderenBilgisi,
                                   BankaKodu = b.BankaKodu,
                                   EslesmeDurumu = b.EslesmeDurumu,
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
                           IslemTarihi = b.IslemTarihi,
                           IslemTutari = b.IslemTutari,
                           Aciklama = b.Aciklama,
                           GonderenIban = b.GonderenIban,
                           GonderenBilgisi = b.GonderenBilgisi,
                           BankaKodu = b.BankaKodu,
                           EslesmeDurumu = b.EslesmeDurumu,
                           Eslesmeleri = b.OdemeEslesmeleri.Select(e => new OdemeBankaEslesmeDto
                           {
                               Id = e.Id,
                               MatchType = e.MatchType,
                               BankaHareketiTutar = b.IslemTutari,
                               BankaHareketiTarih = b.IslemTarihi,
                               BankaHareketiAciklama = b.Aciklama
                           }).ToList()
                       })
                       .FirstOrDefaultAsync();

    // ── Eşleştirme adayları ───────────────────────────────────────────────
    public async Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, IReadOnlyList<int>? tasinmazIds = null)
    {
        var hareketi = await _dbSet.AsNoTracking()
                                   .Where(b => b.Id == bankaHareketiId)
                                   .Select(b => new { b.IslemTutari, b.IslemTarihi })
                                   .FirstOrDefaultAsync();
        if (hareketi == null) return [];

        decimal tutar = hareketi.IslemTutari;
        DateTime tarih = hareketi.IslemTarihi;
        decimal tolerans = tutar * 0.02m;

        IQueryable<TahakkukOdeme> query = _ctx.TahakkukOdemeler.AsNoTracking()
            .Where(o => o.Durum == PaymentStatus.PendingApproval || o.Durum == PaymentStatus.Approved)
            .Where(o => !_ctx.OdemeBankaEslesmeleri.Any(e => e.TahakkukOdemeId == o.Id && e.BankaHareketiId == bankaHareketiId));

        if (tasinmazIds != null)
        {
            var ids = tasinmazIds.ToList();
            query = query.Where(o => ids.Contains(o.Tahakkuk.Birim.TasinmazId));
        }

        var liste = await query.Select(o => new OdemeAdayDto
        {
            Id = o.Id,
            Tutar = o.Tutar,
            OdemeTarihi = o.OdemeTarihi,
            Durum = o.Durum,
            KiraciGosterimAdi = o.Tahakkuk.Kiraci.Ad,
            DonemBaslangic = o.Tahakkuk.DonemBaslangic
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
        var odeme = await _ctx.TahakkukOdemeler.AsNoTracking()
                              .Where(o => o.Id == odemeId)
                              .Select(o => new { o.Tutar, o.OdemeTarihi })
                              .FirstOrDefaultAsync();
        if (odeme == null) return [];

        decimal tutar = odeme.Tutar;
        DateTime tarih = odeme.OdemeTarihi;
        decimal tolerans = tutar * 0.02m;

        var liste = await _dbSet.AsNoTracking()
            .Where(b => b.EslesmeDurumu == BankMatchStatus.Unmatched)
            .Where(b => !_ctx.OdemeBankaEslesmeleri.Any(e => e.BankaHareketiId == b.Id && e.TahakkukOdemeId == odemeId))
            .Select(b => new BankaHareketiListItemDto
            {
                Id = b.Id,
                IslemTarihi = b.IslemTarihi,
                IslemTutari = b.IslemTutari,
                Aciklama = b.Aciklama,
                GonderenIban = b.GonderenIban,
                GonderenBilgisi = b.GonderenBilgisi,
                BankaKodu = b.BankaKodu,
                EslesmeDurumu = b.EslesmeDurumu,
            })
            .ToListAsync();

        return liste.OrderBy(b =>
        {
            bool tutarExact = b.IslemTutari == tutar;
            bool tutarClose = Math.Abs(b.IslemTutari - tutar) <= tolerans;
            int gunFark = Math.Abs((b.IslemTarihi - tarih).Days);
            if (tutarExact && gunFark <= 15) return 0;
            if (tutarExact) return 1;
            if (tutarClose && gunFark <= 15) return 2;
            return 3;
        })
        .ThenBy(b => Math.Abs((b.IslemTarihi - tarih).Days))
        .ThenBy(b => Math.Abs(b.IslemTutari - tutar))
        .ToList();
    }

    // ── Eşleştirme yazma işlemleri ────────────────────────────────────────
    public Task<bool> EslesmeVarMiAsync(int kiraOdemeId, int bankaHareketiId)
        => _ctx.OdemeBankaEslesmeleri.AsNoTracking()
               .AnyAsync(e => e.TahakkukOdemeId == kiraOdemeId && e.BankaHareketiId == bankaHareketiId);

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
