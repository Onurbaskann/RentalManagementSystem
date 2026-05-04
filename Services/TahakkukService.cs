using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TahakkukService : ITahakkukService
{
    private readonly ApplicationDbContext _ctx;

    public TahakkukService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<KiraTahakkuk>> GetAllAsync(int? sozlesmeId = null, string? userId = null)
    {
        var query = _ctx.KiraTahakkuklar
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Birim)
                    .ThenInclude(b => b.Tasinmaz)
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci)
            .AsQueryable();

        if (sozlesmeId.HasValue)
            query = query.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(t => yetkiliIds.Contains(t.KiraSozlesmesi.Birim.TasinmazId));
        }

        return await query.OrderByDescending(t => t.DonemBaslangic).ToListAsync();
    }

    public async Task<KiraTahakkuk?> GetByIdAsync(int id)
    {
        return await _ctx.KiraTahakkuklar
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Birim)
                    .ThenInclude(b => b.Tasinmaz)
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci)
            .Include(t => t.Odemeler)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(bool Basarili, string? Hata)> OlusturAsync(int sozlesmeId, DateTime donemBaslangic)
    {
        var sozlesme = await _ctx.Sozlesmeler.FindAsync(sozlesmeId);
        if (sozlesme == null)
            return (false, "Sözleşme bulunamadı.");

        if (sozlesme.Durum == SozlesmeDurumu.Feshedildi)
            return (false, "Feshedilmiş sözleşme için tahakkuk oluşturulamaz.");

        var donemIlkGunu = new DateTime(donemBaslangic.Year, donemBaslangic.Month, 1);
        var mevcutVar = await _ctx.KiraTahakkuklar
            .AnyAsync(t => t.KiraSozlesmesiId == sozlesmeId && t.DonemBaslangic == donemIlkGunu);

        if (mevcutVar)
            return (false, $"{donemIlkGunu:MMMM yyyy} dönemi için tahakkuk zaten mevcut.");

        var kdvTutari = sozlesme.KdvUygulanacakMi
            ? Math.Round(sozlesme.KiraBedeli * sozlesme.KdvOrani / 100, 2)
            : 0m;

        var tahakkuk = new KiraTahakkuk
        {
            KiraSozlesmesiId = sozlesmeId,
            DonemBaslangic = donemIlkGunu,
            DonemBitis = new DateTime(donemIlkGunu.Year, donemIlkGunu.Month,
                DateTime.DaysInMonth(donemIlkGunu.Year, donemIlkGunu.Month)),
            VadeTarihi = donemIlkGunu,
            BeklenenTutar = sozlesme.KiraBedeli,
            KdvTutari = kdvTutari,
            ToplamTutar = sozlesme.KiraBedeli + kdvTutari,
            OdenenTutar = 0,
            Durum = TahakkukDurumu.Bekleniyor,
            OlusturmaTarihi = DateTime.Now
        };

        _ctx.KiraTahakkuklar.Add(tahakkuk);
        await _ctx.SaveChangesAsync();
        return (true, null);
    }

    public async Task GecikmeleriGuncelleAsync()
    {
        var bugun = DateTime.Today;
        var guncellenmesi = await _ctx.KiraTahakkuklar
            .Where(t => t.Durum != TahakkukDurumu.TamOdendi && t.VadeTarihi < bugun)
            .ToListAsync();

        foreach (var t in guncellenmesi)
            t.Durum = TahakkukDurumu.Gecikti;

        if (guncellenmesi.Count > 0)
            await _ctx.SaveChangesAsync();
    }

    public async Task OdenenTutarGuncelleAsync(int tahakkukId)
    {
        var tahakkuk = await _ctx.KiraTahakkuklar.FindAsync(tahakkukId);
        if (tahakkuk == null) return;

        var odenenTutar = await _ctx.KiraOdemeler
            .Where(o => o.KiraTahakkukId == tahakkukId && o.Durum == OdemeDurumu.Onaylandi)
            .SumAsync(o => (decimal?)o.Tutar) ?? 0m;

        tahakkuk.OdenenTutar = odenenTutar;

        if (odenenTutar >= tahakkuk.ToplamTutar)
            tahakkuk.Durum = TahakkukDurumu.TamOdendi;
        else if (odenenTutar > 0)
            tahakkuk.Durum = TahakkukDurumu.KismenOdendi;
        else
            tahakkuk.Durum = DateTime.Today > tahakkuk.VadeTarihi
                ? TahakkukDurumu.Gecikti
                : TahakkukDurumu.Bekleniyor;

        await _ctx.SaveChangesAsync();
    }
}
