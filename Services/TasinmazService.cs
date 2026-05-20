using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Entities;

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
            .Include(t => t.Birimler)
                .ThenInclude(b => b.Sozlesmeler)
                    .ThenInclude(s => s.SozlesmeTarifeler)
                        .ThenInclude(r => r.BorcTipi)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tasinmaz> CreateAsync(Tasinmaz t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null)
    {
        t.KayitTarihi = DateTime.Now;

        if (t.KiralamaSekli == KiralamaSekli.BirimBazli && birimler != null && birimler.Count > 0)
        {
            foreach (var b in birimler)
            {
                var ad = string.IsNullOrWhiteSpace(b.Ad) ? $"Birim {b.BirimNo}" : b.Ad;
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = b.BirimNo,
                    KatNo = b.KatNo,
                    Ad = ad,
                    Yuzolcumu = b.Yuzolcumu,
                    Aciklama = b.Aciklama,
                    BirimTuruId = b.BirimTuruId
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
                var birim = new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    Ad = string.IsNullOrWhiteSpace(r.Ad) ? "Rezervasyon Alanı" : r.Ad,
                    Yuzolcumu = r.Yuzolcumu,
                    Aciklama = r.Aciklama,
                    BirimTuruId = r.BirimTuruId
                };
                t.Birimler.Add(birim);

                // Ücret kuralını ekle
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Birim = birim,
                    UcretsizSureDakika = r.UcretsizSureDakika,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = r.SaatlikUcret,
                    KdvOrani = r.KdvOrani,
                    Aktif = true,
                    OlusturmaTarihi = DateTime.Now,
                    Aciklama = $"{r.Ad} için otomatik oluşturuldu"
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
