using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services;

public class IstatistikService
{
    private readonly DummyDataService _data;

    public IstatistikService(DummyDataService data)
    {
        _data = data;
    }

    public KiraDurumu GetBirimDurumu(Birim birim)
    {
        var aktif = birim.Sozlesmeler
            .Where(s =>
                s.Durum == SozlesmeDurumu.Aktif &&
                s.BaslangicTarihi <= DateTime.Now &&
                s.BitisTarihi >= DateTime.Now)
            .OrderByDescending(s => s.BitisTarihi)
            .FirstOrDefault();

        if (aktif == null) return KiraDurumu.Bos;

        var kalanGun = (aktif.BitisTarihi - DateTime.Now).Days;
        return kalanGun <= 30 ? KiraDurumu.SuresiDolmakUzere : KiraDurumu.Kirali;
    }

    public KiraSozlesmesi? GetAktifSozlesme(Birim birim)
    {
        return birim.Sozlesmeler
            .Where(s =>
                s.Durum == SozlesmeDurumu.Aktif &&
                s.BaslangicTarihi <= DateTime.Now &&
                s.BitisTarihi >= DateTime.Now)
            .OrderByDescending(s => s.BitisTarihi)
            .FirstOrDefault();
    }

    public bool Aktif(KiraSozlesmesi s) =>
        s.Durum == SozlesmeDurumu.Aktif &&
        s.BaslangicTarihi <= DateTime.Now &&
        s.BitisTarihi >= DateTime.Now;

    public decimal AylikBedel(KiraSozlesmesi s) =>
        s.Periyot == KiraPeriyodu.Yillik ? s.KiraBedeli / 12 : s.KiraBedeli;

    public decimal YillikBedel(KiraSozlesmesi s) =>
        s.Periyot == KiraPeriyodu.Aylik ? s.KiraBedeli * 12 : s.KiraBedeli;

    public int KalanGun(KiraSozlesmesi s) => (int)(s.BitisTarihi - DateTime.Now).TotalDays;

    public double SureYuzdesi(KiraSozlesmesi s)
    {
        var toplam = (s.BitisTarihi - s.BaslangicTarihi).TotalDays;
        var gecen = (DateTime.Now - s.BaslangicTarihi).TotalDays;
        if (toplam <= 0) return 100;
        return Math.Min(100, Math.Max(0, gecen / toplam * 100));
    }

    public decimal ToplamAylikGelir()
    {
        return _data.Sozlesmeler
            .Where(Aktif)
            .Sum(AylikBedel);
    }

    public decimal ToplamYillikProj()
    {
        return _data.Sozlesmeler
            .Where(Aktif)
            .Sum(YillikBedel);
    }

    // --- TÜFE / KDV Hesaplama ---

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
