using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IIstatistikService
{
    KiraDurumu GetBirimDurumu(Birim birim);
    KiraSozlesmesi? GetAktifSozlesme(Birim birim);
    bool Aktif(KiraSozlesmesi s);
    Task<decimal> AylikBedelAsync(KiraSozlesmesi s);
    Task<decimal> YillikBedelAsync(KiraSozlesmesi s);
    int KalanGun(KiraSozlesmesi s);
    double SureYuzdesi(KiraSozlesmesi s);
    decimal TufeArtisliBedel(decimal mevcutBedel, decimal tufeOrani);
    decimal KdvTutari(decimal kdvHaricBedel, decimal kdvOrani);
    decimal KdvDahilTutar(decimal kdvHaricBedel, decimal kdvOrani);
    KiraHesaplamaSonucu HesaplaKiraArtisi(decimal mevcutKiraBedeli, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani);
}
