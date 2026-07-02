using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

// NOT: Bu servis cross-aggregate hesaplama yapar. Tek bir entity aggregate'ine ait değildir.
// Kullanılan repolar: ITahakkukRepository (BorcTipleri lookup)
public class IstatistikService : IIstatistikService
{
    private readonly ITahakkukRepository _tahakkukRepo;
    private readonly IRateResolverService _rateResolver;

    public IstatistikService(ITahakkukRepository tahakkukRepo, IRateResolverService rateResolver)
    {
        _tahakkukRepo = tahakkukRepo;
        _rateResolver = rateResolver;
    }

    public OccupancyStatus GetBirimDurumu(Birim birim)
    {
        var aktif = birim.Sozlesmeler
            .Where(s =>
                s.Durum == LeaseStatus.Active &&
                s.BaslangicTarihi <= DateTime.Now &&
                s.BitisTarihi >= DateTime.Now)
            .OrderByDescending(s => s.BitisTarihi)
            .FirstOrDefault();

        if (aktif == null) return OccupancyStatus.Vacant;

        var kalanGun = (aktif.BitisTarihi - DateTime.Now).Days;
        return kalanGun <= 30 ? OccupancyStatus.ExpiringSoon : OccupancyStatus.Leased;
    }

    public Sozlesme? GetAktifSozlesme(Birim birim)
    {
        return birim.Sozlesmeler
            .Where(s =>
                s.Durum == LeaseStatus.Active &&
                s.BaslangicTarihi <= DateTime.Now &&
                s.BitisTarihi >= DateTime.Now)
            .OrderByDescending(s => s.BitisTarihi)
            .FirstOrDefault();
    }

    public bool Aktif(Sozlesme s) =>
        s.Durum == LeaseStatus.Active &&
        s.BaslangicTarihi <= DateTime.Now &&
        s.BitisTarihi >= DateTime.Now;

    public async Task<decimal> AylikBedelAsync(Sozlesme s)
    {
        var yuzolcumu = s.Birim?.Yuzolcumu ?? 0m;
        var tumBorcTipleri = await _tahakkukRepo.GetAktifUretimBorcTipleriAsync();
        var borcTipleri = tumBorcTipleri.Where(b => b.Davranis == ChargeTypeBehavior.MonthlyFixed).ToList();

        decimal toplam = 0m;
        var donem = DateTime.Today;
        foreach (var bt in borcTipleri)
        {
            var snap = await _rateResolver.ResolveAsync(s.Id, s.KiraciId, s.BirimId, bt.Id, donem);
            if (snap == null) continue;
            toplam += snap.CalculationMethod == CalculationMethod.M2
                ? snap.BirimDeger * yuzolcumu
                : snap.BirimDeger;
        }
        return toplam;
    }

    public async Task<decimal> YillikBedelAsync(Sozlesme s) => await AylikBedelAsync(s) * 12;

    public int KalanGun(Sozlesme s) => (int)(s.BitisTarihi - DateTime.Now).TotalDays;

    public double SureYuzdesi(Sozlesme s)
    {
        var toplam = (s.BitisTarihi - s.BaslangicTarihi).TotalDays;
        var gecen = (DateTime.Now - s.BaslangicTarihi).TotalDays;
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
            KdvOrani = kdvUygulanacakMi ? (kdvOrani ?? 20) : null
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
