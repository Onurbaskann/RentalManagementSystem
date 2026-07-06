using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ChargeGenerationService : IChargeGenerationService, ITransactionalService
{
    private readonly IChargeRepository _tahakkukRepo;
    private readonly IUnitOfWork _uow;
    private readonly IRateResolverService _rateResolver;
    private readonly ILeaseRepository _sozlesmeRepo;
    private readonly IUnitRepository _birimRepo;

    public ChargeGenerationService(
        IChargeRepository tahakkukRepo,
        IUnitOfWork uow,
        IRateResolverService rateResolver,
        ILeaseRepository sozlesmeRepo,
        IUnitRepository birimRepo)
    {
        _tahakkukRepo = tahakkukRepo;
        _uow = uow;
        _rateResolver = rateResolver;
        _sozlesmeRepo = sozlesmeRepo;
        _birimRepo = birimRepo;
    }

    public async Task UretSozlesmeIcinAsync(int leaseId)
    {
        var lease = await _sozlesmeRepo.GetByIdAsync(leaseId);
        if (lease == null) return;

        foreach (var donemIlkGunu in GetDonemler(lease.StartDate, lease.EndDate))
        {
            var mevcutVar = await _tahakkukRepo.AnyAsync(t => t.LeaseId == leaseId
                && t.PeriodStart == donemIlkGunu
                && t.SourceType == ChargeSourceType.Lease);
            if (mevcutVar) continue;

            var proRata = HesaplaProRataKatsayi(donemIlkGunu, lease.StartDate, lease.EndDate);
            var composedPreviews = await ComposeKalemlerAsync(lease.UnitId, lease.TenantId, donemIlkGunu, leaseId);
            var kalemler = new List<ChargeLineItem>();

            foreach (var preview in composedPreviews)
            {
                var kalemProRata = preview.Davranis == ChargeTypeBehavior.FirstMonthOneTime ? 1m : proRata;
                var tutar = Math.Round(preview.Amount * kalemProRata, 2);
                var kdvTutari = Math.Round(tutar * preview.KdvRate / 100, 2);

                kalemler.Add(new ChargeLineItem
                {
                    ChargeTypeId = preview.ChargeTypeId,
                    Description = preview.Aciklama ?? preview.ChargeTypeName,
                    CalculationMethod = preview.CalculationMethod,
                    UnitValue = preview.UnitValue,
                    Multiplier = Math.Round(preview.Multiplier * kalemProRata, 6),
                    Amount = tutar,
                    KdvRate = preview.KdvRate,
                    KdvAmount = kdvTutari,
                    TotalAmount = tutar + kdvTutari,
                    SourceType = preview.SourceType
                });
            }

            var ayBitis = donemIlkGunu.AddMonths(1).AddDays(-1);
            var donemBitis = lease.EndDate < ayBitis ? lease.EndDate : ayBitis;

            var charge = new Charge
            {
                TenantId = lease.TenantId,
                UnitId = lease.UnitId,
                LeaseId = leaseId,
                PeriodStart = donemIlkGunu,
                PeriodEnd = donemBitis,
                DueDate = HesaplaVadeTarihi(donemIlkGunu, lease.DueDateRuleType, lease.DueDay),
                ExpectedAmount = kalemler.Sum(k => k.Amount),
                KdvAmount = kalemler.Sum(k => k.KdvAmount),
                TotalAmount = kalemler.Sum(k => k.TotalAmount),
                PaidAmount = 0,
                Status = ChargeStatus.Pending,
                SourceType = ChargeSourceType.Lease,
                LineItems = kalemler
            };

            await _tahakkukRepo.AddAsync(charge);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task YenidenUretAsync(int leaseId, DateTime baslangicTarihi)
    {
        var ilkGun = new DateTime(baslangicTarihi.Year, baslangicTarihi.Month, 1);
        var silinecekler = await _tahakkukRepo.GetSilineceklerAsync(leaseId, ilkGun);
        await _tahakkukRepo.DeleteRangeAsync(silinecekler);
        await _uow.SaveChangesAsync();
        await UretSozlesmeIcinAsync(leaseId);
    }

    public async Task BekleyenVadeleriYenidenHesaplaAsync(int leaseId)
    {
        var lease = await _sozlesmeRepo.GetByIdAsync(leaseId);
        if (lease == null) return;

        var hedefDurumlar = new[] { ChargeStatus.Pending, ChargeStatus.PartiallyPaid, ChargeStatus.Overdue };
        var bekleyenler = await _tahakkukRepo.GetAllAsync(t =>
            t.LeaseId == leaseId
            && t.SourceType == ChargeSourceType.Lease
            && hedefDurumlar.Contains(t.Status));

        if (bekleyenler.Count == 0) return;

        var bugun = DateTime.Today;
        foreach (var t in bekleyenler)
        {
            t.DueDate = HesaplaVadeTarihi(t.PeriodStart, lease.DueDateRuleType, lease.DueDay);

            t.Status = t.PaidAmount >= t.TotalAmount
                ? ChargeStatus.Paid
                : t.PaidAmount > 0
                    ? ChargeStatus.PartiallyPaid
                    : bugun > t.DueDate
                        ? ChargeStatus.Overdue
                        : ChargeStatus.Pending;
        }

        await _uow.SaveChangesAsync();
    }

    public async Task IptalEtFutureTahakkuklarAsync(int leaseId, DateTime fesihTarihi)
    {
        var ilkGun = new DateTime(fesihTarihi.Year, fesihTarihi.Month, 1).AddMonths(1);
        var iptalEdilecekler = await _tahakkukRepo.GetAllAsync(t =>
            t.LeaseId == leaseId
            && t.PeriodStart >= ilkGun
            && t.Status != ChargeStatus.Paid
            && t.SourceType == ChargeSourceType.Lease);

        foreach (var t in iptalEdilecekler)
            t.Status = ChargeStatus.Cancelled;

        if (iptalEdilecekler.Count > 0)
            await _uow.SaveChangesAsync();
    }

    private static DateTime HesaplaVadeTarihi(DateTime donemIlkGunu, DueDateRuleType tip, int vadeGunu)
    {
        return tip switch
        {
            DueDateRuleType.FixedDayOfMonth =>
                new DateTime(donemIlkGunu.Year, donemIlkGunu.Month,
                    Math.Min(Math.Max(vadeGunu, 1), DateTime.DaysInMonth(donemIlkGunu.Year, donemIlkGunu.Month))),
            DueDateRuleType.PeriodStartOffset =>
                donemIlkGunu.AddDays(Math.Max(vadeGunu - 1, 0)),
            _ => donemIlkGunu
        };
    }

    private static decimal HesaplaProRataKatsayi(DateTime donemIlkGunu, DateTime sozlesmeBaslangic, DateTime sozlesmeBitis)
    {
        var ayBitis = donemIlkGunu.AddMonths(1).AddDays(-1);
        var etkinBaslangic = sozlesmeBaslangic > donemIlkGunu ? sozlesmeBaslangic : donemIlkGunu;
        var etkinBitis = sozlesmeBitis < ayBitis ? sozlesmeBitis : ayBitis;

        if (etkinBaslangic == donemIlkGunu && etkinBitis == ayBitis)
            return 1.0m;

        var gunSayisi = (etkinBitis - etkinBaslangic).Days + 1;
        return Math.Min(1.0m, (decimal)gunSayisi / 30m);
    }

    private static IEnumerable<DateTime> GetDonemler(DateTime baslangic, DateTime bitis)
    {
        var ay = new DateTime(baslangic.Year, baslangic.Month, 1);
        var sonAy = new DateTime(bitis.Year, bitis.Month, 1);
        while (ay <= sonAy)
        {
            yield return ay;
            ay = ay.AddMonths(1);
        }
    }

    public async Task<IList<Models.DTOs.TahakkukKalemiPreview>> ComposeKalemlerAsync(int unitId, int tenantId, DateTime donem, int? leaseId = null)
    {
        var unit = await _birimRepo.GetByIdAsync(unitId);
        if (unit == null) return new List<Models.DTOs.TahakkukKalemiPreview>();

        var aktifBorcTipleri = await _tahakkukRepo.GetAktifUretimBorcTipleriAsync();
        var previewList = new List<Models.DTOs.TahakkukKalemiPreview>();

        foreach (var bt in aktifBorcTipleri)
        {
            if (bt.Behavior == ChargeTypeBehavior.FirstMonthOneTime)
            {
                DateTime? start = null;
                if (leaseId.HasValue)
                {
                    start = await _sozlesmeRepo.GetByIdAsync<DateTime?>(leaseId.Value, s => s.StartDate);
                }
                else
                {
                    start = donem;
                }

                if (start.HasValue && (donem.Year != start.Value.Year || donem.Month != start.Value.Month))
                    continue;
            }

            RateSnapshot? snapshot = await _rateResolver.ResolveAsync(leaseId, tenantId, unitId, bt.Id, donem);

            if (snapshot != null)
            {
                var carpanBase = snapshot.CalculationMethod == CalculationMethod.M2 ? unit.Area : 1m;
                var tutar = Math.Round(snapshot.UnitValue * carpanBase, 2);
                var kdvTutari = Math.Round(tutar * snapshot.KdvRate / 100, 2);

                previewList.Add(new Models.DTOs.TahakkukKalemiPreview
                {
                    ChargeTypeId = bt.Id,
                    ChargeTypeName = bt.Name,
                    ChargeTypeCode = bt.Code,
                    Davranis = bt.Behavior,
                    CalculationMethod = snapshot.CalculationMethod,
                    UnitValue = snapshot.UnitValue,
                    Multiplier = carpanBase,
                    Amount = tutar,
                    KdvRate = snapshot.KdvRate,
                    KdvTutari = kdvTutari,
                    ToplamTutar = tutar + kdvTutari,
                    SourceType = snapshot.SourceType,
                    RateBulundu = true,
                    Aciklama = bt.Name
                });
            }
            else
            {
                previewList.Add(new Models.DTOs.TahakkukKalemiPreview
                {
                    ChargeTypeId = bt.Id,
                    ChargeTypeName = bt.Name,
                    ChargeTypeCode = bt.Code,
                    Davranis = bt.Behavior,
                    CalculationMethod = CalculationMethod.Fixed,
                    UnitValue = 0m,
                    Multiplier = 0m,
                    Amount = 0m,
                    KdvRate = 0m,
                    KdvTutari = 0m,
                    ToplamTutar = 0m,
                    SourceType = LineItemSourceType.UndefinedRate,
                    RateBulundu = false,
                    Aciklama = $"{bt.Name} (Fiyat Tanımsız)"
                });
            }
        }

        return previewList;
    }
}
