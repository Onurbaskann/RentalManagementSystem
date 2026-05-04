using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
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
