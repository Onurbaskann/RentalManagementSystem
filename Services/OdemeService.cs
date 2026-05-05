using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class OdemeService : IOdemeService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ITahakkukService _tahakkukService;

    public OdemeService(ApplicationDbContext ctx, ITahakkukService tahakkukService)
    {
        _ctx = ctx;
        _tahakkukService = tahakkukService;
    }

    public async Task<List<KiraOdeme>> GetAllAsync(int? tahakkukId = null, string? userId = null)
    {
        var query = _ctx.KiraOdemeler
            .Include(o => o.KiraTahakkuk)
                .ThenInclude(t => t.KiraSozlesmesi)
                    .ThenInclude(s => s.Birim)
                        .ThenInclude(b => b.Tasinmaz)
            .Include(o => o.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci)
            .Include(o => o.GirenUser)
            .Include(o => o.OnaylayanUser)
            .Include(o => o.Dekontlar)
            .AsQueryable();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.KiraTahakkukId == tahakkukId.Value);

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(o => yetkiliIds.Contains(o.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId));
        }

        return await query.OrderByDescending(o => o.GirisTarihi).ToListAsync();
    }

    public async Task<PagedResult<KiraOdeme>> GetPagedAsync(TableQuery q, int? tahakkukId = null, string? userId = null)
    {
        var query = _ctx.KiraOdemeler
            .Include(o => o.KiraTahakkuk)
                .ThenInclude(t => t.KiraSozlesmesi)
                    .ThenInclude(s => s.Birim)
                        .ThenInclude(b => b.Tasinmaz)
            .Include(o => o.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci)
            .Include(o => o.GirenUser)
            .Include(o => o.OnaylayanUser)
            .Include(o => o.Dekontlar)
            .AsQueryable();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.KiraTahakkukId == tahakkukId.Value);

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(o => yetkiliIds.Contains(o.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId));
        }

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.KiraSozlesmesi.Kiraci.Ad, $"%{s}%") ||
                (o.KiraSozlesmesi.Kiraci.Soyad != null && EF.Functions.Like(o.KiraSozlesmesi.Kiraci.Soyad, $"%{s}%")) ||
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

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.GirisTarihi)
            .Skip(q.Skip).Take(q.Take)
            .ToListAsync();
        return new PagedResult<KiraOdeme> { Items = items, Total = total, Page = Math.Max(1, q.Page), Size = q.SafeSize };
    }

    public async Task<KiraOdeme?> GetByIdAsync(int id)
    {
        return await _ctx.KiraOdemeler
            .Include(o => o.KiraTahakkuk)
                .ThenInclude(t => t.KiraSozlesmesi)
                    .ThenInclude(s => s.Kiraci)
            .Include(o => o.GirenUser)
            .Include(o => o.OnaylayanUser)
            .Include(o => o.Dekontlar)
            .Include(o => o.BankaEslesmeleri)
                .ThenInclude(e => e.BankaHareketi)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<KiraOdeme> EkleAsync(KiraOdeme odeme)
    {
        odeme.GirisTarihi = DateTime.Now;
        odeme.Durum = OdemeDurumu.OnayBekliyor;
        _ctx.KiraOdemeler.Add(odeme);
        await _ctx.SaveChangesAsync();
        return odeme;
    }

    public async Task<bool> OnaylaAsync(int id, string onaylayanUserId)
    {
        var odeme = await _ctx.KiraOdemeler.FindAsync(id);
        if (odeme == null || odeme.Durum != OdemeDurumu.OnayBekliyor) return false;

        odeme.Durum = OdemeDurumu.Onaylandi;
        odeme.OnaylayanUserId = onaylayanUserId;
        odeme.OnayTarihi = DateTime.Now;
        await _ctx.SaveChangesAsync();

        await _tahakkukService.OdenenTutarGuncelleAsync(odeme.KiraTahakkukId);
        return true;
    }

    public async Task<bool> ReddetAsync(int id, string neden)
    {
        var odeme = await _ctx.KiraOdemeler.FindAsync(id);
        if (odeme == null || odeme.Durum != OdemeDurumu.OnayBekliyor) return false;

        odeme.Durum = OdemeDurumu.Reddedildi;
        odeme.RedNedeni = neden;
        await _ctx.SaveChangesAsync();

        await _tahakkukService.OdenenTutarGuncelleAsync(odeme.KiraTahakkukId);
        return true;
    }
}
