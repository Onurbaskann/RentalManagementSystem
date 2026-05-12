using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ManuelBorcService : IManuelBorcService
{
    private readonly ApplicationDbContext _ctx;

    public ManuelBorcService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<KiraTahakkuk>> GetAllAsync(string? userId = null)
    {
        var query = _ctx.KiraTahakkuklar
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Birim)
                    .ThenInclude(b => b.Tasinmaz)
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci)
            .Include(t => t.Kalemler)
                .ThenInclude(k => k.BorcTipi)
            .Where(t => t.KaynakTipi == TahakkukKaynakTipi.Manuel)
            .AsQueryable();

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(t => t.KiraSozlesmesi != null && yetkiliIds.Contains(t.KiraSozlesmesi.Birim.TasinmazId));
        }

        return await query.OrderByDescending(t => t.OlusturmaTarihi).ToListAsync();
    }

    public async Task<KiraTahakkuk?> GetByIdAsync(int id)
    {
        return await _ctx.KiraTahakkuklar
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Birim)
                    .ThenInclude(b => b.Tasinmaz)
            .Include(t => t.KiraSozlesmesi)
                .ThenInclude(s => s.Kiraci)
            .Include(t => t.Kalemler)
                .ThenInclude(k => k.BorcTipi)
            .Include(t => t.Odemeler)
            .FirstOrDefaultAsync(t => t.Id == id && t.KaynakTipi == TahakkukKaynakTipi.Manuel);
    }

    public async Task<(bool Basarili, string? Hata, int TahakkukId)> CreateAsync(
        ManuelBorcCreateViewModel model, string userId)
    {
        var sozlesme = await _ctx.Sozlesmeler
            .Include(s => s.Kiraci)
            .Include(s => s.Birim)
            .FirstOrDefaultAsync(s => s.Id == model.SozlesmeId);

        if (sozlesme == null)
            return (false, "Sözleşme bulunamadı.", 0);

        if (sozlesme.Durum == SozlesmeDurumu.Feshedildi)
            return (false, "Feshedilmiş sözleşme için manuel borç oluşturulamaz.", 0);

        var borcTipi = await _ctx.BorcTipleri
            .FirstOrDefaultAsync(b => b.Id == model.BorcTipiId && b.Aktif && b.Davranis == BorcTipiDavranisi.KullaniciManuel);

        if (borcTipi == null)
            return (false, "Geçersiz borç tipi.", 0);

        if (model.Tutar <= 0)
            return (false, "Tutar sıfırdan büyük olmalıdır.", 0);

        var kdvTutari = model.KdvUygulanacakMi
            ? Math.Round(model.Tutar * model.KdvOrani / 100, 2)
            : 0m;
        var toplamTutar = model.Tutar + kdvTutari;
        var kdvOrani = model.KdvUygulanacakMi ? model.KdvOrani : 0m;

        var kalem = new TahakkukKalemi
        {
            BorcTipiId       = borcTipi.Id,
            Aciklama         = model.Aciklama,
            HesaplamaYontemi = HesaplamaYontemi.Sabit,
            BirimDeger       = model.Tutar,
            Carpan           = 1m,
            Tutar            = model.Tutar,
            KdvOrani         = kdvOrani,
            KdvTutari        = kdvTutari,
            ToplamTutar      = toplamTutar,
            KaynakTipi       = KaynakTipi.Sozlesme
        };

        var tahakkuk = new KiraTahakkuk
        {
            KiraSozlesmesiId = sozlesme.Id,
            DonemBaslangic   = model.VadeTarihi,
            DonemBitis       = model.VadeTarihi,
            VadeTarihi       = model.VadeTarihi,
            BeklenenTutar    = model.Tutar,
            KdvTutari        = kdvTutari,
            ToplamTutar      = toplamTutar,
            OdenenTutar      = 0,
            Durum            = TahakkukDurumu.Bekleniyor,
            KaynakTipi       = TahakkukKaynakTipi.Manuel,
            IptalNotu        = model.Not,
            OlusturmaTarihi  = DateTime.Now,
            Kalemler         = new List<TahakkukKalemi> { kalem }
        };

        _ctx.KiraTahakkuklar.Add(tahakkuk);
        await _ctx.SaveChangesAsync();

        return (true, null, tahakkuk.Id);
    }

    public async Task<(bool Basarili, string? Hata)> CancelAsync(int tahakkukId, string userId, string neden)
    {
        var tahakkuk = await _ctx.KiraTahakkuklar
            .Include(t => t.Odemeler)
            .FirstOrDefaultAsync(t => t.Id == tahakkukId && t.KaynakTipi == TahakkukKaynakTipi.Manuel);

        if (tahakkuk == null)
            return (false, "Manuel borç kaydı bulunamadı.");

        if (tahakkuk.Durum == TahakkukDurumu.IptalEdildi)
            return (false, "Bu kayıt zaten iptal edilmiş.");

        var odemeVar = tahakkuk.Odemeler.Any(o => o.Durum == OdemeDurumu.Onaylandi);
        if (odemeVar)
            return (false, "Ödemesi alınmış manuel borç iptal edilemez.");

        tahakkuk.Durum = TahakkukDurumu.IptalEdildi;
        tahakkuk.IptalNotu = neden;
        await _ctx.SaveChangesAsync();

        return (true, null);
    }
}
