using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TasinmazService : ITasinmazService
{
    private readonly ApplicationDbContext _ctx;

    public TasinmazService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<Tasinmaz>> GetAllAsync(string? userId = null)
    {
        var query = _ctx.Tasinmazlar
            .Include(t => t.TasinmazTipi)
            .Include(t => t.Birimler)
                .ThenInclude(b => b.Sozlesmeler)
                    .ThenInclude(s => s.Kiraci)
            .AsQueryable();

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(t => yetkiliIds.Contains(t.Id));
        }

        return await query.OrderBy(t => t.Ad).ToListAsync();
    }

    public async Task<Tasinmaz?> GetByIdAsync(int id)
    {
        return await _ctx.Tasinmazlar
            .Include(t => t.TasinmazTipi)
            .Include(t => t.Birimler)
                .ThenInclude(b => b.BirimTuru)
            .Include(t => t.Birimler)
                .ThenInclude(b => b.Sozlesmeler)
                    .ThenInclude(s => s.Kiraci)
            .Include(t => t.Birimler)
                .ThenInclude(b => b.Sozlesmeler)
                    .ThenInclude(s => s.IslemGecmisi)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tasinmaz> CreateAsync(Tasinmaz t, List<OfisBirimInputViewModel>? ofisler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null)
    {
        t.KayitTarihi = DateTime.Now;

        if (t.KiralamaSekli == KiralamaSekli.BirimBazli && ofisler != null && ofisler.Count > 0)
        {
            foreach (var o in ofisler)
            {
                var ad = string.IsNullOrWhiteSpace(o.Ad) ? $"Birim {o.OfisNo}" : o.Ad;
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Ofis,
                    OfisNo = o.OfisNo,
                    KatNo = o.KatNo,
                    Ad = ad,
                    Yuzolcumu = o.Yuzolcumu,
                    Aciklama = o.Aciklama,
                    BirimTuruId = o.BirimTuruId
                });
            }
        }
        else
        {
            t.Birimler.Add(new Birim
            {
                BirimTipi = BirimTipi.Komple,
                Ad = "Komple",
                Yuzolcumu = t.KapaliYuzolcumu > 0 ? t.KapaliYuzolcumu : t.AcikYuzolcumu
            });
        }

        if (rezervasyonAlanlari != null && rezervasyonAlanlari.Count > 0)
        {
            foreach (var r in rezervasyonAlanlari)
            {
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Ofis,
                    Ad = string.IsNullOrWhiteSpace(r.Ad) ? "Rezervasyon Alanı" : r.Ad,
                    Yuzolcumu = r.Yuzolcumu,
                    Aciklama = r.Aciklama,
                    BirimTuruId = r.BirimTuruId
                });
            }
        }

        _ctx.Tasinmazlar.Add(t);
        await _ctx.SaveChangesAsync();
        return t;
    }

    public async Task UpdateAsync(Tasinmaz t)
    {
        _ctx.Tasinmazlar.Update(t);
        await _ctx.SaveChangesAsync();
    }

    public async Task<List<Birim>> GetBosBirimlerAsync(string? userId = null)
    {
        var now = DateTime.Now;
        var query = _ctx.Birimler
            .Include(b => b.Tasinmaz)
            .Include(b => b.Sozlesmeler)
            .Where(b => !b.Sozlesmeler.Any(s =>
                s.Durum == SozlesmeDurumu.Aktif &&
                s.BaslangicTarihi <= now &&
                s.BitisTarihi >= now))
            .AsQueryable();

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(b => yetkiliIds.Contains(b.TasinmazId));
        }

        return await query.ToListAsync();
    }
}
