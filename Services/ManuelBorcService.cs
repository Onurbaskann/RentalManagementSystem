using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ManuelBorcService : IManuelBorcService, ITransactionalService
{
    private readonly ITahakkukRepository _tahakkukRepo;
    private readonly ISozlesmeRepository _sozlesmeRepo;
    private readonly IBorcTipiRepository _borcTipiRepo;
    private readonly ITasinmazRepository _tasinmazRepo;
    private readonly IUnitOfWork _uow;

    public ManuelBorcService(
        ITahakkukRepository tahakkukRepo,
        ISozlesmeRepository sozlesmeRepo,
        IBorcTipiRepository borcTipiRepo,
        ITasinmazRepository tasinmazRepo,
        IUnitOfWork uow)
    {
        _tahakkukRepo = tahakkukRepo;
        _sozlesmeRepo = sozlesmeRepo;
        _borcTipiRepo = borcTipiRepo;
        _tasinmazRepo = tasinmazRepo;
        _uow = uow;
    }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<ManuelBorcListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null, string? durum = null, string? baglanti = null, int? sozlesmeId = null, IReadOnlyList<int>? birimIds = null)
        => await _tahakkukRepo.GetManuelBorcListAsync(tasinmazIds?.ToList(), durum, baglanti, sozlesmeId, birimIds?.ToList());

    public async Task<int> GetIptalSayisiAsync(IReadOnlyList<int>? tasinmazIds = null, IReadOnlyList<int>? birimIds = null)
        => await _tahakkukRepo.GetManuelBorcIptalSayisiAsync(tasinmazIds?.ToList(), birimIds?.ToList());

    // ── Dropdown verileri ────────────────────────────────────────────────
    public Task<List<SozlesmeDropdownDto>> GetAktifSozlesmelerAsync()
        => _sozlesmeRepo.GetAktifDropdownAsync();

    public Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync()
        => _borcTipiRepo.GetManuelBorcTipleriAsync();

    public Task<List<BirimLookupDto>> GetTumBirimlerAsync(IReadOnlyList<int>? tasinmazIds = null)
        => _tasinmazRepo.GetTumBirimlerAsync(tasinmazIds?.ToList());

    // ── Create ────────────────────────────────────────────────────────────
    public async Task<(bool Basarili, string? Hata, int TahakkukId)> CreateAsync(
        ManuelBorcCreateViewModel model, string userId)
    {
        if (model.KiraciId <= 0)
            return (false, "Kiracı seçilmelidir.", 0);
        if (model.BirimId <= 0)
            return (false, "Birim seçilmelidir.", 0);

        int? kiraSozlesmesiId = null;
        if (model.SozlesmeId.HasValue && model.SozlesmeId.Value > 0)
        {
            var sozlesme = await _sozlesmeRepo.GetByIdAsync(model.SozlesmeId.Value);
            if (sozlesme == null)
                return (false, "Sözleşme bulunamadı.", 0);
            if (sozlesme.Durum == LeaseStatus.Terminated)
                return (false, "Feshedilmiş sözleşme için manuel borç oluşturulamaz.", 0);
            if (sozlesme.KiraciId != model.KiraciId)
                return (false, "Seçilen kiracı, sözleşmenin kiracısıyla eşleşmiyor.", 0);
            kiraSozlesmesiId = sozlesme.Id;
        }

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
            CalculationMethod = CalculationMethod.Fixed,
            BirimDeger = model.Tutar,
            Carpan = 1m,
            Tutar = model.Tutar,
            KdvOrani = kdvOrani,
            KdvTutari = kdvTutari,
            ToplamTutar = toplamTutar,
            KaynakTipi = LineItemSourceType.ManualInput
        };

        var tahakkuk = new Tahakkuk
        {
            KiraciId = model.KiraciId,
            BirimId = model.BirimId,
            KiraSozlesmesiId = kiraSozlesmesiId,
            DonemBaslangic = model.VadeTarihi,
            DonemBitis = model.VadeTarihi.AddDays(1),
            VadeTarihi = model.VadeTarihi,
            BeklenenTutar = model.Tutar,
            KdvTutari = kdvTutari,
            ToplamTutar = toplamTutar,
            OdenenTutar = 0,
            Durum = ChargeStatus.Pending,
            KaynakTipi = ChargeSourceType.Manual,
            IptalNotu = model.Not,
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

        if (tahakkuk.Durum == ChargeStatus.Cancelled)
            return (false, "Bu kayıt zaten iptal edilmiş.");

        var odemeVar = tahakkuk.Odemeler.Any(o => o.Durum == PaymentStatus.Approved);
        if (odemeVar)
            return (false, "Ödemesi alınmış manuel borç iptal edilemez.");

        tahakkuk.Durum = ChargeStatus.Cancelled;
        tahakkuk.IptalNotu = string.IsNullOrEmpty(tahakkuk.IptalNotu)
            ? neden
            : $"{tahakkuk.IptalNotu} | İptal: {neden}";

        await _uow.SaveChangesAsync();

        return (true, null);
    }
}
