using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

// NOT: Bu servis cross-aggregate hesaplama yapar. Tek bir entity aggregate'ine ait değildir.
// Kullanılan repolar: IChargeRepository (ChargeTypes lookup)
public class StatisticsService : IStatisticsService
{
    private readonly IChargeRepository _tahakkukRepo;
    private readonly IRateResolverService _rateResolver;

    public StatisticsService(IChargeRepository tahakkukRepo, IRateResolverService rateResolver)
    {
        _tahakkukRepo = tahakkukRepo;
        _rateResolver = rateResolver;
    }

    public OccupancyStatus GetBirimDurumu(Unit birim)
    {
        var aktif = birim.Leases
            .Where(s =>
                s.Status == LeaseStatus.Active &&
                s.StartDate <= DateTime.Now &&
                s.EndDate >= DateTime.Now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault();

        if (aktif == null) return OccupancyStatus.Vacant;

        var kalanGun = (aktif.EndDate - DateTime.Now).Days;
        return kalanGun <= 30 ? OccupancyStatus.ExpiringSoon : OccupancyStatus.Leased;
    }

    public Lease? GetAktifSozlesme(Unit birim)
    {
        return birim.Leases
            .Where(s =>
                s.Status == LeaseStatus.Active &&
                s.StartDate <= DateTime.Now &&
                s.EndDate >= DateTime.Now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault();
    }

    public bool Aktif(Lease s) =>
        s.Status == LeaseStatus.Active &&
        s.StartDate <= DateTime.Now &&
        s.EndDate >= DateTime.Now;

    public async Task<decimal> AylikBedelAsync(Lease s)
    {
        var yuzolcumu = s.Unit?.Area ?? 0m;
        var tumBorcTipleri = await _tahakkukRepo.GetAktifUretimBorcTipleriAsync();
        var borcTipleri = tumBorcTipleri.Where(b => b.Behavior == ChargeTypeBehavior.MonthlyFixed).ToList();

        decimal toplam = 0m;
        var donem = DateTime.Today;
        foreach (var bt in borcTipleri)
        {
            var snap = await _rateResolver.ResolveAsync(s.Id, s.TenantId, s.UnitId, bt.Id, donem);
            if (snap == null) continue;
            toplam += snap.CalculationMethod == CalculationMethod.M2
                ? snap.UnitValue * yuzolcumu
                : snap.UnitValue;
        }
        return toplam;
    }

    public async Task<decimal> YillikBedelAsync(Lease s) => await AylikBedelAsync(s) * 12;

    public int KalanGun(Lease s) => (int)(s.EndDate - DateTime.Now).TotalDays;

    public double SureYuzdesi(Lease s)
    {
        var toplam = (s.EndDate - s.StartDate).TotalDays;
        var gecen = (DateTime.Now - s.StartDate).TotalDays;
        if (toplam <= 0) return 100;
        return Math.Min(100, Math.Max(0, gecen / toplam * 100));
    }

    public decimal TufeArtisliBedel(decimal mevcutBedel, decimal tufeOrani)
    {
        if (tufeOrani < 0) throw new ArgumentException("TÜFE oranı negatif olamaz.");
        return mevcutBedel + (mevcutBedel * tufeOrani / 100);
    }

    public decimal KdvTutari(decimal kdvHaricBedel, decimal kdvOrani)
    {
        if (kdvOrani < 0) throw new ArgumentException("KDV oranı negatif olamaz.");
        return kdvHaricBedel * kdvOrani / 100;
    }

    public decimal KdvDahilTutar(decimal kdvHaricBedel, decimal kdvOrani) =>
        kdvHaricBedel + KdvTutari(kdvHaricBedel, kdvOrani);

    public KiraHesaplamaSonucu HesaplaKiraArtisi(
        decimal mevcutKiraBedeli,
        decimal? tufeOrani,
        bool kdvUygulanacakMi,
        decimal? kdvOrani)
    {
        var sonuc = new KiraHesaplamaSonucu
        {
            MevcutKiraBedeli = mevcutKiraBedeli,
            TufeOrani = tufeOrani,
            KdvUygulandiMi = kdvUygulanacakMi,
            KdvRate = kdvUygulanacakMi ? (kdvOrani ?? 20) : null
        };

        var tufeArtisTutari = tufeOrani.HasValue
            ? mevcutKiraBedeli * tufeOrani.Value / 100
            : 0;

        var tufeSonrasiBedel = mevcutKiraBedeli + tufeArtisTutari;
        sonuc.TufeArtisTutari = tufeArtisTutari;
        sonuc.TufeSonrasiKiraBedeli = tufeSonrasiBedel;

        if (kdvUygulanacakMi)
        {
            var oran = kdvOrani ?? 20;
            sonuc.KdvTutari = tufeSonrasiBedel * oran / 100;
            sonuc.KdvDahilToplam = tufeSonrasiBedel + sonuc.KdvTutari;
        }
        else
        {
            sonuc.KdvTutari = 0;
            sonuc.KdvDahilToplam = tufeSonrasiBedel;
        }

        return sonuc;
    }
}
