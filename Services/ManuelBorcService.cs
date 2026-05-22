using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ManuelBorcService : IManuelBorcService
{
    private readonly ITahakkukRepository _tahakkukRepo;
    private readonly ISozlesmeRepository _sozlesmeRepo;
    private readonly IBorcTipiRepository _borcTipiRepo;
    private readonly IUnitOfWork _uow;
    private readonly IUserTasinmazYetkiService _yetkiService;

    public ManuelBorcService(
        ITahakkukRepository tahakkukRepo,
        ISozlesmeRepository sozlesmeRepo,
        IBorcTipiRepository borcTipiRepo,
        IUnitOfWork uow,
        IUserTasinmazYetkiService yetkiService)
    {
        _tahakkukRepo = tahakkukRepo;
        _sozlesmeRepo = sozlesmeRepo;
        _borcTipiRepo = borcTipiRepo;
        _uow = uow;
        _yetkiService = yetkiService;
    }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<ManuelBorcListItemDto>> GetAllAsync(string? userId = null)
    {
        List<int>? yetkiliIds = null;
        if (userId != null)
            yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);

        return await _tahakkukRepo.GetManuelBorcListAsync(yetkiliIds);
    }

    // ── Dropdown verileri ────────────────────────────────────────────────
    public Task<List<SozlesmeDropdownDto>> GetAktifSozlesmelerAsync()
        => _sozlesmeRepo.GetAktifDropdownAsync();

    public Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync()
        => _borcTipiRepo.GetManuelBorcTipleriAsync();

    // ── Create ────────────────────────────────────────────────────────────
    public async Task<(bool Basarili, string? Hata, int TahakkukId)> CreateAsync(
        ManuelBorcCreateViewModel model, string userId)
    {
        var sozlesme = await _sozlesmeRepo.GetByIdAsync(model.SozlesmeId);
        if (sozlesme == null)
            return (false, "Sözleşme bulunamadı.", 0);

        if (sozlesme.Durum == SozlesmeDurumu.Feshedildi)
            return (false, "Feshedilmiş sözleşme için manuel borç oluşturulamaz.", 0);

        var borcTipi = await _borcTipiRepo.GetActiveManuelByIdAsync(model.BorcTipiId);
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
            BorcTipiId = borcTipi.Id,
            Aciklama = model.Aciklama,
            HesaplamaYontemi = HesaplamaYontemi.Sabit,
            BirimDeger = model.Tutar,
            Carpan = 1m,
            Tutar = model.Tutar,
            KdvOrani = kdvOrani,
            KdvTutari = kdvTutari,
            ToplamTutar = toplamTutar,
            KaynakTipi = KalemKaynakTipi.ManuelGiris
        };

        var tahakkuk = new KiraTahakkuk
        {
            KiraSozlesmesiId = sozlesme.Id,
            DonemBaslangic = model.VadeTarihi,
            DonemBitis = model.VadeTarihi,
            VadeTarihi = model.VadeTarihi,
            BeklenenTutar = model.Tutar,
            KdvTutari = kdvTutari,
            ToplamTutar = toplamTutar,
            OdenenTutar = 0,
            Durum = TahakkukDurumu.Bekleniyor,
            KaynakTipi = TahakkukKaynakTipi.Manuel,
            IptalNotu = model.Not,
            OlusturmaTarihi = DateTime.Now,
            Kalemler = new List<TahakkukKalemi> { kalem }
        };

        await _tahakkukRepo.AddAsync(tahakkuk);
        await _uow.SaveChangesAsync();

        return (true, null, tahakkuk.Id);
    }

    // ── Cancel ────────────────────────────────────────────────────────────
    public async Task<(bool Basarili, string? Hata)> CancelAsync(int tahakkukId, string userId, string neden)
    {
        var tahakkuk = await _tahakkukRepo.GetManuelBorcByIdAsync(tahakkukId);

        if (tahakkuk == null)
            return (false, "Manuel borç kaydı bulunamadı.");

        if (tahakkuk.Durum == TahakkukDurumu.IptalEdildi)
            return (false, "Bu kayıt zaten iptal edilmiş.");

        var odemeVar = tahakkuk.Odemeler.Any(o => o.Durum == OdemeDurumu.Onaylandi);
        if (odemeVar)
            return (false, "Ödemesi alınmış manuel borç iptal edilemez.");

        tahakkuk.Durum = TahakkukDurumu.IptalEdildi;
        tahakkuk.IptalNotu = string.IsNullOrEmpty(tahakkuk.IptalNotu)
            ? neden
            : $"{tahakkuk.IptalNotu} | İptal: {neden}";

        await _uow.SaveChangesAsync();

        return (true, null);
    }
}
