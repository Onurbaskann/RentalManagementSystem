using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class ReservationService : IReservationService, ITransactionalService
{
    private readonly IReservationRepository _repo;
    private readonly IRezervasyonTarifeRepository _tarifeRepo;
    private readonly IUnitRepository _birimRepo;
    private readonly ITenantRepository _kiraciRepo;
    private readonly IUnitOfWork _uow;
    private readonly ApplicationDbContext _ctx;
    public ReservationService(
        IReservationRepository repo,
        IRezervasyonTarifeRepository tarifeRepo,
        IUnitRepository birimRepo,
        ITenantRepository kiraciRepo,
        IUnitOfWork uow,
        ApplicationDbContext ctx)
    {
        _repo = repo;
        _tarifeRepo = tarifeRepo;
        _birimRepo = birimRepo;
        _kiraciRepo = kiraciRepo;
        _uow = uow;
        _ctx = ctx;
    }

    // ── Listeleme ──────────────────────────────────────────────────────────────

    public async Task<List<RezervasyonListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetListAsync(tasinmazIds?.ToList());
    }

    public async Task<RezervasyonListItemDto?> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    // ── Ücret Hesaplama (precedence: birime özel → unit türü genel tarife → hata) ─

    public async Task<RezervasyonHesapSonucu> HesaplaAsync(int unitId, DateTime baslangic, DateTime bitis)
    {
        var sonuc = new RezervasyonHesapSonucu();

        if (bitis <= baslangic)
        {
            sonuc.HataMessaji = "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.";
            return sonuc;
        }

        // 1) Birime özel kural
        var kural = await _repo.GetAktifTarifeForBirimAsync(unitId);

        int ucretsiz;
        int periyot;
        decimal ucret;
        decimal kdv;

        if (kural != null)
        {
            ucretsiz = kural.FreeDurationMinutes;
            periyot = kural.UcretlendirmePeriyoduDakika;
            ucret = kural.PeriyotUcreti;
            kdv = kural.KdvRate;
            sonuc.KuralBulundu = true;
        }
        else
        {
            // 2) Unit Türü bazlı Yıllık Genel Tarife
            var unit = await _birimRepo.GetByIdAsync(unitId, q => q.Include(b => b.UnitType));

            if (unit?.UnitTypeId is not int btId)
            {
                sonuc.HataMessaji = "Unit türü tanımlanmamış.";
                return sonuc;
            }

            int cariYil = baslangic.Year;
            var genel = await _repo.GetGenelTarifeAsync(btId, cariYil);

            if (genel == null)
            {
                sonuc.HataMessaji = $"{cariYil} yılı için '{unit.UnitType?.Ad}' türünde genel reservation tarifesi tanımlı değil.";
                return sonuc;
            }

            ucretsiz = genel.FreeDurationMinutes;
            periyot = genel.UcretlendirmePeriyoduDakika;
            ucret = genel.PeriyotUcreti;
            kdv = genel.KdvRate;
            sonuc.KuralBulundu = true;
        }

        var toplamDakika = (int)Math.Ceiling((bitis - baslangic).TotalMinutes);
        var ucretliDakika = Math.Max(0, toplamDakika - ucretsiz);
        var periyotSayisi = ucretliDakika == 0
            ? 0
            : (int)Math.Ceiling((double)ucretliDakika / periyot);

        sonuc.TotalDurationMinutes = toplamDakika;
        sonuc.FreeDurationMinutes = Math.Min(ucretsiz, toplamDakika);
        sonuc.PaidDurationMinutes = ucretliDakika;
        sonuc.UcretliPeriyotSayisi = periyotSayisi;
        sonuc.UnitRate = ucret;
        sonuc.RateAmount = periyotSayisi * ucret;
        sonuc.KdvRate = kdv;
        sonuc.KdvTutari = Math.Round(sonuc.RateAmount * kdv / 100, 2);
        sonuc.ToplamTutar = sonuc.RateAmount + sonuc.KdvTutari;

        return sonuc;
    }

    // ── Reservation Oluşturma ─────────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata, int ReservationId)> CreateAsync(
        RezervasyonCreateViewModel model, string userId)
    {
        if (model.EndDate <= model.StartDate)
            return (false, "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", 0);

        // 8.5.4 — Çakışma kontrolü
        if (await _repo.IsConflictAsync(model.BirimId.Value, model.StartDate, model.EndDate))
            return (false, "Seçilen zaman aralığında bu unit için başka bir reservation mevcut.", 0);

        var tenant = await _kiraciRepo.GetByIdAsync(model.KiraciId.Value);
        if (tenant == null)
            return (false, "Kiracı bulunamadı.", 0);

        var unit = await _birimRepo.GetByIdAsync(model.BirimId.Value, q => q.Include(b => b.UnitType));
        if (unit == null)
            return (false, "Unit bulunamadı.", 0);
        if (unit.UnitType == null || !unit.UnitType.RezervasyonYapilabilirMi)
            return (false, "Seçilen unit reservation yapılabilir türde değil.", 0);

        var hesap = await HesaplaAsync(model.BirimId.Value, model.StartDate, model.EndDate);

        var reservation = new Reservation
        {
            UnitId = model.BirimId.Value,
            TenantId = model.KiraciId.Value,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            TotalDurationMinutes = hesap.TotalDurationMinutes,
            FreeDurationMinutes = hesap.FreeDurationMinutes,
            PaidDurationMinutes = hesap.PaidDurationMinutes,
            UnitRate = hesap.UnitRate,
            RateAmount = hesap.RateAmount,
            KdvRate = hesap.KdvRate > 0 ? hesap.KdvRate : null,
            KdvAmount = hesap.KdvTutari > 0 ? hesap.KdvTutari : null,
            TotalAmount = hesap.ToplamTutar,
            Status = ReservationStatus.Planned,
            Description = model.Aciklama,
        };

        await _repo.AddAsync(reservation);
        await _uow.SaveChangesAsync();

        return (true, null, reservation.Id);
    }

    // ── İptal ────────────────────────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata)> CancelAsync(int id, string userId, string neden)
    {
        var reservation = await _repo.GetByIdAsync(id, (Func<IQueryable<Reservation>, IQueryable<Reservation>>?)(q => q));

        if (reservation == null)
            return (false, "Reservation bulunamadı.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return (false, "Bu reservation zaten iptal edilmiş.");

        if (reservation.Status == ReservationStatus.TransferredToCharge)
        {
            var charge = await _ctx.Charges
                .Include(t => t.Allocations)
                .FirstOrDefaultAsync(t => t.ReservationId == reservation.Id);

            var odemeVar = charge?.Allocations.Any(o => o.Status == PaymentStatus.Approved) ?? false;
            if (odemeVar)
                return (false, "Ödemesi alınmış tahakkuka bağlı reservation iptal edilemez.");

            if (charge != null)
            {
                charge.Status = ChargeStatus.Cancelled;
                charge.CancellationNote = $"Reservation iptal edildi: {neden}";
            }
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.Description = string.IsNullOrWhiteSpace(reservation.Description)
            ? $"İptal: {neden}"
            : $"{reservation.Description} | İptal: {neden}";

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ── Tahakkuka Aktar (8.6.2) ──────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata, int? ChargeId)> TransferToChargeAsync(int id, string userId)
    {
        var reservation = await _repo.GetByIdAsync(id, q => q
            .Include(r => r.Unit).ThenInclude(b => b.UnitType));

        if (reservation == null)
            return (false, "Reservation bulunamadı.", null);

        if (reservation.Status != ReservationStatus.Planned)
            return (false, "Sadece 'Planlandı' durumundaki rezervasyonlar tahakkuka aktarılabilir.", null);

        if (await _ctx.Charges.AnyAsync(t => t.ReservationId == reservation.Id))
            return (false, "Bu reservation zaten tahakkuka aktarılmış.", null);

        if (reservation.TotalAmount <= 0)
            return (false, "Ücretsiz rezervasyonlar için charge oluşturulamaz.", null);

        var birimTuru = reservation.Unit.UnitType;
        var borcTipi = await _repo.ResolveRezervasyonBorcTipiAsync(birimTuru?.ChargeTypeId);

        if (borcTipi == null)
            return (false, "Reservation borç tipi bulunamadı. Lütfen yöneticinize başvurun.", null);

        var aciklama = $"Toplantı salonu: {reservation.Unit.Name} " +
                       $"({reservation.StartDate:dd.MM.yyyy HH:mm} – {reservation.EndDate:HH:mm})";

        var kalem = new ChargeLineItem
        {
            ChargeTypeId = borcTipi.Id,
            Description = aciklama,
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = reservation.RateAmount,
            Multiplier = 1m,
            Amount = reservation.RateAmount,
            KdvRate = reservation.KdvRate ?? 0m,
            KdvAmount = reservation.KdvAmount ?? 0m,
            TotalAmount = reservation.TotalAmount,
            SourceType = LineItemSourceType.ReservationRule
        };

        var charge = new Charge
        {
            TenantId = reservation.TenantId,
            UnitId = reservation.UnitId,
            ReservationId = reservation.Id,
            PeriodStart = reservation.StartDate,
            PeriodEnd = reservation.EndDate,
            DueDate = reservation.EndDate.Date,
            ExpectedAmount = reservation.RateAmount,
            KdvAmount = reservation.KdvAmount ?? 0m,
            TotalAmount = reservation.TotalAmount,
            PaidAmount = 0,
            Status = ChargeStatus.Pending,
            SourceType = ChargeSourceType.Reservation,
            LineItems = new List<ChargeLineItem> { kalem }
        };

        await _repo.AddTahakkukAsync(charge);
        reservation.Status = ReservationStatus.TransferredToCharge;
        await _uow.SaveChangesAsync();

        return (true, null, charge.Id);
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
            kural = new RezervasyonTarife();
            await _repo.AddUcretKuralAsync(kural);
        }
        else
        {
            kural = await _repo.GetUcretKuralByIdAsync(model.Id)
                    ?? throw new InvalidOperationException("Kural bulunamadı.");
        }

        kural.UnitId = model.BirimId;
        kural.FreeDurationMinutes = model.FreeDurationMinutes;
        kural.UcretlendirmePeriyoduDakika = model.UcretlendirmePeriyoduDakika;
        kural.PeriyotUcreti = model.PeriyotUcreti;
        kural.KdvRate = model.KdvRate;
        kural.IsActive = model.IsActive;
        kural.Aciklama = model.Aciklama;

        await _uow.SaveChangesAsync();
        return (true, null, kural.Id);
    }

    public async Task<(bool Basarili, string? Hata)> ToggleUcretKuralAktifAsync(int id)
    {
        var kural = await _repo.GetUcretKuralByIdAsync(id);
        if (kural == null)
            return (false, "Kural bulunamadı.");

        kural.IsActive = !kural.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
