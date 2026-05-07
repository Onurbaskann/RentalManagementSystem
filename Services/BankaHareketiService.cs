using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Services.Banka;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class BankaHareketiService : IBankaHareketiService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IEnumerable<IBankaHareketiParser> _parsers;

    public BankaHareketiService(ApplicationDbContext ctx, IEnumerable<IBankaHareketiParser> parsers)
    {
        _ctx = ctx;
        _parsers = parsers;
    }

    public async Task<(int Adet, Guid BatchId)> ImportAsync(Stream dosya, string bankaKodu, string userId)
    {
        var parser = _parsers.FirstOrDefault(p =>
            p.BankaKodu.Equals(bankaKodu, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"'{bankaKodu}' için parser bulunamadı.");

        var batchId = Guid.NewGuid();
        var hareketler = parser.Parse(dosya, batchId, userId).ToList();

        _ctx.BankaHareketleri.AddRange(hareketler);
        await _ctx.SaveChangesAsync();
        return (hareketler.Count, batchId);
    }

    public async Task<List<BankaHareketi>> GetAllAsync(BankaEslesmeDurumu? durum = null)
    {
        var query = _ctx.BankaHareketleri
            .Include(b => b.ImportEdenUser)
            .AsQueryable();

        if (durum.HasValue)
            query = query.Where(b => b.EslesmeDurumu == durum.Value);

        return await query.OrderByDescending(b => b.HareketTarihi).ToListAsync();
    }

    public async Task<PagedResult<BankaHareketi>> GetPagedAsync(TableQuery q)
    {
        var query = _ctx.BankaHareketleri
            .Include(b => b.ImportEdenUser)
            .AsQueryable();

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
        var items = await query
            .OrderByDescending(b => b.HareketTarihi)
            .Skip(q.Skip).Take(q.Take)
            .ToListAsync();
        return new PagedResult<BankaHareketi> { Items = items, Total = total, Page = Math.Max(1, q.Page), Size = q.SafeSize };
    }

    public async Task<BankaHareketi?> GetByIdAsync(int id)
        => await _ctx.BankaHareketleri
            .Include(b => b.ImportEdenUser)
            .Include(b => b.OdemeEslesmeleri)
                .ThenInclude(e => e.KiraOdeme)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task EslestirAsync(int odemeId, int bankaHareketiId, string userId)
    {
        var hareketi = await _ctx.BankaHareketleri.FindAsync(bankaHareketiId)
            ?? throw new InvalidOperationException("Banka hareketi bulunamadı.");

        var mevcutVar = await _ctx.OdemeBankaEslesmeleri
            .AnyAsync(e => e.KiraOdemeId == odemeId && e.BankaHareketiId == bankaHareketiId);
        if (mevcutVar) return;

        var eslesme = new OdemeBankaEslesme
        {
            KiraOdemeId      = odemeId,
            BankaHareketiId  = bankaHareketiId,
            EslesmeTipi      = EslesmeTipi.Manuel,
            EslestirenUserId = userId,
            EslesmeTarihi    = DateTime.Now
        };

        hareketi.EslesmeDurumu = BankaEslesmeDurumu.ManuelEslesti;
        _ctx.OdemeBankaEslesmeleri.Add(eslesme);
        await _ctx.SaveChangesAsync();
    }

    public async Task<List<KiraOdeme>> GetOdemeAdaylariAsync(int bankaHareketiId, string? userId = null)
    {
        var hareketi = await _ctx.BankaHareketleri.FindAsync(bankaHareketiId);
        if (hareketi == null) return new List<KiraOdeme>();

        decimal tutar = hareketi.Tutar;
        DateTime tarih = hareketi.HareketTarihi;
        decimal tolerans = tutar * 0.02m;

        var query = _ctx.KiraOdemeler
            .Include(o => o.KiraTahakkuk)
                .ThenInclude(t => t.KiraSozlesmesi)
                    .ThenInclude(s => s.Kiraci)
            .Include(o => o.KiraTahakkuk)
                .ThenInclude(t => t.KiraSozlesmesi)
                    .ThenInclude(s => s.Birim)
                        .ThenInclude(b => b.Tasinmaz)
            .Where(o => o.Durum == OdemeDurumu.OnayBekliyor || o.Durum == OdemeDurumu.Onaylandi)
            .Where(o => !_ctx.OdemeBankaEslesmeleri.Any(e => e.KiraOdemeId == o.Id && e.BankaHareketiId == bankaHareketiId));

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(o => o.KiraTahakkuk.KiraSozlesmesiId != null && yetkiliIds.Contains(o.KiraTahakkuk.KiraSozlesmesi!.Birim.TasinmazId));
        }

        var liste = await query.ToListAsync();

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

    public async Task<List<BankaHareketi>> GetHareketAdaylariAsync(int odemeId)
    {
        var odeme = await _ctx.KiraOdemeler.FindAsync(odemeId);
        if (odeme == null) return new List<BankaHareketi>();

        decimal tutar = odeme.Tutar;
        DateTime tarih = odeme.OdemeTarihi;
        decimal tolerans = tutar * 0.02m;

        var liste = await _ctx.BankaHareketleri
            .Where(b => b.EslesmeDurumu == BankaEslesmeDurumu.Eslestirilmedi)
            .Where(b => !_ctx.OdemeBankaEslesmeleri.Any(e => e.BankaHareketiId == b.Id && e.KiraOdemeId == odemeId))
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

    public async Task EslesmeCozAsync(int eslesmeId)
    {
        var eslesme = await _ctx.OdemeBankaEslesmeleri
            .Include(e => e.BankaHareketi)
            .FirstOrDefaultAsync(e => e.Id == eslesmeId);
        if (eslesme == null) return;

        _ctx.OdemeBankaEslesmeleri.Remove(eslesme);

        var kalanEslesme = await _ctx.OdemeBankaEslesmeleri
            .AnyAsync(e => e.BankaHareketiId == eslesme.BankaHareketiId && e.Id != eslesmeId);

        if (!kalanEslesme)
            eslesme.BankaHareketi.EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi;

        await _ctx.SaveChangesAsync();
    }
}
