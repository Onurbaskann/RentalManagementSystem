using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class RezervasyonService : IRezervasyonService
{
    private readonly IRezervasyonRepository _repo;
    private readonly IRezervasyonTarifeRepository _tarifeRepo;
    private readonly IBirimRepository _birimRepo;
    private readonly IKiraciRepository _kiraciRepo;
    private readonly IUnitOfWork _uow;
    public RezervasyonService(
        IRezervasyonRepository repo,
        IRezervasyonTarifeRepository tarifeRepo,
        IBirimRepository birimRepo,
        IKiraciRepository kiraciRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _tarifeRepo = tarifeRepo;
        _birimRepo = birimRepo;
        _kiraciRepo = kiraciRepo;
        _uow = uow;
    }

    // ── Listeleme ──────────────────────────────────────────────────────────────

    public async Task<List<RezervasyonListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetListAsync(tasinmazIds?.ToList());
    }

    // ── Ücret Hesaplama (precedence: birime özel → birim türü genel tarife → hata) ─

    public async Task<RezervasyonHesapSonucu> HesaplaAsync(int birimId, DateTime baslangic, DateTime bitis)
    {
        var sonuc = new RezervasyonHesapSonucu();

        if (bitis <= baslangic)
        {
            sonuc.HataMessaji = "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.";
            return sonuc;
        }

        // 1) Birime özel kural
        var kural = await _repo.GetAktifTarifeForBirimAsync(birimId);

        int ucretsiz;
        int periyot;
        decimal ucret;
        decimal kdv;

        if (kural != null)
        {
            ucretsiz = kural.UcretsizSureDakika;
            periyot = kural.UcretlendirmePeriyoduDakika;
            ucret = kural.PeriyotUcreti;
            kdv = kural.KdvOrani;
            sonuc.KuralBulundu = true;
        }
        else
        {
            // 2) Birim Türü bazlı Yıllık Genel Tarife
            var birim = await _birimRepo.GetByIdAsync(birimId, q => q.Include(b => b.BirimTuru));

            if (birim?.BirimTuruId is not int btId)
            {
                sonuc.HataMessaji = "Birim türü tanımlanmamış.";
                return sonuc;
            }

            int cariYil = baslangic.Year;
            var genel = await _repo.GetGenelTarifeAsync(btId, cariYil);

            if (genel == null)
            {
                sonuc.HataMessaji = $"{cariYil} yılı için '{birim.BirimTuru?.Ad}' türünde genel rezervasyon tarifesi tanımlı değil.";
                return sonuc;
            }

            ucretsiz = genel.UcretsizSureDakika;
            periyot = genel.UcretlendirmePeriyoduDakika;
            ucret = genel.PeriyotUcreti;
            kdv = genel.KdvOrani;
            sonuc.KuralBulundu = true;
        }

        var toplamDakika = (int)Math.Ceiling((bitis - baslangic).TotalMinutes);
        var ucretliDakika = Math.Max(0, toplamDakika - ucretsiz);
        var periyotSayisi = ucretliDakika == 0
            ? 0
            : (int)Math.Ceiling((double)ucretliDakika / periyot);

        sonuc.ToplamSureDakika = toplamDakika;
        sonuc.UcretsizSureDakika = Math.Min(ucretsiz, toplamDakika);
        sonuc.UcretliSureDakika = ucretliDakika;
        sonuc.UcretliPeriyotSayisi = periyotSayisi;
        sonuc.BirimUcret = ucret;
        sonuc.UcretTutar = periyotSayisi * ucret;
        sonuc.KdvOrani = kdv;
        sonuc.KdvTutari = Math.Round(sonuc.UcretTutar * kdv / 100, 2);
        sonuc.ToplamTutar = sonuc.UcretTutar + sonuc.KdvTutari;

        return sonuc;
    }

    // ── Rezervasyon Oluşturma ─────────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata, int RezervasyonId)> CreateAsync(
        RezervasyonCreateViewModel model, string userId)
    {
        if (model.BitisTarihi <= model.BaslangicTarihi)
            return (false, "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", 0);

        // 8.5.4 — Çakışma kontrolü
        if (await _repo.IsConflictAsync(model.BirimId.Value, model.BaslangicTarihi, model.BitisTarihi))
            return (false, "Seçilen zaman aralığında bu birim için başka bir rezervasyon mevcut.", 0);

        var kiraci = await _kiraciRepo.GetByIdAsync(model.KiraciId.Value);
        if (kiraci == null)
            return (false, "Kiracı bulunamadı.", 0);

        var birim = await _birimRepo.GetByIdAsync(model.BirimId.Value, q => q.Include(b => b.BirimTuru));
        if (birim == null)
            return (false, "Birim bulunamadı.", 0);
        if (birim.BirimTuru == null || !birim.BirimTuru.RezervasyonYapilabilirMi)
            return (false, "Seçilen birim rezervasyon yapılabilir türde değil.", 0);

        var hesap = await HesaplaAsync(model.BirimId.Value, model.BaslangicTarihi, model.BitisTarihi);

        var rezervasyon = new Rezervasyon
        {
            BirimId = model.BirimId.Value,
            KiraciId = model.KiraciId.Value,
            BaslangicTarihi = model.BaslangicTarihi,
            BitisTarihi = model.BitisTarihi,
            ToplamSureDakika = hesap.ToplamSureDakika,
            UcretsizSureDakika = hesap.UcretsizSureDakika,
            UcretliSureDakika = hesap.UcretliSureDakika,
            BirimUcret = hesap.BirimUcret,
            UcretTutar = hesap.UcretTutar,
            KdvOrani = hesap.KdvOrani > 0 ? hesap.KdvOrani : null,
            KdvTutari = hesap.KdvTutari > 0 ? hesap.KdvTutari : null,
            ToplamTutar = hesap.ToplamTutar,
            Durum = RezervasyonDurumu.Planlandi,
            Aciklama = model.Aciklama,
            OlusturanUserId = userId,
            OlusturmaTarihi = DateTime.Now
        };

        await _repo.AddAsync(rezervasyon);
        await _uow.SaveChangesAsync();

        return (true, null, rezervasyon.Id);
    }

    // ── İptal ────────────────────────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata)> CancelAsync(int id, string userId, string neden)
    {
        var rezervasyon = await _repo.GetByIdAsync(id, q => q
            .Include(r => r.Tahakkuk!)
                .ThenInclude(t => t!.Odemeler));

        if (rezervasyon == null)
            return (false, "Rezervasyon bulunamadı.");

        if (rezervasyon.Durum == RezervasyonDurumu.IptalEdildi)
            return (false, "Bu rezervasyon zaten iptal edilmiş.");

        if (rezervasyon.Durum == RezervasyonDurumu.TahakkukaAktarildi)
        {
            var odemeVar = rezervasyon.Tahakkuk?.Odemeler
                .Any(o => o.Durum == OdemeDurumu.Onaylandi) ?? false;
            if (odemeVar)
                return (false, "Ödemesi alınmış tahakkuka bağlı rezervasyon iptal edilemez.");

            if (rezervasyon.Tahakkuk != null)
            {
                rezervasyon.Tahakkuk.Durum = TahakkukDurumu.IptalEdildi;
                rezervasyon.Tahakkuk.IptalNotu = $"Rezervasyon iptal edildi: {neden}";
            }
        }

        rezervasyon.Durum = RezervasyonDurumu.IptalEdildi;
        rezervasyon.Aciklama = string.IsNullOrWhiteSpace(rezervasyon.Aciklama)
            ? $"İptal: {neden}"
            : $"{rezervasyon.Aciklama} | İptal: {neden}";

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ── Tahakkuka Aktar (8.6.2) ──────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata, int? TahakkukId)> TransferToTahakkukAsync(int id, string userId)
    {
        var rezervasyon = await _repo.GetByIdAsync(id, q => q
            .Include(r => r.Birim).ThenInclude(b => b.BirimTuru));

        if (rezervasyon == null)
            return (false, "Rezervasyon bulunamadı.", null);

        if (rezervasyon.Durum != RezervasyonDurumu.Planlandi)
            return (false, "Sadece 'Planlandı' durumundaki rezervasyonlar tahakkuka aktarılabilir.", null);

        if (rezervasyon.TahakkukId != null)
            return (false, "Bu rezervasyon zaten tahakkuka aktarılmış.", null);

        if (rezervasyon.ToplamTutar <= 0)
            return (false, "Ücretsiz rezervasyonlar için tahakkuk oluşturulamaz.", null);

        var birimTuru = rezervasyon.Birim.BirimTuru;
        var borcTipi = await _repo.ResolveRezervasyonBorcTipiAsync(birimTuru?.BorcTipiId);

        if (borcTipi == null)
            return (false, "Rezervasyon borç tipi bulunamadı. Lütfen yöneticinize başvurun.", null);

        var aciklama = $"Toplantı salonu: {rezervasyon.Birim.Ad} " +
                       $"({rezervasyon.BaslangicTarihi:dd.MM.yyyy HH:mm} – {rezervasyon.BitisTarihi:HH:mm})";

        var kalem = new TahakkukKalemi
        {
            BorcTipiId = borcTipi.Id,
            Aciklama = aciklama,
            HesaplamaYontemi = HesaplamaYontemi.Sabit,
            BirimDeger = rezervasyon.UcretTutar,
            Carpan = 1m,
            Tutar = rezervasyon.UcretTutar,
            KdvOrani = rezervasyon.KdvOrani ?? 0m,
            KdvTutari = rezervasyon.KdvTutari ?? 0m,
            ToplamTutar = rezervasyon.ToplamTutar,
            KaynakTipi = KalemKaynakTipi.RezervasyonKurali
        };

        var tahakkuk = new Tahakkuk
        {
            KiraciId = rezervasyon.KiraciId,
            DonemBaslangic = rezervasyon.BaslangicTarihi.Date,
            DonemBitis = rezervasyon.BitisTarihi.Date,
            VadeTarihi = rezervasyon.BitisTarihi.Date,
            BeklenenTutar = rezervasyon.UcretTutar,
            KdvTutari = rezervasyon.KdvTutari ?? 0m,
            ToplamTutar = rezervasyon.ToplamTutar,
            OdenenTutar = 0,
            Durum = TahakkukDurumu.Bekleniyor,
            KaynakTipi = TahakkukKaynakTipi.Rezervasyon,
            Kalemler = new List<TahakkukKalemi> { kalem }
        };

        await _repo.AddTahakkukAsync(tahakkuk);
        await _uow.SaveChangesAsync();

        rezervasyon.TahakkukId = tahakkuk.Id;
        rezervasyon.Durum = RezervasyonDurumu.TahakkukaAktarildi;
        await _uow.SaveChangesAsync();

        return (true, null, tahakkuk.Id);
    }

    // ── Ücret Kuralı CRUD ─────────────────────────────────────────────────────

    public async Task<List<RezervasyonTarifeKuralListItemDto>> GetUcretKurallariAsync()
        => await _tarifeRepo.GetUcretKurallariListAsync();

    public async Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id)
        => await _repo.GetUcretKuralByIdAsync(id);

    public async Task<(bool Basarili, string? Hata, int Id)> SaveUcretKuralAsync(RezervasyonTarifeKuralViewModel model)
    {
        if (model.UcretlendirmePeriyoduDakika <= 0)
            return (false, "Periyot süresi sıfırdan büyük olmalıdır.", 0);

        RezervasyonTarife kural;
        if (model.Id == 0)
        {
            kural = new RezervasyonTarife { OlusturmaTarihi = DateTime.Now };
            await _repo.AddUcretKuralAsync(kural);
        }
        else
        {
            kural = await _repo.GetUcretKuralByIdAsync(model.Id)
                    ?? throw new InvalidOperationException("Kural bulunamadı.");
        }

        kural.BirimId = model.BirimId;
        kural.UcretsizSureDakika = model.UcretsizSureDakika;
        kural.UcretlendirmePeriyoduDakika = model.UcretlendirmePeriyoduDakika;
        kural.PeriyotUcreti = model.PeriyotUcreti;
        kural.KdvOrani = model.KdvOrani;
        kural.Aktif = model.Aktif;
        kural.Aciklama = model.Aciklama;

        await _uow.SaveChangesAsync();
        return (true, null, kural.Id);
    }

    public async Task<(bool Basarili, string? Hata)> ToggleUcretKuralAktifAsync(int id)
    {
        var kural = await _repo.GetUcretKuralByIdAsync(id);
        if (kural == null)
            return (false, "Kural bulunamadı.");

        kural.Aktif = !kural.Aktif;
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
