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
    private readonly IChargeRepository _tahakkukRepo;
    private readonly ISozlesmeRepository _sozlesmeRepo;
    private readonly IBorcTipiRepository _borcTipiRepo;
    private readonly ITasinmazRepository _tasinmazRepo;
    private readonly IUnitOfWork _uow;

    public ManuelBorcService(
        IChargeRepository tahakkukRepo,
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
    public async Task<(bool Basarili, string? Hata, int ChargeId)> CreateAsync(
        ManuelBorcCreateViewModel model, string userId)
    {
        if (model.KiraciId <= 0)
            return (false, "Kiracı seçilmelidir.", 0);
        if (model.BirimId <= 0)
            return (false, "Unit seçilmelidir.", 0);

        int? kiraSozlesmesiId = null;
        if (model.SozlesmeId.HasValue && model.SozlesmeId.Value > 0)
        {
            var sozlesme = await _sozlesmeRepo.GetByIdAsync(model.SozlesmeId.Value);
            if (sozlesme == null)
                return (false, "Sözleşme bulunamadı.", 0);
            if (sozlesme.Status == LeaseStatus.Terminated)
                return (false, "Feshedilmiş sözleşme için manuel borç oluşturulamaz.", 0);
            if (sozlesme.TenantId != model.KiraciId)
                return (false, "Seçilen kiracı, sözleşmenin kiracısıyla eşleşmiyor.", 0);
            kiraSozlesmesiId = sozlesme.Id;
        }

        var borcTipi = await _borcTipiRepo.GetActiveManuelByIdAsync(model.ChargeTypeId);
        if (borcTipi == null)
            return (false, "Geçersiz borç tipi.", 0);

        if (model.Amount <= 0)
            return (false, "Amount sıfırdan büyük olmalıdır.", 0);

        var kdvTutari = model.KdvUygulanacakMi
            ? Math.Round(model.Amount * model.KdvRate / 100, 2)
            : 0m;
        var toplamTutar = model.Amount + kdvTutari;
        var kdvOrani = model.KdvUygulanacakMi ? model.KdvRate : 0m;

        var kalem = new ChargeLineItem
        {
            ChargeTypeId = borcTipi.Id,
            Description = model.Aciklama,
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = model.Amount,
            Multiplier = 1m,
            Amount = model.Amount,
            KdvRate = kdvOrani,
            KdvAmount = kdvTutari,
            TotalAmount = toplamTutar,
            SourceType = LineItemSourceType.ManualInput
        };

        var tahakkuk = new Charge
        {
            TenantId = model.KiraciId,
            UnitId = model.BirimId,
            LeaseId = kiraSozlesmesiId,
            PeriodStart = model.DueDate,
            PeriodEnd = model.DueDate.AddDays(1),
            DueDate = model.DueDate,
            ExpectedAmount = model.Amount,
            KdvAmount = kdvTutari,
            TotalAmount = toplamTutar,
            PaidAmount = 0,
            Status = ChargeStatus.Pending,
            SourceType = ChargeSourceType.Manual,
            CancellationNote = model.Not,
            LineItems = new List<ChargeLineItem> { kalem }
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

        if (tahakkuk.Status == ChargeStatus.Cancelled)
            return (false, "Bu kayıt zaten iptal edilmiş.");

        var odemeVar = tahakkuk.Allocations.Any(o => o.Status == PaymentStatus.Approved);
        if (odemeVar)
            return (false, "Ödemesi alınmış manuel borç iptal edilemez.");

        tahakkuk.Status = ChargeStatus.Cancelled;
        tahakkuk.CancellationNote = string.IsNullOrEmpty(tahakkuk.CancellationNote)
            ? neden
            : $"{tahakkuk.CancellationNote} | İptal: {neden}";

        await _uow.SaveChangesAsync();

        return (true, null);
    }
}
