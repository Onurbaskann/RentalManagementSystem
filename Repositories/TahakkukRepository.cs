using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Repositories;

public class TahakkukRepository : ITahakkukRepository
{
    private readonly ApplicationDbContext _ctx;

    public TahakkukRepository(ApplicationDbContext ctx) => _ctx = ctx;

    // ── Temel sorgu bloğu (tekrar eden Include zinciri bir kez tanımlanır) ─────

    private IQueryable<KiraTahakkuk> BaseQuery() =>
        _ctx.KiraTahakkuklar
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Birim)
                    .ThenInclude(b => b.Tasinmaz)
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci);

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    public async Task<List<KiraTahakkuk>> GetAllAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<KiraTahakkuk> query = BaseQuery()
            .Include(t => t.Kalemler)
                .ThenInclude(k => k.BorcTipi);

        if (sozlesmeId.HasValue)
            query = query.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(t => yetkiliTasinmazIds.Contains(t.KiraSozlesmesi.Birim.TasinmazId));

        return await query.OrderByDescending(t => t.DonemBaslangic).ToListAsync();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    public async Task<PagedResult<KiraTahakkuk>> GetPagedAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<KiraTahakkuk> query = BaseQuery()
            .Include(t => t.Kalemler)
                .ThenInclude(k => k.BorcTipi);

        if (sozlesmeId.HasValue)
            query = query.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(t => yetkiliTasinmazIds.Contains(t.KiraSozlesmesi.Birim.TasinmazId));

        // Metin araması
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(t =>
                EF.Functions.Like(t.KiraSozlesmesi.Kiraci.Ad, $"%{s}%") ||
                (t.KiraSozlesmesi.Kiraci.Soyad != null && EF.Functions.Like(t.KiraSozlesmesi.Kiraci.Soyad, $"%{s}%")) ||
                EF.Functions.Like(t.KiraSozlesmesi.Birim.Tasinmaz.Ad, $"%{s}%"));
        }

        // Tarih aralığı
        if (q.From.HasValue) query = query.Where(t => t.DonemBaslangic >= q.From.Value);
        if (q.To.HasValue)   query = query.Where(t => t.DonemBaslangic <= q.To.Value);

        // Tutar aralığı
        if (q.Min.HasValue) query = query.Where(t => t.ToplamTutar >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(t => t.ToplamTutar <= q.Max.Value);

        // Diğer filtreler
        if (q.TasinmazId.HasValue) query = query.Where(t => t.KiraSozlesmesi.Birim.TasinmazId == q.TasinmazId.Value);
        if (q.BirimId.HasValue)    query = query.Where(t => t.KiraSozlesmesi.BirimId == q.BirimId.Value);
        if (q.KiraciId.HasValue)   query = query.Where(t => t.KiraSozlesmesi.KiraciId == q.KiraciId.Value);
        if (q.Yil.HasValue)        query = query.Where(t => t.DonemBaslangic.Year == q.Yil.Value);

        // Kaynak tipi filtresi
        if (!string.IsNullOrWhiteSpace(q.Kaynak))
        {
            TahakkukKaynakTipi? kt = q.Kaynak.ToLower() switch
            {
                "manuel"      => TahakkukKaynakTipi.Manuel,
                "otomatik"    => TahakkukKaynakTipi.Otomatik,
                "rezervasyon" => TahakkukKaynakTipi.Rezervasyon,
                _             => null
            };
            if (kt.HasValue) query = query.Where(t => t.KaynakTipi == kt.Value);
        }

        // Durum filtresi
        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            TahakkukDurumu? d = q.Durum.ToLower() switch
            {
                "bekliyor"  => TahakkukDurumu.Bekleniyor,
                "kismi"     => TahakkukDurumu.KismenOdendi,
                "tamodendi" => TahakkukDurumu.TamOdendi,
                "gecikti"   => TahakkukDurumu.Gecikti,
                _           => null
            };
            if (d.HasValue) query = query.Where(t => t.Durum == d.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.DonemBaslangic)
            .Skip(q.Skip).Take(q.Take)
            .ToListAsync();

        return new PagedResult<KiraTahakkuk>
        {
            Items = items,
            Total = total,
            Page  = Math.Max(1, q.Page),
            Size  = q.SafeSize
        };
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    public async Task<KiraTahakkuk?> GetByIdAsync(int id) =>
        await BaseQuery()
            .Include(t => t.Kalemler)
                .ThenInclude(k => k.BorcTipi)
            .Include(t => t.Odemeler)
            .FirstOrDefaultAsync(t => t.Id == id);

    // ── GetSozlesmeAsync ──────────────────────────────────────────────────────

    public async Task<KiraSozlesmesi?> GetSozlesmeAsync(int sozlesmeId) =>
        await _ctx.Sozlesmeler.FindAsync(sozlesmeId);

    // ── ExistsForDonemAsync ───────────────────────────────────────────────────

    public async Task<bool> ExistsForDonemAsync(int sozlesmeId, DateTime donemIlkGunu) =>
        await _ctx.KiraTahakkuklar
            .AnyAsync(t =>
                t.KiraSozlesmesiId == sozlesmeId &&
                t.DonemBaslangic   == donemIlkGunu &&
                t.KaynakTipi       == TahakkukKaynakTipi.Otomatik);

    // ── GetGeciktirileceklerAsync ─────────────────────────────────────────────

    public async Task<List<KiraTahakkuk>> GetGeciktirileceklerAsync(DateTime bugun) =>
        await _ctx.KiraTahakkuklar
            .Where(t =>
                t.Durum != TahakkukDurumu.TamOdendi &&
                t.Durum != TahakkukDurumu.IptalEdildi &&
                t.VadeTarihi < bugun)
            .ToListAsync();

    // ── GetOdenenTutarAsync ───────────────────────────────────────────────────

    public async Task<decimal> GetOdenenTutarAsync(int tahakkukId) =>
        await _ctx.KiraOdemeler
            .Where(o => o.KiraTahakkukId == tahakkukId && o.Durum == OdemeDurumu.Onaylandi)
            .SumAsync(o => (decimal?)o.Tutar) ?? 0m;

    // ── FindAsync ─────────────────────────────────────────────────────────────

    public async Task<KiraTahakkuk?> FindAsync(int id) =>
        await _ctx.KiraTahakkuklar.FindAsync(id);

    // ── AddAsync / SaveChangesAsync ───────────────────────────────────────────

    public async Task AddAsync(KiraTahakkuk tahakkuk)
    {
        await _ctx.KiraTahakkuklar.AddAsync(tahakkuk);
    }

    public async Task SaveChangesAsync() =>
        await _ctx.SaveChangesAsync();
}
