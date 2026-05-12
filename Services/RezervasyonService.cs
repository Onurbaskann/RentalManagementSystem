using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RezervasyonService : IRezervasyonService
{
    private readonly ApplicationDbContext _ctx;

    public RezervasyonService(ApplicationDbContext ctx) => _ctx = ctx;

    // ── Listeme ──────────────────────────────────────────────────────────────

    public async Task<List<ToplantiSalonuRezervasyon>> GetAllAsync(string? userId = null)
    {
        var query = _ctx.ToplantiSalonuRezervasyonlari
            .Include(r => r.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(r => r.Kiraci)
            .Include(r => r.KiraSozlesmesi)
            .Include(r => r.KiraTahakkuk)
            .AsQueryable();

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(r => yetkiliIds.Contains(r.Birim.TasinmazId));
        }

        return await query.OrderByDescending(r => r.OlusturmaTarihi).ToListAsync();
    }

    public async Task<ToplantiSalonuRezervasyon?> GetByIdAsync(int id)
    {
        return await _ctx.ToplantiSalonuRezervasyonlari
            .Include(r => r.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(r => r.Kiraci)
            .Include(r => r.KiraSozlesmesi)
            .Include(r => r.KiraTahakkuk)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // ── Ücret Hesaplama (14. bölüm formülü) ─────────────────────────────────

    public async Task<RezervasyonHesapSonucu> HesaplaAsync(int birimId, DateTime baslangic, DateTime bitis)
    {
        var sonuc = new RezervasyonHesapSonucu();

        if (bitis <= baslangic)
        {
            sonuc.HataMessaji = "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.";
            return sonuc;
        }

        // Birime özel kural önce, yoksa genel kural
        var kural = await _ctx.RezervasyonUcretKurallari
            .Where(k => k.Aktif && k.BirimId == birimId)
            .FirstOrDefaultAsync()
            ?? await _ctx.RezervasyonUcretKurallari
            .Where(k => k.Aktif && k.BirimId == null)
            .FirstOrDefaultAsync();

        if (kural == null)
        {
            sonuc.HataMessaji = "Bu birim için tanımlı ücret kuralı bulunamadı.";
            return sonuc;
        }

        sonuc.KuralBulundu = true;

        var toplamDakika = (int)Math.Ceiling((bitis - baslangic).TotalMinutes);
        var ucretliDakika = Math.Max(0, toplamDakika - kural.UcretsizSureDakika);
        var periyotSayisi = ucretliDakika == 0
            ? 0
            : (int)Math.Ceiling((double)ucretliDakika / kural.UcretlendirmePeriyoduDakika);

        sonuc.ToplamSureDakika    = toplamDakika;
        sonuc.UcretsizSureDakika  = Math.Min(kural.UcretsizSureDakika, toplamDakika);
        sonuc.UcretliSureDakika   = ucretliDakika;
        sonuc.UcretliPeriyotSayisi = periyotSayisi;
        sonuc.BirimUcret          = kural.PeriyotUcreti;
        sonuc.UcretTutar          = periyotSayisi * kural.PeriyotUcreti;
        sonuc.KdvOrani            = kural.KdvOrani;
        sonuc.KdvTutari           = Math.Round(sonuc.UcretTutar * kural.KdvOrani / 100, 2);
        sonuc.ToplamTutar         = sonuc.UcretTutar + sonuc.KdvTutari;

        return sonuc;
    }

    // ── Rezervasyon Oluşturma (8.5.4 çakışma kontrolü dahil) ─────────────────

    public async Task<(bool Basarili, string? Hata, int RezervasyonId)> CreateAsync(
        RezervasyonCreateViewModel model, string userId)
    {
        if (model.BitisTarihi <= model.BaslangicTarihi)
            return (false, "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", 0);

        // 8.5.4 — Çakışma kontrolü: iptal edilmiş rezervasyonlar hariç
        var cakismaVar = await _ctx.ToplantiSalonuRezervasyonlari
            .AnyAsync(r =>
                r.BirimId == model.BirimId &&
                r.Durum != RezervasyonDurumu.IptalEdildi &&
                r.BaslangicTarihi < model.BitisTarihi &&
                r.BitisTarihi > model.BaslangicTarihi);

        if (cakismaVar)
            return (false, "Seçilen zaman aralığında bu birim için başka bir rezervasyon mevcut.", 0);

        // Kiracıyı kontrol et
        var kiraci = await _ctx.Kiraciler.FindAsync(model.KiraciId);
        if (kiraci == null)
            return (false, "Kiracı bulunamadı.", 0);

        // Birim + BirimTuru kontrolü
        var birim = await _ctx.Birimler
            .Include(b => b.BirimTuru)
            .FirstOrDefaultAsync(b => b.Id == model.BirimId);
        if (birim == null)
            return (false, "Birim bulunamadı.", 0);
        if (birim.BirimTuru == null || !birim.BirimTuru.RezervasyonYapilabilirMi)
            return (false, "Seçilen birim rezervasyon yapılabilir türde değil.", 0);

        // Ücret hesapla
        var hesap = await HesaplaAsync(model.BirimId, model.BaslangicTarihi, model.BitisTarihi);
        if (!string.IsNullOrEmpty(hesap.HataMessaji) && !hesap.KuralBulundu)
        {
            // Ücret kuralı yoksa rezervasyon 0 ₺ olarak yine de oluşturulabilir
        }

        var rezervasyon = new ToplantiSalonuRezervasyon
        {
            BirimId           = model.BirimId,
            KiraciId          = model.KiraciId,
            KiraSozlesmesiId  = model.KiraSozlesmesiId,
            BaslangicTarihi   = model.BaslangicTarihi,
            BitisTarihi       = model.BitisTarihi,
            ToplamSureDakika  = hesap.ToplamSureDakika,
            UcretsizSureDakika = hesap.UcretsizSureDakika,
            UcretliSureDakika = hesap.UcretliSureDakika,
            BirimUcret        = hesap.BirimUcret,
            UcretTutar        = hesap.UcretTutar,
            KdvOrani          = hesap.KdvOrani > 0 ? hesap.KdvOrani : null,
            KdvTutari         = hesap.KdvTutari > 0 ? hesap.KdvTutari : null,
            ToplamTutar       = hesap.ToplamTutar,
            Durum             = RezervasyonDurumu.Planlandi,
            Aciklama          = model.Aciklama,
            OlusturanUserId   = userId,
            OlusturmaTarihi   = DateTime.Now
        };

        _ctx.ToplantiSalonuRezervasyonlari.Add(rezervasyon);
        await _ctx.SaveChangesAsync();

        return (true, null, rezervasyon.Id);
    }

    // ── İptal ────────────────────────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata)> CancelAsync(int id, string userId, string neden)
    {
        var rezervasyon = await _ctx.ToplantiSalonuRezervasyonlari
            .Include(r => r.KiraTahakkuk)
                .ThenInclude(t => t!.Odemeler)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rezervasyon == null)
            return (false, "Rezervasyon bulunamadı.");

        if (rezervasyon.Durum == RezervasyonDurumu.IptalEdildi)
            return (false, "Bu rezervasyon zaten iptal edilmiş.");

        if (rezervasyon.Durum == RezervasyonDurumu.TahakkukaAktarildi)
        {
            var odemeVar = rezervasyon.KiraTahakkuk?.Odemeler
                .Any(o => o.Durum == OdemeDurumu.Onaylandi) ?? false;
            if (odemeVar)
                return (false, "Ödemesi alınmış tahakkuka bağlı rezervasyon iptal edilemez.");

            // Bağlı tahakkuku da iptal et (ödeme alınmamışsa)
            if (rezervasyon.KiraTahakkuk != null)
            {
                rezervasyon.KiraTahakkuk.Durum     = TahakkukDurumu.IptalEdildi;
                rezervasyon.KiraTahakkuk.IptalNotu = $"Rezervasyon iptal edildi: {neden}";
            }
        }

        rezervasyon.Durum    = RezervasyonDurumu.IptalEdildi;
        rezervasyon.Aciklama = string.IsNullOrWhiteSpace(rezervasyon.Aciklama)
            ? $"İptal: {neden}"
            : $"{rezervasyon.Aciklama} | İptal: {neden}";

        await _ctx.SaveChangesAsync();
        return (true, null);
    }

    // ── Tahakkuka Aktar (8.6.2) ──────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata, int? TahakkukId)> TransferToTahakkukAsync(int id, string userId)
    {
        var rezervasyon = await GetByIdAsync(id);
        if (rezervasyon == null)
            return (false, "Rezervasyon bulunamadı.", null);

        if (rezervasyon.Durum != RezervasyonDurumu.Planlandi)
            return (false, "Sadece 'Planlandı' durumundaki rezervasyonlar tahakkuka aktarılabilir.", null);

        if (rezervasyon.KiraTahakkukId != null)
            return (false, "Bu rezervasyon zaten tahakkuka aktarılmış.", null);

        if (rezervasyon.ToplamTutar <= 0)
            return (false, "Ücretsiz rezervasyonlar için tahakkuk oluşturulamaz.", null);

        var borcTipi = await _ctx.BorcTipleri
            .FirstOrDefaultAsync(b => b.Davranis == BorcTipiDavranisi.RezervasyonOzel && b.Aktif);
        if (borcTipi == null)
            return (false, "Rezervasyon borç tipi bulunamadı. Lütfen yöneticinize başvurun.", null);

        var aciklama = $"Toplantı salonu: {rezervasyon.Birim.Ad} " +
                       $"({rezervasyon.BaslangicTarihi:dd.MM.yyyy HH:mm} – {rezervasyon.BitisTarihi:HH:mm})";

        var kalem = new TahakkukKalemi
        {
            BorcTipiId       = borcTipi.Id,
            Aciklama         = aciklama,
            HesaplamaYontemi = HesaplamaYontemi.Sabit,
            BirimDeger       = rezervasyon.UcretTutar,
            Carpan           = 1m,
            Tutar            = rezervasyon.UcretTutar,
            KdvOrani         = rezervasyon.KdvOrani ?? 0m,
            KdvTutari        = rezervasyon.KdvTutari ?? 0m,
            ToplamTutar      = rezervasyon.ToplamTutar,
            KaynakTipi       = KaynakTipi.Sozlesme
        };

        var tahakkuk = new KiraTahakkuk
        {
            KiraSozlesmesiId = rezervasyon.KiraSozlesmesiId,
            DonemBaslangic   = rezervasyon.BaslangicTarihi.Date,
            DonemBitis       = rezervasyon.BitisTarihi.Date,
            VadeTarihi       = rezervasyon.BitisTarihi.Date,
            BeklenenTutar    = rezervasyon.UcretTutar,
            KdvTutari        = rezervasyon.KdvTutari ?? 0m,
            ToplamTutar      = rezervasyon.ToplamTutar,
            OdenenTutar      = 0,
            Durum            = TahakkukDurumu.Bekleniyor,
            KaynakTipi       = TahakkukKaynakTipi.Rezervasyon,
            OlusturmaTarihi  = DateTime.Now,
            Kalemler         = new List<TahakkukKalemi> { kalem }
        };

        _ctx.KiraTahakkuklar.Add(tahakkuk);
        await _ctx.SaveChangesAsync();

        rezervasyon.KiraTahakkukId = tahakkuk.Id;
        rezervasyon.Durum          = RezervasyonDurumu.TahakkukaAktarildi;
        await _ctx.SaveChangesAsync();

        return (true, null, tahakkuk.Id);
    }

    // ── Ücret Kuralı CRUD ─────────────────────────────────────────────────────

    public async Task<List<RezervasyonUcretKural>> GetUcretKurallariAsync()
    {
        return await _ctx.RezervasyonUcretKurallari
            .Include(k => k.Birim).ThenInclude(b => b!.Tasinmaz)
            .OrderBy(k => k.BirimId == null ? 0 : 1)
            .ThenBy(k => k.Id)
            .ToListAsync();
    }

    public async Task<RezervasyonUcretKural?> GetUcretKuralByIdAsync(int id)
    {
        return await _ctx.RezervasyonUcretKurallari
            .Include(k => k.Birim)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<(bool Basarili, string? Hata, int Id)> SaveUcretKuralAsync(RezervasyonUcretKuralViewModel model)
    {
        if (model.UcretlendirmePeriyoduDakika <= 0)
            return (false, "Periyot süresi sıfırdan büyük olmalıdır.", 0);

        RezervasyonUcretKural kural;
        if (model.Id == 0)
        {
            kural = new RezervasyonUcretKural { OlusturmaTarihi = DateTime.Now };
            _ctx.RezervasyonUcretKurallari.Add(kural);
        }
        else
        {
            kural = await _ctx.RezervasyonUcretKurallari.FindAsync(model.Id)
                    ?? throw new InvalidOperationException("Kural bulunamadı.");
        }

        kural.BirimId                      = model.BirimId;
        kural.UcretsizSureDakika           = model.UcretsizSureDakika;
        kural.UcretlendirmePeriyoduDakika  = model.UcretlendirmePeriyoduDakika;
        kural.PeriyotUcreti                = model.PeriyotUcreti;
        kural.KdvOrani                     = model.KdvOrani;
        kural.Aktif                        = model.Aktif;
        kural.Aciklama                     = model.Aciklama;

        await _ctx.SaveChangesAsync();
        return (true, null, kural.Id);
    }

    public async Task<(bool Basarili, string? Hata)> ToggleUcretKuralAktifAsync(int id)
    {
        var kural = await _ctx.RezervasyonUcretKurallari.FindAsync(id);
        if (kural == null)
            return (false, "Kural bulunamadı.");

        kural.Aktif = !kural.Aktif;
        await _ctx.SaveChangesAsync();
        return (true, null);
    }
}
